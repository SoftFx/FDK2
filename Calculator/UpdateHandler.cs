using System;
using TickTrader.FDK.Common;
using TickTrader.FDK.Extended;

namespace TickTrader.FDK.Calculator
{
    internal delegate void UpdateCallbackHandler(bool? tradeConnected, bool? feedConnected, AccountInfo accountInfoUpdate, Quote quote, TradeUpdate tradeUpdate, NetPositionUpdate positionUpdate, bool? configUpdated);

    sealed class UpdateHandler : IDisposable
    {
        readonly DataTrade trade;
        readonly DataFeed feed;
        readonly Processor processor;
        readonly UpdateCallbackHandler updateCallback;

        public object SyncRoot { get; private set; }

        public UpdateHandler(DataTrade trade, DataFeed feed, UpdateCallbackHandler updateCallback, Processor processor)
        {
            if (trade == null)
                throw new ArgumentNullException(nameof(trade));

            if (feed == null)
                throw new ArgumentNullException(nameof(feed));

            if (updateCallback == null)
                throw new ArgumentNullException(nameof(updateCallback));

            if (processor == null)
                throw new ArgumentNullException(nameof(processor));

            this.trade = trade;
            this.feed = feed;
            this.updateCallback = updateCallback;
            this.processor = processor;

            this.SyncRoot = new object();

            feed.Logon += this.OnFeedLogon;
            feed.Logout += this.OnFeedLogout;
            feed.Tick += this.OnTick;
            feed.Notify += this.OnFeedNotify;

            trade.Logon += this.OnTradeLogon;
            trade.Logout += this.OnTradeLogout;
            trade.AccountInfo += this.OnAccountInfo;
            trade.TradeUpdate += this.OnTradeUpdate;
            trade.PositionReport += this.OnPositionReport;
            trade.Notify += this.OnTradeNotify;
        }

        #region Events Handlers

        private void OnFeedLogon(object sender, LogonEventArgs e)
        {
            lock (this.SyncRoot)
            {
                this.updateCallback(null, true, null, null, null, null, null);
                this.processor.WakeUp();
            }
        }

        private void OnFeedLogout(object sender, LogoutEventArgs e)
        {
            lock (this.SyncRoot)
            {
                this.updateCallback(null, false, null, null, null, null, null);
                this.processor.WakeUp();
            }
        }

        void OnTradeLogon(object sender, LogonEventArgs e)
        {
            lock (this.SyncRoot)
            {
                this.updateCallback(true, null, null, null, null, null, null);
                this.processor.WakeUp();
            }
        }

        private void OnTradeLogout(object sender, LogoutEventArgs e)
        {
            lock (this.SyncRoot)
            {
                this.updateCallback(false, null, null, null, null, null, null);
                this.processor.WakeUp();
            }
        }

        private void OnTick(object sender, TickEventArgs e)
        {
            var quote = e.Tick;

            lock (this.SyncRoot)
            {
                this.updateCallback(null, null, null, quote, null, null, null);
                this.processor.WakeUp();
            }
        }

        private void OnAccountInfo(object sender, AccountInfoEventArgs e)
        {
            lock (this.SyncRoot)
            {
                this.updateCallback(null, null, e.Information, null, null, null, null);
                this.processor.WakeUp();
            }
        }

        private void OnTradeUpdate(object sender, TradeUpdateEventArgs e)
        {
            if (e != null)
            {
                lock (this.SyncRoot)
                {
                    this.updateCallback(null, null, null, null, e.Update, null, null);
                    this.processor.WakeUp();
                }
            }
        }

        private void OnPositionReport(object sender, PositionReportEventArgs e)
        {
            if (e != null)
            {
                lock (this.SyncRoot)
                {
                    this.updateCallback(null, null, null, null, null, new NetPositionUpdate {PreviousPosition = e.Previous, NewPosition = e.Report}, null);
                    this.processor.WakeUp();
                }
            }
        }

        private void OnFeedNotify(object sender, NotificationEventArgs e)
        {
            if (e.Type == NotificationType.ConfigUpdated)
            {
                lock (this.SyncRoot)
                {
                    this.updateCallback(null, null, null, null, null, null, true);
                    this.processor.WakeUp();
                }
            }
        }

        private void OnTradeNotify(object sender, NotificationEventArgs e)
        {
            if (e.Type == NotificationType.ConfigUpdated)
            {
                lock (this.SyncRoot)
                {
                    this.updateCallback(null, null, null, null, null, null, true);
                    this.processor.WakeUp();
                }
            }
        }

        #endregion

        public void Dispose()
        {
            feed.Logon -= this.OnFeedLogon;
            feed.Logout -= this.OnFeedLogout;
            feed.Tick -= this.OnTick;
            feed.Notify -= this.OnFeedNotify;

            trade.Logon -= this.OnTradeLogon;
            trade.Logout -= this.OnTradeLogout;
            trade.AccountInfo -= this.OnAccountInfo;
            trade.TradeUpdate -= this.OnTradeUpdate;
            trade.PositionReport -= this.OnPositionReport;
            trade.Notify -= this.OnTradeNotify;
        }
    }
}
