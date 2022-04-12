namespace TickTrader.FDK.Extended
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using TickTrader.FDK.Common;
    using TickTrader.FDK.Client;

    /// <summary>
    /// Contingent order trigger reports.
    /// </summary>
    public class ContingentOrderTriggerReports : IEnumerable<ContingentOrderTriggerReport>
    {
        public ContingentOrderTriggerReports(DataTrade dataTrade, TimeDirection direction, DateTime? startTime, DateTime? endTime, bool skipFailed) :
            this(dataTrade, direction, startTime, endTime, skipFailed, dataTrade.synchOperationTimeout_)
        {
        }

        public ContingentOrderTriggerReports(DataTrade dataTrade, TimeDirection direction, DateTime? startTime, DateTime? endTime, bool skipFailed, int timeout)
        {
            if (dataTrade == null)
                throw new ArgumentNullException(nameof(dataTrade), "DataTrade instance can not be null.");

            dataTrade_ = dataTrade;
            direction_ = direction;
            startTime_ = startTime;
            endTime_ = endTime;
            skipFailed_ = skipFailed;
            timeout_ = timeout;
        }

        /// <summary>
        /// The method returns contingent order trigger reports enumerator.
        /// </summary>
        /// <returns>Can not be null.</returns>
        public ContingentOrderTriggerReportsEnumerator GetEnumerator()
        {
            DownloadTriggerReportsEnumerator triggerReportsEnumerator = dataTrade_.tradeCaptureClient_.DownloadTriggerReports
            (
                direction_, 
                startTime_, 
                endTime_, 
                skipFailed_,
                timeout_
            );

            return new ContingentOrderTriggerReportsEnumerator(this, triggerReportsEnumerator);
        }

        /// <summary>
        /// The method returns contingent order trigger reports enumerator.
        /// </summary>
        /// <returns>Can not be null.</returns>
        IEnumerator<ContingentOrderTriggerReport> IEnumerable<ContingentOrderTriggerReport>.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// The method returns contingent order trigger reports enumerator.
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
        internal bool skipFailed_;
        internal int timeout_;
    }
}
