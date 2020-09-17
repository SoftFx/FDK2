namespace TickTrader.FDK.Calculator.Rounding
{
    public interface IPrecisionProvider
    {
        int GetCurrencyPrecision(string currency);
    }
}
