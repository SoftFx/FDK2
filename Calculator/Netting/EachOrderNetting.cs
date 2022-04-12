using System;
using System.Collections.Generic;
using TickTrader.FDK.Common;

namespace TickTrader.FDK.Calculator.Netting
{
    internal class EachOrderNetting : IOrderNetting
    {
        private Dictionary<string, IOrderModel> _ordersById = new Dictionary<string, IOrderModel>();

        public EachOrderNetting(IMarginAccountInfo accInfo, OrderSide side)
        {
            AccountData = accInfo;
            Side = side;
        }

        public bool IsEmpty { get; private set; }

        public IMarginAccountInfo AccountData { get; }
        public OrderSide Side { get; }
        public OrderCalculator Calculator { get; set; }

        public IPositionSide NetPosSide { get; private set; }

        public decimal NetPosMargin
        {
            get { return NetPosSide?.Margin ?? 0; }
            private set
            {
                if (NetPosSide != null)
                {
                    NetPosSide.Margin = value;
                }
            }
        }
        public decimal NetPosProfit
        {
            get { return NetPosSide?.Profit ?? 0; }
            private set
            {
                if (NetPosSide != null)
                {
                    NetPosSide.Profit = value;
                }
            }
        }
        public decimal NetPosAmount { get; private set; }
        public decimal NetPosPrice { get; private set; }
        public decimal TotalAmount { get; private set; }
        public CalcError NetPosCallculationError { get; private set; }
        public CalcError WorstOrderError { get; private set; }

        public decimal Amount => TotalAmount;

        public event Action<decimal> AmountChanged;

        public CalcError GetWorstError()
        {
            return CalcError.GetWorst(NetPosCallculationError, WorstOrderError);
        }

        public StatsChange Recalculate()
        {
            WorstOrderError = null;

            var change = new StatsChange();

            foreach (var order in _ordersById.Values)
            {
                CalcError error;
                change += RecalculateOrder(order, out error);
                WorstOrderError = CalcError.GetWorst(WorstOrderError, error);
            }

            change += RecalculateNetPos();

            return change;
        }

        private StatsChange RecalculateOrder(IOrderModel order, out CalcError worstError)
        {
            var oldMargin = order.Margin;
            var oldProfit = order.Profit;
            var oldErrorCount = GetErrorCount(order);
            var oldError = order.CalculationError;

            worstError = null;

            if (order.RemainingAmount > 0)
            {
                CalcError error;
                order.Margin = Calculator.CalculateMargin(order, AccountData.Leverage, out error);
                worstError = error;
            }
            else
                order.Margin = 0;

            if (order.Type == OrderType.Position)
            {
                CalcError error;
                order.Profit = Calculator.CalculateProfit(order, out error);
                worstError = CalcError.GetWorst(worstError, error);
            }
            else
                order.Profit = 0;

            order.CalculationError = worstError;

            var newErrorCount = GetErrorCount(order);

            return new StatsChange(order.Margin - oldMargin, order.Profit - oldProfit, newErrorCount - oldErrorCount, oldError != order.CalculationError);
        }

        private StatsChange RecalculateNetPos()
        {
            var oldMargin = NetPosMargin;
            var oldProfit = NetPosProfit;
            var oldErrorCount = GetErrorCount(NetPosCallculationError);
            var oldError = NetPosCallculationError;

            NetPosCallculationError = null;

            if (NetPosAmount == 0)
            {
                NetPosMargin = 0;
                NetPosProfit = 0;
            }
            else
            {
                CalcError error;
                NetPosMargin = Calculator.CalculateMargin(NetPosAmount, AccountData.Leverage, OrderType.Position, Side, false, false, out error);
                NetPosCallculationError = error;
                NetPosProfit = Calculator.CalculateProfit(NetPosPrice, NetPosAmount, Side, out error);
                NetPosCallculationError = CalcError.GetWorst(NetPosCallculationError, error);
            }

            var newErrorCount = GetErrorCount(NetPosCallculationError);

            return new StatsChange(NetPosMargin - oldMargin, NetPosProfit - oldProfit, newErrorCount - oldErrorCount, oldError != NetPosCallculationError);
        }

        private int GetErrorCount(CalcError error)
        {
            if (error != null)
                return 1;
            return 0;
        }

        private int GetErrorCount(IOrderModel order)
        {
            return GetErrorCount(order.CalculationError);
        }

        public StatsChange AddOrder(IOrderModel order, decimal remAmount, decimal? price)
        {
            AddOrderWithoutCalculation(order, remAmount, price);
            CalcError error;
            var result = RecalculateOrder(order, out error);
            WorstOrderError = CalcError.GetWorst(WorstOrderError, error);
            return result;
        }

        public void AddOrderWithoutCalculation(IOrderModel order, decimal remAmount, decimal? price)
        {
            order.Profit = 0;
            order.Margin = 0;
            ChangeTotalAmountBy(remAmount);
            _ordersById.Add(order.OrderId, order);
        }

        public StatsChange RemoveOrder(IOrderModel order, decimal remAmount, decimal? price)
        {
            _ordersById.Remove(order.OrderId);
            ChangeTotalAmountBy(-remAmount);
            return new StatsChange(-order.Margin, -order.Profit, -GetErrorCount(order), false);
        }

        public void AddPositionWithoutCalculation(IPositionSide posSide, decimal posAmount, decimal posPrice)
        {
            NetPosSide = posSide;
            NetPosAmount = posAmount;
            NetPosPrice = posPrice;
            ChangeTotalAmountBy(NetPosAmount);
        }

        public void RemovePositionWithoutCalculation(decimal posAmount, decimal posPrice)
        {
            var oldAmount = NetPosAmount;
            NetPosSide = null;
            NetPosAmount = 0;
            NetPosPrice = 0;
            ChangeTotalAmountBy(-oldAmount);
        }

        private void ChangeTotalAmountBy(decimal delta)
        {
            TotalAmount += delta;
            IsEmpty = TotalAmount == 0;

            AmountChanged?.Invoke(delta);
        }
    }
}
