namespace TickTrader.FDK.Calculator
{
    internal struct StatsChange
    {
        public StatsChange(decimal marginDelta, decimal profitDelta, int errorDelta, bool isErrorChanged)
        {
            MarginDelta = marginDelta;
            ProfitDelta = profitDelta;
            ErrorDelta = errorDelta;
            IsErrorChanged = isErrorChanged;
        }

        public int ErrorDelta { get; }
        public decimal MarginDelta { get; }
        public decimal ProfitDelta { get; }
        public bool IsErrorChanged { get; }

        public static StatsChange operator +(StatsChange c1, StatsChange c2)
        {
            return new StatsChange(c1.MarginDelta + c2.MarginDelta, c1.ProfitDelta + c2.ProfitDelta, c1.ErrorDelta + c2.ErrorDelta, c1.IsErrorChanged | c2.IsErrorChanged);
        }
    }
}
