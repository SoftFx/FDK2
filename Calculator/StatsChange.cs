namespace TickTrader.FDK.Calculator
{
    internal struct StatsChange
    {
        public StatsChange(decimal marginDelta, decimal profitDelta, int errorDelta)
        {
            MarginDelta = marginDelta;
            ProfitDelta = profitDelta;
            ErrorDelta = errorDelta;
        }

        public int ErrorDelta { get; }
        public decimal MarginDelta { get; }
        public decimal ProfitDelta { get; }

        public static StatsChange operator +(StatsChange c1, StatsChange c2)
        {
            return new StatsChange(c1.MarginDelta + c2.MarginDelta, c1.ProfitDelta + c2.ProfitDelta, c1.ErrorDelta + c2.ErrorDelta);
        }
    }
}
