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
        public bool IsReducedOpenCommission { get; set; }

        /// <summary>
        /// Gets ReducedCloseCommission flag.
        /// </summary>
        public bool IsReducedCloseCommission { get; set; }

        /// <summary>
        /// Gets ImmediateOrCancel flag.
        /// </summary>
        public bool ImmediateOrCancel { get; set; }

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
        ///
        /// </summary>
        [Obsolete("Please use OrdeType property")]
        public TradeRecordType TradeRecordType { get; set; }

        /// <summary>
        ///
        /// </summary>
        [Obsolete("Please use OrdeSide property")]
        public TradeRecordSide TradeRecordSide { get; set; }        

        public override string ToString()
        {
            return string.Format("Id = {0}; Type = {1}; Reason = {2}; Time = {3}; ClientId = {4}; OrderType = {5}; Symbol = {6}; OrderSide = {7}; InitialVolume = {8}; Price = {9}; LeavesVolume = {10}; TradeAmount = {11}; TradePrice = {12}", Id, TradeTransactionReportType, TradeTransactionReason, TransactionTime, ClientId, OrderType, Symbol, OrderSide, Quantity, Price, LeavesQuantity, OrderLastFillAmount, OrderFillPrice);
        }
    }
}
