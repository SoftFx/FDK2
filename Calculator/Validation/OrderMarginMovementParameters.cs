using TickTrader.FDK.Common;

namespace TickTrader.FDK.Calculator.Validation
{
    public class OrderMarginMovementParameters
    {
        public OrderType Type { get; set; }
        public OrderSide Side { get; set; }
        public ISymbolInfo Symbol { get; set; }
        public decimal? MarginMovement { get; set; }
    }
}