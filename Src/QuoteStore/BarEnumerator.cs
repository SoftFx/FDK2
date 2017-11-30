using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TickTrader.FDK.Common;

namespace TickTrader.FDK.QuoteStore
{
    public class BarEnumerator : IDisposable
    {
        internal BarEnumerator(Client client, string downloadId, DateTime availFrom, DateTime availTo)
        {
            client_ = client;
            downloadId_ = downloadId;
            availFrom_ = availFrom;
            availTo_ = availTo;

            mutex_ = new object();
            completed_ = false;
            taskCompletionSource_ = null;
            arrayTaskCompletionSource_ = null;
            bars_ = new Bar[GrowSize];
            barCount_ = 0;
            beginIndex_ = 0;
            endIndex_ = 0;
            exception_ = null;
        }

        public string DownloadId
        {
            get { return downloadId_;  }
        }

        public DateTime AvailFrom
        {
            get { return availFrom_; }
        }

        public DateTime AvailTo
        {
            get { return availTo_; }
        }

        public Bar Next(int timeout)
        {
            return Client.ConvertToSync(NextAsync(), timeout);
        }

        public int Next(Bar[] bars, int timeout)
        {
            return Client.ConvertToSync(NextAsync(bars), timeout);
        }

        public Task<Bar> NextAsync()
        {
            lock (mutex_)
            {
                if (taskCompletionSource_ != null || arrayTaskCompletionSource_ != null)
                    throw new Exception("Invalid enumerator call");

                if (barCount_ > 0)
                {
                    Bar bar = bars_[beginIndex_];
                    bars_[beginIndex_] = null;       // !
                    beginIndex_ = (beginIndex_ + 1) % bars_.Length;
                    -- barCount_;

                    TaskCompletionSource<Bar> taskCompletionSource = new TaskCompletionSource<Bar>();
                    taskCompletionSource.SetResult(bar);

                    return taskCompletionSource.Task;
                }

                if (exception_ != null)
                {
                    TaskCompletionSource<Bar> taskCompletionSource = new TaskCompletionSource<Bar>();
                    taskCompletionSource.SetException(exception_);

                    return taskCompletionSource.Task;
                }

                if (completed_)
                {
                    TaskCompletionSource<Bar> taskCompletionSource = new TaskCompletionSource<Bar>();
                    taskCompletionSource.SetResult(null);

                    return taskCompletionSource.Task;
                }

                taskCompletionSource_ = new TaskCompletionSource<Bar>();

                return taskCompletionSource_.Task;
            }            
        }

        public Task<int> NextAsync(Bar[] bars)
        {
            lock (mutex_)
            {
                if (taskCompletionSource_ != null || arrayTaskCompletionSource_ != null)
                    throw new Exception("Invalid enumerator call");

                if (bars.Length == 0)
                {
                    TaskCompletionSource<int> taskCompletionSource = new TaskCompletionSource<int>();
                    taskCompletionSource.SetResult(0);

                    return taskCompletionSource.Task;
                }

                if (barCount_ > 0)
                {
                    int count = 0;
                    for (int index = beginIndex_; index != endIndex_; index = (index + 1 ) % bars_.Length)
                    {
                        bars[count ++] = bars_[index];
                        bars_[index] = null;         // !                        

                        if (count == bars.Length)
                            break;
                    }

                    beginIndex_ = (beginIndex_ + count) % bars_.Length;
                    barCount_ -= count;

                    TaskCompletionSource<int> taskCompletionSource = new TaskCompletionSource<int>();
                    taskCompletionSource.SetResult(count);

                    return taskCompletionSource.Task;
                }

                if (exception_ != null)
                {
                    TaskCompletionSource<int> taskCompletionSource = new TaskCompletionSource<int>();
                    taskCompletionSource.SetException(exception_);

                    return taskCompletionSource.Task;
                }

                if (completed_)
                {
                    TaskCompletionSource<int> taskCompletionSource = new TaskCompletionSource<int>();
                    taskCompletionSource.SetResult(0);

                    return taskCompletionSource.Task;
                }

                arrayTaskCompletionSource_ = new TaskCompletionSource<int>();
                arrayBars_ = bars;
                arrayBarCount_ = 0;

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

                    try
                    {
                        client_.SendDownloadCancel(downloadId_);
                    }
                    catch
                    {
                    }

                    if (taskCompletionSource_ != null)
                    {
                        taskCompletionSource_.SetResult(null);
                        taskCompletionSource_ = null;
                    }
                    else if (arrayTaskCompletionSource_ != null)
                    {
                        arrayTaskCompletionSource_.SetResult(0);
                        arrayTaskCompletionSource_ = null;
                        arrayBars_ = null;
                    }
                }

                if (barCount_ > 0)
                {
                    for (int index = beginIndex_; index != endIndex_; index = (index + 1) % bars_.Length)
                        bars_[index] = null;

                    barCount_ = 0;
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

        internal void SetResult(Bar bar)
        {
            lock (mutex_)
            {
                if (! completed_)
                {
                    if (taskCompletionSource_ != null)
                    {
                        if (bar != null)
                        {
                            taskCompletionSource_.SetResult(bar);
                            taskCompletionSource_ = null;
                        }
                    }
                    else if (arrayTaskCompletionSource_ != null)
                    {
                        if (bar != null)
                        {
                            arrayBars_[arrayBarCount_++] = bar;

                            if (arrayBarCount_ == arrayBars_.Length)
                            {
                                arrayTaskCompletionSource_.SetResult(arrayBarCount_);
                                arrayTaskCompletionSource_ = null;
                                arrayBars_ = null;
                            }
                        }
                        else if (arrayBarCount_ > 0)
                        {
                            arrayTaskCompletionSource_.SetResult(arrayBarCount_);
                            arrayTaskCompletionSource_ = null;
                            arrayBars_ = null;
                        }
                    }
                    else
                    {
                        if (bar != null)
                        {
                            if (barCount_ == bars_.Length)
                            {
                                Bar[] bars = new Bar[bars_.Length + GrowSize];

                                if (endIndex_ > beginIndex_)
                                {
                                    Array.Copy(bars_, beginIndex_, bars, 0, barCount_);
                                }
                                else
                                {
                                    int count = bars_.Length - beginIndex_;
                                    Array.Copy(bars_, beginIndex_, bars, 0, count);
                                    Array.Copy(bars_, 0, bars, count, endIndex_);
                                }

                                bars_ = bars;
                                beginIndex_ = 0;
                                endIndex_ = barCount_;
                            }

                            bars_[endIndex_] = bar;
                            endIndex_ = (endIndex_ + 1) % bars_.Length;
                            ++barCount_;
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
                        taskCompletionSource_.SetResult(null);
                        taskCompletionSource_ = null;
                    }
                    else if (arrayTaskCompletionSource_ != null)
                    {
                        arrayTaskCompletionSource_.SetResult(0);
                        arrayTaskCompletionSource_ = null;
                        arrayBars_ = null;
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
                        taskCompletionSource_.SetException(exception);
                        taskCompletionSource_ = null;
                    }
                    else if (arrayTaskCompletionSource_ != null)
                    {
                        arrayTaskCompletionSource_.SetException(exception);
                        arrayTaskCompletionSource_ = null;
                        arrayBars_ = null;
                    }
                }
            }
        }
        
        const int GrowSize = 1000;

        Client client_;
        string downloadId_;
        DateTime availFrom_;
        DateTime availTo_;

        object mutex_;
        bool completed_;

        TaskCompletionSource<Bar> taskCompletionSource_;
        TaskCompletionSource<int> arrayTaskCompletionSource_;
        Bar[] arrayBars_;
        int arrayBarCount_;

        Bar[] bars_;
        int barCount_;
        int beginIndex_;
        int endIndex_;
        Exception exception_;
    }
}