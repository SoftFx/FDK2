using System;

namespace TickTrader.FDK.Calculator.Conversion
{
    public interface IConversionFormula
    {
        decimal Value { get; }
        CalcError Error { get; }

        void AddUsage();
        void RemoveUsage();

        event Action ValChanged;
    }
}
