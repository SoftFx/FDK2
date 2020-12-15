namespace TickTrader.FDK.Common
{
    /// <summary>
    /// Trade transaction reason.
    /// </summary>
    public enum TradeTransactionReason
    {
        /// <summary>
        ///
        /// </summary>
        None = -1,

        /// <summary>
        ///
        /// </summary>
        ClientRequest = 0,

        /// <summary>
        ///
        /// </summary>
        PendingOrderActivation = 1,

        /// <summary>
        ///
        /// </summary>
        StopOut = 2,

        /// <summary>
        ///
        /// </summary>
        StopLossActivation = 3,

        /// <summary>
        ///
        /// </summary>
        TakeProfitActivation = 4,

        /// <summary>
        ///
        /// </summary>
        DealerDecision = 5,
        /// <summary>
        ///
        /// </summary>
        Rollover = 6,
        /// <summary>
        ///
        /// </summary>
        DeleteAccount = 7,
        /// <summary>
        ///
        /// </summary>
        Expired = 8,
        /// <summary>
        ///
        /// </summary>
        TransferMoney = 9,
        /// <summary>
        /// 
        /// </summary>
        Split = 10,
        /// <summary>
        /// 
        /// </summary>
        Dividend = 11,
        /// <summary>
        /// 
        /// </summary>
        OneCancelsTheOther
    }
}
