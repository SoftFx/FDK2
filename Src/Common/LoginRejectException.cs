namespace TickTrader.FDK.Common
{
    using System;

    public class LoginRejectException : Exception
    {
        public LoginRejectException()
        {
            reason_ = LoginRejectReason.None;
        }

        public LoginRejectException(LoginRejectReason reason, string text) : base(text)
        {
            reason_ = reason;
        }

        public LoginRejectReason Reason
        {
            get { return reason_; }
        }

        LoginRejectReason reason_;
    }
}