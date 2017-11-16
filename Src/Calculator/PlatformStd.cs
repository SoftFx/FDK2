using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TickTrader.FDK.Calculator
{
    public static class PlatformStd
    {
		public const string LogAndJournalDateTimeFormat = "MM/dd/yyyy HH:mm:ss.fff";
        public static readonly Func<DateTime, string> LogDateTimeFormatAction = dt => dt.ToString(LogAndJournalDateTimeFormat);
    }
}
