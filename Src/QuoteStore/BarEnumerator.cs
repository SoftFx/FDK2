using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TickTrader.FDK.Common;

namespace TickTrader.FDK.QuoteStore
{
    public class BarEnumerator : IDisposable
    {
        internal BarEnumerator(Client client, string downloadId)
        {
            client_ = client;
            downloadId_ = downloadId;

            mutex_ = new object();
            completed_ = false;
            taskCompletionSource_ = null;
            bar_ = new Bar();
            event_ = new AutoResetEvent(false);
        }

        public string DownloadId
        {
            get { return downloadId_;  }
        }

        public Bar Next(int timeout)
        {
            return Client.ConvertToSync(NextAsync(), timeout);
        }

        public Task<Bar> NextAsync()
        {
            lock (mutex_)
            {
                if (completed_)
                    throw new Exception(string.Format("Enumerator completed : {0}", downloadId_));

                if (taskCompletionSource_ != null)
                    throw new Exception("Invalid enumerator call");

                taskCompletionSource_ = new TaskCompletionSource<Bar>();
                event_.Set();

                return taskCompletionSource_.Task;
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
                        Exception exception = new Exception(string.Format("Enumerator closed : {0}", downloadId_));

                        taskCompletionSource_.SetException(exception);
                        taskCompletionSource_ = null;
                    }

                    try
                    {
                        client_.SendDownloadCancel(downloadId_);
                    }
                    catch
                    {
                    }
                }

                if (event_ != null)
                {
                    event_.Set();
                    event_.Close();
                    event_ = null;
                }
            }
        }

        public void Dispose()
        {
            Close();

            GC.SuppressFinalize(this);
        }

        Client client_;
        string downloadId_;

        internal object mutex_;
        internal bool completed_;
        internal TaskCompletionSource<Bar> taskCompletionSource_;
        internal Bar bar_;
        internal AutoResetEvent event_;
    }
}