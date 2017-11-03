namespace TickTrader.FDK.Extended
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using TickTrader.FDK.Common;

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
        public PairBars(DataFeed datafeed, string symbol, BarPeriod period, DateTime startTime, DateTime endTime)
        {
            this.bids = new Bars(datafeed, symbol, PriceType.Bid, period, startTime, endTime);
            this.asks = new Bars(datafeed, symbol, PriceType.Ask, period, startTime, endTime);
            this.positive = DateTime.Compare(startTime, endTime) >= 0;
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
            this.bids = new Bars(datafeed, symbol, PriceType.Bid, period, startTime, endTime, timeout);
            this.asks = new Bars(datafeed, symbol, PriceType.Ask, period, startTime, endTime, timeout);
            this.positive = DateTime.Compare(startTime, endTime) >= 0;
        }

        /// <summary>
        /// The method returns bars enumerator.
        /// </summary>
        /// <returns>Can not be null.</returns>
        public IEnumerator<PairBar> GetEnumerator()
        {
            IEnumerator<Bar> bidBarsEnumerator = bids.GetEnumerator();
            IEnumerator<Bar> askBarsEnumerator = asks.GetEnumerator();

            return new PairBarsEnumerator(this, bidBarsEnumerator, askBarsEnumerator);
        }

        /// <summary>
        /// The method returns bars enumerator.
        /// </summary>
        /// <returns>Can not be null.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            IEnumerator<Bar> bidBarsEnumerator = bids.GetEnumerator();
            IEnumerator<Bar> askBarsEnumerator = asks.GetEnumerator();

            return new PairBarsEnumerator(this, bidBarsEnumerator, askBarsEnumerator);
        }

        internal IEnumerable<Bar> bids;
        internal IEnumerable<Bar> asks;
        internal bool positive;
    }
}
