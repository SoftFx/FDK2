using System;
using TickTrader.FDK.Calculator.Conversion;
using TickTrader.FDK.Common;

namespace TickTrader.FDK.Calculator
{
    public sealed class OrderCalculator
	{
        private Converter<int, int> _leverageProvider;
        private readonly string _symbolName;
        private readonly ConversionManager _conversion;
        private readonly string _accountCurrency;

        internal OrderCalculator(string symbolName, SymbolMarketNode tracker, ConversionManager conversion, string accountCurrency)
        {
            _symbolName = symbolName ?? throw new ArgumentNullException("symbolName");
            RateTracker = tracker ?? throw new ArgumentNullException("tracker");
            _conversion = conversion ?? throw new ArgumentNullException("conversion");
            _accountCurrency = accountCurrency ?? throw new ArgumentNullException("conversion");
            Init();
        }

        internal void Init()
        {
            PositiveProfitConversionRate = _conversion.GetPositiveProfitFormula(RateTracker, _accountCurrency);
            NegativeProfitConversionRate = _conversion.GetNegativeProfitFormula(RateTracker, _accountCurrency);
            MarginConversionRate = _conversion.GetMarginFormula(RateTracker, _accountCurrency);
            SymbolInfo = RateTracker.SymbolInfo;

            if (SymbolInfo != null && SymbolInfo.MarginMode != MarginCalcMode.Forex && SymbolInfo.MarginMode != MarginCalcMode.CfdLeverage)
                _leverageProvider = _ => 1;
            else
                _leverageProvider = n => n;

            if (SymbolInfo != null)
                InitMarginFactorCache();

            if (SymbolInfo == null)
                InitError = new SymbolNotFoundMisconfigError(_symbolName);
            else if (SymbolInfo.ProfitCurrency == null || SymbolInfo.MarginCurrency == null)
                InitError = new MisconfigurationError($"Currency not found for symbol {_symbolName}.");
            else
                InitError = null;
        }

        public ISymbolRate CurrentRate => RateTracker.Rate;
        public ISymbolInfo SymbolInfo { get; private set; }
        public CalcError InitError { get; private set; }
        public IConversionFormula PositiveProfitConversionRate { get; private set; }
        public IConversionFormula NegativeProfitConversionRate { get; private set; }
        public IConversionFormula MarginConversionRate { get; private set; }

        internal SymbolMarketNode RateTracker { get; }

        //public void Dispose()
        //{
        //}

        #region Margin

        private decimal _baseMarginFactor;
        private decimal _stopMarginFactor;
        private decimal _hiddenMarginFactor;
        public decimal HedgedMarginFactor { get; private set; }

        public decimal CalculateMargin(IPositionModel position, int leverage, out CalcError error)
        {
            error = null;
            var result = 0.0m;

            if (position.Short.Amount > 0)
                result += CalculateMargin(position.Short.Amount, leverage, OrderType.Position, OrderSide.Sell, false, false, out error);

            if (error == null && position.Long.Amount > 0)
                result += CalculateMargin(position.Long.Amount, leverage, OrderType.Position, OrderSide.Buy, false, false, out error);

            return result;
        }

        public decimal CalculateMargin(IOrderCalcInfo order, int leverage, out CalcError error)
        {
            return CalculateMargin(order.RemainingAmount, leverage, order.Type, order.Side, order.IsHidden, order.IsContingent, out error);
        }

        public decimal CalculateMargin(decimal orderVolume, int leverage, OrderType ordType, OrderSide side, bool isHidden, bool isContingent, out CalcError error)
        {
            error = InitError;

            if (isContingent)
                return 0;

            if (error != null)
                return 0;

            error = MarginConversionRate.Error;

            if (error != null)
                return 0;

            double lFactor = _leverageProvider(leverage);
            decimal marginFactor = GetMarginFactor(ordType, isHidden);
            decimal marginRaw = orderVolume * marginFactor / (decimal)lFactor;

            return marginRaw * MarginConversionRate.Value;
        }

        private decimal GetMarginFactor(OrderType ordType, bool isHidden)
        {
            if (ordType == OrderType.Stop || ordType == OrderType.StopLimit)
                return _stopMarginFactor;
            if (ordType == OrderType.Limit && isHidden)
                return _hiddenMarginFactor;
            return _baseMarginFactor;
        }

        private void InitMarginFactorCache()
        {
            _baseMarginFactor = (decimal)SymbolInfo.MarginFactorFractional;
            _stopMarginFactor = _baseMarginFactor * (decimal)SymbolInfo.StopOrderMarginReduction;
            _hiddenMarginFactor = _baseMarginFactor * (decimal)SymbolInfo.HiddenLimitOrderMarginReduction;
            HedgedMarginFactor = 2 * (decimal)SymbolInfo.MarginHedged - 1;
        }

        #endregion

        #region Profit

        public decimal CalculateProfit(IPositionModel position, out CalcError error)
        {
            error = null;
            var result = 0.0m;

            if (position.Short.Amount > 0)
                result += CalculateProfit(position.Short.Price, position.Short.Amount, OrderSide.Sell, out error);

            if (error == null && position.Long.Amount > 0)
                result += CalculateProfit(position.Long.Price, position.Long.Amount, OrderSide.Buy, out error);

            return result;
        }

        public decimal CalculateProfit(IOrderCalcInfo order, decimal amount, out decimal closePrice, out CalcError error)
        {
            return CalculateProfit(order.Price.Value, amount, order.Side, out closePrice, out error);
        }

        public decimal CalculateProfit(IOrderCalcInfo order, decimal amount, out decimal closePrice, out decimal conversionRate, out CalcError error)
        {
            return CalculateProfit(order.Price.Value, amount, order.Side, out closePrice, out conversionRate, out error);
        }

        public decimal CalculateProfit(IOrderCalcInfo order, out CalcError error)
        {
            return CalculateProfit(order.Price.Value, order.RemainingAmount, order.Side, out error);
        }

        public decimal CalculateProfit(IOrderCalcInfo order, out decimal closePrice, out CalcError error)
        {
            return CalculateProfit(order.Price.Value, order.RemainingAmount, order.Side, out closePrice, out error);
        }

        public decimal CalculateProfit(decimal openPrice, decimal volume, OrderSide side, out CalcError error)
        {
            return CalculateProfit(openPrice, volume, side, out _, out _, out error);
        }

        public decimal CalculateProfit(decimal openPrice, decimal volume, OrderSide side, out decimal closePrice, out CalcError error)
        {
            return CalculateProfit(openPrice, volume, side, out closePrice, out _, out error);
        }

        public decimal CalculateProfit(decimal openPrice, decimal volume, OrderSide side, out decimal closePrice, out decimal conversionRate, out CalcError error)
        {
            error = InitError;

            if (error != null)
            {
                conversionRate = 0;
                closePrice = 0;
                return 0;
            }

            conversionRate = 0;

            if (side == OrderSide.Buy)
            {
                if (!GetBid(out closePrice, out error))
                    return 0;
            }
            else
            {
                if (!GetAsk(out closePrice, out error))
                    return 0;
            }

            return CalculateProfitInternal(openPrice, closePrice, volume, side, out conversionRate, out error);
        }

        public decimal CalculateProfitFixedPrice(IOrderCalcInfo order, decimal amount, decimal closePrice, out CalcError error)
        {
            return CalculateProfitInternal(order.Price.Value, closePrice, amount, order.Side, out _, out error);
        }

        public decimal CalculateProfitFixedPrice(IOrderCalcInfo order, decimal amount, decimal closePrice, out decimal conversionRate, out CalcError error)
        {
            return CalculateProfitInternal(order.Price.Value, closePrice, amount, order.Side, out conversionRate, out error);
        }

        public decimal CalculateProfitFixedPrice(decimal openPrice, decimal volume, decimal closePrice, OrderSide side, out CalcError error)
        {
            return CalculateProfitFixedPrice(openPrice, closePrice, volume, side, out _, out error);
        }

        public decimal CalculateProfitFixedPrice(decimal openPrice, decimal volume, decimal closePrice, OrderSide side, out decimal conversionRate, out CalcError error)
        {
            return CalculateProfitInternal(openPrice, closePrice, volume, side, out conversionRate, out error);
        }

        private decimal CalculateProfitInternal(decimal openPrice, decimal closePrice, decimal volume, OrderSide side, out decimal conversionRate, out CalcError error)
        {
            error = InitError;

            if (error != null)
            {
                closePrice = 0;
                conversionRate = 0;
                return 0;
            }

            decimal nonConvProfit;

            if (side == OrderSide.Buy)
                nonConvProfit = (closePrice - openPrice) * volume;
            else
                nonConvProfit = (openPrice - closePrice) * volume;

            return ConvertProfitToAccountCurrency(nonConvProfit, out conversionRate, out error);
        }

        public decimal ConvertMarginToAccountCurrency(decimal margin, out CalcError error)
        {
            error = InitError;

            if (error != null)
                return 0;

            error = MarginConversionRate.Error;
            if (error == null)
                return margin * MarginConversionRate.Value;
            return 0;
        }

        public decimal ConvertProfitToAccountCurrency(decimal profit, out CalcError error)
        {
            return ConvertProfitToAccountCurrency(profit, out _, out error);
        }

        public decimal ConvertProfitToAccountCurrency(decimal profit, out decimal conversionRate, out CalcError error)
        {
            error = InitError;

            if (error != null)
            {
                conversionRate = 0;
                return 0;
            }

            conversionRate = GetProfitConvertionrate(profit >= 0, out error);

            if (error == null)
                return profit * conversionRate;
            return 0;
        }

        private decimal GetProfitConvertionrate(bool profitIsPositive, out CalcError error)
        {
            if (profitIsPositive)
            {
                error = PositiveProfitConversionRate.Error;
                if (error == null)
                    return PositiveProfitConversionRate.Value;
                return 0;
            }
            else
            {
                error = NegativeProfitConversionRate.Error;
                if (error == null)
                    return NegativeProfitConversionRate.Value;
                return 0;
            }
        }

        #endregion

        #region Commission

        public decimal CalculateCommission(decimal amount, decimal cValue, CommissionType vType, CommissionChargeType chType, out CalcError error)
        {
            error = InitError;

            if (error != null)
                return 0;

            if (cValue == 0)
                return 0;

            //UL: all calculation for CommissionChargeType.PerLot
            if (vType == CommissionType.Absolute)
            {
                //if (chType == CommissionChargeType.PerDeal)
                //    return -cValue;
                //else if (chType == CommissionChargeType.PerLot)
                return -(amount / (decimal)SymbolInfo.ContractSizeFractional * (decimal)cValue);
            }
            else if (vType == CommissionType.Percent)
            {
                //if (chType == CommissionChargeType.PerDeal || chType == CommissionChargeType.PerLot)
                error = MarginConversionRate.Error;
                if (error != null)
                    return 0;
                return -(amount * cValue * (decimal)MarginConversionRate.Value) / 100;
            }
            else if (vType == CommissionType.PerUnit)
            {
                decimal ptValue = cValue / (decimal)Math.Pow(10, SymbolInfo.Precision);

                //if (chType == CommissionChargeType.PerDeal)
                //    return - (ptValue * MarginConversionRate.Value);
                //else if (chType == CommissionChargeType.PerLot)
                //error = MarginConversionRate.Error;
                //if (error != null)
                //    return 0;
                return ConvertProfitToAccountCurrency(-(amount * ptValue), out _, out error);
            }

            throw new Exception("Invalid comission configuration: chType=" + chType + " vType= " + vType);
        }

        #endregion Commission

        #region Swap

        public decimal CalculateSwap(decimal amount, OrderSide side, DateTime now, out CalcError error, out SwapType? type, out double? swapSize)
        {
            error = InitError;
            type = null;
            swapSize = null;

            if (error != null)
                return 0;

            decimal swapAmount = (decimal)GetSwapModifier(side, ref type, ref swapSize) * amount;
            decimal swap = 0;

            if (SymbolInfo.SwapType == SwapType.Points)
                swap = ConvertProfitToAccountCurrency(swapAmount, out error);
            else if (SymbolInfo.SwapType == SwapType.PercentPerYear)
                swap = ConvertMarginToAccountCurrency(swapAmount, out error);

            if (SymbolInfo.TripleSwapDay > 0)
            {
                //var now = DateTime.UtcNow;
                DayOfWeek swapDayOfWeek = now.DayOfWeek == DayOfWeek.Sunday ? DayOfWeek.Saturday : (int)now.DayOfWeek - DayOfWeek.Monday;
                if (SymbolInfo.TripleSwapDay == (int)swapDayOfWeek)
                    swap *= 3;
                else if (swapDayOfWeek == DayOfWeek.Saturday || swapDayOfWeek == DayOfWeek.Sunday)
                    swap = 0;
            }

            return swap;
        }

        private double GetSwapModifier(OrderSide side, ref SwapType? type, ref double? swapSize)
        {
            if (SymbolInfo.SwapEnabled)
            {
                type = SymbolInfo.SwapType;
                if (side == OrderSide.Buy)
                    swapSize = SymbolInfo.SwapSizeLong;
                if (side == OrderSide.Sell)
                    swapSize = SymbolInfo.SwapSizeShort;
                if (SymbolInfo.SwapType == SwapType.Points)
                {
                    if (side == OrderSide.Buy)
                        return SymbolInfo.SwapSizeLong / Math.Pow(10, SymbolInfo.Precision);
                    if (side == OrderSide.Sell)
                        return SymbolInfo.SwapSizeShort / Math.Pow(10, SymbolInfo.Precision);
                }
                else if (SymbolInfo.SwapType == SwapType.PercentPerYear)
                {
                    const double power = 1.0 / 365.0;
                    double factor = 0.0;
                    if (side == OrderSide.Buy)
                        factor = Math.Sign(SymbolInfo.SwapSizeLong) * (Math.Pow(1 + Math.Abs(SymbolInfo.SwapSizeLong), power) - 1);
                    if (side == OrderSide.Sell)
                        factor = Math.Sign(SymbolInfo.SwapSizeShort) * (Math.Pow(1 + Math.Abs(SymbolInfo.SwapSizeShort), power) - 1);

                    //if (double.IsInfinity(factor) || double.IsNaN(factor))
                    //    throw new MarketConfigurationException($"Can not calculate swap: side={side} symbol={SymbolInfo.Symbol} swaptype={SymbolInfo.SwapType} sizelong={SymbolInfo.SwapSizeLong} sizeshort={SymbolInfo.SwapSizeShort}");

                    return factor;
                }
            }

            return 0;
        }

        #endregion

        public decimal GetOrderOpenPrice(OrderSide side)
        {
            BusinessLogicException.ThrowIfError(InitError);

            if (side == OrderSide.Buy)
            {
                if (CurrentRate.TickType == TickTypes.IndicativeAsk || CurrentRate.TickType == TickTypes.IndicativeBidAsk)
                {
                    // TO DO: get rid of exceptions!
                    throw new OffQuoteException("Open price for " + side + " " + CurrentRate.Symbol + " order is indicative!", CurrentRate.Symbol);
                }
                return CurrentRate.Ask;
            }
            else if (side == OrderSide.Sell)
            {
                if (CurrentRate.TickType == TickTypes.IndicativeBid || CurrentRate.TickType == TickTypes.IndicativeBidAsk)
                {
                    // TO DO: get rid of exceptions!
                    throw new OffQuoteException("Open price for " + side + " " + CurrentRate.Symbol + " order is indicative!", CurrentRate.Symbol);
                }
                return CurrentRate.Bid;
            }

            throw new Exception("Unknown order side: " + side);
        }

        public bool CheckOrderOpenPrice(OrderSide side)
        {
            if (side == OrderSide.Buy)
            {
                if (CurrentRate.TickType == TickTypes.IndicativeAsk || CurrentRate.TickType == TickTypes.IndicativeBidAsk)
                {
                    return false;
                }
            }
            else if (side == OrderSide.Sell)
            {
                if (CurrentRate.TickType == TickTypes.IndicativeBid || CurrentRate.TickType == TickTypes.IndicativeBidAsk)
                {
                    return false;
                }   
            }

            return true;
        }

        private bool GetBid(out decimal bid, out CalcError error)
        {
            if (!RateTracker.HasBid)
            {
                error = RateTracker.NoBidError;
                bid = 0;
                return false;
            }

            error = null;
            bid = RateTracker.Bid;
            return true;
        }

        private bool GetAsk(out decimal ask, out CalcError error)
        {
            if (!RateTracker.HasAsk)
            {
                error = RateTracker.NoAskError;
                ask = 0;
                return false;
            }

            error = null;
            ask = RateTracker.Ask;
            return true;
        }

        #region Usage Management

        internal int UsageCount { get; private set; }

        public UsageToken UsageScope()
        {
            return new UsageToken(this);
        }

        internal void AddUsage()
        {
            if (UsageCount == 0)
                Attach();
            UsageCount++;
        }

        internal void RemoveUsage()
        {
            UsageCount--;
            if (UsageCount == 0)
                Deattach();
        }

        private void Attach()
        {
            PositiveProfitConversionRate?.AddUsage();
            NegativeProfitConversionRate?.AddUsage();
            MarginConversionRate?.AddUsage();
        }

        private void Deattach()
        {
            PositiveProfitConversionRate?.RemoveUsage();
            NegativeProfitConversionRate?.RemoveUsage();
            MarginConversionRate?.RemoveUsage();
        }

        public struct UsageToken : IDisposable
        {
            public UsageToken(OrderCalculator calc)
            {
                Calculator = calc;
                calc.AddUsage();
            }

            public OrderCalculator Calculator { get; }

            public void Dispose()
            {
                Calculator.RemoveUsage();
            }
        }

        #endregion
    }
}
