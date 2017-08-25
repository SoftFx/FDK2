namespace TickTrader.FDK.Calculator.Rounding
{
    interface IPrecisionProvider
    {
        int GetCurrencyPrecision(string currency);
    }
}
