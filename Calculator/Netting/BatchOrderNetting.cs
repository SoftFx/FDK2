using System;
using TickTrader.FDK.Common;

namespace TickTrader.FDK.Calculator.Netting
{
    internal class BacthOrderNetting : IOrderNetting
    {
        public BacthOrderNetting(IMarginAccountInfo accInfo, OrderType type, OrderSide side, bool isHidden)
        {
            AccountData = accInfo;
            Type = type;
            Side = side;
            IsHidden = isHidden;
        }

        public bool IsEmpty { get; private set; } = true;
        public decimal MarginAmount { get; private set; }
        public decimal ProfitAmount { get; private set; }
        public decimal WeightedAveragePrice { get; private set; }
        public decimal TotalWeight { get; private set; }

        public decimal Margin { get; private set; }
        public decimal Profit { get; private set; }
        public int ErrorCount { get; private set; }

        public IMarginAccountInfo AccountData { get; }
        public OrderCalculator Calculator { get; set; }
        public OrderType Type { get; }
        public OrderSide Side { get; }
        public bool IsHidden { get; }

        public CalcError MarginError { get; private set; }
        public CalcError ProfitError { get; private set; }

        public decimal Amount => MarginAmount;

        public event Action<decimal> AmountChanged;

        public StatsChange Recalculate()
        {
            var oldMargin = Margin;
            var oldProfit = Profit;
            var oldErros = ErrorCount;

            ErrorCount = 0;

            if (MarginAmount > 0)
            {
                Margin = Calculator.CalculateMargin(MarginAmount, AccountData.Leverage, Type, Side, IsHidden, out var error);
                MarginError = error;
                if (error != null)
                    ErrorCount++;
            }
            else
                Margin = 0;

            if (ProfitAmount > 0)
            {
                Profit = Calculator.CalculateProfit(WeightedAveragePrice, ProfitAmount, Side, out var error);
                ProfitError = error;
                if (error != null)
                    ErrorCount++;
            }
            else
                Profit = 0;

            return new StatsChange(Margin - oldMargin, Profit - oldProfit, ErrorCount - oldErros);
        }

        public StatsChange AddOrder(IOrderModel order, decimal remAmount, decimal? price)
        {
            AddOrderWithoutCalculation(order, remAmount, price);
            return Recalculate();
        }

        public void AddOrderWithoutCalculation(IOrderModel order, decimal remAmount, decimal? price)
        {
            ChangeMarginAmountBy(remAmount);

            if (Type == OrderType.Position)
            {
                ChangeProfitAmountBy(remAmount);
                TotalWeight += remAmount * (price ?? 0);
                UpdateAveragePrice();
            }
        }

        public void AddPositionWithoutCalculation(IPositionSide posSide, decimal posAmount, decimal posPrice)
        {
            ChangeMarginAmountBy(posAmount);
            ChangeProfitAmountBy(posAmount);
            TotalWeight += posAmount * posPrice;
            UpdateAveragePrice();
        }

        public void RemovePositionWithoutCalculation(decimal posAmount, decimal posPrice)
        {
            ChangeMarginAmountBy(-posAmount);
            ChangeProfitAmountBy(-posAmount);
            TotalWeight -= posAmount * posPrice;
            UpdateAveragePrice();
        }

        public StatsChange RemoveOrder(IOrderModel order, decimal remAmount, decimal? price)
        {
            //Count--;
            ChangeMarginAmountBy(-remAmount);

            if (Type == OrderType.Position)
            {
                ChangeProfitAmountBy(-remAmount);
                TotalWeight -= remAmount * (price ?? 0);
                UpdateAveragePrice();
            }

            return Recalculate();
        }

        public CalcError GetWorstError()
        {
            return CalcError.GetWorst(ProfitError, MarginError);
        }

        private void UpdateAveragePrice()
        {
            if (ProfitAmount > 0)
                WeightedAveragePrice = TotalWeight / ProfitAmount;
            else
                WeightedAveragePrice = 0;
        }

        private void ChangeMarginAmountBy(decimal delta)
        {
            MarginAmount += delta;
            IsEmpty = MarginAmount == 0;

            AmountChanged?.Invoke(delta);
        }

        private void ChangeProfitAmountBy(decimal delta)
        {
            ProfitAmount += delta;
        }

        //private void Order_TypeAmountChanged(TypeAmountChangeArgs obj)
        //{
        //}

        //private void Order_RemAmountChanged(ParamChangeArgs<decimal> obj)
        //{
        //}
    }
}
