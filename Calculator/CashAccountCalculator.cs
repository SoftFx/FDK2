using System;
using System.Collections.Generic;
using TickTrader.FDK.Common;

namespace TickTrader.FDK.Calculator
{
    public class CashAccountCalculator : IDisposable
    {
        private readonly ICashAccountInfo account;
        private readonly Dictionary<string, IAssetModel> assets = new Dictionary<string, IAssetModel>();
        private MarketStateBase market;

        public MarketStateBase Market
        {
            get { return market; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value), @"Market property cannot be null.");

                if (ReferenceEquals(market, value))
                    return;

                market = value;
            }
        }

        public CashAccountCalculator(ICashAccountInfo infoProvider, MarketStateBase market)
        {
            if (infoProvider == null)
                throw new ArgumentNullException("infoProvider");

            if (market == null)
                throw new ArgumentNullException("market");

            this.account = infoProvider;
            this.market = market;

            if (this.account.Assets != null)
                this.account.Assets.Foreach(a => AddRemoveAsset(a, AssetChangeTypes.Added));
            this.account.AssetsChanged += AddRemoveAsset;
            this.AddOrdersBunch(this.account.Orders);
            this.account.OrderAdded += AddOrder;
            this.account.OrderRemoved += RemoveOrder;
            this.account.OrdersAdded += AddOrdersBunch;
            //this.account.OrderReplaced += UpdateOrder;
        }

        public bool HasSufficientMarginToOpenOrder(IOrderModel order, decimal? marginMovement, out IAssetModel marginAsset)
        {
            var symbol = order.SymbolInfo ?? throw CreateNoSymbolException(order.Symbol);
            return HasSufficientMarginToOpenOrder(order.Type, order.Side, symbol, marginMovement, out marginAsset);
        }

        public bool HasSufficientMarginToOpenOrder(OrderType type, OrderSide side, ISymbolInfo symbol, decimal? marginMovement, out IAssetModel marginAsset)
        {
            //if (order == null)
            //    throw new ArgumentNullException("order");

            //if (type != OrderTypes.Limit && type != OrderTypes.StopLimit)
            //    throw new ArgumentException("Invalid Order Type", "order");

            //if (type == OrderTypes.Stop || type == OrderTypes.StopLimit)
            //{
            //    if (stopPrice == null || stopPrice <= 0)
            //        throw new ArgumentException("Invalid Stop Price", "order");
            //}

            //if (type != OrderTypes.Stop)
            //{
            //    if (price == null || price <= 0)
            //        throw new ArgumentException("Invalid Price", "order");
            //}

            //if (order.Amount <= 0)
            //    throw new ArgumentException("Invalid Amount", "order");

            if (marginMovement == null)
                throw new MarginNotCalculatedException("Provided order must have calculated Margin.");

            marginAsset = GetMarginAsset(symbol, side);
            if (marginAsset == null || marginAsset.Amount == 0)
                throw new NotEnoughMoneyException($"Asset {GetMarginAssetCurrency(symbol, side)} is empty.");

            if (marginMovement.Value > marginAsset.FreeAmount)
                throw new NotEnoughMoneyException($"{marginAsset}, Margin={marginAsset.Margin}, MarginMovement={marginMovement.Value}.");

            return true;
        }

        public static decimal CalculateMarginFactor(IOrderCalcInfo order, ISymbolInfo symbol)
        {
            return CalculateMarginFactor(order.Type, symbol, order.IsHidden);
        }

        public static decimal CalculateMarginFactor(OrderType type, ISymbolInfo symbol, bool isHidden)
        {
            decimal combinedMarginFactor = 1.0M;
            if (type == OrderType.Stop || type == OrderType.StopLimit)
                combinedMarginFactor *= (decimal)symbol.StopOrderMarginReduction;
            else if (type == OrderType.Limit && isHidden)
                combinedMarginFactor *= (decimal)symbol.HiddenLimitOrderMarginReduction;
            return combinedMarginFactor;
        }

        public static decimal CalculateMargin(IOrderCalcInfo order, ISymbolInfo symbol)
        {
            return CalculateMargin(order.Type, order.RemainingAmount, order.Price, order.StopPrice, order.Side, symbol, order.IsHidden);
        }

        public static decimal CalculateMargin(OrderType type, decimal amount, decimal? orderPrice, decimal? orderStopPrice, OrderSide side, ISymbolInfo symbol, bool isHidden)
        {
            decimal combinedMarginFactor = CalculateMarginFactor(type, symbol, isHidden);

            decimal price = ((type == OrderType.Stop) || (type == OrderType.StopLimit)) ? orderStopPrice.Value : orderPrice.Value;

            if (side == OrderSide.Buy)
                return combinedMarginFactor * amount * price;
            else
                return combinedMarginFactor * amount;
        }

        public decimal CalculateCurrentPrice(IOrderCalcInfo order, out CalcError error)
        {
            return CalculateCurrentPrice(order.Side, order.Symbol, out error);
        }

        public decimal CalculateCurrentPrice(OrderSide side, string symbol, out CalcError error)
        {
            var node = market.GetSymbolNode(symbol, false);
            if (node == null)
            {
                error = new MisconfigurationError("Symbol Not Found: " + symbol);
                return 0;
            }

            if (side == OrderSide.Sell)
                return (decimal)node.GetBidOrError(out error);
            else
                return (decimal)node.GetAskOrError(out error);
        }

        public IAssetModel GetMarginAsset(IOrderModel order)
        {
            return GetMarginAsset(order, out _);
        }

        public IAssetModel GetMarginAsset(IOrderModel order, out ISymbolInfo symbol)
        {
            //if (order.MarginCurrency == null || order.ProfitCurrency == null)
            //    throw new MarketConfigurationException("Order must have both margin & profit currencies specified.");

            symbol = order.SymbolInfo ?? throw CreateNoSymbolException(order.Symbol);
            return assets.GetOrDefault(GetMarginAssetCurrency(symbol, order.Side));
        }

        public IAssetModel GetMarginAsset(ISymbolInfo symbol, OrderSide side)
        {
            //if (order.MarginCurrency == null || order.ProfitCurrency == null)
            //    throw new MarketConfigurationException("Order must have both margin & profit currencies specified.");

            return assets.GetOrDefault(GetMarginAssetCurrency(symbol, side));
        }

        public string GetMarginAssetCurrency(ISymbolInfo smb, OrderSide side)
        {
            //var symbol = smb ?? throw CreateNoSymbolException(smb.Name);

            return (side == OrderSide.Buy) ? smb.ProfitCurrency : smb.MarginCurrency;
        }

        public void AddRemoveAsset(IAssetModel asset, AssetChangeTypes changeType)
        {
            if (changeType == AssetChangeTypes.Added)
                this.assets.Add(asset.Currency, asset);
            else if (changeType == AssetChangeTypes.Removed)
                this.assets.Remove(asset.Currency);
            else if (changeType == AssetChangeTypes.Replaced)
            {
                var oldAsset = this.assets[asset.Currency];
                this.assets[asset.Currency] = asset;
                asset.Margin = oldAsset.Margin;
            }
        }

        public void AddOrder(IOrderModel order)
        {
            var symbol = order.SymbolInfo ?? throw CreateNoSymbolException(order.Symbol);
            order.CashMargin = CalculateMargin(order, symbol);
            //order.Margin = margin;
            //OrderLightClone clone = new OrderLightClone(order);
            //orders.Add(order.OrderId, clone);

            IAssetModel marginAsset = GetMarginAsset(order);
            if (marginAsset != null)
                marginAsset.Margin += order.CashMargin;

            order.EssentialsChanged += OnOrderChanged;
        }

        public void OnOrderChanged(OrderEssentialsChangeArgs args)
        {
            var order = args.Order;
            var symbol = order.SymbolInfo ?? throw CreateNoSymbolException(order.Symbol);
            //OrderLightClone clone = GetOrderOrThrow(order.OrderId);
            IAssetModel marginAsset = GetMarginAsset(order);
            marginAsset.Margin -= order.CashMargin;
            order.CashMargin = CalculateMargin(order, symbol);
            marginAsset.Margin += order.CashMargin;

            //OrderLightClone newClone = new OrderLightClone(order);
            //orders[order.OrderId] = newClone;

            //if (clone.OrderModelRef != order) // resubscribe if order model is replaced
            //{
            //    clone.OrderModelRef.EssentialParametersChanged -= UpdateOrder;
            //    order.EssentialParametersChanged += UpdateOrder;
            //}
        }

        public void AddOrdersBunch(IEnumerable<IOrderModel> bunch)
        {
            bunch.Foreach(AddOrder);
        }

        public void RemoveOrder(IOrderModel order)
        {
            //OrderLightClone clone = GetOrderOrThrow(order.OrderId);
            //orders.Remove(order.OrderId);

            IAssetModel marginAsset = GetMarginAsset(order);
            if (marginAsset != null)
                marginAsset.Margin -= order.CashMargin;

            order.EssentialsChanged -= OnOrderChanged;
        }

        //OrderLightClone GetOrderOrThrow(long orderId)
        //{
        //    OrderLightClone clone;
        //    if (!orders.TryGetValue(orderId, out clone))
        //        throw new InvalidOperationException("Order Not Found: " + orderId);
        //    return clone;
        //}

        public void Dispose()
        {
            this.account.AssetsChanged -= AddRemoveAsset;
            this.account.OrderAdded -= AddOrder;
            this.account.OrderRemoved -= RemoveOrder;
            this.account.OrdersAdded -= AddOrdersBunch;
            //this.account.OrderReplaced -= UpdateOrder;

            foreach (var order in account.Orders)
            {
                //orders.Remove(order.OrderId);
                order.EssentialsChanged -= OnOrderChanged;
            }
        }

        private Exception CreateNoSymbolException(string smbName)
        {
            return new MarketConfigurationException("Symbol not found: " + smbName);
        }

        
    }
}
