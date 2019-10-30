namespace TickTrader.FDK.Common
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Options for indicative tick.
    /// </summary>
    public enum TickTypes
    {
        Normal,
        IndicativeBid,
        IndicativeAsk,
        IndicativeBidAsk
    }


    /// <summary>
    /// Tick class contains bid/ask quotes for a symbol.
    /// </summary>
    public class Quote
    {
        /// <summary>
        /// The constructor is used by types serializer.
        /// </summary>
        public Quote()
        {
            Bids = new List<QuoteEntry>(100);
            Asks = new List<QuoteEntry>(100);
        }

        /// <summary>
        /// Gets true, if the tick has bid quote.
        /// </summary>
        public bool HasBid
        {
            get
            {
                return this.Bids.Count > 0;
            }
        }

        /// <summary>
        /// Gets true, if the tick has ask quote.
        /// </summary>
        public bool HasAsk
        {
            get
            {
                return this.Asks.Count > 0;
            }
        }

        /// <summary>
        /// Gets the best price for selling.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">If off quotes for selling.</exception>
        public double? Bid
        {
            get
            {
                if (!this.HasBid)
                    //throw new InvalidOperationException("Off bid quotes.");
                    return null;

                return this.Bids[0].Price;
            }
        }

        /// <summary>
        /// Gets the best price for buying.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">If off quotes for buying.</exception>
        public double? Ask
        {
            get
            {
                if (!this.HasAsk)
                    //throw new InvalidOperationException("Off ask quotes.");
                    return null;

                return this.Asks[0].Price;
            }
        }

        /// <summary>
        /// Gets the quote spread, if it is available.
        /// </summary>
        public double Spread
        {
            get
            {
                if (this.Ask != null && this.Bid != null)
                    return (double) this.Ask - (double) this.Bid;
                return double.NaN;
            }
        }

        /// <summary>
        /// Get the quote creating time.
        /// </summary>
        public DateTime CreatingTime { get; set; }

        /// <summary>
        /// Gets symbol name.
        /// </summary>
        public string Symbol { get; set; }

        /// <summary>
        /// Gets bid quotes; returned array can not be null.
        /// </summary>
        public List<QuoteEntry> Bids { get; set; }

        /// <summary>
        /// Gets ask quotes; returned array can not be null.
        /// </summary>
        public List<QuoteEntry> Asks { get; set; }

        /// <summary>
        /// The identifier is used by quotes storage.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Check if the tick is indicative.
        /// </summary>
        public bool IndicativeTick { get; set; }


        /// <summary>
        /// Indicative Tick Option
        /// </summary>
        public TickTypes TickType { get; set; }


        /// <summary>
        /// 
        /// </summary>
        public Quote Clone()
        {
            Quote quote = new Quote();
            quote.Id = Id;
            quote.Symbol = Symbol;
            quote.CreatingTime = CreatingTime;
            quote.Asks = new List<QuoteEntry>(Asks);
            quote.Bids = new List<QuoteEntry>(Bids);
            quote.IndicativeTick = IndicativeTick;
            quote.TickType = TickType;

            return quote;
        }

        /// <summary>
        /// Compares to quotes for equality.
        /// </summary>
        /// <param name="first">the first quote to compare.</param>
        /// <param name="second">the second quote to compare.</param>
        /// <returns>true, if two quotes are the same or null, else false</returns>
        public static bool Equals(Quote first, Quote second)
        {
            if (object.ReferenceEquals(first, second))
                return true;

            if (object.ReferenceEquals(first, null))
                return false;

            if (object.ReferenceEquals(null, second))
                return false;

            if (first.Symbol != second.Symbol)
                return false;

            if (first.CreatingTime != second.CreatingTime)
                return false;

            if (!Equals(first.Bids, second.Bids))
                return false;

            if (!Equals(first.Asks, second.Asks))
                return false;

            if (!Equals(first.IndicativeTick, second.IndicativeTick))
                return false;

            return true;
        }

        static bool Equals(QuoteEntry[] first, QuoteEntry[] second)
        {
            if (ReferenceEquals(first, second))
            {
                return true;
            }

            var count = first.Length;
            if (count != second.Length)
                return false;

            for (var index = 0; index < count; ++index)
            {
                var f = first[index];
                var s = second[index];

                if (f.Price != s.Price)
                    return false;

                if (f.Volume != s.Volume)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Returns formatted string for the class instance.
        /// </summary>
        /// <returns>can not be null</returns>
        public override string ToString()
        {
            var bid = HasBid ? Bid.ToString() : "None";
            var ask = HasAsk ? Ask.ToString() : "None";

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Symbol = {0}; Bid = {1}; Ask = {2}", this.Symbol, bid, ask);
            if (IndicativeTick)
                sb.Append(", IndicativeTick=True");
            sb.AppendFormat(", TickType={0}", TickType);


            return sb.ToString();
        }
    }
}
