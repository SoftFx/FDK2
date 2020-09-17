using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TickTrader.FDK.Common
{
    public class MergerAndAcquisition
    {
        public Guid Id { get; set; }
        public Dictionary<string, string> Values { get; set; }
        public override string ToString()
        {
            return $"Id={Id}; {string.Join("; ", Values.Select(it => $"{it.Key}={it.Value}"))}";
        }
    }
}
