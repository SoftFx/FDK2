using System.Collections.Generic;
using TickTrader.FDK.Common;

namespace TickTrader.FDK.Calculator.Adapter
{
    static class CalculatorConvert
    {
        public static ISymbolRate ToSymbolRate(KeyValuePair<string, PriceEntry> price)
        {
            return new SymbolRate(price.Key, price.Value);
        }

        public static ISymbolRate ToSymbolRate(this Quote quote)
        {
            return new SymbolRate(quote.Symbol, new PriceEntry(quote.Bid, quote.Ask, quote.TickType));
        }
    }
}
