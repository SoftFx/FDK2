namespace TickTrader.FDK.Common
{
    using System;

    public class TimeoutException : Exception
    {
        public TimeoutException()
        {
        }

        public TimeoutException(string text) : base(text)
        {
        }
    }
}