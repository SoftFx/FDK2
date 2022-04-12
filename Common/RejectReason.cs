﻿namespace TickTrader.FDK.Common
{
    /// <summary>
    /// Possible reject reasons.
    /// </summary>
    public enum RejectReason
    {
        /// <summary>
        /// 
        /// </summary>
        None = -1,

        /// <summary>
        /// Dealer reject.
        /// </summary>
        DealerReject = 0,

        /// <summary>
        /// Unknown symbol.
        /// </summary>
        UnknownSymbol = 1,

        /// <summary>
        /// Trade session is closed.
        /// </summary>
        TradeSessionIsClosed = 2,

        /// <summary>
        /// Order exceeds limit.
        /// </summary>
        OrderExceedsLimit = 3,

        /// <summary>
        /// Off quotes
        /// </summary>
        OffQuotes = 4,

        /// <summary>
        /// You try to use (modify, close, delete etc.) unknown order.
        /// </summary>
        UnknownOrder = 5,

        /// <summary>
        /// Duplicate client order ID.
        /// </summary>
        DuplicateClientOrderId = 6,

        /// <summary>
        /// Unsupported order characteristic.
        /// </summary>
        InvalidTradeRecordParameters = 11,

        /// <summary>
        /// Incorrect quantity.
        /// </summary>
        IncorrectQuantity = 13,

        /// <summary>
        /// Trade Not Allowed.
        /// </summary>
        TradeNotAllowed = 14,

        /// <summary>
        /// 
        /// </summary>
        ThrottlingLimits = 15,

        /// <summary>
        /// 
        /// </summary>
        RequestCancelled = 16,

        /// <summary>
        /// 
        /// </summary>
        InternalServerError = 17,

        /// <summary>
        /// 
        /// </summary>
        CloseOnly = 18,

        /// <summary>
        /// 
        /// </summary>
        LongOnly = 19,

        /// <summary>
        /// Account exceeds orders limit
        /// </summary>
        OrdersLimitExceeded = 20,

        /// <summary>
        /// Unknown error.
        /// </summary>
        Other = 99,
    }
}
