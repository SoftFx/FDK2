namespace TickTrader.FDK.Calculator.Validation
{
    internal class BuySellMargin
    {
        public decimal BuyMargin { get; set; }
        public decimal SellMargin { get; set; }

        public BuySellMargin(decimal buyMargin, decimal sellMargin)
        {
            BuyMargin = buyMargin;
            SellMargin = sellMargin;
        }
    }
}
