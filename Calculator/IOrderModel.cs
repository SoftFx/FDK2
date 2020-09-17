using System;
using TickTrader.FDK.Common;

namespace TickTrader.FDK.Calculator
{
    public interface IOrderCalcInfo
    {
        string Symbol { get; }
        decimal? Price { get; }
        decimal? StopPrice { get; }
        OrderSide Side { get; }
        OrderType Type { get; }
        decimal RemainingAmount { get; }
        decimal Commission { get; }
        decimal Swap { get; }
        bool IsHidden { get; }
    }

    /// <summary>
    /// Defines methods and properties for order which is subject of market summary calculations.
    /// Properties Profit, Margin and CalculationError are updated only in NettingCalculationTypes.OneByOne mode.
    /// </summary>
    public interface IOrderModel : IOrderCalcInfo
    {
        string OrderId { get; }
        OrderCalculator Calculator { get; set; }

        decimal CashMargin { get; set; }
        ISymbolInfo SymbolInfo { get; }

        decimal Profit { get; set; }
        decimal Margin { get; set; }
        CalcError CalculationError { get; set; }

        event Action<OrderEssentialsChangeArgs> EssentialsChanged;
        event Action<OrderPropArgs<decimal>> SwapChanged;
        event Action<OrderPropArgs<decimal>> CommissionChanged;
    }

    public struct OrderEssentialsChangeArgs
    {
        public OrderEssentialsChangeArgs(IOrderModel order, decimal oldRemAmount, decimal? oldPrice, decimal? oldStopPrice, OrderType oldType, bool oldIsHidden)
        {
            Order = order;
            OldRemAmount = oldRemAmount;
            OldPrice = oldPrice;
            OldStopPrice = oldStopPrice;
            OldType = oldType;
            OldIsHidden = oldIsHidden;
        }

        public IOrderModel Order { get; }
        public decimal OldRemAmount { get; }
        public decimal? OldPrice { get; }
        public decimal? OldStopPrice { get; }
        public OrderType OldType { get; }
        public bool OldIsHidden { get; }
    }

    public struct OrderPropArgs<T>
    {
        public OrderPropArgs(IOrderModel order, T oldVal, T newVal)
        {
            Order = order;
            OldVal = oldVal;
            NewVal = newVal;
        }

        public IOrderModel Order { get; }
        public T OldVal { get; }
        public T NewVal { get; }
    }
}
