using System;

namespace TickTrader.FDK.Calculator
{
    public abstract class BusinessLogicException : Exception
    {
        public BusinessLogicException(string msg, bool count = true)
            : base(msg)
        {
            if (count)
                _count++;
        }

        public abstract CalcErrorCode CalcError { get; }

        private static volatile int _count;

        public static int GetCount()
        {
            return _count;
        }

        //[Obsolete("We should get rid of exceptions in TradeServer!")]
        public static void ThrowIfError(CalcError error)
        {
            if (error == null)
                return;

            if (error.Code == CalcErrorCode.OffQuotes)
            {
                var offQuoteError = (OffQuoteError)error;
                if (offQuoteError.CausedByCrossSymbol)
                    throw new OffCrossQuoteException(offQuoteError.Symbol, offQuoteError.Side);
                else
                    throw new OffQuoteException(offQuoteError.Symbol, offQuoteError.Side);
            }
            else if (error.Code == CalcErrorCode.Misconfiguration)
            {
                var mError = (MisconfigurationError)error;
                throw new MarketConfigurationException(mError.Description);
            }
        }
    }

    public class MarketConfigurationException : BusinessLogicException
    {
        public MarketConfigurationException(string msg)
            : base(msg)
        {
        }

        public override CalcErrorCode CalcError { get { return CalcErrorCode.Misconfiguration; } }
    }

    public class OffQuoteException : BusinessLogicException
    {
        public string Symbol { get; private set; }

        public OffQuoteException(string msg, string symbol, bool count = true)
            : base(msg, false)
        {
            this.Symbol = symbol;
            if (count)
                _count++;
        }

        public OffQuoteException(string symbol, FxPriceType? price = null)
            : this("Off Quotes: " + symbol + CreatePostfix(price), symbol)
        {
        }

        protected static string CreatePostfix(FxPriceType? price)
        {
            if (!price.HasValue)
                return string.Empty;

            if (price.Value == FxPriceType.Ask)
                return " (Ask)";
            else
                return " (Bid)";
        }

        public override CalcErrorCode CalcError { get { return CalcErrorCode.OffQuotes; } }

        private static volatile int _count;

        public static int GetCount()
        {
            return _count;
        }
    }

    public class OffCrossQuoteException : OffQuoteException
    {
        public OffCrossQuoteException(string symbol, FxPriceType? price = null)
            : base("Off Cross Quotes: " + symbol + CreatePostfix(price), symbol, false)
        {
            _count++;
        }

        private static volatile int _count;

        new public static int GetCount()
        {
            return _count;
        }
    }

    public class SymbolNotFoundException : BusinessLogicException
    {
        public string Symbol { get; private set; }

        public SymbolNotFoundException(string symbol)
            : base("Symbol Not Found: " + symbol)
        {
            this.Symbol = symbol;
        }

        public override CalcErrorCode CalcError { get { return CalcErrorCode.Misconfiguration; } }
    }

    public class SymbolConfigException : MarketConfigurationException
    {
        public SymbolConfigException(string msg)
            : base(msg)
        {
        }
    }

    public class ConversionConfigException : MarketConfigurationException
    {
        public ConversionConfigException(string msg)
            : base(msg)
        {
        }
    }

    public class MarginNotCalculatedException : MarketConfigurationException
    {
        public MarginNotCalculatedException(string msg)
            : base(msg)
        {
        }
    }

    public class NotEnoughMoneyException : Exception
    {
        public NotEnoughMoneyException(string msg)
            : base(msg)
        {
        }
    }
}
