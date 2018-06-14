namespace TickTrader.FDK.Calculator.Netting
{
    public sealed class OrderLightClone : ICommonOrder
    {
        public OrderLightClone()
        {
        }

        public OrderLightClone(IOrderModel originalOrder)
        {
            this.OrderModelRef = originalOrder;

            this.OrderId = originalOrder.OrderId;
            this.Symbol = originalOrder.Symbol;
            this.ProfitCurrency = originalOrder.ProfitCurrency;
            this.MarginCurrency = originalOrder.MarginCurrency;
            this.Side = originalOrder.Side;
            this.Type = originalOrder.Type;
            this.Amount = originalOrder.Amount;
            this.RemainingAmount = originalOrder.RemainingAmount;
            this.Price = originalOrder.Price;
            this.StopPrice = originalOrder.StopPrice;
            this.Commission = originalOrder.Commission.GetValueOrDefault();
            this.AgentCommission = originalOrder.AgentCommision.GetValueOrDefault();
            this.Swap = originalOrder.Swap.GetValueOrDefault();
            this.Margin = originalOrder.Margin;
            this.IsHidden = originalOrder.IsHidden;
            this.IsIceberg = originalOrder.IsIceberg;
        }

        public IOrderModel OrderModelRef { get; private set; }

        public long OrderId { get; set; }
        public string Symbol { get; set; }
        public string ProfitCurrency { get; set; }
        public string MarginCurrency { get; set; }
        public OrderSides Side { get; set; }
        public OrderTypes Type { get; set; }
        public decimal Amount { get; set; }
        public decimal RemainingAmount { get; set; }
        public bool IsHidden { get; set; }
        public bool IsIceberg { get; }
        public decimal? Price { get; set; }
        public decimal? StopPrice { get; set; }
        public decimal Commission { get; set; }
        public decimal AgentCommission { get; set; }
        public decimal Swap { get; set; }
        public decimal? Margin { get; set; }

        public decimal? OrderPrice => StopPrice ?? Price;
    }
}
