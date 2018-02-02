using System;
using System.Collections.Generic;
using System.Threading;
using TickTrader.FDK.Common;

namespace TickTrader.FDK.QuoteStore
{
    public class BarEnumerator : IDisposable
    {
        public BarEnumerator(Client client, string downloadId, DateTime availFrom, DateTime availTo)
        {
            client_ = client;
            downloadId_ = downloadId;
            availFrom_ = availFrom;
            availTo_ = availTo;

            mutex_ = new object();
            completed_ = false;
            bars_ = new Bar[GrowSize];
            barCount_ = 0;
            beginIndex_ = 0;
            endIndex_ = 0;
            exception_ = null;
            event_ = new AutoResetEvent(false);
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
            while (true)
            {
                lock (mutex_)
                {
                    if (barCount_ > 0)
                    {
                        Bar bar = bars_[beginIndex_];
                        bars_[beginIndex_] = null;       // !
                        beginIndex_ = (beginIndex_ + 1) % bars_.Length;
                        --barCount_;

                        return bar;
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

        public void Close()
        {
            lock (mutex_)
            {
                if (!completed_)
                {
                    completed_ = true;

                    try
                    {
                        client_.CancelDownloadBarsAsync(null, downloadId_);
                    }
                    catch
                    {
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

                event_.Close();
            }
        }

        public void Dispose()
        {
            Close();

            GC.SuppressFinalize(this);
        }

        public void SetResult(Bar bar)
        {
            lock (mutex_)
            {
                if (! completed_)
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

                    event_.Set();
                }
            }
        }

        public void SetEnd()
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

        public void SetError(Exception exception)
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
        string downloadId_;
        DateTime availFrom_;
        DateTime availTo_;

        object mutex_;
        bool completed_;

        Bar[] bars_;
        int barCount_;
        int beginIndex_;
        int endIndex_;
        Exception exception_;
        AutoResetEvent event_;
    }
}