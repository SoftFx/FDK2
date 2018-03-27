namespace TickTrader.FDK.Extended
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using TickTrader.FDK.Common;
    using TickTrader.FDK.TradeCapture;

    /// <summary>
    /// Account reports.
    /// </summary>
    public class AccountReports : IEnumerable<AccountReport>
    {
        public AccountReports(DataTrade dataTrade, TimeDirection direction, DateTime? startTime, DateTime? endTime) :
            this(dataTrade, direction, startTime, endTime, dataTrade.synchOperationTimeout_)
        {
        }

        public AccountReports(DataTrade dataTrade, TimeDirection direction, DateTime? startTime, DateTime? endTime, int timeout)
        {
            if (dataTrade == null)
                throw new ArgumentNullException(nameof(dataTrade), "DataTrade instance can not be null.");

            dataTrade_ = dataTrade;
            direction_ = direction;
            startTime_ = startTime;
            endTime_ = endTime;
            timeout_ = timeout;
        }

        /// <summary>
        /// The method returns bars enumerator.
        /// </summary>
        /// <returns>Can not be null.</returns>
        public AccountReportsEnumerator GetEnumerator()
        {
            DownloadAccountReportsEnumerator accountReportEnumerator = dataTrade_.tradeCaptureClient_.DownloadAccountReports
            (
                direction_, 
                startTime_, 
                endTime_, 
                timeout_
            );

            return new AccountReportsEnumerator(this, accountReportEnumerator);
        }

        /// <summary>
        /// The method returns bars enumerator.
        /// </summary>
        /// <returns>Can not be null.</returns>
        IEnumerator<AccountReport> IEnumerable<AccountReport>.GetEnumerator()
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
        internal int timeout_;
    }
}
