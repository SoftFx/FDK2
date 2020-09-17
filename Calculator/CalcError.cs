namespace TickTrader.FDK.Calculator
{
    ///// <summary>
    ///// Defines codes for possible calculation errors.
    ///// Codes go in order of severity.
    ///// </summary>
    public enum CalcErrorCode
    {
        /// <summary>
        /// No error.
        /// </summary>
        None,                   // green

        /// <summary>
        /// Quote is missing.
        /// </summary>
        OffQuotes,              // yellow

        /// <summary>
        /// Configuration is incorrect.
        /// </summary>
        Misconfiguration       // red
    }

    /// <summary>
    /// Represents order calculation error.
    /// </summary>
    public abstract class CalcError
    {
        public CalcError(CalcErrorCode code, string message)
        {
            Code = code;
            Description = message;
        }

        /// <summary>
        /// Gets order calculation error code.
        /// </summary>
        public CalcErrorCode Code { get; }

        /// <summary>
        /// Gets order calculatiion error description.
        /// </summary>
        public string Description { get; }

        internal static CalcError GetWorst(CalcError e1, CalcError e2)
        {
            if (e1 == null)
                return e2;

            if (e2 == null)
                return e1;

            if (e1.Code >= e2.Code)
                return e1;

            return e2;
        }

        public override string ToString()
        {
            return $"{Code}. {Description}";
        }
    }

    public class MisconfigurationError : CalcError
    {
        public MisconfigurationError(string msg) : base(CalcErrorCode.Misconfiguration, msg)
        {
        }
    }

    public class OffQuoteError : CalcError
    {
        public OffQuoteError(bool cross, string symbol, FxPriceType? side = null)
            : base(CalcErrorCode.OffQuotes, CreateMessage(cross, symbol, side))
        {
            Symbol = symbol;
            Side = side;
            CausedByCrossSymbol = cross;
        }

        public bool CausedByCrossSymbol { get; }
        public string Symbol { get; }
        public FxPriceType? Side { get; }

        private static string CreateMessage(bool cross, string symbol, FxPriceType? side)
        {
            if (cross)
                return "Off Cross Quotes: " + symbol + CreatePostfix(side);
            return "Off Quotes: " + symbol + CreatePostfix(side);
        }

        private static string CreatePostfix(FxPriceType? price)
        {
            if (!price.HasValue)
                return string.Empty;

            if (price.Value == FxPriceType.Ask)
                return " (Ask)";
            else
                return " (Bid)";
        }
    }
}
