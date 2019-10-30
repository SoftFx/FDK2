using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TickTrader.FDK.Common
{
    /// <summary>
    /// Contains qoute history information of a symbol
    /// </summary>
    public class HistoryInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public string Symbol { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public DateTime? AvailFrom { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public DateTime? AvailTo { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string LastTickId { get; set; }

        public override string ToString()
        {
            return $"Symbol = {Symbol}; AvailFrom = {AvailFrom}; AvailTo = {AvailTo}; LastTickId = {LastTickId}";
        }
    }
}
