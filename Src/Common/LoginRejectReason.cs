namespace TickTrader.FDK.Common
{
    /// <summary>
    /// Possible login reject reasons.
    /// </summary>
    public enum LoginRejectReason
    {
        /// <summary>
        /// 
        /// </summary>
        None,

        /// <summary>
        /// Invalid username and/or password.
        /// </summary>
        InvalidCredentials,

        /// <summary>
        /// Your account is blocked.
        /// </summary>
        BlockedAccount,

        /// <summary>
        /// Throttling limits exceeded
        /// </summary>
        Throttling,

        /// <summary>
        /// Internal server error
        /// </summary>
        InternalServerError,

        /// <summary>
        /// Other
        /// </summary>
        Other
    }
}
