namespace TickTrader.FDK.Extended
{
    using System;
    using TickTrader.FDK.Common;

    /// <summary>
    /// Represents market, position or pending order.
    /// </summary>
    public class TradeRecord
    {
        public TradeRecord(DataTrade dataTrade)
        {
            this.DataTrade = dataTrade;
        }

        #region Properties

        /// <summary>
        /// Gets related data trade instance.
        /// </summary>
        public DataTrade DataTrade { get; set; }

        /// <summary>
        /// Gets unique identifier of the order. Can not be null.
        /// </summary>
        public string OrderId { get; set; }

        /// <summary>
        /// Gets unique client identifier of the order. Can not be null.
        /// </summary>
        public string ClientOrderId { get; set; }

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
        public double AgentCommission { get; set;}

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

        #endregion

        #region Computing properties

        /// <summary>
        /// Returns true, if the trade record is position.
        /// </summary>
        public bool IsPosition
        {
            get
            {
                return this.Type == OrderType.Position;
            }
        }

        /// <summary>
        /// Returns true, if the trade record is stop order.
        /// </summary>
        public bool IsStopOrder
        {
            get
            {
                return this.Type == OrderType.Stop;
            }
        }

        /// <summary>
        /// Returns true, if the trade record is limit order.
        /// </summary>
        public bool IsLimitOrder
        {
            get
            {
                return this.Type == OrderType.Limit;
            }
        }

        /// <summary>
        /// Returns true, if the trade record is stop limit order.
        /// </summary>
        public bool IsStopLimitOrder
        {
            get
            {
                return this.Type == OrderType.StopLimit;
            }
        }

        /// <summary>
        /// Returns true, if the trade record is limit or stop order.
        /// </summary>
        public bool IsPendingOrder
        {
            get
            {
                return this.IsLimitOrder || this.IsStopOrder || this.IsStopLimitOrder;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Modifies an existing order.
        /// </summary>
        /// <param name="newPrice">A new pending order price.</param>
        /// <param name="newStopPrice">A new pending order stop price.</param>
        /// <param name="newStopLoss">A new pending order stop loss.</param>
        /// <param name="newTakeProfit">A new pending order take profit.</param>
        /// <param name="newExpirationTime">A new pending order expiration time.</param>
        /// <param name="newComment">A new comment</param>
        /// <param name="newTag">A new comment</param>
        /// <param name="newMagic">A new comment</param>
        /// <returns>A modified trade record.</returns>
        public TradeRecord Modify(double? newPrice, double? newStopPrice, double? newStopLoss, double? newTakeProfit, DateTime? newExpirationTime, string newComment, string newTag, int? newMagic)
        {
            return ModifyEx(newPrice, newStopPrice, newStopLoss, newTakeProfit, newExpirationTime, newComment, newTag, newMagic, this.DataTrade.synchOperationTimeout_);
        }

        /// <summary>
        /// Modifies an existing order.
        /// </summary>
        /// <param name="newPrice">A new pending order price.</param>
        /// <param name="newStopPrice">A new pending order stop price.</param>
        /// <param name="newStopLoss">A new pending order stop loss.</param>
        /// <param name="newTakeProfit">A new pending order take profit.</param>
        /// <param name="newExpirationTime">A new pending order expiration time.</param>
        /// <param name="newComment">A new comment</param>
        /// <param name="newTag">A new comment</param>
        /// <param name="newMagic">A new comment</param>
        /// <param name="timeoutInMilliseconds">Timeout of the operation in milliseconds.</param>
        /// <returns>A modified trade record.</returns>
        public TradeRecord ModifyEx(double? newPrice, double? newStopPrice, double? newStopLoss, double? newTakeProfit, DateTime? newExpirationTime, string newComment, string newTag, int? newMagic, int timeoutInMilliseconds)
        {
            return DataTrade.Server.ModifyTradeRecordEx(this.OrderId, this.Symbol, this.Type, this.Side, this.Volume, null, newPrice, newStopPrice, newStopLoss, newTakeProfit, newExpirationTime, newComment, newTag, newMagic, timeoutInMilliseconds);
        }

        /// <summary>
        /// Modifies an existing order.
        /// </summary>
        /// <param name="operationId">Operation Id</param>
        /// <param name="newPrice">A new pending order price.</param>
        /// <param name="newStopPrice">A new pending order stop price.</param>
        /// <param name="newStopLoss">A new pending order stop loss.</param>
        /// <param name="newTakeProfit">A new pending order take profit.</param>
        /// <param name="newExpirationTime">A new pending order expiration time.</param>
        /// <param name="newComment">A new comment</param>
        /// <param name="newTag">A new comment</param>
        /// <param name="newMagic">A new comment</param>
        /// <param name="timeoutInMilliseconds">Timeout of the operation in milliseconds.</param>
        /// <returns>A modified trade record.</returns>
        public TradeRecord ModifyEx(string operationId, double? newPrice, double? newStopPrice, double? newStopLoss, double? newTakeProfit, DateTime? newExpirationTime, string newComment, string newTag, int? newMagic, int timeoutInMilliseconds)
        {
            return DataTrade.Server.ModifyTradeRecordEx(operationId, this.OrderId, this.ClientOrderId, this.Symbol, this.Type, this.Side, this.Volume, null, newPrice, newStopPrice, newStopLoss, newTakeProfit, newExpirationTime, newComment, newTag, newMagic, timeoutInMilliseconds);
        }

        /// <summary>
        /// Deletes pending order; not valid for market orders.
        /// </summary>
        public void Delete()
        {
            DeleteEx(Guid.NewGuid().ToString(), this.DataTrade.synchOperationTimeout_);
        }

        /// <summary>
        /// Deletes pending order; not valid for market orders.
        /// </summary>
        /// <param name="timeoutInMilliseconds">timeout of the operation in milliseconds</param>
        public void DeleteEx(int timeoutInMilliseconds)
        {
            DeleteEx(Guid.NewGuid().ToString(), timeoutInMilliseconds);
        }

        /// <summary>
        /// Deletes pending order; not valid for market orders.
        /// </summary>
        /// <param name="operationId">Operation Id</param>
        /// <param name="timeoutInMilliseconds">timeout of the operation in milliseconds</param>
        public void DeleteEx(string operationId, int timeoutInMilliseconds)
        {
            DataTrade.Server.DeletePendingOrderEx(operationId, this.OrderId, timeoutInMilliseconds);
        }

        /// <summary>
        /// The method closes an existing position.
        /// </summary>
        /// <returns>Can not be null.</returns>
        public ClosePositionResult Close()
        {
            return this.DataTrade.Server.ClosePosition(this.OrderId);
        }

        /// <summary>
        /// The method closes an existing position.
        /// </summary>
        /// <param name="timeoutInMilliseconds">Timeout of the operation ins milliseconds.</param>
        /// <returns>Can not be null.</returns>
        public ClosePositionResult CloseEx(int timeoutInMilliseconds)
        {
            return this.DataTrade.Server.ClosePositionEx(this.OrderId, timeoutInMilliseconds);
        }

        /// <summary>
        /// The method closes an existing position.
        /// </summary>
        /// <param name="operationId">
        /// Can be null, in this case FDK generates a new unique operation ID automatically.
        /// Otherwise, please use GenerateOperationId method of DataClient object.
        /// </param>
        /// <returns>Can not be null.</returns>
        public ClosePositionResult CloseEx(string operationId)
        {
            return this.DataTrade.Server.ClosePositionEx(this.OrderId, operationId);
        }

        /// <summary>
        /// The method closes an existing position.
        /// </summary>
        /// <param name="operationId">
        /// Can be null, in this case FDK generates a new unique operation ID automatically.
        /// Otherwise, please use GenerateOperationId method of DataClient object.
        /// </param>
        /// <param name="timeoutInMilliseconds">Timeout of the operation ins milliseconds.</param>
        /// <returns>Can not be null.</returns>
        public ClosePositionResult CloseEx(string operationId, int timeoutInMilliseconds)
        {
            return this.DataTrade.Server.ClosePositionEx(this.OrderId, operationId, timeoutInMilliseconds);
        }

        /// <summary>
        /// Closes an existing position partially; not valid for pending orders.
        /// </summary>
        /// <param name="volume">Closing volume.</param>
        /// <returns>Can not be null.</returns>
        public ClosePositionResult ClosePartially(double volume)
        {
            return this.DataTrade.Server.ClosePositionPartially(this.OrderId, volume);
        }

        /// <summary>
        /// Closes an existing position partially; not valid for pending orders.
        /// </summary>
        /// <param name="volume">Closing volume.</param>
        /// <param name="timeoutInMilliseconds">Timeout of the operation in milliseconds.</param>
        /// <returns>Can not be null.</returns>
        public ClosePositionResult ClosePartiallyEx(double volume, int timeoutInMilliseconds)
        {
            return this.DataTrade.Server.ClosePositionPartiallyEx(this.OrderId, volume, timeoutInMilliseconds);
        }

        /// <summary>
        /// Closes an existing position partially; not valid for pending orders.
        /// </summary>
        /// <param name="volume">Closing volume.</param>
        /// <param name="operationId">
        /// Can be null, in this case FDK generates a new unique operation ID automatically.
        /// Otherwise, please use GenerateOperationId method of DataClient object.
        /// </param>
        public ClosePositionResult ClosePartiallyEx(double volume, string operationId)
        {
            return this.DataTrade.Server.ClosePositionPartiallyEx(this.OrderId, volume, operationId);
        }

        /// <summary>
        /// Closes an existing position partially; not valid for pending orders.
        /// </summary>
        /// <param name="volume">Closing volume.</param>
        /// <param name="operationId">
        /// Can be null, in this case FDK generates a new unique operation ID automatically.
        /// Otherwise, please use GenerateOperationId method of DataClient object.
        /// </param>
        /// <param name="timeoutInMilliseconds">Timeout of the operation in milliseconds.</param>
        public ClosePositionResult ClosePartiallyEx(double volume, string operationId, int timeoutInMilliseconds)
        {
            return this.DataTrade.Server.ClosePositionPartiallyEx(this.OrderId, volume, operationId, timeoutInMilliseconds);
        }

        /// <summary>
        /// Closes by two orders.
        /// </summary>
        /// <param name="other">Another order; can not be null.</param>
        /// <returns>True, if the operation has been succeeded; otherwise false.</returns>
        /// <returns>Can not be null.</returns>
        public bool CloseBy(TradeRecord other)
        {
            return this.DataTrade.Server.CloseByPositions(this.OrderId, other.OrderId);
        }

        /// <summary>
        /// Closes by two orders.
        /// </summary>
        /// <param name="other">Another order; can not be null.</param>
        /// <param name="timeoutInMilliseconds">Timeout of the operation in milliseconds.</param>
        /// <returns>True, if the operation has been succeeded; otherwise false.</returns>
        /// <returns>Can not be null.</returns>
        public bool CloseByEx(TradeRecord other, int timeoutInMilliseconds)
        {
            return this.CloseByEx(null, other, timeoutInMilliseconds);
        }

        /// <summary>
        /// Closes by two orders.
        /// </summary>
        /// <param name="operationId">Operation Id</param>
        /// <param name="other">Another order; can not be null.</param>
        /// <param name="timeoutInMilliseconds">Timeout of the operation in milliseconds.</param>
        /// <returns>True, if the operation has been succeeded; otherwise false.</returns>
        /// <returns>Can not be null.</returns>
        public bool CloseByEx(string operationId, TradeRecord other, int timeoutInMilliseconds)
        {
            return this.DataTrade.Server.CloseByPositionsEx(operationId, this.OrderId, other.OrderId, timeoutInMilliseconds);
        }

        /// <summary>
        /// Returns formatted string for the class instance.
        /// </summary>
        /// <returns>can not be null</returns>
        public override string ToString()
        {
            return string.Format("Id = {0}; {1} {2} {3}; Price = {4}; Volume = {5}; SP = {8}; SL = {6}; TP = {7}", this.OrderId, this.Symbol, this.Type, this.Side, this.Price, this.Volume, this.StopLoss, this.TakeProfit, this.StopPrice);
        }

        #endregion
    }
}
