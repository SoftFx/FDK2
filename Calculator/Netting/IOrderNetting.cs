using System;

namespace TickTrader.FDK.Calculator.Netting
{
    internal interface IOrderNetting
    {
        bool IsEmpty { get; }
        OrderCalculator Calculator { get; set; }

        event Action<decimal> AmountChanged;

        StatsChange Recalculate();
        CalcError GetWorstError();
        StatsChange AddOrder(IOrderModel order, decimal remAmount, decimal? price);
        void AddOrderWithoutCalculation(IOrderModel order, decimal remAmount, decimal? price);
        StatsChange RemoveOrder(IOrderModel order, decimal remAmount, decimal? price);
        void AddPositionWithoutCalculation(IPositionSide posSide, decimal posAmount, decimal posPrice);
        void RemovePositionWithoutCalculation(decimal posAmount, decimal posPrice);
        decimal Amount { get; }
    }
}
