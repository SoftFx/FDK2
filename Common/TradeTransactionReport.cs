namespace TickTrader.FDK.Common
{
    using System;

    /// <summary>
    /// Trade transaction report
    /// </summary>
    public class TradeTransactionReport
    {
        public TradeTransactionReport()
        {
        }

        public string TradeTransactionId { get; set; }

        /// <summary>
        ///
        /// </summary>
        public TradeTransactionReportType TradeTransactionReportType { get; set; }

        /// <summary>
        ///
        /// </summary>
        public TradeTransactionReason TradeTransactionReason { get; set; }

        /// <summary>
        ///
        /// </summary>
        public double AccountBalance { get; set; }

        /// <summary>
        ///
        /// </summary>
        public double TransactionAmount { get; set; }

        /// <summary>
        ///
        /// </summary>
        public string TransactionCurrency { get; set; }

        /// <summary>
        ///
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        ///
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        ///
        /// </summary>
        public double Quantity { get; set; }

        /// <summary>
        ///
        /// </summary>
        public double? MaxVisibleQuantity { get; set; }

        /// <summary>
        ///
        /// </summary>
        public double LeavesQuantity { get; set; }

        /// <summary>
        ///
        /// </summary>
        public double Price { get; set; }

        /// <summary>
        ///
        /// </summary>
        public double StopPrice { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public OrderType OrderType { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public OrderSide OrderSide { get; set;  }

        /// <summary>
        /// 
        /// </summary>
        public OrderTimeInForce? TimeInForce { get; set; }

        /// <summary>
        ///
        /// </summary>
        public string Symbol { get; set; }

        /// <summary>
        /// Gets user-defined comment.
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// Gets user-defined tag.
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        /// Gets user-defined magic number.
        /// </summary>
        public int? Magic { get; set; }

        /// <summary>
        /// Gets ReducedOpenCommission flag.
        /// </summary>
        public bool ReducedOpenCommission { get; set; }

        /// <summary>
        /// Gets ReducedCloseCommission flag.
        /// </summary>
        public bool ReducedCloseCommission { get; set; }

        /// <summary>
        /// Gets MarketWithSlippage flag.
        /// </summary>
        public bool MarketWithSlippage { get; set; }

        /// <summary>
        ///
        /// </summary>
        public DateTime OrderCreated { get; set; }

        /// <summary>
        ///
        /// </summary>
        public DateTime OrderModified { get; set; }

        /// <summary>
        /// Requested order type.
        /// </summary>
        public OrderType? ReqOrderType { get; set; }

        /// <summary>
        /// Requested open price.
        /// </summary>
        public double? ReqOpenPrice { get; set; }

        /// <summary>
        /// Requested open quantity.
        /// </summary>
        public double? ReqOpenQuantity { get; set; }

        /// <summary>
        /// Requested close price.
        /// </summary>
        public double? ReqClosePrice { get; set; }

        /// <summary>
        /// Requested close quantity.
        /// </summary>
        public double? ReqCloseQuantity { get; set; }

        /// <summary>
        ///
        /// </summary>
        public string PositionId { get; set; }

        /// <summary>
        ///
        /// </summary>
        public string PositionById { get; set; }

        /// <summary>
        /// Time of position opening (always indicated in UTC).
        /// </summary>
        public DateTime PositionOpened { get; set; }

        /// <summary>
        /// Requested (by client) price at which the position is to be opened
        /// </summary>
        public double PosOpenReqPrice { get; set; }

        /// <summary>
        /// Real price at which the position has be opened.
        /// </summary>
        public double PosOpenPrice { get; set; }

        /// <summary>
        /// Quantity of a position. Quantity closed on this (last) fill.
        /// </summary>
        public double PositionQuantity { get; set; }

        /// <summary>
        /// Quantity of the last fill transaction.
        /// </summary>
        public double PositionLastQuantity { get; set; }

        /// <summary>
        /// Quantity of position is still opened for further execution after a transaction.
        /// </summary>
        public double PositionLeavesQuantity { get; set; }

        /// <summary>
        /// Requested (by client) price at which the position is to be closed.
        /// </summary>
        public double PositionCloseRequestedPrice { get; set; }

        /// <summary>
        /// Real price at which the position has be closed.
        /// </summary>
        public double PositionClosePrice { get; set; }

        /// <summary>
        /// Time of position closing (always indicated in UTC).
        /// </summary>
        public DateTime PositionClosed { get; set; }

        /// <summary>
        /// Time of position modification (always indicated in UTC).
        /// </summary>
        public DateTime PositionModified { get; set; }

        /// <summary>
        /// Position remaining amount side.
        /// </summary>
        public OrderSide PosRemainingSide { get; set;}

        /// <summary>
        /// Position remaining amount price.
        /// </summary>
        public double? PosRemainingPrice { get; set; }

        /// <summary>
        /// Commission.
        /// </summary>
        public double Commission { get; set; }

        /// <summary>
        /// Agent Commission.
        /// </summary>
        public double AgentCommission { get; set; }

        /// <summary>
        /// Swap.
        /// </summary>
        public double Swap { get; set; }

        /// <summary>
        /// Specifies currency to be used for Commission.
        /// </summary>
        public string CommCurrency { get; set; }

        /// <summary>
        /// Price at which the order is to be closed.
        /// </summary>
        public double StopLoss { get; set; }

        /// <summary>
        /// Price at which the order is to be closed.
        /// </summary>
        public double TakeProfit { get; set; }

        /// <summary>
        ///
        /// </summary>
        public string NextStreamPositionId { get; set; }

        /// <summary>
        /// Transaction time.
        /// </summary>
        public DateTime TransactionTime { get; set; }

        /// <summary>
        /// Last fill price.
        /// </summary>
        public double? OrderFillPrice { get; set; }

        /// <summary>
        /// Last fill amount.
        /// </summary>
        public double? OrderLastFillAmount { get; set; }

        /// <summary>
        /// Open conversion rate.
        /// </summary>
        public double? OpenConversionRate { get; set; }

        /// <summary>
        /// Close conversion rate.
        /// </summary>
        public double? CloseConversionRate { get; set; }

        /// <summary>
        /// Order action number.
        /// </summary>
        public int ActionId { get; set; }

        /// <summary>
        /// Gets ExpireTime = 126 field.
        /// </summary>
        public DateTime? Expiration { get; set; }

        /// <summary>
        /// Gets source asset currency.
        /// </summary>
        public string SrcAssetCurrency { get; set; }

        /// <summary>
        /// Gets source asset amount.
        /// </summary>
        public double? SrcAssetAmount { get; set; }

        /// <summary>
        /// Gets source asset movement.
        /// </summary>
        public double? SrcAssetMovement { get; set; }

        /// <summary>
        /// Gets destination asset currency.
        /// </summary>
        public string DstAssetCurrency { get; set; }

        /// <summary>
        /// Gets destination asset amount.
        /// </summary>
        public double? DstAssetAmount { get; set; }

        /// <summary>
        /// Gets destination asset movement.
        /// </summary>
        public double? DstAssetMovement { get; set; }

        /// <summary>
        /// </summary>
        public double? MarginCurrencyToUsdConversionRate { get; set; }

        /// <summary>
        /// </summary>
        public double? UsdToMarginCurrencyConversionRate { get; set; }

        /// <summary>
        /// </summary>
        public string MarginCurrency { get; set; }

        /// <summary>
        /// </summary>
        public double? ProfitCurrencyToUsdConversionRate { get; set; }

        /// <summary>
        /// </summary>
        public double? UsdToProfitCurrencyConversionRate { get; set; }

        /// <summary>
        /// </summary>
        public string ProfitCurrency { get; set; }

        /// <summary>
        /// </summary>
        public double? SrcAssetToUsdConversionRate { get; set; }

        /// <summary>
        /// </summary>
        public double? UsdToSrcAssetConversionRate { get; set; }

        /// <summary>
        /// </summary>
        public double? DstAssetToUsdConversionRate { get; set; }

        /// <summary>
        /// </summary>
        public double? UsdToDstAssetConversionRate { get; set; }

        /// <summary>
        /// </summary>
        public string MinCommissionCurrency { get; set; }

        /// <summary>
        /// </summary>
        public double? MinCommissionConversionRate { get; set; }

        /// <summary>
        /// Gets ImmediateOrCancel flag.
        /// </summary>
        public bool ImmediateOrCancel { get; set; }

        /// <summary>
        /// </summary>
        public double? Slippage { get; set; }

        /// <summary>
        /// </summary>
        public double? MarginCurrencyToReportConversionRate { get; set; }

        /// <summary>
        /// </summary>
        public double? ReportToMarginCurrencyConversionRate { get; set; }

        /// <summary>
        /// </summary>
        public double? ProfitCurrencyToReportConversionRate { get; set; }

        /// <summary>
        /// </summary>
        public double? ReportToProfitCurrencyConversionRate { get; set; }

        /// <summary>
        /// </summary>
        public double? SrcAssetToReportConversionRate { get; set; }

        /// <summary>
        /// </summary>
        public double? ReportToSrcAssetConversionRate { get; set; }

        /// <summary>
        /// </summary>
        public double? DstAssetToReportConversionRate { get; set; }

        /// <summary>
        /// </summary>
        public double? ReportToDstAssetConversionRate { get; set; }

        /// <summary>
        /// </summary>
        public string ReportCurrency { get; set; }

        /// <summary>
        /// </summary>
        public string TokenCommissionCurrency { get; set; }

        /// <summary>
        /// </summary>
        public double? TokenCommissionCurrencyDiscount { get; set; }

        /// <summary>
        /// </summary>
        public double? TokenCommissionConversionRate { get; set; }

        /// <summary>
        /// </summary>
        public double? SplitRatio { get; set; }

        /// <summary>
        /// </summary>
        public double? DividendGrossRate { get; set; }

        /// <summary>
        /// </summary>
        public double? DividendToBalanceConversionRate { get; set; }

        /// <summary>
        /// Tax.
        /// </summary>
        public double Tax { get; set; }

        /// <summary>
        /// The Tax value at the time of dividend payment.
        /// </summary>
        public double TaxValue { get; set; }

        /// <summary>
        /// Rebate
        /// </summary>
        public double Rebate { get; set; }

        /// <summary>
        /// Rebate currency
        /// </summary>
        public string RebateCurrency { get; set; }


        public TradeTransactionReport Clone()
        {
            TradeTransactionReport tradeTransactionReport = new TradeTransactionReport();
            tradeTransactionReport.TradeTransactionId = TradeTransactionId;
            tradeTransactionReport.TradeTransactionReportType = TradeTransactionReportType;
            tradeTransactionReport.TradeTransactionReason = TradeTransactionReason;
            tradeTransactionReport.AccountBalance = AccountBalance;
            tradeTransactionReport.TransactionAmount = TransactionAmount;
            tradeTransactionReport.TransactionCurrency = TransactionCurrency;
            tradeTransactionReport.Id = Id;
            tradeTransactionReport.ClientId = ClientId;
            tradeTransactionReport.Quantity = Quantity;
            tradeTransactionReport.MaxVisibleQuantity = MaxVisibleQuantity;
            tradeTransactionReport.LeavesQuantity = LeavesQuantity;
            tradeTransactionReport.Price = Price;
            tradeTransactionReport.StopPrice = StopPrice;
            tradeTransactionReport.OrderType = OrderType;
            tradeTransactionReport.OrderSide = OrderSide;
            tradeTransactionReport.TimeInForce = TimeInForce;
            tradeTransactionReport.Symbol = Symbol;
            tradeTransactionReport.Comment = Comment;
            tradeTransactionReport.Tag = Tag;
            tradeTransactionReport.Magic = Magic;
            tradeTransactionReport.ReducedOpenCommission = ReducedOpenCommission;
            tradeTransactionReport.ReducedCloseCommission = ReducedCloseCommission;
            tradeTransactionReport.MarketWithSlippage = MarketWithSlippage;
            tradeTransactionReport.OrderCreated = OrderCreated;
            tradeTransactionReport.OrderModified = OrderModified;
            tradeTransactionReport.ReqOrderType = ReqOrderType;
            tradeTransactionReport.ReqOpenPrice = ReqOpenPrice;
            tradeTransactionReport.ReqOpenQuantity = ReqOpenQuantity;
            tradeTransactionReport.ReqClosePrice = ReqClosePrice;
            tradeTransactionReport.ReqCloseQuantity = ReqCloseQuantity;
            tradeTransactionReport.PositionId = PositionId;
            tradeTransactionReport.PositionById = PositionById;
            tradeTransactionReport.PositionOpened = PositionOpened;
            tradeTransactionReport.PosOpenReqPrice = PosOpenReqPrice;
            tradeTransactionReport.PosOpenPrice = PosOpenPrice;
            tradeTransactionReport.PositionQuantity = PositionQuantity;
            tradeTransactionReport.PositionLastQuantity = PositionLastQuantity;
            tradeTransactionReport.PositionLeavesQuantity = PositionLeavesQuantity;
            tradeTransactionReport.PositionCloseRequestedPrice = PositionCloseRequestedPrice;
            tradeTransactionReport.PositionClosePrice = PositionClosePrice;
            tradeTransactionReport.PositionClosed = PositionClosed;
            tradeTransactionReport.PositionModified = PositionModified;
            tradeTransactionReport.PosRemainingSide = PosRemainingSide;
            tradeTransactionReport.PosRemainingPrice = PosRemainingPrice;
            tradeTransactionReport.Commission = Commission;
            tradeTransactionReport.AgentCommission = AgentCommission;
            tradeTransactionReport.Swap = Swap;
            tradeTransactionReport.CommCurrency = CommCurrency;
            tradeTransactionReport.StopLoss = StopLoss;
            tradeTransactionReport.TakeProfit = TakeProfit;
            tradeTransactionReport.NextStreamPositionId = NextStreamPositionId;
            tradeTransactionReport.TransactionTime = TransactionTime;
            tradeTransactionReport.OrderFillPrice = OrderFillPrice;
            tradeTransactionReport.OrderLastFillAmount = OrderLastFillAmount;
            tradeTransactionReport.OpenConversionRate = OpenConversionRate;
            tradeTransactionReport.CloseConversionRate = CloseConversionRate;
            tradeTransactionReport.ActionId = ActionId;
            tradeTransactionReport.Expiration = Expiration;
            tradeTransactionReport.SrcAssetCurrency = SrcAssetCurrency;
            tradeTransactionReport.SrcAssetAmount = SrcAssetAmount;
            tradeTransactionReport.SrcAssetMovement = SrcAssetMovement;
            tradeTransactionReport.DstAssetCurrency = DstAssetCurrency;
            tradeTransactionReport.DstAssetAmount = DstAssetAmount;
            tradeTransactionReport.DstAssetMovement = DstAssetMovement;
            tradeTransactionReport.MarginCurrencyToUsdConversionRate = MarginCurrencyToUsdConversionRate;
            tradeTransactionReport.UsdToMarginCurrencyConversionRate = UsdToMarginCurrencyConversionRate;
            tradeTransactionReport.MarginCurrency = MarginCurrency;
            tradeTransactionReport.ProfitCurrencyToUsdConversionRate = ProfitCurrencyToUsdConversionRate;
            tradeTransactionReport.UsdToProfitCurrencyConversionRate = UsdToProfitCurrencyConversionRate;
            tradeTransactionReport.ProfitCurrency = ProfitCurrency;
            tradeTransactionReport.SrcAssetToUsdConversionRate = SrcAssetToUsdConversionRate;
            tradeTransactionReport.UsdToSrcAssetConversionRate = UsdToSrcAssetConversionRate;
            tradeTransactionReport.DstAssetToUsdConversionRate = DstAssetToUsdConversionRate;
            tradeTransactionReport.UsdToDstAssetConversionRate = UsdToDstAssetConversionRate;
            tradeTransactionReport.MinCommissionCurrency = MinCommissionCurrency;
            tradeTransactionReport.MinCommissionConversionRate = MinCommissionConversionRate;
            tradeTransactionReport.ImmediateOrCancel = ImmediateOrCancel;
            tradeTransactionReport.Slippage = Slippage;
            tradeTransactionReport.MarginCurrencyToReportConversionRate = MarginCurrencyToReportConversionRate;
            tradeTransactionReport.ReportToMarginCurrencyConversionRate = ReportToMarginCurrencyConversionRate;
            tradeTransactionReport.ProfitCurrencyToReportConversionRate = ProfitCurrencyToReportConversionRate;
            tradeTransactionReport.ReportToProfitCurrencyConversionRate = ReportToProfitCurrencyConversionRate;
            tradeTransactionReport.SrcAssetToReportConversionRate = SrcAssetToReportConversionRate;
            tradeTransactionReport.ReportToSrcAssetConversionRate = ReportToSrcAssetConversionRate;
            tradeTransactionReport.DstAssetToReportConversionRate = DstAssetToReportConversionRate;
            tradeTransactionReport.ReportToDstAssetConversionRate = ReportToDstAssetConversionRate;
            tradeTransactionReport.ReportCurrency = ReportCurrency;
            tradeTransactionReport.TokenCommissionCurrency = TokenCommissionCurrency;
            tradeTransactionReport.TokenCommissionCurrencyDiscount = TokenCommissionCurrencyDiscount;
            tradeTransactionReport.TokenCommissionConversionRate = TokenCommissionConversionRate;
            tradeTransactionReport.SplitRatio = SplitRatio;
            tradeTransactionReport.DividendGrossRate = DividendGrossRate;
            tradeTransactionReport.DividendToBalanceConversionRate = DividendToBalanceConversionRate;
            tradeTransactionReport.Tax = Tax;
            tradeTransactionReport.TaxValue = TaxValue;
            tradeTransactionReport.Rebate = Rebate;
            tradeTransactionReport.RebateCurrency = RebateCurrency;

            return tradeTransactionReport;
        }

        public override string ToString()
        {
            return string.Format
            (
                "Id = {0}; Type = {1}; Reason = {2}; Time = {3}; ClientId = {4}; OrderType = {5}; Symbol = {6}; OrderSide = {7}; InitialVolume = {8}; Price = {9}; LeavesVolume = {10}; TradeAmount = {11}; TradePrice = {12}", 
                Id, 
                TradeTransactionReportType, 
                TradeTransactionReason, 
                TransactionTime, 
                ClientId, 
                OrderType, 
                Symbol, 
                OrderSide, 
                Quantity, 
                Price, 
                LeavesQuantity, 
                OrderLastFillAmount, 
                OrderFillPrice
            );
        }
    }
}
