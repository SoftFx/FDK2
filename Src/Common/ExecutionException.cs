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
            reports_ = null;
        }

        public ExecutionException(ExecutionReport[] reports)
        {
            reports_ = reports;
        }

        public ExecutionReport[] Reports
        {
            get
            {
                return reports_;
            }
        }

        public override string Message
        {
            get
            {
                if (reports_ != null && reports_.Length > 0)
                {
                    ExecutionReport lastReport = reports_[reports_.Length - 1];

                    return lastReport.Text;
                }

                return null;
            }
        }

        ExecutionReport[] reports_;
    }
}