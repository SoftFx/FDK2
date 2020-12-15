using System;
using System.Collections.Generic;
using System.Text;
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

        public bool HasSufficientAssetsToOpenOrder(IOrderCalcInfo order, decimal marginMovement, decimal commission, IAssetModel marginAsset, IAssetModel commissAsset)
        {
            bool notEnoughMargin = false;
            bool notEnoughCommiss = false;
            bool checkOverdaft = account.MaxOverdraftAmount > 0 && (order.Type == OrderType.Market || (order.Type == OrderType.Limit && order.ImmediateOrCancel));

            if (commission == 0)
            {
                notEnoughMargin = marginMovement > marginAsset.FreeAmount;
            }
            else
            {
                if (marginAsset == commissAsset)
                {
                    //commissAsset = marginAsset;
                    if (commission < 0)
                        marginMovement += Math.Abs(commission);
                    else
                        marginMovement -= Math.Abs(commission);

                    notEnoughMargin = marginMovement > marginAsset.FreeAmount;
                }
                else
                {
                    notEnoughMargin = marginMovement > marginAsset.FreeAmount;
                    notEnoughCommiss = commission < 0 && Math.Abs(commission) > commissAsset.FreeAmount;
                }
            }

            if (!notEnoughMargin && !notEnoughCommiss)
                return true;

            if (!checkOverdaft)
            {
                if (notEnoughMargin)
                    throw new NotEnoughMoneyException($"{marginAsset.Currency} Movement={marginMovement} FreeAmount={marginAsset.FreeAmount}.");

                throw new NotEnoughMoneyException($"{commissAsset.Currency} Commission Movement={Math.Abs(commission)} FreeAmount={commissAsset.FreeAmount}.");
            }

            Dictionary<IAssetModel, decimal> overdrafts = new Dictionary<IAssetModel, decimal>();
            StringBuilder info = new StringBuilder();
            if (notEnoughMargin)
            {
                decimal newMarginOverdraft = marginAsset.FreeAmount > 0
                    ? (marginMovement - marginAsset.FreeAmount)
                    : marginMovement;
                overdrafts.Add(marginAsset, newMarginOverdraft);
                info.Append($"{marginAsset.Currency} Movement={marginMovement} FreeAmount={marginAsset.FreeAmount}.");
            }

            if (notEnoughCommiss)
            {
                decimal newCommissOverdraft = commissAsset.FreeAmount > 0
                    ? (Math.Abs(commission) - commissAsset.FreeAmount)
                    : Math.Abs(commission);
                overdrafts.Add(commissAsset, newCommissOverdraft);
                if (info.Length > 0)
                    info.Append(" ");
                info.Append($"{commissAsset.Currency} Commission Movement={Math.Abs(commission)} FreeAmount={commissAsset.FreeAmount}.");
            }

            if (OverdraftsExceedLimit(overdrafts, out var usedOverdraft, out var newOverdraft))
                throw new NotEnoughMoneyException($"{info} Overdraft {newOverdraft} exceeds limit {account.MaxOverdraftAmount} {account.OverdraftCurrency}.");

            return true;
        }

        public bool HasSufficientMarginToOpenOrder(OrderType type, OrderSide side, ISymbolInfo symbol, decimal? marginMovement, out IAssetModel marginAsset)
        {
            if (marginMovement == null)
                throw new MarginNotCalculatedException("Provided order must have calculated Margin.");

            marginAsset = GetMarginAsset(symbol, side);
            if (marginAsset == null || marginAsset.Amount == 0)
                throw new NotEnoughMoneyException($"Asset {GetMarginAssetCurrency(symbol, side)} is empty.");

            if (marginMovement.Value > marginAsset.FreeAmount)
                throw new NotEnoughMoneyException($"FreeAmount={marginAsset.FreeAmount}, Movement={marginMovement.Value}.");

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
            // Commented due to the decision to calculate margin by the best price (for client-side validation)

            /*if (!order.Slippage.HasValue || (order.InitialType != OrderType.Market && !(order.InitialType == OrderType.Limit && order.ImmediateOrCancel)))
                return CalculateMargin(order.Type, order.RemainingAmount, order.Price, order.StopPrice, order.Side, symbol, order.IsHidden);

            var price = order.Price * (1m + order.Slippage);
            price = order.Side == OrderSide.Buy
                ? FinancialRounding.Instance.RoundProfit(symbol.Precision, price.Value)
                : FinancialRounding.Instance.RoundMargin(symbol.Precision, price.Value);*/
            var price = order.Price;

            return CalculateMargin(order.Type, order.RemainingAmount, price, order.StopPrice, order.Side, symbol, order.IsHidden);
        }


        public static decimal CalculateMargin(OrderType type, decimal amount, decimal? orderPrice, decimal? orderStopPrice, OrderSide side, ISymbolInfo symbol, bool isHidden)
        {
            decimal combinedMarginFactor = CalculateMarginFactor(type, symbol, isHidden);

            // UL: TP-3976. For StopLimit orders margin must be calculated using Price instead of StopPrice
            decimal price = type == OrderType.Stop ? orderStopPrice.Value : orderPrice.Value;

            var margin = side == OrderSide.Buy
                ? combinedMarginFactor * amount * price
                : combinedMarginFactor * amount;
            return margin;
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
                error = new SymbolNotFoundMisconfigError(symbol);
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
            symbol = order.SymbolInfo ?? throw CreateNoSymbolException(order.Symbol);
            return assets.GetOrDefault(GetMarginAssetCurrency(symbol, order.Side));
        }

        public IAssetModel GetMarginAsset(ISymbolInfo symbol, OrderSide side)
        {
            return assets.GetOrDefault(GetMarginAssetCurrency(symbol, side));
        }

        public string GetMarginAssetCurrency(ISymbolInfo smb, OrderSide side)
        {
            return (side == OrderSide.Buy) ? smb.ProfitCurrency : smb.MarginCurrency;
        }

        public decimal CalculateOverdraft()
        {
            if (account.MaxOverdraftAmount <= 0 || account.OverdraftCurrency == null)
                return 0;

            decimal overdraftTotal = 0;
            foreach (IAssetModel asset in assets.Values)
            {
                if (asset.FreeAmount < 0)
                {
                    decimal assetOverdraft = Math.Abs(asset.FreeAmount);
                    decimal overdraftInCurrency = assetOverdraft * market.ConversionMap.GetNegativeAssetConversion(asset.Currency, account.OverdraftCurrency).Value;
                    overdraftTotal += overdraftInCurrency;
                }
            }

            return overdraftTotal;
        }

        public decimal? TryCalculateOverdraft(out string error)
        {
            try
            {
                error = null;
                return CalculateOverdraft();
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return null;
            }
        }

        private bool OverdraftsExceedLimit(Dictionary<IAssetModel, decimal> overdrafts, out decimal usedOverdraft, out decimal newOverdraft)
        {
            newOverdraft = CalculateOverdraft();
            usedOverdraft = newOverdraft;

            foreach (var overdraft in overdrafts)
            {
                if (overdraft.Value <= 0)
                    continue;
                newOverdraft += overdraft.Value * market.ConversionMap.GetNegativeAssetConversion(overdraft.Key.Currency, account.OverdraftCurrency).Value;
            }

            return newOverdraft > account.MaxOverdraftAmount;
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
