namespace TickTrader.FDK.Extended
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using Common;

    /// <summary>
    /// The class contains methods, which are executed in client side.
    /// </summary>
    public class DataFeedCache
    {
        internal DataFeedCache(DataFeed dataFeed)
        {
            dataFeed_ = dataFeed;
            mutex_ = new object();            
            currencies_ = null;
            symbols_ = null;
            sessionInfo_ = null;
            quotes_ = new Dictionary<string, Quote>();
        }

        /// <summary>
        /// Gets currencies information. Returned value can not be null.
        /// </summary>
        public CurrencyInfo[] Currencies
        {
            get
            {
                lock (mutex_)
                {
                    return currencies_ != null ? currencies_ : emptyCurrencies_;
                }
            }
        }

        /// <summary>
        /// Gets symbols information. Returned value can not be null.
        /// </summary>
        public SymbolInfo[] Symbols
        {
            get
            {
                lock (mutex_)
                {
                    return symbols_ != null ? symbols_ : emptySymbols_;
                }
            }
        }

        /// <summary>
        /// Returns cache of session information.
        /// </summary>
        public SessionInfo SessionInfo
        {
            get
            {
                lock (mutex_)
                {
                    return sessionInfo_ != null ? sessionInfo_ : emptySessionInfo_;
                }
            }
        }

        /// <summary>
        /// Gets latest quotes. Returned value can not be null.
        /// </summary>
        public Quote[] Quotes
        {
            get
            {
                lock (mutex_)
                {
                    return quotes_.Values.ToArray();
                }
            }
        }

        /// <summary>
        /// The method gets the best bid price by symbol.
        /// </summary>
        /// <param name="symbol">a required financial security.</param>
        /// <returns>The best bid price.</returns>
        public double GetBid(string symbol)
        {
            double result;
            if (!this.TryGetBid(symbol, out result))
            {
                var message = string.Format("Off quotes for symbol={0}", symbol);
                throw new ArgumentException(message);
            }
            return result;
        }

        /// <summary>
        /// The method gets the best bid price by symbol.
        /// </summary>
        /// <param name="symbol">a required financial security.</param>
        /// <param name="price">the best bid.</param>
        /// <returns>false, if off quotes, otherwise true.</returns>
        public bool TryGetBid(string symbol, out double price)
        {
            double volume;
            DateTime creationTime;
            return this.TryGetBid(symbol, out price, out volume, out creationTime);
        }

        /// <summary>
        /// The method gets the best bid price, volume and creation time by symbol.
        /// </summary>
        /// <param name="symbol">Can not be null.</param>
        /// <param name="price">the best bid.</param>
        /// <param name="volume">volume of the best bid.</param>
        /// <param name="creationTime">the quote creation time.</param>
        /// <returns>false, if off quotes, otherwise true.</returns>
        public bool TryGetBid(string symbol, out double price, out double volume, out DateTime creationTime)
        {
            lock (mutex_)
            {
                Quote quote1 = quotes_[symbol];

                if (quote1 != null)
                {
                    if (quote1.HasBid)
                    {
                        QuoteEntry bestBid1 = quote1.Bids[0];
                        price = bestBid1.Price;
                        volume = bestBid1.Volume;
                        creationTime = quote1.CreatingTime;

                        return true;
                    }
                }
            }

            price = 0;
            volume = 0;
            creationTime = new DateTime();

            return false;
        }

        /// <summary>
        /// The method gets the best ask price by symbol.
        /// </summary>
        /// <param name="symbol">a required financial security.</param>
        /// <returns>The best ask price.</returns>
        public double GetAsk(string symbol)
        {
            double result;
            if (!this.TryGetAsk(symbol, out result))
            {
                var message = string.Format("Off quotes for symbol={0}", symbol);
                throw new ArgumentException(message);
            }

            return result;
        }

        /// <summary>
        /// The method gets the best ask price by symbol.
        /// </summary>
        /// <param name="symbol">a required financial security.</param>
        /// <param name="price">the best ask.</param>
        /// <returns>false, if off quotes, otherwise true.</returns>
        public bool TryGetAsk(string symbol, out double price)
        {
            double volume;
            DateTime creationTime;
            return this.TryGetAsk(symbol, out price, out volume, out creationTime);
        }

        /// <summary>
        /// The method gets the best ask price, volume and creation time by symbol.
        /// </summary>
        /// <param name="symbol">Can not be null.</param>
        /// <param name="price">the best ask.</param>
        /// <param name="volume">volume of the best ask.</param>
        /// <param name="creationTime">the quote creation time.</param>
        /// <returns>false, if off quotes, otherwise true.</returns>
        public bool TryGetAsk(string symbol, out double price, out double volume, out DateTime creationTime)
        {
            lock (mutex_)
            {
                Quote quote1 = quotes_[symbol];

                if (quote1 != null)
                {
                    if (quote1.HasAsk)
                    {
                        QuoteEntry bestAsk1 = quote1.Asks[0];
                        price = bestAsk1.Price;
                        volume = bestAsk1.Volume;
                        creationTime = quote1.CreatingTime;

                        return true;
                    }
                }
            }

            price = 0;
            volume = 0;
            creationTime = new DateTime();

            return false;
        }

        /// <summary>
        /// The method gets level2 quotes by symbol.
        /// </summary>
        /// <param name="symbol">Can not be null.</param>
        /// <returns>Level2 quotes.</returns>
        public Quote GetLevel2(string symbol)
        {
            Quote result;
            if (!this.TryGetLevel2(symbol, out result))
            {
                var message = string.Format("Off quotes for symbol={0}", symbol);
                throw new ArgumentException(message);
            }

            return result;
        }

        /// <summary>
        /// The method gets level2 quotes by symbol.
        /// </summary>
        /// <param name="symbol">Can not be null.</param>
        /// <param name="quote"></param>
        /// <returns>True, if quote for the symbol is presented, otherwise false.</returns>
        public bool TryGetLevel2(string symbol, out Quote quote)
        {
            lock (mutex_)
            {
                Quote quote1 = quotes_[symbol];

                if (quote1 != null)
                {
                    quote = quote1;

                    return true;
                }
            }

            quote = null;

            return false;
        }
                
        static CurrencyInfo[] emptyCurrencies_ = new CurrencyInfo[0];
        static SymbolInfo[] emptySymbols_ = new SymbolInfo[0];
        static SessionInfo emptySessionInfo_ = new SessionInfo();

        DataFeed dataFeed_;

        internal object mutex_;        
        internal CurrencyTypeInfo[] currencyTypes_;
        internal CurrencyInfo[] currencies_;
        internal SymbolInfo[] symbols_;
        internal SessionInfo sessionInfo_;
        internal Dictionary<string, Quote> quotes_;
    }
}
