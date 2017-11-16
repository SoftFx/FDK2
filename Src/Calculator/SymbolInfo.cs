using System;
using System.Runtime.Serialization;

namespace TickTrader.FDK.Calculator
{
    /// <summary>
    /// Margin calculation modes
    /// </summary>
    public enum MarginCalculationModes
    {
        Forex,
        CFD,
        Futures,
        CFD_Index,
        CFD_Leverage
    }

    /// <summary>
    /// Profit calculation modes
    /// </summary>
    public enum ProfitCalculationModes
    {
        Forex,
        CFD,
        Futures,
        CFD_Index,
        CFD_Leverage
    }

    public enum CommissionValueType
    {
        Money = 0,
        Points = 1,
        Percentage = 2
    }

    public enum CommissionChargeType
    {
        PerLot = 0,
        PerDeal = 1
    }

    /// <summary>
    /// Quotes write modes
    /// </summary>
    [Flags]
    public enum QuotesWriteModes
    {
        None = 0x00,
        Bars = 0x01,
        Ticks = 0x02,
        TicksLevel2 = 0x04,
        BarsAndTicks = Bars | Ticks,
        All =  BarsAndTicks | TicksLevel2
    }

    public enum SwapType
    {
        Points,
        PercentPerYear,
    }

    /// <summary>
    /// Represents a symbol
    /// </summary>
    [DataContract]
//    [ProtoContract]
//    [ProtoInclude(100, typeof(GroupSymbolInfo))]
    class SymbolInfo : ISymbolInfo, IExtensibleDataObject
    {
        /// <summary>
        /// Gets or sets a unique ID for this SymbolInfo instance.
        /// </summary>
        [DataMember]
        public short Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the symbol.
        /// </summary>
        [DataMember]
        public String Symbol { get; set; }

        /// <summary>
        /// Gets or sets the name of security of the symbol.
        /// </summary>
        [DataMember]
        public string Security { get; set; }

        /// <summary>
        /// Gets or sets the precision of the symbol.
        /// </summary>
        [DataMember]
        public int Precision { get; set; }

        /// <summary>
        /// Gets or sets whether trade is allowed for the symbol or not.
        /// </summary>
        [DataMember]
        public bool TradeIsAllowed { get; set; }

        /// <summary>
        /// Gets or sets the mode of margin calculation.
        /// Supported values:
        ///   Forex
        ///   CFD
        ///   Futures
        ///   CFD_Index
        ///   CFD_Leverage
        /// </summary>
        [DataMember]
        public WEnum<MarginCalculationModes> MarginMode { get; set; }
        MarginCalculationModes ISymbolInfo.MarginMode { get { return MarginMode.Value; } }

        /// <summary>
        /// Gets or sets the mode of profit calculation.
        /// Supported values:
        ///   Forex
        ///   CFD
        ///   Futures
        ///   CFD_Index
        ///   CFD_Leverage
        /// </summary>
        [DataMember]
        public WEnum<ProfitCalculationModes> ProfitMode { get; set; }

        /// <summary>
        /// Gets or sets the mode of writing quotes.
        /// Supported values:
        ///   None = 0x00
        ///   Bars = 0x01
        ///   Ticks = 0x02
        ///   TicksLevel2 = 0x04
        ///   BarsAndTicks = Bars|Ticks
        ///   All = TicksLevel2|BarsAndTicks
        /// </summary>
        [DataMember]
        public WEnum<QuotesWriteModes> QuotesWriteMode { get; set; }

        /// <summary>
        /// Gets or sets the size of contract fractional.
        /// </summary>
        [DataMember]
        public double ContractSizeFractional { get; set; }

        /// <summary>
        /// Gets or sets the margin ratio, which is taken for hedged positions.
        /// Supported values: [0.0, 1.0].
        /// </summary>
        [DataMember]
        public double MarginHedged { get; set; }

        /// <summary>
        /// Gets or sets the margin ratio fractional.
        /// Supported values: [0.0, 1.0].
        /// </summary>
        [DataMember]
        public double MarginFactorFractional { get; set; }

        /// <summary>
        /// Gets or sets whether margin is calculated in static mode or in dynamic mode.
        /// </summary>
        [DataMember]
        public bool MarginStrongMode { get; set; }

        /// <summary>
        /// Gets or sets the margin currency.
        /// </summary>
        [DataMember]
        public string MarginCurrency { get; set; }

        /// <summary>
        /// Gets or sets the margin currency ID.
        /// </summary>
        [DataMember]
        public short? MarginCurrencyId { get; set; }

        /// <summary>
        /// Gets or sets the precision of margin currency.
        /// </summary>
        [DataMember]
        public int MarginCurrencyPrecision { get; set; }

        /// <summary>
        /// Gets or sets the priority of margin currency.
        /// </summary>
        [DataMember]
        public int MarginCurrencySortOrder { get; set; }

        /// <summary>
        /// Gets or sets the profit currency.
        /// </summary>
        [DataMember]
        public string ProfitCurrency { get; set; }

        /// <summary>
        /// Gets or sets the profit currency ID.
        /// </summary>
        [DataMember]
        public short? ProfitCurrencyId { get; set; }

        /// <summary>
        /// Gets or sets the precision of profit currency.
        /// </summary>
        [DataMember]
        public int ProfitCurrencyPrecision { get; set; }

        /// <summary>
        /// Gets or sets the priority of profit currency.
        /// </summary>
        [DataMember]
        public int ProfitCurrencySortOrder { get; set; }

        /// <summary>
        /// Gets or sets the color of the symbol.
        /// </summary>
        [DataMember]
        public int ColorRef { get; set; }

        /// <summary>
        /// Gets or sets the description of the symbol.
        /// </summary>
        [DataMember]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets whether swaps is enabled for the symbol.
        /// </summary>
        [DataMember]
        public bool SwapEnabled { get; set; }

        /// <summary>
        /// Gets or sets the size of swap for short positions.
        /// </summary>
        [DataMember]
        public float SwapSizeShort { get; set; }

        /// <summary>
        /// Gets or sets the size of swap for long positions.
        /// </summary>
        [DataMember]
        public float SwapSizeLong { get; set; }

        /// <summary>
        /// Gets or sets whether the symbol is primary or not.
        /// </summary>
        [DataMember]
        public bool IsPrimary { get; set; }

        /// <summary>
        /// Gets or sets the priority of the symbol.
        /// </summary>
        [DataMember]
        public int SortOrder { get; set; }

        /// <summary>
        /// Gets or sets whether quotes filtering is disabled for the symbol or not.
        /// </summary>
        [DataMember]
        public bool IsQuotesFilteringDisabled { get; set; }

        /// <summary>
        /// Gets or sets schedule
        /// </summary>
        [DataMember]
        public string Schedule { get; set; }

        /// <summary>
        /// Default Slippage
        /// </summary>
        [DataMember]
        public decimal DefaultSlippage { get; set; }

        /// <summary>
        /// Margin reduction for Stop orders
        /// </summary>
        [DataMember]
        public double StopOrderMarginReduction { get; set; }

        /// <summary>
        /// Margin reduction for Stop orders
        /// </summary>
        [DataMember]
        public double HiddenLimitOrderMarginReduction { get; set; }

        /// <summary>
        /// Gets or sets swap type
        /// </summary>
        [DataMember]
        public WEnum<SwapType> SwapType { get; set; }
        SwapType ISymbolInfo.SwapType => SwapType;

        /// <summary>
        /// Gets or sets a day of week (from Monday to Friday) when 3-days swaps are charged.
        /// If value is 0 3-days swaps are disabled.
        /// </summary>
        [DataMember]
        public int TripleSwapDay { get; set; }

        [DataMember]
        public string Alias { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public SymbolInfo Clone()
        {
            var result = new SymbolInfo();
            result.Id = Id;
            result.Symbol = Symbol;
            result.Alias = Alias;
            result.Security = Security;
            result.Precision = Precision;
            result.TradeIsAllowed = TradeIsAllowed;
            result.MarginMode = MarginMode;
            result.ProfitMode = ProfitMode;
            result.QuotesWriteMode = QuotesWriteMode;
            result.ContractSizeFractional = ContractSizeFractional;
            result.MarginHedged = MarginHedged;
            result.MarginFactorFractional = MarginFactorFractional;
            result.StopOrderMarginReduction = StopOrderMarginReduction;
            result.HiddenLimitOrderMarginReduction = HiddenLimitOrderMarginReduction;
            result.MarginStrongMode = MarginStrongMode;
            result.MarginCurrency = MarginCurrency;
            result.MarginCurrencyId = MarginCurrencyId;
            result.MarginCurrencyPrecision = MarginCurrencyPrecision;
            result.MarginCurrencySortOrder = MarginCurrencySortOrder;
            result.ProfitCurrency = ProfitCurrency;
            result.ProfitCurrencyId = ProfitCurrencyId;
            result.ProfitCurrencyPrecision = ProfitCurrencyPrecision;
            result.ProfitCurrencySortOrder = ProfitCurrencySortOrder;
            result.ColorRef = ColorRef;
            result.Description = Description;
            result.SwapEnabled = SwapEnabled;
            result.SwapType = SwapType;
            result.SwapSizeShort = SwapSizeShort;
            result.SwapSizeLong = SwapSizeLong;
            result.TripleSwapDay = TripleSwapDay;
            result.IsPrimary = IsPrimary;
            result.SortOrder = SortOrder;
            result.IsQuotesFilteringDisabled = IsQuotesFilteringDisabled;
            result.Schedule = Schedule;
            result.DefaultSlippage = DefaultSlippage;
            return result;
        }

        public override string ToString()
        {
            var builder = new BriefToStringBuilder();
            builder.Append("Symbol", Symbol);
            builder.AppendNotNull("Alias", Alias);
            builder.Append("Security", Security);
            builder.Append("Precision", Precision);
            builder.Append("TradeIsAllowed", TradeIsAllowed);
            builder.Append("MarginMode", MarginMode);
            builder.Append("ProfitMode", ProfitMode);
            builder.Append("QuotesWriteMode", QuotesWriteMode);
            builder.Append("ContractSize", ContractSizeFractional);
            builder.Append("MarginHedged", MarginHedged);
            builder.Append("MarginFactor", MarginFactorFractional);
            builder.Append("MarginStrongMode", MarginStrongMode);
            builder.Append("MarginCurrency", MarginCurrency);
            builder.Append("MarginCurrencyPrecision", MarginCurrencyPrecision);
            builder.Append("MarginCurrencySortOrder", MarginCurrencySortOrder);
            builder.Append("ProfitCurrency", ProfitCurrency);
            builder.Append("ProfitCurrencyPrecision", ProfitCurrencyPrecision);
            builder.Append("ProfitCurrencySortOrder", ProfitCurrencySortOrder);
            builder.Append("ColorRef", ColorRef);
            builder.Append("SwapEnabled", SwapEnabled);
            builder.Append("SwapType", SwapType);
            builder.Append("SwapSizeShort", SwapSizeShort);
            builder.Append("SwapSizeLong", SwapSizeLong);
            builder.Append("TripleSwapDay", TripleSwapDay);
            builder.Append("IsPrimary", IsPrimary);
            builder.Append("SortOrder", SortOrder);
            builder.Append("IsQuotesFilteringDisabled", IsQuotesFilteringDisabled);
            builder.Append("Schedule", Schedule);
            builder.Append("DefaultSlippage", DefaultSlippage);
            builder.Append("StopOrderMarginReduction", StopOrderMarginReduction);
            builder.Append("HiddenLimitOrderMarginReduction", HiddenLimitOrderMarginReduction);
            builder.AppendNotNull("Description", Description);
            return builder.ToString();
        }

        #region IExtensibleDataObject

        [NonSerialized]
        private ExtensionDataObject theData;

        public virtual ExtensionDataObject ExtensionData
        {
            get { return theData; }
            set { theData = value; }
        }

        #endregion
    };

    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    class GroupSymbolInfo : SymbolInfo
    {
        /// <summary>
        /// Initializes a new instance of GroupSymbolInfo class.
        /// </summary>
        public GroupSymbolInfo()
        {
            MaxTradeAmount = 100000000 / 100;
            MinTradeAmount = 10000 / 100;
            TradeAmountStep = 1000 / 100;
            IsTradeAllowed = true;
        }

        /// <summary>
        /// Gets or sets the maximum trade amount in lots.
        /// </summary>
        [DataMember]
        public decimal MaxTradeAmount { get; set; }

        /// <summary>
        /// Gets or sets the TODO
        /// </summary>
        [DataMember]
        public decimal MinTradeAmount { get; set; }

        /// <summary>
        /// Gets or sets the TODO
        /// </summary>
        [DataMember]
        public decimal TradeAmountStep { get; set; }

        /// <summary>
        /// Gets or sets the TODO
        /// </summary>
        [DataMember]
        public bool IsTradeAllowed { get; set; }

        /// <summary>
        /// Gets or sets the TODO
        /// </summary>
        [DataMember]
        public WEnum<CommissionValueType> CommissionType { get; set; }

        /// <summary>
        /// Gets or sets the TODO
        /// </summary>
        [DataMember]
        public WEnum<CommissionChargeType> CommissionChargeType { get; set; }

        /// <summary>
        /// Gets or sets the TODO
        /// </summary>
        [DataMember]
        public decimal LimitsCommission { get; set; }

        /// <summary>
        /// Gets or sets the TODO
        /// </summary>
        [DataMember]
        public decimal Commission { get; set; }

        /// <summary>
        /// Gets or sets the TODO
        /// </summary>
        [DataMember]
        public int GroupSortOrder { get; set; }

        /// <summary>
        /// Gets or sets the TODO
        /// </summary>
        [DataMember]
        public string SecurityDescription { get; set; }

        /// <summary>
        /// Gets or sets the TODO
        /// </summary>
        [DataMember]
        public string SecurityName { get; set; }

        /// <summary>
        /// Gets or sets the TODO
        /// </summary>
        [DataMember]
        public decimal MinCommission { get; set; }

        /// <summary>
        /// Gets or sets the TODO
        /// </summary>
        [DataMember]
        public string MinCommissionCurrency { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    class ServerSymbolInfo : GroupSymbolInfo
    {
        /// <summary>
        /// Gets or sets the TODO
        /// </summary>
        [DataMember]
        public string SymbolOriginal { get; set; }
    }
}
