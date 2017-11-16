using System;

namespace TickTrader.FDK.Calculator
{
    public interface IPositionModel
    {
        string Symbol { get; }
        decimal Commission { get; }
        decimal AgentCommission { get; }
        decimal Swap { get;  }
        IPositionSide Long { get; } // buy
        IPositionSide Short { get; } //sell
        DateTime? Modified { get; }
        OrderCalculator Calculator { get; set; }
    }

    public interface IPositionSide
    {
        decimal Amount { get; }
        decimal Price { get; }
        decimal Margin { get; set; }
        decimal Profit { get; set; }
    }

    public enum PositionChageTypes
    {
        AddedModified,
        Removed
    }
}
