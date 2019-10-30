using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TickTrader.FDK.Calculator.Netting
{
    internal class PositionContainer
    {
        private SideNetting parent;

        public PositionContainer(SideNetting parent, IMarginAccountInfo accountInfo, OrderSides side)
        {
            this.parent = parent;
            this.Side = side;
            this.AccountData = accountInfo;
        }

        public OrderCalculator Calculator { get { return parent.Calculator; } }
        public IMarginAccountInfo AccountData { get; private set; }
        public IPositionSide PosRef { get; private set; }

        public bool IsEmpty { get; private set; }
        public OrderSides Side { get; private set; }
        public decimal Amount { get; private set; }
        public decimal Price { get; private set; }
        public decimal Margin { get; private set; }
        public decimal Profit { get; private set; }
        public decimal Commission { get; private set; }
        public decimal AgentCommission { get; private set; }
        public decimal Swap { get; private set; }

        public int InvalidOrdersCount { get; private set; }
        public OrderErrorCode WorstError { get; private set; }

        public void Recalculate(UpdateKind updateKind)
        {
            Recalculate();
        }

        public void Update(IPositionModel position)
        {
            position.Calculator = this.Calculator;

            if (Side == OrderSides.Buy)
            {
                this.Amount = position.Long.Amount;
                this.Price = position.Long.Price;
                this.Swap = position.Swap;
                this.Commission = position.Commission;
                this.AgentCommission = position.AgentCommission;
                this.PosRef = position.Long;
            }
            else
            {
                this.Amount = position.Short.Amount;
                this.Price = position.Short.Price;
                this.PosRef = position.Short;
            }
            Recalculate();
        }

        public void Remove(IPositionModel position)
        {
            this.Amount = 0;
            this.Price = 0;
            this.Commission = 0;
            this.AgentCommission = 0;
            this.Swap = 0;
            this.Margin = 0;
            this.Profit = 0;
        }

        void Recalculate()
        {
            InvalidOrdersCount = 0;

            try
            {
                if (PosRef != null)
                {
                    PosRef.Margin = this.Calculator.CalculateMargin(Amount, AccountData.Leverage, OrderTypes.Position, Side, false);
                    Margin = PosRef.Margin;
                }
            }
            catch (BusinessLogicException ex)
            {
                if (Amount != 0)
                {
                    InvalidOrdersCount = 1;
                    WorstError = ex.CalcError;
                }
            }
            try
            {
                if (PosRef != null)
                {
                    PosRef.Profit = this.Calculator.CalculateProfit(Price, Amount, Side);
                    Profit = PosRef.Profit;
                }
            }
            catch (BusinessLogicException ex)
            {
                if (Amount != 0)
                {
                    InvalidOrdersCount = 1;
                    WorstError = ex.CalcError;
                }
            }
        }
    }
}
