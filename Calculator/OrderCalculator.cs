namespace TickTrader.FDK.Calculator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using TickTrader.FDK.Calculator.Conversion;

    public sealed class OrderCalculator : IDependOnRates, IDisposable
	{
        readonly string symbol;
        readonly string accountCurrency;
        readonly MarketState market;
        readonly ConversionManager conversionMap;
        readonly Converter<int, int> leverageProvider;

        public OrderError InitError { get; private set; }

        public OrderCalculator(string symbol, MarketState market, string accountCurrency)
		{
            if (string.IsNullOrEmpty(symbol))
                throw new ArgumentException("Symbol must not be empty.", "symbol");

            if (market == null)
                throw new ArgumentNullException("market");

            if (string.IsNullOrEmpty(accountCurrency))
                throw new ArgumentException("Account currency must not be empty.", "accountCurrency");

            this.symbol = symbol;
            this.accountCurrency = accountCurrency;
            this.market = market;
            this.conversionMap = this.market.ConversionMap;

            this.Init();

            if (this.SymbolInfo != null && this.SymbolInfo.MarginMode != MarginCalculationModes.Forex && this.SymbolInfo.MarginMode != MarginCalculationModes.CFD_Leverage)
                this.leverageProvider = _ => 1;
            else
                this.leverageProvider = n => n;
		}

        public ISymbolRate CurrentRate { get { return this.RateTracker.Rate; } }
		public IConversionFormula PositiveProfitConversionRate { get; private set; }
        public IConversionFormula NegativeProfitConversionRate { get; private set; }
        public IConversionFormula MarginConversionRate { get; private set; }
		public ISymbolInfo SymbolInfo { get; private set; }
		public bool IsValid { get { return this.InitError == null; } }

        internal SymbolRateTracker RateTracker { get; private set; }

        IEnumerable<IConversionFormula> Formulas
        {
            get
            {
                if (PositiveProfitConversionRate != null)
                    yield return PositiveProfitConversionRate;
                if (NegativeProfitConversionRate != null)
                    yield return NegativeProfitConversionRate;
                if (MarginConversionRate != null)
                    yield return MarginConversionRate;
            }
        }

        IEnumerable<string> IDependOnRates.DependOnSymbols
        {
            get
            {
                return this.Formulas.OfType<IDependOnRates>().SelectMany(o => o.DependOnSymbols);
            }
        }

        void Init()
		{
			try
			{
				this.InitOrThrow();
			}
			catch (BusinessLogicException ex)
			{
                this.InitError = new OrderError(ex);
			}
		}

        void InitOrThrow()
		{
			this.SymbolInfo = this.market.GetISymbolInfo(this.symbol);
            this.RateTracker = this.market.GetSymbolTracker(this.symbol);

            if (this.SymbolInfo == null)
                throw new SymbolConfigException("Cannot find configuration for symbol " + this.symbol + ".");

			if (this.SymbolInfo.ProfitCurrency == null && this.SymbolInfo.MarginCurrency == null)
				throw new SymbolConfigException("Currency configuration is missing for symbol " + this.SymbolInfo.Symbol + ".");
            
            this.PositiveProfitConversionRate = this.conversionMap.GetPositiveProfitConversion(symbol, accountCurrency);
            this.NegativeProfitConversionRate = this.conversionMap.GetNegativeProfitConversion(symbol, accountCurrency);
            this.MarginConversionRate = this.conversionMap.GetMarginConversion(symbol, accountCurrency);
		}

        void VerifyInitialized()
        {
            if (this.InitError != null)
                throw this.InitError.Exception;
        }

        public void UpdateOrder(IOrderModel order, IMarginAccountInfo acc)
		{
			order.CalculationError = null;
			this.UpdateMargin(order, acc);
			this.UpdateProfit(order);
		}

		#region Margin

        public void UpdateMargin(IOrderModel order, IMarginAccountInfo acc)
        {
            try
            {
                if (InitError != null)
                    order.CalculationError = InitError;
                else
                {
                    order.Margin = CalculateMargin(order.RemainingAmount, acc.Leverage, order.Type, order.Side, order.IsHidden);
                    order.MarginRateCurrent = MarginConversionRate.Value;
                }
            }
            catch (BusinessLogicException ex)
            {
                order.CalculationError = new OrderError(ex);
            }
        }

		public decimal CalculateMargin(ICommonOrder order, IMarginAccountInfo acc)
		{
			return CalculateMargin(order.RemainingAmount, acc.Leverage, order.Type, order.Side, order.IsHidden);
		}

        public decimal CalculateMargin(decimal orderVolume, int leverage, OrderTypes ordType, OrderSides side, bool isHidden)
        {
            VerifyInitialized();

            double combinedMarginFactor = SymbolInfo.MarginFactorFractional;
            if (ordType == OrderTypes.Stop || ordType == OrderTypes.StopLimit)
                combinedMarginFactor *= SymbolInfo.StopOrderMarginReduction;
            else if (ordType == OrderTypes.Limit && isHidden)
                combinedMarginFactor *= SymbolInfo.HiddenLimitOrderMarginReduction;

            return (orderVolume * (decimal)combinedMarginFactor / leverageProvider(leverage)) * MarginConversionRate.Value;
        }

        #endregion Margin

        #region Profit

        public void UpdateProfit(IOrderModel order)
		{
            try
            {
                if (InitError != null)
                    order.CalculationError = InitError;
                else
                {
                    if (order.Type == OrderTypes.Position)
                    {
                        decimal closePrice;
                        order.Profit = CalculateProfit(order.Price.Value, order.RemainingAmount, order.Side, out closePrice);
                        order.CurrentPrice = closePrice;
                    }
                    else if (order.Type == OrderTypes.Limit || order.Type == OrderTypes.Stop || order.Type == OrderTypes.StopLimit)
                    {
                        order.CurrentPrice = order.Side == OrderSides.Sell ? GetBid() : GetAsk();
                    }
                }
            }
            catch (BusinessLogicException ex)
            {
                order.CalculationError = new OrderError(ex);
            }
		}

		public decimal CalculateProfit(IOrder order, decimal amount, out decimal closePrice)
		{
			return CalculateProfit(order.Price.Value, amount, order.Side, out closePrice);
		}

        public decimal? CalculateProfit(IOrder order)
        {
            try
            {
                decimal closePrice;
                return CalculateProfit(order, out closePrice);
            }
            catch (InvalidOperationException)
            {
                //Do nothing
                return null;
            }
        }

        public decimal CalculateProfit(IOrder order, out decimal closePrice)
		{
			return CalculateProfit(order.Price.Value, order.RemainingAmount, order.Side, out closePrice);
		}

		public decimal CalculateProfitFixedPrice(IOrder order, decimal amount, decimal closePrice)
		{
            decimal conversionRate;
			return CalculateProfitInternal(order.Price.Value, closePrice, amount, order.Side, out conversionRate);
		}

		public decimal CalculateProfit(decimal openPrice, decimal volume, OrderSides side)
		{
			decimal closePrice;
			return CalculateProfit(openPrice, volume, side, out closePrice);
		}

        public decimal CalculateProfit(decimal openPrice, decimal volume, OrderSides side, out decimal closePrice)
        {
            if (side == OrderSides.Buy)
                closePrice = GetBid();
            else
                closePrice = GetAsk();

            decimal conversionRate;
            return CalculateProfitInternal(openPrice, closePrice, volume, side, out conversionRate);
        }

		public decimal CalculateProfitFixedPrice(decimal openPrice, decimal volume, decimal closePrice, OrderSides side)
		{
            decimal conversionRate;
            return CalculateProfitFixedPrice(openPrice, closePrice, volume, side, out conversionRate);
		}

        public decimal CalculateProfitFixedPrice(decimal openPrice, decimal volume, decimal closePrice, OrderSides side, out decimal conversionRate)
        {
            return CalculateProfitInternal(openPrice, closePrice, volume, side, out conversionRate);
        }

        decimal CalculateProfitInternal(decimal openPrice, decimal closePrice, decimal volume, OrderSides side, out decimal conversionRate)
		{
            this.VerifyInitialized();

			decimal nonConvProfit;

			if (side == OrderSides.Buy)
				nonConvProfit = (closePrice - openPrice) * volume;
			else
				nonConvProfit = (openPrice - closePrice) * volume;

			return ConvertProfitToAccountCurrency(nonConvProfit, out conversionRate);
		}


        public decimal ConvertProfitToAccountCurrency(decimal profit)
        {
            decimal conversionRate;
            return ConvertProfitToAccountCurrency(profit, out conversionRate);
        }

        public decimal ConvertProfitToAccountCurrency(decimal profit, out decimal conversionRate)
        {
            if (profit >= 0)
                conversionRate = PositiveProfitConversionRate.Value;
            else
                conversionRate = NegativeProfitConversionRate.Value;
            return profit * conversionRate;
        }

        decimal GetBid()
        {
            if (CurrentRate == null || CurrentRate.NullableBid == null)
                throw new OffCrossQuoteException(SymbolInfo.Symbol, FxPriceType.Bid);
            return CurrentRate.NullableBid.Value;
        }

        decimal GetAsk()
        {
            if (CurrentRate == null || CurrentRate.NullableAsk == null)
                throw new OffCrossQuoteException(SymbolInfo.Symbol, FxPriceType.Ask);
            return CurrentRate.NullableAsk.Value;
        }

        #endregion Profit

        #region Commission

        public decimal CalculateAgentCommission(decimal amount, decimal agentCommissionValue, CommissionValueType agentCommissionValueType, CommissionChargeType agentCommissionChargeType)
        {
            return CalculateCommission(amount, agentCommissionValue, agentCommissionValueType, agentCommissionChargeType);
        }

        public decimal CalculateCommission(decimal amount, decimal cValue, CommissionValueType vType, CommissionChargeType chType)
        {
            if (cValue == 0)
                return 0;

            //UL: all calculation for CommissionChargeType.PerLot
            if (vType == CommissionValueType.Money)
            {
                //if (chType == CommissionChargeType.PerDeal)
                //    return -cValue;
                //else if (chType == CommissionChargeType.PerLot)
                return -(amount / (decimal)SymbolInfo.ContractSizeFractional * cValue);
            }
            else if (vType == CommissionValueType.Percentage)
            {
                //if (chType == CommissionChargeType.PerDeal || chType == CommissionChargeType.PerLot)
                return -(amount * cValue * MarginConversionRate.Value) / 100m;
            }
            else if (vType == CommissionValueType.Points)
            {
                decimal ptValue = cValue / (decimal)Math.Pow(10, SymbolInfo.Precision);

                //if (chType == CommissionChargeType.PerDeal)
                //    return - (ptValue * MarginConversionRate.Value);
                //else if (chType == CommissionChargeType.PerLot)
                return ConvertProfitToAccountCurrency(-(amount * ptValue));
            }

            throw new Exception("Invalid comission configuration: chType=" + chType + " vType= " + vType);
        }

        #endregion Commission

        #region Swap

        public decimal CalculateSwap(decimal amount, OrderSides side)
        {
            decimal swapAmount = GetSwapModifier(side) * amount;
            decimal swap = 0;
            if (SymbolInfo.SwapType == SwapType.Points)
                swap = ConvertProfitToAccountCurrency(swapAmount);
            else if (SymbolInfo.SwapType == SwapType.PercentPerYear)
                swap = swapAmount * MarginConversionRate.Value;

            if (SymbolInfo.TripleSwapDay > 0)
            {
                var now = DateTime.UtcNow;
                DayOfWeek swapDayOfWeek = now.DayOfWeek == DayOfWeek.Sunday ? DayOfWeek.Saturday : (int)now.DayOfWeek - DayOfWeek.Monday;
                if (SymbolInfo.TripleSwapDay == (int)swapDayOfWeek)
                    swap *= 3;
                else if (swapDayOfWeek == DayOfWeek.Saturday || swapDayOfWeek == DayOfWeek.Sunday)
                    swap = 0;
            }

            return swap;
        }

        private decimal GetSwapModifier(OrderSides side)
        {
            if (!IsValid)
                throw InitError.Exception;

            if (SymbolInfo.SwapEnabled)
            {
                if (SymbolInfo.SwapType == SwapType.Points)
                {
                    if (side == OrderSides.Buy)
                        return (decimal) SymbolInfo.SwapSizeLong/(decimal) Math.Pow(10, SymbolInfo.Precision);
                    if (side == OrderSides.Sell)
                        return (decimal) SymbolInfo.SwapSizeShort/(decimal) Math.Pow(10, SymbolInfo.Precision);
                }
                else if (SymbolInfo.SwapType == SwapType.PercentPerYear)
                {
                    const double power = 1.0/365.0;
                    double factor = 0.0;
                    if (side == OrderSides.Buy)
                        factor = Math.Sign(SymbolInfo.SwapSizeLong) * (Math.Pow(1 + Math.Abs(SymbolInfo.SwapSizeLong), power) - 1);
                    if (side == OrderSides.Sell)
                        factor = Math.Sign(SymbolInfo.SwapSizeShort) * (Math.Pow(1 + Math.Abs(SymbolInfo.SwapSizeShort), power) - 1);

                    if (double.IsInfinity(factor) || double.IsNaN(factor))
                        throw new MarketConfigurationException($"Can not calculate swap: side={side} symbol={SymbolInfo.Symbol} swaptype={SymbolInfo.SwapType} sizelong={SymbolInfo.SwapSizeLong} sizeshort={SymbolInfo.SwapSizeShort}");

                    return (decimal) factor;
                }
            }

            return 0;
        }

        #endregion

        public decimal GetOrderOpenPrice(OrderSides side)
        {
            if (side == OrderSides.Buy)
                return CurrentRate.Ask;
            else if (side == OrderSides.Sell)
                return CurrentRate.Bid;

            throw new Exception("Unknown order side: " + side);
        }

        public void Dispose()
        {
        }
    }
}
