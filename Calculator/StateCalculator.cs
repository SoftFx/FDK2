using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TickTrader.FDK.Common;    
using TickTrader.FDK.Extended;
using TickTrader.FDK.Calculator.Adapter;

namespace TickTrader.FDK.Calculator
{
    /// <summary>
    /// Provides functionality for account financial state calculation.
    /// </summary>
    public class StateCalculator : IDisposable
    {
        readonly UpdateHandler updateHandler;
        readonly Processor processor;

        //EventHandler<StateInfoEventArgs> stateInfoChanged;

        readonly DataTrade trade;
        readonly DataFeed feed;

        readonly FinancialCalculator calculator;
        readonly IDictionary<string, Quote> calculatorQuotes;

        #region State Fields
        bool isReady;
        bool isInitRequired = true;
        InitFlags initFlags;
        AccountInfo accountInfo;
        IDictionary<string, Quote> quotes;
        BalanceOperation balanceOperation;
        Queue<TradeUpdate> tradeUpdatesQueue = new Queue<TradeUpdate>();
        Queue<NetPositionUpdate> positionUpdatesQueue = new Queue<NetPositionUpdate>();

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
            this.calculatorQuotes = new ConcurrentDictionary<string, Quote>();

            this.calculator = new FinancialCalculator();

            this.trade = trade;
            this.feed = feed;

            this.processor = new Processor(this.Calculate);

            this.processor.Exception += this.OnException;
            this.processor.Executed += this.OnExecuted;

            this.updateHandler = new UpdateHandler(trade, feed, this.OnUpdate, processor);

            processor.Start();
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

        void OnUpdate(bool? tradeConnected, bool? feedConnected, AccountInfo accountInfoUpdate, Quote quote,
            TradeUpdate tradeUpdate, NetPositionUpdate positionUpdate, bool? configUpdated)
        {
            if (tradeConnected != null)
            {
                if (tradeConnected.Value)
                {
                    this.initFlags |= InitFlags.Trade;
                }
                else
                {
                    this.initFlags &= ~InitFlags.Trade;
                    this.isInitRequired = true;
                }
            }

            if (feedConnected != null)
            {
                if (feedConnected.Value)
                {
                    this.initFlags |= InitFlags.Feed;
                }
                else
                {
                    this.initFlags &= ~InitFlags.Feed;
                    this.isInitRequired = true;
                }
            }

            if (accountInfoUpdate != null)
                this.accountInfo = accountInfoUpdate;

            if (quote != null)
                this.quotes[quote.Symbol] = quote;

            if (tradeUpdate != null)
                tradeUpdatesQueue.Enqueue(tradeUpdate);

            if (positionUpdate != null)
                positionUpdatesQueue.Enqueue(positionUpdate);

            if (configUpdated != null && configUpdated.Value)
            {
                this.isInitRequired = true;
            }
        }

        void OnExecuted(object sender, EventArgs e)
        {
            StateInfo info;
            lock (this.calculator)
            {
                if (!this.calculator.IsInitialized)
                    return;
                info = GetState();
            }
            Events.Raise(this.StateInfoChanged, this, () => new StateInfoEventArgs(info));
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
            AccountInfo accountUpdate;
            IDictionary<string, Quote> quotesUpdate;
            IEnumerable<TradeUpdate> tradeUpdates = null;
            IEnumerable<NetPositionUpdate> positionUpdates = null;
            bool initialize = false;

            lock (this.updateHandler.SyncRoot)
            {
                if (!isReady && initFlags != InitFlags.All)
                {
                    this.processor.EndWakeUp();
                    return;
                }
                isReady = true;

                if (isInitRequired && initFlags == InitFlags.All)
                {
                    initialize = true;
                    isInitRequired = false;
                }

                accountUpdate = this.accountInfo;
                this.accountInfo = null;

                quotesUpdate = this.quotes;
                this.quotes = new Dictionary<string, Quote>();

                if (this.tradeUpdatesQueue.Count > 0)
                {
                    tradeUpdates = this.tradeUpdatesQueue;
                    tradeUpdatesQueue = new Queue<TradeUpdate>();
                }

                if (this.positionUpdatesQueue.Count > 0)
                {
                    positionUpdates = this.positionUpdatesQueue;
                    positionUpdatesQueue = new Queue<NetPositionUpdate>();
                }

                this.processor.EndWakeUp();
            }

            lock (this.calculator)
            {
                this.ProcessUpdates(accountUpdate, quotesUpdate, tradeUpdates, positionUpdates, initialize);
            }
        }

        void ProcessUpdates(AccountInfo accountUpdate, IDictionary<string, Quote> quotesUpdate, IEnumerable<TradeUpdate> tradeUpdates, IEnumerable<NetPositionUpdate> positionUpdates, bool initialize = false)
        {
            if (initialize)
            {
                var cacheQuotes = this.feed.Cache.Quotes;
                this.calculator.Initialize(this.feed.Cache.Symbols, this.feed.Cache.Currencies, cacheQuotes, this.trade.Cache.AccountInfo, this.trade.Cache.TradeRecords, this.trade.Cache.Positions);

                foreach (var quote in cacheQuotes)
                {
                    this.calculatorQuotes[quote.Symbol] = quote;
                }
            }
            else
            {
                if (quotesUpdate != null)
                {
                    foreach (var quote in quotesUpdate.Values)
                    {
                        this.calculator.UpdateRate(quote.ToSymbolRate());
                        this.calculatorQuotes[quote.Symbol] = quote;
                    }
                }

                if (tradeUpdates != null)
                {
                    foreach (var update in tradeUpdates)
                    {
                        this.calculator.ProcessTradeUpdate(update);
                    }
                }

                if (positionUpdates != null)
                {
                    foreach (var update in positionUpdates)
                    {
                        this.calculator.ProcessPositionUpdate(update);
                    }
                }

                // Should be processed after trade updates since we update Assets with the latest ones from cache
                if (accountUpdate != null)
                {
                    this.calculator.ProcessAccountInfoUpdate(this.trade.Cache.AccountInfo);
                }
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
                return new StateInfo(this.calculator.Account, calculatorQuotes, this.processor.Generation, this.calculator.IsInitialized);
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// State calculator raises this event when something has been changed.
        /// </summary>
        public event EventHandler<StateInfoEventArgs> StateInfoChanged;
        /*{
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
        }*/

        /// <summary>
        /// State calculator raises this event when exception has been encountered.
        /// </summary>
        public event EventHandler<ExceptionEventArgs> CalculatorException;

        #endregion

        public void Dispose()
        {
            try
            {
                lock (this.updateHandler.SyncRoot)
                {
                    this.processor.Stop();
                    this.processor.Exception -= this.OnException;
                    this.processor.Executed -= this.OnExecuted;

                    this.updateHandler.Dispose();
                }

                lock (this.calculator)
                {
                    this.calculator.Clear();
                }
            }
            catch { }
        }

        [Flags]
        internal enum InitFlags
        {
            None = 0x00,
            Feed = 0x01,
            Trade = 0x02,
            All = Feed | Trade
        }
    }
}

// Feb 27, 2020