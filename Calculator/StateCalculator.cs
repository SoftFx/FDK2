namespace TickTrader.FDK.Calculator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using TickTrader.FDK.Common;    
    using TickTrader.FDK.Extended;
    using TickTrader.FDK.Calculator.Rounding;

    /// <summary>
    /// Provides functionality for account financial state calculation.
    /// </summary>
    public class StateCalculator
    {
        readonly UpdateHandler updateHandler;
        readonly Processor processor;

        EventHandler<StateInfoEventArgs> stateInfoChanged;

        readonly DataTrade trade;
        readonly DataFeed feed;

        readonly FinancialCalculator calculator;
        readonly AccountEntry account;
        readonly IDictionary<string, Quote> calculatorQuotes;

        #region State Fields

        IEnumerable<Common.CurrencyInfo> currencyInfo;
        IEnumerable<Common.SymbolInfo> symbolInfo;
        Common.AccountInfo accountInfo;
        IDictionary<string, Quote> quotes;

        #endregion

        #region Construction

        /// <summary>
        /// Creates new financial state of account calculator.
        /// </summary>
        /// <param name="trade">valid instance of not started data trade</param>
        /// <param name="feed">valid instance of not started data feed</param>
        public StateCalculator(DataTrade trade, DataFeed feed)
        {
            if (trade == null)
                throw new ArgumentNullException(nameof(trade), "Data trade argument can not be null");

            if (trade.IsStarted)
                throw new ArgumentException("Started data trade can not be used for creating state calculator.", nameof(trade));

            if (feed == null)
                throw new ArgumentNullException(nameof(feed), "Data feed argument can not be null");

            if (feed.IsStarted)
                throw new ArgumentException("Started data feed can not be used for creating state calculator.", nameof(feed));


            this.quotes = new Dictionary<string, Quote>();
            this.calculatorQuotes = new Dictionary<string, Quote>();

            this.calculator = new FinancialCalculator();
            this.account = new AccountEntry(calculator);
            this.calculator.Accounts.Add(this.account);

            this.trade = trade;
            this.feed = feed;

            this.processor = new Processor(this.Calculate);

            this.processor.Exception += this.OnException;
            this.processor.Executed += this.OnExecuted;

            this.updateHandler = new UpdateHandler(trade, feed, this.OnUpdate, processor);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Be careful that you use Calculator properties inside of critical section; example:
        /// StateCalculator calculator = ...
        /// FinancialCalculator calc = calcualtor.Calculator;
        /// lock (calc)
        /// {
        ///     calc.Currencies.Add("EUR");
        /// }
        /// </summary>
        public FinancialCalculator Calculator
        {
            get
            {
                return this.calculator;
            }
        }

        #endregion

        #region Event Handlers

        void OnUpdate(Common.CurrencyInfo[] currencyInfo, Common.SymbolInfo[] symbolInfo, Common.AccountInfo accountInfo, Quote quote)
        {
            if (currencyInfo != null)
                this.currencyInfo = currencyInfo;

            if (symbolInfo != null)
                this.symbolInfo = symbolInfo;

            if (accountInfo != null)
                this.accountInfo = accountInfo;

            if (quote != null)
                this.quotes[quote.Symbol] = quote;
        }

        void OnExecuted(object sender, EventArgs e)
        {
            Events.Raise(this.stateInfoChanged, this, () => new StateInfoEventArgs(this.GetState()));
        }

        void OnException(object sender, ExceptionEventArgs e)
        {
            Events.Raise(this.CalculatorException, this, e);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Recalculates margin and profit.
        /// </summary>
        public void Calculate()
        {
            IEnumerable<Common.CurrencyInfo> currencyUpdate;
            IEnumerable<Common.SymbolInfo> symbolsUpdate;
            Common.AccountInfo accountUpdate;
            IDictionary<string, Quote> quotesUpdate;

            var newQuotes = new Dictionary<string, Quote>();

            lock (this.updateHandler.SyncRoot)
            {
                currencyUpdate = this.currencyInfo;
                this.currencyInfo = null;

                symbolsUpdate = this.symbolInfo;
                this.symbolInfo = null;

                accountUpdate = this.accountInfo;
                this.accountInfo = null;

                quotesUpdate = this.quotes;
                this.quotes = newQuotes;

                this.processor.EndWakeUp();
            }

            lock (this.calculator)
            {
                this.PrepareCalculator(accountUpdate, currencyUpdate, symbolsUpdate, quotesUpdate);

                this.calculator.Calculate();
            }
        }

        void PrepareCalculator(Common.AccountInfo accountUpdate, IEnumerable<Common.CurrencyInfo> currencyUpdate, IEnumerable<Common.SymbolInfo> symbolsUpdate, IDictionary<string, Quote> quotesUpdate)
        {
            if (accountUpdate != null)
            {               
                this.account.Balance = accountUpdate.Balance.GetValueOrDefault();
                this.account.Currency = accountUpdate.Currency;
                this.account.Type = accountUpdate.Type;

                if (this.account.Type != AccountType.Cash)
                {
                    this.account.Leverage = accountUpdate.Leverage.GetValueOrDefault();

                    if (this.feed.Cache.Currencies.Select(o => o.Name).Contains(account.Currency))
                    {
                        var precisionProvider = new CurrencyPrecisionProvider(this.feed.Cache.Currencies);
                        this.account.RoundingService = new AccountRoundingService(FinancialRounding.Instance, precisionProvider, account.Currency);
                    }
                }
            }

            if (this.account.Type != AccountType.Cash)
            {
                this.account.Balance = this.trade.Cache.AccountInfo.Balance.GetValueOrDefault();
            }

            if (currencyUpdate != null)
            {
                var currencies = currencyUpdate.OrderBy(o => o.SortOrder).Select(c => new CurrencyEntry(this.calculator, c.Name, c.Precision, c.SortOrder));

                this.calculator.Currencies.Clear();

                foreach (var currency in currencies)
                {
                    this.calculator.Currencies.Add(currency);
                }
            }

            if (symbolsUpdate != null)
            {
                this.calculator.Symbols.Clear();

                foreach (var symbol in symbolsUpdate.OrderByDescending(o => o.Name))
                {
                    var entry = new SymbolEntry(this.calculator, symbol.Name, symbol.SettlementCurrency, symbol.Currency)
                    {
                        MarginFactor = symbol.GetMarginFactor(),
                        MarginFactorOfPositions = 1,
                        MarginFactorOfLimitOrders = 1,
                        MarginFactorOfStopOrders = 1,
                        Hedging = symbol.MarginHedge,
                        MarginCalcMode = symbol.MarginCalcMode,
                        StopOrderMarginReduction = symbol.StopOrderMarginReduction,
                        HiddenLimitOrderMarginReduction = symbol.HiddenLimitOrderMarginReduction
                    };

                    entry.GroupSortOrder = symbol.GroupSortOrder;
                    entry.SortOrder = symbol.SortOrder;

                    this.calculator.Symbols.Add(entry);
                }

                if (this.feed.Cache.Currencies.Select(o => o.Name).Contains(account.Currency))
                {
                    var precisionProvider = new CurrencyPrecisionProvider(this.feed.Cache.Currencies);
                    this.account.RoundingService = new AccountRoundingService(FinancialRounding.Instance, precisionProvider, account.Currency);
                }
            }

            foreach (var quote in quotesUpdate.Values)
            {
                this.calculator.Prices.Update(quote.Symbol, quote.Bid, quote.Ask);
                this.calculatorQuotes[quote.Symbol] = quote;
            }

            this.account.Trades.Clear();

            var records = this.trade.Cache.TradeRecords;
            foreach (var record in records)
            {
                var entry = new TradeEntry(this.account, record.Type, record.Side, record.Symbol, record.Volume, record.MaxVisibleVolume, record.Price, record.StopPrice)
                {
                    Tag = record,
                    Commission = record.Commission,
                    AgentCommission = record.AgentCommission,
                    Swap = record.Swap,
                };

                this.account.Trades.Add(entry);
            }

            var positions = this.trade.Cache.Positions;
            foreach (var position in positions)
            {
                var buy = new TradeEntry(this.account, OrderType.Position, OrderSide.Buy, position.Symbol, position.BuyAmount, null, position.BuyPrice.Value, null)
                {
                    Tag = position,
                    Commission = position.Commission,
                    AgentCommission = position.AgentCommission,
                    Swap = position.Swap

                };
                var sell = new TradeEntry(this.account, OrderType.Position, OrderSide.Sell, position.Symbol, position.SellAmount, null, position.SellPrice.Value, null)
                {
                    Tag = position
                };

                // some magic; see TryProcessAsPosition of StateInfo class
                this.account.Trades.Add(buy);
                this.account.Trades.Add(sell);
            }
        }

        /// <summary>
        /// Returns StateInfo representation of current state.
        /// </summary>
        /// <returns></returns>
        public StateInfo GetState()
        {
            lock (this.calculator)
            {
                IDictionary<string, Asset> assets;

                if (this.account.Type != AccountType.Cash)
                    assets = this.account.Assets;
                else
                    assets = new CashAssets(this.trade.Cache.AccountInfo.Assets, this.account, this.Calculator.MarketState).AsDictionary();

                return new StateInfo(this.account, assets, this.calculator.Prices.ToDictionary(), this.calculatorQuotes, this.calculator.Symbols, this.processor.Generation);
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// State calculator raises this event when something has been changed.
        /// </summary>
        public event EventHandler<StateInfoEventArgs> StateInfoChanged
        {
            add
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value), "StateInfoChanged can not be null");

                lock (this.updateHandler.SyncRoot)
                {
                    var startProcessor = this.stateInfoChanged == null;

                    this.stateInfoChanged += value;

                    if (startProcessor)
                        this.processor.Start();
                }
            }
            remove
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value), "StateInfoChanged can not be null");

                lock (this.updateHandler.SyncRoot)
                {
                    this.stateInfoChanged -= value;

                    if (this.stateInfoChanged == null)
                        this.processor.Stop();
                }
            }
        }

        /// <summary>
        /// State calculator raises this event when exception has been encountered.
        /// </summary>
        public event EventHandler<ExceptionEventArgs> CalculatorException;

        #endregion
    }
}

// Dec 6, 2017