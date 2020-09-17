using System;

namespace TickTrader.FDK.Calculator
{
    public interface IPositionModel
    {
        string Symbol { get; }
        decimal Commission { get; }
        decimal Swap { get; }
        IPositionSide Long { get; } // buy
        IPositionSide Short { get; } //sell
        OrderCalculator Calculator { get; set; }
        ISymbolInfo SymbolInfo { get; }
    }

    public interface IPositionSide
    {
        decimal Amount { get; }
        decimal Price { get; }
        decimal Margin { get; set; }
        decimal Profit { get; set; }
    }
}
