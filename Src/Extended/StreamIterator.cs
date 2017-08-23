namespace TickTrader.FDK.Extended
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Contains common part of all streams.
    /// </summary>
    public class StreamIterator<T> : IDisposable
    {
        internal StreamIterator()
        {
        }

        #region Public Methods

        /// <summary>
        /// Returns total items in the iterator (0 if information is not available).
        /// </summary>
        public int TotalItems
        {
            get
            {
                throw new Exception("Not impled");
            }
        }

        /// <summary>
        /// Returns true, if the end of associated stream has been reached.
        /// </summary>
        public bool EndOfStream
        {
            get
            {
                throw new Exception("Not impled");
            }
        }

        /// <summary>
        /// Moves the iterator to the next stream element.
        /// </summary>
        public void Next()
        {
            throw new Exception("Not impled");
        }

        /// <summary>
        /// Moves the iterator to the next stream element.
        /// </summary>
        /// <param name="timeoutInMilliseconds">Timeout of the operation in milliseconds.</param>
        public void NextEx(int timeoutInMilliseconds)
        {
            throw new Exception("Not impled");
        }

        /// <summary>
        /// Gets the current stream element.
        /// </summary>
        public T Item
        {
            get
            {
                throw new Exception("Not impled");
            }
        }

        /// <summary>
        /// Reads an associated stream to the end and returns all elements as array.
        /// </summary>
        /// <returns>Can not be null.</returns>
        public T[] ToArray()
        {
            var list = new List<T>();
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
        }

        /// <summary>
        /// Release all unmanaged resources.
        /// </summary>
        ~StreamIterator()
        {
            if (!Environment.HasShutdownStarted)
            {
                this.Dispose();
            }
        }

        #endregion

        #region Members

        #endregion
    }
}
