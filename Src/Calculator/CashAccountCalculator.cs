using System;
using System.Collections.Generic;
using TickTrader.FDK.Calculator.Netting;

namespace TickTrader.FDK.Calculator
{
    public class CashAccountCalculator : IDisposable
    {
        private readonly ICashAccountInfo account;
        private readonly Dictionary<string, IAssetModel> assets = new Dictionary<string, IAssetModel>();
        private readonly Dictionary<long, OrderLightClone> orders = new Dictionary<long, OrderLightClone>();
        private MarketState market;

        public MarketState Market
        {
            get { return market;}
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value), @"Market property cannot be null.");

                if (ReferenceEquals(market, value))
                    return;

                market = value;
            }
        }

        public CashAccountCalculator(ICashAccountInfo infoProvider, MarketState market)
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
            this.account.OrderReplaced += UpdateOrder;
        }

        public bool HasSufficientMarginToOpenOrder(ICommonOrder order, decimal? margin)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            if (order.Type != OrderTypes.Limit && order.Type != OrderTypes.StopLimit)
                throw new ArgumentException("Invalid Order Type", "order");

            if (order.Type == OrderTypes.Stop || order.Type == OrderTypes.StopLimit)
            {
                if (order.StopPrice == null || order.StopPrice <= 0)
                    throw new ArgumentException("Invalid Stop Price", "order");
            }

            if (order.Type != OrderTypes.Stop)
            {
                if (order.Price == null || order.Price <= 0)
                    throw new ArgumentException("Invalid Price", "order");
            }

            if (order.Amount <= 0)
                throw new ArgumentException("Invalid Amount", "order");

            if (margin == null)
                throw new MarginNotCalculatedException("Provided order must have calculated Margin.");

            IAssetModel marginAsset = GetMarginAsset(order);
            if (marginAsset == null || marginAsset.Amount == 0)
                throw new NotEnoughMoneyException($"Asset {GetMarginAssetCurrency(order)} is empty.");

            if (margin.Value > marginAsset.FreeAmount)
                throw new NotEnoughMoneyException($"{marginAsset}, OrderMargin={margin.Value}.");

            return true;
        }

        public static decimal CalculateMargin(ICommonOrder order, ISymbolInfo symbol)
        {
            decimal combinedMarginFactor = 1.0M;
            if (order.Type == OrderTypes.Stop || order.Type == OrderTypes.StopLimit)
                combinedMarginFactor *= (decimal)symbol.StopOrderMarginReduction;
            else if (order.Type == OrderTypes.Limit && order.IsHidden)
                combinedMarginFactor *= (decimal)symbol.HiddenLimitOrderMarginReduction;

            decimal amount = order.RemainingAmount;
            decimal price = (order.Type == OrderTypes.Stop) ? order.StopPrice.Value : order.Price.Value;

            if (order.Side == OrderSides.Buy)
                return combinedMarginFactor * amount * price;
            else
                return combinedMarginFactor * amount;
        }

        public IAssetModel GetMarginAsset(ICommonOrder order)
        {
            if (order.MarginCurrency == null || order.ProfitCurrency == null)
                throw new MarketConfigurationException("Order must have both margin & profit currencies specified.");

            return assets.GetOrDefault(GetMarginAssetCurrency(order));
        }

        public string GetMarginAssetCurrency(ICommonOrder order)
        {
            return (order.Side == OrderSides.Buy) ? order.ProfitCurrency : order.MarginCurrency;
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
            ISymbolInfo symbol = Market.GetSymbolInfoOrThrow(order.Symbol);
            decimal margin = CalculateMargin(order, symbol);
            order.Margin = margin;
            OrderLightClone clone = new OrderLightClone(order);
            orders.Add(order.OrderId, clone);

            IAssetModel marginAsset = GetMarginAsset(order);
            if (marginAsset != null)
                marginAsset.Margin += margin;

            order.EssentialParametersChanged += UpdateOrder;
        }

        public void UpdateOrder(IOrderModel order)
        {
            ISymbolInfo symbol = Market.GetSymbolInfoOrThrow(order.Symbol);
            OrderLightClone clone = GetOrderOrThrow(order.OrderId);
            IAssetModel marginAsset = GetMarginAsset(order);
            marginAsset.Margin -= clone.Margin.GetValueOrDefault();

            decimal margin = CalculateMargin(order, symbol);
            marginAsset.Margin += margin;
            order.Margin = margin;

            OrderLightClone newClone = new OrderLightClone(order);
            orders[order.OrderId] = newClone;

            if (clone.OrderModelRef != order) // resubscribe if order model is replaced
            {
                clone.OrderModelRef.EssentialParametersChanged -= UpdateOrder;
                order.EssentialParametersChanged += UpdateOrder;
            }
        }

        public void AddOrdersBunch(IEnumerable<IOrderModel> bunch)
        {
            bunch.Foreach(AddOrder);
        }

        public void RemoveOrder(IOrderModel order)
        {
            OrderLightClone clone = GetOrderOrThrow(order.OrderId);
            orders.Remove(order.OrderId);

            IAssetModel marginAsset = GetMarginAsset(order);
            if (marginAsset != null)
                marginAsset.Margin -= clone.Margin.GetValueOrDefault();

            order.EssentialParametersChanged -= UpdateOrder;
        }

        OrderLightClone GetOrderOrThrow(long orderId)
        {
            OrderLightClone clone;
            if (!orders.TryGetValue(orderId, out clone))
                throw new InvalidOperationException("Order Not Found: " + orderId);
            return clone;
        }

        public void Dispose()
        {
            this.account.AssetsChanged -= AddRemoveAsset;
            this.account.OrderAdded -= AddOrder;
            this.account.OrderRemoved -= RemoveOrder;
            this.account.OrdersAdded -= AddOrdersBunch;
            this.account.OrderReplaced -= UpdateOrder;
        }
    }
}
