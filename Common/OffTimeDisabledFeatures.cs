using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TickTrader.FDK.Common
{
    [Flags]
    public enum OffTimeDisabledFeatures
    {
        None = 0x00,
        //0x01 for modification and extensibility purposes
        //0x02
        QuoteHistory = 0x04,
        Trade = 0x08,
        Feed = 0x10,
        All = QuoteHistory | Trade | Feed
    }
}
