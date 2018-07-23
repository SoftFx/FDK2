using System;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using SoftFX.Net.OrderEntry;
using TickTrader.FDK.Common;

namespace TickTrader.FDK.Client
{
    public class OrderEntry : IDisposable
    {
        #region Constructors

        public OrderEntry
        (
            string name,
            bool logEvents =  false,
            bool logStates =  false,
            bool logMessages = false,
            int port = 5040, 
            string serverCertificateName = "*.soft-fx.com",
            int connectAttempts = -1,
            int reconnectAttempts = -1,
            int connectInterval = 10000,
            int heartbeatInterval = 10000,
            string logDirectory = "Logs"            
        )
        {
            ClientSessionOptions options = new ClientSessionOptions(port);
            options.ConnectionType = SoftFX.Net.Core.ConnectionType.SecureSocket;
            options.ServerCertificateName = serverCertificateName;
            options.ServerMinMinorVersion = Info.OrderEntry.MinorVersion;
            options.ConnectMaxCount = connectAttempts;
            options.ReconnectMaxCount = reconnectAttempts;
            options.ConnectInterval = connectInterval;
            options.HeartbeatInterval = heartbeatInterval;
            options.Log.Directory = logDirectory;
            options.Log.Events = logEvents;
            options.Log.States = logStates;
            options.Log.Messages = logMessages;

            session_ = new ClientSession(name, options);
            sessionListener_ = new ClientSessionListener(this);
            session_.Listener = sessionListener_;
        }

        ClientSession session_;
        ClientSessionListener sessionListener_;

        #endregion

        #region IDisposable        

        public void Dispose()
        {
            session_.Dispose();

            GC.SuppressFinalize(this);
        }

        #endregion

        #region Network activity

        public NetworkActivity NetworkActivity
        {
            get
            {
                SoftFX.Net.Core.ClientSessionStatistics statistics = session_.Statistics;

                return new NetworkActivity(statistics.SendDataSize, statistics.ReceiveDataSize);
            }
        }

        #endregion

        #region Connect / disconnect

        public delegate void ConnectResultDelegate(OrderEntry orderEntry, object data);
        public delegate void ConnectErrorDelegate(OrderEntry orderEntry, object data, Exception exception);
        public delegate void DisconnectResultDelegate(OrderEntry orderEntry, object data, string text);
        public delegate void DisconnectDelegate(OrderEntry orderEntry, string text);
        public delegate void ReconnectDelegate(OrderEntry orderEntry);
        public delegate void ReconnectErrorDelegate(OrderEntry orderEntry, Exception exception);
        public delegate void SendDelegate(OrderEntry orderEntry);

        public event ConnectResultDelegate ConnectResultEvent;
        public event ConnectErrorDelegate ConnectErrorEvent;
        public event DisconnectResultDelegate DisconnectResultEvent;
        public event DisconnectDelegate DisconnectEvent;
        public event ReconnectDelegate ReconnectEvent;
        public event ReconnectErrorDelegate ReconnectErrorEvent;
        public event SendDelegate SendEvent;

        public void Connect(string address, int timeout)
        {
            ConnectAsyncContext context = new ConnectAsyncContext(true);

            ConnectInternal(context, address);

            if (! context.Wait(timeout))
            {
                DisconnectInternal(null, "Connect timeout");
                Join();

                throw new Common.TimeoutException("Method call timed out");
            }

            if (context.exception_ != null)
                throw context.exception_;
        }

        public void ConnectAsync(object data, string address)
        {
            ConnectAsyncContext context = new ConnectAsyncContext(false);
            context.Data = data;

            ConnectInternal(context, address);
        }

        void ConnectInternal(ConnectAsyncContext context, string address)
        {
            session_.Connect(context, address);
        }

        public string Disconnect(string text)
        {
            string result;

            DisconnectAsyncContext context = new DisconnectAsyncContext(true);

            if (DisconnectInternal(context, text))
            {
                context.Wait(-1);

                result = context.text_;
            }
            else
                result = null;

            return result;
        }

        public bool DisconnectAsync(object data, string text)
        {
            DisconnectAsyncContext context = new DisconnectAsyncContext(false);
            context.Data = data;

            return DisconnectInternal(context, text);
        }

        bool DisconnectInternal(DisconnectAsyncContext context, string text)
        {
            return session_.Disconnect(context, text);
        }

        public void Join()
        {
            session_.Join();
        }

        #endregion

        #region Login / logout

        public delegate void LoginResultDelegate(OrderEntry orderEntry, object data);
        public delegate void LoginErrorDelegate(OrderEntry orderEntry, object data, Exception exception);
        public delegate void TwoFactorLoginRequestDelegate(OrderEntry orderEntry, string message);
        public delegate void TwoFactorLoginResultDelegate(OrderEntry orderEntry, object data, DateTime expireTime);
        public delegate void TwoFactorLoginErrorDelegate(OrderEntry orderEntry, object data, Exception exception);
        public delegate void TwoFactorLoginResumeDelegate(OrderEntry orderEntry, object data, DateTime expireTime);
        public delegate void LogoutResultDelegate(OrderEntry orderEntry, object data, LogoutInfo logoutInfo);
        public delegate void LogoutErrorDelegate(OrderEntry orderEntry, object data, Exception exception);
        public delegate void LogoutDelegate(OrderEntry orderEntry, LogoutInfo logoutInfo);

        public event LoginResultDelegate LoginResultEvent;
        public event LoginErrorDelegate LoginErrorEvent;
        public event TwoFactorLoginRequestDelegate TwoFactorLoginRequestEvent;
        public event TwoFactorLoginResultDelegate TwoFactorLoginResultEvent;
        public event TwoFactorLoginErrorDelegate TwoFactorLoginErrorEvent;
        public event TwoFactorLoginResumeDelegate TwoFactorLoginResumeEvent;
        public event LogoutResultDelegate LogoutResultEvent;
        public event LogoutErrorDelegate LogoutErrorEvent;
        public event LogoutDelegate LogoutEvent;

        public void Login(string username, string password, string deviceId, string appId, string sessionId, int timeout)
        {
            LoginAsyncContext context = new LoginAsyncContext(true);

            LoginInternal(context, username, password, deviceId, appId, sessionId);

            if (! context.Wait(timeout))
                throw new Common.TimeoutException("Method call timed out");

            if (context.exception_ != null)
                throw context.exception_;
        }

        public void LoginAsync(object data, string username, string password, string deviceId, string appId, string sessionId)
        {
            LoginAsyncContext context = new LoginAsyncContext(false);
            context.Data = data;

            LoginInternal(context, username, password, deviceId, appId, sessionId);
        }

        void LoginInternal(LoginAsyncContext context, string username, string password, string deviceId, string appId, string sessionId)
        {
            if (string.IsNullOrEmpty(appId))
                appId = "FDK2";

            LoginRequest request = new LoginRequest(0);
            request.Username = username;
            request.Password = password;
            request.DeviceId = deviceId;
            request.AppId = appId;
            request.SessionId = sessionId;

            session_.SendLoginRequest(context, request);
        }

        public DateTime TwoFactorLoginResponse(string oneTimePassword, int timeout)
        {
            TwoFactorLoginResponseAsyncContext context = new TwoFactorLoginResponseAsyncContext(true);

            TwoFactorLoginResponseInternal(context, oneTimePassword);

            if (! context.Wait(timeout))
                throw new Common.TimeoutException("Method call timed out");

            if (context.exception_ != null)
                throw context.exception_;

            return context.dateTime_;
        }

        public void TwoFactorLoginResponseAsync(object data, string oneTimePassword)
        {
            TwoFactorLoginResponseAsyncContext context = new TwoFactorLoginResponseAsyncContext(false);
            context.Data = data;

            TwoFactorLoginResponseInternal(context, oneTimePassword);
        }

        void TwoFactorLoginResponseInternal(TwoFactorLoginResponseAsyncContext context, string oneTimePassword)
        {
            TwoFactorLogin message = new TwoFactorLogin(0);
            message.Reason = TwoFactorReason.ClientResponse;
            message.OneTimePassword = oneTimePassword;

            session_.SendTwoFactorLoginResponse(context, message);
        }

        public DateTime TwoFactorLoginResume(int timeout)
        {
            TwoFactorLoginResumeAsyncContext context = new TwoFactorLoginResumeAsyncContext(true);

            TwoFactorLoginResumeInternal(context);

            if (! context.Wait(timeout))
                throw new Common.TimeoutException("Method call timed out");

            if (context.exception_ != null)
                throw context.exception_;

            return context.dateTime_;
        }

        public void TwoFactorLoginResumeAsync(object data)
        {
            TwoFactorLoginResumeAsyncContext context = new TwoFactorLoginResumeAsyncContext(false);
            context.Data = data;

            TwoFactorLoginResumeInternal(context);
        }

        void TwoFactorLoginResumeInternal(TwoFactorLoginResumeAsyncContext context)
        {
            TwoFactorLogin message = new TwoFactorLogin(0);
            message.Reason = TwoFactorReason.ClientResume;

            session_.SendTwoFactorLoginResume(context, message);
        }

        public LogoutInfo Logout(string message, int timeout)
        {
            LogoutAsyncContext context = new LogoutAsyncContext(true);

            LogoutInternal(context, message);

            if (!context.Wait(timeout))
                throw new Common.TimeoutException("Method call timed out");

            if (context.exception_ != null)
                throw context.exception_;

            return context.logoutInfo_;
        }

        public void LogoutAsync(object data, string message)
        {
            LogoutAsyncContext context = new LogoutAsyncContext(false);
            context.Data = data;

            LogoutInternal(context, message);
        }

        void LogoutInternal(LogoutAsyncContext context, string message)
        {
            session_.Reconnect = false;

            Logout request = new Logout(0);
            request.Text = message;

            session_.SendLogout(context, request);
        }

        #endregion

        #region Order Entry
                
        public delegate void TradeServerInfoResultDelegate(OrderEntry orderEntry, object data, TickTrader.FDK.Common.TradeServerInfo tradeServerInfo);
        public delegate void TradeServerInfoErrorDelegate(OrderEntry orderEntry, object data, Exception exception);
        public delegate void AccountInfoResultDelegate(OrderEntry orderEntry, object data, TickTrader.FDK.Common.AccountInfo accountInfo);
        public delegate void AccountInfoErrorDelegate(OrderEntry orderEntry, object data, Exception exception);
        public delegate void SessionInfoResultDelegate(OrderEntry orderEntry, object data, TickTrader.FDK.Common.SessionInfo sessionInfo);
        public delegate void SessionInfoErrorDelegate(OrderEntry orderEntry, object data, Exception exception);
        public delegate void OrdersBeginResultDelegate(OrderEntry orderEntry, object data, string id, int orderCount);
        public delegate void OrdersResultDelegate(OrderEntry orderEntry, object data, TickTrader.FDK.Common.ExecutionReport executionReport);
        public delegate void OrdersEndResultDelegate(OrderEntry orderEntry, object data);
        public delegate void OrdersErrorDelegate(OrderEntry orderEntry, object data, Exception exception);
        public delegate void CancelOrdersResultDelegate(OrderEntry orderEntry, object data);
        public delegate void CancelOrdersErrorDelegate(OrderEntry orderEntry, object data, Exception exception);
        public delegate void PositionsResultDelegate(OrderEntry orderEntry, object data, TickTrader.FDK.Common.Position[] positions);
        public delegate void PositionsErrorDelegate(OrderEntry orderEntry, object data, Exception exception);
        public delegate void NewOrderResultDelegate(OrderEntry orderEntry, object data, TickTrader.FDK.Common.ExecutionReport executionReport);
        public delegate void NewOrderErrorDelegate(OrderEntry orderEntry, object data, Exception exception);
        public delegate void ReplaceOrderResultDelegate(OrderEntry orderEntry, object data, TickTrader.FDK.Common.ExecutionReport executionReport);
        public delegate void ReplaceOrderErrorDelegate(OrderEntry orderEntry, object data, Exception exception);
        public delegate void CancelOrderResultDelegate(OrderEntry orderEntry, object data, TickTrader.FDK.Common.ExecutionReport executionReport);
        public delegate void CancelOrderErrorDelegate(OrderEntry orderEntry, object data, Exception exception);
        public delegate void ClosePositionResultDelegate(OrderEntry orderEntry, object data, TickTrader.FDK.Common.ExecutionReport executionReport);
        public delegate void ClosePositionErrorDelegate(OrderEntry orderEntry, object data, Exception exception);
        public delegate void ClosePositionByResultDelegate(OrderEntry orderEntry, object data, TickTrader.FDK.Common.ExecutionReport executionReport);
        public delegate void ClosePositionByErrorDelegate(OrderEntry orderEntry, object data, Exception exception);
        public delegate void OrderUpdateDelegate(OrderEntry orderEntry, TickTrader.FDK.Common.ExecutionReport executionReport);
        public delegate void PositionUpdateDelegate(OrderEntry orderEntry, TickTrader.FDK.Common.Position position);
        public delegate void AccountInfoUpdateDelegate(OrderEntry orderEntry, TickTrader.FDK.Common.AccountInfo accountInfo);
        public delegate void SessionInfoUpdateDelegate(OrderEntry orderEntry, SessionInfo sessionInfo);
        public delegate void BalanceUpdateDelegate(OrderEntry orderEntry, BalanceOperation balanceOperation);
        public delegate void NotificationDelegate(OrderEntry orderEntry, TickTrader.FDK.Common.Notification notification);
                
        public event TradeServerInfoResultDelegate TradeServerInfoResultEvent;
        public event TradeServerInfoErrorDelegate TradeServerInfoErrorEvent;
        public event AccountInfoResultDelegate AccountInfoResultEvent;
        public event AccountInfoErrorDelegate AccountInfoErrorEvent;
        public event SessionInfoResultDelegate SessionInfoResultEvent;
        public event SessionInfoErrorDelegate SessionInfoErrorEvent;
        public event OrdersBeginResultDelegate OrdersBeginResultEvent;
        public event OrdersResultDelegate OrdersResultEvent;
        public event OrdersEndResultDelegate OrdersEndResultEvent;
        public event OrdersErrorDelegate OrdersErrorEvent;
        public event CancelOrdersResultDelegate CancelOrdersResultEvent;
        public event CancelOrdersErrorDelegate CancelOrdersErrorEvent;
        public event PositionsResultDelegate PositionsResultEvent;
        public event PositionsErrorDelegate PositionsErrorEvent;
        public event NewOrderResultDelegate NewOrderResultEvent;
        public event NewOrderErrorDelegate NewOrderErrorEvent;
        public event ReplaceOrderResultDelegate ReplaceOrderResultEvent;
        public event ReplaceOrderErrorDelegate ReplaceOrderErrorEvent;
        public event CancelOrderResultDelegate CancelOrderResultEvent;
        public event CancelOrderErrorDelegate CancelOrderErrorEvent;
        public event ClosePositionResultDelegate ClosePositionResultEvent;
        public event ClosePositionErrorDelegate ClosePositionErrorEvent;
        public event ClosePositionByResultDelegate ClosePositionByResultEvent;
        public event ClosePositionByErrorDelegate ClosePositionByErrorEvent;
        public event OrderUpdateDelegate OrderUpdateEvent;
        public event PositionUpdateDelegate PositionUpdateEvent;
        public event AccountInfoUpdateDelegate AccountInfoUpdateEvent;
        public event SessionInfoUpdateDelegate SessionInfoUpdateEvent;
        public event BalanceUpdateDelegate BalanceUpdateEvent;
        public event NotificationDelegate NotificationEvent;

        public TickTrader.FDK.Common.TradeServerInfo GetTradeServerInfo(int timeout)
        {
            TradeServerInfoAsyncContext context = new TradeServerInfoAsyncContext(true);

            GetTradeServerInfoInternal(context);

            if (!context.Wait(timeout))
                throw new Common.TimeoutException("Method call timed out");

            if (context.exception_ != null)
                throw context.exception_;

            return context.tradeServerInfo_;
        }

        public void GetTradeServerInfoAsync(object data)
        {
            TradeServerInfoAsyncContext context = new TradeServerInfoAsyncContext(false);
            context.Data = data;

            GetTradeServerInfoInternal(context);
        }

        void GetTradeServerInfoInternal(TradeServerInfoAsyncContext context)
        {
            TradeServerInfoRequest request = new TradeServerInfoRequest(0);
            request.Id = Guid.NewGuid().ToString();

            session_.SendTradeServerInfoRequest(context, request);
        }

        public TickTrader.FDK.Common.AccountInfo GetAccountInfo(int timeout)
        {
            AccountInfoAsyncContext context = new AccountInfoAsyncContext(true);

            GetAccountInfoInternal(context);

            if (!context.Wait(timeout))
                throw new Common.TimeoutException("Method call timed out");

            if (context.exception_ != null)
                throw context.exception_;

            return context.accountInfo_;
        }

        public void GetAccountInfoAsync(object data)
        {
            AccountInfoAsyncContext context = new AccountInfoAsyncContext(false);
            context.Data = data;

            GetAccountInfoInternal(context);
        }

        void GetAccountInfoInternal(AccountInfoAsyncContext context)
        {
            AccountInfoRequest request = new AccountInfoRequest(0);
            request.Id = Guid.NewGuid().ToString();

            // Send request to the server
            session_.SendAccountInfoRequest(context, request);
        }

        public TickTrader.FDK.Common.SessionInfo GetSessionInfo(int timeout)
        {
            SessionInfoAsyncContext context = new SessionInfoAsyncContext(true);

            GetSessionInfoInternal(context);

            if (!context.Wait(timeout))
                throw new Common.TimeoutException("Method call timed out");

            if (context.exception_ != null)
                throw context.exception_;

            return context.sessionInfo_;
        }

        public void GetSessionInfoAsync(object data)
        {
            SessionInfoAsyncContext context = new SessionInfoAsyncContext(false);
            context.Data = data;

            GetSessionInfoInternal(context);
        }

        void GetSessionInfoInternal(SessionInfoAsyncContext context)
        {
            TradingSessionStatusRequest request = new TradingSessionStatusRequest(0);
            request.Id = Guid.NewGuid().ToString();

            session_.SendTradingSessionStatusRequest(context, request);
        }

        public GetOrdersEnumerator GetOrders(int timeout)
        {
            OrdersAsyncContext context = new OrdersAsyncContext(true);
            context.enumerator_ = new GetOrdersEnumerator(this);

            GetOrdersInternal(context);

            context.enumerator_.Begin(timeout);

            return context.enumerator_;
        }

        public void GetOrdersAsync(object data)
        {
            OrdersAsyncContext context = new OrdersAsyncContext(false);
            context.Data = data;

            GetOrdersInternal(context);
        }

        void GetOrdersInternal(OrdersAsyncContext context)
        {
            OrderMassStatusRequest request = new OrderMassStatusRequest(0);
            request.Id = Guid.NewGuid().ToString();
            request.Type = OrderMassStatusRequestType.All;

            session_.SendOrderMassStatusRequest(context, request);
        }

        public void CancelOrders(string id, int timeout)
        {
            CancelOrdersAsyncContext context = new CancelOrdersAsyncContext(true);

            CancelOrdersInternal(context, id);

            if (! context.Wait(timeout))
                throw new Common.TimeoutException("Method call timed out");

            if (context.exception_ != null)
                throw context.exception_;
        }

        public void CancelOrdersAsync(object data, string id)
        {
            CancelOrdersAsyncContext context = new CancelOrdersAsyncContext(false);
            context.Data = data;

            CancelOrdersInternal(context, id);
        }

        void CancelOrdersInternal(CancelOrdersAsyncContext context, string id)
        {
            OrderMassStatusCancelRequest request = new OrderMassStatusCancelRequest(0);
            request.Id = Guid.NewGuid().ToString();
            request.RequestId = id;

            session_.SendOrderMassStatusCancelRequest(context, request);
        }

        public TickTrader.FDK.Common.Position[] GetPositions(int timeout)
        {
            PositionsAsyncContext context = new PositionsAsyncContext(true);

            GetPositionsInternal(context);

            if (! context.Wait(timeout))
                throw new Common.TimeoutException("Method call timed out");

            if (context.exception_ != null)
                throw context.exception_;

            return context.positions_;
        }

        public void GetPositionsAsync(object data)
        {
            PositionsAsyncContext context = new PositionsAsyncContext(false);
            context.Data = data;

            GetPositionsInternal(context);
        }

        void GetPositionsInternal(PositionsAsyncContext context)
        {
            PositionListRequest request = new PositionListRequest(0);
            request.Id = Guid.NewGuid().ToString();
            request.Type = PositionListRequestType.All;

            session_.SendPositionListRequest(context, request);
        }

        public TickTrader.FDK.Common.ExecutionReport[] NewOrder
        (
            string clientOrderId,
            string symbol,
            TickTrader.FDK.Common.OrderType type,
            TickTrader.FDK.Common.OrderSide side,
            double qty,
            double? maxVisibleQty,
            double? price,
            double? stopPrice,
            TickTrader.FDK.Common.OrderTimeInForce? timeInForce,
            DateTime? expireTime,
            double? stopLoss,
            double? takeProfit,
            string comment,
            string tag,
            int? magic,
            int timeout
        )
        {
            NewOrderAsyncContext context = new NewOrderAsyncContext(true);

            context.executionReportList_ = new List<Common.ExecutionReport>();

            NewOrderInternal
            (
                context,
                clientOrderId,
                symbol,
                type,
                side,
                qty,
                maxVisibleQty,
                price,
                stopPrice,
                timeInForce,
                expireTime,
                stopLoss,
                takeProfit,
                comment,
                tag,
                magic
            );

            if (! context.Wait(timeout))
                throw new Common.TimeoutException("Method call timed out");

            if (context.exception_ != null)
                throw context.exception_;

            return context.executionReportList_.ToArray();
        }

        public void NewOrderAsync
        (
            object data,
            string clientOrderId,
            string symbol,
            TickTrader.FDK.Common.OrderType type,
            TickTrader.FDK.Common.OrderSide side,
            double qty,
            double? maxVisibleQty,
            double? price,
            double? stopPrice,
            TickTrader.FDK.Common.OrderTimeInForce? timeInForce,
            DateTime? expireTime,
            double? stopLoss,
            double? takeProfit,
            string comment,
            string tag,
            int? magic
        )
        {
            NewOrderAsyncContext context = new NewOrderAsyncContext(false);
            context.Data = data;

            NewOrderInternal
            (
                context,
                clientOrderId,
                symbol,
                type,
                side,
                qty,
                maxVisibleQty,
                price,
                stopPrice,
                timeInForce,
                expireTime,
                stopLoss,
                takeProfit,
                comment,
                tag,
                magic
            );
        }

        void NewOrderInternal
        (
            NewOrderAsyncContext context,
            string clientOrderId,
            string symbol, 
            TickTrader.FDK.Common.OrderType type, 
            TickTrader.FDK.Common.OrderSide side,            
            double qty, 
            double? maxVisibleQty, 
            double? price, 
            double? stopPrice,
            TickTrader.FDK.Common.OrderTimeInForce? timeInForce,      
            DateTime? expireTime, 
            double? stopLoss, 
            double? takeProfit, 
            string comment, 
            string tag, 
            int? magic
        )
        {
            NewOrderSingle message = new NewOrderSingle(0);
            message.ClOrdId = clientOrderId;
            message.SymbolId = symbol;
            message.Type = GetOrderType(type);            
            message.Side = GetOrderSide(side);
            message.Qty = qty;
            message.MaxVisibleQty = maxVisibleQty;
            message.Price = price;
            message.StopPrice = stopPrice;

            if (timeInForce.HasValue)
            {
                message.TimeInForce = GetOrderTimeInForce(timeInForce.Value);
            }
            else
                message.TimeInForce = null;

            message.ExpireTime = expireTime;
            message.StopLoss = stopLoss;
            message.TakeProfit = takeProfit;
            message.Comment = comment;
            message.Tag = tag;
            message.Magic = magic;

            session_.SendNewOrderSingle(context, message);
        }

        public TickTrader.FDK.Common.ExecutionReport[] ReplaceOrder
        (
            string clientOrderId,
            string origClientOrderId,
            string orderId,
            string symbol,
            TickTrader.FDK.Common.OrderType type,
            TickTrader.FDK.Common.OrderSide side,
            double qty,
            double? maxVisibleQty,
            double? price,
            double? stopPrice,
            TickTrader.FDK.Common.OrderTimeInForce? timeInForce,
            DateTime? expireTime,
            double? stopLoss,
            double? takeProfit,
            bool inFlightMitigation,
            double? currentQty,
            string comment,
            string tag,
            int? magic,
            int timeout
        )
        {
            ReplaceOrderAsyncContext context = new ReplaceOrderAsyncContext(true);

            context.executionReportList_ = new List<Common.ExecutionReport>();

            ReplaceOrderInternal
            (
                context,
                clientOrderId,
                origClientOrderId,
                orderId,
                symbol,
                type,
                side,
                qty,
                maxVisibleQty,
                price,
                stopPrice,
                timeInForce,
                expireTime,
                stopLoss,
                takeProfit,
                inFlightMitigation,
                currentQty,
                comment,
                tag,
                magic
            );

            if (! context.Wait(timeout))
                throw new Common.TimeoutException("Method call timed out");

            if (context.exception_ != null)
                throw context.exception_;

            return context.executionReportList_.ToArray();
        }

        public void ReplaceOrderAsync
        (
            object data,
            string clientOrderId,
            string origClientOrderId,
            string orderId,
            string symbol,
            TickTrader.FDK.Common.OrderType type,
            TickTrader.FDK.Common.OrderSide side,
            double qty,
            double? maxVisibleQty,
            double? price,
            double? stopPrice,
            TickTrader.FDK.Common.OrderTimeInForce? timeInForce,
            DateTime? expireTime,
            double? stopLoss,
            double? takeProfit,
            bool inFlightMitigation,
            double? currentQty,
            string comment,
            string tag,
            int? magic
        )
        {
            ReplaceOrderAsyncContext context = new ReplaceOrderAsyncContext(false);
            context.Data = data;

            ReplaceOrderInternal
            (
                context,
                clientOrderId,
                origClientOrderId,
                orderId,
                symbol,
                type,
                side,
                qty,
                maxVisibleQty,
                price,
                stopPrice,
                timeInForce,
                expireTime,
                stopLoss,
                takeProfit,
                inFlightMitigation,
                currentQty,
                comment,
                tag,
                magic
            );
        }

        void ReplaceOrderInternal
        (
            ReplaceOrderAsyncContext context,
            string clientOrderId,
            string origClientOrderId,
            string orderId,
            string symbol, 
            TickTrader.FDK.Common.OrderType type, 
            TickTrader.FDK.Common.OrderSide side,            
            double qty, 
            double? maxVisibleQty, 
            double? price, 
            double? stopPrice,       
            TickTrader.FDK.Common.OrderTimeInForce? timeInForce,
            DateTime? expireTime, 
            double? stopLoss, 
            double? takeProfit, 
            bool inFlightMitigation,
            double? currentQty,
            string comment, 
            string tag, 
            int? magic
        )
        {
            OrderCancelReplaceRequest message = new OrderCancelReplaceRequest(0);
            message.ClOrdId = clientOrderId;
            message.OrigClOrdId = origClientOrderId;

            if (orderId != null)
            {
                message.OrderId = long.Parse(orderId);
            }
            else
                message.OrderId = null;

            message.SymbolId = symbol;
            message.Type = GetOrderType(type);            
            message.Side = GetOrderSide(side);
            message.Qty = qty;
            message.MaxVisibleQty = maxVisibleQty;
            message.Price = price;
            message.StopPrice = stopPrice;

            if (timeInForce.HasValue)
            {
                message.TimeInForce = GetOrderTimeInForce(timeInForce.Value);
            }
            else
                message.TimeInForce = null;

            message.ExpireTime = expireTime;
            message.StopLoss = stopLoss;
            message.TakeProfit = takeProfit;
            message.Comment = comment;
            message.Tag = tag;
            message.Magic = magic;
            message.InFlightMitigationFlag = inFlightMitigation;
            message.LeavesQty = currentQty;

            session_.SendOrderCancelReplaceRequest(context, message);
        }

        public TickTrader.FDK.Common.ExecutionReport[] CancelOrder(string clientOrderId, string origClientOrderId, string orderId, int timeout)
        {
            CancelOrderAsyncContext context = new CancelOrderAsyncContext(true);

            context.executionReportList_ = new List<Common.ExecutionReport>();

            CancelOrderInternal(context, clientOrderId, origClientOrderId, orderId);

            if (! context.Wait(timeout))
                throw new Common.TimeoutException("Method call timed out");

            if (context.exception_ != null)
                throw context.exception_;

            return context.executionReportList_.ToArray();
        }

        public void CancelOrderAsync(object data, string clientOrderId, string origClientOrderId, string orderId)
        {
            CancelOrderAsyncContext context = new CancelOrderAsyncContext(false);
            context.Data = data;

            CancelOrderInternal(context, clientOrderId, origClientOrderId, orderId);
        }

        void CancelOrderInternal(CancelOrderAsyncContext context, string clientOrderId, string origClientOrderId, string orderId)
        {
            OrderCancelRequest message = new OrderCancelRequest(0);
            message.ClOrdId = clientOrderId;
            message.OrigClOrdId = origClientOrderId;

            if (orderId != null)
            {
                message.OrderId = long.Parse(orderId);
            }
            else
                message.OrderId = null;

            session_.SendOrderCancelRequest(context, message);
        }

        public TickTrader.FDK.Common.ExecutionReport[] ClosePosition(string clientOrderId, string orderId, double? qty, int timeout)
        {
            ClosePositionAsyncContext context = new ClosePositionAsyncContext(true);

            context.executionReportList_ = new List<Common.ExecutionReport>();

            ClosePositionInternal(context, clientOrderId, orderId, qty);

            if (! context.Wait(timeout))
                throw new Common.TimeoutException("Method call timed out");

            if (context.exception_ != null)
                throw context.exception_;

            return context.executionReportList_.ToArray();
        }

        public void ClosePositionAsync(object data, string clientOrderId, string orderId, double? qty)
        {
            ClosePositionAsyncContext context = new ClosePositionAsyncContext(false);
            context.Data = data;

            ClosePositionInternal(context, clientOrderId, orderId, qty);
        }

        void ClosePositionInternal(ClosePositionAsyncContext context, string clientOrderId, string orderId, double? qty)
        {
            ClosePositionRequest message = new ClosePositionRequest(0);
            message.ClOrdId = clientOrderId;
            message.OrderId = long.Parse(orderId);
            message.Type = ClosePositionRequestType.Close;
            message.Qty = qty;

            session_.SendClosePositionRequest(context, message);
        }

        public TickTrader.FDK.Common.ExecutionReport[] ClosePositionBy(string clientOrderId, string orderId, string byOrderId, int timeout)
        {
            ClosePositionByAsyncContext context = new ClosePositionByAsyncContext(true);

            context.executionReportList_ = new List<Common.ExecutionReport>();

            ClosePositionByInternal(context, clientOrderId, orderId, byOrderId);

            if (! context.Wait(timeout))
                throw new Common.TimeoutException("Method call timed out");

            if (context.exception_ != null)
                throw context.exception_;

            return context.executionReportList_.ToArray();
        }

        public void ClosePositionByAsync(object data, string clientOrderId, string orderId, string byOrderId)
        {
            ClosePositionByAsyncContext context = new ClosePositionByAsyncContext(false);
            context.Data = data;

            ClosePositionByInternal(context, clientOrderId, orderId, byOrderId);
        }
        
        void ClosePositionByInternal(ClosePositionByAsyncContext context, string clientOrderId, string orderId, string byOrderId)
        {
            ClosePositionRequest message = new ClosePositionRequest(0);
            message.ClOrdId = clientOrderId;
            message.OrderId = long.Parse(orderId);
            message.Type = ClosePositionRequestType.CloseBy;
            message.ByOrderId = long.Parse(byOrderId);

            session_.SendClosePositionByRequest(context, message);
        }

        #endregion

        #region Implementation

        SoftFX.Net.OrderEntry.OrderType GetOrderType(TickTrader.FDK.Common.OrderType type)
        {
            switch (type)
            {
                case TickTrader.FDK.Common.OrderType.Market:
                    return SoftFX.Net.OrderEntry.OrderType.Market;

                case TickTrader.FDK.Common.OrderType.Limit:
                    return SoftFX.Net.OrderEntry.OrderType.Limit;

                case TickTrader.FDK.Common.OrderType.Stop:
                    return SoftFX.Net.OrderEntry.OrderType.Stop;

                case TickTrader.FDK.Common.OrderType.Position:
                    return SoftFX.Net.OrderEntry.OrderType.Position;

                case TickTrader.FDK.Common.OrderType.StopLimit:
                    return SoftFX.Net.OrderEntry.OrderType.StopLimit;

                default:
                    throw new Exception("Invalid order type : " + type);
            }
        }

        SoftFX.Net.OrderEntry.OrderSide GetOrderSide(TickTrader.FDK.Common.OrderSide side)
        {
            switch (side)
            {
                case TickTrader.FDK.Common.OrderSide.Buy:
                    return SoftFX.Net.OrderEntry.OrderSide.Buy;

                case TickTrader.FDK.Common.OrderSide.Sell:
                    return SoftFX.Net.OrderEntry.OrderSide.Sell;

                default:
                    throw new Exception("Invalid order side : " + side);
            }
        }

        SoftFX.Net.OrderEntry.OrderTimeInForce GetOrderTimeInForce(TickTrader.FDK.Common.OrderTimeInForce time)
        {
            switch (time)
            {
                case TickTrader.FDK.Common.OrderTimeInForce.GoodTillCancel:
                    return SoftFX.Net.OrderEntry.OrderTimeInForce.GoodTillCancel;

                case TickTrader.FDK.Common.OrderTimeInForce.ImmediateOrCancel:
                    return SoftFX.Net.OrderEntry.OrderTimeInForce.ImmediateOrCancel;

                case TickTrader.FDK.Common.OrderTimeInForce.GoodTillDate:
                    return SoftFX.Net.OrderEntry.OrderTimeInForce.GoodTillDate;

                default:
                    throw new Exception("Invalid order time : " + time);
            }
        }

        interface IAsyncContext
        {
            void ProcessDisconnect(OrderEntry orderEntry, string text);
        }

        class ConnectAsyncContext : ConnectClientContext
        {
            public ConnectAsyncContext(bool waitbale) : base(waitbale)
            {
            }

            public Exception exception_;
        }

        class DisconnectAsyncContext : DisconnectClientContext
        {
            public DisconnectAsyncContext(bool waitable) : base(waitable)
            {
            }

            public string text_;
        }

        class LoginAsyncContext : LoginRequestClientContext, IAsyncContext
        {
            public LoginAsyncContext(bool waitable) : base(waitable)
            {
            }

            public void ProcessDisconnect(OrderEntry orderEntry, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (orderEntry.LoginErrorEvent != null)
                {
                    try
                    {
                        orderEntry.LoginErrorEvent(orderEntry, Data, exception);
                    }
                    catch
                    {
                    }
                }

                if (Waitable)
                {
                    exception_ = exception;
                }
            }

            public Exception exception_;
        }

        class TwoFactorLoginResponseAsyncContext : TwoFactorLoginResponseClientContext, IAsyncContext
        {
            public TwoFactorLoginResponseAsyncContext(bool waitable) : base(waitable)
            {
            }

            public void ProcessDisconnect(OrderEntry orderEntry, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (orderEntry.LoginErrorEvent != null)
                {
                    try
                    {
                        orderEntry.LoginErrorEvent(orderEntry, Data, exception);
                    }
                    catch
                    {
                    }
                }

                if (Waitable)
                {
                    exception_ = exception;
                }
            }

            public Exception exception_;
            public DateTime dateTime_;
        }

        class TwoFactorLoginResumeAsyncContext : TwoFactorLoginResumeClientContext, IAsyncContext
        {
            public TwoFactorLoginResumeAsyncContext(bool waitbale) : base(waitbale)
            {
            }

            public void ProcessDisconnect(OrderEntry orderEntry, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (orderEntry.LoginErrorEvent != null)
                {
                    try
                    {
                        orderEntry.LoginErrorEvent(orderEntry, Data, exception);
                    }
                    catch
                    {
                    }
                }

                if (Waitable)
                {
                    exception_ = exception;
                }
            }

            public Exception exception_;
            public DateTime dateTime_;
        }

        class LogoutAsyncContext : LogoutClientContext, IAsyncContext
        {
            public LogoutAsyncContext(bool waitable) : base(waitable)
            {
            }

            public void ProcessDisconnect(OrderEntry orderEntry, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (orderEntry.LogoutErrorEvent != null)
                {
                    try
                    {
                        orderEntry.LogoutErrorEvent(orderEntry, Data, exception);
                    }
                    catch
                    {
                    }
                }

                if (Waitable)
                {
                    exception_ = exception;
                }
            }

            public Exception exception_;
            public LogoutInfo logoutInfo_;
        }

        class TradeServerInfoAsyncContext : TradeServerInfoRequestClientContext, IAsyncContext
        {
            public TradeServerInfoAsyncContext(bool waitable) : base(waitable)
            {
            }

            public void ProcessDisconnect(OrderEntry orderEntry, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (orderEntry.TradeServerInfoErrorEvent != null)
                {
                    try
                    {
                        orderEntry.TradeServerInfoErrorEvent(orderEntry, Data, exception);
                    }
                    catch
                    {
                    }
                }

                if (Waitable)
                {
                    exception_ = exception;
                }
            }

            public Exception exception_;
            public TradeServerInfo tradeServerInfo_;
        }

        class AccountInfoAsyncContext : AccountInfoRequestClientContext, IAsyncContext
        {
            public AccountInfoAsyncContext(bool waitable) : base(waitable)
            {
            }

            public void ProcessDisconnect(OrderEntry orderEntry, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (orderEntry.AccountInfoErrorEvent != null)
                {
                    try
                    {
                        orderEntry.AccountInfoErrorEvent(orderEntry, Data, exception);
                    }
                    catch
                    {
                    }
                }

                if (Waitable)
                {
                    exception_ = exception;
                }
            }

            public Exception exception_;
            public TickTrader.FDK.Common.AccountInfo accountInfo_;
        }

        class SessionInfoAsyncContext : TradingSessionStatusRequestClientContext, IAsyncContext
        {
            public SessionInfoAsyncContext(bool waitable) : base(waitable)
            {
            }

            public void ProcessDisconnect(OrderEntry orderEntry, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (orderEntry.SessionInfoErrorEvent != null)
                {
                    try
                    {
                        orderEntry.SessionInfoErrorEvent(orderEntry, Data, exception);
                    }
                    catch
                    {
                    }
                }

                if (Waitable)
                {
                    exception_ = exception;
                }
            }

            public Exception exception_;
            public TickTrader.FDK.Common.SessionInfo sessionInfo_;
        }

        class OrdersAsyncContext : OrderMassStatusRequestClientContext, IAsyncContext
        {
            public OrdersAsyncContext(bool waitable) : base(waitable)
            {
            }

            public void ProcessDisconnect(OrderEntry orderEntry, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (orderEntry.OrdersErrorEvent != null)
                {
                    try
                    {
                        orderEntry.OrdersErrorEvent(orderEntry, Data, exception);
                    }
                    catch
                    {
                    }
                }

                if (Waitable)
                {
                    enumerator_.SetError(exception);
                }
            }
                        
            public TickTrader.FDK.Common.ExecutionReport executionReport_;
            public GetOrdersEnumerator enumerator_;
        }

        class CancelOrdersAsyncContext : OrderMassStatusCancelRequestClientContext, IAsyncContext
        {
            public CancelOrdersAsyncContext(bool waitable) : base(waitable)
            {
            }

            public void ProcessDisconnect(OrderEntry orderEntry, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (orderEntry.CancelOrderErrorEvent != null)
                {
                    try
                    {
                        orderEntry.CancelOrderErrorEvent(orderEntry, Data, exception);
                    }
                    catch
                    {
                    }
                }

                if (Waitable)
                {
                    exception_ = exception;
                }
            }

            public Exception exception_;
        }

        class PositionsAsyncContext : PositionListRequestClientContext, IAsyncContext
        {
            public PositionsAsyncContext(bool waitable) : base(waitable)
            {
            }

            public void ProcessDisconnect(OrderEntry orderEntry, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (orderEntry.PositionsErrorEvent != null)
                {
                    try
                    {
                        orderEntry.PositionsErrorEvent(orderEntry, Data, exception);
                    }
                    catch
                    {
                    }
                }

                if (Waitable)
                {
                    exception_ = exception;
                }
            }

            public Exception exception_;
            public TickTrader.FDK.Common.Position[] positions_;
        }

        class NewOrderAsyncContext : NewOrderSingleClientContext, IAsyncContext
        {
            public NewOrderAsyncContext(bool waitable) : base(waitable)
            {
            }

            public void ProcessDisconnect(OrderEntry orderEntry, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (orderEntry.NewOrderErrorEvent != null)
                {
                    try
                    {
                        orderEntry.NewOrderErrorEvent(orderEntry, Data, exception);
                    }
                    catch
                    {
                    }
                }

                if (Waitable)
                {
                    exception_ = exception;
                }
            }

            public Exception exception_;
            public List<TickTrader.FDK.Common.ExecutionReport> executionReportList_;
        }

        class ReplaceOrderAsyncContext : OrderCancelReplaceRequestClientContext, IAsyncContext
        {
            public ReplaceOrderAsyncContext(bool waitable) : base(waitable)
            {
            }

            public void ProcessDisconnect(OrderEntry orderEntry, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (orderEntry.ReplaceOrderErrorEvent != null)
                {
                    try
                    {
                        orderEntry.ReplaceOrderErrorEvent(orderEntry, Data, exception);
                    }
                    catch
                    {
                    }
                }

                if (Waitable)
                {
                    exception_ = exception;
                }
            }

            public Exception exception_;
            public List<TickTrader.FDK.Common.ExecutionReport> executionReportList_;
        }

        class CancelOrderAsyncContext : OrderCancelRequestClientContext, IAsyncContext
        {
            public CancelOrderAsyncContext(bool waitable) : base(waitable)
            {
            }

            public void ProcessDisconnect(OrderEntry orderEntry, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (orderEntry.CancelOrderErrorEvent != null)
                {
                    try
                    {
                        orderEntry.CancelOrderErrorEvent(orderEntry, Data, exception);
                    }
                    catch
                    {
                    }
                }

                if (Waitable)
                {
                    exception_ = exception;
                }
            }

            public Exception exception_;
            public List<TickTrader.FDK.Common.ExecutionReport> executionReportList_;
        }

        class ClosePositionAsyncContext : ClosePositionRequestClientContext, IAsyncContext
        {
            public ClosePositionAsyncContext(bool waitable) : base(waitable)
            {
            }

            public void ProcessDisconnect(OrderEntry orderEntry, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (orderEntry.ClosePositionErrorEvent != null)
                {
                    try
                    {
                        orderEntry.ClosePositionErrorEvent(orderEntry, Data, exception);
                    }
                    catch
                    {
                    }
                }

                if (Waitable)
                {
                    exception_ = exception;
                }
            }

            public Exception exception_;
            public List<TickTrader.FDK.Common.ExecutionReport> executionReportList_;
        }

        class ClosePositionByAsyncContext : ClosePositionByRequestClientContext, IAsyncContext
        {
            public ClosePositionByAsyncContext(bool waitable) : base(waitable)
            {
            }

            public void ProcessDisconnect(OrderEntry orderEntry, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (orderEntry.ClosePositionByErrorEvent != null)
                {
                    try
                    {
                        orderEntry.ClosePositionByErrorEvent(orderEntry, Data, exception);
                    }
                    catch
                    {
                    }
                }

                if (Waitable)
                {
                    exception_ = exception;
                }
            }

            public Exception exception_;
            public List<TickTrader.FDK.Common.ExecutionReport> executionReportList_;
        }

        class ClientSessionListener : SoftFX.Net.OrderEntry.ClientSessionListener
        {
            public ClientSessionListener(OrderEntry orderEntry)
            {
                client_ = orderEntry;
            }

            public override void OnConnect(ClientSession clientSession, ConnectClientContext connectContext)
            {
                try
                {
                    ConnectAsyncContext connectAsyncContext = (ConnectAsyncContext)connectContext;

                    if (client_.ConnectResultEvent != null)
                    {
                        try
                        {
                            client_.ConnectResultEvent(client_, connectAsyncContext.Data);
                        }
                        catch
                        {
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnConnect(ClientSession clientSession)
            {
                try
                {
                    if (client_.ReconnectEvent != null)
                    {
                        try
                        {
                            client_.ReconnectEvent(client_);
                        }
                        catch
                        {
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnConnectError(ClientSession clientSession, ConnectClientContext connectContext, string text)
            {               
                try
                {
                    ConnectAsyncContext connectAsyncContext = (ConnectAsyncContext) connectContext;

                    ConnectException exception = new ConnectException(text);

                    if (client_.ConnectErrorEvent != null)
                    {
                        try
                        {
                            client_.ConnectErrorEvent(client_, connectAsyncContext.Data, exception);
                        }
                        catch
                        {
                        }
                    }

                    if (connectAsyncContext.Waitable)
                    {
                        connectAsyncContext.exception_ = exception;
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnConnectError(ClientSession clientSession, string text)
            {                
                try
                {
                    ConnectException exception = new ConnectException(text);

                    if (client_.ReconnectErrorEvent != null)
                    {
                        try
                        {
                            client_.ReconnectErrorEvent(client_, exception);
                        }
                        catch
                        {
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnDisconnect(ClientSession clientSession, DisconnectClientContext disconnectContext, ClientContext[] contexts, string text)
            {
                try
                {
                    DisconnectAsyncContext disconnectAsyncContext = (DisconnectAsyncContext)disconnectContext;

                    foreach (ClientContext context in contexts)
                    {
                        try
                        {
                            ((IAsyncContext)context).ProcessDisconnect(client_, text);
                        }
                        catch
                        {
                        }
                    }

                    if (client_.DisconnectResultEvent != null)
                    {
                        try
                        {
                            client_.DisconnectResultEvent(client_, disconnectAsyncContext.Data, text);
                        }
                        catch
                        {
                        }
                    }

                    if (disconnectAsyncContext.Waitable)
                    {
                        disconnectAsyncContext.text_ = text;
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnDisconnect(ClientSession clientSession, ClientContext[] contexts, string text)
            {
                try
                {
                    foreach (ClientContext context in contexts)
                    {
                        try
                        {
                            ((IAsyncContext)context).ProcessDisconnect(client_, text);
                        }
                        catch
                        {
                        }
                    }

                    if (client_.DisconnectEvent != null)
                    {
                        try
                        {
                            client_.DisconnectEvent(client_, text);
                        }
                        catch
                        {
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnSend(ClientSession clientSession, int size)
            {
                try
                {
                    if (client_.SendEvent != null)
                    {
                        try
                        {
                            client_.SendEvent(client_);
                        }
                        catch
                        {
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnLoginReport(ClientSession session, LoginRequestClientContext LoginRequestClientContext, LoginReport message)
            {
                try
                {
                    LoginAsyncContext context = (LoginAsyncContext)LoginRequestClientContext;

                    try
                    {
                        if (client_.LoginResultEvent != null)
                        {
                            try
                            {
                                client_.LoginResultEvent(client_, context.Data);
                            }
                            catch
                            {
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.LoginErrorEvent != null)
                        {
                            try
                            {
                                client_.LoginErrorEvent(client_, context.Data, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.exception_ = exception;
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnLoginReject(ClientSession session, LoginRequestClientContext LoginRequestClientContext, LoginReject message)
            {
                try
                {
                    LoginAsyncContext context = (LoginAsyncContext)LoginRequestClientContext;

                    TickTrader.FDK.Common.LogoutReason reason = GetLogoutReason(message.Reason);

                    LoginException exception = new LoginException(reason, message.Text);

                    if (client_.LoginErrorEvent != null)
                    {
                        try
                        {
                            client_.LoginErrorEvent(client_, context.Data, exception);
                        }
                        catch
                        {
                        }
                    }

                    if (context.Waitable)
                    {
                        context.exception_ = exception;
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnTwoFactorLoginRequest(ClientSession session, LoginRequestClientContext LoginRequestClientContext, TwoFactorLogin message)
            {
                try
                {
                    LoginAsyncContext context = (LoginAsyncContext)LoginRequestClientContext;

                    try
                    {
                        string text = message.Text;

                        if (client_.TwoFactorLoginRequestEvent != null)
                        {
                            try
                            {
                                client_.TwoFactorLoginRequestEvent(client_, text);
                            }
                            catch
                            {
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.LoginErrorEvent != null)
                        {
                            try
                            {
                                client_.LoginErrorEvent(client_, context.Data, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.exception_ = exception;
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnTwoFactorLoginSuccess(ClientSession session, LoginRequestClientContext LoginRequestClientContext, TwoFactorLoginResponseClientContext TwoFactorLoginResponseClientContext, TwoFactorLogin message)
            {
                try
                {
                    LoginAsyncContext loginContext = (LoginAsyncContext)LoginRequestClientContext;

                    try
                    {
                        TwoFactorLoginResponseAsyncContext responseContext = (TwoFactorLoginResponseAsyncContext)TwoFactorLoginResponseClientContext;

                        try
                        {
                            DateTime expireTime = message.ExpireTime.Value;

                            if (client_.TwoFactorLoginResultEvent != null)
                            {
                                try
                                {
                                    client_.TwoFactorLoginResultEvent(client_, responseContext.Data, expireTime);
                                }
                                catch
                                {
                                }
                            }

                            if (responseContext.Waitable)
                            {
                                responseContext.dateTime_ = expireTime;
                            }
                        }
                        catch (Exception exception)
                        {
                            if (client_.TwoFactorLoginErrorEvent != null)
                            {
                                try
                                {
                                    client_.TwoFactorLoginErrorEvent(client_, responseContext.Data, exception);
                                }
                                catch
                                {
                                }
                            }

                            if (responseContext.Waitable)
                            {
                                responseContext.exception_ = exception;
                            }

                            throw;
                        }

                        if (client_.LoginResultEvent != null)
                        {
                            try
                            {
                                client_.LoginResultEvent(client_, loginContext.Data);
                            }
                            catch
                            {
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.LoginErrorEvent != null)
                        {
                            try
                            {
                                client_.LoginErrorEvent(client_, loginContext.Data, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (loginContext.Waitable)
                        {
                            loginContext.exception_ = exception;
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnTwoFactorLoginReject(ClientSession session, LoginRequestClientContext LoginRequestClientContext, TwoFactorLoginResponseClientContext TwoFactorLoginResponseClientContext, TwoFactorReject message)
            {
                try
                {
                    LoginAsyncContext loginContext = (LoginAsyncContext) LoginRequestClientContext;
                    TwoFactorLoginResponseAsyncContext responseContext = (TwoFactorLoginResponseAsyncContext)TwoFactorLoginResponseClientContext;

                    Exception exception = new Exception(message.Text);

                    if (client_.TwoFactorLoginErrorEvent != null)
                    {
                        try
                        {
                            client_.TwoFactorLoginErrorEvent(client_, responseContext.Data, exception);
                        }
                        catch
                        {
                        }
                    }

                    if (responseContext.Waitable)
                    {
                        responseContext.exception_ = exception;
                    }

                    // the login procedure continues..
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnTwoFactorLoginError(ClientSession session, LoginRequestClientContext LoginRequestClientContext, TwoFactorLoginResponseClientContext TwoFactorLoginResponseClientContext, TwoFactorLogin message)
            {
                try
                {
                    LoginAsyncContext loginContext = (LoginAsyncContext)LoginRequestClientContext;
                    TwoFactorLoginResponseAsyncContext responseContext = (TwoFactorLoginResponseAsyncContext)TwoFactorLoginResponseClientContext;

                    Exception exception = new Exception(message.Text);

                    if (client_.TwoFactorLoginErrorEvent != null)
                    {
                        try
                        {
                            client_.TwoFactorLoginErrorEvent(client_, responseContext.Data, exception);
                        }
                        catch
                        {
                        }
                    }

                    if (responseContext.Waitable)
                    {
                        responseContext.exception_ = exception;
                    }

                    if (client_.LoginErrorEvent != null)
                    {
                        try
                        {
                            client_.LoginErrorEvent(client_, loginContext.Data, exception);
                        }
                        catch
                        {
                        }
                    }

                    if (loginContext.Waitable)
                    {
                        loginContext.exception_ = exception;
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnTwoFactorLoginResume(ClientSession session, TwoFactorLoginResumeClientContext TwoFactorLoginResumeClientContext, TwoFactorLogin message)
            {
                try
                {
                    TwoFactorLoginResumeAsyncContext context = (TwoFactorLoginResumeAsyncContext) TwoFactorLoginResumeClientContext;

                    try
                    {
                        DateTime expireTime = message.ExpireTime.Value;

                        if (client_.TwoFactorLoginResumeEvent != null)
                        {
                            try
                            {
                                client_.TwoFactorLoginResumeEvent(client_, context.Data, expireTime);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.dateTime_ = expireTime;
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.TwoFactorLoginErrorEvent != null)
                        {
                            try
                            {
                                client_.TwoFactorLoginErrorEvent(client_, context.Data, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.exception_ = exception;
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnLogout(ClientSession session, LogoutClientContext LogoutClientContext, Logout message)
            {
                try
                {
                    LogoutAsyncContext context = (LogoutAsyncContext)LogoutClientContext;

                    try
                    {
                        LogoutInfo result = new LogoutInfo();
                        result.Reason = GetLogoutReason(message.Reason);
                        result.Message = message.Text;

                        if (client_.LogoutResultEvent != null)
                        {
                            try
                            {
                                client_.LogoutResultEvent(client_, context.Data, result);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.logoutInfo_ = result;
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.LogoutErrorEvent != null)
                        {
                            try
                            {
                                client_.LogoutErrorEvent(client_, context.Data, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.exception_ = exception;
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnTradeServerInfoReport(ClientSession session, TradeServerInfoRequestClientContext TradeServerInfoRequestClientContext, TradeServerInfoReport message)
            {
                try
                {
                    TradeServerInfoAsyncContext context = (TradeServerInfoAsyncContext) TradeServerInfoRequestClientContext;

                    try
                    {
                        TickTrader.FDK.Common.TradeServerInfo resultTradeServerInfo = new TickTrader.FDK.Common.TradeServerInfo();
                        resultTradeServerInfo.CompanyName = message.CompanyName;
                        resultTradeServerInfo.CompanyFullName = message.CompanyFullName;
                        resultTradeServerInfo.CompanyDescription = message.CompanyDescription;
                        resultTradeServerInfo.CompanyAddress = message.CompanyAddress;
                        resultTradeServerInfo.CompanyEmail = message.CompanyEmail;
                        resultTradeServerInfo.CompanyPhone = message.CompanyPhone;
                        resultTradeServerInfo.CompanyWebSite = message.CompanyWebSite;
                        resultTradeServerInfo.ServerName = message.ServerName;
                        resultTradeServerInfo.ServerFullName = message.ServerFullName;
                        resultTradeServerInfo.ServerDescription = message.ServerDescription;
                        resultTradeServerInfo.ServerAddress = message.GatewayAddress;

                        TradeServerRestApi restApi = message.RestApi;
                        resultTradeServerInfo.ServerRestPort = restApi.Port;

                        TradeServerWsApi wsApi = message.WsApi;
                        resultTradeServerInfo.ServerWebSocketFeedPort = wsApi.FeedPort;
                        resultTradeServerInfo.ServerWebSocketTradePort = wsApi.TradePort;

                        TradeServerSfxApi sfxApi = message.SfxApi;
                        resultTradeServerInfo.ServerSfxQuoteFeedPort = sfxApi.QuoteFeedPort;
                        resultTradeServerInfo.ServerSfxQuoteFeedPort = sfxApi.QuoteStorePort;
                        resultTradeServerInfo.ServerSfxOrderEntryPort = sfxApi.OrderEntryPort;
                        resultTradeServerInfo.ServerSfxTradeCapturePort = sfxApi.OrderEntryPort;

                        TradeServerFixApi fixApi = message.FixApi;
                        resultTradeServerInfo.ServerFixFeedSslPort = fixApi.FeedPort;
                        resultTradeServerInfo.ServerFixTradeSslPort = fixApi.TradePort;

                        resultTradeServerInfo.WebTerminalAddress = message.WebTerminalAddress;
                        resultTradeServerInfo.WebCabinetAddress = message.WebCabinetAddress;
                        resultTradeServerInfo.SupportCrmAddress = message.SupportCrmAddress;

                        if (client_.TradeServerInfoResultEvent != null)
                        {
                            try
                            {
                                client_.TradeServerInfoResultEvent(client_, context.Data, resultTradeServerInfo);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.tradeServerInfo_ = resultTradeServerInfo;
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.TradeServerInfoErrorEvent != null)
                        {
                            try
                            {
                                client_.TradeServerInfoErrorEvent(client_, context.Data, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.exception_ = exception;
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnTradeServerInfoReject(ClientSession session, TradeServerInfoRequestClientContext TradeServerInfoRequestClientContext, Reject message)
            {
                try
                {
                    TradeServerInfoAsyncContext context = (TradeServerInfoAsyncContext)TradeServerInfoRequestClientContext;

                    TickTrader.FDK.Common.RejectReason rejectReason = GetRejectReason(message.Reason);
                    RejectException exception = new RejectException(rejectReason, message.Text);

                    if (client_.TradeServerInfoErrorEvent != null)
                    {
                        try
                        {
                            client_.TradeServerInfoErrorEvent(client_, context.Data, exception);
                        }
                        catch
                        {
                        }
                    }

                    if (context.Waitable)
                    {
                        context.exception_ = exception;
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnAccountInfoReport(ClientSession session, AccountInfoRequestClientContext AccountInfoRequestClientContext, AccountInfoReport message)
            {
                try
                {
                    AccountInfoAsyncContext context = (AccountInfoAsyncContext)AccountInfoRequestClientContext;

                    try
                    {
                        TickTrader.FDK.Common.AccountInfo resultAccountInfo = new TickTrader.FDK.Common.AccountInfo();

                        FillAccountInfo(resultAccountInfo, message.AccountInfo);
                        
                        if (client_.AccountInfoResultEvent != null)
                        {
                            try
                            {
                                client_.AccountInfoResultEvent(client_, context.Data, resultAccountInfo);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.accountInfo_ = resultAccountInfo;
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.AccountInfoErrorEvent != null)
                        {
                            try
                            {
                                client_.AccountInfoErrorEvent(client_, context.Data, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.exception_ = exception;
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnAccountInfoReject(ClientSession session, AccountInfoRequestClientContext AccountInfoRequestClientContext, Reject message)
            {
                try
                {
                    AccountInfoAsyncContext context = (AccountInfoAsyncContext) AccountInfoRequestClientContext;

                    TickTrader.FDK.Common.RejectReason rejectReason = GetRejectReason(message.Reason);
                    RejectException exception = new RejectException(rejectReason, message.Text);

                    if (client_.AccountInfoErrorEvent != null)
                    {
                        try
                        {
                            client_.AccountInfoErrorEvent(client_, context.Data, exception);
                        }
                        catch
                        {
                        }
                    }

                    if (context.Waitable)
                    {
                        context.exception_ = exception;
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnTradingSessionStatusReport(ClientSession session, TradingSessionStatusRequestClientContext TradingSessionStatusRequestClientContext, TradingSessionStatusReport message)
            {
                try
                {
                    SessionInfoAsyncContext context = (SessionInfoAsyncContext)TradingSessionStatusRequestClientContext;

                    try
                    {
                        TickTrader.FDK.Common.SessionInfo resultStatusInfo = new TickTrader.FDK.Common.SessionInfo();
                        SoftFX.Net.OrderEntry.TradingSessionStatusInfo reportStatusInfo = message.StatusInfo;

                        resultStatusInfo.Status = GetSessionStatus(reportStatusInfo.Status);
                        resultStatusInfo.StartTime = reportStatusInfo.StartTime;
                        resultStatusInfo.EndTime = reportStatusInfo.EndTime;
                        resultStatusInfo.OpenTime = reportStatusInfo.OpenTime;
                        resultStatusInfo.CloseTime = reportStatusInfo.CloseTime;

                        TradingSessionStatusGroupArray reportGroups = reportStatusInfo.Groups;
                        int count = reportGroups.Length;
                        StatusGroupInfo[] resultGroups = new StatusGroupInfo[count];

                        for (int index = 0; index < count; ++index)
                        {
                            TradingSessionStatusGroup reportGroup = reportGroups[index];

                            StatusGroupInfo resultGroup = new StatusGroupInfo();
                            resultGroup.StatusGroupId = reportGroup.Id;
                            resultGroup.Status = GetSessionStatus(reportGroup.Status);
                            resultGroup.StartTime = reportGroup.StartTime;
                            resultGroup.EndTime = reportGroup.EndTime;
                            resultGroup.OpenTime = reportGroup.OpenTime;
                            resultGroup.CloseTime = reportGroup.CloseTime;

                            resultGroups[index] = resultGroup;
                        }

                        resultStatusInfo.StatusGroups = resultGroups;

                        if (client_.SessionInfoResultEvent != null)
                        {
                            try
                            {
                                client_.SessionInfoResultEvent(client_, context.Data, resultStatusInfo);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.sessionInfo_ = resultStatusInfo;
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.SessionInfoErrorEvent != null)
                        {
                            try
                            {
                                client_.SessionInfoErrorEvent(client_, context.Data, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.exception_ = exception;
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnTradingSessionStatusReject(ClientSession session, TradingSessionStatusRequestClientContext TradingSessionStatusRequestClientContext, Reject message)
            {
                try
                {
                    SessionInfoAsyncContext context = (SessionInfoAsyncContext)TradingSessionStatusRequestClientContext;

                    TickTrader.FDK.Common.RejectReason rejectReason = GetRejectReason(message.Reason);
                    RejectException exception = new RejectException(rejectReason, message.Text);

                    if (client_.SessionInfoErrorEvent != null)
                    {
                        try
                        {
                            client_.SessionInfoErrorEvent(client_, context.Data, exception);
                        }
                        catch
                        {
                        }
                    }

                    if (context.Waitable)
                    {
                        context.exception_ = exception;
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnOrderMassStatusBeginReport(ClientSession session, OrderMassStatusRequestClientContext OrderMassStatusRequestClientContext, OrderMassStatusBeginReport message)
            {
                try
                {
                    OrdersAsyncContext context = (OrdersAsyncContext)OrderMassStatusRequestClientContext;

                    context.executionReport_ = new TickTrader.FDK.Common.ExecutionReport();

                    if (client_.OrdersBeginResultEvent != null)
                    {
                        try
                        {
                            client_.OrdersBeginResultEvent(client_, context.Data, message.RequestId, message.OrderCount);
                        }
                        catch
                        {
                        }
                    }

                    if (context.Waitable)
                    {
                        context.enumerator_.SetBegin(message.RequestId, message.OrderCount);
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnOrderMassStatusReport(ClientSession session, OrderMassStatusRequestClientContext OrderMassStatusRequestClientContext, OrderMassStatusReport message)
            {
                try
                {
                    OrdersAsyncContext context = (OrdersAsyncContext) OrderMassStatusRequestClientContext;
                    TickTrader.FDK.Common.ExecutionReport resultExecutionReport = context.executionReport_;

                    resultExecutionReport.ExecutionType = Common.ExecutionType.OrderStatus;
                    resultExecutionReport.OrigClientOrderId = message.OrigClOrdId;
                    resultExecutionReport.OrderId = message.OrderId.ToString();
                    resultExecutionReport.Symbol = message.SymbolId;
                    resultExecutionReport.OrderSide = GetOrderSide(message.Side);
                    resultExecutionReport.OrderType = GetOrderType(message.Type);

                    if (message.TimeInForce.HasValue)
                    {
                        resultExecutionReport.OrderTimeInForce = GetOrderTimeInForce(message.TimeInForce.Value);
                    }
                    else
                        resultExecutionReport.OrderTimeInForce = null;

                    resultExecutionReport.InitialVolume = message.Qty;
                    resultExecutionReport.MaxVisibleVolume = message.MaxVisibleQty;
                    resultExecutionReport.Price = message.Price;
                    resultExecutionReport.StopPrice = message.StopPrice;
                    resultExecutionReport.Expiration = message.ExpireTime;
                    resultExecutionReport.TakeProfit = message.TakeProfit;
                    resultExecutionReport.StopLoss = message.StopLoss;
                    resultExecutionReport.MarketWithSlippage = (message.Flags & OrderFlags.Slippage) != 0;
                    resultExecutionReport.OrderStatus = GetOrderStatus(message.Status);
                    resultExecutionReport.InitialOrderType = GetOrderType(message.ReqType);
                    resultExecutionReport.InitialVolume = message.ReqQty;
                    resultExecutionReport.InitialPrice = message.ReqPrice;
                    resultExecutionReport.ExecutedVolume = message.CumQty;
                    resultExecutionReport.LeavesVolume = message.LeavesQty;
                    resultExecutionReport.TradeAmount = message.LastQty;
                    resultExecutionReport.TradePrice = message.LastPrice;
                    resultExecutionReport.Commission = message.Commission;
                    resultExecutionReport.AgentCommission = message.AgentCommission;
                    resultExecutionReport.ReducedOpenCommission = (message.CommissionFlags & OrderCommissionFlags.OpenReduced) != 0;
                    resultExecutionReport.ReducedCloseCommission = (message.CommissionFlags & OrderCommissionFlags.CloseReduced) != 0;
                    resultExecutionReport.Swap = message.Swap;
                    resultExecutionReport.AveragePrice = message.AvgPrice;
                    resultExecutionReport.Created = message.Created;
                    resultExecutionReport.Modified = message.Modified;
                    resultExecutionReport.RejectReason = TickTrader.FDK.Common.RejectReason.None;
                    resultExecutionReport.Comment = message.Comment;
                    resultExecutionReport.Tag = message.Tag;
                    resultExecutionReport.Magic = message.Magic;

                    if (client_.OrdersResultEvent != null)
                    {
                        try
                        {
                            client_.OrdersResultEvent(client_, context.Data, resultExecutionReport);
                        }
                        catch
                        {
                        }
                    }

                    if (context.Waitable)
                    {
                        TickTrader.FDK.Common.ExecutionReport executionReport = resultExecutionReport.Clone();

                        context.enumerator_.SetResult(executionReport);
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnOrderMassStatusEndReport(ClientSession session, OrderMassStatusRequestClientContext OrderMassStatusRequestClientContext, OrderMassStatusEndReport message)
            {
                try
                {
                    OrdersAsyncContext context = (OrdersAsyncContext)OrderMassStatusRequestClientContext;

                    if (client_.OrdersEndResultEvent != null)
                    {
                        try
                        {
                            client_.OrdersEndResultEvent(client_, context.Data);
                        }
                        catch
                        {
                        }
                    }

                    if (context.Waitable)
                    {
                        context.enumerator_.SetEnd();
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnOrderMassStatusReject(ClientSession session, OrderMassStatusRequestClientContext OrderMassStatusRequestClientContext, Reject message)
            {
                try
                {
                    OrdersAsyncContext context = (OrdersAsyncContext)OrderMassStatusRequestClientContext;

                    TickTrader.FDK.Common.RejectReason rejectReason = GetRejectReason(message.Reason);
                    RejectException exception = new RejectException(rejectReason, message.Text);

                    if (client_.OrdersErrorEvent != null)
                    {
                        try
                        {
                            client_.OrdersErrorEvent(client_, context.Data, exception);
                        }
                        catch
                        {
                        }
                    }

                    if (context.Waitable)
                    {
                        context.enumerator_.SetError(exception);                            
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnOrderMassStatusCancelReport(ClientSession session, OrderMassStatusCancelRequestClientContext OrderMassStatusCancelRequestClientContext, OrderMassStatusCancelReport message)
            {
                try
                {
                    CancelOrdersAsyncContext context = (CancelOrdersAsyncContext) OrderMassStatusCancelRequestClientContext;

                    try
                    {
                        if (client_.CancelOrdersResultEvent != null)
                        {
                            try
                            {
                                client_.CancelOrdersResultEvent(client_, context.Data);
                            }
                            catch
                            {
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.CancelOrdersErrorEvent != null)
                        {
                            try
                            {
                                client_.CancelOrdersErrorEvent(client_, context.Data, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.exception_ = exception;
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnOrderMassStatusCancelReject(ClientSession session, OrderMassStatusCancelRequestClientContext OrderMassStatusCancelRequestClientContext, Reject message)
            {
                try
                {
                    CancelOrdersAsyncContext context = (CancelOrdersAsyncContext) OrderMassStatusCancelRequestClientContext;

                    TickTrader.FDK.Common.RejectReason rejectReason = GetRejectReason(message.Reason);
                    RejectException exception = new RejectException(rejectReason, message.Text);                    

                    if (client_.CancelOrdersErrorEvent != null)
                    {
                        try
                        {
                            client_.CancelOrdersErrorEvent(client_, context.Data, exception);
                        }
                        catch
                        {
                        }
                    }

                    if (context.Waitable)
                    {
                        context.exception_ = exception;
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnPositionListReport(ClientSession session, PositionListRequestClientContext PositionListRequestClientContext, PositionListReport message)
            {
                try
                {
                    PositionsAsyncContext context = (PositionsAsyncContext)PositionListRequestClientContext;

                    try
                    {
                        SoftFX.Net.OrderEntry.PositionArray reportPositions = message.Positions;
                        int count = reportPositions.Length;
                        TickTrader.FDK.Common.Position[] resultPositions = new TickTrader.FDK.Common.Position[count];

                        for (int index = 0; index < count; ++index)
                        {
                            SoftFX.Net.OrderEntry.Position reportPosition = reportPositions[index];

                            TickTrader.FDK.Common.Position resultPosition = new TickTrader.FDK.Common.Position();
                            resultPosition.Symbol = reportPosition.SymbolId;
                            resultPosition.Commission = reportPosition.Commission;
                            resultPosition.AgentCommission = reportPosition.AgentCommission;
                            resultPosition.Swap = reportPosition.Swap;

                            PosType reportPosType = reportPosition.Type;

                            if (reportPosType == PosType.Long)
                            {
                                resultPosition.BuyAmount = reportPosition.Qty;
                                resultPosition.BuyPrice = reportPosition.Price;
                                resultPosition.SellAmount = 0;
                                resultPosition.SellPrice = 0;
                            }
                            else if (reportPosType == PosType.Short)
                            {
                                resultPosition.BuyAmount = 0;
                                resultPosition.BuyPrice = 0;
                                resultPosition.SellAmount = reportPosition.Qty;
                                resultPosition.SellPrice = reportPosition.Price;
                            }
                            else
                                throw new Exception("Invalid position type : " + reportPosType);

                            resultPositions[index] = resultPosition;
                        }

                        if (client_.PositionsResultEvent != null)
                        {
                            try
                            {
                                client_.PositionsResultEvent(client_, context.Data, resultPositions);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.positions_ = resultPositions;
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.PositionsErrorEvent != null)
                        {
                            try
                            {
                                client_.PositionsErrorEvent(client_, context.Data, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.exception_ = exception;
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnPositionListReject(ClientSession session, PositionListRequestClientContext PositionListRequestClientContext, Reject message)
            {
                try
                {
                    PositionsAsyncContext context = (PositionsAsyncContext)PositionListRequestClientContext;

                    TickTrader.FDK.Common.RejectReason rejectReason = GetRejectReason(message.Reason);
                    RejectException exception = new RejectException(rejectReason, message.Text);

                    if (client_.PositionsErrorEvent != null)
                    {
                        try
                        {
                            client_.PositionsErrorEvent(client_, context.Data, exception);
                        }
                        catch
                        {
                        }
                    }

                    if (context.Waitable)
                    {
                        context.exception_ = exception;
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnNewOrderSingleMarketNewReport(ClientSession session, NewOrderSingleClientContext NewOrderSingleClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                try
                {
                    NewOrderAsyncContext context = (NewOrderAsyncContext) NewOrderSingleClientContext;

                    try
                    {
                        TickTrader.FDK.Common.ExecutionReport result = new Common.ExecutionReport();
                        result.Last = false;

                        FillExecutionReport(result, message);

                        if (client_.NewOrderResultEvent != null)
                        {
                            try
                            {
                                client_.NewOrderResultEvent(client_, context.Data, result);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.executionReportList_.Add(result);
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.NewOrderErrorEvent != null)
                        {
                            try
                            {
                                client_.NewOrderErrorEvent(client_, context.Data, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.exception_ = exception;
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnNewOrderSingleMarketFillReport(ClientSession session, NewOrderSingleClientContext NewOrderSingleClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                try
                {
                    NewOrderAsyncContext context = (NewOrderAsyncContext) NewOrderSingleClientContext;

                    try
                    {
                        TickTrader.FDK.Common.ExecutionReport result = new Common.ExecutionReport();
                        result.Last = true;

                        FillExecutionReport(result, message);

                        if (client_.NewOrderResultEvent != null)
                        {
                            try
                            {
                                client_.NewOrderResultEvent(client_, context.Data, result);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.executionReportList_.Add(result);
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.NewOrderErrorEvent != null)
                        {
                            try
                            {
                                client_.NewOrderErrorEvent(client_, context.Data, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.exception_ = exception;
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnNewOrderSingleMarketPartialFillReport(ClientSession session, NewOrderSingleClientContext NewOrderSingleClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                try
                {
                    NewOrderAsyncContext context = (NewOrderAsyncContext)NewOrderSingleClientContext;

                    try
                    {
                        TickTrader.FDK.Common.ExecutionReport result = new Common.ExecutionReport();
                        result.Last = false;

                        FillExecutionReport(result, message);

                        if (client_.NewOrderResultEvent != null)
                        {
                            try
                            {
                                client_.NewOrderResultEvent(client_, context.Data, result);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.executionReportList_.Add(result);
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.NewOrderErrorEvent != null)
                        {
                            try
                            {
                                client_.NewOrderErrorEvent(client_, context.Data, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.exception_ = exception;
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnNewOrderSingleLimitIocNewReport(ClientSession session, NewOrderSingleClientContext NewOrderSingleClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                try
                {
                    NewOrderAsyncContext context = (NewOrderAsyncContext) NewOrderSingleClientContext;

                    try
                    {
                        TickTrader.FDK.Common.ExecutionReport result = new Common.ExecutionReport();
                        result.Last = false;

                        FillExecutionReport(result, message);

                        if (client_.NewOrderResultEvent != null)
                        {
                            try
                            {
                                client_.NewOrderResultEvent(client_, context.Data, result);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.executionReportList_.Add(result);
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.NewOrderErrorEvent != null)
                        {
                            try
                            {
                                client_.NewOrderErrorEvent(client_, context.Data, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.exception_ = exception;
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnNewOrderSingleLimitIocCalculatedReport(ClientSession session, NewOrderSingleClientContext NewOrderSingleClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                try
                {
                    NewOrderAsyncContext context = (NewOrderAsyncContext) NewOrderSingleClientContext;

                    try
                    {
                        TickTrader.FDK.Common.ExecutionReport result = new Common.ExecutionReport();
                        result.Last = false;

                        FillExecutionReport(result, message);

                        if (client_.NewOrderResultEvent != null)
                        {
                            try
                            {
                                client_.NewOrderResultEvent(client_, context.Data, result);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.executionReportList_.Add(result);
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.NewOrderErrorEvent != null)
                        {
                            try
                            {
                                client_.NewOrderErrorEvent(client_, context.Data, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.exception_ = exception;
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnNewOrderSingleLimitIocFillReport(ClientSession session, NewOrderSingleClientContext NewOrderSingleClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                try
                {
                    NewOrderAsyncContext context = (NewOrderAsyncContext) NewOrderSingleClientContext;

                    try
                    {
                        TickTrader.FDK.Common.ExecutionReport result = new Common.ExecutionReport();
                        result.Last = true;

                        FillExecutionReport(result, message);

                        if (client_.NewOrderResultEvent != null)
                        {
                            try
                            {
                                client_.NewOrderResultEvent(client_, context.Data, result);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.executionReportList_.Add(result);
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.NewOrderErrorEvent != null)
                        {
                            try
                            {
                                client_.NewOrderErrorEvent(client_, context.Data, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.exception_ = exception;
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnNewOrderSingleLimitIocPartialFillReport(ClientSession session, NewOrderSingleClientContext NewOrderSingleClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                try
                {
                    NewOrderAsyncContext context = (NewOrderAsyncContext) NewOrderSingleClientContext;

                    try
                    {
                        TickTrader.FDK.Common.ExecutionReport result = new Common.ExecutionReport();
                        result.Last = false;

                        FillExecutionReport(result, message);

                        if (client_.NewOrderResultEvent != null)
                        {
                            try
                            {
                                client_.NewOrderResultEvent(client_, context.Data, result);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.executionReportList_.Add(result);
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.NewOrderErrorEvent != null)
                        {
                            try
                            {
                                client_.NewOrderErrorEvent(client_, context.Data, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.exception_ = exception;
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnNewOrderSingleLimitIocCancelledReport(ClientSession session, NewOrderSingleClientContext NewOrderSingleClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                try
                {
                    NewOrderAsyncContext context = (NewOrderAsyncContext) NewOrderSingleClientContext;

                    try
                    {
                        TickTrader.FDK.Common.ExecutionReport result = new Common.ExecutionReport();
                        result.Last = true;

                        FillExecutionReport(result, message);

                        if (client_.NewOrderResultEvent != null)
                        {
                            try
                            {
                                client_.NewOrderResultEvent(client_, context.Data, result);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.executionReportList_.Add(result);
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.NewOrderErrorEvent != null)
                        {
                            try
                            {
                                client_.NewOrderErrorEvent(client_, context.Data, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.exception_ = exception;
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnNewOrderSingleLimitIocRejectReport(ClientSession session, NewOrderSingleClientContext NewOrderSingleClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                try
                {
                    NewOrderAsyncContext context = (NewOrderAsyncContext)NewOrderSingleClientContext;

                    TickTrader.FDK.Common.ExecutionReport result = new Common.ExecutionReport();
                    result.Last = true;

                    FillExecutionReport(result, message);

                    ExecutionException exception = new ExecutionException(result);

                    if (client_.NewOrderErrorEvent != null)
                    {
                        try
                        {
                            client_.NewOrderErrorEvent(client_, context.Data, exception);
                        }
                        catch
                        {
                        }
                    }

                    if (context.Waitable)
                    {
                        context.exception_ = exception;
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnNewOrderSingleNewReport(ClientSession session, NewOrderSingleClientContext NewOrderSingleClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                try
                {
                    NewOrderAsyncContext context = (NewOrderAsyncContext)NewOrderSingleClientContext;

                    try
                    {
                        TickTrader.FDK.Common.ExecutionReport result = new Common.ExecutionReport();
                        result.Last = false;

                        FillExecutionReport(result, message);

                        if (client_.NewOrderResultEvent != null)
                        {
                            try
                            {
                                client_.NewOrderResultEvent(client_, context.Data, result);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.executionReportList_.Add(result);
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.NewOrderErrorEvent != null)
                        {
                            try
                            {
                                client_.NewOrderErrorEvent(client_, context.Data, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.exception_ = exception;
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnNewOrderSingleCalculatedReport(ClientSession session, NewOrderSingleClientContext NewOrderSingleClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                try
                {
                    NewOrderAsyncContext context = (NewOrderAsyncContext) NewOrderSingleClientContext;

                    try
                    {
                        TickTrader.FDK.Common.ExecutionReport result = new Common.ExecutionReport();
                        result.Last = true;

                        FillExecutionReport(result, message);

                        if (client_.NewOrderResultEvent != null)
                        {
                            try
                            {
                                client_.NewOrderResultEvent(client_, context.Data, result);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.executionReportList_.Add(result);
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.NewOrderErrorEvent != null)
                        {
                            try
                            {
                                client_.NewOrderErrorEvent(client_, context.Data, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.exception_ = exception;
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnNewOrderSingleRejectReport(ClientSession session, NewOrderSingleClientContext NewOrderSingleClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                try
                {
                    NewOrderAsyncContext context = (NewOrderAsyncContext)NewOrderSingleClientContext;

                    TickTrader.FDK.Common.ExecutionReport result = new Common.ExecutionReport();
                    result.Last = true;

                    FillExecutionReport(result, message);

                    ExecutionException exception = new ExecutionException(result);

                    if (client_.NewOrderErrorEvent != null)
                    {
                        try
                        {
                            client_.NewOrderErrorEvent(client_, context.Data, exception);
                        }
                        catch
                        {
                        }
                    }

                    if (context.Waitable)
                    {
                        context.exception_ = exception;
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnOrderCancelReplacePendingReplaceReport(ClientSession session, OrderCancelReplaceRequestClientContext OrderCancelReplaceRequestClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                try
                {
                    ReplaceOrderAsyncContext context = (ReplaceOrderAsyncContext)OrderCancelReplaceRequestClientContext;

                    try
                    {
                        TickTrader.FDK.Common.ExecutionReport result = new Common.ExecutionReport();
                        result.Last = false;

                        FillExecutionReport(result, message);

                        if (client_.ReplaceOrderResultEvent != null)
                        {
                            try
                            {
                                client_.ReplaceOrderResultEvent(client_, context.Data, result);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.executionReportList_.Add(result);
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.ReplaceOrderErrorEvent != null)
                        {
                            try
                            {
                                client_.ReplaceOrderErrorEvent(client_, context.Data, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.exception_ = exception;
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnOrderCancelReplaceReplacedReport(ClientSession session, OrderCancelReplaceRequestClientContext OrderCancelReplaceRequestClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                try
                {
                    ReplaceOrderAsyncContext context = (ReplaceOrderAsyncContext)OrderCancelReplaceRequestClientContext;

                    try
                    {
                        TickTrader.FDK.Common.ExecutionReport result = new Common.ExecutionReport();
                        result.Last = true;

                        FillExecutionReport(result, message);

                        if (client_.ReplaceOrderResultEvent != null)
                        {
                            try
                            {
                                client_.ReplaceOrderResultEvent(client_, context.Data, result);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.executionReportList_.Add(result);
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.ReplaceOrderErrorEvent != null)
                        {
                            try
                            {
                                client_.ReplaceOrderErrorEvent(client_, context.Data, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.exception_ = exception;
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnOrderCancelReplaceRejectReport(ClientSession session, OrderCancelReplaceRequestClientContext OrderCancelReplaceRequestClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                try
                {
                    ReplaceOrderAsyncContext context = (ReplaceOrderAsyncContext)OrderCancelReplaceRequestClientContext;

                    TickTrader.FDK.Common.ExecutionReport result = new Common.ExecutionReport();
                    result.Last = true;

                    FillExecutionReport(result, message);

                    ExecutionException exception = new ExecutionException(result);

                    if (client_.ReplaceOrderErrorEvent != null)
                    {
                        try
                        {
                            client_.ReplaceOrderErrorEvent(client_, context.Data, exception);
                        }
                        catch
                        {
                        }
                    }

                    if (context.Waitable)
                    {
                        context.exception_ = exception;
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnOrderCancelPendingCancelReport(ClientSession session, OrderCancelRequestClientContext OrderCancelRequestClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                try
                {
                    CancelOrderAsyncContext context = (CancelOrderAsyncContext)OrderCancelRequestClientContext;

                    try
                    {
                        TickTrader.FDK.Common.ExecutionReport result = new Common.ExecutionReport();
                        result.Last = false;

                        FillExecutionReport(result, message);

                        if (client_.CancelOrderResultEvent != null)
                        {
                            try
                            {
                                client_.CancelOrderResultEvent(client_, context.Data, result);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.executionReportList_.Add(result);
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.CancelOrderErrorEvent != null)
                        {
                            try
                            {
                                client_.CancelOrderErrorEvent(client_, context.Data, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.exception_ = exception;
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnOrderCancelCancelledReport(ClientSession session, OrderCancelRequestClientContext OrderCancelRequestClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                try
                {
                    CancelOrderAsyncContext context = (CancelOrderAsyncContext)OrderCancelRequestClientContext;

                    try
                    {
                        TickTrader.FDK.Common.ExecutionReport result = new Common.ExecutionReport();
                        result.Last = true;

                        FillExecutionReport(result, message);

                        if (client_.CancelOrderResultEvent != null)
                        {
                            try
                            {
                                client_.CancelOrderResultEvent(client_, context.Data, result);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.executionReportList_.Add(result);
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.CancelOrderErrorEvent != null)
                        {
                            try
                            {
                                client_.CancelOrderErrorEvent(client_, context.Data, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.exception_ = exception;
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnOrderCancelRejectReport(ClientSession session, OrderCancelRequestClientContext OrderCancelRequestClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                try
                {
                    CancelOrderAsyncContext context = (CancelOrderAsyncContext)OrderCancelRequestClientContext;

                    TickTrader.FDK.Common.ExecutionReport result = new Common.ExecutionReport();
                    result.Last = true;

                    FillExecutionReport(result, message);

                    ExecutionException exception = new ExecutionException(result);

                    if (client_.CancelOrderErrorEvent != null)
                    {
                        try
                        {
                            client_.CancelOrderErrorEvent(client_, context.Data, exception);
                        }
                        catch
                        {
                        }
                    }

                    if (context.Waitable)
                    {
                        context.exception_ = exception;
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnClosePositionPendingCloseReport(ClientSession session, ClosePositionRequestClientContext ClosePositionRequestClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                try
                {
                    ClosePositionAsyncContext context = (ClosePositionAsyncContext)ClosePositionRequestClientContext;

                    try
                    {
                        TickTrader.FDK.Common.ExecutionReport result = new Common.ExecutionReport();
                        result.Last = false;

                        FillExecutionReport(result, message);

                        if (client_.ClosePositionResultEvent != null)
                        {
                            try
                            {
                                client_.ClosePositionResultEvent(client_, context.Data, result);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.executionReportList_.Add(result);
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.ClosePositionErrorEvent != null)
                        {
                            try
                            {
                                client_.ClosePositionErrorEvent(client_, context.Data, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.exception_ = exception;
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnClosePositionFillReport(ClientSession session, ClosePositionRequestClientContext ClosePositionRequestClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                try
                {
                    ClosePositionAsyncContext context = (ClosePositionAsyncContext) ClosePositionRequestClientContext;

                    try
                    {
                        TickTrader.FDK.Common.ExecutionReport result = new Common.ExecutionReport();
                        result.Last = true;

                        FillExecutionReport(result, message);

                        if (client_.ClosePositionResultEvent != null)
                        {
                            try
                            {
                                client_.ClosePositionResultEvent(client_, context.Data, result);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.executionReportList_.Add(result);
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.ClosePositionErrorEvent != null)
                        {
                            try
                            {
                                client_.ClosePositionErrorEvent(client_, context.Data, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.exception_ = exception;
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnClosePositionRejectReport(ClientSession session, ClosePositionRequestClientContext ClosePositionRequestClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                try
                {
                    ClosePositionAsyncContext context = (ClosePositionAsyncContext)ClosePositionRequestClientContext;

                    TickTrader.FDK.Common.ExecutionReport result = new Common.ExecutionReport();
                    result.Last = true;

                    FillExecutionReport(result, message);

                    ExecutionException exception = new ExecutionException(result);

                    if (client_.ClosePositionErrorEvent != null)
                    {
                        try
                        {
                            client_.ClosePositionErrorEvent(client_, context.Data, exception);
                        }
                        catch
                        {
                        }
                    }

                    if (context.Waitable)
                    {
                        context.exception_ = exception;
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnClosePositionByFillReport1(ClientSession session, ClosePositionByRequestClientContext ClosePositionByRequestClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                try
                {
                    ClosePositionByAsyncContext context = (ClosePositionByAsyncContext)ClosePositionByRequestClientContext;

                    try
                    {
                        TickTrader.FDK.Common.ExecutionReport result = new Common.ExecutionReport();
                        result.Last = false;

                        FillExecutionReport(result, message);

                        if (client_.ClosePositionByResultEvent != null)
                        {
                            try
                            {
                                client_.ClosePositionByResultEvent(client_, context.Data, result);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.executionReportList_.Add(result);
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.ClosePositionByErrorEvent != null)
                        {
                            try
                            {
                                client_.ClosePositionByErrorEvent(client_, context.Data, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.exception_ = exception;
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnClosePositionByFillReport2(ClientSession session, ClosePositionByRequestClientContext ClosePositionByRequestClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                try
                {
                    ClosePositionByAsyncContext context = (ClosePositionByAsyncContext)ClosePositionByRequestClientContext;

                    try
                    {
                        TickTrader.FDK.Common.ExecutionReport result = new Common.ExecutionReport();
                        result.Last = true;

                        FillExecutionReport(result, message);

                        if (client_.ClosePositionByResultEvent != null)
                        {
                            try
                            {
                                client_.ClosePositionByResultEvent(client_, context.Data, result);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.executionReportList_.Add(result);
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.ClosePositionByErrorEvent != null)
                        {
                            try
                            {
                                client_.ClosePositionByErrorEvent(client_, context.Data, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.exception_ = exception;
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnClosePositionByRejectReport1(ClientSession session, ClosePositionByRequestClientContext ClosePositionByRequestClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                try
                {
                    ClosePositionByAsyncContext context = (ClosePositionByAsyncContext)ClosePositionByRequestClientContext;

                    TickTrader.FDK.Common.ExecutionReport result = new Common.ExecutionReport();
                    result.Last = false;

                    FillExecutionReport(result, message);

                    ExecutionException exception = new ExecutionException(result);

                    if (client_.ClosePositionByErrorEvent != null)
                    {
                        try
                        {
                            client_.ClosePositionByErrorEvent(client_, context.Data, exception);
                        }
                        catch
                        {
                        }
                    }

                    if (context.Waitable)
                    {
                        context.exception_ = exception;
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnClosePositionByRejectReport2(ClientSession session, ClosePositionByRequestClientContext ClosePositionByRequestClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                try
                {
                    ClosePositionByAsyncContext context = (ClosePositionByAsyncContext) ClosePositionByRequestClientContext;

                    TickTrader.FDK.Common.ExecutionReport result = new Common.ExecutionReport();
                    result.Last = true;

                    FillExecutionReport(result, message);

                    ExecutionException exception = new ExecutionException(result);

                    if (client_.ClosePositionByErrorEvent != null)
                    {
                        try
                        {
                            client_.ClosePositionByErrorEvent(client_, context.Data, exception);
                        }
                        catch
                        {
                        }
                    }

                    if (context.Waitable)
                    {
                        context.exception_ = exception;
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnExecutionReport(ClientSession session, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                try
                {
                    TickTrader.FDK.Common.ExecutionReport result = new Common.ExecutionReport();

                    FillExecutionReport(result, message);
                                 
                    if (client_.OrderUpdateEvent != null)
                    {
                        try
                        {
                            client_.OrderUpdateEvent(client_, result);
                        }
                        catch
                        {
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnPositionReport(ClientSession session, PositionReport message)
            {
                try
                {
                    SoftFX.Net.OrderEntry.Position reportPosition = message.Position;
                    TickTrader.FDK.Common.Position resultPosition = new TickTrader.FDK.Common.Position();

                    resultPosition.Symbol = reportPosition.SymbolId;
                    resultPosition.Commission = reportPosition.Commission;
                    resultPosition.AgentCommission = reportPosition.AgentCommission;
                    resultPosition.Swap = reportPosition.Swap;

                    PosType reportPosType = reportPosition.Type;

                    if (reportPosType == PosType.Long)
                    {
                        resultPosition.BuyAmount = reportPosition.Qty;
                        resultPosition.BuyPrice = reportPosition.Price;
                        resultPosition.SellAmount = 0;
                        resultPosition.SellPrice = 0;
                    }
                    else if (reportPosType == PosType.Short)
                    {
                        resultPosition.BuyAmount = 0;
                        resultPosition.BuyPrice = 0;
                        resultPosition.SellAmount = reportPosition.Qty;
                        resultPosition.SellPrice = reportPosition.Price;
                    }
                    else
                        throw new Exception("Invalid position type : " + reportPosType);

                    if (client_.PositionUpdateEvent != null)
                    {
                        try
                        {
                            client_.PositionUpdateEvent(client_, resultPosition);
                        }
                        catch
                        {
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnAccountInfoUpdate(ClientSession session, AccountInfoUpdate message)
            {
                try
                {
                    TickTrader.FDK.Common.AccountInfo resultAccountInfo = new TickTrader.FDK.Common.AccountInfo();

                    FillAccountInfo(resultAccountInfo, message.AccountInfo);

                    if (client_.AccountInfoUpdateEvent != null)
                    {
                        try
                        {
                            client_.AccountInfoUpdateEvent(client_, resultAccountInfo);
                        }
                        catch
                        {
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnTradingSessionStatusUpdate(ClientSession session, TradingSessionStatusUpdate message)
            {
                try
                {
                    TickTrader.FDK.Common.SessionInfo resultStatusInfo = new TickTrader.FDK.Common.SessionInfo();
                    SoftFX.Net.OrderEntry.TradingSessionStatusInfo reportStatusInfo = message.StatusInfo;

                    resultStatusInfo.Status = GetSessionStatus(reportStatusInfo.Status);
                    resultStatusInfo.StartTime = reportStatusInfo.StartTime;
                    resultStatusInfo.EndTime = reportStatusInfo.EndTime;
                    resultStatusInfo.OpenTime = reportStatusInfo.OpenTime;
                    resultStatusInfo.CloseTime = reportStatusInfo.CloseTime;

                    TradingSessionStatusGroupArray reportGroups = reportStatusInfo.Groups;
                    int count = reportGroups.Length;
                    StatusGroupInfo[] resultGroups = new StatusGroupInfo[count];

                    for (int index = 0; index < count; ++index)
                    {
                        TradingSessionStatusGroup reportGroup = reportGroups[index];

                        StatusGroupInfo resultGroup = new StatusGroupInfo();
                        resultGroup.StatusGroupId = reportGroup.Id;
                        resultGroup.Status = GetSessionStatus(reportGroup.Status);
                        resultGroup.StartTime = reportGroup.StartTime;
                        resultGroup.EndTime = reportGroup.EndTime;
                        resultGroup.OpenTime = reportGroup.OpenTime;
                        resultGroup.CloseTime = reportGroup.CloseTime;

                        resultGroups[index] = resultGroup;
                    }

                    resultStatusInfo.StatusGroups = resultGroups;

                    if (client_.SessionInfoUpdateEvent != null)
                    {
                        try
                        {
                            client_.SessionInfoUpdateEvent(client_, resultStatusInfo);
                        }
                        catch
                        {
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnBalanceUpdate(ClientSession session, BalanceUpdate update)
            {
                try
                {
                    TickTrader.FDK.Common.BalanceOperation result = new TickTrader.FDK.Common.BalanceOperation();

                    SoftFX.Net.OrderEntry.Balance updateBalance = update.Balance;
                    result.TransactionCurrency = updateBalance.CurrId;
                    result.TransactionAmount = updateBalance.Move.Value;
                    result.Balance = updateBalance.Total;

                    if (client_.BalanceUpdateEvent != null)
                    {
                        try
                        {
                            client_.BalanceUpdateEvent(client_, result);
                        }
                        catch
                        {
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnNotification(ClientSession session, SoftFX.Net.OrderEntry.Notification message)
            {
                try
                {
                    TickTrader.FDK.Common.Notification result = new TickTrader.FDK.Common.Notification();
                    result.Id = message.Id;
                    result.Type = GetNotificationType(message.Type);
                    result.Severity = GetNotificationSeverity(message.Severity);
                    result.Message = message.Text;

                    if (client_.NotificationEvent != null)
                    {
                        try
                        {
                            client_.NotificationEvent(client_, result);
                        }
                        catch
                        {
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnLogout(ClientSession session, Logout message)
            {
                try
                {
                    LogoutInfo result = new LogoutInfo();
                    result.Reason = GetLogoutReason(message.Reason);
                    result.Message = message.Text;

                    if (client_.LogoutEvent != null)
                    {
                        try
                        {
                            client_.LogoutEvent(client_, result);
                        }
                        catch
                        {
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            void FillAccountInfo(TickTrader.FDK.Common.AccountInfo resultAccountInfo, SoftFX.Net.OrderEntry.AccountInfo reportAccountInfo)
            {
                resultAccountInfo.AccountId = reportAccountInfo.Id.ToString();
                resultAccountInfo.Type = GetAccountType(reportAccountInfo.Type);
                resultAccountInfo.Name = reportAccountInfo.Name;
                resultAccountInfo.FirtName = reportAccountInfo.FirstName;
                resultAccountInfo.LastName = reportAccountInfo.LastName;
                resultAccountInfo.Phone = reportAccountInfo.Phone;
                resultAccountInfo.Country = reportAccountInfo.Country;
                resultAccountInfo.City = reportAccountInfo.City;
                resultAccountInfo.Address = reportAccountInfo.Address;
                resultAccountInfo.ZipCode = reportAccountInfo.ZipCode;
                resultAccountInfo.Comment = reportAccountInfo.Description;
                resultAccountInfo.Email = reportAccountInfo.Email;                
                resultAccountInfo.RegistredDate = reportAccountInfo.RegistDate;                

                BalanceNull reportBalance = reportAccountInfo.Balance;

                if (reportBalance.HasValue)
                {
                    resultAccountInfo.Currency = reportBalance.CurrId;
                    resultAccountInfo.Balance = reportBalance.Total;
                }
                else
                {
                    resultAccountInfo.Currency = "";     // TODO: something in the orderEntry calculator crashes if null
                    resultAccountInfo.Balance = 0;
                }

                resultAccountInfo.Leverage = reportAccountInfo.Leverage;
                resultAccountInfo.Margin = reportAccountInfo.Margin;
                resultAccountInfo.Equity = reportAccountInfo.Equity;
                resultAccountInfo.MarginCallLevel = reportAccountInfo.MarginCallLevel;
                resultAccountInfo.StopOutLevel = reportAccountInfo.StopOutLevel;

                AccountFlags flags = reportAccountInfo.Flags;
                if ((flags & AccountFlags.Valid) != 0)
                    resultAccountInfo.IsValid = true;
                if ((flags & AccountFlags.Blocked) != 0)
                    resultAccountInfo.IsBlocked = true;
                if ((flags & AccountFlags.Investor) != 0)
                    resultAccountInfo.IsReadOnly = true;
                if ((flags & AccountFlags.RestWsApiEnabled) != 0)
                    resultAccountInfo.IsWebApiEnabled = true;

                AssetArray reportAssets = reportAccountInfo.Assets;

                int count = reportAssets.Length;
                AssetInfo[] resultAssets = new AssetInfo[count];

                for (int index = 0; index < count; ++index)
                {
                    Asset reportAsset = reportAssets[index];

                    AssetInfo resultAsset = new AssetInfo();
                    resultAsset.Currency = reportAsset.CurrId;
                    resultAsset.LockedAmount = reportAsset.Locked;
                    resultAsset.Balance = reportAsset.Total;

                    resultAssets[index] = resultAsset;
                }

                resultAccountInfo.Assets = resultAssets;
            }

            void FillExecutionReport(TickTrader.FDK.Common.ExecutionReport result, SoftFX.Net.OrderEntry.ExecutionReport report)
            {
                result.ExecutionType = GetExecutionType(report.ExecType);
                result.ClientOrderId = report.ClOrdId;
                result.OrigClientOrderId = report.OrigClOrdId;

                if (report.OrderId.HasValue)
                {
                    result.OrderId = report.OrderId.Value.ToString();
                }
                else
                    result.OrderId = null;

                result.Symbol = report.SymbolId;
                result.OrderType = GetOrderType(report.Type);
                result.OrderSide = GetOrderSide(report.Side);

                if (report.TimeInForce.HasValue)
                {
                    result.OrderTimeInForce = GetOrderTimeInForce(report.TimeInForce.Value);
                }
                else
                    result.OrderTimeInForce = null;

                result.InitialVolume = report.Qty;
                result.MaxVisibleVolume = report.MaxVisibleQty;
                result.Price = report.Price;
                result.StopPrice = report.StopPrice;
                result.Expiration = report.ExpireTime;
                result.TakeProfit = report.TakeProfit;
                result.StopLoss = report.StopLoss;
                result.MarketWithSlippage = (report.Flags & OrderFlags.Slippage) != 0;
                result.OrderStatus = GetOrderStatus(report.Status);
                result.InitialOrderType = GetOrderType(report.ReqType);
                result.InitialVolume = report.ReqQty;
                result.InitialPrice = report.ReqPrice;
                result.ExecutedVolume = report.CumQty;                
                result.LeavesVolume = report.LeavesQty;
                result.TradeAmount = report.LastQty;
                result.TradePrice = report.LastPrice;
                result.Commission = report.Commission;
                result.AgentCommission = report.AgentCommission;
                result.ReducedOpenCommission = (report.CommissionFlags & OrderCommissionFlags.OpenReduced) != 0;
                result.ReducedCloseCommission = (report.CommissionFlags & OrderCommissionFlags.CloseReduced) != 0;
                result.Swap = report.Swap;                
                result.AveragePrice = report.AvgPrice;                
                result.Created = report.Created;
                result.Modified = report.Modified;

                if (report.RejectReason.HasValue)
                {
                    result.RejectReason = GetRejectReason(report.RejectReason.Value);
                }
                else
                    result.RejectReason = TickTrader.FDK.Common.RejectReason.None;

                result.Text = report.Text;
                result.Comment = report.Comment;
                result.Tag = report.Tag;
                result.Magic = report.Magic;

                BalanceNull reportBalance = report.Balance;

                if (reportBalance.HasValue)
                {
                    result.Balance = reportBalance.Total;
                    result.BalanceTradeAmount = reportBalance.Move;
                }
                else
                {
                    result.Balance = null;
                    result.BalanceTradeAmount = null;
                }

                SoftFX.Net.OrderEntry.AssetNull reportAsset1 = report.Asset1;
                SoftFX.Net.OrderEntry.AssetNull reportAsset2 = report.Asset2;

                if (reportAsset1.HasValue && reportAsset2.HasValue)
                {
                    TickTrader.FDK.Common.AssetInfo resultAsset1 = new AssetInfo();
                    resultAsset1.Currency = reportAsset1.CurrId;
                    resultAsset1.TradeAmount = reportAsset1.Move.Value;
                    resultAsset1.LockedAmount = reportAsset1.Locked;
                    resultAsset1.Balance = reportAsset1.Total;

                    TickTrader.FDK.Common.AssetInfo resultAsset2 = new AssetInfo();
                    resultAsset2.Currency = reportAsset2.CurrId;
                    resultAsset2.TradeAmount = reportAsset2.Move.Value;
                    resultAsset2.LockedAmount = reportAsset2.Locked;
                    resultAsset2.Balance = reportAsset2.Total;

                    result.Assets = new TickTrader.FDK.Common.AssetInfo[]
                    {
                        resultAsset1,
                        resultAsset2
                    };
                }
                else if (!reportAsset1.HasValue && !reportAsset2.HasValue)
                {
                    result.Assets = new TickTrader.FDK.Common.AssetInfo[0];   // TODO: or null ?
                }
                else
                    throw new Exception("Invalid assets");
            }

            TickTrader.FDK.Common.LogoutReason GetLogoutReason(SoftFX.Net.OrderEntry.LoginRejectReason reason)
            {
                switch (reason)
                {
                    case SoftFX.Net.OrderEntry.LoginRejectReason.IncorrectCredentials:
                        return TickTrader.FDK.Common.LogoutReason.InvalidCredentials;

                    case SoftFX.Net.OrderEntry.LoginRejectReason.ThrottlingLimits:
                        return TickTrader.FDK.Common.LogoutReason.Unknown;

                    case SoftFX.Net.OrderEntry.LoginRejectReason.BlockedLogin:
                        return TickTrader.FDK.Common.LogoutReason.BlockedAccount;

                    case SoftFX.Net.OrderEntry.LoginRejectReason.InternalServerError:
                        return TickTrader.FDK.Common.LogoutReason.ServerError;

                    case SoftFX.Net.OrderEntry.LoginRejectReason.Other:
                        return TickTrader.FDK.Common.LogoutReason.Unknown;

                    default:
                        throw new Exception("Invalid login reject reason : " + reason);
                }
            }

            TickTrader.FDK.Common.LogoutReason GetLogoutReason(SoftFX.Net.OrderEntry.LogoutReason reason)
            {
                switch (reason)
                {
                    case SoftFX.Net.OrderEntry.LogoutReason.ClientLogout:
                        return TickTrader.FDK.Common.LogoutReason.ClientInitiated;

                    case SoftFX.Net.OrderEntry.LogoutReason.ServerLogout:
                        return TickTrader.FDK.Common.LogoutReason.ServerLogout;

                    case SoftFX.Net.OrderEntry.LogoutReason.DeletedLogin:
                        return TickTrader.FDK.Common.LogoutReason.LoginDeleted;

                    case SoftFX.Net.OrderEntry.LogoutReason.InternalServerError:
                        return TickTrader.FDK.Common.LogoutReason.ServerError;

                    case SoftFX.Net.OrderEntry.LogoutReason.BlockedLogin:
                        return TickTrader.FDK.Common.LogoutReason.BlockedAccount;

                    case SoftFX.Net.OrderEntry.LogoutReason.Other:
                        return TickTrader.FDK.Common.LogoutReason.Unknown;

                    default:
                        throw new Exception("Invalid logout reason : " + reason);
                }
            }

            TickTrader.FDK.Common.RejectReason GetRejectReason(SoftFX.Net.OrderEntry.RejectReason reason)
            {
                switch (reason)
                {
                    case SoftFX.Net.OrderEntry.RejectReason.ThrottlingLimits:
                        return Common.RejectReason.ThrottlingLimits;

                    case SoftFX.Net.OrderEntry.RejectReason.RequestCancelled:
                        return Common.RejectReason.RequestCancelled;

                    case SoftFX.Net.OrderEntry.RejectReason.InternalServerError:
                        return Common.RejectReason.InternalServerError;

                    case SoftFX.Net.OrderEntry.RejectReason.Other:
                        return Common.RejectReason.Other;

                    default:
                        throw new Exception("Invalid reject reason : " + reason);
                }
            }

            TickTrader.FDK.Common.AccountType GetAccountType(SoftFX.Net.OrderEntry.AccountType type)
            {
                switch (type)
                {
                    case SoftFX.Net.OrderEntry.AccountType.Gross:
                        return TickTrader.FDK.Common.AccountType.Gross;

                    case SoftFX.Net.OrderEntry.AccountType.Net:
                        return TickTrader.FDK.Common.AccountType.Net;

                    case SoftFX.Net.OrderEntry.AccountType.Cash:
                        return TickTrader.FDK.Common.AccountType.Cash;

                    default:
                        throw new Exception("Invalid account type : " + type);
                }
            }

            TickTrader.FDK.Common.SessionStatus GetSessionStatus(SoftFX.Net.OrderEntry.TradingSessionStatus status)
            {
                switch (status)
                {
                    case SoftFX.Net.OrderEntry.TradingSessionStatus.Close:
                        return TickTrader.FDK.Common.SessionStatus.Closed;

                    case SoftFX.Net.OrderEntry.TradingSessionStatus.Open:
                        return TickTrader.FDK.Common.SessionStatus.Open;

                    default:
                        throw new Exception("Invalid trading session status : " + status);
                }
            }

            TickTrader.FDK.Common.OrderStatus GetOrderStatus(SoftFX.Net.OrderEntry.OrderStatus status)
            {
                switch (status)
                {
                    case SoftFX.Net.OrderEntry.OrderStatus.New:
                        return TickTrader.FDK.Common.OrderStatus.New;

                    case SoftFX.Net.OrderEntry.OrderStatus.PartiallyFilled:
                        return TickTrader.FDK.Common.OrderStatus.PartiallyFilled;

                    case SoftFX.Net.OrderEntry.OrderStatus.Filled:
                        return TickTrader.FDK.Common.OrderStatus.Filled;

                    case SoftFX.Net.OrderEntry.OrderStatus.Cancelled:
                        return TickTrader.FDK.Common.OrderStatus.Canceled;

                    case SoftFX.Net.OrderEntry.OrderStatus.PendingCancel:
                        return TickTrader.FDK.Common.OrderStatus.PendingCancel;

                    case SoftFX.Net.OrderEntry.OrderStatus.Rejected:
                        return TickTrader.FDK.Common.OrderStatus.Rejected;

                    case SoftFX.Net.OrderEntry.OrderStatus.Calculated:
                        return TickTrader.FDK.Common.OrderStatus.Calculated;

                    case SoftFX.Net.OrderEntry.OrderStatus.Expired:
                        return TickTrader.FDK.Common.OrderStatus.Expired;

                    case SoftFX.Net.OrderEntry.OrderStatus.PendingReplace:
                        return TickTrader.FDK.Common.OrderStatus.PendingReplace;

                    case SoftFX.Net.OrderEntry.OrderStatus.PendingClose:
                        return TickTrader.FDK.Common.OrderStatus.PendingClose;

                    case SoftFX.Net.OrderEntry.OrderStatus.Activated:
                        return TickTrader.FDK.Common.OrderStatus.Activated;

                    default:
                        throw new Exception("Invalid order status : " + status);
                }
            }

            TickTrader.FDK.Common.OrderType GetOrderType(SoftFX.Net.OrderEntry.OrderType type)
            {
                switch (type)
                {
                    case SoftFX.Net.OrderEntry.OrderType.Market:
                        return TickTrader.FDK.Common.OrderType.Market;

                    case SoftFX.Net.OrderEntry.OrderType.Limit:
                        return TickTrader.FDK.Common.OrderType.Limit;

                    case SoftFX.Net.OrderEntry.OrderType.Stop:
                        return TickTrader.FDK.Common.OrderType.Stop;

                    case SoftFX.Net.OrderEntry.OrderType.Position:
                        return TickTrader.FDK.Common.OrderType.Position;

                    case SoftFX.Net.OrderEntry.OrderType.StopLimit:
                        return TickTrader.FDK.Common.OrderType.StopLimit;

                    default:
                        throw new Exception("Invalid order type : " + type);
                }
            }

            TickTrader.FDK.Common.OrderSide GetOrderSide(SoftFX.Net.OrderEntry.OrderSide side)
            {
                switch (side)
                {
                    case SoftFX.Net.OrderEntry.OrderSide.Buy:
                        return TickTrader.FDK.Common.OrderSide.Buy;

                    case SoftFX.Net.OrderEntry.OrderSide.Sell:
                        return TickTrader.FDK.Common.OrderSide.Sell;

                    default:
                        throw new Exception("Invalid order side : " + side);
                }
            }

            TickTrader.FDK.Common.OrderTimeInForce GetOrderTimeInForce(SoftFX.Net.OrderEntry.OrderTimeInForce timeInForce)
            {
                switch (timeInForce)
                {
                    case SoftFX.Net.OrderEntry.OrderTimeInForce.GoodTillCancel:
                        return TickTrader.FDK.Common.OrderTimeInForce.GoodTillCancel;

                    case SoftFX.Net.OrderEntry.OrderTimeInForce.ImmediateOrCancel:
                        return TickTrader.FDK.Common.OrderTimeInForce.ImmediateOrCancel;

                    case SoftFX.Net.OrderEntry.OrderTimeInForce.GoodTillDate:
                        return TickTrader.FDK.Common.OrderTimeInForce.GoodTillDate;

                    default:
                        throw new Exception("Invalid order time in force : " + timeInForce);
                }
            }

            TickTrader.FDK.Common.ExecutionType GetExecutionType(SoftFX.Net.OrderEntry.ExecutionType type)
            {
                switch (type)
                {
                    case SoftFX.Net.OrderEntry.ExecutionType.New:
                        return TickTrader.FDK.Common.ExecutionType.New;

                    case SoftFX.Net.OrderEntry.ExecutionType.Fill:
                        return TickTrader.FDK.Common.ExecutionType.Trade;

                    case SoftFX.Net.OrderEntry.ExecutionType.PartialFill:
                        return TickTrader.FDK.Common.ExecutionType.Trade;

                    case SoftFX.Net.OrderEntry.ExecutionType.Cancelled:
                        return TickTrader.FDK.Common.ExecutionType.Canceled;

                    case SoftFX.Net.OrderEntry.ExecutionType.PendingCancel:
                        return TickTrader.FDK.Common.ExecutionType.PendingCancel;

                    case SoftFX.Net.OrderEntry.ExecutionType.Rejected:
                        return TickTrader.FDK.Common.ExecutionType.Rejected;

                    case SoftFX.Net.OrderEntry.ExecutionType.Calculated:
                        return TickTrader.FDK.Common.ExecutionType.Calculated;

                    case SoftFX.Net.OrderEntry.ExecutionType.Expired:
                        return TickTrader.FDK.Common.ExecutionType.Expired;

                    case SoftFX.Net.OrderEntry.ExecutionType.Replaced:
                        return TickTrader.FDK.Common.ExecutionType.Replace;

                    case SoftFX.Net.OrderEntry.ExecutionType.PendingReplace:
                        return TickTrader.FDK.Common.ExecutionType.PendingReplace;

                    case SoftFX.Net.OrderEntry.ExecutionType.PendingClose:
                        return TickTrader.FDK.Common.ExecutionType.PendingClose;

                    default:
                        throw new Exception("Invalid exec type : " + type);
                }
            }

            TickTrader.FDK.Common.RejectReason GetRejectReason(SoftFX.Net.OrderEntry.ExecutionRejectReason reason)
            {
                switch (reason)
                {
                    case SoftFX.Net.OrderEntry.ExecutionRejectReason.Dealer:
                        return TickTrader.FDK.Common.RejectReason.DealerReject;

                    case SoftFX.Net.OrderEntry.ExecutionRejectReason.DealerTimeout:
                        return TickTrader.FDK.Common.RejectReason.DealerReject;

                    case SoftFX.Net.OrderEntry.ExecutionRejectReason.UnknownSymbol:
                        return TickTrader.FDK.Common.RejectReason.UnknownSymbol;

                    case SoftFX.Net.OrderEntry.ExecutionRejectReason.LimitsExceeded:
                        return TickTrader.FDK.Common.RejectReason.OrderExceedsLImit;

                    case SoftFX.Net.OrderEntry.ExecutionRejectReason.OffQuotes:
                        return TickTrader.FDK.Common.RejectReason.OffQuotes;

                    case SoftFX.Net.OrderEntry.ExecutionRejectReason.UnknownOrder:
                        return TickTrader.FDK.Common.RejectReason.UnknownOrder;

                    case SoftFX.Net.OrderEntry.ExecutionRejectReason.DuplicateOrder:
                        return TickTrader.FDK.Common.RejectReason.DuplicateClientOrderId;

                    case SoftFX.Net.OrderEntry.ExecutionRejectReason.IncorrectCharacteristics:
                        return TickTrader.FDK.Common.RejectReason.InvalidTradeRecordParameters;

                    case SoftFX.Net.OrderEntry.ExecutionRejectReason.IncorrectQty:
                        return TickTrader.FDK.Common.RejectReason.IncorrectQuantity;

                    case SoftFX.Net.OrderEntry.ExecutionRejectReason.TooLate:
                        return TickTrader.FDK.Common.RejectReason.Other;

                    case SoftFX.Net.OrderEntry.ExecutionRejectReason.InternalServerError:
                        return TickTrader.FDK.Common.RejectReason.Other;

                    case SoftFX.Net.OrderEntry.ExecutionRejectReason.Other:
                        return TickTrader.FDK.Common.RejectReason.Other;

                    default:
                        throw new Exception("Invalid order reject reason : " + reason);
                }
            }

            TickTrader.FDK.Common.NotificationType GetNotificationType(SoftFX.Net.OrderEntry.NotificationType type)
            {
                switch (type)
                {
                    case SoftFX.Net.OrderEntry.NotificationType.MarginCall:
                        return TickTrader.FDK.Common.NotificationType.MarginCall;

                    case SoftFX.Net.OrderEntry.NotificationType.MarginCallRevocation:
                        return TickTrader.FDK.Common.NotificationType.MarginCallRevocation;

                    case SoftFX.Net.OrderEntry.NotificationType.StopOut:
                        return TickTrader.FDK.Common.NotificationType.StopOut;

                    case SoftFX.Net.OrderEntry.NotificationType.ConfigUpdate:
                        return TickTrader.FDK.Common.NotificationType.ConfigUpdated;

                    default:
                        throw new Exception("Invalid notification type : " + type);
                }
            }

            TickTrader.FDK.Common.NotificationSeverity GetNotificationSeverity(SoftFX.Net.OrderEntry.NotificationSeverity severity)
            {
                switch (severity)
                {
                    case SoftFX.Net.OrderEntry.NotificationSeverity.Info:
                        return TickTrader.FDK.Common.NotificationSeverity.Information;

                    case SoftFX.Net.OrderEntry.NotificationSeverity.Warning:
                        return TickTrader.FDK.Common.NotificationSeverity.Warning;

                    case SoftFX.Net.OrderEntry.NotificationSeverity.Error:
                        return TickTrader.FDK.Common.NotificationSeverity.Error;

                    default:
                        throw new Exception("Invalid notification severity : " + severity);
                }
            }

            OrderEntry client_;
        }

        #endregion
    }
}
