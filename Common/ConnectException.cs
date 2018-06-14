namespace TickTrader.FDK.Common
{
    using System;

    public class ConnectException : Exception
    {
        public ConnectException()
        {
        }

        public ConnectException(string text) : base(text)
        {
        }
    }
}