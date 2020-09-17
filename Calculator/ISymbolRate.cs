using TickTrader.FDK.Common;

namespace TickTrader.FDK.Calculator
{
    public interface ISymbolRate
    {
        decimal Ask { get; }
        decimal Bid { get; }
        string Symbol { get; }
        decimal? NullableAsk { get; }
        decimal? NullableBid { get; }
        TickTypes TickType { get; }
    }
}
