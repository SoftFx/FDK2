﻿using System.Linq;

namespace TickTrader.FDK.Extended
{
    using System;
    using Common;
    using Client;

    /// <summary>
    /// The class contains methods, which are executed in server side.
    /// </summary>
    public class DataTradeServer
    {
        internal DataTradeServer(DataTrade dataTrade)
        {
            dataTrade_ = dataTrade;
        }

        #region 2FA

        public DateTime SendTwoFactorLoginResponse(string oneTimePassword)
        {
            return SendTwoFactorLoginResponseEx(oneTimePassword, dataTrade_.synchOperationTimeout_);
        }

        public DateTime SendTwoFactorLoginResponseEx(string oneTimePassword, int timeoutInMilliseconds)
        {
            lock (dataTrade_.synchronizer_)
            {
                if (dataTrade_.twoFactorLoginState_ != DataTrade.TwoFactorLoginState.Request)
                    throw new Exception("Two factor login is not requested");

                if (dataTrade_.orderEntryTwoFactorLoginState_ == DataTrade.TwoFactorLoginState.Request)
                {                    
                    dataTrade_.orderEntryClient_.TwoFactorLoginResponseAsync(null, oneTimePassword);
                    dataTrade_.orderEntryTwoFactorLoginState_ = DataTrade.TwoFactorLoginState.Response;
                    //Console.WriteLine("orderEntryTwoFactorLoginState_:{0}", dataTrade_.orderEntryTwoFactorLoginState_);
                }

                if (dataTrade_.tradeCaptureTwoFactorLoginState_ == DataTrade.TwoFactorLoginState.Request)
                {
                    dataTrade_.tradeCaptureClient_.TwoFactorLoginResponseAsync(null, oneTimePassword);
                    dataTrade_.tradeCaptureTwoFactorLoginState_ = DataTrade.TwoFactorLoginState.Response;
                    //Console.WriteLine("tradeCaptureTwoFactorLoginState_:{0}", dataTrade_.tradeCaptureTwoFactorLoginState_);
                }

                dataTrade_.twoFactorLoginState_ = DataTrade.TwoFactorLoginState.Response;
                //Console.WriteLine("twoFactorLoginState_:{0}", dataTrade_.twoFactorLoginState_);
            }

            if (! dataTrade_.twoFactorLoginEvent_.WaitOne(timeoutInMilliseconds))
                throw new Common.TimeoutException("Method call timed out");

            if (dataTrade_.twoFactorLoginException_ != null)
                throw dataTrade_.twoFactorLoginException_;

            return dataTrade_.twoFactorLoginExpiration_;
        }

        public DateTime SendTwoFactorLoginResume()
        {
            return SendTwoFactorLoginResumeEx(dataTrade_.synchOperationTimeout_);
        }

        public DateTime SendTwoFactorLoginResumeEx(int timeoutInMilliseconds)
        {
            lock (dataTrade_.synchronizer_)
            {
                if (dataTrade_.twoFactorLoginState_ != DataTrade.TwoFactorLoginState.Success)
                    throw new Exception("Two factor login is not succeeded");

                if (dataTrade_.orderEntryTwoFactorLoginState_ == DataTrade.TwoFactorLoginState.Success)
                {
                    dataTrade_.orderEntryClient_.TwoFactorLoginResumeAsync(null);
                    dataTrade_.orderEntryTwoFactorLoginState_ = DataTrade.TwoFactorLoginState.Resume;
                    //Console.WriteLine("orderEntryTwoFactorLoginState_:{0}", dataTrade_.orderEntryTwoFactorLoginState_);
                }

                if (dataTrade_.tradeCaptureTwoFactorLoginState_ == DataTrade.TwoFactorLoginState.Success)
                {
                    dataTrade_.tradeCaptureClient_.TwoFactorLoginResumeAsync(null);
                    dataTrade_.tradeCaptureTwoFactorLoginState_ = DataTrade.TwoFactorLoginState.Resume;
                    //Console.WriteLine("tradeCaptureTwoFactorLoginState_:{0}", dataTrade_.tradeCaptureTwoFactorLoginState_);
                }

                dataTrade_.twoFactorLoginState_ = DataTrade.TwoFactorLoginState.Resume;
                //Console.WriteLine("twoFactorLoginState_:{0}", dataTrade_.twoFactorLoginState_);
            }

            if (! dataTrade_.twoFactorLoginEvent_.WaitOne(timeoutInMilliseconds))
                throw new Common.TimeoutException("Method call timed out");

            if (dataTrade_.twoFactorLoginException_ != null)
                throw dataTrade_.twoFactorLoginException_;

            return dataTrade_.twoFactorLoginExpiration_;
        }

        #endregion

        #region Server/Account/Session Info

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

        #endregion

        #region Trades

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
            using (GetOrdersEnumerator orderEnumerator = dataTrade_.orderEntryClient_.GetOrders(timeoutInMilliseconds))
            {
                TradeRecord[] tradeRecords = new TradeRecord[orderEnumerator.TotalCount];
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

        #endregion

        #region Send Order

        /// <summary>
        /// The method opens a new order.
        /// </summary>
        /// <param name="symbol">Trading currency pair symbol; can not be null.</param>
        /// <param name="orderType">Market, limit or stop.</param>
        /// <param name="side">Order side: buy or sell.</param>
        /// <param name="volume">Requsted volume.</param>
        /// <param name="maxVisibleVolume">Max visible volume.</param>
        /// <param name="price">Activating price for pending orders; price threshold for market orders.</param>
        /// <param name="stopPrice">Stop price.</param>
        /// <param name="stopLoss">Stop loss price.</param>
        /// <param name="takeProfit">Take profit price.</param>
        /// <param name="orderTimeInForce">TimeInForce</param>
        /// <param name="expiration">Expiration time, should be specified for pending orders.</param>
        /// <param name="comment">User defined comment for a new opening order. Null is interpreded as empty string.</param>
        /// <param name="tag">User defined tag for a new opening order. Null is interpreded as empty string.</param>
        /// <param name="magic">User defined magic number for a new opening order. Null is not defined.</param>
        /// <param name="immediateOrCancelFlag">Flag that shows if the order is ImmediateOrCancel.</param>
        /// <param name="slippage">Slippage.</param>
        /// <param name="oneCancelsTheOtherFlag">Flag OneCancelsTheOther</param>
        /// <param name="ocoEqualVolume">Take the amount from the other order for a new order</param>
        /// <param name="relatedOrderId">Related order for OneCancelsTheOther</param>
        /// <returns>A new order; can not be null.</returns>
        public TradeRecord SendOrder(string symbol, OrderType orderType, OrderSide side, double volume, double? maxVisibleVolume,
            double? price, double? stopPrice, double? stopLoss, double? takeProfit, OrderTimeInForce? orderTimeInForce, DateTime? expiration,
            string comment, string tag, int? magic, bool immediateOrCancelFlag, double? slippage,
            bool oneCancelsTheOtherFlag, bool ocoEqualVolume, long? relatedOrderId)
        {
            return SendOrderEx(Guid.NewGuid().ToString(), symbol, orderType, side, volume, maxVisibleVolume,
                price, stopPrice, stopLoss, takeProfit, orderTimeInForce, expiration,
                comment, tag, magic, immediateOrCancelFlag, slippage,
                oneCancelsTheOtherFlag, ocoEqualVolume, relatedOrderId, null, null, null);
        }

        /// <summary>
        /// The method opens a new order.
        /// </summary>
        /// <param name="symbol">Trading currency pair symbol; can not be null.</param>
        /// <param name="orderType">Market, limit or stop.</param>
        /// <param name="side">Trade record side: buy or sell.</param>
        /// <param name="volume">Requsted volume.</param>
        /// <param name="maxVisibleVolume">Max visible volume.</param>
        /// <param name="price">Activating price for pending orders; price threshold for market orders.</param>
        /// <param name="stopPrice">Stop price.</param>
        /// <param name="stopLoss">Stop loss price.</param>
        /// <param name="takeProfit">Take profit price.</param>
        /// <param name="orderTimeInForce">TimeInForce</param>
        /// <param name="expiration">Expiration time, should be specified for pending orders.</param>
        /// <param name="comment">User defined comment for a new opening order. Null is interpreded as empty string.</param>
        /// <param name="tag">User defined tag for a new opening order. Null is interpreded as empty string.</param>
        /// <param name="magic">User defined magic number for a new opening order. Null is not defined.</param>
        /// <param name="timeoutInMilliseconds">Timeout of the synchronous operation.</param>
        /// <param name="immediateOrCancelFlag">Flag that shows if the order is ImmediateOrCancel.</param>
        /// <param name="slippage">Slippage.</param>
        /// <param name="oneCancelsTheOtherFlag">Flag OneCancelsTheOther</param>
        /// <param name="ocoEqualVolume">Take the amount from the other order for a new order</param>
        /// <param name="relatedOrderId">Related order for OneCancelsTheOther</param>
        /// <param name="triggerType">Contingent order trigger type</param>
        /// <param name="triggerTime">Time of triggering. Use it if TriggerType is OnTime</param>
        /// <param name="orderIdTriggeredBy">Id of related order. Use it if triggerType is OnPendingOrderExpired or OnPendingOrderPartiallyFilled</param>
        /// <returns>A new trade record; can not be null.</returns>
        public TradeRecord SendOrderEx(string symbol, OrderType orderType, OrderSide side, double volume, double? maxVisibleVolume,
            double? price, double? stopPrice, double? stopLoss, double? takeProfit, OrderTimeInForce? orderTimeInForce, DateTime? expiration,
            string comment, string tag, int? magic, int timeoutInMilliseconds, bool immediateOrCancelFlag, double? slippage,
            bool oneCancelsTheOtherFlag, bool ocoEqualVolume, long? relatedOrderId,
            Common.ContingentOrderTriggerType? triggerType, DateTime? triggerTime, long? orderIdTriggeredBy)
        {
            return SendOrderEx(Guid.NewGuid().ToString(), symbol, orderType, side, volume, maxVisibleVolume,
                price, stopPrice, stopLoss, takeProfit, orderTimeInForce, expiration,
                comment, tag, magic, immediateOrCancelFlag, slippage,
                oneCancelsTheOtherFlag, ocoEqualVolume, relatedOrderId,
                triggerType, triggerTime, orderIdTriggeredBy);
        }

        /// <summary>
        /// The method opens a new order.
        /// </summary>
        /// <param name="operationId">
        /// Can be null, in this case FDK generates a new unique operation ID automatically.
        /// Otherwise, please use GenerateOperationId method of DataClient object.
        /// </param>
        /// <param name="symbol">Trading currency pair symbol; can not be null.</param>
        /// <param name="orderType">Market, limit or stop.</param>
        /// <param name="side">Trade record side: buy or sell.</param>
        /// <param name="volume">Requsted volume.</param>
        /// <param name="maxVisibleVolume">Max visible volume.</param>
        /// <param name="price">Activating price for pending orders; price threshold for market orders.</param>
        /// <param name="stopPrice">Stop price.</param>
        /// <param name="stopLoss">Stop loss price.</param>
        /// <param name="takeProfit">Take profit price.</param>
        /// <param name="orderTimeInForce">TimeInForce</param>
        /// <param name="expiration">Expiration time, should be specified for pending orders.</param>
        /// <param name="comment">User defined comment for a new opening order. Null is interpreded as empty string.</param>
        /// <param name="tag">User defined tag for a new opening order. Null is interpreded as empty string.</param>
        /// <param name="magic">User defined magic number for a new opening order. Null is not defined.</param>
        /// <param name="immediateOrCancelFlag">Flag that shows if the order is ImmediateOrCancel.</param>
        /// <param name="slippage">Slippage.</param>
        /// <param name="oneCancelsTheOtherFlag">Flag OneCancelsTheOther</param>
        /// <param name="ocoEqualVolume">Take the amount from the other order for a new order</param>
        /// <param name="relatedOrderId">Related order for OneCancelsTheOther</param>
        /// <param name="triggerType">Contingent order trigger type</param>
        /// <param name="triggerTime">Time of triggering. Use it if TriggerType is OnTime</param>
        /// <param name="orderIdTriggeredBy">Id of related order. Use it if triggerType is OnPendingOrderExpired or OnPendingOrderPartiallyFilled</param>
        /// <returns>A new trade record; can not be null.</returns>
        public TradeRecord SendOrderEx(string operationId, string symbol, OrderType orderType, OrderSide side, double volume, double? maxVisibleVolume,
            double? price, double? stopPrice, double? stopLoss, double? takeProfit, OrderTimeInForce? orderTimeInForce, DateTime? expiration,
            string comment, string tag, int? magic, bool immediateOrCancelFlag, double? slippage,
            bool oneCancelsTheOtherFlag, bool ocoEqualVolume, long? relatedOrderId,
            Common.ContingentOrderTriggerType? triggerType, DateTime? triggerTime, long? orderIdTriggeredBy)
        {
            ExecutionReport[] executionReports = dataTrade_.orderEntryClient_.NewOrder(
                operationId, symbol, orderType, side,
                volume, maxVisibleVolume, price, stopPrice,
                orderTimeInForce, expiration, stopLoss, takeProfit,
                comment, tag, magic, dataTrade_.synchOperationTimeout_,
                immediateOrCancelFlag, slippage,
                oneCancelsTheOtherFlag, ocoEqualVolume, relatedOrderId,
                triggerType, triggerTime, orderIdTriggeredBy);

            ExecutionReport lastExecutionReport = executionReports[executionReports.Length - 1];

            return dataTrade_.GetTradeRecord(lastExecutionReport);
        }

        #endregion

        #region Send OCO Orders

        public Tuple<TradeRecord, TradeRecord> SendOcoOrders(string symbol,
            Common.OrderType type1, Common.OrderSide side1, double qty1, double? maxVisibleQty1, double? price1, double? stopPrice1,
            Common.OrderTimeInForce? timeInForce1, DateTime? expireTime1, double? stopLoss1, double? takeProfit1,
            string comment1, string tag1, int? magic1, double? slippage1,
            Common.OrderType type2, Common.OrderSide side2, double qty2, double? maxVisibleQty2, double? price2, double? stopPrice2,
            Common.OrderTimeInForce? timeInForce2, DateTime? expireTime2, double? stopLoss2, double? takeProfit2,
            string comment2, string tag2, int? magic2, double? slippage2,
            Common.ContingentOrderTriggerType? triggerType, DateTime? triggerTime, long? orderIdTriggeredBy
        )
        {
            return SendOcoOrdersEx(Guid.NewGuid().ToString(), symbol, Guid.NewGuid().ToString(), type1, side1, qty1, maxVisibleQty1, price1, stopPrice1, timeInForce1, expireTime1, stopLoss1,
                takeProfit1, comment1, tag1, magic1, slippage1, Guid.NewGuid().ToString(), type2, side2, qty2, maxVisibleQty2, price2, stopPrice2, timeInForce2, expireTime2, stopLoss2, takeProfit2,
                comment2, tag2, magic2, slippage2, triggerType, triggerTime, orderIdTriggeredBy);
        }

        public Tuple<TradeRecord, TradeRecord> SendOcoOrders(string symbol,
            string clientOrderId1, Common.OrderType type1, Common.OrderSide side1, double qty1, double? maxVisibleQty1, double? price1, double? stopPrice1,
            Common.OrderTimeInForce? timeInForce1, DateTime? expireTime1, double? stopLoss1, double? takeProfit1,
            string comment1, string tag1, int? magic1, double? slippage1,
            string clientOrderId2, Common.OrderType type2, Common.OrderSide side2, double qty2, double? maxVisibleQty2, double? price2, double? stopPrice2,
            Common.OrderTimeInForce? timeInForce2, DateTime? expireTime2, double? stopLoss2, double? takeProfit2,
            string comment2, string tag2, int? magic2, double? slippage2,
            Common.ContingentOrderTriggerType? triggerType, DateTime? triggerTime, long? orderIdTriggeredBy
        )
        {
            return SendOcoOrdersEx(Guid.NewGuid().ToString(), symbol, clientOrderId1, type1, side1, qty1, maxVisibleQty1, price1, stopPrice1, timeInForce1, expireTime1, stopLoss1,
                takeProfit1, comment1, tag1, magic1, slippage1, clientOrderId2, type2, side2, qty2, maxVisibleQty2, price2, stopPrice2, timeInForce2, expireTime2, stopLoss2, takeProfit2,
                comment2, tag2, magic2, slippage2, triggerType, triggerTime, orderIdTriggeredBy);
        }

        public Tuple<TradeRecord, TradeRecord> SendOcoOrdersEx(string operationId, string symbol,
            string clientOrderId1, Common.OrderType type1, Common.OrderSide side1, double qty1, double? maxVisibleQty1, double? price1, double? stopPrice1,
            Common.OrderTimeInForce? timeInForce1, DateTime? expireTime1, double? stopLoss1, double? takeProfit1,
            string comment1, string tag1, int? magic1, double? slippage1,
            string clientOrderId2, Common.OrderType type2, Common.OrderSide side2, double qty2, double? maxVisibleQty2, double? price2, double? stopPrice2,
            Common.OrderTimeInForce? timeInForce2, DateTime? expireTime2, double? stopLoss2, double? takeProfit2,
            string comment2, string tag2, int? magic2, double? slippage2,
            Common.ContingentOrderTriggerType? triggerType, DateTime? triggerTime, long? orderIdTriggeredBy
        )
        {
            var reports = dataTrade_.orderEntryClient_.OpenOcoOrdersEx(operationId, symbol, clientOrderId1, type1, side1, qty1, maxVisibleQty1, price1, stopPrice1, timeInForce1, expireTime1, stopLoss1,
                takeProfit1, comment1, tag1, magic1, slippage1, clientOrderId2, type2, side2, qty2, maxVisibleQty2, price2, stopPrice2, timeInForce2, expireTime2, stopLoss2, takeProfit2,
                comment2, tag2, magic2, slippage2, triggerType, triggerTime, orderIdTriggeredBy, dataTrade_.synchOperationTimeout_);

            var calculatedReports = reports.Where(it => it.OrderStatus == OrderStatus.Calculated).ToList();
            if (calculatedReports.Count == 2)
            {
                return new Tuple<TradeRecord, TradeRecord>(dataTrade_.GetTradeRecord(calculatedReports[0]), dataTrade_.GetTradeRecord(calculatedReports[1]));
            }
            return null;
        }

        #endregion

        #region Modify Trade Record

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
        /// <param name="immediateOrCancelFlag">Flag that shows if the order is ImmediateOrCancel.</param>
        /// <param name="slippage">Slippage.</param>
        /// <returns>A modified trade record.</returns>
        [Obsolete("Modify with params newVolume and inFlightMitigation is deprecated.", true)]
        public TradeRecord ModifyTradeRecord(string orderId, string symbol, OrderType type, OrderSide side, double? newVolume, double? newMaxVisibleVolume, double? newPrice, double? newStopPrice, double? newStopLoss, double? newTakeProfit, OrderTimeInForce? newOrderTimeInForce, DateTime? newExpiration, bool? inFlightMitigation, double? currentQty, string newComment, string newTag, int? newMagic, bool? immediateOrCancelFlag, double? slippage)
        {
            return ModifyTradeRecordEx(orderId, symbol, type, side, newVolume, newMaxVisibleVolume, newPrice, newStopPrice, newStopLoss, newTakeProfit, newOrderTimeInForce, newExpiration, inFlightMitigation, currentQty, newComment, newTag, newMagic, dataTrade_.synchOperationTimeout_, immediateOrCancelFlag, slippage);
        }

        /// <summary>
        /// The method modifies an existing trade record.
        /// </summary>
        /// <param name="orderId">An existing pending order ID.</param>
        /// <param name="symbol">Currency pair.</param>
        /// <param name="type">Order type: Limit or Stop.</param>
        /// <param name="side">Order side: buy or sell.</param>
        /// <param name="volumeChange">A value by which the volume of pending order will be changed.</param>
        /// <param name="newMaxVisibleVolume">A new max visible volume of pending order.</param>
        /// <param name="newPrice">A new price of pending order.</param>
        /// <param name="newStopPrice">A new stop price of pending order.</param>
        /// <param name="newStopLoss">A new stop loss price of pending order.</param>
        /// <param name="newTakeProfit">A new take profit price of pending order.</param>
        /// <param name="newOrderTimeInForce"></param>
        /// <param name="newExpiration">A new expiration time.</param>
        /// <param name="newComment">A new comment.</param>
        /// <param name="newTag">A new tag.</param>
        /// <param name="newMagic">A new magic.</param>
        /// <param name="immediateOrCancelFlag">Flag that shows if the order is ImmediateOrCancel.</param>
        /// <param name="slippage">Slippage.</param>
        /// <param name="oneCancelsTheOtherFlag"></param>
        /// <param name="ocoEqualVolume"></param>
        /// <param name="relatedOrderId"></param>
        /// <param name="triggerType"></param>
        /// <param name="triggerTime"></param>
        /// <param name="orderIdTriggeredBy"></param>
        /// <returns>A modified trade record.</returns>
        public TradeRecord ModifyTradeRecord(string orderId, string symbol, OrderType type, OrderSide side,
            double? volumeChange, double? newMaxVisibleVolume, double? newPrice, double? newStopPrice,
            double? newStopLoss, double? newTakeProfit, OrderTimeInForce? newOrderTimeInForce, DateTime? newExpiration,
            string newComment, string newTag, int? newMagic, bool? immediateOrCancelFlag, double? slippage,
            bool? oneCancelsTheOtherFlag, bool? ocoEqualVolume, long? relatedOrderId,
            Common.ContingentOrderTriggerType? triggerType, DateTime? triggerTime, long? orderIdTriggeredBy)
        {
            return ModifyTradeRecordEx(Guid.NewGuid().ToString(), orderId, null, symbol, type, side, volumeChange,
                newMaxVisibleVolume, newPrice, newStopPrice, newStopLoss, newTakeProfit, newOrderTimeInForce,
                newExpiration, newComment, newTag, newMagic, dataTrade_.synchOperationTimeout_,
                immediateOrCancelFlag, slippage, oneCancelsTheOtherFlag, ocoEqualVolume, relatedOrderId,
                triggerType, triggerTime, orderIdTriggeredBy);
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
        /// <param name="immediateOrCancelFlag">Flag that shows if the order is ImmediateOrCancel.</param>
        /// <param name="slippage">Slippage.</param>
        /// <returns>A modified trade record.</returns>
        [Obsolete("Modify with params newVolume and inFlightMitigation is deprecated.", true)]
        public TradeRecord ModifyTradeRecordEx(string orderId, string symbol, OrderType type, OrderSide side, double? newVolume, double? newMaxVisibleVolume, double? newPrice, double? newStopPrice, double? newStopLoss, double? newTakeProfit, OrderTimeInForce? newOrderTimeInForce, DateTime? newExpiration, bool? inFlightMitigation, double? currentQty, string newComment, string newTag, int? newMagic, int timeoutInMilliseconds, bool? immediateOrCancelFlag, double? slippage)
        {
            return ModifyTradeRecordEx(Guid.NewGuid().ToString(), orderId, null, symbol, type, side, newVolume, newMaxVisibleVolume, newPrice, newStopPrice, newStopLoss, newTakeProfit, newOrderTimeInForce, newExpiration, inFlightMitigation, currentQty, newComment, newTag, newMagic, timeoutInMilliseconds, immediateOrCancelFlag, slippage);
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
        /// <param name="immediateOrCancelFlag">Flag that shows if the order is ImmediateOrCancel.</param>
        /// <param name="slippage">Slippage.</param>
        /// <returns>A modified trade record.</returns>
        [Obsolete("Modify with params newVolume and inFlightMitigation is deprecated.", true)]
        public TradeRecord ModifyTradeRecordEx(string operationId, string orderId, string symbol, OrderType type, OrderSide side, double? newVolume, double? newMaxVisibleVolume, double? newPrice, double? newStopPrice, double? newStopLoss, double? newTakeProfit, OrderTimeInForce? newOrderTimeInForce, DateTime? newExpiration, bool? inFlightMitigation, double? currentQty, string newComment, string newTag, int? newMagic, bool? immediateOrCancelFlag, double? slippage)
        {
            return ModifyTradeRecordEx(operationId, orderId, null, symbol, type, side, newVolume, newMaxVisibleVolume, newPrice, newStopPrice, newStopLoss, newTakeProfit, newOrderTimeInForce, newExpiration, inFlightMitigation, currentQty, newComment, newTag, newMagic, dataTrade_.synchOperationTimeout_, immediateOrCancelFlag, slippage);
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
        /// <param name="immediateOrCancelFlag">Flag that shows if the order is ImmediateOrCancel.</param>
        /// <param name="slippage">Slippage.</param>
        /// <returns>A modified trade record.</returns>
        [Obsolete("Modify with params newVolume and inFlightMitigation is deprecated.", true)]
        public TradeRecord ModifyTradeRecordEx(string operationId, string orderId, string clientId, string symbol, OrderType type, OrderSide side, double? newVolume, double? newMaxVisibleVolume, double? newPrice, double? newStopPrice, double? newStopLoss, double? newTakeProfit, OrderTimeInForce? newOrderTimeInForce, DateTime? newExpiration, bool? inFlightMitigation, double? currentQty, string newComment, string newTag, int? newMagic, int timeoutInMilliseconds, bool? immediateOrCancelFlag, double? slippage)
        {
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
                newOrderTimeInForce,
                newExpiration,
                newStopLoss,
                newTakeProfit,
                inFlightMitigation,
                currentQty,
                newComment,
                newTag,
                newMagic,
                timeoutInMilliseconds,
                immediateOrCancelFlag,
                slippage
            );

            ExecutionReport lastExecutionReport = executionReports[executionReports.Length - 1];

            return dataTrade_.GetTradeRecord(lastExecutionReport);
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
        /// <param name="volumeChange">A value by which the volume of pending order will be changed.</param>
        /// <param name="newMaxVisibleVolume">A new max visible volume of pending order.</param>
        /// <param name="newPrice">A new price of pending order.</param>
        /// <param name="newStopPrice">A new stop price of pending order.</param>
        /// <param name="newStopLoss">A new stop loss price of pending order.</param>
        /// <param name="newTakeProfit">A new take profit price of pending order.</param>
        /// <param name="newOrderTimeInForce"></param>
        /// <param name="newExpiration">A new expiration time.</param>
        /// <param name="newComment">A new comment.</param>
        /// <param name="newTag">A new tag.</param>
        /// <param name="newMagic">A new magic.</param>
        /// <param name="timeoutInMilliseconds">Timeout of the synchronous operation.</param>
        /// <param name="immediateOrCancelFlag">Flag that shows if the order is ImmediateOrCancel.</param>
        /// <param name="slippage">Slippage.</param>
        /// <param name="oneCancelsTheOtherFlag"></param>
        /// <param name="ocoEqualVolume"></param>
        /// <param name="relatedOrderId"></param>
        /// <param name="triggerType"></param>
        /// <param name="triggerTime"></param>
        /// <param name="orderIdTriggeredBy"></param>
        /// <returns>A modified trade record.</returns>
        public TradeRecord ModifyTradeRecordEx(string operationId, string orderId, string clientId,
            string symbol, OrderType type, OrderSide side, double? volumeChange, double? newMaxVisibleVolume,
            double? newPrice, double? newStopPrice, double? newStopLoss, double? newTakeProfit,
            OrderTimeInForce? newOrderTimeInForce, DateTime? newExpiration, string newComment, string newTag,
            int? newMagic, int timeoutInMilliseconds, bool? immediateOrCancelFlag, double? slippage,
            bool? oneCancelsTheOtherFlag, bool? ocoEqualVolume, long? relatedOrderId,
            Common.ContingentOrderTriggerType? triggerType, DateTime? triggerTime, long? orderIdTriggeredBy)
        {
            ExecutionReport[] executionReports = dataTrade_.orderEntryClient_.ReplaceOrder
            (
                operationId,
                clientId,
                orderId,
                symbol,
                type,
                side,
                volumeChange,
                newMaxVisibleVolume,
                newPrice,
                newStopPrice,
                newOrderTimeInForce,
                newExpiration,
                newStopLoss,
                newTakeProfit,
                newComment,
                newTag,
                newMagic,
                timeoutInMilliseconds,
                immediateOrCancelFlag,
                slippage,
                oneCancelsTheOtherFlag,
                ocoEqualVolume,
                relatedOrderId,
                triggerType,
                triggerTime,
                orderIdTriggeredBy
            );

            ExecutionReport lastExecutionReport = executionReports[executionReports.Length - 1];

            return dataTrade_.GetTradeRecord(lastExecutionReport);
        }

        #endregion

        #region Delete Pending (Contingent) Order

        /// <summary>
        /// The method deletes an existing pending order.
        /// </summary>
        /// <param name="orderId">An existing pending order ID.</param>
        /// <param name="side">Order side: buy or sell.</param>
        public void DeletePendingOrder(string orderId)
        {
            DeletePendingOrderEx(Guid.NewGuid().ToString(), orderId, dataTrade_.synchOperationTimeout_);
        }

        /// <summary>
        /// The method deletes an existing pending order.
        /// </summary>
        /// <param name="orderId">An existing pending order ID.</param>
        /// <param name="side">Order side: buy or sell.</param>
        /// <param name="timeoutInMilliseconds">Timeout of the synchronous operation.</param>
        [Obsolete]
        public void DeletePendingOrderEx(string orderId, int timeoutInMilliseconds)
        {
            DeletePendingOrderEx(Guid.NewGuid().ToString(), orderId, timeoutInMilliseconds);
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
            dataTrade_.orderEntryClient_.CancelOrder(operationId, null, orderId, timeoutInMilliseconds);
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
        [Obsolete]
        public void DeletePendingOrderEx(string operationId, string orderId)
        {
            DeletePendingOrderEx(operationId, orderId, dataTrade_.synchOperationTimeout_);
        }

        #endregion

        #region Close Position

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
        [Obsolete]
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
            ExecutionReport[] executionReports = dataTrade_.orderEntryClient_.ClosePosition(operationId, orderId, null, null, timeoutInMilliseconds);

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
        [Obsolete]
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
            ExecutionReport[] executionReports = dataTrade_.orderEntryClient_.ClosePosition(operationId, orderId, volume, null,timeoutInMilliseconds);

            ExecutionReport tradeExecutionReport = executionReports.LastOrDefault();

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
            return CloseByPositionsEx(Guid.NewGuid().ToString(), firstOrderId, secondOrderId, dataTrade_.synchOperationTimeout_);
        }

        /// <summary>
        /// The method closes by two orders.
        /// The method is supported by Gross account only.
        /// </summary>
        /// <param name="firstOrderId">The first order ID; can not be null.</param>
        /// <param name="secondOrderId">The second order ID; can not be null.</param>
        /// <param name="timeoutInMilliseconds">Timeout of the operation in milliseconds.</param>
        /// <returns>True, if the operation has been succeeded; otherwise false.</returns>
        [Obsolete]
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

        #endregion

        #region Trade Transaction Reports, Contingent Order Trigger Reports and Account Reports

        /// <summary>
        /// The method starts trade transaction reports receiving.
        /// </summary>  
        /// <param name="from"></param>
        /// <param name="skipCancel"></param>
        public SubscribeTradeTransactionReportsEnumerator SubscribeTradeTransactionReports(DateTime? from, bool skipCancel)
        {
            return SubscribeTradeTransactionReportsEx(from, skipCancel, dataTrade_.synchOperationTimeout_);
        }

        /// <summary>
        /// The method starts trade transaction reports receiving.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="skipCancel"></param>
        /// <param name="timeoutInMilliseconds"></param>
        public SubscribeTradeTransactionReportsEnumerator SubscribeTradeTransactionReportsEx(DateTime? from, bool skipCancel, int timeoutInMilliseconds)
        {
            SubscribeTradesEnumerator tradeTransactionReportEnumerator = dataTrade_.tradeCaptureClient_.SubscribeTrades(from, skipCancel, timeoutInMilliseconds);

            return new SubscribeTradeTransactionReportsEnumerator(dataTrade_, from, skipCancel, timeoutInMilliseconds, tradeTransactionReportEnumerator);
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
        /// The method starts contingent order trigger reports receiving.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="skipFailed"></param>
        public SubscribeContingentOrderTriggerReportsEnumerator SubscribeContingentOrderTriggerReports(DateTime? from, bool skipFailed)
        {
            return SubscribeContingentOrderTriggerReportsEx(from, skipFailed, dataTrade_.synchOperationTimeout_);
        }

        /// <summary>
        /// The method starts contingent order trigger reports receiving.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="skipFailed"></param>
        /// <param name="timeoutInMilliseconds"></param>
        public SubscribeContingentOrderTriggerReportsEnumerator SubscribeContingentOrderTriggerReportsEx(DateTime? from, bool skipFailed, int timeoutInMilliseconds)
        {
            SubscribeTriggerReportsEnumerator triggerReportsEnumerator = dataTrade_.tradeCaptureClient_.SubscribeTriggerReports(from, skipFailed, timeoutInMilliseconds);

            return new SubscribeContingentOrderTriggerReportsEnumerator(dataTrade_, from, skipFailed, timeoutInMilliseconds, triggerReportsEnumerator);
        }

        /// <summary>
        /// The method stops contingent order trigger reports receiving.
        /// </summary>
        public void UnsubscribeontingentOrderTriggerReports()
        {
            UnsubscribeTradeTransactionReportsEx(dataTrade_.synchOperationTimeout_);
        }

        /// <summary>
        /// The method stops contingent order trigger reports receiving.
        /// </summary>
        /// <param name="timeoutInMilliseconds">Timeout of the operation in milliseconds</param>
        public void UnsubscribeontingentOrderTriggerReportsEx(int timeoutInMilliseconds)
        {
            dataTrade_.tradeCaptureClient_.UnsubscribeTriggerReports(timeoutInMilliseconds);
        }

        /// <summary>
        /// The method gets contingent order trigger reports from the server.
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="skipFailed"></param>
        /// <returns></returns>
        public ContingentOrderTriggerReports GetContingentOrderTriggerReportsHistory(TimeDirection direction, DateTime? startTime, DateTime? endTime, bool skipFailed)
        {
            return GetContingentOrderTriggerReportsEx(direction, startTime, endTime, skipFailed, dataTrade_.synchOperationTimeout_);
        }

        /// <summary>
        /// The method gets contingent order trigger reports from the server.
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="subscribeToNotifications"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="skipCancel"></param>
        /// <param name="timeoutInMilliseconds"></param>
        /// <returns></returns>
        public ContingentOrderTriggerReports GetContingentOrderTriggerReportsEx(TimeDirection direction, DateTime? startTime, DateTime? endTime, bool skipFailed, int timeoutInMilliseconds)
        {
            return new ContingentOrderTriggerReports(dataTrade_, direction, startTime, endTime, skipFailed, timeoutInMilliseconds);
        }

        /// <summary>
        /// The method gets account history from the server.
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public AccountReports GetAccountHistory(TimeDirection direction, DateTime? from, DateTime? to)
        {
            return GetAccountHistoryEx(direction, from, to, dataTrade_.synchOperationTimeout_);
        }

        /// <summary>
        /// The method gets account history from the server.
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="timeoutInMilliseconds"></param>
        /// <returns></returns>
        public AccountReports GetAccountHistoryEx(TimeDirection direction, DateTime? from, DateTime? to, int timeoutInMilliseconds)
        {
            return new AccountReports(dataTrade_, direction, from, to, timeoutInMilliseconds);
        }

        #endregion

        #region Splits Dividends MergerAndAcquisitions

        public Split[] GetSplitList()
        {
            return GetSplitListEx(dataTrade_.synchOperationTimeout_);
        }

        public Split[] GetSplitListEx(int timeoutInMilliseconds)
        {
            return dataTrade_.orderEntryClient_.GetSplitList(timeoutInMilliseconds);
        }

        public Dividend[] GetDividendList()
        {
            return GetDividendListEx(dataTrade_.synchOperationTimeout_);
        }

        public Dividend[] GetDividendListEx(int timeoutInMilliseconds)
        {
            return dataTrade_.orderEntryClient_.GetDividendList(timeoutInMilliseconds);
        }

        public MergerAndAcquisition[] GetMergerAndAcquisitionList()
        {
            return GetMergerAndAcquisitionListEx(dataTrade_.synchOperationTimeout_);
        }

        public MergerAndAcquisition[] GetMergerAndAcquisitionListEx(int timeoutInMilliseconds)
        {
            return dataTrade_.orderEntryClient_.GetMergerAndAcquisitionList(timeoutInMilliseconds);
        }

        #endregion

        DataTrade dataTrade_;
    }
}
