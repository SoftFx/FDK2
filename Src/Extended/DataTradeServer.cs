namespace TickTrader.FDK.Extended
{
    using System;
    using System.Collections.Generic;    
    using Common;
    using OrderEntry;

    /// <summary>
    /// The class contains methods, which are executed in server side.
    /// </summary>
    public class DataTradeServer
    {
        internal DataTradeServer(DataTrade dataTrade)
        {
            dataTrade_ = dataTrade;
        }

        /// <summary>
        /// The method send two factor response with one time password.
        /// </summary>
        /// <param name="reason">Two factor response reason</param>
        /// <param name="otp">One time password</param>
        /// <returns>can not be null.</returns>
        public void SendTwoFactorResponse(TwoFactorReason reason, string otp)
        {
            if (reason == TwoFactorReason.ClientResponse)
            {
                lock (dataTrade_.synchronizer_)
                {
                    if (dataTrade_.orderEntryTwoFactorLogin_)
                        dataTrade_.orderEntryClient_.TwoFactorLoginResponseAsync(null, otp);

                    if (dataTrade_.tradeCaptureTwoFactorLogin_)
                        dataTrade_.tradeCaptureClient_.TwoFactorLoginResponseAsync(null, otp);
                }
            }
            else if (reason == TwoFactorReason.ClientResume)
            {
                lock (dataTrade_.synchronizer_)
                {
                    if (dataTrade_.orderEntryTwoFactorLogin_)
                        dataTrade_.orderEntryClient_.TwoFactorLoginResumeAsync(null);

                    if (dataTrade_.tradeCaptureTwoFactorLogin_)
                        dataTrade_.tradeCaptureClient_.TwoFactorLoginResumeAsync(null);
                }
            }
            else
                throw new Exception("Invalid two factor reason : " + reason);
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public TradeServerInfo GetTradeServerInfo()
        {
            return GetTradeServerInfoEx(dataTrade_.synchOperationTimeout_);
        }

        /// <summary>
        /// </summary>
        /// <param name="timeoutInMilliseconds"></param>
        /// <returns></returns>
        public TradeServerInfo GetTradeServerInfoEx(int timeoutInMilliseconds)
        {
            return dataTrade_.orderEntryClient_.GetTradeServerInfo(timeoutInMilliseconds);
        }

        /// <summary>
        /// The method returns the current account information.
        /// </summary>
        /// <returns>Can not be null.</returns>
        public AccountInfo GetAccountInfo()
        {
            return GetAccountInfoEx(dataTrade_.synchOperationTimeout_);
        }

        /// <summary>
        /// The method returns the current account information.
        /// </summary>
        /// <param name="timeoutInMilliseconds">timeout of the synchrnous operation.</param>
        /// <returns>Can not be null.</returns>
        public AccountInfo GetAccountInfoEx(int timeoutInMilliseconds)
        {
            return dataTrade_.orderEntryClient_.GetAccountInfo(timeoutInMilliseconds);
        }

        /// <summary>
        /// The method returns the current session information.
        /// </summary>
        /// <returns>Can not be null.</returns>
        public SessionInfo GetSessionInfo()
        {
            return GetSessionInfoEx(dataTrade_.synchOperationTimeout_);
        }

        /// <summary>
        /// The method returns the current session information.
        /// </summary>
        /// <param name="timeoutInMilliseconds">timeout of the synchrnous operation.</param>
        /// <returns>Can not be null.</returns>
        public SessionInfo GetSessionInfoEx(int timeoutInMilliseconds)
        {
            return dataTrade_.orderEntryClient_.GetSessionInfo(timeoutInMilliseconds);
        }

        /// <summary>
        /// The method returns all trade records for the account.
        /// </summary>
        /// <returns>can not be null</returns>
        public TradeRecord[] GetTradeRecords()
        {
            return GetTradeRecordsEx(dataTrade_.synchOperationTimeout_);
        }

        /// <summary>
        /// The method returns all trade records for the account.
        /// </summary>
        /// <param name="timeoutInMilliseconds">Timeout of the operation in milliseconds</param>
        /// <returns>can not be null</returns>
        public TradeRecord[] GetTradeRecordsEx(int timeoutInMilliseconds)
        {
            using (OrderEnumerator orderEnumerator = dataTrade_.orderEntryClient_.GetOrders(timeoutInMilliseconds))
            {
                TradeRecord[] tradeRecords = new TradeRecord[orderEnumerator.OrderCount];
                int index = 0;

                for
                (
                    ExecutionReport executionReport = orderEnumerator.Next(timeoutInMilliseconds);
                    executionReport != null;
                    executionReport = orderEnumerator.Next(timeoutInMilliseconds)
                )
                {
                    TradeRecord tradeRecord = dataTrade_.GetTradeRecord(executionReport);
                    tradeRecords[index] = tradeRecord;

                    ++index;
                }

                return tradeRecords;
            }
        }


        /// <summary>
        /// The method returns all positions for the account.
        /// </summary>
        /// <returns>can not be null</returns>
        public Position[] GetPositions()
        {
            return GetPositionsEx(dataTrade_.synchOperationTimeout_);
        }

        /// <summary>
        /// The method returns all positions for the account.
        /// </summary>
        /// <param name="timeoutInMilliseconds">Timeout of the operation in milliseconds</param>
        /// <returns>can not be null</returns>
        public Position[] GetPositionsEx(int timeoutInMilliseconds)
        {
            return dataTrade_.orderEntryClient_.GetPositions(timeoutInMilliseconds);
        }

        /// <summary>
        /// The method opens a new order.
        /// </summary>
        /// <param name="symbol">Trading currency pair symbol; can not be null.</param>
        /// <param name="command">Market, limit or stop.</param>
        /// <param name="side">Order side: buy or sell.</param>
        /// <param name="volume">Requsted volume.</param>
        /// <param name="maxVisibleVolume">Max visible volume.</param>
        /// <param name="price">Activating price for pending orders; price threshold for market orders.</param>
        /// <param name="stopPrice">Stop price.</param>
        /// <param name="stopLoss">Stop loss price.</param>
        /// <param name="takeProfit">Take profit price.</param>
        /// <param name="expiration">Expiration time, should be specified for pending orders.</param>
        /// <param name="comment">User defined comment for a new opening order. Null is interpreded as empty string.</param>
        /// <param name="tag">User defined tag for a new opening order. Null is interpreded as empty string.</param>
        /// <param name="magic">User defined magic number for a new opening order. Null is not defined.</param>
        /// <returns>A new order; can not be null.</returns>
        public TradeRecord SendOrder(string symbol, TradeCommand command, OrderSide side, double volume, double? maxVisibleVolume, double? price, double? stopPrice, double? stopLoss, double? takeProfit, DateTime? expiration, string comment, string tag, int? magic)
        {
            return SendOrderEx(Guid.NewGuid().ToString(), symbol, command, side, volume, maxVisibleVolume, price, stopPrice, stopLoss, takeProfit, expiration, comment, tag, magic, dataTrade_.synchOperationTimeout_);
        }

        /// <summary>
        /// The method opens a new order.
        /// </summary>
        /// <param name="symbol">Trading currency pair symbol; can not be null.</param>
        /// <param name="command">Market, limit or stop.</param>
        /// <param name="side">Trade record side: buy or sell.</param>
        /// <param name="volume">Requsted volume.</param>
        /// <param name="maxVisibleVolume">Max visible volume.</param>
        /// <param name="price">Activating price for pending orders; price threshold for market orders.</param>
        /// <param name="stopPrice">Stop price.</param>
        /// <param name="stopLoss">Stop loss price.</param>
        /// <param name="takeProfit">Take profit price.</param>
        /// <param name="expiration">Expiration time, should be specified for pending orders.</param>
        /// <param name="comment">User defined comment for a new opening order. Null is interpreded as empty string.</param>
        /// <param name="tag">User defined tag for a new opening order. Null is interpreded as empty string.</param>
        /// <param name="magic">User defined magic number for a new opening order. Null is not defined.</param>
        /// <param name="timeoutInMilliseconds">Timeout of the synchronous operation.</param>
        /// <returns>A new trade record; can not be null.</returns>
        public TradeRecord SendOrderEx(string symbol, TradeCommand command, OrderSide side, double volume, double? maxVisibleVolume, double? price, double? stopPrice, double? stopLoss, double? takeProfit, DateTime? expiration, string comment, string tag, int? magic, int timeoutInMilliseconds)
        {
            return SendOrderEx(Guid.NewGuid().ToString(), symbol, command, side, volume, maxVisibleVolume, price, stopPrice, stopLoss, takeProfit, expiration, comment, tag, magic, timeoutInMilliseconds);
        }

        /// <summary>
        /// The method opens a new order.
        /// </summary>
        /// <param name="operationId">
        /// Can be null, in this case FDK generates a new unique operation ID automatically.
        /// Otherwise, please use GenerateOperationId method of DataClient object.
        /// </param>
        /// <param name="symbol">Trading currency pair symbol; can not be null.</param>
        /// <param name="command">Market, limit or stop.</param>
        /// <param name="side">Trade record side: buy or sell.</param>
        /// <param name="volume">Requsted volume.</param>
        /// <param name="maxVisibleVolume">Max visible volume.</param>
        /// <param name="price">Activating price for pending orders; price threshold for market orders.</param>
        /// <param name="stopPrice">Stop price.</param>
        /// <param name="stopLoss">Stop loss price.</param>
        /// <param name="takeProfit">Take profit price.</param>
        /// <param name="expiration">Expiration time, should be specified for pending orders.</param>
        /// <param name="comment">User defined comment for a new opening order. Null is interpreded as empty string.</param>
        /// <param name="tag">User defined tag for a new opening order. Null is interpreded as empty string.</param>
        /// <param name="magic">User defined magic number for a new opening order. Null is not defined.</param>
        /// <returns>A new trade record; can not be null.</returns>
        public TradeRecord SendOrderEx(string operationId, string symbol, TradeCommand command, OrderSide side, double volume, double? maxVisibleVolume, double? price, double? stopPrice, double? stopLoss, double? takeProfit, DateTime? expiration, string comment, string tag, int? magic)
        {
            return SendOrderEx(operationId, symbol, command, side, volume, maxVisibleVolume, price, stopPrice, stopLoss, takeProfit, expiration, comment, tag, magic, dataTrade_.synchOperationTimeout_);
        }

        /// <summary>
        /// The method opens a new order.
        /// </summary>
        /// <param name="operationId">
        /// Can be null, in this case FDK generates a new unique operation ID automatically.
        /// Otherwise, please use GenerateOperationId method of DataClient object.
        /// </param>
        /// <param name="symbol">Trading currency pair symbol; can not be null.</param>
        /// <param name="command">Market, limit or stop.</param>
        /// <param name="side">Trade record side: buy or sell.</param>
        /// <param name="volume">Requsted volume.</param>
        /// <param name="maxVisibleVolume">Max visible volume.</param>
        /// <param name="price">Activating price for pending orders; price threshold for market orders.</param>
        /// <param name="stopPrice">Stop price.</param>
        /// <param name="stopLoss">Stop loss price.</param>
        /// <param name="takeProfit">Take profit price.</param>
        /// <param name="expiration">Expiration time, should be specified for pending orders.</param>
        /// <param name="comment">User defined comment for a new opening order. Null is interpreded as empty string.</param>
        /// <param name="tag">User defined tag for a new opening order. Null is interpreded as empty string.</param>
        /// <param name="magic">User defined magic number for a new opening order. Null is not defined.</param>
        /// <param name="timeoutInMilliseconds">Timeout of the synchronous operation.</param>
        /// <returns>A new trade record; can not be null.</returns>
        TradeRecord SendOrderEx(string operationId, string symbol, TradeCommand command, OrderSide side, double volume, double? maxVisibleVolume, double? price, double? stopPrice, double? stopLoss, double? takeProfit, DateTime? expiration, string comment, string tag, int? magic, int timeoutInMilliseconds)
        {
            OrderType orderType;
            OrderTimeInForce? orderTimeInForce;

            if (command == TradeCommand.Market)
            {
                orderType = OrderType.Market;
                orderTimeInForce = null;
            }
            else if (command == TradeCommand.Limit)
            {
                orderType = OrderType.Limit;

                if (expiration != null)
                {
                    orderTimeInForce = OrderTimeInForce.GoodTillDate;
                }
                else
                    orderTimeInForce = OrderTimeInForce.GoodTillCancel;
            }
            else if (command == TradeCommand.Stop)
            {
                orderType = OrderType.Stop;

                if (expiration != null)
                {
                    orderTimeInForce = OrderTimeInForce.GoodTillDate;
                }
                else
                    orderTimeInForce = OrderTimeInForce.GoodTillCancel;
            }
            else if (command == TradeCommand.IoC)
            {
                orderType = OrderType.Limit;
                orderTimeInForce = OrderTimeInForce.ImmediateOrCancel;
            }
            else if (command == TradeCommand.StopLimit)
            {
                orderType = OrderType.StopLimit;

                if (expiration != null)
                {
                    orderTimeInForce = OrderTimeInForce.GoodTillDate;
                }
                else
                    orderTimeInForce = OrderTimeInForce.GoodTillCancel;
            }
            else
                throw new Exception("Invalid trade command: " + command);

            ExecutionReport[] executionReports = dataTrade_.orderEntryClient_.NewOrder
            (
                operationId, 
                symbol, 
                orderType, 
                side, 
                volume, 
                maxVisibleVolume, 
                price, 
                stopPrice, 
                orderTimeInForce, 
                expiration, 
                stopLoss, 
                takeProfit, 
                comment, 
                tag, 
                magic, 
                timeoutInMilliseconds
            );

            ExecutionReport lastExecutionReport = executionReports[executionReports.Length - 1];

            return dataTrade_.GetTradeRecord(lastExecutionReport);
        }

        /// <summary>
        /// The method modifies an existing trade record.
        /// </summary>
        /// <param name="orderId">An existing pending order ID.</param>
        /// <param name="symbol">Currency pair.</param>
        /// <param name="type">Order type: Limit or Stop.</param>
        /// <param name="side">Order side: buy or sell.</param>
        /// <param name="newVolume">A new volume of pending order.</param>
        /// <param name="newMaxVisibleVolume">A new max visible volume of pending order.</param>
        /// <param name="newPrice">A new price of pending order.</param>
        /// <param name="newStopPrice">A new stop price of pending order.</param>
        /// <param name="newStopLoss">A new stop loss price of pending order.</param>
        /// <param name="newTakeProfit">A new take profit price of pending order.</param>
        /// <param name="newExpiration">A new expiration time.</param>
        /// <param name="newComment">A new comment.</param>
        /// <param name="newTag">A new tag.</param>
        /// <param name="newMagic">A new magic.</param>
        /// <returns>A modified trade record.</returns>
        public TradeRecord ModifyTradeRecord(string orderId, string symbol, OrderType type, OrderSide side, double? newVolume, double? newMaxVisibleVolume, double? newPrice, double? newStopPrice, double? newStopLoss, double? newTakeProfit, DateTime? newExpiration, bool inFlightMitigation, double? currentQty, string newComment, string newTag, int? newMagic)
        {
            return ModifyTradeRecordEx(orderId, symbol, type, side, newVolume, newMaxVisibleVolume, newPrice, newStopPrice, newStopLoss, newTakeProfit, newExpiration, inFlightMitigation, currentQty, newComment, newTag, newMagic, dataTrade_.synchOperationTimeout_);
        }

        /// <summary>
        /// The method modifies an existing trade record.
        /// </summary>
        /// <param name="orderId">An existing pending order ID.</param>
        /// <param name="symbol">Currency pair.</param>
        /// <param name="type">Order type: Limit or Stop.</param>
        /// <param name="side">Order side: buy or sell.</param>
        /// <param name="newVolume">A new volume of pending order.</param>
        /// <param name="newMaxVisibleVolume">A new max visible volume of pending order.</param>
        /// <param name="newPrice">A new price of pending order.</param>
        /// <param name="newStopPrice">A new stop price of pending order.</param>
        /// <param name="newStopLoss">A new stop loss price of pending order.</param>
        /// <param name="newTakeProfit">A new take profit price of pending order.</param>
        /// <param name="newExpiration">A new expiration time.</param>
        /// <param name="newComment">A new comment.</param>
        /// <param name="newTag">A new tag.</param>
        /// <param name="newMagic">A new magic.</param>
        /// <param name="timeoutInMilliseconds">Timeout of the synchronous operation.</param>
        /// <returns>A modified trade record.</returns>
        public TradeRecord ModifyTradeRecordEx(string orderId, string symbol, OrderType type, OrderSide side, double? newVolume, double? newMaxVisibleVolume, double? newPrice, double? newStopPrice, double? newStopLoss, double? newTakeProfit, DateTime? newExpiration, bool inFlightMitigation, double? currentQty, string newComment, string newTag, int? newMagic, int timeoutInMilliseconds)
        {
            return ModifyTradeRecordEx(Guid.NewGuid().ToString(), orderId, null, symbol, type, side, newVolume, newMaxVisibleVolume, newPrice, newStopPrice, newStopLoss, newTakeProfit, newExpiration, inFlightMitigation, currentQty, newComment, newTag, newMagic, timeoutInMilliseconds);
        }

        /// <summary>
        /// The method modifies an existing trade record.
        /// </summary>
        /// <param name="operationId">
        /// Can be null, in this case FDK generates a new unique operation ID automatically.
        /// Otherwise, please use GenerateOperationId method of DataClient object.
        /// </param>
        /// <param name="orderId">An existing pending order ID.</param>
        /// <param name="symbol">Currency pair.</param>
        /// <param name="type">Order type: Limit or Stop.</param>
        /// <param name="side">Order side: buy or sell.</param>
        /// <param name="newVolume">A new volume of pending order.</param>
        /// <param name="newMaxVisibleVolume">A new max visible volume of pending order.</param>
        /// <param name="newPrice">A new price of pending order.</param>
        /// <param name="newStopPrice">A new stop price of pending order.</param>
        /// <param name="newStopLoss">A new stop loss price of pending order.</param>
        /// <param name="newTakeProfit">A new take profit price of pending order.</param>
        /// <param name="newExpiration">A new expiration time.</param>
        /// <param name="newComment">A new comment.</param>
        /// <param name="newTag">A new tag.</param>
        /// <param name="newMagic">A new magic.</param>
        /// <returns>A modified trade record.</returns>
        public TradeRecord ModifyTradeRecordEx(string operationId, string orderId, string symbol, OrderType type, OrderSide side, double? newVolume, double? newMaxVisibleVolume, double? newPrice, double? newStopPrice, double? newStopLoss, double? newTakeProfit, DateTime? newExpiration, bool inFlightMitigation, double? currentQty, string newComment, string newTag, int? newMagic)
        {
            return ModifyTradeRecordEx(operationId, orderId, null, symbol, type, side, newVolume, newMaxVisibleVolume, newPrice, newStopPrice, newStopLoss, newTakeProfit, newExpiration, inFlightMitigation, currentQty, newComment, newTag, newMagic, dataTrade_.synchOperationTimeout_);
        }

        /// <summary>
        /// The method modifies an existing trade record.
        /// </summary>
        /// <param name="operationId">
        /// Can be null, in this case FDK generates a new unique operation ID automatically.
        /// Otherwise, please use GenerateOperationId method of DataClient object.
        /// </param>
        /// <param name="orderId">An existing pending order ID.</param>
        /// <param name="symbol">Currency pair.</param>
        /// <param name="type">Order type: Limit or Stop.</param>
        /// <param name="side">Order side: buy or sell.</param>
        /// <param name="newVolume">A new volume of pending order.</param>
        /// <param name="newMaxVisibleVolume">A new max visible volume of pending order.</param>
        /// <param name="newPrice">A new price of pending order.</param>
        /// <param name="newStopPrice">A new stop price of pending order.</param>
        /// <param name="newStopLoss">A new stop loss price of pending order.</param>
        /// <param name="newTakeProfit">A new take profit price of pending order.</param>
        /// <param name="newExpiration">A new expiration time.</param>
        /// <param name="newComment">A new comment.</param>
        /// <param name="newTag">A new tag.</param>
        /// <param name="newMagic">A new magic.</param>
        /// <param name="timeoutInMilliseconds">Timeout of the synchronous operation.</param>
        /// <returns>A modified trade record.</returns>
        public TradeRecord ModifyTradeRecordEx(string operationId, string orderId, string clientId, string symbol, OrderType type, OrderSide side, double? newVolume, double? newMaxVisibleVolume, double? newPrice, double? newStopPrice, double? newStopLoss, double? newTakeProfit, DateTime? newExpiration, bool inFlightMitigation, double? currentQty, string newComment, string newTag, int? newMagic, int timeoutInMilliseconds)
        {
            OrderTimeInForce orderTimeInForce;

            if (newExpiration != null)
            {
                orderTimeInForce = OrderTimeInForce.GoodTillDate;
            }
            else
                orderTimeInForce = OrderTimeInForce.GoodTillCancel;

            ExecutionReport[] executionReports = dataTrade_.orderEntryClient_.ReplaceOrder
            (
                operationId,
                clientId,
                orderId,
                symbol,
                type,
                side,
                newVolume.GetValueOrDefault(),
                newMaxVisibleVolume,
                newPrice,
                newStopPrice,
                orderTimeInForce,
                newExpiration,
                newStopLoss,
                newTakeProfit,
                inFlightMitigation,
                currentQty,
                newComment,
                newTag,
                newMagic,
                timeoutInMilliseconds
            );

            ExecutionReport lastExecutionReport = executionReports[executionReports.Length - 1];

            return dataTrade_.GetTradeRecord(lastExecutionReport);
        }

        /// <summary>
        /// The method deletes an existing pending order.
        /// </summary>
        /// <param name="orderId">An existing pending order ID.</param>
        /// <param name="side">Order side: buy or sell.</param>
        public void DeletePendingOrder(string orderId)
        {
            DeletePendingOrderEx(Guid.NewGuid().ToString(), orderId, null, dataTrade_.synchOperationTimeout_);
        }

        /// <summary>
        /// The method deletes an existing pending order.
        /// </summary>
        /// <param name="orderId">An existing pending order ID.</param>
        /// <param name="side">Order side: buy or sell.</param>
        /// <param name="timeoutInMilliseconds">Timeout of the synchronous operation.</param>
        public void DeletePendingOrderEx(string orderId, int timeoutInMilliseconds)
        {
            DeletePendingOrderEx(Guid.NewGuid().ToString(), orderId, null, timeoutInMilliseconds);
        }

        /// <summary>
        /// The method deletes an existing pending order.
        /// </summary>
        /// <param name="operationId">
        /// <param name="orderId">An existing pending order ID.</param>
        /// <param name="side">Order side: buy or sell.</param>
        /// <param name="timeoutInMilliseconds">Timeout of the synchronous operation.</param>
        public void DeletePendingOrderEx(string operationId, string orderId, int timeoutInMilliseconds)
        {
            DeletePendingOrderEx(operationId, orderId, null, timeoutInMilliseconds);
        }

        /// <summary>
        /// The method deletes an existing pending order.
        /// </summary>
        /// <param name="operationId">
        /// Can be null, in this case FDK generates a new unique operation ID automatically.
        /// Otherwise, please use GenerateOperationId method of DataClient object.
        /// </param>
        /// <param name="orderId">An existing pending order ID.</param>
        /// <param name="side">Order side: buy or sell.</param>
        public void DeletePendingOrderEx(string operationId, string orderId)
        {
            DeletePendingOrderEx(operationId, orderId, null, dataTrade_.synchOperationTimeout_);
        }

        /// <summary>
        /// The method deletes an existing pending order.
        /// </summary>
        /// <param name="operationId">
        /// Can be null, in this case FDK generates a new unique operation ID automatically.
        /// Otherwise, please use GenerateOperationId method of DataClient object.
        /// </param>
        /// <param name="orderId">An existing pending order ID.</param>
        /// <param name="side">Order side: buy or sell.</param>
        /// <param name="timeoutInMilliseconds">Timeout of the synchronous operation.</param>
        void DeletePendingOrderEx(string operationId, string orderId, string clientId, int timeoutInMilliseconds)
        {
            dataTrade_.orderEntryClient_.CancelOrder(operationId, clientId, orderId, timeoutInMilliseconds);
        }

        /// <summary>
        /// The method closes an existing position.
        /// The method is supported by Gross account only.
        /// </summary>
        /// <param name="orderId">Order ID; can not be null.</param>
        /// <returns>Can not be null.</returns>
        public ClosePositionResult ClosePosition(string orderId)
        {
            return ClosePositionEx(orderId, Guid.NewGuid().ToString(), dataTrade_.synchOperationTimeout_);
        }

        /// <summary>
        /// The method closes an existing position.
        /// The method is supported by Gross account only.
        /// </summary>
        /// <param name="orderId">Order ID; can not be null.</param>
        /// <param name="operationId">
        /// Can be null, in this case FDK generates a new unique operation ID automatically.
        /// Otherwise, please use GenerateOperationId method of DataClient object.
        /// </param>
        /// <returns>Can not be null.</returns>
        public ClosePositionResult ClosePositionEx(string orderId, string operationId)
        {
            return ClosePositionEx(orderId, operationId, dataTrade_.synchOperationTimeout_);
        }

        /// <summary>
        /// The method closes an existing position.
        /// The method is supported by Gross account only.
        /// </summary>
        /// <param name="orderId">Order ID; can not be null.</param>
        /// <param name="timeoutInMilliseconds">Timeout of the operation in milliseconds.</param>
        /// <returns>Can not be null.</returns>
        public ClosePositionResult ClosePositionEx(string orderId, int timeoutInMilliseconds)
        {
            return ClosePositionEx(orderId, Guid.NewGuid().ToString(), timeoutInMilliseconds);
        }

        /// <summary>
        /// The method closes an existing position.
        /// The method is supported by Gross account only.
        /// </summary>
        /// <param name="orderId">Order ID; can not be null.</param>
        /// <param name="operationId">
        /// Can be null, in this case FDK generates a new unique operation ID automatically.
        /// Otherwise, please use GenerateOperationId method of DataClient object.
        /// </param>
        /// <param name="timeoutInMilliseconds">Timeout of the operation in milliseconds.</param>
        /// <returns>Can not be null.</returns>
        public ClosePositionResult ClosePositionEx(string orderId, string operationId, int timeoutInMilliseconds)
        {
            ExecutionReport[] executionReports = dataTrade_.orderEntryClient_.ClosePosition(operationId, orderId, null, timeoutInMilliseconds);

            ExecutionReport lastExecutionReport = executionReports[executionReports.Length - 1];

            return GetClosePositionResult(lastExecutionReport);
        }

        /// <summary>
        /// The method closes an existing market order.
        /// The method is supported by Gross account only.
        /// </summary>
        /// <param name="orderId">Order ID; can not be null.</param>
        /// <param name="volume">closing volume</param>
        /// <returns></returns>
        public ClosePositionResult ClosePositionPartially(string orderId, double volume)
        {
            return ClosePositionPartiallyEx(orderId, volume, Guid.NewGuid().ToString(), dataTrade_.synchOperationTimeout_);
        }

        /// <summary>
        /// The method closes an existing market order.
        /// The method is supported by Gross account only.
        /// </summary>
        /// <param name="orderId">Order ID; can not be null.</param>
        /// <param name="volume">closing volume</param>
        /// <param name="timeoutInMilliseconds">Timeout of the operation in milliseconds.</param>
        /// <returns></returns>
        public ClosePositionResult ClosePositionPartiallyEx(string orderId, double volume, int timeoutInMilliseconds)
        {
            return ClosePositionPartiallyEx(orderId, volume, Guid.NewGuid().ToString(), timeoutInMilliseconds);
        }

        /// <summary>
        /// The method closes an existing market order.
        /// The method is supported by Gross account only.
        /// </summary>
        /// <param name="orderId">Order ID; can not be null.</param>
        /// <param name="volume">closing volume</param>
        /// <param name="operationId">
        /// Can be null, in this case FDK generates a new unique operation ID automatically.
        /// Otherwise, please use GenerateOperationId method of DataClient object.
        /// </param>
        /// <returns></returns>
        public ClosePositionResult ClosePositionPartiallyEx(string orderId, double volume, string operationId)
        {
            return ClosePositionPartiallyEx(orderId, volume, operationId, dataTrade_.synchOperationTimeout_);
        }

        /// <summary>
        /// The method closes an existing market order.
        /// The method is supported by Gross account only.
        /// </summary>
        /// <param name="orderId">Order ID; can not be null.</param>
        /// <param name="volume">closing volume</param>
        /// <param name="operationId">
        /// Can be null, in this case FDK generates a new unique operation ID automatically.
        /// Otherwise, please use GenerateOperationId method of DataClient object.
        /// </param>
        /// <param name="timeoutInMilliseconds">Timeout of the operation in milliseconds.</param>
        /// <returns></returns>
        public ClosePositionResult ClosePositionPartiallyEx(string orderId, double volume, string operationId, int timeoutInMilliseconds)
        {
            ExecutionReport[] executionReports = dataTrade_.orderEntryClient_.ClosePosition(operationId, orderId, volume, timeoutInMilliseconds);                       

            ExecutionReport tradeExecutionReport = executionReports[1];

            return GetClosePositionResult(tradeExecutionReport);
        }

        /// <summary>
        /// The method closes by two orders.
        /// The method is supported by Gross account only.
        /// </summary>
        /// <param name="firstOrderId">The first order ID; can not be null.</param>
        /// <param name="secondOrderId">The second order ID; can not be null.</param>
        /// <returns>True, if the operation has been succeeded; otherwise false.</returns>
        public bool CloseByPositions(string firstOrderId, string secondOrderId)
        {
            return CloseByPositionsEx(firstOrderId, secondOrderId, dataTrade_.synchOperationTimeout_);
        }

        /// <summary>
        /// The method closes by two orders.
        /// The method is supported by Gross account only.
        /// </summary>
        /// <param name="firstOrderId">The first order ID; can not be null.</param>
        /// <param name="secondOrderId">The second order ID; can not be null.</param>
        /// <param name="timeoutInMilliseconds">Timeout of the operation in milliseconds.</param>
        /// <returns>True, if the operation has been succeeded; otherwise false.</returns>
        public bool CloseByPositionsEx(string firstOrderId, string secondOrderId, int timeoutInMilliseconds)
        {
            return CloseByPositionsEx(Guid.NewGuid().ToString(), firstOrderId, secondOrderId, timeoutInMilliseconds);
        }

        /// <summary>
        /// The method closes by two orders.
        /// The method is supported by Gross account only.
        /// </summary>
        /// <param name="operationId">Operation Id</param>
        /// <param name="firstOrderId">The first order ID; can not be null.</param>
        /// <param name="secondOrderId">The second order ID; can not be null.</param>
        /// <param name="timeoutInMilliseconds">Timeout of the operation in milliseconds.</param>
        /// <returns>True, if the operation has been succeeded; otherwise false.</returns>
        public bool CloseByPositionsEx(string operationId, string firstOrderId, string secondOrderId, int timeoutInMilliseconds)
        {
            dataTrade_.orderEntryClient_.ClosePositionBy(operationId, firstOrderId, secondOrderId, timeoutInMilliseconds);

            return true;
        }
/*
        /// <summary>
        /// The method closes all opened market orders.
        /// The method is supported by Gross account only.
        /// </summary>
        /// <returns>Number of affected orders.</returns>
        public int CloseAllPositions()
        {
            return CloseAllPositionsEx(dataTrade_.synchOperationTimeout_);
        }

        /// <summary>
        /// The method closes all opened market orders.
        /// The method is supported by Gross account only.
        /// </summary>
        /// <param name="timeoutInMilliseconds">Timeout of the operation in milliseconds.</param>
        /// <returns>Number of affected orders.</returns>
        public int CloseAllPositionsEx(int timeoutInMilliseconds)
        {
            return CloseAllPositionsEx(Guid.NewGuid().ToString(), timeoutInMilliseconds);
        }

        /// <summary>
        /// The method closes all opened market orders.
        /// The method is supported by Gross account only.
        /// </summary>
        /// <param name="operationId">Operation Id</param>
        /// <param name="timeoutInMilliseconds">Timeout of the operation in milliseconds.</param>
        /// <returns>Number of affected orders.</returns>
        public int CloseAllPositionsEx(string operationId, int timeoutInMilliseconds)
        {
            throw new Exception("Not impled");
        }
*/

        /// <summary>
        /// The method starts trade transaction reports receiving.
        /// </summary>
        /// <param name="skipCancel"></param>
        public void SubscribeTradeTransactionReports(bool skipCancel)
        {
            dataTrade_.tradeCaptureClient_.SubscribeTrades(skipCancel, dataTrade_.synchOperationTimeout_);
        }

        /// <summary>
        /// The method starts trade transaction reports receiving.
        /// </summary>
        /// <param name="skipCancel"></param>
        /// <param name="timeoutInMilliseconds"></param>
        public void SubscribeTradeTransactionReportsEx(bool skipCancel, int timeoutInMilliseconds)
        {
            dataTrade_.tradeCaptureClient_.SubscribeTrades(skipCancel, timeoutInMilliseconds);
        }

        /// <summary>
        /// The method stops trade transaction reports receiving.
        /// </summary>
        public void UnsubscribeTradeTransactionReports()
        {
            UnsubscribeTradeTransactionReportsEx(dataTrade_.synchOperationTimeout_);
        }

        /// <summary>
        /// The method stops trade transaction reports receiving.
        /// </summary>
        /// <param name="timeoutInMilliseconds">Timeout of the operation in milliseconds</param>
        public void UnsubscribeTradeTransactionReportsEx(int timeoutInMilliseconds)
        {
            dataTrade_.tradeCaptureClient_.UnsubscribeTrades(timeoutInMilliseconds);
        }

        /// <summary>
        /// The method gets history trades from the server.
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="subscribeToNotifications"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="skipCancel"></param>
        /// <returns></returns>
        public TradeTransactionReports GetTradeTransactionReportsHistory(TimeDirection direction, DateTime? startTime, DateTime? endTime, bool skipCancel)
        {
            return GetTradeTransactionReportsHistoryEx(direction, startTime, endTime, skipCancel, dataTrade_.synchOperationTimeout_);
        }

        /// <summary>
        /// The method gets history trades from the server.
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="subscribeToNotifications"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="skipCancel"></param>
        /// <param name="timeoutInMilliseconds"></param>
        /// <returns></returns>
        public TradeTransactionReports GetTradeTransactionReportsHistoryEx(TimeDirection direction, DateTime? startTime, DateTime? endTime, bool skipCancel, int timeoutInMilliseconds)
        {
            return new TradeTransactionReports(dataTrade_, direction, startTime, endTime, skipCancel, timeoutInMilliseconds);
        }

        /// <summary>
        /// The method gets snapshot of trade transaction reports and subscribe to notifications.
        /// All reports will be received as events.
        /// </summary>
        /// <param name="direction">Time direction of reports snapshot</param>>
        /// <param name="subscribeToNotifications">Specify false to receive only history snapshot; true to receive history snapshot and updates.</param>
        /// <param name="from">
        /// Optional parameter, which specifies the start date and time for trade transaction reports.
        /// You should specify the parameter, if you specified "to" parameter.
        /// The parameter is supported since 1.6 FIX version.
        /// </param>
        /// <param name="to">
        /// Optional parameter, which specifies the finish date and time for trade transaction reports.
        /// You should specify the parameter, if you specified "from" parameter.
        /// The parameter is supported since 1.6 FIX version.
        /// </param>
        /// <returns>Can not be null.</returns>
        public TradeTransactionReportsEnumerator GetTradeTransactionReports(TimeDirection direction, bool subscribeToNotifications, DateTime? from, DateTime? to, bool? skipCancel)
        {
            return this.GetTradeTransactionReports(direction, subscribeToNotifications, from, to, 16, skipCancel);
        }

        /// <summary>
        /// The method gets snapshot of trade transaction reports and subscribe to notifications.
        /// All reports will be received as events.
        /// </summary>
        /// <param name="direction">Time direction of reports snapshot</param>>
        /// <param name="subscribeToNotifications">Specifye false to receive only history snapshot; true to receive history snapshot and updates.</param>
        /// <param name="from">
        /// Optional parameter, which specifies the start date and time for trade transaction reports.
        /// You should specify the parameter, if you specified "to" parameter.
        /// The parameter is supported since 1.6 FIX version.
        /// </param>
        /// <param name="to">
        /// Optional parameter, which specifies the finish date and time for trade transaction reports.
        /// You should specify the parameter, if you specified "from" parameter.
        /// The parameter is supported since 1.6 FIX version.
        /// </param>
        /// <param name="preferedBufferSize"> Specifies number of reports requested at once. Server has itself limitation and if you specify out of range value it will be ignored.</param>
        /// <returns>Can not be null.</returns>
        public TradeTransactionReportsEnumerator GetTradeTransactionReports(TimeDirection direction, bool subscribeToNotifications, DateTime? from, DateTime? to, int preferedBufferSize, bool? skipCancel)
        {
            return GetTradeTransactionReportsEx(direction, subscribeToNotifications, from, to, preferedBufferSize, skipCancel, dataTrade_.synchOperationTimeout_);
        }

        /// <summary>
        /// The method gets snapshot of trade transaction reports and subscribe to notifications.
        /// All reports will be received as events.
        /// </summary>
        /// <param name="direction">Time direction of reports snapshot</param>
        /// <param name="subscribeToNotifications">Specifye false to receive only history snapshot; true to receive history snapshot and updates.</param>
        /// <param name="from">
        /// Optional parameter, which specifies the start date and time for trade transaction reports.
        /// You should specify the parameter, if you specified "to" parameter.
        /// The parameter is supported since 1.6 FIX version.
        /// </param>
        /// <param name="to">
        /// Optional parameter, which specifies the finish date and time for trade transaction reports.
        /// You should specify the parameter, if you specified "from" parameter.
        /// The parameter is supported since 1.6 FIX version.
        /// </param>
        /// <param name="preferedBufferSize"> Specifies number of reports requested at once. Server has itself limitation and if you specify out of range value it will be ignored.</param>
        /// <param name="timeoutInMilliseconds">Timeout of the operation in milliseconds</param>
        /// <returns>Can not be null.</returns>
        public TradeTransactionReportsEnumerator GetTradeTransactionReportsEx(TimeDirection direction, bool subscribeToNotifications, DateTime? from, DateTime? to, int preferedBufferSize, bool? skipCancel, int timeoutInMilliseconds)
        {
            bool skipCancelValue = skipCancel != null ? skipCancel.Value : false;

            if (subscribeToNotifications)
            {
                dataTrade_.tradeCaptureClient_.SubscribeTrades(skipCancelValue, timeoutInMilliseconds);
            }

            try
            {
                TradeTransactionReports tradeTransactionReports = new TradeTransactionReports(dataTrade_, direction, from, to, skipCancelValue, timeoutInMilliseconds);

                return tradeTransactionReports.GetEnumerator();
            }
            catch
            {
                if (subscribeToNotifications)
                {
                    dataTrade_.tradeCaptureClient_.UnsubscribeTrades(timeoutInMilliseconds);
                }

                throw;
            }
        }

        ClosePositionResult GetClosePositionResult(ExecutionReport executionReport)
        {
            if (executionReport.ExecutionType != ExecutionType.Trade)
                throw new Exception("Invalid execution report : " + executionReport.ExecutionType);

            ClosePositionResult closePositionResult = new ClosePositionResult();            
            closePositionResult.Sucess = true;
            closePositionResult.ExecutedVolume = executionReport.TradeAmount.Value;
            closePositionResult.ExecutedPrice = executionReport.TradePrice.Value;

            return closePositionResult;
        }

        OrderType GetOrderType(TradeCommand tradeCommand)
        {
            switch (tradeCommand)
            {
                case TradeCommand.Market:
                    return OrderType.Market;

                case TradeCommand.Limit:
                    return OrderType.Limit;

                case TradeCommand.Stop:
                    return OrderType.Stop;

                case TradeCommand.StopLimit:
                    return OrderType.StopLimit;

                default:
                    throw new Exception("Invalid trade command : " + tradeCommand);
            }
        }

        DataTrade dataTrade_;
    }
}
