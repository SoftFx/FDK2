﻿using System;
using System.Collections.Generic;
using System.Threading;
using TickTrader.FDK.Common;

namespace TickTrader.FDK.Client
{
    public class SubscribeTriggerReportsEnumerator : IDisposable
    {
        internal SubscribeTriggerReportsEnumerator(TradeCapture tradeCapture)
        {
            tradeCapture_ = tradeCapture;

            mutex_ = new object();
            started_ = false;
            completed_ = false;
            tradeTransactionReports_ = new ContingentOrderTriggerReport[GrowSize];
            tradeTransactionReportCount_ = 0;
            beginIndex_ = 0;
            endIndex_ = 0;
            exception_ = null;
            event_ = new AutoResetEvent(false);
        }

        public int TotalCount
        {
            get { return totalCount_; }
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

                if (!event_.WaitOne(timeout))
                    throw new Common.TimeoutException("Method call timed out");
            }
        }

        public ContingentOrderTriggerReport Next(int timeout)
        {
            while (true)
            {
                lock (mutex_)
                {
                    if (tradeTransactionReportCount_ > 0)
                    {
                        ContingentOrderTriggerReport tradeTransactionReport = tradeTransactionReports_[beginIndex_];
                        tradeTransactionReports_[beginIndex_] = null;       // !
                        beginIndex_ = (beginIndex_ + 1) % tradeTransactionReports_.Length;
                        --tradeTransactionReportCount_;

                        return tradeTransactionReport;
                    }

                    if (exception_ != null)
                        throw exception_;

                    if (completed_)
                        return null;
                }

                if (!event_.WaitOne(timeout))
                    throw new Common.TimeoutException("Method call timed out");
            }
        }

        public void End(int timeout)
        {
            while (true)
            {
                lock (mutex_)
                {
                    if (tradeTransactionReportCount_ > 0)
                    {
                        for (int index = beginIndex_; index != endIndex_; index = (index + 1) % tradeTransactionReports_.Length)
                            tradeTransactionReports_[index] = null;

                        tradeTransactionReportCount_ = 0;
                        beginIndex_ = 0;
                        endIndex_ = 0;
                    }

                    if (exception_ != null)
                        throw exception_;

                    if (completed_)
                        return;
                }

                if (!event_.WaitOne(timeout))
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
                        tradeCapture_.UnsubscribeTriggerReportsAsync(null);
                    }
                    catch
                    {
                    }
                }

                if (tradeTransactionReportCount_ > 0)
                {
                    for (int index = beginIndex_; index != endIndex_; index = (index + 1) % tradeTransactionReports_.Length)
                        tradeTransactionReports_[index] = null;

                    tradeTransactionReportCount_ = 0;
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

        internal void SetBegin(int totalCount)
        {
            lock (mutex_)
            {
                if (!completed_)
                {
                    totalCount_ = totalCount;
                    started_ = true;

                    event_.Set();
                }
            }
        }

        internal void SetResult(ContingentOrderTriggerReport tradeTransactionReport)
        {
            lock (mutex_)
            {
                if (!completed_)
                {
                    if (tradeTransactionReportCount_ == tradeTransactionReports_.Length)
                    {
                        ContingentOrderTriggerReport[] tradeTransactionReports = new ContingentOrderTriggerReport[tradeTransactionReports_.Length + GrowSize];

                        if (endIndex_ > beginIndex_)
                        {
                            Array.Copy(tradeTransactionReports_, beginIndex_, tradeTransactionReports, 0, tradeTransactionReportCount_);
                        }
                        else
                        {
                            int count = tradeTransactionReports_.Length - beginIndex_;
                            Array.Copy(tradeTransactionReports_, beginIndex_, tradeTransactionReports, 0, count);
                            Array.Copy(tradeTransactionReports_, 0, tradeTransactionReports, count, endIndex_);
                        }

                        tradeTransactionReports_ = tradeTransactionReports;
                        beginIndex_ = 0;
                        endIndex_ = tradeTransactionReportCount_;
                    }

                    tradeTransactionReports_[endIndex_] = tradeTransactionReport;
                    endIndex_ = (endIndex_ + 1) % tradeTransactionReports_.Length;
                    ++tradeTransactionReportCount_;

                    event_.Set();
                }
            }
        }

        internal void SetEnd()
        {
            lock (mutex_)
            {
                if (!completed_)
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
                if (!completed_)
                {
                    exception_ = exception;
                    completed_ = true;

                    event_.Set();
                }
            }
        }

        const int GrowSize = 1000;

        TradeCapture tradeCapture_;
        int totalCount_;

        object mutex_;
        bool started_;
        bool completed_;

        ContingentOrderTriggerReport[] tradeTransactionReports_;
        int tradeTransactionReportCount_;
        int beginIndex_;
        int endIndex_;
        Exception exception_;
        AutoResetEvent event_;
    }
}