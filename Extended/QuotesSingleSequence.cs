namespace TickTrader.FDK.Extended
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using TickTrader.FDK.Common;
    using TickTrader.FDK.Client;

    /// <summary>
    /// The sequence enumerates quotes for a specified time interval.
    /// If startTime is less than endTime this is enumeration from past to future.
    /// If startTime is more than endTime this is enumeration from future to past.
    /// </summary>
    public class QuotesSingleSequence : IEnumerable<Quote>
    {
        #region Construction

        /// <summary>
        /// Creates a new single quotes sequence.
        /// </summary>
        /// <param name="storage">specifies storage, which will be used for quotes requesting.</param>
        /// <param name="symbol">specifies symbol of quotes enumeration.</param>
        /// <param name="startTime">specifies start time of quotes enumeration.</param>
        /// <param name="endTime">specifies finish time of quotes enumeration.</param>
        /// <param name="depth">specifies required depth of enumerating quotes.</param>
        /// <exception cref="System.ArgumentNullException">If storage or symbol are null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">If depth is negative or zero.</exception>
        public QuotesSingleSequence(DataFeed dataFeed, string symbol, DateTime startTime, DateTime endTime, int depth) :
            this(dataFeed, symbol, startTime, endTime, depth, dataFeed.synchOperationTimeout_)
        {
        }

        /// <summary>
        /// Creates a new single quotes sequence.
        /// </summary>
        /// <param name="storage">specifies storage, which will be used for quotes requesting.</param>
        /// <param name="symbol">specifies symbol of quotes enumeration.</param>
        /// <param name="startTime">specifies start time of quotes enumeration.</param>
        /// <param name="endTime">specifies finish time of quotes enumeration.</param>
        /// <param name="depth">specifies required depth of enumerating quotes.</param>
        /// <exception cref="System.ArgumentNullException">If storage or symbol are null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">If depth is negative or zero.</exception>
        public QuotesSingleSequence(DataFeed dataFeed, string symbol, DateTime startTime, DateTime endTime, int depth, int timeout)
        {
            if (dataFeed == null)
                throw new ArgumentNullException(nameof(dataFeed), "Data feed can not be null.");

            if (symbol == null)
                throw new ArgumentNullException(nameof(symbol), "Symbol can not be null.");

            if (depth <= 0)
                throw new ArgumentOutOfRangeException(nameof(depth), depth, "Expected positive depth value.");

            this.DataFeed = dataFeed;
            this.Symbol = symbol;
            this.StartTime = startTime;
            this.EndTime = endTime;
            this.Depth = depth;
            this.Timeout = timeout;
        }

        #endregion

        #region Parameters Properties

        /// <summary>
        /// Gets used data feed.
        /// </summary>
        public DataFeed DataFeed { get; private set; }

        /// <summary>
        /// Gets used symbol.
        /// </summary>
        public string Symbol { get; private set; }

        /// <summary>
        /// Gets used start time.
        /// </summary>
        public DateTime StartTime { get; private set; }

        /// <summary>
        /// Gets used end time.
        /// </summary>
        public DateTime EndTime { get; private set; }

        /// <summary>
        /// Gets used level2 depth.
        /// </summary>
        public int Depth { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public int Timeout { get; private set; }

		#endregion

		/// <summary>
		/// Creates enumerator for the sequence.
		/// </summary>
		/// <returns>a new enumerator instance.</returns>
		public QuotesSingleSequenceEnumerator GetEnumerator()
		{
            DownloadQuotesEnumerator quoteEnumerator = DataFeed.quoteStoreClient_.DownloadQuotes
            (
                Symbol,
                Depth == 1 ? QuoteDepth.Top : QuoteDepth.Level2,
                StartTime,
                EndTime,
                Timeout
            );

            return new QuotesSingleSequenceEnumerator(this, quoteEnumerator);
		}

		#region IEnumerable Interface Implementation

		/// <summary>
		/// Creates enumerator for the sequence.
		/// </summary>
		/// <returns>a new enumerator instance.</returns>
		IEnumerator<Quote> IEnumerable<Quote>.GetEnumerator()
		{
            DownloadQuotesEnumerator quoteEnumerator = DataFeed.quoteStoreClient_.DownloadQuotes
            (
                Symbol,
                Depth == 1 ? QuoteDepth.Top : QuoteDepth.Level2,
                StartTime,
                EndTime,
                Timeout
            );

            return new QuotesSingleSequenceEnumerator(this, quoteEnumerator);
		}

		/// <summary>
		/// Creates enumerator for the sequence.
		/// </summary>
		/// <returns>a new enumerator instance.</returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
            DownloadQuotesEnumerator quoteEnumerator = DataFeed.quoteStoreClient_.DownloadQuotes
            (
                Symbol,
                Depth == 1 ? QuoteDepth.Top : QuoteDepth.Level2,
                StartTime,
                EndTime,
                Timeout
            );

            return new QuotesSingleSequenceEnumerator(this, quoteEnumerator);
		}

		#endregion
	}
}
