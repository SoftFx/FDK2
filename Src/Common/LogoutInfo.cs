namespace TickTrader.FDK.Common
{
    public class LogoutInfo
    {
        public LogoutReason Reason;
        public string Message;

        public override string ToString() { return string.Format("Reason={0}, Message={1}", Reason, Message); }
    }
}