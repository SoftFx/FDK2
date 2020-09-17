namespace TickTrader.FDK.Calculator.Conversion
{
    internal static class FormulaBuilder
    {
        public static IConversionFormula Direct()
        {
            return new NoConvertion();
        }

        public static IConversionFormula Conversion(SymbolMarketNode tracker, FxPriceType side)
        {
            if (side == FxPriceType.Bid)
                return new GetBid() { SrcSymbol = tracker };
            else
                return new GetAsk() { SrcSymbol = tracker };
        }

        public static IConversionFormula InverseConversion(SymbolMarketNode tracker, FxPriceType side)
        {
            if (side == FxPriceType.Bid)
                return new GetInvertedBid() { SrcSymbol = tracker };
            else
                return new GetInvertedAsk() { SrcSymbol = tracker };
        }

        public static IConversionFormula Then(this IConversionFormula formula, SymbolMarketNode tracker, FxPriceType side)
        {
            if (side == FxPriceType.Bid)
                return new MultByBid() { SrcSymbol = tracker, SrcFromula = formula };
            else
                return new MultByAsk() { SrcSymbol = tracker, SrcFromula = formula };
        }

        public static IConversionFormula ThenDivide(this IConversionFormula formula, SymbolMarketNode tracker, FxPriceType side)
        {
            if (side == FxPriceType.Bid)
                return new DivByBid() { SrcSymbol = tracker, SrcFromula = formula };
            else
                return new DivByAsk() { SrcSymbol = tracker, SrcFromula = formula };
        }

        public static IConversionFormula Error(ISymbolInfo symbol, string currency, string accountCurrency)
        {
            var msg = string.Format("Conversion is not possible: {0} -> {1} ({2})", currency, accountCurrency, symbol.Symbol);
            var error = new MisconfigurationError(msg);
            return new ConversionError(error);
        }

        public static IConversionFormula Error(string fromCurrency, string toCurrency)
        {
            var msg = string.Format("Conversion is not possible: {0} -> {1}", fromCurrency, toCurrency);
            var error = new MisconfigurationError(msg);
            return new ConversionError(error);
        }
    }
}
