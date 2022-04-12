using System;
using System.Collections.Generic;
using System.Text;
using TickTrader.FDK.Calculator.Validation;
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
            return HasSufficientAssetsToOpenOrders(new[]
            {
                new AssetsMovementParameters
                {
                    Order = order,
                    MarginMovement = marginMovement,
                    CommissionMovement = commission,
                    MarginAsset = marginAsset,
                    CommissionAsset = commissAsset
                }
            });
        }

        public bool HasSufficientAssetsToOpenOrders(IEnumerable<AssetsMovementParameters> parameters)
        {
            Dictionary<IAssetModel, decimal> overdrafts = new Dictionary<IAssetModel, decimal>();
            Dictionary<IAssetModel, decimal> marginMovements = new Dictionary<IAssetModel, decimal>();
            foreach (var param in parameters)
            {
                var order = param.Order;
                var marginMovement = param.MarginMovement;
                var commission = param.CommissionMovement;
                var marginAsset = param.MarginAsset;
                var commissAsset = param.CommissionAsset;

                if (!marginMovements.ContainsKey(marginAsset))
                {
                    marginMovements[marginAsset] = 0;
                }

                if (commissAsset != null)
                {
                    if (!marginMovements.ContainsKey(commissAsset))
                    {
                        marginMovements[commissAsset] = 0;
                    }
                }
                 
                bool notEnoughMargin = false;
                bool notEnoughCommiss = false;
                bool checkOverdaft = account.MaxOverdraftAmount > 0 && (order.Type == OrderType.Market || (order.Type == OrderType.Limit && order.ImmediateOrCancel));

                if (commission == 0 || commissAsset == null)
                {
                    marginMovements[marginAsset] += marginMovement;
                    notEnoughMargin = marginMovements[marginAsset] > marginAsset.FreeAmount;
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

                        marginMovements[marginAsset] += marginMovement;
                        notEnoughMargin = marginMovements[marginAsset] > marginAsset.FreeAmount;
                    }
                    else
                    {
                        marginMovements[marginAsset] += marginMovement;
                        marginMovements[commissAsset] += -commission;
                        notEnoughMargin = marginMovements[marginAsset] > marginAsset.FreeAmount;
                        notEnoughCommiss = marginMovements[commissAsset] > commissAsset.FreeAmount;
                    }
                }

                if (!checkOverdaft)
                {
                    if (notEnoughMargin)
                        throw new NotEnoughMoneyException($"{marginAsset.Currency} Movement={marginMovements[marginAsset]} FreeAmount={marginAsset.FreeAmount}.");

                    if (notEnoughCommiss)
                        throw new NotEnoughMoneyException($"{commissAsset.Currency} Movement={marginMovements[commissAsset]} FreeAmount={commissAsset.FreeAmount}.");
                }

                if (notEnoughMargin)
                {
                    decimal newMarginOverdraft = marginAsset.FreeAmount > 0
                        ? (marginMovements[marginAsset] - marginAsset.FreeAmount)
                        : marginMovements[marginAsset];
                    overdrafts[marginAsset] = newMarginOverdraft;
                }

                if (notEnoughCommiss)
                {
                    decimal newCommissOverdraft = commissAsset.FreeAmount > 0
                        ? (marginMovements[commissAsset] - commissAsset.FreeAmount)
                        : marginMovements[commissAsset];
                    overdrafts[commissAsset] = newCommissOverdraft;
                }
            }

            if (overdrafts.Count > 0 && OverdraftsExceedLimit(overdrafts, out var usedOverdraft, out var newOverdraft))
            {
                throw new NotEnoughMoneyException($"Overdraft {newOverdraft} exceeds limit {account.MaxOverdraftAmount} {account.OverdraftCurrency}.");
            }

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

        public bool HasSufficientMarginToOpenOrders(IEnumerable<OrderMarginMovementParameters> parameters)
        {
            Dictionary<IAssetModel, decimal> marginMovements = new Dictionary<IAssetModel, decimal>();
            foreach (var param in parameters)
            {
                var marginMovement = param.MarginMovement;
                var side = param.Side;
                var symbol = param.Symbol;
                if (marginMovement == null)
                    throw new MarginNotCalculatedException("Provided order must have calculated Margin.");

                var marginAsset = GetMarginAsset(symbol, side);
                if (marginAsset == null || marginAsset.Amount == 0)
                    throw new NotEnoughMoneyException($"Asset {GetMarginAssetCurrency(symbol, side)} is empty.");

                if (!marginMovements.ContainsKey(marginAsset))
                    marginMovements[marginAsset] = 0;

                marginMovements[marginAsset] += marginMovement.Value;
            }            
            foreach (var marginMovement in marginMovements)
            {
                if (marginMovement.Value > marginMovement.Key.FreeAmount)
                    throw new NotEnoughMoneyException($"FreeAmount={marginMovement.Key.FreeAmount}, Movement={marginMovement.Value}.");
            }
            return true;
        }

        public static decimal CalculateMarginFactor(IOrderCalcInfo order, ISymbolInfo symbol, bool onActivate = false)
        {
            return CalculateMarginFactor(order.Type, symbol, order.IsHidden, onActivate);
        }

        public static decimal CalculateMarginFactor(OrderType type, ISymbolInfo symbol, bool isHidden, bool onActivate = false)
        {
            decimal combinedMarginFactor = 1.0M;
            if ((type == OrderType.Stop || type == OrderType.StopLimit) && !onActivate)
                combinedMarginFactor *= (decimal)symbol.StopOrderMarginReduction;
            else if (type == OrderType.Limit && isHidden)
                combinedMarginFactor *= (decimal)symbol.HiddenLimitOrderMarginReduction;
            return combinedMarginFactor;
        }

        public static decimal CalculateMargin(IOrderCalcInfo order, ISymbolInfo symbol, bool onActivate = false)
        {
            // Commented due to the decision to calculate margin for Market order by the best price (for client-side validation)
            if (!order.Slippage.HasValue || (order.InitialType != OrderType.Stop /*&& order.InitialType != OrderType.Market && !(order.InitialType == OrderType.Limit && order.ImmediateOrCancel)*/) )
                return CalculateMargin(order.Type, order.RemainingAmount, order.Price, order.StopPrice, order.Side, symbol, order.IsHidden, order.IsContingent, onActivate);

            var price = onActivate ? order.Price : AdjustPriceForSlippage(order.Price, order.Slippage);
            var stopPrice = onActivate ? order.StopPrice : AdjustPriceForSlippage(order.StopPrice, order.Slippage);

            return CalculateMargin(order.Type, order.RemainingAmount, price, stopPrice, order.Side, symbol, order.IsHidden, order.IsContingent, onActivate);
        }

        public static decimal? AdjustPriceForSlippage(decimal? price, decimal? slippage)
        {
            if (price == null)
                return null;
            if (slippage == null)
                return price;

            return price * (1m + slippage);
        }

        public static decimal? AdjustPriceForSlippage(decimal? price, decimal? slippage, OrderSide side, int symbolPrecision)
        {
            if (price == null)
                return null;
            if (slippage == null)
                return price;

            var adjustedPrice = price * (1m + slippage);
            adjustedPrice = side == OrderSide.Buy
                ? Rounding.FinancialRounding.Instance.RoundProfit(symbolPrecision, adjustedPrice.Value)
                : Rounding.FinancialRounding.Instance.RoundMargin(symbolPrecision, adjustedPrice.Value);

            return adjustedPrice;
        }

        public static decimal CalculateMargin(OrderType type, decimal amount, decimal? orderPrice, decimal? orderStopPrice, OrderSide side, ISymbolInfo symbol, bool isHidden, bool isContingent, bool onActivate = false)
        {
            if (isContingent)
                return 0;

            decimal combinedMarginFactor = CalculateMarginFactor(type, symbol, isHidden, onActivate);

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
            if (order.IsContingent)
                return;
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
            if (order.IsContingent)
                return;
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
            if (order.IsContingent)
                return;
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
