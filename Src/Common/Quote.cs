namespace TickTrader.FDK.Common
{
    using System;

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
        }

        /// <summary>
        /// Gets true, if the tick has bid quote.
        /// </summary>
        public bool HasBid
        {
            get
            {
                return this.Bids.Length > 0;
            }
        }

        /// <summary>
        /// Gets true, if the tick has ask quote.
        /// </summary>
        public bool HasAsk
        {
            get
            {
                return this.Asks.Length > 0;
            }
        }

        /// <summary>
        /// Gets the best price for selling.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">If off quotes for selling.</exception>
        public double Bid
        {
            get
            {
                if (!this.HasBid)
                    throw new InvalidOperationException("Off bid quotes.");

                return this.Bids[0].Price;
            }
        }

        /// <summary>
        /// Gets the best price for buying.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">If off quotes for buying.</exception>
        public double Ask
        {
            get
            {
                if (!this.HasAsk)
                    throw new InvalidOperationException("Off ask quotes.");

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
                return this.Ask - this.Bid;
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
        public QuoteEntry[] Bids { get; set; }

        /// <summary>
        /// Gets ask quotes; returned array can not be null.
        /// </summary>
        public QuoteEntry[] Asks { get; set; }

        /// <summary>
        /// The identifier is used by quotes storage.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Quote Clone()
        {
            Quote quote = new Quote();
            quote.Id = Id;
            quote.Symbol = Symbol;
            quote.CreatingTime = CreatingTime;

            quote.Asks = new QuoteEntry[Asks.Length];
            Asks.CopyTo(quote.Asks, 0);

            quote.Bids = new QuoteEntry[Asks.Length];
            Bids.CopyTo(quote.Bids, 0);

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

            return true;
        }

        static bool Equals(QuoteEntry[] first, QuoteEntry[] second)
        {
            if(ReferenceEquals(first,second))
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

            return string.Format("Symbol = {0}; Bid = {1}; Ask = {2}", this.Symbol, bid, ask);
        }
    }
}
