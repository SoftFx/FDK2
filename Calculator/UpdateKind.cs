namespace TickTrader.FDK.Calculator
{
    public enum UpdateKind
    {
        AccountBalanceChanged,
        AccountLeverageChanged,
        OrderAdded,
        OrderRemoved,
        OrderChanged,
        PositionUpdated,
        PositionRemoved,
        QuoteUpdated,
        SymbolsChanged,
        CurrenciesChanged,
        ClientPoll,
        Unknown,
    }
}
