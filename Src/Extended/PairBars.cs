namespace TickTrader.FDK.Extended
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;
    using System.Runtime.ExceptionServices;
    using TickTrader.FDK.Common;
    using TickTrader.FDK.QuoteStore;

    /// <summary>
    /// Bars enumeration.
    /// </summary>
    public class PairBars : IEnumerable<PairBar>
    {
        /// <summary>
        /// Creates a new PairBars stream instance.
        /// If startTime is less or equal than endTime then this is forward bars enumeration (from past to future), otherwise this is backward enumeration (from future to past).
        /// Anyway all bars should be in the following time range: Min(startTime, endTime) &lt;= Bar.From and Bar.To &lt;= Max(startTime, endTime)
        /// </summary>
        /// <param name="datafeed">Datafeed instance; can not be null.</param>
        /// <param name="symbol">A required symbol; can not be null.</param>
        /// <param name="period">Bar period instance; can not be null.</param>
        /// <param name="startTime">A start time of bars enumeration.</param>
        /// <param name="endTime">A end time of bars enumeration.</param>
        /// <exception cref="System.ArgumentNullException">If datafeed, period or symbol is null.</exception>
        public PairBars(DataFeed datafeed, string symbol, BarPeriod period, DateTime startTime, DateTime endTime) :
            this(datafeed, symbol, period, startTime, endTime, datafeed.synchOperationTimeout_)
        {
        }

        /// <summary>
        /// Creates a new PairBars stream instance.
        /// If startTime is less or equal than endTime then this is forward bars enumeration (from past to future), otherwise this is backward enumeration (from future to past).
        /// Anyway all bars should be in the following time range: Min(startTime, endTime) &lt;= Bar.From and Bar.To &lt;= Max(startTime, endTime)
        /// </summary>
        /// <param name="datafeed">Datafeed instance; can not be null.</param>
        /// <param name="symbol">A required symbol; can not be null.</param>
        /// <param name="period">Bar period instance; can not be null.</param>
        /// <param name="startTime">A start time of bars enumeration.</param>
        /// <param name="endTime">A end time of bars enumeration.</param>
        /// <exception cref="System.ArgumentNullException">If datafeed, period or symbol is null.</exception>
        public PairBars(DataFeed datafeed, string symbol, BarPeriod period, DateTime startTime, DateTime endTime, int timeout)
        {
            if (datafeed == null)
                throw new ArgumentNullException(nameof(datafeed), "DataFeed instance can not be null.");

            if (symbol == null)
                throw new ArgumentNullException(nameof(symbol), "Symbol can not be null.");

            if (period == null)
                throw new ArgumentNullException(nameof(period), "Bar period instance can not be null.");

            datafeed_ = datafeed;
            symbol_ = symbol;
            period_ = period;
            startTime_ = startTime;
            endTime_ = endTime;
            timeout_ = timeout;
            positive = DateTime.Compare(startTime, endTime) >= 0;
        }

        /// <summary>
        /// The method returns bars enumerator.
        /// </summary>
        /// <returns></returns>
        public PairBarsEnumerator GetEnumerator()
        {
            DataFeed.PairBarDownloadContext pairBarDownloadContext = new DataFeed.PairBarDownloadContext();
            pairBarDownloadContext.pairBars_ = this;
            pairBarDownloadContext.bidBarEnumerator_ = null;
            pairBarDownloadContext.askBarEnumerator_ = null;
            pairBarDownloadContext.exception_ = null;
            pairBarDownloadContext.pairBarsEnumerator_ = null;
            pairBarDownloadContext.event_ = new AutoResetEvent(false);

            DataFeed.BarDownloadContext bidBarDownloadContext = new DataFeed.BarDownloadContext();
            bidBarDownloadContext.priceType_ = PriceType.Bid;
            bidBarDownloadContext.pairContext_ = pairBarDownloadContext;

            DataFeed.BarDownloadContext askBarDownloadContext = new DataFeed.BarDownloadContext();
            askBarDownloadContext.priceType_ = PriceType.Ask;
            askBarDownloadContext.pairContext_ = pairBarDownloadContext;

            datafeed_.quoteStoreClient_.DownloadBarsAsync
            (
                bidBarDownloadContext,
                symbol_,
                PriceType.Bid,
                period_,
                startTime_,
                endTime_
            );

            datafeed_.quoteStoreClient_.DownloadBarsAsync
            (
                askBarDownloadContext,
                symbol_,
                PriceType.Ask,
                period_,
                startTime_,
                endTime_
            );

            if (!pairBarDownloadContext.event_.WaitOne(timeout_))
                throw new Common.TimeoutException("Method call timed out");

            if (pairBarDownloadContext.exception_ != null)
                throw pairBarDownloadContext.exception_;

            return pairBarDownloadContext.pairBarsEnumerator_;
        }

        /// <summary>
        /// The method returns bars enumerator.
        /// </summary>
        /// <returns>Can not be null.</returns>
        IEnumerator<PairBar> IEnumerable<PairBar>.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// The method returns bars enumerator.
        /// </summary>
        /// <returns>Can not be null.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal DataFeed datafeed_;
        internal string symbol_;
        internal BarPeriod period_;
        internal DateTime startTime_;
        internal DateTime endTime_;
        internal int timeout_;
        internal bool positive;
    }
}
