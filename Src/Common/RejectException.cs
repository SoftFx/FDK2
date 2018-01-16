namespace TickTrader.FDK.Common
{
    using System;

    public class RejectException : Exception
    {
        public RejectException()
        {
            reason_ = RejectReason.None;
        }

        public RejectException(RejectReason reason, string text) : base(text)
        {
            reason_ = reason;
        }

        public RejectReason Reason
        {
            get { return reason_; }
        }

        RejectReason reason_;
    }
}