namespace TickTrader.FDK.Calculator.Validation
{
    public class ModifyOrderRequest
    {
        public string OrderId { get; set; }
        public decimal? AmountChange { get; set; }
        public decimal? MaxVisibleAmount { get; set; }
        public decimal? Price { get; set; }
        public decimal? StopPrice { get; set; }
        public decimal? Slippage { get; set; }
    }
}