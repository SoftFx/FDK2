using System;

namespace TickTrader.FDK.Extended
{
    /// <summary>
    /// Two factor auth details.
    /// </summary>
    public class TwoFactorAuth
    {
        /// <summary>
        /// Two factor auth reason.
        /// </summary>
        public TwoFactorReason Reason { get; set; }

        /// <summary>
        /// Two factor auth details.
        /// Reason == TwoFactorReason.ServerError
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Two factor auth session expiration time.
        /// Reason == TwoFactorReason.ServerSuccess || Reason == TwoFactorReason.ServerResume
        /// </summary>
        public DateTime Expire { get; set; }
    }
}
