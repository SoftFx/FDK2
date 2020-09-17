using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TickTrader.FDK.Common
{
    /// <summary>
    /// stock event quote history modifier
    /// </summary>
    public class SEQHModifier
    {
        public long Id { get; set; }
        public DateTime StartTime { get; set; }
        public double Ratio { get; set; }
        public double FromFactor { get; set; }
        public double ToFactor { get; set; }
    }
}
