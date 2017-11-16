using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TickTrader.FDK.Common;

namespace TickTrader.FDK.QuoteStore
{
    public class QuoteEnumerator : IDisposable
    {
        internal QuoteEnumerator(Client client, string downloadId, DateTime availFrom, DateTime availTo)
        {
            client_ = client;
            downloadId_ = downloadId;
            availFrom_ = availFrom;
            availTo_ = availTo;

            mutex_ = new object();
            completed_ = false;
            taskCompletionSource_ = null;
            quotes_ = new Quote[GrowSize];
            quoteCount_ = 0;
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

        public Quote Next(int timeout)
        {
            return Client.ConvertToSync(NextAsync(), timeout);
        }

        public Task<Quote> NextAsync()
        {
            lock (mutex_)
            {
                if (taskCompletionSource_ != null)
                    throw new Exception("Invalid enumerator call");

                if (quoteCount_ > 0)
                {
                    Quote quote = quotes_[beginIndex_];
                    quotes_[beginIndex_] = null;       // !
                    beginIndex_ = (beginIndex_ + 1) % quotes_.Length;
                    -- quoteCount_;

                    TaskCompletionSource<Quote> taskCompletionSource = new TaskCompletionSource<Quote>();
                    taskCompletionSource.SetResult(quote);

                    return taskCompletionSource.Task;
                }

                if (exception_ != null)
                {
                    TaskCompletionSource<Quote> taskCompletionSource = new TaskCompletionSource<Quote>();
                    taskCompletionSource.SetException(exception_);

                    return taskCompletionSource.Task;
                }

                if (completed_)
                {
                    TaskCompletionSource<Quote> taskCompletionSource = new TaskCompletionSource<Quote>();
                    taskCompletionSource.SetResult(null);

                    return taskCompletionSource.Task;
                }

                taskCompletionSource_ = new TaskCompletionSource<Quote>();

                return taskCompletionSource_.Task;
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
                }

                for (int index = beginIndex_; index != endIndex_; ++ index)
                    quotes_[index % quotes_.Length] = null;

                quoteCount_ = 0;
                beginIndex_ = 0;
                endIndex_ = 0;
            }
        }

        public void Dispose()
        {
            Close();

            GC.SuppressFinalize(this);
        }

        internal void SetResult(Quote quote)
        {
            lock (mutex_)
            {
                if (! completed_)
                {
                    if (taskCompletionSource_ != null)
                    {
                        taskCompletionSource_.SetResult(quote);
                        taskCompletionSource_ = null;
                    }
                    else
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
                        ++ quoteCount_;
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
        TaskCompletionSource<Quote> taskCompletionSource_;
        Quote[] quotes_;
        int quoteCount_;
        int beginIndex_;
        int endIndex_;
        Exception exception_;
    }
}