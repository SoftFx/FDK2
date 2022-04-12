namespace TickTrader.FDK.Calculator
{
    using System;
    using TickTrader.FDK.Common;

    /// <summary>
    /// Represents bid/ask prices entry.
    /// </summary>
    public struct PriceEntry
    {
        #region Fields

        /// <summary>
        /// The best price of bids.
        /// </summary>
        public decimal? Bid { get; private set; }

        /// <summary>
        /// The best price of asks.
        /// </summary>
        public decimal? Ask { get; private set; }

        /// <summary>
        /// Tick type info
        /// </summary>
        public TickTypes TickType { get; private set; }

        #endregion

        #region Construction

        /// <summary>
        /// Creates a new price entry instance.
        /// </summary>
        /// <param name="bid">a price for bid</param>
        /// <param name="ask">a price for ask</param>
        /// <param name="tickType">tick type info</param>
        internal PriceEntry(decimal? bid, decimal? ask, TickTypes tickType)
            : this()
        {
            this.Bid = bid;
            this.Ask = ask;
            this.TickType = tickType;
        }

        /// <summary>
        /// Creates a new price entry instance.
        /// </summary>
        /// <param name="bid">a price for bid</param>
        /// <param name="ask">a price for ask</param>
        /// <param name="tickType">tick type info</param>
        internal PriceEntry(double? bid, double? ask, TickTypes tickType)
            : this()
        {
            this.Bid = bid == null || double.IsNaN(bid.Value) ? null : (decimal?)bid;
            this.Ask = ask == null || double.IsNaN(ask.Value) ? null : (decimal?)ask;
            this.TickType = tickType;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Update price entry instance.
        /// </summary>
        /// <param name="bid">a price for bid</param>
        /// <param name="ask">a price for ask</param>
        /// <param name="tickType">tick type info</param>
        internal void Update(decimal? bid, decimal? ask, TickTypes tickType)
        {
            this.Bid = bid;
            this.Ask = ask;
            this.TickType = tickType;
        }

        /// <summary>
        /// Returns ask for buy and bid for sell
        /// </summary>
        /// <param name="side">trade entry side</param>
        /// <returns></returns>
        public decimal? PriceFromSide(OrderSide side)
        {
            if (side == OrderSide.Buy)
                return this.Ask;
            else if (side == OrderSide.Sell)
                return this.Bid;

            var message = string.Format("Unknown side={0}", side);
            throw new ArgumentException(message, nameof(side));
        }

        /// <summary>
        /// Returns bid for buy and ask for sell
        /// </summary>
        /// <param name="side">trade entry side</param>
        /// <returns></returns>
        public decimal? PriceFromOppositeSide(OrderSide side)
        {
            switch (side)
            {
                case OrderSide.Buy:
                    return this.Bid;
                case OrderSide.Sell:
                    return this.Ask;
            }

            var message = string.Format("Unknown side={0}", side);
            throw new ArgumentException(message, nameof(side));
        }

        /// <summary>
        /// Returns price rate, which should be used as multiplier for converting profit
        /// from profit currency to account currency.
        /// Example: xxx/yyy => zzz
        /// in this case we need profit conversion from yyy to zzz
        /// profit(zzz) = profit(yyy) * PriceMultiplierFromProfit(profit)
        /// </summary>
        /// <param name="profit">a converting profit</param>
        /// <returns></returns>
        public decimal? PriceMultiplierFromProfit(double profit)
        {
            // Price1 - ask if Py < 0, bid if Py >= 0;
            if (profit >= 0)
                return this.Bid;

            return this.Ask;
        }

        /// <summary>
        /// Returns price rate, which should be used as divisor for converting profit
        /// from profit currency to account currency.
        /// Example: xxx/yyy => zzz
        /// in this case we need profit conversion from yyy to zzz
        /// profit(zzz) = profit(yyy) / PriceDivisorFromProfit(profit)
        /// </summary>
        /// <param name="profit">a converting profit</param>
        /// <returns></returns>
        public decimal? PriceDivisorFromProfit(double profit)
        {
            // Price2 - bid if Py < 0, ask if Py >= 0;
            if (profit >= 0)
                return this.Ask;

            return this.Bid;
        }

        /// <summary>
        /// Returns price rate, which should be used as multiplier for converting asset
        /// from asset currency to account currency.
        /// </summary>
        /// <param name="asset">a converting asset</param>
        /// <returns></returns>
        public decimal? PriceMultiplierFromAsset(double asset)
        {
            if (asset >= 0)
                return this.Bid;

            return this.Ask;
        }

        /// <summary>
        /// Returns price rate, which should be used as divisor for converting asset
        /// from asset currency to account currency.
        /// </summary>
        /// <param name="asset">a converting asset</param>
        /// <returns></returns>
        public decimal? PriceDivisorFromAsset(double asset)
        {
            if (asset >= 0)
                return this.Ask;

            return this.Bid;
        }

        #endregion
    }
}
