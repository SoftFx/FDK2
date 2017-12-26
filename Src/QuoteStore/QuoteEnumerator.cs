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
            arrayTaskCompletionSource_ = null;
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

        public int Next(Quote[] quotes, int timeout)
        {
            return Client.ConvertToSync(NextAsync(quotes), timeout);
        }

        public Task<Quote> NextAsync()
        {
            lock (mutex_)
            {
                if (taskCompletionSource_ != null || arrayTaskCompletionSource_ != null)
                    throw new Exception("Invalid enumerator call");

                if (quoteCount_ > 0)
                {
                    Quote quote = quotes_[beginIndex_];
                    quotes_[beginIndex_] = null;       // !
                    beginIndex_ = (beginIndex_ + 1) % quotes_.Length;
                    -- quoteCount_;

                    TaskCompletionSource<Quote> taskCompletionSource = new TaskCompletionSource<Quote>();
                    Task.Run(() => { taskCompletionSource.SetResult(quote); });

                    return taskCompletionSource.Task;
                }

                if (exception_ != null)
                {
                    TaskCompletionSource<Quote> taskCompletionSource = new TaskCompletionSource<Quote>();
                    Task.Run(() => { taskCompletionSource.SetException(exception_); });

                    return taskCompletionSource.Task;
                }

                if (completed_)
                {
                    TaskCompletionSource<Quote> taskCompletionSource = new TaskCompletionSource<Quote>();
                    Task.Run(() => { taskCompletionSource.SetResult(null); });

                    return taskCompletionSource.Task;
                }

                taskCompletionSource_ = new TaskCompletionSource<Quote>();

                return taskCompletionSource_.Task;
            }            
        }

        public Task<int> NextAsync(Quote[] quotes)
        {
            lock (mutex_)
            {
                if (taskCompletionSource_ != null || arrayTaskCompletionSource_ != null)
                    throw new Exception("Invalid enumerator call");

                if (quotes.Length == 0)
                {
                    TaskCompletionSource<int> taskCompletionSource = new TaskCompletionSource<int>();
                    Task.Run(() => { taskCompletionSource.SetResult(0); });

                    return taskCompletionSource.Task;
                }

                if (quoteCount_ > 0)
                {
                    int count = 0;
                    for (int index = beginIndex_; index != endIndex_; index = (index + 1) % quotes.Length)
                    {
                        quotes[count ++] = quotes_[index];
                        quotes_[index] = null;         // !

                        if (count == quotes.Length)
                            break;
                    }

                    beginIndex_ = (beginIndex_ + count) % quotes_.Length;
                    quoteCount_ -= count;

                    TaskCompletionSource<int> taskCompletionSource = new TaskCompletionSource<int>();
                    Task.Run(() => { taskCompletionSource.SetResult(count); });

                    return taskCompletionSource.Task;
                }

                if (exception_ != null)
                {
                    TaskCompletionSource<int> taskCompletionSource = new TaskCompletionSource<int>();
                    Task.Run(() => { taskCompletionSource.SetException(exception_); });

                    return taskCompletionSource.Task;
                }

                if (completed_)
                {
                    TaskCompletionSource<int> taskCompletionSource = new TaskCompletionSource<int>();
                    Task.Run(() => { taskCompletionSource.SetResult(0); });

                    return taskCompletionSource.Task;
                }

                arrayTaskCompletionSource_ = new TaskCompletionSource<int>();
                arrayQuotes_ = quotes;
                arrayQuoteCount_ = 0;

                return arrayTaskCompletionSource_.Task;
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
                        TaskCompletionSource<Quote> taskCompletionSource = taskCompletionSource_;
                        Task.Run(() => { taskCompletionSource.SetResult(null); });
                        taskCompletionSource_ = null;
                    }
                    else if (arrayTaskCompletionSource_ != null)
                    {
                        TaskCompletionSource<int> arrayTaskCompletionSource = arrayTaskCompletionSource_;
                        Task.Run(() => { arrayTaskCompletionSource.SetResult(0); });
                        arrayTaskCompletionSource_ = null;
                        arrayQuotes_ = null;
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
                        if (quote != null)
                        {
                            TaskCompletionSource<Quote> taskCompletionSource = taskCompletionSource_;
                            Task.Run(() => { taskCompletionSource.SetResult(quote); });
                            taskCompletionSource_ = null;
                        }
                    }
                    else if (arrayTaskCompletionSource_ != null)
                    {
                        if (quote != null)
                        {
                            arrayQuotes_[arrayQuoteCount_++] = quote;

                            if (arrayQuoteCount_ == arrayQuotes_.Length)
                            {
                                TaskCompletionSource<int> arrayTaskCompletionSource = arrayTaskCompletionSource_;
                                Task.Run(() => { arrayTaskCompletionSource.SetResult(arrayQuoteCount_); });
                                arrayTaskCompletionSource_ = null;
                                arrayQuotes_ = null;
                            }
                        }
                        else if (arrayQuoteCount_ > 0)
                        {
                            TaskCompletionSource<int> arrayTaskCompletionSource = arrayTaskCompletionSource_;
                            Task.Run(() => { arrayTaskCompletionSource.SetResult(arrayQuoteCount_); });
                            arrayTaskCompletionSource_ = null;
                            arrayQuotes_ = null;
                        }
                    }
                    else
                    {
                        if (quote != null)
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
                        TaskCompletionSource<Quote> taskCompletionSource = taskCompletionSource_;
                        Task.Run(() => { taskCompletionSource.SetResult(null); });
                        taskCompletionSource_ = null;
                    }
                    else if (arrayTaskCompletionSource_ != null)
                    {
                        TaskCompletionSource<int> arrayTaskCompletionSource = arrayTaskCompletionSource_;
                        Task.Run(() => { arrayTaskCompletionSource.SetResult(0); });
                        arrayTaskCompletionSource_ = null;
                        arrayQuotes_ = null;
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
                        TaskCompletionSource<Quote> taskCompletionSource = taskCompletionSource_;
                        Task.Run(() => { taskCompletionSource.SetException(exception); });
                        taskCompletionSource_ = null;
                    }                        
                    else if (arrayTaskCompletionSource_ != null)
                    {
                        TaskCompletionSource<int> arrayTaskCompletionSource = arrayTaskCompletionSource_;
                        Task.Run(() => { arrayTaskCompletionSource.SetException(exception); });
                        arrayTaskCompletionSource_ = null;
                        arrayQuotes_ = null;
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
        TaskCompletionSource<int> arrayTaskCompletionSource_;
        Quote[] arrayQuotes_;
        int arrayQuoteCount_;

        Quote[] quotes_;
        int quoteCount_;
        int beginIndex_;
        int endIndex_;
        Exception exception_;
    }
}