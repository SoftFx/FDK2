using System;
using System.Collections;
using System.Collections.Generic;
using TickTrader.FDK.Common;
using TickTrader.FDK.Client;

namespace TickTrader.FDK.Extended
{
    public class ContingentOrderTriggerReportsEnumerator : IEnumerator<ContingentOrderTriggerReport>
    {
        internal ContingentOrderTriggerReportsEnumerator(ContingentOrderTriggerReports triggerReports, DownloadTriggerReportsEnumerator triggerReportsEnumerator)
        {
            triggerReports_ = triggerReports;
            downloadTriggerReportsEnumerator_ = triggerReportsEnumerator;

            triggerReport_ = null;
        }

        public ContingentOrderTriggerReport Current
        {
            get { return triggerReport_; }
        }

        object IEnumerator.Current
        {
            get { return triggerReport_; }
        }

        public bool MoveNext()
        {
            triggerReport_ = downloadTriggerReportsEnumerator_.Next(triggerReports_.timeout_);

            return triggerReport_ != null;
        }

        public void Reset()
        {
            downloadTriggerReportsEnumerator_.Dispose();

            downloadTriggerReportsEnumerator_ = triggerReports_.dataTrade_.tradeCaptureClient_.DownloadTriggerReports
            (
                triggerReports_.direction_,
                triggerReports_.startTime_,
                triggerReports_.endTime_,
                triggerReports_.skipFailed_,
                triggerReports_.timeout_
            );

            triggerReport_ = null;
        }

        public void Dispose()
        {
            downloadTriggerReportsEnumerator_.Dispose();

            GC.SuppressFinalize(this);
        }

        ContingentOrderTriggerReports triggerReports_;
        DownloadTriggerReportsEnumerator downloadTriggerReportsEnumerator_;
        ContingentOrderTriggerReport triggerReport_;
    }
}
