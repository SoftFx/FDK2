using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TickTrader.FDK.Common;

namespace TickTrader.FDK.Calculator
{
    public interface ISymbolInfo
    {
        string Symbol { get; }
        double ContractSizeFractional { get; }
        string MarginCurrency { get; }
        string ProfitCurrency { get; }
        double MarginFactorFractional { get; }
        double StopOrderMarginReduction { get; }
        double HiddenLimitOrderMarginReduction { get; }
        double MarginHedged { get; }
        MarginCalcMode MarginMode { get; }
        int Precision { get; }
        bool SwapEnabled { get; }
        SwapType SwapType { get; }
        float SwapSizeLong { get; }
        float SwapSizeShort { get; }
        int TripleSwapDay { get; }
        string Security { get; }
        int SortOrder { get; }
        CommissionType CommissionType { get; }
        //CommissionChargeType CommissionChargeType { get; }
        //CommissionChargeMethod CommissionChargeMethod { get; }
        double LimitsCommission { get; }
        double Commission { get; }
        double MinCommission { get; }
        string MinCommissionCurrency { get; }
    }

    public interface ICurrencyInfo
    {
        string Name { get; }
        int Precision { get; }
        int SortOrder { get; }
    }
}

