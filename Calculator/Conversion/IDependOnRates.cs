namespace TickTrader.FDK.Calculator.Conversion
{
    using System.Collections.Generic;

    interface IDependOnRates
    {
        IEnumerable<string> DependOnSymbols { get; }
    }
}
