namespace TickTrader.FDK.Extended
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Runtime.ExceptionServices;
    using Common;
    using System.Linq;

    /// <summary>
    /// The class contains methods, which are executed in server side.
    /// </summary>
    public class DataFeedServer
    {
        internal DataFeedServer(DataFeed dataFeed)
        {
            dataFeed_ = dataFeed;
        }

        /// <summary>
        /// The method returns list of currenc types supported by server.
        /// </summary>
        /// <returns></returns>
        public CurrencyTypeInfo[] GetCurrencyTypes()
        {
            return GetCurrencyTypesEx(dataFeed_.synchOperationTimeout_);
        }

        /// <summary>
        /// The method returns list of currency types supported by server.
        /// </summary>
        /// <param name="timeoutInMilliseconds">timeout of the operation</param>
        /// <returns></returns>
        public CurrencyTypeInfo[] GetCurrencyTypesEx(int timeoutInMilliseconds)
        {
            return dataFeed_.quoteFeedClient_.GetCurrencyTypeList(timeoutInMilliseconds);
        }

        /// <summary>
        /// The method returns list of currencies supported by server.
        /// </summary>
        /// <returns></returns>
        public CurrencyInfo[] GetCurrencies()
        {
            return GetCurrenciesEx(dataFeed_.synchOperationTimeout_);
        }

        /// <summary>
        /// The method returns list of currencies supported by server.
        /// </summary>
        /// <param name="timeoutInMilliseconds">timeout of the operation</param>
        /// <returns></returns>
        public CurrencyInfo[] GetCurrenciesEx(int timeoutInMilliseconds)
        {
            return dataFeed_.quoteFeedClient_.GetCurrencyList(timeoutInMilliseconds);
        }

        /// <summary>
        /// The method returns list of symbols supported by server.
        /// </summary>
        /// <returns>can not be null</returns>
        public SymbolInfo[] GetSymbols()
        {
            return GetSymbolsEx(dataFeed_.synchOperationTimeout_);
        }

        /// <summary>
        /// The method returns list of symbols supported by server.
        /// </summary>
        /// <param name="timeoutInMilliseconds">timeout of the operation</param>
        /// <returns>can not be null</returns>
        public SymbolInfo[] GetSymbolsEx(int timeoutInMilliseconds)
        {
            return dataFeed_.quoteFeedClient_.GetSymbolList(timeoutInMilliseconds);
        }

        /// <summary>
        /// The method returns the current trade session information.
        /// </summary>
        /// <returns>can not be null.</returns>
        public SessionInfo GetSessionInfo()
        {
            return GetSessionInfoEx(dataFeed_.synchOperationTimeout_);
        }

        /// <summary>
        /// The method returns the current trade session information.
        /// </summary>
        /// <param name="timeoutInMilliseconds">timeout of the operation in milliseconds</param>
        /// <returns>can not be null.</returns>
        public SessionInfo GetSessionInfoEx(int timeoutInMilliseconds)
        {
            return dataFeed_.quoteFeedClient_.GetSessionInfo(timeoutInMilliseconds);
        }

        public void SubscribeToBars(IEnumerable<BarSubscriptionSymbolEntry> entries)
        {
            SubscribeToBarsEx(entries, dataFeed_.synchOperationTimeout_);
        }

        public void SubscribeToBarsEx(IEnumerable<BarSubscriptionSymbolEntry> entries, int timeout)
        {
            dataFeed_.quoteFeedClient_.SubscribeBars(entries.ToArray(), timeout);
        }

        public void UnsubscribeBars(IEnumerable<string> symbols)
        {
            UnsubscribeBars(symbols, dataFeed_.synchOperationTimeout_);
        }

        public void UnsubscribeBars(IEnumerable<string> symbols, int timeout)
        {
            dataFeed_.quoteFeedClient_.UnsubscribeBars(symbols.ToArray(), timeout);
        }

        /// <summary>
        /// The method subscribes to quotes.
        /// </summary>
        /// <param name="symbols">list of requested symbols; can not be null</param>
        /// <param name="depth">
        /// 0 - full book
        /// (1..5) - restricted book
        /// </param>
        public void SubscribeToQuotes(IEnumerable<string> symbols, int depth)
        {
            SubscribeToQuotesEx(symbols, depth, dataFeed_.synchOperationTimeout_);
        }

        /// <summary>
        /// The method subscribes to quotes.
        /// </summary>
        /// <param name="symbols">list of requested symbols; can not be null</param>
        /// <param name="depth">
        /// 0 - full book
        /// (1..5) - restricted book
        /// </param>
        /// <param name="timeoutInMilliseconds">Timeout of the operation</param>
        public void SubscribeToQuotesEx(IEnumerable<string> symbols, int depth, int timeoutInMilliseconds)
        {
            List<SymbolEntry> symbolEntryList = new List<SymbolEntry>();
            foreach (string symbol in symbols)
            {
                SymbolEntry symbolEntry = new SymbolEntry();
                symbolEntry.Id = symbol;
                symbolEntry.MarketDepth = (ushort) depth;

                symbolEntryList.Add(symbolEntry);
            }

            dataFeed_.quoteFeedClient_.SubscribeQuotes(symbolEntryList.ToArray(), timeoutInMilliseconds);
        }

        /// <summary>
        /// The method subscribes to quotes.
        /// </summary>
        /// <param name="symbols">list of requested symbols; can not be null</param>
        /// <param name="depth">
        /// 0 - full book
        /// (1..5) - restricted book
        /// </param>
        /// <param name="frequencyPrioirity">
        /// -1 - realtime
        /// (0..5) - frequency equals to 2^frequencyPrioirity * 500 ms;
        /// </param>
        public void SubscribeToFreqQuotes(IEnumerable<string> symbols, int depth, int frequencyPrioirity)
        {
            SubscribeToFreqQuotesEx(symbols, depth, frequencyPrioirity, dataFeed_.synchOperationTimeout_);
        }

        /// <summary>
        /// The method subscribes to quotes.
        /// </summary>
        /// <param name="symbols">list of requested symbols; can not be null</param>
        /// <param name="depth">
        /// 0 - full book
        /// (1..5) - restricted book
        /// </param>
        /// <param name="frequencyPrioirity">
        /// -1 - realtime
        /// (0..5) - frequency equals to 2^frequencyPrioirity * 500 ms;
        /// </param>
        /// <param name="timeoutInMilliseconds">Timeout of the operation</param>
        public void SubscribeToFreqQuotesEx(IEnumerable<string> symbols, int depth, int frequencyPrioirity, int timeoutInMilliseconds)
        {
            List<SymbolEntry> symbolEntryList = new List<SymbolEntry>();
            foreach (string symbol in symbols)
            {
                SymbolEntry symbolEntry = new SymbolEntry();
                symbolEntry.Id = symbol;
                symbolEntry.MarketDepth = (ushort)depth;

                symbolEntryList.Add(symbolEntry);
            }

            dataFeed_.quoteFeedClient_.SubscribeQuotes(symbolEntryList.ToArray(), frequencyPrioirity, timeoutInMilliseconds);
        }

        /// <summary>
        /// The method unsubscribes quotes.
        /// </summary>
        /// <param name="symbols">list of symbols, which server should not send to the client; can not be null</param>
        public void UnsubscribeQuotes(IEnumerable<string> symbols)
        {
            UnsubscribeQuotesEx(symbols, dataFeed_.synchOperationTimeout_);
        }

        /// <summary>
        /// The method unsubscribes quotes.
        /// </summary>
        /// <param name="symbols">list of symbols, which server should not send to the client; can not be null</param>
        /// <param name="timeoutInMilliseconds">timeout of the operation</param>
        public void UnsubscribeQuotesEx(IEnumerable<string> symbols, int timeoutInMilliseconds)
        {
            dataFeed_.quoteFeedClient_.UnsubscribeQuotes(symbols.ToArray(), timeoutInMilliseconds);
        }

        /// <summary>
        /// The method returns the latest quotes.
        /// </summary>
        /// <param name="symbols">list of symbols; can not be null</param>
        public Quote[] GetQuotes(IEnumerable<string> symbols, int depth)
        {
            return GetQuotesEx(symbols, depth, dataFeed_.synchOperationTimeout_);
        }

        /// <summary>
        /// The method returns list of symbols supported by server.
        /// </summary>
        /// <param name="timeoutInMilliseconds">timeout of the operation</param>
        /// <returns>can not be null</returns>
        public Quote[] GetQuotesEx(IEnumerable<string> symbols, int depth, int timeoutInMilliseconds)
        {
            List<SymbolEntry> symbolEntryList = new List<SymbolEntry>();
            foreach (string symbol in symbols)
            {
                SymbolEntry symbolEntry = new SymbolEntry();
                symbolEntry.Id = symbol;
                symbolEntry.MarketDepth = (ushort) depth;

                symbolEntryList.Add(symbolEntry);
            }

            return dataFeed_.quoteFeedClient_.GetQuotes(symbolEntryList.ToArray(), timeoutInMilliseconds);
        }

        /// <summary>
        /// This method retrieves a limited list of bars of a symbol
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="priceType"></param>
        /// <param name="period"></param>
        /// <param name="startTime"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public Bar[] GetBars(string symbol, PriceType priceType, BarPeriod period, DateTime startTime, int count)
        {
            return GetBarsEx(symbol, priceType, period, startTime, count, dataFeed_.synchOperationTimeout_);
        }

        /// <summary>
        /// This method retrieves a limited list of bars of a symbol
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="priceType"></param>
        /// <param name="period"></param>
        /// <param name="startTime"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public Bar[] GetBarsEx(string symbol, PriceType priceType, BarPeriod period, DateTime startTime, int count, int timeoutInMilliseconds)
        {
            return dataFeed_.quoteStoreClient_.GetBarList(symbol, priceType, period, startTime, count, timeoutInMilliseconds);
        }

        /// <summary>
        /// This method retrieves a limited list of bars of array of symbols
        /// </summary>
        /// <param name="symbols"></param>
        /// <param name="priceType"></param>
        /// <param name="period"></param>
        /// <param name="startTime"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public Bar[] GetBars(string[] symbols, PriceType priceType, BarPeriod period, DateTime startTime, int count)
        {
            return GetBarsEx(symbols, priceType, period, startTime, count, dataFeed_.synchOperationTimeout_);
        }

        /// <summary>
        /// This method retrieves a limited list of bars of array of symbols
        /// </summary>
        /// <param name="symbols"></param>
        /// <param name="priceType"></param>
        /// <param name="period"></param>
        /// <param name="startTime"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public Bar[] GetBarsEx(string[] symbols, PriceType priceType, BarPeriod period, DateTime startTime, int count, int timeoutInMilliseconds)
        {
            return dataFeed_.quoteStoreClient_.GetBarList(symbols, priceType, period, startTime, count, timeoutInMilliseconds);
        }

        /// <summary>
        /// This method retrieves a limited list of bar pairs of array of symbols
        /// </summary>
        /// <param name="symbols"></param>
        /// <param name="period"></param>
        /// <param name="startTime"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public IDictionary<string, PairBar[]> GetBars(string[] symbols, BarPeriod period, DateTime startTime, int count)
        {
            return GetBarsEx(symbols, period, startTime, count, dataFeed_.synchOperationTimeout_);
        }

        /// <summary>
        /// This method retrieves a limited list of bar pairs of array of symbols
        /// </summary>
        /// <param name="symbols"></param>
        /// <param name="period"></param>
        /// <param name="startTime"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public IDictionary<string, PairBar[]> GetBarsEx(string[] symbols, BarPeriod period, DateTime startTime, int count, int timeoutInMilliseconds)
        {
            DataFeed.BarListContext bidBarListContext = new DataFeed.BarListContext();
            DataFeed.BarListContext askBarListContext = new DataFeed.BarListContext();

            symbols = symbols.Distinct().ToArray();

            dataFeed_.quoteStoreClient_.GetBarListAsync(bidBarListContext, symbols, PriceType.Bid, period, startTime, count);
            dataFeed_.quoteStoreClient_.GetBarListAsync(askBarListContext, symbols, PriceType.Ask, period, startTime, count);

            if (!bidBarListContext.event_.WaitOne(timeoutInMilliseconds))
                throw new Common.TimeoutException("Method call timed out");

            if (!askBarListContext.event_.WaitOne(timeoutInMilliseconds))
                throw new Common.TimeoutException("Method call timed out");

            if (bidBarListContext.exception_ != null)
                throw bidBarListContext.exception_;

            if (askBarListContext.exception_ != null)
                throw askBarListContext.exception_;

            var bids = bidBarListContext.bars_
                .GroupBy(b => b.Symbol)
                .ToDictionary(g => g.Key, g => g.ToArray());
            var asks = askBarListContext.bars_
                .GroupBy(b => b.Symbol)
                .ToDictionary(g => g.Key, g => g.ToArray());

            var barsBySymbols = new Dictionary<string, PairBar[]>();
            var emptyArray = new Bar[0];
            foreach (var symbol in symbols)
            {
                barsBySymbols[symbol] = GetPairBarList(bids.ContainsKey(symbol) ? bids[symbol] : emptyArray,
                    asks.ContainsKey(symbol) ? asks[symbol] : emptyArray, count);
            }

            return barsBySymbols;
        }

        /// <summary>
        /// This method retrieves a limited list of bar pairs of a symbol
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="period"></param>
        /// <param name="startTime"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public PairBar[] GetBars(string symbol, BarPeriod period, DateTime startTime, int count)
        {
            return GetBarsEx(symbol, period, startTime, count, dataFeed_.synchOperationTimeout_);
        }

        /// <summary>
        /// This method retrieves a limited list of bar pairs of a symbol
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="period"></param>
        /// <param name="startTime"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public PairBar[] GetBarsEx(string symbol, BarPeriod period, DateTime startTime, int count, int timeoutInMilliseconds)
        {
            DataFeed.BarListContext bidBarListContext = new DataFeed.BarListContext();
            DataFeed.BarListContext askBarListContext = new DataFeed.BarListContext();

            dataFeed_.quoteStoreClient_.GetBarListAsync(bidBarListContext, symbol, PriceType.Bid, period, startTime, count);
            dataFeed_.quoteStoreClient_.GetBarListAsync(askBarListContext, symbol, PriceType.Ask, period, startTime, count);

            if (! bidBarListContext.event_.WaitOne(timeoutInMilliseconds))
                throw new Common.TimeoutException("Method call timed out");

            if (! askBarListContext.event_.WaitOne(timeoutInMilliseconds))
                throw new Common.TimeoutException("Method call timed out");

            if (bidBarListContext.exception_ != null)
                throw bidBarListContext.exception_;

            if (askBarListContext.exception_ != null)
                throw askBarListContext.exception_;

            return GetPairBarList(bidBarListContext.bars_, askBarListContext.bars_, count);
        }

        /// <summary>
        /// The method gets history bars from the server.
        /// </summary>
        /// <param name="symbol">A required symbol; can not be null.</param>
        /// <param name="priceType">A required price type: Bid or Ask.</param>
        /// <param name="startTime">A start time of bars enumeration.</param>
        /// <param name="endTime">A end time of bars enumeration.</param>
        /// <param name="period">Bar period instance; can not be null.</param>
        /// <returns></returns>
        public Bars GetBarsHistory(string symbol, PriceType priceType, BarPeriod period, DateTime startTime, DateTime endTime)
        {
            return GetBarsHistoryEx(symbol, priceType, period, startTime, endTime, dataFeed_.synchOperationTimeout_);
        }

        /// <summary>
        /// The method gets history bars from the server.
        /// </summary>
        /// <param name="symbol">A required symbol; can not be null.</param>
        /// <param name="priceType">A required price type: Bid or Ask.</param>
        /// <param name="startTime">A start time of bars enumeration.</param>
        /// <param name="endTime">A end time of bars enumeration.</param>
        /// <param name="period">Bar period instance; can not be null.</param>
        /// <returns></returns>
        public Bars GetBarsHistoryEx(string symbol, PriceType priceType, BarPeriod period, DateTime startTime, DateTime endTime, int timeoutInMilliseconds)
        {
            return new Bars(dataFeed_, symbol, priceType, period, startTime, endTime, timeoutInMilliseconds);
        }

        /// <summary>
        /// The method gets history bars from the server.
        /// </summary>
        /// <param name="symbol">A required symbol; can not be null.</param>
        /// <param name="priceType">A required price type: Bid or Ask.</param>
        /// <param name="startTime">A start time of bars enumeration.</param>
        /// <param name="endTime">A end time of bars enumeration.</param>
        /// <param name="period">Bar period instance; can not be null.</param>
        /// <returns></returns>
        public PairBars GetBarsHistory(string symbol, BarPeriod period, DateTime startTime, DateTime endTime)
        {
            return GetBarsHistoryEx(symbol, period, startTime, endTime, dataFeed_.synchOperationTimeout_);
        }

        /// <summary>
        /// The method gets history bars from the server.
        /// </summary>
        /// <param name="symbol">A required symbol; can not be null.</param>
        /// <param name="priceType">A required price type: Bid or Ask.</param>
        /// <param name="startTime">A start time of bars enumeration.</param>
        /// <param name="endTime">A end time of bars enumeration.</param>
        /// <param name="period">Bar period instance; can not be null.</param>
        /// <returns></returns>
        public PairBars GetBarsHistoryEx(string symbol, BarPeriod period, DateTime startTime, DateTime endTime, int timeoutInMilliseconds)
        {
            return new PairBars(dataFeed_, symbol, period, startTime, endTime, timeoutInMilliseconds);
        }

        /// <summary>
        /// This method retrieves a limited list of quotes of a symbol
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="startTime"></param>
        /// <param name="count"></param>
        /// <param name="depth"></param>
        /// <returns></returns>
        public Quote[] GetQuotes(string symbol, DateTime startTime, int count, int depth)
        {
            return GetQuotesEx(symbol, startTime, count, depth, dataFeed_.synchOperationTimeout_);
        }

        /// <summary>
        /// This method retrieves a limited list of quotes of a symbol
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="startTime"></param>
        /// <param name="count"></param>
        /// <param name="depth"></param>
        /// <returns></returns>
        public Quote[] GetQuotesEx(string symbol, DateTime startTime, int count, int depth, int timeoutInMilliseconds)
        {
            return dataFeed_.quoteStoreClient_.GetQuoteList(symbol, depth == 1 ? QuoteDepth.Top : QuoteDepth.Level2, startTime, count, timeoutInMilliseconds);
        }

        /// <summary>
        /// The method gets history quotes from the server.
        /// </summary>
        public QuotesSingleSequence GetQuotesHistory(string symbol, DateTime startTime, DateTime endTime, int depth)
        {
            return GetQuotesHistoryEx(symbol, startTime, endTime, depth, dataFeed_.synchOperationTimeout_);
        }

        /// <summary>
        /// The method gets history quotes from the server.
        /// </summary>
        public QuotesSingleSequence GetQuotesHistoryEx(string symbol, DateTime startTime, DateTime endTime, int depth, int timeoutInMilliseconds)
        {
            return new QuotesSingleSequence(dataFeed_, symbol, startTime, endTime, depth, timeoutInMilliseconds);
        }

        /// <summary>
        /// The method returns list of currencies supported by server.
        /// </summary>
        /// <param name="timeoutInMilliseconds">timeout of the operation</param>
        /// <returns></returns>
        [Obsolete("Please use GetCurrenciesEx()")]
        public CurrencyInfo[] GetCurrencies(int timeoutInMilliseconds)
        {
            return GetCurrenciesEx(timeoutInMilliseconds);
        }

        /// <summary>
        /// The method gets bars history information of a symbol from server.
        /// </summary>
        /// <param name="symbol">A required symbol; can not be null.</param>
        /// <param name="priceType">A required price type: Bid or Ask.</param>
        /// <param name="period">Bar period instance; can not be null.</param>
        /// <returns></returns>
        public HistoryInfo GetBarsHistoryInfo(string symbol, PriceType priceType, BarPeriod period)
        {
            return GetBarsHistoryInfoEx(symbol, priceType, period, dataFeed_.synchOperationTimeout_);
        }

        /// <summary>
        /// The method gets bars history information of a symbol from server.
        /// </summary>
        /// <param name="symbol">A required symbol; can not be null.</param>
        /// <param name="priceType">A required price type: Bid or Ask.</param>
        /// <param name="period">Bar period instance; can not be null.</param>
        /// <param name="timeoutInMilliseconds">Timeout in milliseconds</param>
        /// <returns></returns>
        public HistoryInfo GetBarsHistoryInfoEx(string symbol, PriceType priceType, BarPeriod period, int timeoutInMilliseconds)
        {
            return dataFeed_.quoteStoreClient_.GetBarsHistoryInfo(symbol, period, priceType, timeoutInMilliseconds);
        }

        /// <summary>
        /// The method gets quotes history information of a symbol from server.
        /// </summary>
        /// <param name="symbol">A required symbol; can not be null.</param>
        /// <param name="level2">Level2 history if true</param>
        /// <returns></returns>
        public HistoryInfo GetQuotesHistoryInfo(string symbol, bool level2)
        {
            return GetQuotesHistoryInfoEx(symbol, level2, dataFeed_.synchOperationTimeout_);
        }

        /// <summary>
        /// The method gets quotes history information of a symbol from server.
        /// </summary>
        /// <param name="symbol">A required symbol; can not be null.</param>
        /// <param name="level2">Level2 history if true</param>
        /// <param name="timeoutInMilliseconds">Timeout in milliseconds</param>
        /// <returns></returns>
        public HistoryInfo GetQuotesHistoryInfoEx(string symbol, bool level2, int timeoutInMilliseconds)
        {
            return dataFeed_.quoteStoreClient_.GetQuotesHistoryInfo(symbol, level2, timeoutInMilliseconds);
        }

        TickTrader.FDK.Common.PairBar[] GetPairBarList(TickTrader.FDK.Common.Bar[] bidBars, TickTrader.FDK.Common.Bar[] askBars, int count)
        {
            int absCount = Math.Abs(count);
            List<PairBar> pairBars = new List<PairBar>(absCount);

            int bidIndex = (count < 0) ? (bidBars.Length - 1) : 0;
            int askIndex = (count < 0) ? (askBars.Length - 1) : 0;

            while (pairBars.Count < absCount)
            {
                TickTrader.FDK.Common.Bar bidBar = (bidIndex >= 0) && (bidIndex < bidBars.Length) ? bidBars[bidIndex] : null;
                TickTrader.FDK.Common.Bar askBar = (askIndex >= 0) && (askIndex < askBars.Length) ? askBars[askIndex] : null;

                PairBar pairBar;

                if (bidBar != null)
                {
                    if (askBar != null)
                    {
                        int i = DateTime.Compare(bidBar.From, askBar.From);

                        if (count < 0)
                            i = -i;

                        if (i < 0)
                        {
                            pairBar = new PairBar(bidBar, null);
                            bidIndex += (count < 0) ? -1 : 1;
                        }
                        else if (i > 0)
                        {
                            pairBar = new PairBar(null, askBar);
                            askIndex += (count < 0) ? -1 : 1;
                        }
                        else
                        {
                            pairBar = new PairBar(bidBar, askBar);
                            bidIndex += (count < 0) ? -1 : 1;
                            askIndex += (count < 0) ? -1 : 1;
                        }
                    }
                    else
                    {
                        pairBar = new PairBar(bidBar, null);
                        bidIndex += (count < 0) ? -1 : 1;
                    }
                }
                else if (askBar != null)
                {
                    pairBar = new PairBar(null, askBar);
                    askIndex += (count < 0) ? -1 : 1;
                }
                else
                    break;

                pairBars.Add(pairBar);
            }

            return pairBars.ToArray();
        }

        DataFeed dataFeed_;
    }
}
