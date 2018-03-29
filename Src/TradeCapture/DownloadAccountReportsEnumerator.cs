using System;
using System.Collections.Generic;
using System.Threading;
using TickTrader.FDK.Common;

namespace TickTrader.FDK.TradeCapture
{
    public class DownloadAccountReportsEnumerator : IDisposable
    {
        internal DownloadAccountReportsEnumerator(Client client)
        {
            client_ = client;

            mutex_ = new object();
            started_ = false;
            completed_ = false;
            accountReports_ = new AccountReport[GrowSize];
            count_ = 0;
            beginIndex_ = 0;
            endIndex_ = 0;
            exception_ = null;
            event_ = new AutoResetEvent(false);
        }

        public int TotalCount
        {
            get { return totalCount_;  }
        }

        public void Begin(int timeout)
        {
            while (true)
            {
                lock (mutex_)
                {
                    if (exception_ != null)
                        throw exception_;

                    if (started_)
                        return;
                }

                if (! event_.WaitOne(timeout))
                    throw new Common.TimeoutException("Method call timed out");
            }
        }

        public AccountReport Next(int timeout)
        {
            while (true)
            {
                lock (mutex_)
                {
                    if (count_ > 0)
                    {
                        AccountReport tradeTransactionReport = accountReports_[beginIndex_];
                        accountReports_[beginIndex_] = null;       // !
                        beginIndex_ = (beginIndex_ + 1) % accountReports_.Length;
                        --count_;

                        return tradeTransactionReport;
                    }

                    if (exception_ != null)
                        throw exception_;

                    if (completed_)
                        return null;
                }

                if (! event_.WaitOne(timeout))
                    throw new Common.TimeoutException("Method call timed out");
            }
        }

        public void End(int timeout)
        {
            while (true)
            {
                lock (mutex_)
                {
                    if (count_ > 0)
                    {
                        for (int index = beginIndex_; index != endIndex_; index = (index + 1) % accountReports_.Length)
                            accountReports_[index] = null;

                        count_ = 0;
                        beginIndex_ = 0;
                        endIndex_ = 0;
                    }

                    if (exception_ != null)
                        throw exception_;

                    if (completed_)
                        return;
                }

                if (! event_.WaitOne(timeout))
                    throw new Common.TimeoutException("Method call timed out");
            }
        }

        public void Close()
        {
            lock (mutex_)
            {
                if (!completed_)
                {
                    completed_ = true;

                    try
                    {
                        client_.CancelDownloadAccountReportsAsync(null, id_);
                    }
                    catch
                    {
                    }
                }

                if (count_ > 0)
                {
                    for (int index = beginIndex_; index != endIndex_; index = (index + 1) % accountReports_.Length)
                        accountReports_[index] = null;

                    count_ = 0;
                    beginIndex_ = 0;
                    endIndex_ = 0;
                }

                event_.Close();
            }
        }

        public void Dispose()
        {
            Close();

            GC.SuppressFinalize(this);
        }

        internal void SetBegin(string id, int totalCount)
        {
            lock (mutex_)
            {
                if (! completed_)
                {
                    id_ = id;
                    totalCount_ = totalCount;
                    started_ = true;

                    event_.Set();
                }
            }
        }

        internal void SetResult(AccountReport tradeTransactionReport)
        {
            lock (mutex_)
            {
                if (! completed_)
                {
                    if (count_ == accountReports_.Length)
                    {
                        AccountReport[] tradeTransactionReports = new AccountReport[accountReports_.Length + GrowSize];

                        if (endIndex_ > beginIndex_)
                        {
                            Array.Copy(accountReports_, beginIndex_, tradeTransactionReports, 0, count_);
                        }
                        else
                        {
                            int count = accountReports_.Length - beginIndex_;
                            Array.Copy(accountReports_, beginIndex_, tradeTransactionReports, 0, count);
                            Array.Copy(accountReports_, 0, tradeTransactionReports, count, endIndex_);
                        }

                        accountReports_ = tradeTransactionReports;
                        beginIndex_ = 0;
                        endIndex_ = count_;
                    }

                    accountReports_[endIndex_] = tradeTransactionReport;
                    endIndex_ = (endIndex_ + 1) % accountReports_.Length;
                    ++count_;

                    event_.Set();
                }
            }
        }

        internal void SetEnd()
        {
            lock (mutex_)
            {
                if (! completed_)
                {
                    completed_ = true;

                    event_.Set();
                }
            }
        }

        internal void SetError(Exception exception)
        {
            lock (mutex_)
            {
                if (! completed_)
                {
                    exception_ = exception;                    
                    completed_ = true;                    

                    event_.Set();
                }
            }
        }

        const int GrowSize = 1000;

        Client client_;
        string id_;
        int totalCount_;

        object mutex_;
        bool started_;
        bool completed_;

        AccountReport[] accountReports_;
        int count_;
        int beginIndex_;
        int endIndex_;
        Exception exception_;
        AutoResetEvent event_;
    }
}