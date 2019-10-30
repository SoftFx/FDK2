namespace TickTrader.FDK.Calculator.Netting
{
    using System;

    class BasicNettingContainer : NettingContainer
    {
        public BasicNettingContainer(SideNetting parent)
            : base(parent)
        {
        }

        void UpdateSummary()
        {
            this.Margin = 0;
            this.Profit = 0;
            this.Commission = 0;
            this.AgentCommission = 0;
            this.Swap = 0;

            foreach (var clone in this.Orders.Values)
                this.AddToSummary(clone);
        }

        void AddToSummary(OrderLightClone clone)
        {
            this.Margin += clone.OrderModelRef.Margin.GetValueOrDefault();
            this.Profit += clone.OrderModelRef.Profit.GetValueOrDefault();
            if (!clone.OrderModelRef.Margin.HasValue || !clone.OrderModelRef.Profit.HasValue)
            {
                InvalidOrdersCount++;
            }

            this.Commission += clone.Commission;
            this.AgentCommission += clone.AgentCommission;
            this.Swap += clone.Swap;
        }

        void RemoveFromSummary(OrderLightClone clone)
        {
            if (clone.OrderModelRef.IsCalculated)
            {
                this.Margin -= clone.OrderModelRef.Margin.GetValueOrDefault();
                this.Profit -= clone.OrderModelRef.Profit.GetValueOrDefault();
            }
            else
                this.InvalidOrdersCount--;

            this.Commission -= clone.Commission;
            this.AgentCommission -= clone.AgentCommission;
            this.Swap -= clone.Swap;
        }

        void CalculateOrders()
        {
            foreach (var clone in this.Orders.Values)
                this.Calculator.UpdateOrder(clone.OrderModelRef, this.AccountData);
        }

        internal override void Recalculate(UpdateKind updateKind)
        {
            this.CalculateOrders();
            this.UpdateSummary();
        }

        internal override void AddOrder(OrderLightClone order)
        {
            this.Orders.Add(order.OrderId, order);
            this.AddToSummary(order);
        }

        internal override bool RemoveOrder(long orderId)
        {
            OrderLightClone clone;

            if (this.Orders.TryGetValue(orderId, out clone))
            {
                this.Orders.Remove(orderId);

                if (this.Orders.Count > 0)
                    this.RemoveFromSummary(clone);
                else
                    this.UpdateSummary();

                return true;
            }

            return false;
        }
    }
}
