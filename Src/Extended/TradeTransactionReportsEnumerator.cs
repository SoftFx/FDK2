namespace TickTrader.FDK.Extended
{
    using System;
    using System.Collections.Generic;
    using Common;
    using TradeCapture;

    // TODO: support IEnumerator<> and IEnumerator reset semantics ?
    public class TradeTransactionReportsEnumerator : IDisposable
    {
        internal TradeTransactionReportsEnumerator(TradeTransactionReportEnumerator tradeTransactionReportEnumerator, int timeout)
        {
            tradeTransactionReportEnumerator_ = tradeTransactionReportEnumerator;
            timeout_ = timeout;
            tradeTransactionReport_ = tradeTransactionReportEnumerator_.Next(timeout_);
        }

        #region Public Methods

        /// <summary>
        /// Returns total items in the iterator (0 if information is not available).
        /// </summary>
        public int TotalItems
        {
            get
            {
                return 0;
            }
        }

        /// <summary>
        /// Returns true, if the end of associated stream has been reached.
        /// </summary>
        public bool EndOfStream
        {
            get
            {
                return tradeTransactionReport_ == null;
            }
        }

        /// <summary>
        /// Moves the iterator to the next stream element.
        /// </summary>
        public void Next()
        {
            NextEx(timeout_);
        }

        /// <summary>
        /// Moves the iterator to the next stream element.
        /// </summary>
        /// <param name="timeoutInMilliseconds">Timeout of the operation in milliseconds.</param>
        public void NextEx(int timeoutInMilliseconds)
        {
            tradeTransactionReport_ = tradeTransactionReportEnumerator_.Next(timeoutInMilliseconds);
        }

        /// <summary>
        /// Gets the current stream element.
        /// </summary>
        public TradeTransactionReport Item
        {
            get
            {
                return tradeTransactionReport_;
            }
        }

        /// <summary>
        /// Reads an associated stream to the end and returns all elements as array.
        /// </summary>
        /// <returns>Can not be null.</returns>
        public TradeTransactionReport[] ToArray()
        {
            var list = new List<TradeTransactionReport>();
            for (; !this.EndOfStream; this.Next())
            {
                var item = this.Item;
                list.Add(item);
            }

            return list.ToArray();
        }

        /// <summary>
        /// Release all unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            tradeTransactionReportEnumerator_.Dispose();

            GC.SuppressFinalize(this);
        }

        #endregion

        #region Members
        
        TradeTransactionReportEnumerator tradeTransactionReportEnumerator_;
        int timeout_;
        TradeTransactionReport tradeTransactionReport_;

        #endregion
    }
}
