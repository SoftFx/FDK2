namespace TickTrader.FDK.Common
{
    using System;

    /// <summary>
    /// This exception indicates that user's request has been rejected by server.
    /// </summary>
    public class ExecutionException : Exception
    {
        public ExecutionException()
        {
            report_ = null;
        }

        public ExecutionException(ExecutionReport report)
        {
            report_ = report;
        }

        public ExecutionReport Report
        {
            get
            {
                return report_;
            }
        }

        public override string Message
        {
            get
            {
                return report_ != null ? report_.Text : null;
            }
        }

        ExecutionReport report_;
    }
}