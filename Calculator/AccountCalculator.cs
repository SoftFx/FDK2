namespace TickTrader.FDK.Calculator
{
    using System;
    using System.Collections.Generic;
    using TickTrader.FDK.Calculator.Netting;

    public class AccountCalculator : ICalculable, IDisposable
    {
        readonly IMarginAccountInfo account;
        readonly IDictionary<string, SymbolNetting> nettingMap = new Dictionary<string, SymbolNetting>();
        protected MarketState market;

        public const int DefaultRounding = 2;

        public AccountCalculator(IMarginAccountInfo infoProvider, MarketState market)
        {
            if (infoProvider == null)
                throw new ArgumentNullException("infoProvider");

            if (market == null)
                throw new ArgumentNullException("market");

            this.Stats = new AccountCalculatorStats();

            this.market = market;

            this.account = infoProvider;

            this.AddOrdersBunch(this.account.Orders);

            if (this.account.Positions != null)
                this.account.Positions.Foreach(Update);

            this.account.OrderAdded += AddOrder;
            this.account.OrderRemoved += RemoveOrder;
            this.account.OrdersAdded += AddOrdersBunch;
            this.account.OrderReplaced += ReplaceOrder;
            this.account.PositionChanged += Update;

            // YZ: This shouldn't be there, this calculator shouldn't be used for cash accounts
            if (account.AccountingType != AccountingTypes.Cash)
            {
                this.market.CurrenciesChanged += this.InitRounding;
                this.InitRounding();
            }
        }

        public int RoundingDigits { get; private set; }

        public IMarginAccountInfo Info
        {
            get { return this.account; }
        }

        public IEnumerable<SymbolNetting> Nettings
        {
            get { return this.nettingMap.Values; }
        }

        public MarketState Market
        {
            get { return this.market; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value", "Market property cannot be null.");

                if (this.market == value)
                    return;
                
                this.market = value;
                this.nettingMap.Values.Foreach(n => n.Market = value);
            }
        }

        public AccountCalculatorStats Stats { get; private set; }

        #region Summary

        public bool IsCalculated { get; private set; }
        public OrderErrorCode WorstCalculationError { get; private set; }

        public decimal Profit { get; private set; }
        public decimal Equity { get; private set; }
        public decimal Margin { get; private set; }
        public decimal MarginLevel { get; private set; }
	    public decimal Commission { get; private set; }
        public decimal AgentCommission { get; private set; }
        public decimal Swap { get; private set; }

        #endregion

        #region Public Methods

        public bool HasSufficientMarginToOpenOrder(ICommonOrder order, decimal? margin)
        {
            decimal oldMargin;
            decimal newMargin;
            return HasSufficientMarginToOpenOrder(order, margin, out oldMargin, out newMargin);
        }

        public bool HasSufficientMarginToOpenOrder(ICommonOrder order, decimal? margin, out decimal oldMargin, out decimal newMargin)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            if (margin == null)
                throw new MarginNotCalculatedException("Provided order must have calculated Margin.");

            var netting = this.nettingMap.GetOrDefault(order.Symbol);

            if (netting == null)
            {
                oldMargin = 0;
                newMargin = margin.Value;
            }
            else
            {
                var marginBuy = netting.Buy.Margin;
                var marginSell = netting.Sell.Margin;
                oldMargin = Math.Max(netting.Sell.Margin, netting.Buy.Margin);

                if (order.Side == OrderSides.Buy)
                    newMargin = Math.Max(marginSell, marginBuy + margin.Value);
                else
                    newMargin = Math.Max(marginSell + margin.Value, marginBuy);
            }

            decimal marginIncrement = newMargin - oldMargin;
            if (marginIncrement <= 0)
            {
                newMargin = oldMargin;
                return true;
            }

            return (this.Margin + marginIncrement) < this.Equity;
        }

        // Cached thread Id
        int? cacheThreadId;
        object cacheLock = new object();

        public void UpdateSummary(UpdateKind updateKind)
        {
            // Сheck thread Id of the account calculator
            int currentThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            if (cacheThreadId.HasValue && (cacheThreadId.Value != currentThreadId))
            {
                var trace = new System.Diagnostics.StackTrace();
                account.LogWarn($"Update summary for account {account.Id} is called from thread {currentThreadId}, but expected thread is {cacheThreadId.Value}" + Environment.NewLine + trace);
            }
            cacheThreadId = currentThreadId;

            // Use lock to protect account update summary code from exotic places (e.g. config change events)
            lock (cacheLock)
            {
                Profit = 0;
                Equity = 0;
                Margin = 0;
                MarginLevel = 0;
                Commission = 0;
                AgentCommission = 0;
                Swap = 0;
                IsCalculated = true;
                WorstCalculationError = OrderErrorCode.None;

                foreach (var netting in nettingMap.Values)
                {
                    if (!netting.IsCalculated)
                    {
                        IsCalculated = false;
                        WorstCalculationError = OrderError.GetWorst(WorstCalculationError, netting.WorstError);
                    }

                    Profit += netting.Profit;
                    Margin += netting.Margin;
                    Commission += netting.Commission;
                    AgentCommission += netting.AgentCommission;
                    Swap += netting.Swap;
                }

                Equity = account.Balance + Profit + Swap + Commission + AgentCommission;

                if (Margin > 0)
                    MarginLevel = 100 * Equity / Margin;

                this.Stats.Update(updateKind);

                this.OnUpdated();
            }
        }

        protected virtual void OnUpdated()
        {
        }

        public void AddOrder(IOrderModel order)
        {
            var netting = GetOrAddNetting(order.Symbol);
            netting.AddOrder(order);
            UpdateSummary(UpdateKind.OrderAdded);
        }

        public void AddOrdersBunch(IEnumerable<IOrderModel> bunch)
        {
            foreach (var order in bunch)
            {
                var netting = GetOrAddNetting(order.Symbol);
                netting.AddOrder(order);
            }

            UpdateSummary(UpdateKind.OrderAdded);
        }

        public void RemoveOrder(IOrderModel order)
        {
            var netting = nettingMap.GetOrDefault(order.Symbol);

            if (netting == null)
                throw new InvalidOperationException("Cannot find netting for symbol " + order.Symbol);

            netting.RemoveOrder(order.OrderId);

            RemoveNettingIfEmpty(netting);

            UpdateSummary(UpdateKind.OrderRemoved);
        }

        public void ReplaceOrder(IOrderModel order)
        {
            var netting = GetOrAddNetting(order.Symbol);

            if (netting == null)
                throw new InvalidOperationException("Cannot find netting for symbol " + order.Symbol);

            netting.ReplaceOrder(order);

            UpdateSummary(UpdateKind.OrderChanged);
        }

        public void Update(IPositionModel position, PositionChageTypes chType)
        {
            if (chType == PositionChageTypes.AddedModified)
                Update(position);
            else if (chType == PositionChageTypes.Removed)
                Remove(position);
        }

        public void Update(IPositionModel position)
        {
            var netting = GetOrAddNetting(position.Symbol);
            netting.Update(position);
            UpdateSummary(UpdateKind.PositionUpdated);
            if (position.Short.Amount == 0 && position.Long.Amount == 0)
                RemoveNettingIfEmpty(netting);
        }

        public void Remove(IPositionModel position)
        {
            var netting = GetOrAddNetting(position.Symbol);
            netting.Remove(position);
            UpdateSummary(UpdateKind.PositionRemoved);
            RemoveNettingIfEmpty(netting);
        }

        #endregion

        private void RemoveNettingIfEmpty(SymbolNetting netting)
        {
            if (netting.IsEmpty)
            {
                nettingMap.Remove(netting.Symbol);
                netting.Dispose();
            }
        }

        private SymbolNetting GetOrAddNetting(string symbol)
        {
            return nettingMap.GetOrAdd(symbol, () => new SymbolNetting(symbol, this, market));
        }

        void InitRounding()
        {
            ICurrencyInfo curr = this.market.GetCurrencyOrThrow(account.BalanceCurrency);
            if (curr != null && curr.Precision >= 0)
                this.RoundingDigits = curr.Precision;
            else
                this.RoundingDigits = DefaultRounding;
        }

        public virtual void Dispose()
        {
            this.account.OrderAdded -= AddOrder;
            this.account.OrderRemoved -= RemoveOrder;
            this.account.OrdersAdded -= AddOrdersBunch;
            this.account.PositionChanged -= Update;
            this.account.OrderReplaced -= ReplaceOrder;

            // YZ: This shouldn't be there, this calculator shouldn't be used for cash accounts
            if (account.AccountingType != AccountingTypes.Cash)
                this.market.CurrenciesChanged -= this.InitRounding;

            foreach (var netting in nettingMap.Values)
                netting.Dispose();
        }

        public decimal GetMinCommissionConversionRate(string minCommissCurrency)
        {
            try
            {
                return Market.ConversionMap.GetNegativeAssetConversion(minCommissCurrency, account.BalanceCurrency).Value;
            }
            catch (Exception)
            {
                WorstCalculationError = OrderErrorCode.Misconfiguration;
                throw;
            }
        }
    }
}
