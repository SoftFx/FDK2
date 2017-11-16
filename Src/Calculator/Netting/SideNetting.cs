namespace TickTrader.FDK.Calculator.Netting
{
    using System;
    using System.Linq;
    using System.Collections.Generic;

    public sealed class SideNetting
    {
        readonly PositionContainer netPositions;
        readonly NettingContainer grossPositions;
        readonly NettingContainer limitOrders;
        readonly NettingContainer stopOrders;
        readonly NettingContainer stopLimitOrders;
        readonly NettingContainer marketOrders;
        readonly NettingContainer hiddenLimitOrders;
        readonly SymbolNetting parent;

        public SideNetting(SymbolNetting parent, IMarginAccountInfo accInfo, OrderSides side, NettingCalculationTypes calcType)
        {
            this.parent = parent;
            this.netPositions = new PositionContainer(this, accInfo, side);
            this.grossPositions = NettingContainer.Create(this, accInfo, OrderTypes.Position, side, calcType, false);
            this.limitOrders = NettingContainer.Create(this, accInfo, OrderTypes.Limit, side, calcType, false);
            this.stopOrders = NettingContainer.Create(this, accInfo, OrderTypes.Stop, side, calcType, false);
            this.stopLimitOrders = NettingContainer.Create(this, accInfo, OrderTypes.StopLimit, side, calcType, false);
            this.marketOrders = NettingContainer.Create(this, accInfo, OrderTypes.Market, side, calcType, false);
            this.hiddenLimitOrders = NettingContainer.Create(this, accInfo, OrderTypes.Limit, side, calcType, true);
        }

        #region ISideNetting

        public decimal PendingMargin { get { return limitOrders.Margin + stopOrders.Margin + stopLimitOrders.Margin + marketOrders.Margin + hiddenLimitOrders.Margin; } }
        public decimal PositionMargin { get { return grossPositions.Margin + netPositions.Margin; } }

        #endregion

        #region IMarketSummary

        public decimal Margin { get { return this.PositionMargin + this.PendingMargin; } }
        public decimal Profit { get { return grossPositions.Profit + netPositions.Profit; } }
        public decimal Commission { get { return grossPositions.Commission + netPositions.Commission; } }
        public decimal AgentCommission { get { return grossPositions.AgentCommission + netPositions.AgentCommission; } }
        public decimal Swap { get { return grossPositions.Swap + netPositions.Swap; } }

        #endregion

        #region IReportErrors, ICalculable

        public int InvalidOrdersCount { get { return limitOrders.InvalidOrdersCount + stopOrders.InvalidOrdersCount + stopLimitOrders.InvalidOrdersCount + grossPositions.InvalidOrdersCount + netPositions.InvalidOrdersCount + marketOrders.InvalidOrdersCount + hiddenLimitOrders.InvalidOrdersCount; } }
        public bool IsCalculated { get { return InvalidOrdersCount == 0; } }
        public OrderErrorCode WorstError { get { return OrderError.GetWorst(limitOrders.WorstError, stopOrders.WorstError, stopLimitOrders.WorstError, grossPositions.WorstError, netPositions.WorstError, marketOrders.WorstError, hiddenLimitOrders.WorstError); } }

        #endregion

        public bool IsEmpty { get { return netPositions.IsEmpty && grossPositions.IsEmpty && limitOrders.IsEmpty && stopOrders.IsEmpty && stopLimitOrders.IsEmpty && marketOrders.IsEmpty && hiddenLimitOrders.IsEmpty; } }

        public OrderCalculator Calculator
        {
            get { return this.parent.Calculator; }
        }

        internal void AddOrder(OrderLightClone order)
        {
            this.GetRequiredContainer(order.Type, order.IsHidden).AddOrder(order);
        }

        internal bool RemoveOrder(OrderLightClone order)
        {
            return GetRequiredContainer(order.Type, order.IsHidden).RemoveOrder(order.OrderId);
        }

        internal void Update(IPositionModel position)
        {
            netPositions.Update(position);
        }

        internal void Remove(IPositionModel position)
        {
            netPositions.Remove(position);
        }

        NettingContainer GetRequiredContainer(OrderTypes type, bool hidden)
        {
            if (hidden && type == OrderTypes.Limit)
                return hiddenLimitOrders;
            if (type == OrderTypes.Position)
                return grossPositions;
            if (type == OrderTypes.Market)
                return marketOrders;
            if (type == OrderTypes.Limit)
                return limitOrders;
            if (type == OrderTypes.Stop)
                return stopOrders;
            if (type == OrderTypes.StopLimit)
                return stopLimitOrders;

            throw new Exception("Unsupported Order Type: " + type);
        }

        internal void Recalculate(UpdateKind updateKind)
        {
            this.netPositions.Recalculate(updateKind);
            this.grossPositions.Recalculate(updateKind);
            this.limitOrders.Recalculate(updateKind);
            this.stopOrders.Recalculate(updateKind);
            this.stopLimitOrders.Recalculate(updateKind);
            this.marketOrders.Recalculate(updateKind);
            this.hiddenLimitOrders.Recalculate(updateKind);
        }

        public int OrderCount
        {
            get
            {
                return this.grossPositions.OrderCount + this.limitOrders.OrderCount + this.stopOrders.OrderCount + this.stopLimitOrders.OrderCount + this.marketOrders.OrderCount + this.hiddenLimitOrders.OrderCount;
            }
        }

        public IEnumerable<IOrderModel> Orders
        {
            get { return Collection.FromValues(this.grossPositions, this.limitOrders, this.stopOrders, this.stopLimitOrders, this.marketOrders, this.hiddenLimitOrders).SelectMany(o => o.OrderList); }
        }
    }
}
