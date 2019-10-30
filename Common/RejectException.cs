namespace TickTrader.FDK.Common
{
    using System;

    public class RejectException : Exception
    {
        public RejectException()
        {
            reason_ = RejectReason.None;
            text_ = null;
            clOrdId_ = null;
        }

        public RejectException(RejectReason reason, string text, string clOrdId = null) : base(text)
        {
            reason_ = reason;
            text_ = text;
            clOrdId_ = clOrdId;
        }

        public RejectReason Reason
        {
            get { return reason_; }
        }

        public string ClientOrderId
        {
            get { return clOrdId_; }
        }

        public string Text
        {
            get { return text_; }
        }

        RejectReason reason_;
        private string clOrdId_;
        private string text_;
    }
}