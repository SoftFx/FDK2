namespace TickTrader.FDK.Extended
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using TickTrader.FDK.Common;
    using TickTrader.FDK.Client;

    /// <summary>
    /// Bars enumeration.
    /// </summary>
    public class Bars : IEnumerable<Bar>
    {
        /// <summary>
        /// Creates a new Bars stream instance.
        /// If startTime is less or equal than endTime then this is forward bars enumeration (from past to future), otherwise this is backward enumeration (from future to past).
        /// Anyway all bars should be in the following time range: Min(startTime, endTime) &lt;= Bar.From and Bar.To &lt;= Max(startTime, endTime)
        /// </summary>
        /// <param name="datafeed">DataFeed instance; can not be null.</param>
        /// <param name="symbol">A required symbol; can not be null.</param>
        /// <param name="priceType">A required price type: Bid or Ask.</param>
        /// <param name="period">Bar period instance; can not be null.</param>
        /// <param name="startTime">A start time of bars enumeration.</param>
        /// <param name="endTime">A end time of bars enumeration.</param>
        /// <exception cref="System.ArgumentNullException">If datafeed, period or symbol is null.</exception>
        public Bars(DataFeed datafeed, string symbol, PriceType priceType, BarPeriod period, DateTime startTime, DateTime endTime) :
            this(datafeed, symbol, priceType, period, startTime, endTime, datafeed.synchOperationTimeout_)
        {
        }

        /// <summary>
        /// Creates a new Bars stream instance.
        /// If startTime is less or equal than endTime then this is forward bars enumeration (from past to future), otherwise this is backward enumeration (from future to past).
        /// Anyway all bars should be in the following time range: Min(startTime, endTime) &lt;= Bar.From and Bar.To &lt;= Max(startTime, endTime)
        /// </summary>
        /// <param name="datafeed">DataFeed instance; can not be null.</param>
        /// <param name="symbol">A required symbol; can not be null.</param>
        /// <param name="priceType">A required price type: Bid or Ask.</param>
        /// <param name="period">Bar period instance; can not be null.</param>
        /// <param name="startTime">A start time of bars enumeration.</param>
        /// <param name="endTime">A end time of bars enumeration.</param>
        /// <exception cref="System.ArgumentNullException">If datafeed, period or symbol is null.</exception>
        public Bars(DataFeed datafeed, string symbol, PriceType priceType, BarPeriod period, DateTime startTime, DateTime endTime, int timeout)
        {
            if (datafeed == null)
                throw new ArgumentNullException(nameof(datafeed), "DataFeed instance can not be null.");

            if (symbol == null)
                throw new ArgumentNullException(nameof(symbol), "Symbol can not be null.");

            if (period == null)
                throw new ArgumentNullException(nameof(period), "Bar period instance can not be null.");

            datafeed_ = datafeed;
            symbol_ = symbol;
            priceType_ = priceType;
            period_ = period;
            startTime_ = startTime;
            endTime_ = endTime;
            timeout_ = timeout;
        }

        /// <summary>
        /// The method returns bars enumerator.
        /// </summary>
        /// <returns>Can not be null.</returns>
        public BarsEnumerator GetEnumerator()
        {
            DownloadBarsEnumerator barEnumerator = datafeed_.quoteStoreClient_.DownloadBars
            (
                symbol_, 
                priceType_, 
                period_, 
                startTime_, 
                endTime_, 
                timeout_
            );

            return new BarsEnumerator(this, barEnumerator);
        }

        /// <summary>
        /// The method returns bars enumerator.
        /// </summary>
        /// <returns>Can not be null.</returns>
        IEnumerator<Bar> IEnumerable<Bar>.GetEnumerator()
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
        internal PriceType priceType_;
        internal BarPeriod period_;
        internal DateTime startTime_;
        internal DateTime endTime_;
        internal int timeout_;
    }
}
