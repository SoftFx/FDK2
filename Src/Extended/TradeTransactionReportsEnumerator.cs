namespace TickTrader.FDK.Extended
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using TickTrader.FDK.Common;
    using TickTrader.FDK.TradeCapture;

    public class TradeTransactionReportsEnumerator : IEnumerator<TradeTransactionReport>
    {
        internal TradeTransactionReportsEnumerator(TradeTransactionReports tradeTransactionReports, DownloadTradesEnumerator tradeTransactionReportEnumerator)
        {
            tradeTransactionReports_ = tradeTransactionReports;
            downloadTradesEnumerator_ = tradeTransactionReportEnumerator;

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
            tradeTransactionReport_ = downloadTradesEnumerator_.Next(tradeTransactionReports_.timeout_);

            return tradeTransactionReport_ != null;
        }

        public void Reset()
        {
            downloadTradesEnumerator_.Dispose();

            downloadTradesEnumerator_ = tradeTransactionReports_.dataTrade_.tradeCaptureClient_.DownloadTrades
            (
                tradeTransactionReports_.direction_, 
                tradeTransactionReports_.startTime_, 
                tradeTransactionReports_.endTime_, 
                tradeTransactionReports_.skipCancel_,
                tradeTransactionReports_.timeout_
            );

            tradeTransactionReport_ = null;
        }

        public void Dispose()
        {
            downloadTradesEnumerator_.Dispose();

            GC.SuppressFinalize(this);
        }

        TradeTransactionReports tradeTransactionReports_;
        DownloadTradesEnumerator downloadTradesEnumerator_;
        TradeTransactionReport tradeTransactionReport_;
    }
}
