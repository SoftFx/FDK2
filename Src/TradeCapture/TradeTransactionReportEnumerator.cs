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
            tradeTransactionReport_ = new TradeTransactionReport();
            event_ = new AutoResetEvent(false);
        }

        public TradeTransactionReport Next(int timeout)
        {
            return Client.ConvertToSync(NextAsync(), timeout);
        }

        public Task<TradeTransactionReport> NextAsync()
        {
            lock (mutex_)
            {
                if (completed_)
                    throw new Exception("Enumerator completed");

                if (taskCompletionSource_ != null)
                    throw new Exception("Invalid enumerator call");

                taskCompletionSource_ = new TaskCompletionSource<TradeTransactionReport>();
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
                        Exception exception = new Exception("Enumerator closed");

                        taskCompletionSource_.SetException(exception);
                        taskCompletionSource_ = null;
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

        internal object mutex_;
        internal bool completed_;
        internal TaskCompletionSource<TradeTransactionReport> taskCompletionSource_;
        internal TradeTransactionReport tradeTransactionReport_;
        internal AutoResetEvent event_;
    }
}