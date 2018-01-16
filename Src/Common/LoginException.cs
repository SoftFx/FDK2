namespace TickTrader.FDK.Common
{
    using System;

    public class LoginException : Exception
    {
        public LoginException()
        {
            logoutReason_ = LogoutReason.None;
        }

        public LoginException(LogoutReason logoutReason, string text) : base(text)
        {
            logoutReason_ = logoutReason;
        }

        public LogoutReason LogoutReason
        {
            get { return logoutReason_; }
        }

        LogoutReason logoutReason_;
    }
}