using TickTrader.FDK.Calculator.Netting;
using TickTrader.FDK.Common;

namespace TickTrader.FDK.Calculator.Validation
{
    public class MarginDeltaParameters
    {
        public decimal MarginDelta { get; set; }
        public string Symbol { get; set; }
        public OrderSide OrderSide { get; set; }
        public OrderType OrderType { get; set; }
    }
}
