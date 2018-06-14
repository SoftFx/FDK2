namespace TickTrader.FDK.Calculator.Netting
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using TickTrader.FDK.Calculator.Conversion;

    public sealed class SymbolNetting : IDisposable
    {
        private MarketState market;
        private OrderCalculator orderCalculator;
        private readonly IDictionary<long, OrderLightClone> clones = new Dictionary<long, OrderLightClone>();
        private readonly SideNetting sell;
        private readonly SideNetting buy;
        private readonly AccountCalculator accountCalculator;

        #region IMarketSummary

        public decimal Margin { get; private set; }
        public decimal Profit { get { return Sell.Profit + Buy.Profit; } }
        public decimal Commission { get { return Sell.Commission + Buy.Commission; } }
        public decimal AgentCommission { get { return Sell.AgentCommission + Buy.AgentCommission; } }
        public decimal Swap { get { return Sell.Swap + Buy.Swap; } }

        #endregion

        #region IReportErrors, ICalculable

        public int InvalidOrdersCount { get { return Sell.InvalidOrdersCount + Buy.InvalidOrdersCount; } }
        public bool IsCalculated { get { return InvalidOrdersCount == 0; } }
        public OrderErrorCode WorstError { get { return OrderError.GetWorst(Sell.WorstError, Buy.WorstError); } }

        #endregion

        public SymbolNetting(string symbol, AccountCalculator calculator, MarketState market)
        {
            this.Symbol = symbol;
            this.accountCalculator = calculator;

            sell = new SideNetting(this, calculator.Info, OrderSides.Sell, market.RateUpdater.CalculationType);
            buy = new SideNetting(this, calculator.Info, OrderSides.Buy, market.RateUpdater.CalculationType);

            ChangeMarket(market);
        }

        internal event Action<SymbolNetting> Invalidated = delegate { };

        public SideNetting Sell { get { return this.sell; } }
        public SideNetting Buy { get { return this.buy; } }
        public string Symbol { get; private set; }
        public int OrderCount { get; private set; }
        public bool IsEmpty { get { return sell.IsEmpty && buy.IsEmpty; } }

        public OrderCalculator Calculator
        {
            get { return this.orderCalculator; }
        }

        public IEnumerable<IOrderModel> Orders
        {
            get { return Collection.FromValues(this.sell, this.buy).SelectMany(o => o.Orders); }
        }

        public IMarginAccountInfo AccountInfo
        {
            get { return this.accountCalculator.Info; }
        }

        // YZ: TODO: Set this property once when new calc is created
        internal IEnumerable<string> DependOnSymbols
        {
            get
            {
                yield return this.Symbol;
                if (this.orderCalculator == null)
                    yield break;
                IDependOnRates calc = this.orderCalculator;
                foreach (var symbol in calc.DependOnSymbols)
                    yield return symbol;
            }
        }

        public MarketState Market
        {
            get
            {
                return market;
            }
            set
            {
                this.ChangeMarket(value);
            }
        }

        public void ChangeMarket(MarketState market)
        {
            if (this.market != null)
            {
                this.market.RateUpdater.Unregister(this);
                this.market.SymbolsChanged -= this.OnSymbolsChanged;
            }

            if (market != null)
            {
                this.market = market;

                this.RecreateCalculator();

                market.RateUpdater.Register(this);
                market.SymbolsChanged += this.OnSymbolsChanged;
            }
        }

        public void Update(IPositionModel position)
        {
            sell.Update(position);
            buy.Update(position);

            this.UpdateMargin();
        }

        public void Remove(IPositionModel position)
        {
            sell.Remove(position);
            buy.Remove(position);

            this.UpdateMargin();
        }

        public void AddOrder(IOrderModel order)
        {
            var clone = AddClone(order);

            order.Calculator = this.orderCalculator;
            this.orderCalculator.UpdateOrder(order, this.AccountInfo);
            this.GetRequiredContainer(order.Side).AddOrder(clone);

            order.EssentialParametersChanged += this.ReplaceOrder;
            this.OrderCount++;

            this.UpdateMargin();
        }

        public void ReplaceOrder(IOrderModel order)
        {
            var oldClone = GetCloneOrThrow(order.OrderId);
            var oldContainer = this.GetRequiredContainer(oldClone.Side);
            oldContainer.RemoveOrder(oldClone);
            oldClone.OrderModelRef.EssentialParametersChanged -= this.ReplaceOrder;

            var newClone = ReplaceClone(order);
            this.orderCalculator.UpdateOrder(order, this.AccountInfo);
            var newContainer = GetRequiredContainer(newClone.Side);
            newContainer.AddOrder(newClone);
            newClone.OrderModelRef.EssentialParametersChanged += this.ReplaceOrder;

            this.UpdateMargin();
            this.accountCalculator.UpdateSummary(UpdateKind.OrderChanged);
        }

        public void RemoveOrder(long orderId)
        {
            var clone = GetCloneOrThrow(orderId);

            if (this.GetRequiredContainer(clone.Side).RemoveOrder(clone))
            {
                clones.Remove(orderId);
                clone.OrderModelRef.EssentialParametersChanged -= this.ReplaceOrder;
                this.OrderCount--;
                this.UpdateMargin();
            }
            else
                throw new InvalidOperationException("Cannot remove order #" + orderId + " from container!");
        }

        private OrderLightClone AddClone(IOrderModel order)
        {
            var clone = new OrderLightClone(order);
            clones.Add(clone.OrderId, clone);
            return clone;
        }

        private OrderLightClone ReplaceClone(IOrderModel order)
        {
            var clone = new OrderLightClone(order);
            clones[clone.OrderId] = clone;
            return clone;
        }

        private OrderLightClone GetCloneOrThrow(long orderId)
        {
            OrderLightClone clone;
            if (!clones.TryGetValue(orderId, out clone))
                throw new InvalidOperationException("Cannot find order with id=#" + orderId + " in netting container.");
            return clone;
        }

        private SideNetting GetRequiredContainer(OrderSides side)
        {
            if (side == OrderSides.Buy)
                return buy;
            else if (side == OrderSides.Sell)
                return sell;
            else
                throw new InvalidOperationException("Unknown order side: " + side);
        }

        public void Recalculate(UpdateKind updateKind)
        {
            sell.Recalculate(updateKind);
            buy.Recalculate(updateKind);

            UpdateMargin();
            this.accountCalculator.UpdateSummary(updateKind);
        }

        void UpdateMargin()
        {
            decimal sellMargin;
            decimal buyMargin;

            if (this.AccountInfo.AccountingType == AccountingTypes.Gross)
            {
                buyMargin = this.buy.Margin;
                sellMargin = this.sell.Margin;
            }
            else
            {
                buyMargin = this.buy.PendingMargin;
                sellMargin = this.sell.PendingMargin;

                if (this.buy.PositionMargin > this.sell.PositionMargin)
                    buyMargin += this.buy.PositionMargin - this.sell.PositionMargin;
                else if (this.sell.PositionMargin > this.buy.PositionMargin)
                    sellMargin += this.sell.PositionMargin - this.buy.PositionMargin;
            }

            var hedge = orderCalculator.SymbolInfo != null ? (decimal)orderCalculator.SymbolInfo.MarginHedged : 0.5M;

            Margin = Math.Max(sellMargin, buyMargin) + (2 * hedge - 1) * Math.Min(sellMargin, buyMargin);
        }

        public void Dispose()
        {
            this.ChangeMarket(null);
        }

        void OnSymbolsChanged()
        {
            this.market.RateUpdater.Unregister(this);
            this.RecreateCalculator();
            this.market.RateUpdater.Register(this);
            this.Recalculate(UpdateKind.SymbolsChanged);
        }

        void RecreateCalculator()
        {
            this.orderCalculator = this.market.GetCalculator(Symbol, this.AccountInfo.BalanceCurrency);
            foreach (var order in this.Orders)
                order.Calculator = this.orderCalculator;
        }
    }
}
