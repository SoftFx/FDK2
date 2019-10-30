using System;

namespace TickTrader.FDK.Common
{
    public class Dividend
    {
        public long Id { get; set; }
        public string Symbol { get; set; }
        public DateTime Time { get; set; }
        public double GrossRate { get; set; }
        public double Fee { get; set; }

        public override string ToString()
        {
            return $"#{Id}; Symbol = {Symbol}; Time = {Time:u}; GrossRate = {GrossRate:F}; Fee = {Fee:P}";
        }
    }
}
