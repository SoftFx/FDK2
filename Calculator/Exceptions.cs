using System;

namespace TickTrader.FDK.Calculator
{
    public abstract class BusinessLogicException : Exception
    {
        public BusinessLogicException(string msg)
            : base(msg)
        {
        }

        public abstract OrderErrorCode CalcError { get; }
    }

    public class MarketConfigurationException : BusinessLogicException
    {
        public MarketConfigurationException(string msg)
            : base(msg)
        {
        }

        public override OrderErrorCode CalcError { get { return OrderErrorCode.Misconfiguration; } }
    }

    public class OffQuoteException : BusinessLogicException
    {
        public string Symbol { get; private set; }

        public OffQuoteException(string msg, string symbol)
            : base(msg)
        {
            this.Symbol = symbol;
        }

        public OffQuoteException(string symbol)
            : this("Off Quotes: " + symbol, symbol)
        {
        }

        public override OrderErrorCode CalcError { get { return OrderErrorCode.OffQuotes; } }
    }

    public class OffCrossQuoteException : OffQuoteException
    {
        public OffCrossQuoteException(string symbol, FxPriceType ? price = null)
            : base("Off Cross Quotes: " + symbol + CreatePostfix(price), symbol)
        {
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

    public class SymbolNotFoundException : BusinessLogicException
    {
        public string Symbol { get; private set; }

        public SymbolNotFoundException(string symbol)
            : base("Symbol Not Found: " + symbol)
        {
            this.Symbol = symbol;
        }

        public override OrderErrorCode CalcError { get { return OrderErrorCode.Misconfiguration; } }
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
