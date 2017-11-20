namespace TickTrader.FDK.Extended
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using TickTrader.FDK.Common;
    using TickTrader.FDK.TradeCapture;

    public class TradeTransactionReportsEnumerator : IEnumerator<TradeTransactionReport>
    {
        internal TradeTransactionReportsEnumerator(TradeTransactionReports tradeTransactionReports, TradeTransactionReportEnumerator tradeTransactionReportEnumerator)
        {
            tradeTransactionReports_ = tradeTransactionReports;
            tradeTransactionReportEnumerator_ = tradeTransactionReportEnumerator;

            tradeTransactionReport_ = new TradeTransactionReport();
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
            if (tradeTransactionReport_ != null)
            {
                tradeTransactionReport_ = tradeTransactionReportEnumerator_.Next(tradeTransactionReports_.timeout_);

                return tradeTransactionReport_ != null;
            }

            return false;
        }

        public void Reset()
        {
            tradeTransactionReportEnumerator_.Dispose();

            tradeTransactionReportEnumerator_ = tradeTransactionReports_.dataTrade_.tradeCaptureClient_.DownloadTrades
            (
                tradeTransactionReports_.direction_, 
                tradeTransactionReports_.startTime_, 
                tradeTransactionReports_.endTime_, 
                tradeTransactionReports_.skipCancel_,
                tradeTransactionReports_.timeout_
            );

            tradeTransactionReport_ = new TradeTransactionReport();
        }

        public void Dispose()
        {
            tradeTransactionReportEnumerator_.Dispose();

            GC.SuppressFinalize(this);
        }

        TradeTransactionReports tradeTransactionReports_;
        TradeTransactionReportEnumerator tradeTransactionReportEnumerator_;
        TradeTransactionReport tradeTransactionReport_;
    }
}
