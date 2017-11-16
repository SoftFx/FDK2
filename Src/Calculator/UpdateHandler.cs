namespace TickTrader.FDK.Calculator
{
    using System;
    using TickTrader.FDK.Common;
    using TickTrader.FDK.Extended;

    sealed class UpdateHandler
    {
        readonly Processor processor;
        readonly Action<Common.CurrencyInfo[], Common.SymbolInfo[], Common.AccountInfo, Quote> updateCallback;

        public object SyncRoot { get; private set; }

        public UpdateHandler(DataTrade trade, DataFeed feed, Action<Common.CurrencyInfo[], Common.SymbolInfo[], Common.AccountInfo, Quote> updateCallback, Processor processor)
        {
            if (trade == null)
                throw new ArgumentNullException(nameof(trade));

            if (feed == null)
                throw new ArgumentNullException(nameof(feed));

            if (updateCallback == null)
                throw new ArgumentNullException(nameof(updateCallback));

            if (processor == null)
                throw new ArgumentNullException(nameof(processor));

            this.updateCallback = updateCallback;
            this.processor = processor;

            this.SyncRoot = new object();

            feed.Logon += this.OnFeedLogon;
            feed.Tick += this.OnTick;

            trade.Logon += this.OnTradeLogon;
            trade.AccountInfo += this.OnAccountInfo;
            trade.BalanceOperation += this.OnBalanceOperation;
            trade.ExecutionReport += this.OnExecutionReport;
            trade.PositionReport += this.OnPositionReport;
        }

        #region Events Handlers

        void OnFeedLogon(object sender, LogonEventArgs e)
        {
            DataFeed feed = (DataFeed) sender;

            lock (this.SyncRoot)
            {
                this.updateCallback(feed.Cache.Currencies, feed.Cache.Symbols, null, null);
                this.processor.WakeUp();
            }
        }

        void OnTradeLogon(object sender, LogonEventArgs e)
        {
            DataTrade trade = (DataTrade) sender;

            lock (this.SyncRoot)
            {
                this.updateCallback(null, null, trade.Cache.AccountInfo, null);
                this.processor.WakeUp();
            }
        }

        void OnTick(object sender, TickEventArgs e)
        {
            var quote = e.Tick;

            lock (this.SyncRoot)
            {
                this.updateCallback(null, null, null, quote);
                this.processor.WakeUp();
            }
        }

        void OnAccountInfo(object sender, AccountInfoEventArgs e)
        {
            lock (this.SyncRoot)
            {
                this.updateCallback(null, null, e.Information, null);
                this.processor.WakeUp();
            }
        }

        void OnBalanceOperation(object sender, NotificationEventArgs<BalanceOperation> e)
        {
            lock (this.SyncRoot)
            {
                this.processor.WakeUp();
            }
        }

        void OnExecutionReport(object sender, ExecutionReportEventArgs e)
        {
            lock (this.SyncRoot)
            {
                this.processor.WakeUp();
            }
        }

        void OnPositionReport(object sender, PositionReportEventArgs e)
        {
            lock (this.SyncRoot)
            {
                this.processor.WakeUp();
            }
        }

        #endregion
    }
}
