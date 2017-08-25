namespace TickTrader.FDK.Extended
{
    using System;
    using System.Collections.Generic;
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
                dataFeed_.client_.SendOneTimePassword(otp);
            }
            else
                throw new Exception("Invalid two factor reason : " + reason);
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
            return dataFeed_.client_.GetSessionInfo(timeoutInMilliseconds);
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
        [Obsolete("Please use GetCurrenciesEx()")]
        public CurrencyInfo[] GetCurrencies(int timeoutInMilliseconds)
        {
            return GetCurrenciesEx(timeoutInMilliseconds);
        }

        /// <summary>
        /// The method returns list of currencies supported by server.
        /// </summary>
        /// <param name="timeoutInMilliseconds">timeout of the operation</param>
        /// <returns></returns>
        public CurrencyInfo[] GetCurrenciesEx(int timeoutInMilliseconds)
        {
            return dataFeed_.client_.GetCurrencyList(timeoutInMilliseconds);
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
            return dataFeed_.client_.GetSymbolList(timeoutInMilliseconds);
        }

        /// <summary>
        /// Returns version of server quotes history.
        /// </summary>
        /// <returns>quote history version</returns>
        public int GetQuotesHistoryVersion()
        {
            throw new Exception("Not impled");
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

            dataFeed_.client_.SubscribeQuotes(symbolList.ToArray(), depth, timeoutInMilliseconds);
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

            dataFeed_.client_.UnsbscribeQuotes(symbolList.ToArray(), timeoutInMilliseconds);
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

            return dataFeed_.client_.GetQuotes(symbolList.ToArray(), depth, timeoutInMilliseconds);
        }

        // TODO:
#if TODO
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
            return new Bars(dataFeed_, symbol, priceType, period, startTime, endTime);
        }
#endif
        /// <summary>
        /// The method gets history bars from the server.
        /// </summary>
        /// <param name="symbol">A required symbol; can not be null.</param>
        /// <param name="time">Date and time which specifies the historical point.</param>
        /// <param name="barsNumber">The maximum number of bars in the requested chart. The value can be negative or positive.
        /// Positive value means historical chart from the specified historical point to future.</param>
        /// <param name="priceType">Can be bid or ask.</param>
        /// <param name="period">Chart periodicity.</param>
        /// <returns>Can not be null.</returns>
        public DataHistoryInfo GetHistoryBars(string symbol, DateTime time, int barsNumber, PriceType priceType, BarPeriod period)
        {
            return GetHistoryBarsEx(symbol, time, barsNumber, priceType, period, dataFeed_.synchOperationTimeout_);
        }

        /// <summary>
        /// The method gets history bars from the server.
        /// </summary>
        /// <param name="symbol">A required symbol; can not be null.</param>
        /// <param name="time">Date and time which specifies the historical point.</param>
        /// <param name="barsNumber">The maximum number of bars in the requested chart. The value can be negative or positive.
        /// Positive value means historical chart from the specified historical point to future.</param>
        /// <param name="priceType">Can be bid or ask.</param>
        /// <param name="period">Chart periodicity.</param>
        /// <param name="timeoutInMilliseconds">timeout of the operation</param>
        /// <returns>Can not be null.</returns>
        public DataHistoryInfo GetHistoryBarsEx(string symbol, DateTime time, int barsNumber, PriceType priceType, BarPeriod period, int timeoutInMilliseconds)
        {
            throw new Exception("Not impled");
        }

        /// <summary>
        /// The method gets history bars from the server.
        /// </summary>
        /// <param name="symbol">A required symbol; can not be null.</param>
        /// <param name="time">Date and time which specifies the historical point.</param>
        /// <param name="priceType"></param>
        /// <param name="period"></param>
        /// <returns>Can not be null.</returns>
        public DataHistoryInfo GetBarsHistoryFiles(string symbol, DateTime time, PriceType priceType, string period)
        {
            return GetBarsHistoryFilesEx(symbol, time, priceType, period, dataFeed_.synchOperationTimeout_);
        }

        /// <summary>
        /// The method gets history bars from the server.
        /// </summary>
        /// <param name="symbol">A required symbol; can not be null.</param>
        /// <param name="time">Date and time which specifies the historical point.</param>
        /// <param name="timeoutInMilliseconds">Timeout of the operation in milliseconds.</param>
        /// <param name="priceType"></param>
        /// <param name="period"></param>
        /// <returns>Can not be null.</returns>
        public DataHistoryInfo GetBarsHistoryFilesEx(string symbol, DateTime time, PriceType priceType, string period, int timeoutInMilliseconds)
        {
            throw new Exception("Not impled");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="includeLevel2"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        public DataHistoryInfo GetQuotesHistoryFiles(string symbol, bool includeLevel2, DateTime time)
        {
            return GetQuotesHistoryFilesEx(symbol, includeLevel2, time, dataFeed_.synchOperationTimeout_);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="includeLevel2"></param>
        /// <param name="time"></param>
        /// <param name="timeoutInMilliseconds"></param>
        /// <returns></returns>
        public DataHistoryInfo GetQuotesHistoryFilesEx(string symbol, bool includeLevel2, DateTime time, int timeoutInMilliseconds)
        {
            throw new Exception("Not impled");
        }

        /// <summary>
        /// Gets meta information file ID for a specified input arguments.
        /// </summary>
        /// <param name="symbol">Can not be null.</param>
        /// <param name="priceType"></param>
        /// <param name="period">Can not be null</param>
        /// <returns></returns>
        public string GetBarsHistoryMetaInfoFile(string symbol, PriceType priceType, string period)
        {
            return GetBarsHistoryMetaInfoFileEx(symbol, priceType, period, dataFeed_.synchOperationTimeout_);
        }

        /// <summary>
        /// Gets meta information file ID for a specified input arguments.
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="priceType"></param>
        /// <param name="period"></param>
        /// <param name="timeoutInMilliseconds">Timeout of the operation in milliseconds.</param>
        /// <returns></returns>
        public string GetBarsHistoryMetaInfoFileEx(string symbol, PriceType priceType, string period, int timeoutInMilliseconds)
        {
            throw new Exception("Not impled");
        }

        /// <summary>
        /// Gets meta information file ID for a specified input arguments.
        /// </summary>
        /// <param name="symbol">Can not be null.</param>
        /// <param name="includeLevel2">False: ticks contains only the best bid/ask prices; true: ticks contains full level2.</param>
        /// <returns></returns>
        public string GetQuotesHistoryMetaInfoFile(string symbol, bool includeLevel2)
        {
            return GetQuotesHistoryMetaInfoFileEx(symbol, includeLevel2, dataFeed_.synchOperationTimeout_);
        }

        /// <summary>
        /// Gets meta information file ID for a specified input arguments.
        /// </summary>
        /// <param name="symbol">Can not be null.</param>
        /// <param name="includeLevel2">False: ticks contains only the best bid/ask prices; true: ticks contains full level2.</param>
        /// <param name="timeoutInMilliseconds">Timeout of the operation in milliseconds.</param>
        /// <returns></returns>
        public string GetQuotesHistoryMetaInfoFileEx(string symbol, bool includeLevel2, int timeoutInMilliseconds)
        {
            throw new Exception("Not impled");
        }

        DataFeed dataFeed_;
    }
}
