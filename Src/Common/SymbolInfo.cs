namespace TickTrader.FDK.Common
{
    /// <summary>
    /// Contains symbol parameters.
    /// </summary>
    public class SymbolInfo
    {
        public SymbolInfo()
        {
        }

        #region Properties

        /// <summary>
        /// Gets symbol name.
        /// </summary>
        /// <exception cref="SoftFX.Extended.Errors.UnsupportedFeatureException">If the feature is not supported by used protocol version.</exception>
        public string Name
        {
            get
            {
                return this.name;
            }
            set
            {
                this.name = value;
            }
        }

        /// <summary>
        /// Gets currency of the symbol.
        /// </summary>
        /// <exception cref="SoftFX.Extended.Errors.UnsupportedFeatureException">If the feature is not supported by used protocol version.</exception>
        public string Currency
        {
            get
            {
                return this.currency;
            }
            set
            {
                this.currency = value;
            }
        }

        /// <summary>
        /// Gets settlement currency of the symbol.
        /// </summary>
        /// <exception cref="SoftFX.Extended.Errors.UnsupportedFeatureException">If the feature is not supported by used protocol version.</exception>
        public string SettlementCurrency
        {
            get
            {
                return this.settlementCurrency;
            }
            set
            {
                this.settlementCurrency = value;
            }
        }

        /// <summary>
        /// Gets description of the symbol.
        /// </summary>
        /// <exception cref="SoftFX.Extended.Errors.UnsupportedFeatureException">If the feature is not supported by used protocol version.</exception>
        public string Description
        {
            get
            {
                return this.description;
            }
            set
            {
                this.description = value;
            }
        }

        /// <summary>
        /// Gets precision of the symbol.
        /// </summary>
        /// <exception cref="SoftFX.Extended.Errors.UnsupportedFeatureException">If the feature is not supported by used protocol version.</exception>
        public int Precision
        {
            get
            {
                return this.precision;
            }
            set
            {
                this.precision = value;
            }
        }

        /// <summary>
        /// Gets round lot of the symbol.
        /// </summary>
        /// <exception cref="SoftFX.Extended.Errors.UnsupportedFeatureException">If the feature is not supported by used protocol version.</exception>
        public double RoundLot
        {
            get
            {
                return this.roundLot;
            }
            set
            {
                this.roundLot = value;
            }
        }

        /// <summary>
        /// Gets minimum trade volume of the symbol.
        /// </summary>
        /// <exception cref="SoftFX.Extended.Errors.UnsupportedFeatureException">If the feature is not supported by used protocol version.</exception>
        public double MinTradeVolume
        {
            get
            {
                return this.minTradeVolume;
            }
            set
            {
                this.minTradeVolume = value;
            }
        }

        /// <summary>
        /// Gets maximum trade volume of the symbol.
        /// </summary>
        /// <exception cref="SoftFX.Extended.Errors.UnsupportedFeatureException">If the feature is not supported by used protocol version.</exception>
        public double MaxTradeVolume
        {
            get
            {
                return this.maxTradeVolume;
            }
            set
            {
                this.maxTradeVolume = value;
            }
        }

        /// <summary>
        /// Gets trading volume step of the symbol.
        /// </summary>
        /// <exception cref="SoftFX.Extended.Errors.UnsupportedFeatureException">If the feature is not supported by used protocol version.</exception>
        public double TradeVolumeStep
        {
            get
            {
                return this.tradeVolumeStep;
            }
            set
            {
                this.tradeVolumeStep = value;
            }
        }

        /// <summary>
        /// Gets profit calculation mode of the symbol.
        /// </summary>
        /// <exception cref="SoftFX.Extended.Errors.UnsupportedFeatureException">If the feature is not supported by used protocol version.</exception>
        public ProfitCalcMode ProfitCalcMode
        {
            get
            {
                return this.profitCalcMode;
            }
            set
            {
                this.profitCalcMode = value;
            }
        }

        /// <summary>
        /// Gets margin calculation mode of the symbol.
        /// </summary>
        /// <exception cref="SoftFX.Extended.Errors.UnsupportedFeatureException">If the feature is not supported by used protocol version.</exception>
        public MarginCalcMode MarginCalcMode
        {
            get
            {
                return this.marginCalcMode;
            }
            set
            {
                this.marginCalcMode = value;
            }
        }

        /// <summary>
        /// Gets margin hedge of the symbol.
        /// </summary>
        /// <exception cref="SoftFX.Extended.Errors.UnsupportedFeatureException">If the feature is not supported by used protocol version.</exception>
        public double MarginHedge
        {
            get
            {
                return this.marginHedge;
            }
            set
            {
                this.marginHedge = value;
            }
        }

        /// <summary>
        /// Gets margin factor of the symbol.
        /// </summary>
        /// <exception cref="SoftFX.Extended.Errors.UnsupportedFeatureException">If the feature is not supported by used protocol version.</exception>
        public int MarginFactor
        {
            get
            {
                return this.marginFactor;
            }
            set
            {
                this.marginFactor = value;
            }
        }

        /// <summary>
        /// Gets margin factor of the symbol.
        /// </summary>
        /// <exception cref="SoftFX.Extended.Errors.UnsupportedFeatureException">If the feature is not supported by used protocol version.</exception>
        public double? MarginFactorFractional
        {
            get
            {
                return this.marginFactorFractional;
            }
            set
            {
                this.marginFactorFractional = value;
            }
        }

        /// <summary>
        /// Gets contract multiplier.
        /// </summary>
        /// <exception cref="SoftFX.Extended.Errors.UnsupportedFeatureException">If the feature is not supported by used protocol version.</exception>
        public double ContractMultiplier
        {
            get
            {
                return this.contractMultiplier;
            }
            set
            {
                this.contractMultiplier = value;
            }
        }

        /// <summary>
        /// Gets color of the symbol assigned by server.
        /// </summary>
        /// <exception cref="SoftFX.Extended.Errors.UnsupportedFeatureException">If the feature is not supported by used protocol version.</exception>
        public int Color
        {
            get
            {
                return this.color;
            }
            set
            {
                this.color = value;
            }
        }

        /// <summary>
        /// Gets commission type.
        /// </summary>
        /// <exception cref="SoftFX.Extended.Errors.UnsupportedFeatureException">If the feature is not supported by used protocol version.</exception>
        public CommissionType CommissionType
        {
            get
            {
                return this.commType;
            }
            set
            {
                this.commType = value;
            }
        }

        /// <summary>
        /// Gets commission charge type.
        /// </summary>
        /// <exception cref="SoftFX.Extended.Errors.UnsupportedFeatureException">If the feature is not supported by used protocol version.</exception>
        public CommissionChargeType CommissionChargeType
        {
            get
            {
                return this.commChargeType;
            }
            set
            {
                this.commChargeType = value;
            }
        }

        /// <summary>
        /// Gets commission charge method.
        /// </summary>
        /// <exception cref="SoftFX.Extended.Errors.UnsupportedFeatureException">If the feature is not supported by used protocol version.</exception>
        public CommissionChargeMethod CommissionChargeMethod
        {
            get
            {
                return this.commChargeMethod;
            }
            set
            {
                this.commChargeMethod = value;
            }
        }

        /// <summary>
        /// Gets commission value for limits.
        /// </summary>
        /// <exception cref="SoftFX.Extended.Errors.UnsupportedFeatureException">If the feature is not supported by used protocol version.</exception>
        public double LimitsCommission
        {
            get
            {
                return this.limitsCommission;
            }
            set
            {
                this.limitsCommission = value;
            }
        }

        /// <summary>
        /// Gets commission value.
        /// </summary>
        /// <exception cref="SoftFX.Extended.Errors.UnsupportedFeatureException">If the feature is not supported by used protocol version.</exception>
        public double Commission
        {
            get
            {
                return this.commission;
            }
            set
            {
                this.commission = value;
            }
        }

        /// <summary>
        /// Gets min commission value.
        /// </summary>
        /// <exception cref="SoftFX.Extended.Errors.UnsupportedFeatureException">If the feature is not supported by used protocol version.</exception>
        public double MinCommission
        {
            get
            {
                return this.minCommission;
            }
            set
            {
                this.minCommission = value;
            }
        }

        /// <summary>
        /// Gets min commission currency value.
        /// </summary>
        /// <exception cref="SoftFX.Extended.Errors.UnsupportedFeatureException">If the feature is not supported by used protocol version.</exception>
        public string MinCommissionCurrency
        {
            get
            {
                return this.minCommissionCurrency;
            }
            set
            {
                this.minCommissionCurrency = value;
            }
        }

        /// <summary>
        /// Gets swap type.
        /// </summary>
        public SwapType SwapType
        {
            get
            {
                return this.swapType;
            }
            set
            {
                this.swapType = value;
            }
        }

        /// <summary>
        /// Gets triple swap day.
        /// 0 - 3-days swap is disabled;
        /// 1,2,3,4,5 - days of week from Monday to Friday;
        /// </summary>
        public int TripleSwapDay
        {
            get
            {
                return this.tripleSwapDay;
            }
            set
            {
                this.tripleSwapDay = value;
            }
        }

        /// <summary>
        /// Gets swap size short.
        /// </summary>
        public double? SwapSizeShort
        {
            get
            {
                return this.swapSizeShort;
            }
            set
            {
                this.swapSizeShort = value;
            }
        }

        /// <summary>
        /// Gets swap size long.
        /// </summary>
        public double? SwapSizeLong
        {
            get
            {
                return this.swapSizeLong;
            }
            set
            {
                this.swapSizeLong = value;
            }
        }

        /// <summary>
        /// Gets default slippage.
        /// </summary>
        public double? DefaultSlippage
        {
            get
            {
                return this.defaultSlippage;
            }
            set
            {
                this.defaultSlippage = value;
            }
        }

        /// <summary>
        /// Gets whether trade is enabled for this symbol.
        /// </summary>
        public bool IsTradeEnabled
        {
            get
            {
                return this.isTradeEnabled;
            }
            set
            {
                this.isTradeEnabled = value;
            }
        }

        public int GroupSortOrder
        {
            get
            {
                return this.groupSortOrder;
            }
            set
            {
                this.groupSortOrder = value;
            }
        }

        public int SortOrder
        {
            get
            {
                return this.sortOrder;
            }
            set
            {
                this.sortOrder = value;
            }
        }

        public int CurrencySortOrder
        {
            get
            {
                return this.currencySortOrder;
            }
            set
            {
                this.currencySortOrder = value;
            }
        }

        public int SettlementCurrencySortOrder
        {
            get
            {
                return this.settlementCurrencySortOrder;
            }
            set
            {
                this.settlementCurrencySortOrder = value;
            }
        }

        public int CurrencyPrecision
        {
            get
            {
                return this.currencyPrecision;
            }
            set
            {
                this.currencyPrecision = value;
            }
        }

        public int SettlementCurrencyPrecision
        {
            get
            {
                return this.settlementCurrencyPrecision;
            }
            set
            {
                this.settlementCurrencyPrecision = value;
            }
        }

        /// <summary>
        /// Symbol status group id.
        /// </summary>
        public string StatusGroupId
        {
            get
            {
                return this.statusGroupId;
            }
            set
            {
                this.statusGroupId = value;
            }
        }

        /// <summary>
        /// Symbol security name.
        /// </summary>
        public string SecurityName
        {
            get
            {
                return this.securityName;
            }
            set
            {
                this.securityName = value;
            }
        }

        /// <summary>
        /// Symbol security description.
        /// </summary>
        public string SecurityDescription
        {
            get
            {
                return this.securityDescription;
            }
            set
            {
                this.securityDescription = value;
            }
        }

        /// <summary>
        /// </summary>
        public double? StopOrderMarginReduction
        {
            get
            {
                return this.stopOrderMarginReduction;
            }
            set
            {
                this.stopOrderMarginReduction = value;
            }
        }

        /// <summary>
        /// </summary>
        public double? HiddenLimitOrderMarginReduction
        {
            get
            {
                return this.hiddenLimitOrderMarginReduction;
            }
            set
            {
                this.hiddenLimitOrderMarginReduction = value;
            }
        }

        #endregion

        /// <summary>
        /// Returns whether swap is enabled for symbol.
        /// </summary>
        /// <param name="symbolInfo"></param>
        /// <returns>True if swap is enabled.</returns>
        public bool IsSwapEnabled()
        {
            return SwapSizeShort.HasValue && SwapSizeLong.HasValue;
        }

        /// <summary>
        /// Returns margin factor.
        /// </summary>
        /// <param name="symbolInfo"></param>
        /// <returns>Margin factor.</returns>
        public double GetMarginFactor()
        {
            if (MarginFactorFractional.HasValue)
                return MarginFactorFractional.Value;

            return MarginFactor / 100D;
        }

        /// <summary>
        /// Converts SymbolInfo to string; format is 'Name = {0}; ContractMultiplier = {1}'
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("Name = {0}; ContractMultiplier = {1}; StatusGroupId = {2}; Descrtiption = {3}", this.Name, this.ContractMultiplier, this.StatusGroupId, this.Description);
        }

        #region Members

        string name;
        string currency;
        string settlementCurrency;
        string description;
        int precision;
        double roundLot;
        double minTradeVolume;
        double maxTradeVolume;
        double tradeVolumeStep;
        ProfitCalcMode profitCalcMode;
        MarginCalcMode marginCalcMode;
        double marginHedge;
        int marginFactor;
        double? marginFactorFractional;
        double contractMultiplier;
        int color;
        double limitsCommission;
        double commission;
        CommissionType commType;
        CommissionChargeType commChargeType;
        CommissionChargeMethod commChargeMethod;
        double minCommission;
        string minCommissionCurrency;
        SwapType swapType;
        int tripleSwapDay;
        double? swapSizeShort;
        double? swapSizeLong;
        double? defaultSlippage;
        bool isTradeEnabled;
        int groupSortOrder;
        int sortOrder;
        int currencySortOrder;
        int settlementCurrencySortOrder;
        int currencyPrecision;
        int settlementCurrencyPrecision;
        string statusGroupId;
        string securityName;
        string securityDescription;
        double? stopOrderMarginReduction;
        double? hiddenLimitOrderMarginReduction;

        #endregion
    }
}
