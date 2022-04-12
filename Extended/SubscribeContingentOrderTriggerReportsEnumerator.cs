using System;
using System.Collections;
using System.Collections.Generic;
using TickTrader.FDK.Common;
using TickTrader.FDK.Client;

namespace TickTrader.FDK.Extended
{
    public class SubscribeContingentOrderTriggerReportsEnumerator : IEnumerator<ContingentOrderTriggerReport>
    {
        internal SubscribeContingentOrderTriggerReportsEnumerator(DataTrade dataTrade, DateTime? from, bool skipFailed, int timeout, SubscribeTriggerReportsEnumerator subscribeTriggerReportsEnumerator)
        {
            dataTrade_ = dataTrade;
            from_ = from;
            skipFailed_ = skipFailed;
            timeout_ = timeout;
            subscribeTriggerReportsEnumerator_ = subscribeTriggerReportsEnumerator;

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
            triggerReport_ = subscribeTriggerReportsEnumerator_.Next(timeout_);

            return triggerReport_ != null;
        }

        public void Reset()
        {
            subscribeTriggerReportsEnumerator_.Dispose();

            subscribeTriggerReportsEnumerator_ = dataTrade_.tradeCaptureClient_.SubscribeTriggerReports
            (
                from_,
                skipFailed_,
                timeout_
            );

            triggerReport_ = null;
        }

        public void Dispose()
        {
            subscribeTriggerReportsEnumerator_.Dispose();

            GC.SuppressFinalize(this);
        }

        DataTrade dataTrade_;
        DateTime? from_;
        bool skipFailed_;
        int timeout_;
        SubscribeTriggerReportsEnumerator subscribeTriggerReportsEnumerator_;
        ContingentOrderTriggerReport triggerReport_;
    }
}
