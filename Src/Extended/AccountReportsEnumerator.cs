namespace TickTrader.FDK.Extended
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using TickTrader.FDK.Common;
    using TickTrader.FDK.TradeCapture;

    public class AccountReportsEnumerator : IEnumerator<AccountReport>
    {
        internal AccountReportsEnumerator(AccountReports accountReports, AccountReportEnumerator accountReportEnumerator)
        {
            accountReports_ = accountReports;
            accountReportEnumerator_ = accountReportEnumerator;

            accountReport_ = null;
        }

        public AccountReport Current
        {
            get { return accountReport_; }
        }

        object IEnumerator.Current
        {
            get { return accountReport_; }
        }

        public bool MoveNext()
        {
            accountReport_ = accountReportEnumerator_.Next(accountReports_.timeout_);

            return accountReport_ != null;
        }

        public void Reset()
        {
            accountReportEnumerator_.Dispose();

            accountReportEnumerator_ = accountReports_.dataTrade_.tradeCaptureClient_.DownloadAccountReports
            (
                accountReports_.direction_, 
                accountReports_.startTime_, 
                accountReports_.endTime_, 
                accountReports_.timeout_
            );

            accountReport_ = null;
        }

        public void Dispose()
        {
            accountReportEnumerator_.Dispose();

            GC.SuppressFinalize(this);
        }

        AccountReports accountReports_;
        AccountReportEnumerator accountReportEnumerator_;
        AccountReport accountReport_;
    }
}
