using System;

namespace TickTrader.FDK.Calculator
{
    public interface ICommonOrder
    {
        long OrderId { get; }
        string Symbol { get; }
        string ProfitCurrency { get; set; }
        string MarginCurrency { get; set; }
        OrderTypes Type { get; set; }
        OrderSides Side { get; set; }
        decimal? Price { get; set; }
        decimal? StopPrice { get; set; }
        decimal Amount { get; set; }
        decimal RemainingAmount { get; set; }
        bool IsHidden { get; }
        bool IsIceberg { get; }
    }

    /// <summary>
    /// Defines methods and properties for order which is subject of market summary calculations.
    /// </summary>
    public interface IOrderModel : ICommonOrder
    {
        decimal? AgentCommision { get; }

        OrderError CalculationError { get; set; }
        OrderCalculator Calculator { get; set; }

        event Action<IOrderModel> EssentialParametersChanged;

        bool IsCalculated { get; }

        decimal? Margin { get; set; }
        decimal? MarginRateCurrent { get; set; }
        decimal? Profit { get; set; }
        decimal? Swap { get; }
        decimal? Commission { get; }
        decimal? CurrentPrice { get; set; }
    }
}
