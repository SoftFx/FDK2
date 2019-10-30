namespace TickTrader.FDK.Common
{
    /// <summary>
    /// Represents two possible accounting types.
    /// </summary>
    public enum AccountType
    {
        /// <summary>
        /// </summary>
        None = -1,

        /// <summary>
        /// Net accounting is similar to bank accounting.
        /// </summary>
        Net = 0,

        /// <summary>
        /// Gross accounting.
        /// </summary>
        Gross = 1,

        /// <summary>
        /// Cash account
        /// </summary>
        Cash = 2
    }
}
