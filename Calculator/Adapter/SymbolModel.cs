using TickTrader.FDK.Common;

namespace TickTrader.FDK.Calculator.Adapter
{
    public class SymbolModel : ISymbolInfo
    {
        public SymbolModel(SymbolInfo symbol)
        {
            Symbol = symbol.Name;
            ContractSizeFractional = symbol.RoundLot;
            MarginCurrency = symbol.Currency;
            ProfitCurrency = symbol.SettlementCurrency;
            MarginFactorFractional = symbol.MarginFactorFractional ?? 1;
            StopOrderMarginReduction = symbol.StopOrderMarginReduction ?? 1;
            HiddenLimitOrderMarginReduction = symbol.HiddenLimitOrderMarginReduction ?? 1;
            MarginHedged = symbol.MarginHedge;
            MarginMode = symbol.MarginCalcMode;
            Precision = symbol.Precision;
            SwapEnabled = symbol.SwapEnabled;
            SwapType = symbol.SwapType;
            SwapSizeLong = (float) (symbol.SwapSizeLong ?? 0);
            SwapSizeShort = (float) (symbol.SwapSizeShort ?? 0);
            TripleSwapDay = symbol.TripleSwapDay;
            Security = symbol.SecurityName;
            SortOrder = symbol.SortOrder;
        }

        public string Symbol { get; }
        public double ContractSizeFractional { get; }
        public string MarginCurrency { get; }
        public string ProfitCurrency { get; }
        public double MarginFactorFractional { get; }
        public double StopOrderMarginReduction { get; }
        public double HiddenLimitOrderMarginReduction { get; }
        public double MarginHedged { get; }
        public MarginCalcMode MarginMode { get; }
        public int Precision { get; }
        public bool SwapEnabled { get; }
        public SwapType SwapType { get; }
        public float SwapSizeLong { get; }
        public float SwapSizeShort { get; }
        public int TripleSwapDay { get; }
        public string Security { get; }
        public int SortOrder { get; }
    }
}
