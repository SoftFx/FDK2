namespace TickTrader.FDK.Calculator.Conversion
{
    using System.Collections.Generic;

    interface IConversionBuilder
    {
        IConversionFormula AsFormula();
        IConversionFormula Then(SymbolRateTracker symbol, FxPriceType side);
        IConversionFormula ThenDivide(SymbolRateTracker symbol, FxPriceType side);
        IConversionBuilder ThenConversion(SymbolRateTracker symbol, FxPriceType side);
        IConversionBuilder ThenDivideConversion(SymbolRateTracker symbol, FxPriceType side);
    }

    class Formulas
    {
        public static readonly Formulas Instance = new Formulas();
        static readonly IConversionFormula NoConversionFormula = new NoConversion();

        Formulas()
        {
        }

        #region Methods

        public IConversionFormula Direct
        {
            get
            {
                return NoConversionFormula;
            }
        }

        public IConversionFormula CreateError(ISymbolInfo symbol, string currency, string accountCurrency)
        {
            return new ErrorConversion(symbol, currency, accountCurrency);
        }

        public IConversionFormula CreateError(string fromCurrency, string accountCurrency)
        {
            return new ErrorConversion(fromCurrency, accountCurrency);
        }

        public IConversionBuilder Conversion(SymbolRateTracker symbol, FxPriceType side)
        {
            return new ConversionBuilder(symbol, false, side);
        }

        public IConversionBuilder InverseConversion(SymbolRateTracker symbol, FxPriceType side)
        {
            return new ConversionBuilder(symbol, true, side);
        }

        #endregion

        #region Formulas & Builder

        class ConversionBuilder : IConversionBuilder
        {
            readonly ComplexConversion conversion;

            public ConversionBuilder(SymbolRateTracker symbol, bool reversed, FxPriceType side)
            {
                conversion = new ComplexConversion(symbol, reversed, side);
            }

            public static implicit operator ComplexConversion(ConversionBuilder builder)
            {
                return builder.conversion;
            }

            public IConversionFormula AsFormula()
            {
                return (ComplexConversion)this;
            }

            public IConversionFormula Then(SymbolRateTracker symbol, FxPriceType side)
            {
                conversion.Last.SetNext(new ComplexConversion(symbol, false, side));
                return conversion;
            }

            public IConversionFormula ThenDivide(SymbolRateTracker symbol, FxPriceType side)
            {
                conversion.Last.SetNext(new ComplexConversion(symbol, true, side));
                return conversion;
            }

            public IConversionBuilder ThenConversion(SymbolRateTracker symbol, FxPriceType side)
            {
                conversion.Last.SetNext(new ComplexConversion(symbol, false, side));
                return this;
            }

            public IConversionBuilder ThenDivideConversion(SymbolRateTracker symbol, FxPriceType side)
            {
                conversion.Last.SetNext(new ComplexConversion(symbol, true, side));
                return this;
            }
        }

        class ErrorConversion : IConversionFormula
        {
            readonly string message;

            public ErrorConversion(ISymbolInfo symbol, string currency, string accountCurrency)
            {
                this.message = string.Format("Conversion is not possible: {0} -> {1} ({2})", currency, accountCurrency, symbol.Symbol);
            }

            public ErrorConversion(string fromCurrency, string accountCurrency)
            {
                this.message = string.Format("Conversion is not possible: {0} -> {1}", fromCurrency, accountCurrency);
            }

            public ErrorConversion(string msg)
            {
                this.message = msg;
            }

            public decimal Value { get { throw new ConversionConfigException(this.message); } }
        }

        class NoConversion : IConversionFormula
        {
            public decimal Value { get { return 1; } }
        }

        class ComplexConversion : IConversionFormula, IDependOnRates
        {
            readonly SymbolRateTracker symbolTracker;
            readonly bool reversed;
            readonly FxPriceType side;
            private ComplexConversion next;

            public ComplexConversion(SymbolRateTracker symbolTracker, bool reversed, FxPriceType side, ComplexConversion next = null)
            {
                this.symbolTracker = symbolTracker;
                this.reversed = reversed;
                this.side = side;
                this.next = next;
            }

            public ComplexConversion Last
            {
                get
                {
                    if (next == null)
                        return this;
                    return next.Last;
                }
            }

            public void SetNext(ComplexConversion next)
            {
                this.next = next;
            }

            public decimal Value
            {
                get
                {
                    decimal result;

                    if (symbolTracker.Rate == null)
                        throw new OffQuoteException(symbolTracker.Symbol);

                    if (side == FxPriceType.Ask)
                    {
                        decimal? ask = symbolTracker.Rate.NullableAsk;

                        if (ask == null || ask.Value == 0)
                            throw new OffCrossQuoteException(symbolTracker.Symbol);

                        result = ask.Value;
                    }
                    else
                    {
                        decimal? bid = symbolTracker.Rate.NullableBid;

                        if (bid == null || bid.Value == 0)
                            throw new OffCrossQuoteException(symbolTracker.Symbol);

                        result = bid.Value;
                    }

                    if (reversed)
                        result = 1 / result;

                    if (next != null)
                        result *= next.Value;

                    return result;
                }
            }

            IEnumerable<string> IDependOnRates.DependOnSymbols
            {
                get
                {
                    yield return this.symbolTracker.Symbol;
                    if (this.next != null)
                        yield return this.next.symbolTracker.Symbol;
                }
            }
        }

        #endregion
    }
}

