namespace TickTrader.FDK.Common
{
    using System;

    public class Order
    {
        /// <summary>
        /// Gets unique identifier of the order. Can not be null.
        /// </summary>
        public string OrderId { get; set; }

        /// <summary>
        /// Gets unique client identifier of the order. Can not be null.
        /// </summary>
        public string ClientOrderId { get; set; }

        /// <summary>
        /// Gets unique identifier of the parent order. Can be null.
        /// </summary>
        public string ParentOrderId { get; set; }

        /// <summary>
        /// Gets currency pair of the order. Can not be null.
        /// </summary>
        public string Symbol { get; set; }

        /// <summary>
        /// Initially requested order size.
        /// </summary>
        public double InitialVolume { get; set; }

        /// <summary>
        /// Gets volume of the order.
        /// </summary>
        public double Volume { get; set; }

        /// <summary>
        /// Gets max visible volume of the order.
        /// </summary>
        public double? MaxVisibleVolume { get; set; }

        /// <summary>
        /// Gets price of the order.
        /// </summary>
        public double? InitialPrice { get; set; }

        /// <summary>
        /// Gets price of the order.
        /// </summary>
        public double? Price { get; set; }

        /// <summary>
        /// Gets stop price of the order.
        /// </summary>
        public double? StopPrice { get; set; }

        /// <summary>
        /// Gets take profit of the order.
        /// </summary>
        public double? TakeProfit { get; set; }

        /// <summary>
        /// Gets stop loss of the order.
        /// </summary>
        public double? StopLoss { get; set; }

        /// <summary>
        /// Gets commission of the trade record.
        /// </summary>
        public double Commission { get; set; }

        /// <summary>
        /// Gets agents' commission of the trade record.
        /// </summary>
        public double AgentCommission { get; set; }

        /// <summary>
        ///
        /// </summary>
        public double Swap { get; set; }

        /// <summary>
        /// It's used by FinancialCalculator.
        /// </summary>
        public double? Profit { get; set; }

        /// <summary>
        /// It's used by FinancialCalculator.
        /// </summary>
        public double? Margin { get; set; }

        /// <summary>
        /// Gets type of the order.
        /// </summary>
        public OrderType InitialType { get; set; }

        /// <summary>
        /// Gets type of the order.
        /// </summary>
        public OrderType Type { get; set; }

        /// <summary>
        /// Gets side of the order.
        /// </summary>
        public OrderSide Side { get; set; }

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
        /// Gets IsHidden trade.
        /// </summary>
        public bool IsHidden
        { get { return MaxVisibleVolume.HasValue && MaxVisibleVolume.Value == 0; } }

        /// <summary>
        /// Gets IsIceberg trade.
        /// </summary>
        public bool IsIceberg
        { get { return MaxVisibleVolume.HasValue && MaxVisibleVolume.Value > 0; } }

        /// <summary>
        /// Gets IsHiddenOrIceberg trade.
        /// </summary>
        public bool IsHiddenOrIceberg
        { get { return IsHidden || IsIceberg; } }

        /// <summary>
        /// Gets expiration time of the trade record (if specified by user).
        /// </summary>
        public DateTime? Expiration { get; set; }

        /// <summary>
        /// Gets the trade record created time.
        /// </summary>
        public DateTime? Created { get; set; }

        /// <summary>
        /// Gets the trade record modified time.
        /// </summary>
        public DateTime? Modified { get; set; }

        /// <summary>
        /// Gets comment of the order. Can not be null.
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// Gets tag of the order. Can not be null.
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        /// Gets magic number of the order.
        /// </summary>
        public int? Magic { get; set; }

        /// <summary>
        /// Gets slippage.
        /// </summary>
        public double? Slippage { get; set; }

        /// <summary>
        /// Rebate
        /// </summary>
        public double? Rebate { get; set; }

        /// <summary>
        /// Rebate currency
        /// </summary>
        public string RebateCurrency { get; set; }

        /// <summary>
        /// OneCancelsTheOther Flag
        /// </summary>
        public bool OneCancelsTheOtherFlag { get; set; }

        /// <summary>
        /// Related order id if OneCancelsTheOtherFlag is true
        /// </summary>
        public long? RelatedOrderId { get; set; }

        /// <summary>
        /// Time when order execution expired
        /// </summary>
        public DateTime? ExecutionExpired { get; set; }

        /// <summary>
        /// Is Contingent order
        /// </summary>
        public bool ContingentOrderFlag { get; set; }
        /// <summary>
        /// Type of contingent order trigger
        /// </summary>
        public ContingentOrderTriggerType? TriggerType { get; set; }
        /// <summary>
        /// Id of related order. It is used if TriggerType is OnPendingOrderExpired or OnPendingOrderPartiallyFilled
        /// </summary>
        public long? OrderIdTriggeredBy { get; set; }
        /// <summary>
        /// Time of triggering. It is used if TriggerType is OnTime
        /// </summary>
        public DateTime? TriggerTime { get; set; }


        public override string ToString()
        {
            return $"#{OrderId}; Symbol={Symbol}; Side={Side}; Type={Type}; Volume={Volume}; Price={Price}; StopPrice={StopPrice}";
        }

    }
}
