using System;
using System.Collections.Generic;

namespace TickTrader.FDK.Common
{
    public class Split
    {
        public long Id { get; set; }
        public DateTime StartTime { get; set; }
        public double Ratio { get; set; }
        public List<string> Symbols { get; set; }
        public List<string> Currencies { get; set; }
        public List<string> SymbolsNotAffectQH { get; set; }
        public double FromFactor { get; set; }
        public double ToFactor { get; set; }

        public override string ToString()
        {
            return $"#{Id}; StartTime = {StartTime:u}; Ratio = {Ratio:F}; Currencies = [{string.Join(",", Currencies)}]; Symbols = [{string.Join(",", Symbols)}]; SymbolsNotAffectQH = [{string.Join(",", SymbolsNotAffectQH)}]; FromFactor = {FromFactor}; ToFactor = {ToFactor}";
        }
    }
}
