namespace TickTrader.FDK.Common
{
    using System;

    public class DisconnectException : Exception
    {
        public DisconnectException()
        {
        }

        public DisconnectException(string text) : base(text)
        {
        }
    }
}