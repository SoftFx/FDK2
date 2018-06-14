namespace TickTrader.FDK.Calculator.Netting
{
    using System;
    using System.Linq;
    using System.Collections.Generic;

    abstract class NettingContainer
    {
        readonly IDictionary<long, OrderLightClone> orders = new Dictionary<long, OrderLightClone>();
        readonly SideNetting parent;

        internal static NettingContainer Create(SideNetting parent, IMarginAccountInfo accountInfo, OrderTypes ordType, OrderSides ordSide, NettingCalculationTypes calcType, bool hidden)
        {
            NettingContainer newContainer;

            if (calcType == NettingCalculationTypes.OneByOne)
                newContainer = new BasicNettingContainer(parent);
            else if (calcType == NettingCalculationTypes.Optimized)
                newContainer = new OptimizedNettingContainer(parent, hidden);
            else
                throw new Exception("Unknow Calculation Type: " + calcType);

            newContainer.AccountData = accountInfo;
            newContainer.Type = ordType;
            newContainer.Side = ordSide;

            return newContainer;
        }

        internal NettingContainer(SideNetting parent)
        {
            this.parent = parent;
        }

        public IEnumerable<IOrderModel> OrderList { get { return this.Orders.Values.Select(o => o.OrderModelRef); } }

        protected IDictionary<long, OrderLightClone> Orders
        {
            get { return this.orders; }
        }

        public int OrderCount
        {
            get { return this.orders.Count; }
        }

        public bool IsEmpty { get { return OrderCount != 0; } }

        public OrderCalculator Calculator
        {
            get { return this.parent.Calculator; }
        }

        public IMarginAccountInfo AccountData { get; protected set; }
        public OrderSides Side { get; protected set; }
        public OrderTypes Type { get; protected set; }

        public decimal Margin { get; protected set; }
        public decimal Profit { get; protected set; }
        public decimal Commission { get; protected set; }
        public decimal AgentCommission { get; protected set; }
        public decimal Swap { get; protected set; }

        public int InvalidOrdersCount { get; protected set; }
        public OrderErrorCode WorstError { get; protected set; }

        internal abstract void AddOrder(OrderLightClone order);
        internal abstract bool RemoveOrder(long orderId);
        internal abstract void Recalculate(UpdateKind updateKind);

        protected void ValidateOrder(IOrderModel order)
        {
            if (order.Type != Type)
                throw new InvalidOperationException("Order type (" + order.Type + ") does not match container type (" + Type + ")");

            if (order.Side != Side)
                throw new InvalidOperationException("Order side (" + order.Side + ") does not match container side (" + Side + ")");
        }
    }
}

