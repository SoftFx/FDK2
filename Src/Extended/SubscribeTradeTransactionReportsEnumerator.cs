namespace TickTrader.FDK.Extended
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using TickTrader.FDK.Common;
    using TickTrader.FDK.TradeCapture;

    public class SubscribeTradeTransactionReportsEnumerator : IEnumerator<TradeTransactionReport>
    {
        internal SubscribeTradeTransactionReportsEnumerator(DataTrade dataTrade, DateTime? from, bool skipCancel, int timeout, SubscribeTradesEnumerator subscribeTradesEnumerator)
        {
            dataTrade_ = dataTrade;
            from_ = from;
            skipCancel_ = skipCancel;
            timeout_ = timeout;
            subscribeTradesEnumerator_ = subscribeTradesEnumerator;

            tradeTransactionReport_ = null;
        }

        public TradeTransactionReport Current
        {
            get { return tradeTransactionReport_; }
        }

        object IEnumerator.Current
        {
            get { return tradeTransactionReport_; }
        }

        public bool MoveNext()
        {
            tradeTransactionReport_ = subscribeTradesEnumerator_.Next(timeout_);

            return tradeTransactionReport_ != null;
        }

        public void Reset()
        {
            subscribeTradesEnumerator_.Dispose();

            subscribeTradesEnumerator_ = dataTrade_.tradeCaptureClient_.SubscribeTrades
            (
                from_,
                skipCancel_,
                timeout_
            );

            tradeTransactionReport_ = null;
        }

        public void Dispose()
        {
            subscribeTradesEnumerator_.Dispose();

            GC.SuppressFinalize(this);
        }

        DataTrade dataTrade_;
        DateTime? from_;
        bool skipCancel_;
        int timeout_;
        SubscribeTradesEnumerator subscribeTradesEnumerator_;
        TradeTransactionReport tradeTransactionReport_;
    }
}
