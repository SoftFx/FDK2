using System;
using System.Collections.Generic;
using System.Threading;
using TickTrader.FDK.Common;

namespace TickTrader.FDK.OrderEntry
{
    public class GetOrdersEnumerator : IDisposable
    {
        internal GetOrdersEnumerator(Client client)
        {
            client_ = client;

            mutex_ = new object();
            started_ = false;
            completed_ = false;
            orders_ = new ExecutionReport[GrowSize];
            count_ = 0;
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

                if (! event_.WaitOne(timeout))
                    throw new Common.TimeoutException("Method call timed out");
            }
        }

        public ExecutionReport Next(int timeout)
        {
            while (true)
            {
                lock (mutex_)
                {
                    if (count_ > 0)
                    {
                        ExecutionReport order = orders_[beginIndex_];
                        orders_[beginIndex_] = null;       // !
                        beginIndex_ = (beginIndex_ + 1) % orders_.Length;
                        --count_;

                        return order;
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
                        for (int index = beginIndex_; index != endIndex_; index = (index + 1) % orders_.Length)
                            orders_[index] = null;

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
                if (! completed_)
                {
                    completed_ = true;

                    try
                    {
                        client_.CancelOrdersAsync(null, requestId_);
                    }
                    catch
                    {
                    }
                }

                if (count_ > 0)
                {
                    for (int index = beginIndex_; index != endIndex_; index = (index + 1) % orders_.Length)
                        orders_[index] = null;

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

        internal void SetBegin(string requestId, int totalCount)
        {
            lock (mutex_)
            {
                if (! completed_)
                {
                    requestId_ = requestId;
                    totalCount_ = totalCount;
                    started_ = true;

                    event_.Set();
                }
            }
        }

        internal void SetResult(ExecutionReport order)
        {
            lock (mutex_)
            {
                if (! completed_)
                {
                    if (count_ == orders_.Length)
                    {
                        ExecutionReport[] orders = new ExecutionReport[orders_.Length + GrowSize];

                        if (endIndex_ > beginIndex_)
                        {
                            Array.Copy(orders_, beginIndex_, orders, 0, count_);
                        }
                        else
                        {
                            int count = orders_.Length - beginIndex_;
                            Array.Copy(orders_, beginIndex_, orders, 0, count);
                            Array.Copy(orders_, 0, orders, count, endIndex_);
                        }

                        orders_ = orders;
                        beginIndex_ = 0;
                        endIndex_ = count_;
                    }

                    orders_[endIndex_] = order;
                    endIndex_ = (endIndex_ + 1) % orders_.Length;
                    ++ count_;

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
        string requestId_;
        int totalCount_;

        object mutex_;
        bool started_;
        bool completed_;

        ExecutionReport[] orders_;
        int count_;
        int beginIndex_;
        int endIndex_;
        Exception exception_;
        AutoResetEvent event_;
    }
}