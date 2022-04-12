using System;
using System.Collections.Generic;
using System.Linq;
using TickTrader.FDK.Calculator.Netting;
using TickTrader.FDK.Calculator.Rounding;
using TickTrader.FDK.Calculator.Validation;
using TickTrader.FDK.Common;

namespace TickTrader.FDK.Calculator
{
    public class AccountCalculator : IDisposable
    {
        private MarketStateBase _market;
        private readonly IDictionary<string, SymbolNetting> _bySymbolMap = new Dictionary<string, SymbolNetting>();
        private readonly Dictionary<string, MarginAsset> assets = new Dictionary<string, MarginAsset>();
        private int _errorCount;
        private bool _autoUpdate;
        private decimal _cms;
        private decimal _swap;
        private decimal _rebate;

        public const int DefaultRounding = 2;

        public AccountCalculator(NettingCalculationTypes nettingType, IMarginAccountInfo accInfo, MarketStateBase market, bool autoUpdate = false)
        {
            Info = accInfo;
            ChangeMarket(market);
            _autoUpdate = autoUpdate;
            NettingType = nettingType;

            _market.Initialized += OnMarketConfigChanged;
            InitRounding();

            AddOrdersBunch(accInfo.Orders);
            AddPositions(accInfo.Positions);

            Info.OrderAdded += AddOrder;
            Info.OrderRemoved += RemoveOrder;
            Info.OrdersAdded += AddOrdersBunch;
            Info.PositionChanged += PositionChanged;
        }

        public IMarginAccountInfo Info { get; }
        public bool IsCalculated { get; private set; } = true;
        public CalcError WorstError { get; private set; }
        public CalcErrorCode WorstErrorCode { get; private set; }
        public int RoundingDigits { get; private set; }
        public decimal Profit { get; private set; }
        public decimal Equity => Info.Balance + Profit + Commission + Swap + Rebate;
        public decimal FreeMargin => Equity - Margin;
        public decimal Margin { get; private set; }
        public decimal MarginLevel => CalculateMarginLevel();
        public NettingCalculationTypes NettingType { get; }

        public MarketStateBase Market
        {
            get { return _market; }
            set { ChangeMarket(value); }
        }

        public event EventHandler WorstErrorCodeChanged;

        public decimal Commission
        {
            get { return _cms; }
            set
            {
                _cms = value;
            }
        }

        public decimal Rebate
        {
            get { return _rebate; }
            set
            {
                _rebate = value;
            }
        }

        public decimal Swap
        {
            get { return _swap; }
            set
            {
                _swap = value;
            }
        }

        public void Dispose()
        {
            Info.OrderAdded -= AddOrder;
            Info.OrderRemoved -= RemoveOrder;
            Info.OrdersAdded -= AddOrdersBunch;
            Info.PositionChanged -= PositionChanged;

            _market.Initialized -= OnMarketConfigChanged;

            foreach (var smbCalc in _bySymbolMap.Values)
                DisposeCalc(smbCalc);

            _bySymbolMap.Clear();
        }

        public bool HasSufficientMarginToOpenOrder(IOrderCalcInfo order, out CalcError error)
        {
            return HasSufficientMarginToOpenOrder(order, out _, out error);
        }

        public bool HasSufficientMarginToOpenOrder(IOrderCalcInfo order, out decimal newAccountMargin, out CalcError error)
        {
            var netting = GetNetting(order.Symbol);
            var calc = netting?.Calc ?? _market.GetCalculator(order.Symbol, Info.BalanceCurrency);
            using (calc.UsageScope())
            {
                var orderMargin = calc.CalculateMargin(order, Info.Leverage, out error);
                decimal marginDelta = orderMargin;
                decimal? orderMarginDelta = null;

                if (Info.AccountingType == AccountType.Net)
                {
                    if (order.Type == OrderType.Market || (order.Type == OrderType.Limit && order.ImmediateOrCancel))
                    {
                        var netPosBuyMargin = netting?.Buy?.NetPosMargin ?? 0m;
                        var netPosSellMargin = netting?.Sell?.NetPosMargin ?? 0m;
                        if (order.Side == OrderSide.Buy && netPosSellMargin > 0)
                        {
                            if (orderMargin < netPosSellMargin)
                            {
                                marginDelta = -orderMargin;
                            }
                            else
                            {
                                marginDelta = -netPosSellMargin;
                                orderMarginDelta = orderMargin - netPosSellMargin;
                            }
                        }
                        else if (order.Side == OrderSide.Sell && netPosBuyMargin > 0)
                        {
                            if (orderMargin < netPosBuyMargin)
                            {
                                marginDelta = -orderMargin;
                            }
                            else
                            {
                                marginDelta = -netPosBuyMargin;
                                orderMarginDelta = orderMargin - netPosBuyMargin;
                            }
                        }
                    }
                }

                return CanIncreaseMarginBy(marginDelta, orderMarginDelta, netting, order.Side, order.Type, out newAccountMargin);
            }
        }

        public bool HasSufficientMarginToOpenOrders(IEnumerable<IOrderCalcInfo> orders, out decimal? newAccountMargin, out CalcError error)
        {
            error = null;
            var marginDeltas = new List<MarginDeltaParameters>(orders.Count());
            foreach (var order in orders)
            {
                var netting = GetNetting(order.Symbol);
                var calc = netting?.Calc ?? _market.GetCalculator(order.Symbol, Info.BalanceCurrency);
                using (calc.UsageScope())
                {
                    var orderMargin = calc.CalculateMargin(order, Info.Leverage, out error);
                    if (error != null)
                    {
                        newAccountMargin = 0;
                        return false;
                    }
                    marginDeltas.Add(new MarginDeltaParameters
                    {
                        MarginDelta = orderMargin,
                        Symbol = order.Symbol,
                        OrderSide = order.Side,
                        OrderType = order.Type
                    });
                }
            }
            return CanIncreaseMarginBy(marginDeltas, out newAccountMargin);
        }

        public bool CanIncreaseMarginBy(decimal orderMargin, string symbol, OrderSide orderSide, OrderType orderType)
        {
            var netting = GetNetting(symbol);
            return CanIncreaseMarginBy(orderMargin, null, netting, orderSide, orderType, out _);
        }

        public bool CanIncreaseMarginBy(decimal marginDelta, decimal? orderMarginDelta, string symbol, OrderSide orderSide, OrderType orderType, out decimal newAccountMargin)
        {
            var netting = GetNetting(symbol);
            return CanIncreaseMarginBy(marginDelta, orderMarginDelta, netting, orderSide, orderType, out newAccountMargin);
        }

        private bool CanIncreaseMarginBy(decimal marginDelta, decimal? orderMarginDelta, SymbolNetting netting, OrderSide orderSide, OrderType orderType, out decimal newAccountMargin)
        {
            decimal symbolMargin;
            decimal newSymbolMargin;

            if (netting == null)
            {
                symbolMargin = 0;
                newSymbolMargin = marginDelta;
            }
            else
            {
                decimal symbolMarginBuy = netting.Buy?.Margin ?? 0;
                decimal symbolMarginSell = netting.Sell?.Margin ?? 0;
                symbolMargin = netting.Margin;
                newSymbolMargin = symbolMargin;

                if (Info.AccountingType == AccountType.Gross)
                {
                    if (orderSide == OrderSide.Buy)
                    {
                        symbolMarginBuy += marginDelta;
                    }
                    else
                    {
                        symbolMarginSell += marginDelta;
                    }
                }
                else if (Info.AccountingType == AccountType.Net)
                {
                    var ordMarginDelta = orderMarginDelta ?? 0m;
                    var posAmountBuy = netting.Buy?.NetPosAmount ?? 0m;
                    var posAmountSell = netting.Sell?.NetPosAmount ?? 0m;
                    var posMarginBuy = posAmountBuy > 0m ? (netting.Buy?.NetPosMargin ?? 0m) : 0m;
                    var posMarginSell = posAmountSell > 0m ? (netting.Sell?.NetPosMargin ?? 0m) : 0m;

                    if (symbolMargin == 0) //(Math.Abs(symbolMargin) < double.Epsilon)
                    {
                        if (orderSide == OrderSide.Buy)
                            symbolMarginBuy = marginDelta;
                        else if (orderSide == OrderSide.Sell)
                            symbolMarginSell = marginDelta;
                    }
                    else if (symbolMarginBuy == symbolMarginSell) //(Math.Abs(symbolMarginBuy - symbolMarginSell).IsApproximatelyZero())
                    {
                        if (marginDelta > 0)
                        {
                            if (orderSide == OrderSide.Buy)
                            {
                                symbolMarginBuy += marginDelta;
                            }
                            else if (orderSide == OrderSide.Sell)
                            {
                                symbolMarginSell += marginDelta;
                            }
                        }
                        else
                        {
                            if (posAmountBuy > 0)
                            {
                                if (orderSide == OrderSide.Buy)
                                {
                                    symbolMarginBuy += (ordMarginDelta - marginDelta);
                                }
                                else if (orderSide == OrderSide.Sell)
                                {
                                    symbolMarginBuy += marginDelta;
                                    symbolMarginSell += ordMarginDelta;
                                }
                            }
                            else if (posAmountSell > 0)
                            {
                                if (orderSide == OrderSide.Sell)
                                {
                                    symbolMarginSell += (ordMarginDelta - marginDelta);
                                }
                                else if (orderSide == OrderSide.Buy)
                                {
                                    symbolMarginBuy += ordMarginDelta;
                                    symbolMarginSell += marginDelta;
                                }
                            }
                            else
                            {
                                if (orderSide == OrderSide.Buy)
                                {
                                    symbolMarginBuy += (ordMarginDelta - marginDelta);
                                }
                                else if (orderSide == OrderSide.Sell)
                                {
                                    symbolMarginSell += (ordMarginDelta - marginDelta);
                                }
                            }
                        }
                    }
                    else if (symbolMarginBuy > symbolMarginSell)
                    {
                        if (orderSide == OrderSide.Buy)
                        {
                            if (marginDelta > 0)
                            {
                                symbolMarginBuy += marginDelta;
                            }
                            else if (orderType != OrderType.StopLimit && posAmountSell > 0)
                            {
                                var posMarginDelta = posMarginSell + marginDelta;
                                var sellMarginDelta = posMarginDelta > 0m? marginDelta : -posMarginSell;
                                var buyMarginDelta = posMarginDelta > 0m ? ordMarginDelta : ordMarginDelta - posMarginDelta;
                                symbolMarginSell += sellMarginDelta;
                                symbolMarginBuy += buyMarginDelta;
                            }
                            else
                            {
                                symbolMarginBuy += (ordMarginDelta - marginDelta);
                            }
                        }
                        else if (orderSide == OrderSide.Sell)
                        {
                            if (marginDelta > 0)
                            {
                                symbolMarginSell += marginDelta;
                            }
                            else if (orderType != OrderType.StopLimit && posAmountBuy > 0)
                            {
                                var posMarginDelta = posMarginBuy + marginDelta;
                                var buyMarginDelta = posMarginDelta > 0m ? marginDelta : -posMarginBuy;
                                var sellMarginDelta = posMarginDelta > 0m ? ordMarginDelta : ordMarginDelta - posMarginDelta;
                                symbolMarginBuy += buyMarginDelta;
                                symbolMarginSell += sellMarginDelta;
                            }
                            else
                            {
                                symbolMarginSell += (ordMarginDelta - marginDelta);
                            }
                        }
                    }
                    else if (symbolMarginSell > symbolMarginBuy)
                    {
                        if (orderSide == OrderSide.Sell)
                        {
                            if (marginDelta > 0)
                            {
                                symbolMarginSell += marginDelta;
                            }
                            else if (orderType != OrderType.StopLimit && posAmountBuy > 0)
                            {
                                var posMarginDelta = posMarginBuy + marginDelta;
                                var buyMarginDelta = posMarginDelta > 0m ? marginDelta : -posMarginBuy;
                                var sellMarginDelta = posMarginDelta > 0m ? ordMarginDelta : ordMarginDelta - posMarginDelta;
                                symbolMarginBuy += buyMarginDelta;
                                symbolMarginSell += sellMarginDelta;
                            }
                            else
                            {
                                symbolMarginSell += (ordMarginDelta - marginDelta);
                            }
                        }
                        else if (orderSide == OrderSide.Buy)
                        {
                            if (marginDelta > 0)
                            {
                                symbolMarginBuy += marginDelta;
                            }
                            else if (orderType != OrderType.StopLimit && posAmountSell > 0)
                            {
                                var posMarginDelta = posMarginSell + marginDelta;
                                var sellMarginDelta = posMarginDelta > 0m ? marginDelta : -posMarginSell;
                                var buyMarginDelta = posMarginDelta > 0m ? ordMarginDelta : ordMarginDelta - posMarginDelta;
                                symbolMarginSell += sellMarginDelta;
                                symbolMarginBuy += buyMarginDelta;
                            }
                            else
                            {
                                symbolMarginBuy += (ordMarginDelta - marginDelta);
                            }
                        }
                    }
                }
                else
                    throw new Exception("Not a margin account!");

                newSymbolMargin = SymbolNetting.CalculateMargin(netting, symbolMarginBuy, symbolMarginSell);
            }

            var marginIncrement = newSymbolMargin - symbolMargin;
            newAccountMargin = Margin + marginIncrement;

            return marginIncrement <= 0 || newAccountMargin < Equity; // (newAccountMargin - Equity) < double.Epsilon;
        }

        public bool CanIncreaseMarginBy(IEnumerable<MarginDeltaParameters> marginDeltas, out decimal? newAccountMargin)
        {
            newAccountMargin = null;
            if (marginDeltas != null)
            {
                if (Info.AccountingType == AccountType.Gross)
                {
                    var buySellMarginBySymbol = new Dictionary<string, BuySellMargin>();
                    var newMargin = Margin;
                    foreach (var deltaParam in marginDeltas)
                    {
                        var symbol = deltaParam.Symbol;
                        var orderSide = deltaParam.OrderSide;
                        var buyMarginDelta = orderSide == OrderSide.Buy ? deltaParam.MarginDelta : 0;
                        var sellMarginDelta = orderSide == OrderSide.Sell ? deltaParam.MarginDelta : 0;
                        var netting = GetNetting(symbol);

                        if (!buySellMarginBySymbol.ContainsKey(symbol))
                        {
                            buySellMarginBySymbol[symbol] = new BuySellMargin(netting?.Buy?.Margin ?? 0, netting?.Sell?.Margin ?? 0);
                        }

                        var buySellMargin = buySellMarginBySymbol[symbol];
                        var marginBuy = buySellMargin.BuyMargin;
                        var marginSell = buySellMargin.SellMargin;

                        decimal smbMargin;
                        decimal newSmbMargin;
                        if (netting == null)
                        {
                            var symbolInfo = Market?.Symbols?.FirstOrDefault(s => s.Symbol == symbol);
                            smbMargin = SymbolNetting.CalculateMargin(symbolInfo, marginBuy, marginSell);
                            newSmbMargin = SymbolNetting.CalculateMargin(symbolInfo, marginBuy + buyMarginDelta, marginSell + sellMarginDelta);
                        }
                        else
                        {
                            smbMargin = SymbolNetting.CalculateMargin(netting, marginBuy, marginSell);
                            newSmbMargin = SymbolNetting.CalculateMargin(netting, marginBuy + buyMarginDelta, marginSell + sellMarginDelta);
                        }

                        var marginIncrement = newSmbMargin - smbMargin;

                        newMargin += marginIncrement;

                        if (marginIncrement > 0 && newMargin >= Equity)
                        {
                            newAccountMargin = newMargin;
                            return false;
                        }

                        buySellMargin.BuyMargin += buyMarginDelta;
                        buySellMargin.SellMargin += sellMarginDelta;
                    }
                    newAccountMargin = newMargin;
                    return true;
                }
                else if (Info.AccountingType == AccountType.Net)
                {
                    throw new NotSupportedException("Not supported for Net account!");
                }
                else
                    throw new Exception("Not a margin account!");
            }
            else
                return true;
        }

        public SymbolNetting GetNetting(string symbol)
        {
            SymbolNetting calc;
            _bySymbolMap.TryGetValue(symbol, out calc);
            return calc;
        }

        public decimal GetMinCommissionConversionRate(string minCommissCurrency, out CalcError error)
        {
            var convertion = _market.ConversionMap.GetNegativeAssetConversion(minCommissCurrency, Info.BalanceCurrency);

            error = convertion.Error;
            return convertion.Value;
        }

        public IDictionary<string, Asset> GetAssets(out CalcError error)
        {
            var marginAssets = new Dictionary<string, Asset>();
            error = null;
            try
            {
                List<string> keysToRemove = new List<string>();
                foreach (var key in assets.Keys)
                {
                    if (assets[key].Volume == 0)
                    {
                        keysToRemove.Add(key);
                        continue;
                    }
                    marginAssets.Add(key, new Asset {Currency = key, Volume = (double) assets[key].Volume});
                }
                keysToRemove.ForEach(k => assets.Remove(k));

                var accountCurrency = Info.BalanceCurrency;
                if (!marginAssets.ContainsKey(accountCurrency))
                {
                    marginAssets.Add(accountCurrency, new Asset {Currency = accountCurrency});
                }
                marginAssets[accountCurrency].Volume += (double)FreeMargin * Info.Leverage;

                foreach (var assetPair in marginAssets)
                {
                    var currency = assetPair.Key;
                    var asset = assetPair.Value;
                    if (asset.Volume >= 0)
                    {
                        var conversion = _market.ConversionMap.GetPositiveAssetConversion(currency, accountCurrency);
                        asset.Rate = (double)conversion.Value;
                        CalcError.GetWorst(error, conversion.Error);
                    }
                    else
                    {
                        var conversion = _market.ConversionMap.GetNegativeAssetConversion(currency, accountCurrency);
                        asset.Rate = (double)conversion.Value;
                        CalcError.GetWorst(error, conversion.Error);
                    }
                    asset.DepositCurrency = FinancialRounding.Instance.RoundProfit(RoundingDigits, asset.Volume * asset.Rate / Info.Leverage);
                }
            }
            catch{}

            return marginAssets;
        }

        //public void EnableAutoUpdate()
        //{
        //    _market.RateChanged += a =>
        //    {
        //        var smbCalc = GetSymbolStats(a.Symbol);
        //        if (smbCalc != null)
        //            smbCalc.Recalculate();
        //    };
        //}

        private void AddOrder(IOrderModel order)
        {
            if (!order.IsContingent)
            {
                AddInternal(order);
                GetOrAddSymbolCalculator(order.Symbol).AddOrder(order);
            }
        }

        private void AddOrderWithoutCalculation(IOrderModel order)
        {
            if (!order.IsContingent)
            {
                AddInternal(order);
                GetOrAddSymbolCalculator(order.Symbol).AddOrderWithoutCalculation(order);
            }
        }

        private void AddInternal(IOrderModel order)
        {
            Swap += order.Swap;
            Commission += order.Commission;
            Rebate += order.Rebate;
            order.SwapChanged += Order_SwapChanged;
            order.CommissionChanged += Order_CommissionChanged;
            order.RebateChanged += Order_RebateChanged;
            order.EssentialsChanged += Order_EssentialsChanged;

            if (order.Type == OrderType.Position)
            {
                ChangeMarginAsset(order.RemainingAmount, order.SymbolInfo, order.Side);
                ChangeProfitAsset(order.RemainingAmount * (order.Price ?? 0), order.SymbolInfo, order.Side);
            }
        }

        private void AddOrdersBunch(IEnumerable<IOrderModel> bunch)
        {
            foreach (var order in bunch)
                AddOrderWithoutCalculation(order);

            foreach (var smb in _bySymbolMap.Values)
                smb.Recalculate();
        }

        private void RemoveOrder(IOrderModel order)
        {
            if (!order.IsContingent)
            {
                Swap -= order.Swap;
                Commission -= order.Commission;
                Rebate -= order.Rebate;
                order.SwapChanged -= Order_SwapChanged;
                order.CommissionChanged -= Order_CommissionChanged;
                order.RebateChanged -= Order_RebateChanged;
                order.EssentialsChanged -= Order_EssentialsChanged;
                var smbCalc = GetOrAddSymbolCalculator(order.Symbol);
                smbCalc.RemoveOrder(order);
                RemoveIfEmpty(smbCalc);

                if (order.Type == OrderType.Position)
                {
                    ChangeMarginAsset(-order.RemainingAmount, order.SymbolInfo, order.Side);
                    ChangeProfitAsset(-order.RemainingAmount * (order.Price ?? 0), order.SymbolInfo, order.Side);
                }
            }
        }

        private void AddPositions(IEnumerable<IPositionModel> positions)
        {
            if (positions != null)
            {
                foreach (var pos in positions)
                    UpdateNetPos(pos);
            }
        }

        private void PositionChanged(PositionEssentialsChangeArgs args)
        {
            UpdateNetPos(args.Position, args.OldLongAmount, args.OldLongPrice, args.OldShortAmount, args.OldShortPrice);
        }

        private void UpdateNetPos(IPositionModel position, decimal? oldLongAmount = null, decimal? oldLongPrice = null, decimal? oldShortAmount = null, decimal? oldShortPrice = null)
        {
            decimal dSwap, dComm;
            var smbCalc = GetOrAddSymbolCalculator(position.Symbol);
            smbCalc.UpdatePosition(position, out dSwap, out dComm);
            Swap += dSwap;
            Commission += dComm;

            var marginAssetDelta = position.Long.Amount;
            var profitAssetDelta = position.Long.Amount * position.Long.Price;
            if (oldLongAmount != null)
                marginAssetDelta -= oldLongAmount.Value;
            if (oldLongAmount != null && oldLongPrice != null)
                profitAssetDelta -= oldLongAmount.Value * oldLongPrice.Value;
            ChangeMarginAsset(marginAssetDelta, position.SymbolInfo, OrderSide.Buy);
            ChangeProfitAsset(profitAssetDelta, position.SymbolInfo, OrderSide.Buy);

            marginAssetDelta = position.Short.Amount;
            profitAssetDelta = position.Short.Amount * position.Short.Price;
            if (oldShortAmount != null)
                marginAssetDelta -= oldShortAmount.Value;
            if (oldShortAmount != null && oldShortPrice != null)
                profitAssetDelta -= oldShortAmount.Value * oldShortPrice.Value;
            ChangeMarginAsset(marginAssetDelta, position.SymbolInfo, OrderSide.Sell);
            ChangeProfitAsset(profitAssetDelta, position.SymbolInfo, OrderSide.Sell);
        }

        private void ChangeMarginAsset(decimal delta, ISymbolInfo symbol, OrderSide side)
        {
            if (symbol == null)
                return;
            var asset = assets.GetOrAdd(symbol.MarginCurrency);
            if (side == OrderSide.Buy)
                asset.Volume += delta;
            else
                asset.Volume -= delta;
        }

        private void ChangeProfitAsset(decimal delta, ISymbolInfo symbol, OrderSide side)
        {
            if (symbol == null)
                return;
            var asset = assets.GetOrAdd(symbol.ProfitCurrency);
            if (side == OrderSide.Buy)
                asset.Volume -= delta;
            else
                asset.Volume += delta;
        }

        private void RemoveIfEmpty(SymbolNetting calc)
        {
            if (calc.IsEmpty)
            {
                _bySymbolMap.Remove(calc.Symbol);
                DisposeCalc(calc);
            }
        }

        private void DisposeCalc(SymbolNetting calc)
        {
            //calc.StatsChanged -= Calc_StatsChanged;
            calc.Dispose();
        }

        private SymbolNetting GetOrAddSymbolCalculator(string symbol)
        {
            SymbolNetting calc;
            if (!_bySymbolMap.TryGetValue(symbol, out calc))
            {
                calc = new SymbolNetting(symbol, this, _market, _autoUpdate);
                //calc.StatsChanged += Calc_StatsChanged;
                _bySymbolMap.Add(symbol, calc);
            }
            return calc;
        }

        internal void Calc_StatsChanged(StatsChange args)
        {
            Profit += args.ProfitDelta;
            Margin += args.MarginDelta;
            if (args.ErrorDelta != 0 || args.IsErrorChanged)

            {
                _errorCount += args.ErrorDelta;
                var oldErrorCode = WorstErrorCode;
                if (_errorCount <= 0)
                {
                    IsCalculated = true;
                    WorstError = null;
                    WorstErrorCode = CalcErrorCode.None;
                }
                else
                {
                    IsCalculated = false;
                    WorstError = GetWorstError();
                    WorstErrorCode = WorstError?.Code ?? CalcErrorCode.None;
                }

                if (oldErrorCode != WorstErrorCode)
                    WorstErrorCodeChanged?.Invoke(this, new EventArgs());
            }

            OnUpdated();
        }

        protected virtual void OnUpdated() { }

        private void Order_SwapChanged(OrderPropArgs<decimal> args)
        {
            Swap += args.NewVal - args.OldVal;
        }

        private void Order_CommissionChanged(OrderPropArgs<decimal> args)
        {
            Commission += args.NewVal - args.OldVal;
        }

        private void Order_RebateChanged(OrderPropArgs<decimal> args)
        {
            Rebate += args.NewVal - args.OldVal;
        }

        public void Order_EssentialsChanged(OrderEssentialsChangeArgs args)
        {
            var order = args.Order;
            if (order.Type == OrderType.Position)
            {
                ChangeMarginAsset(order.RemainingAmount - args.OldRemAmount, order.SymbolInfo, order.Side);
                ChangeProfitAsset(order.RemainingAmount * (order.Price ?? 0) - args.OldRemAmount * (args.OldPrice ?? 0),
                    order.SymbolInfo, order.Side);
            }
        }

        private decimal CalculateMarginLevel()
        {
            if (Margin <= 0)
                return 0;
            else
                return Equity / Margin; // !!! Don't multiply by 100
        }

        private void InitRounding()
        {
            ICurrencyInfo curr = _market.GetCurrencyOrThrow(Info.BalanceCurrency);
            if (curr != null && curr.Precision >= 0)
                RoundingDigits = curr.Precision;
            else
                RoundingDigits = 2;
        }

        private void OnMarketConfigChanged()
        {
            InitRounding();
            //ResetCalculators();
            RecalculateAll();
        }

        private CalcError GetWorstError()
        {
            CalcError worstError = null;

            foreach (var netting in _bySymbolMap.Values)
            {
                var nettingError = netting.GetWorstError();
                worstError = CalcError.GetWorst(worstError, nettingError);
            }

            return worstError;
        }

        private void ChangeMarket(MarketStateBase newMarket)
        {
            if (!newMarket.IsInitialized)
                throw new InvalidOperationException("MarketState is not initialized!");

            if (_market != null)
                _market.Initialized -= OnMarketConfigChanged;

            _market = newMarket;
            InitRounding();
            ResetCalculators();
            RecalculateAll();

            _market.Initialized += OnMarketConfigChanged;
        }

        private void ResetCalculators()
        {
            foreach (var netting in _bySymbolMap.Values)
                netting.CreateCalculator();
        }
        private void RecalculateAll()
        {
            foreach (var netting in _bySymbolMap.Values)
                netting.Recalculate();
        }

        private Exception CreateNoSymbolException(string smbName)
        {
            return new MarketConfigurationException("Symbol not found: " + smbName);
        }
    }
}
