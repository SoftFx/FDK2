namespace TickTrader.FDK.Calculator
{
    using System;

    public interface ISymbolRate
    {
        decimal Ask { get; }
        decimal Bid { get; }
        string Symbol { get; }
        decimal? NullableAsk { get; }
        decimal? NullableBid { get; }
    }
}
