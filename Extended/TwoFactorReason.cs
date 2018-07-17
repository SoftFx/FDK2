namespace TickTrader.FDK.Extended
{
    /// <summary>
    /// Contains possible values of two factor auth reason.
    /// </summary>
    public enum TwoFactorReason
    {
        /// <summary>
        /// Server request two factor auth.
        /// </summary>
        ServerRequest = 1,

        /// <summary>
        /// Server success response two factor auth.
        /// </summary>
        ServerSuccess = 2,

        /// <summary>
        /// Server error response two factor auth.
        /// </summary>
        ServerError = 3,

        /// <summary>
        /// Server resume response two factor session.
        /// </summary>
        ServerResume = 4
    }
}
