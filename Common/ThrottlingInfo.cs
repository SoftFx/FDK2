namespace TickTrader.FDK.Common
{
    using System.Collections.Generic;

    public enum ThrottlingMethod
    {
        Login,
        TwoFactor,
        SessionInfo,
        Currencies,
        Symbols,
        Ticks,
        Level2,
        Tickers,
        FeedSubscribe,
        QuoteHistory,
        QuoteHistoryCache,
        TradeSessionInfo,
        TradeServerInfo,
        Account,
        Assets,
        Positions,
        Trades,
        TradeCreate,
        TradeModify,
        TradeDelete,
        TradeHistory,
        DailyAccountSnapshots,
        UnknownMethod = 999
    }

    /// <summary>
    /// This class contains the information about throttling limits.
    /// </summary>
    public class ThrottlingInfo
    {
        /// <summary>
        /// Creates a new empty instance of ThrottlingInfo.
        /// </summary>
        public ThrottlingInfo()
        {
        }

        /// <summary>
        /// Gets or sets the allowed amount of sessions for the particular account.
        /// </summary>
        public int? SessionsPerAccount { get; set; }

        /// <summary>
        /// Gets or sets the allowed amount of requests per second for the particular account.
        /// </summary>
        public int? RequestsPerSecond { get; set; }

        /// <summary>
        /// Contains the information about throttling limits for methods.
        /// </summary>
        public List<ThrottlingMethodInfo> ThrottlingMethods { get; set; }
    }

    /// <summary>
    /// This class contains the information about throttling limits for methods.
    /// </summary>
    public class ThrottlingMethodInfo
    {
        /// <summary>
        /// Gets or sets method's name.
        /// </summary>
        public ThrottlingMethod Method { get; set; }

        /// <summary>
        /// Gets or sets  the allowed amount of requests per second for the particular method.
        /// </summary>
        public int? RequestsPerSecond { get; set; }

        /// <summary>
        /// Creates a new empty instance of ThrottlingMethodInfo.
        /// </summary>
        public ThrottlingMethodInfo()
        {
        }
    }
}
