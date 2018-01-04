using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TickTrader.FDK.Common;

namespace TickTrader.FDK.TradeCapture
{
    public class TradeTransactionReportEnumerator : IDisposable
    {
        internal TradeTransactionReportEnumerator(Client client)
        {
            client_ = client;

            mutex_ = new object();
            completed_ = false;
            taskCompletionSource_ = null;
            arrayTaskCompletionSource_ = null;
            tradeTransactionReports_ = new TradeTransactionReport[GrowSize];
            tradeTransactionReportCount_ = 0;
            beginIndex_ = 0;
            endIndex_ = 0;
            exception_ = null;
        }

        public TradeTransactionReport Next(int timeout)
        {
            return Client.ConvertToSync(NextAsync(), timeout);
        }

        public int Next(TradeTransactionReport[] tradeTransactionReports, int timeout)
        {
            return Client.ConvertToSync(NextAsync(tradeTransactionReports), timeout);
        }

        public Task<TradeTransactionReport> NextAsync()
        {
            lock (mutex_)
            {
                if (taskCompletionSource_ != null || arrayTaskCompletionSource_ != null)
                    throw new Exception("Invalid enumerator call");

                if (tradeTransactionReportCount_ > 0)
                {
                    TradeTransactionReport tradeTransactionReport = tradeTransactionReports_[beginIndex_];
                    tradeTransactionReports_[beginIndex_] = null;       // !
                    beginIndex_ = (beginIndex_ + 1) % tradeTransactionReports_.Length;
                    -- tradeTransactionReportCount_;

                    TaskCompletionSource<TradeTransactionReport> taskCompletionSource = new TaskCompletionSource<TradeTransactionReport>();
                    Task.Run(() => { taskCompletionSource.SetResult(tradeTransactionReport); });

                    return taskCompletionSource.Task;
                }

                if (exception_ != null)
                {
                    TaskCompletionSource<TradeTransactionReport> taskCompletionSource = new TaskCompletionSource<TradeTransactionReport>();
                    Exception exception = exception_;
                    Task.Run(() => { taskCompletionSource.SetException(exception); });

                    return taskCompletionSource.Task;
                }

                if (completed_)
                {
                    TaskCompletionSource<TradeTransactionReport> taskCompletionSource = new TaskCompletionSource<TradeTransactionReport>();
                    Task.Run(() => { taskCompletionSource.SetResult(null); });

                    return taskCompletionSource.Task;
                }

                taskCompletionSource_ = new TaskCompletionSource<TradeTransactionReport>();

                return taskCompletionSource_.Task;
            }            
        }

        public Task<int> NextAsync(TradeTransactionReport[] tradeTransactionReports)
        {
            lock (mutex_)
            {
                if (taskCompletionSource_ != null || arrayTaskCompletionSource_ != null)
                    throw new Exception("Invalid enumerator call");

                if (tradeTransactionReports.Length == 0)
                {
                    TaskCompletionSource<int> taskCompletionSource = new TaskCompletionSource<int>();
                    Task.Run(() => { taskCompletionSource.SetResult(0); });

                    return taskCompletionSource.Task;
                }

                if (tradeTransactionReportCount_ > 0)
                {
                    int count = 0;
                    for (int index = beginIndex_; index != endIndex_; index = (index + 1) % tradeTransactionReports_.Length)
                    {
                        tradeTransactionReports[count ++] = tradeTransactionReports_[index];
                        tradeTransactionReports_[index] = null;         // !

                        if (count == tradeTransactionReports.Length)
                            break;
                    }

                    beginIndex_ = (beginIndex_ + count) % tradeTransactionReports_.Length;
                    tradeTransactionReportCount_ -= count;

                    TaskCompletionSource<int> taskCompletionSource = new TaskCompletionSource<int>();
                    Task.Run(() => { taskCompletionSource.SetResult(count); });

                    return taskCompletionSource.Task;
                }

                if (exception_ != null)
                {
                    TaskCompletionSource<int> taskCompletionSource = new TaskCompletionSource<int>();
                    Exception exception = exception_;
                    Task.Run(() => { taskCompletionSource.SetException(exception); });

                    return taskCompletionSource.Task;
                }

                if (completed_)
                {
                    TaskCompletionSource<int> taskCompletionSource = new TaskCompletionSource<int>();
                    Task.Run(() => { taskCompletionSource.SetResult(0); });

                    return taskCompletionSource.Task;
                }

                arrayTaskCompletionSource_ = new TaskCompletionSource<int>();
                arrayTradeTransactionReports_ = tradeTransactionReports;
                arrayTradeTransactionReportCount_ = 0;

                return arrayTaskCompletionSource_.Task;
            }            
        }

        public void Close()
        {
            lock (mutex_)
            {
                if (! completed_)
                {
                    completed_ = true;

                    if (taskCompletionSource_ != null)
                    {
                        TaskCompletionSource<TradeTransactionReport> taskCompletionSource = taskCompletionSource_;                        
                        Task.Run(() => { taskCompletionSource.SetResult(null); });
                        taskCompletionSource_ = null;
                    }
                    else if (arrayTaskCompletionSource_ != null)
                    {
                        TaskCompletionSource<int> arrayTaskCompletionSource = arrayTaskCompletionSource_;
                        Task.Run(() => { arrayTaskCompletionSource.SetResult(0); });
                        arrayTaskCompletionSource_ = null;
                        arrayTradeTransactionReports_ = null;
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
            }
        }

        public void Dispose()
        {
            Close();

            GC.SuppressFinalize(this);
        }

        internal void SetResult(TradeTransactionReport tradeTransactionReport)
        {
            lock (mutex_)
            {
                if (! completed_)
                {
                    if (taskCompletionSource_ != null)
                    {
                        if (tradeTransactionReport != null)
                        {
                            TaskCompletionSource<TradeTransactionReport> taskCompletionSource = taskCompletionSource_;
                            Task.Run(() => { taskCompletionSource.SetResult(tradeTransactionReport); });
                            taskCompletionSource_ = null;
                        }
                    }
                    else if (arrayTaskCompletionSource_ != null)
                    {
                        if (tradeTransactionReport != null)
                        {
                            arrayTradeTransactionReports_[arrayTradeTransactionReportCount_++] = tradeTransactionReport;

                            if (arrayTradeTransactionReportCount_ == arrayTradeTransactionReports_.Length)
                            {
                                TaskCompletionSource<int> arrayTaskCompletionSource = arrayTaskCompletionSource_;
                                int arrayTradeTransactionReportCount = arrayTradeTransactionReportCount_;
                                Task.Run(() => { arrayTaskCompletionSource.SetResult(arrayTradeTransactionReportCount); });
                                arrayTaskCompletionSource_ = null;
                                arrayTradeTransactionReports_ = null;
                            }
                        }
                        else if (arrayTradeTransactionReportCount_ > 0)
                        {
                            TaskCompletionSource<int> arrayTaskCompletionSource = arrayTaskCompletionSource_;
                            int arrayTradeTransactionReportCount = arrayTradeTransactionReportCount_;
                            Task.Run(() => { arrayTaskCompletionSource.SetResult(arrayTradeTransactionReportCount); });
                            arrayTaskCompletionSource_ = null;
                            arrayTradeTransactionReports_ = null;
                        }
                    }
                    else
                    {
                        if (tradeTransactionReport != null)
                        {
                            if (tradeTransactionReportCount_ == tradeTransactionReports_.Length)
                            {
                                TradeTransactionReport[] tradeTransactionReports = new TradeTransactionReport[tradeTransactionReports_.Length + GrowSize];

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
                        }
                    }
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

                    if (taskCompletionSource_ != null)
                    {
                        TaskCompletionSource<TradeTransactionReport> taskCompletionSource = taskCompletionSource_;
                        Task.Run(() => { taskCompletionSource.SetResult(null); });
                        taskCompletionSource_ = null;
                    }
                    else if (arrayTaskCompletionSource_ != null)
                    {
                        TaskCompletionSource<int> arrayTaskCompletionSource = arrayTaskCompletionSource_;
                        Task.Run(() => { arrayTaskCompletionSource.SetResult(0); });
                        arrayTaskCompletionSource_ = null;
                        arrayTradeTransactionReports_ = null;
                    }
                }
            }
        }

        internal void SetError(Exception exception)
        {
            lock (mutex_)
            {
                if (! completed_)
                {
                    completed_ = true;
                    exception_ = exception;                        

                    if (taskCompletionSource_ != null)
                    {
                        TaskCompletionSource<TradeTransactionReport> taskCompletionSource = taskCompletionSource_;
                        Task.Run(() => { taskCompletionSource.SetException(exception); });
                        taskCompletionSource_ = null;
                    }                        
                    else if (arrayTaskCompletionSource_ != null)
                    {
                        TaskCompletionSource<int> arrayTaskCompletionSource = arrayTaskCompletionSource_;
                        Task.Run(() => { arrayTaskCompletionSource.SetException(exception); });
                        arrayTaskCompletionSource_ = null;
                        arrayTradeTransactionReports_ = null;
                    }
                }
            }
        }

        const int GrowSize = 1000;

        Client client_;

        internal object mutex_;
        internal bool completed_;

        TaskCompletionSource<TradeTransactionReport> taskCompletionSource_;
        TaskCompletionSource<int> arrayTaskCompletionSource_;
        TradeTransactionReport[] arrayTradeTransactionReports_;
        int arrayTradeTransactionReportCount_;

        TradeTransactionReport[] tradeTransactionReports_;
        int tradeTransactionReportCount_;
        int beginIndex_;
        int endIndex_;
        Exception exception_;
    }
}