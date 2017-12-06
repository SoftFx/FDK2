using System;

namespace TickTrader.FDK.Calculator.Netting
{
    internal class OptimizedNettingContainer : NettingContainer
    {
        private bool _hidden;

        public int TotalCount { get; protected set; }
        public decimal TotalAmount { get; protected set; }
        public decimal WeightedAveragePrice { get; protected set; }
        public decimal TotalWeight { get; private set; }

        public OptimizedNettingContainer(SideNetting parent)
            : base(parent)
        {
        }

        public OptimizedNettingContainer(SideNetting parent, bool hidden) : this(parent)
        {
            _hidden = hidden;
        }

        internal override void Recalculate(UpdateKind updateKind)
        {
            this.Recalculate();
        }

        void Recalculate()
        {
            InvalidOrdersCount = 0;

            try
            {
                Margin = this.Calculator.CalculateMargin(TotalAmount, AccountData.Leverage, Type, Side, _hidden);
                if (Type == OrderTypes.Position && TotalAmount != 0)
                    Profit = this.Calculator.CalculateProfit(WeightedAveragePrice, TotalAmount, Side);
                else
                    Profit = 0;
            }
            catch (BusinessLogicException ex)
            {
                InvalidOrdersCount = TotalCount;
                WorstError = ex.CalcError;
            }
        }

        internal override void AddOrder(OrderLightClone order)
        {
            Orders.Add(order.OrderId, order);
            AddToTotals(order);
            Recalculate();
        }

        internal override bool RemoveOrder(long orderId)
        {
            OrderLightClone order;

            if (this.Orders.TryGetValue(orderId, out order))
            {
                Orders.Remove(orderId);
                RemoveFromTotals(order);
                Recalculate();
                return true;
            }

            return false;
        }

        private void AddToTotals(OrderLightClone order)
        {
            TotalCount++;
            TotalAmount += order.RemainingAmount;
            Swap += order.Swap;
            Commission += order.Commission;
            AgentCommission += order.AgentCommission;

            if (Type == OrderTypes.Position)
            {
                TotalWeight += order.RemainingAmount * order.OrderPrice.GetValueOrDefault();
                UpdateAveragePrice();
            }
        }

        private void RemoveFromTotals(OrderLightClone order)
        {
            TotalCount--;
            TotalAmount -= order.RemainingAmount;
            Swap -= order.Swap;
            Commission -= order.Commission;
            AgentCommission -= order.AgentCommission;

            if (Type == OrderTypes.Position)
            {
                TotalWeight -= order.RemainingAmount * order.OrderPrice.GetValueOrDefault();
                UpdateAveragePrice();
            }
        }

        private void UpdateAveragePrice()
        {
            if (TotalAmount > 0)
                WeightedAveragePrice = TotalWeight / TotalAmount;
            else
                WeightedAveragePrice = 0;
        }
    }
}