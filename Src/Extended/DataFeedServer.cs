﻿namespace TickTrader.FDK.Extended
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Runtime.ExceptionServices;
    using Common;

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
        /// The method send two factor response with one time password.
        /// </summary>
        /// <param name="reason">Two factor response reason</param>
        /// <param name="otp">One time password</param>
        /// <returns>can not be null.</returns>
        public void SendTwoFactorResponse(TwoFactorReason reason, string otp)
        {
            if (reason == TwoFactorReason.ClientResponse)
            {
                dataFeed_.quoteFeedClient_.TwoFactorLoginResponseAsync(null, otp);
            }
            else if (reason == TwoFactorReason.ClientResume)
            {
                dataFeed_.quoteFeedClient_.TwoFactorLoginResumeAsync(null);
            }
            else
                throw new Exception("Invalid two factor reason : " + reason);
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
            List<string> symbolList = new List<string>();
            foreach (string symbol in symbols)
                symbolList.Add(symbol);

            dataFeed_.quoteFeedClient_.SubscribeQuotes(symbolList.ToArray(), depth, timeoutInMilliseconds);
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
            List<string> symbolList = new List<string>();
            foreach (string symbol in symbols)
                symbolList.Add(symbol);

            dataFeed_.quoteFeedClient_.UnsbscribeQuotes(symbolList.ToArray(), timeoutInMilliseconds);
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
            List<string> symbolList = new List<string>();
            foreach (string symbol in symbols)
                symbolList.Add(symbol);

            return dataFeed_.quoteFeedClient_.GetQuotes(symbolList.ToArray(), depth, timeoutInMilliseconds);
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
            try
            {
                Task<Bar[]> bidTask = dataFeed_.quoteStoreClient_.GetBarListAsync(symbol, PriceType.Bid, period, startTime, count);
                Task<Bar[]> askTask = dataFeed_.quoteStoreClient_.GetBarListAsync(symbol, PriceType.Ask, period, startTime, count);

                if (!bidTask.Wait(timeoutInMilliseconds))
                    throw new TimeoutException("Methof call timed out");

                if (!askTask.Wait(timeoutInMilliseconds))
                    throw new TimeoutException("Methof call timed out");

                Bar[] bidBars = bidTask.Result;
                Bar[] askBars = askTask.Result;

                int absCount = Math.Abs(count);
                List<PairBar> pairBars = new List<PairBar>(absCount);

                int bidIndex = 0;
                int askIndex = 0;

                while (pairBars.Count < absCount)
                {
                    Bar bidBar = bidIndex < bidBars.Length ? bidBars[bidIndex] : null;
                    Bar askBar = askIndex < askBars.Length ? askBars[askIndex] : null;

                    PairBar pairBar;

                    if (bidBar != null)
                    {
                        if (askBar != null)
                        {
                            int i = DateTime.Compare(bidBar.From, askBar.From);

                            if (i < 0)
                            {
                                pairBar = new PairBar(bidBar, null);
                                ++bidIndex;
                            }
                            else if (i > 0)
                            {
                                pairBar = new PairBar(null, askBar);
                                ++askIndex;
                            }
                            else
                            {
                                pairBar = new PairBar(bidBar, askBar);
                                ++bidIndex;
                                ++askIndex;
                            }
                        }
                        else
                        {
                            pairBar = new PairBar(bidBar, null);
                            ++bidIndex;
                        }
                    }
                    else if (askBar != null)
                    {
                        pairBar = new PairBar(null, askBar);
                        ++askIndex;
                    }
                    else
                        break;

                    pairBars.Add(pairBar);
                }

                return pairBars.ToArray();
            }
            catch (AggregateException ex)
            {
                ExceptionDispatchInfo.Capture(ex.Flatten().InnerExceptions[0]).Throw();
                // Unreacheble code...
                return null;
            }
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

        DataFeed dataFeed_;
    }
}
