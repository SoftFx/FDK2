using TickTrader.FDK.Common;

namespace TickTrader.FDK.Calculator.Adapter
{
    public class CurrencyModel : ICurrencyInfo
    {
        public const int DefaultPrecision = 2;

        public CurrencyModel(CurrencyInfo currency)
        {
            Name = currency.Name;
            Precision = currency.Precision;
            SortOrder = currency.SortOrder;
        }

        public string Name { get; }
        public int Precision { get; }
        public int SortOrder { get; }
    }
}
