namespace TickTrader.FDK.Extended
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using TickTrader.FDK.Common;
    using TickTrader.FDK.Client;

    /// <summary>
    /// Trade transaction reports.
    /// </summary>
    public class TradeTransactionReports : IEnumerable<TradeTransactionReport>
    {
        public TradeTransactionReports(DataTrade dataTrade, TimeDirection direction, DateTime? startTime, DateTime? endTime, bool skipCancel) :
            this(dataTrade, direction, startTime, endTime, skipCancel, dataTrade.synchOperationTimeout_)
        {
        }

        public TradeTransactionReports(DataTrade dataTrade, TimeDirection direction, DateTime? startTime, DateTime? endTime, bool skipCancel, int timeout)
        {
            if (dataTrade == null)
                throw new ArgumentNullException(nameof(dataTrade), "DataTrade instance can not be null.");

            dataTrade_ = dataTrade;
            direction_ = direction;
            startTime_ = startTime;
            endTime_ = endTime;
            skipCancel_ = skipCancel;
            timeout_ = timeout;
        }

        /// <summary>
        /// The method returns bars enumerator.
        /// </summary>
        /// <returns>Can not be null.</returns>
        public TradeTransactionReportsEnumerator GetEnumerator()
        {
            DownloadTradesEnumerator tradeTransactionReportEnumerator = dataTrade_.tradeCaptureClient_.DownloadTrades
            (
                direction_, 
                startTime_, 
                endTime_, 
                skipCancel_,
                timeout_
            );

            return new TradeTransactionReportsEnumerator(this, tradeTransactionReportEnumerator);
        }

        /// <summary>
        /// The method returns bars enumerator.
        /// </summary>
        /// <returns>Can not be null.</returns>
        IEnumerator<TradeTransactionReport> IEnumerable<TradeTransactionReport>.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// The method returns bars enumerator.
        /// </summary>
        /// <returns>Can not be null.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal DataTrade dataTrade_;
        internal TimeDirection direction_;
        internal DateTime? startTime_;
        internal DateTime? endTime_;
        internal bool skipCancel_;
        internal int timeout_;
    }
}
