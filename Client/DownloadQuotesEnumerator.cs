using System;
using System.Collections.Generic;
using System.Threading;
using TickTrader.FDK.Common;

namespace TickTrader.FDK.Client
{
    public class DownloadQuotesEnumerator : IDisposable
    {
        internal DownloadQuotesEnumerator(QuoteStore quoteStore)
        {
            quoteStore_ = quoteStore;

            mutex_ = new object();
            started_ = false;
            completed_ = false;
            quotes_ = new Quote[GrowSize];
            quoteCount_ = 0;
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

        public Quote Next(int timeout)
        {
            while (true)
            {
                lock (mutex_)
                {
                    if (quoteCount_ > 0)
                    {
                        Quote quote = quotes_[beginIndex_];
                        quotes_[beginIndex_] = null;       // !
                        beginIndex_ = (beginIndex_ + 1) % quotes_.Length;
                        --quoteCount_;

                        return quote;
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
                    if (quoteCount_ > 0)
                    {
                        for (int index = beginIndex_; index != endIndex_; index = (index + 1) % quotes_.Length)
                            quotes_[index] = null;

                        quoteCount_ = 0;
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
                        quoteStore_.CancelDownloadQuotesAsync(null, downloadId_);
                    }
                    catch
                    {
                    }
                }

                if (quoteCount_ > 0)
                {
                    for (int index = beginIndex_; index != endIndex_; index = (index + 1) % quotes_.Length)
                        quotes_[index] = null;

                    quoteCount_ = 0;
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

        internal void SetBegin(string downloadId, DateTime availFrom, DateTime availTo)
        {
            lock (mutex_)
            {
                if (! completed_)
                {
                    downloadId_ = downloadId;
                    availFrom_ = availFrom;
                    availTo_ = availTo;
                    started_ = true;

                    event_.Set();
                }
            }
        }

        internal void SetResult(Quote quote)
        {
            lock (mutex_)
            {
                if (! completed_)
                {
                    if (quoteCount_ == quotes_.Length)
                    {
                        Quote[] quotes = new Quote[quotes_.Length + GrowSize];

                        if (endIndex_ > beginIndex_)
                        {
                            Array.Copy(quotes_, beginIndex_, quotes, 0, quoteCount_);
                        }
                        else
                        {
                            int count = quotes_.Length - beginIndex_;
                            Array.Copy(quotes_, beginIndex_, quotes, 0, count);
                            Array.Copy(quotes_, 0, quotes, count, endIndex_);
                        }

                        quotes_ = quotes;
                        beginIndex_ = 0;
                        endIndex_ = quoteCount_;
                    }

                    quotes_[endIndex_] = quote;
                    endIndex_ = (endIndex_ + 1) % quotes_.Length;
                    ++quoteCount_;

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

        QuoteStore quoteStore_;
        string downloadId_;
        DateTime availFrom_;
        DateTime availTo_;

        object mutex_;
        bool started_;
        bool completed_;

        Quote[] quotes_;
        int quoteCount_;
        int beginIndex_;
        int endIndex_;
        Exception exception_;
        AutoResetEvent event_;
    }
}