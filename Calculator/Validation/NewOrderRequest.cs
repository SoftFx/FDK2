using TickTrader.FDK.Common;

namespace TickTrader.FDK.Calculator.Validation
{
    public class NewOrderRequest : IOrderCalcInfo
    {
        public string Symbol { get; set; }
        public decimal? Price { get; set; }
        public decimal? StopPrice { get; set; }
        public OrderSide Side { get; set; }
        public OrderType Type { get; set; }
        public OrderType InitialType { get; set; }
        public decimal Amount { get; set; }
        public decimal? MaxVisibleAmount { get; set; }
        public decimal? Slippage { get; set; }
        public bool ImmediateOrCancel { get; set; }

        decimal IOrderCalcInfo.RemainingAmount => Amount;
        bool IOrderCalcInfo.IsHidden => Extensions.IsHiddenOrder(MaxVisibleAmount);
    }
}