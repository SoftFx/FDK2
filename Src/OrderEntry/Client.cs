using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using SoftFX.Net.OrderEntry;
using TickTrader.FDK.Common;

namespace TickTrader.FDK.OrderEntry
{
    public class Client : IDisposable
    {
        #region Constructors

        public Client(string name) : this(name, 5040, true, "Logs", false)
        {
        }

        public Client(string name, int port, bool reconnect, string logDirectory, bool logMessages)
        {
            ClientSessionOptions options = new ClientSessionOptions(port);
            options.ConnectionType = SoftFX.Net.Core.ConnectionType.Secure;
            options.ServerCertificateName = "TickTraderManagerService";
            options.ReconnectMaxCount = reconnect ? -1 : 0;
            options.Log.Directory = logDirectory;
#if DEBUG
            options.Log.Events = true;
            options.Log.States = false;
            options.Log.Messages = true;
#else
            options.Log.Events = false;
            options.Log.States = false;
            options.Log.Messages = logMessages;
#endif
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
            DisconnectAsync(this, "Client disconnect");
            Join();

            GC.SuppressFinalize(this);
        }

        #endregion

        #region Connect / disconnect

        public delegate void ConnectDelegate(Client client, object data);
        public delegate void ConnectErrorDelegate(Client client, object data, string text);
        public delegate void DisconnectDelegate(Client client, object data, string text);

        public event ConnectDelegate ConnectEvent;
        public event ConnectErrorDelegate ConnectErrorEvent;
        public event DisconnectDelegate DisconnectEvent;

        public void Connect(string address, int timeout)
        {
            try
            {
                ConvertToSync(ConnectAsync(address), timeout);
            }
            catch (TimeoutException)
            {
                DisconnectAsync(this, "Connect timeout");
                Join();

                throw;
            }
        }

        public void ConnectAsync(object data, string address)
        {
            ConnectAsyncContext context = new ConnectAsyncContext();
            context.Data = data;

            ConnectInternal(context, address);
        }

        public Task ConnectAsync(string address)
        {
            ConnectAsyncContext context = new ConnectAsyncContext();
            context.taskCompletionSource_ = new TaskCompletionSource<object>();

            ConnectInternal(context, address);

            return context.taskCompletionSource_.Task;
        }

        void ConnectInternal(ConnectAsyncContext context, string address)
        {
            session_.Connect(context, address);
        }

        public void Disconnect(string text)
        {
            ConvertToSync(DisconnectAsync(text), -1);
        }

        public void DisconnectAsync(object data, string text)
        {
            DisconnectAsyncContext context = new DisconnectAsyncContext();
            context.Data = data;

            DisconnectInernal(context, text);
        }

        public Task DisconnectAsync(string text)
        {
            DisconnectAsyncContext context = new DisconnectAsyncContext();
            context.taskCompletionSource_ = new TaskCompletionSource<object>();

            DisconnectInernal(context, text);

            return context.taskCompletionSource_.Task;
        }

        void DisconnectInernal(DisconnectAsyncContext context, string text)
        {
            session_.Disconnect(context, text);
        }

        public void Join()
        {
            session_.Join();
        }

        #endregion

        #region Login / logout

        public delegate void LoginResultDelegate(Client client, object data);
        public delegate void LoginErrorDelegate(Client client, object data, string text);
        public delegate void TwoFactorLoginRequestDelegate(Client client, string message);
        public delegate void TwoFactorLoginResultDelegate(Client client, object data, DateTime expireTime);
        public delegate void TwoFactorLoginErrorDelegate(Client client, object data, string message);
        public delegate void TwoFactorLoginResumeDelegate(Client client, object data, DateTime expireTime);
        public delegate void LogoutResultDelegate(Client client, object data, LogoutInfo logoutInfo);
        public delegate void LogoutDelegate(Client client, LogoutInfo logoutInfo);

        public event LoginResultDelegate LoginResultEvent;
        public event LoginErrorDelegate LoginErrorEvent;
        public event TwoFactorLoginRequestDelegate TwoFactorLoginRequestEvent;
        public event TwoFactorLoginResultDelegate TwoFactorLoginResultEvent;
        public event TwoFactorLoginErrorDelegate TwoFactorLoginErrorEvent;
        public event TwoFactorLoginResumeDelegate TwoFactorLoginResumeEvent;
        public event LogoutResultDelegate LogoutResultEvent;
        public event LogoutDelegate LogoutEvent;

        public void Login(string username, string password, string deviceId, string appId, string sessionId, int timeout)
        {
            ConvertToSync(LoginAsync(username, password, deviceId, appId, sessionId), timeout);
        }

        public void LoginAsync(object data, string username, string password, string deviceId, string appId, string sessionId)
        {
            // Create a new async context
            var context = new LoginAsyncContext();
            context.Data = data;

            LoginInternal(context, username, password, deviceId, appId, sessionId);
        }

        public Task LoginAsync(string username, string password, string deviceId, string appId, string sessionId)
        {
            // Create a new async context
            var context = new LoginAsyncContext();
            context.taskCompletionSource_ = new TaskCompletionSource<object>();

            LoginInternal(context, username, password, deviceId, appId, sessionId);

            return context.taskCompletionSource_.Task;
        }

        void LoginInternal(LoginAsyncContext context, string username, string password, string deviceId, string appId, string sessionId)
        {
            if (string.IsNullOrEmpty(appId))
                appId = "FDK2";

            // Create a request
            var request = new LoginRequest(0)
            {
                Username = username,
                Password = password,
                DeviceId = deviceId,
                AppId = appId,
                SessionId = sessionId
            };

            // Send request to the server
            session_.SendLoginRequest(context, request);
        }

        public DateTime TwoFactorLoginResponse(string oneTimePassword, int timeout)
        {
            return ConvertToSync(TwoFactorLoginResponseAsync(oneTimePassword), timeout);
        }

        public void TwoFactorLoginResponseAsync(object data, string oneTimePassword)
        {
            // Create a new async context
            TwoFactorLoginResponseAsyncContext context = new TwoFactorLoginResponseAsyncContext();
            context.Data = data;

            TwoFactorLoginResponseInternal(context, oneTimePassword);
        }

        public Task<DateTime> TwoFactorLoginResponseAsync(string oneTimePassword)
        {
            // Create a new async context
            TwoFactorLoginResponseAsyncContext context = new TwoFactorLoginResponseAsyncContext();
            context.taskCompletionSource_ = new TaskCompletionSource<DateTime>();

            TwoFactorLoginResponseInternal(context, oneTimePassword);

            return context.taskCompletionSource_.Task;
        }

        void TwoFactorLoginResponseInternal(TwoFactorLoginResponseAsyncContext context, string oneTimePassword)
        {
            // Create a message
            var message = new TwoFactorLogin(0)
            {
                Reason = TwoFactorReason.ClientResponse,
                OneTimePassword = oneTimePassword
            };

            // Send message to the server
            session_.SendTwoFactorLoginResponse(context, message);
        }

        public DateTime TwoFactorLoginResume(int timeout)
        {
            return ConvertToSync(TwoFactorLoginResumeAsync(), timeout);
        }

        public void TwoFactorLoginResumeAsync(object data)
        {
            // Create a new async context
            TwoFactorLoginResumeAsyncContext context = new TwoFactorLoginResumeAsyncContext();
            context.Data = data;

            TwoFactorLoginResumeInternal(context);
        }

        public Task<DateTime> TwoFactorLoginResumeAsync()
        {
            // Create a new async context
            TwoFactorLoginResumeAsyncContext context = new TwoFactorLoginResumeAsyncContext();
            context.taskCompletionSource_ = new TaskCompletionSource<DateTime>();

            TwoFactorLoginResumeInternal(context);

            return context.taskCompletionSource_.Task;
        }

        void TwoFactorLoginResumeInternal(TwoFactorLoginResumeAsyncContext context)
        {
            // Create a message
            var message = new TwoFactorLogin(0)
            {
                Reason = TwoFactorReason.ClientResume
            };

            // Send message to the server
            session_.SendTwoFactorLoginResume(context, message);
        }

        public LogoutInfo Logout(string message, int timeout)
        {
            return ConvertToSync(LogoutAsync(message), timeout);
        }

        public void LogoutAsync(object data, string message)
        {
            // Create a new async context
            var context = new LogoutAsyncContext();
            context.Data = data;

            LogoutInternal(context, message);
        }

        public Task<LogoutInfo> LogoutAsync(string message)
        {
            // Create a new async context
            var context = new LogoutAsyncContext();
            context.taskCompletionSource_ = new TaskCompletionSource<LogoutInfo>();

            LogoutInternal(context, message);

            return context.taskCompletionSource_.Task;
        }

        void LogoutInternal(LogoutAsyncContext context, string message)
        {
            session_.Reconnect = false;

            // Create a request
            var request = new Logout(0)
            {
                Text = message
            };

            // Send request to the server
            session_.SendLogout(context, request);
        }

        #endregion

        #region Order Entry
                
        public delegate void TradeServerInfoResultDelegate(Client client, object data, TickTrader.FDK.Common.TradeServerInfo info);
        public delegate void TradeServerInfoErrorDelegate(Client client, object data, string message);
        public delegate void AccountInfoResultDelegate(Client client, object data, TickTrader.FDK.Common.AccountInfo info);
        public delegate void AccountInfoErrorDelegate(Client client, object data, string message);
        public delegate void SessionInfoResultDelegate(Client client, object data, TickTrader.FDK.Common.SessionInfo info);
        public delegate void SessionInfoErrorDelegate(Client client, object data, string message);
        public delegate void OrdersResultDelegate(Client client, object data, TickTrader.FDK.Common.ExecutionReport[] reports);
        public delegate void OrdersErrorDelegate(Client client, object data, string message);
        public delegate void PositionsResultDelegate(Client client, object data, TickTrader.FDK.Common.Position[] positions);
        public delegate void PositionsErrorDelegate(Client client, object data, string message);
        public delegate void NewOrderResultDelegate(Client client, object data, TickTrader.FDK.Common.ExecutionReport report);
        public delegate void NewOrderErrorDelegate(Client client, object data, TickTrader.FDK.Common.ExecutionReport report);
        public delegate void ReplaceOrderResultDelegate(Client client, object data, TickTrader.FDK.Common.ExecutionReport report);
        public delegate void ReplaceOrderErrorDelegate(Client client, object data, TickTrader.FDK.Common.ExecutionReport report);
        public delegate void CancelOrderResultDelegate(Client client, object data, TickTrader.FDK.Common.ExecutionReport report);
        public delegate void CancelOrderErrorDelegate(Client client, object data, TickTrader.FDK.Common.ExecutionReport report);
        public delegate void ClosePositionResultDelegate(Client client, object data, TickTrader.FDK.Common.ExecutionReport report);
        public delegate void ClosePositionErrorDelegate(Client client, object data, TickTrader.FDK.Common.ExecutionReport report);
        public delegate void ClosePositionByResultDelegate(Client client, object data, TickTrader.FDK.Common.ExecutionReport report);
        public delegate void ClosePositionByErrorDelegate(Client client, object data, TickTrader.FDK.Common.ExecutionReport report);
        public delegate void OrderUpdateDelegate(Client client, TickTrader.FDK.Common.ExecutionReport executionReport);
        public delegate void PositionUpdateDelegate(Client client, TickTrader.FDK.Common.Position position);
        public delegate void AccountInfoUpdateDelegate(Client client, TickTrader.FDK.Common.AccountInfo accountInfo);
        public delegate void SessionInfoUpdateDelegate(Client client, SessionInfo sessionInfo);
        public delegate void BalanceUpdateDelegate(Client client, BalanceOperation balanceOperation);
        public delegate void NotificationDelegate(Client client, TickTrader.FDK.Common.Notification notification);
                
        public event TradeServerInfoResultDelegate TradeServerInfoResultEvent;
        public event TradeServerInfoErrorDelegate TradeServerInfoErrorEvent;
        public event AccountInfoResultDelegate AccountInfoResultEvent;
        public event AccountInfoErrorDelegate AccountInfoErrorEvent;
        public event SessionInfoResultDelegate SessionInfoResultEvent;
        public event SessionInfoErrorDelegate SessionInfoErrorEvent;
        public event OrdersResultDelegate OrdersResultEvent;
        public event OrdersErrorDelegate OrdersErrorEvent;        
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
            return ConvertToSync(GetTradeServerInfoAsync(), timeout);
        }

        public void GetTradeServerInfoAsync(object data)
        {
            // Create a new async context
            var context = new TradeServerInfoAsyncContext();
            context.Data = data;

            GetTradeServerInfoInternal(context);
        }

        public Task<TickTrader.FDK.Common.TradeServerInfo> GetTradeServerInfoAsync()
        {
            // Create a new async context
            var context = new TradeServerInfoAsyncContext();
            context.taskCompletionSource_ = new TaskCompletionSource<TradeServerInfo>();

            GetTradeServerInfoInternal(context);

            return context.taskCompletionSource_.Task;
        }

        void GetTradeServerInfoInternal(TradeServerInfoAsyncContext context)
        {
            // Create a request
            var request = new TradeServerInfoRequest(0)
            {
                Id = Guid.NewGuid().ToString()
            };

            // Send request to the server
            session_.SendTradeServerInfoRequest(context, request);
        }

        public TickTrader.FDK.Common.AccountInfo GetAccountInfo(int timeout)
        {
            return ConvertToSync(GetAccountInfoAsync(), timeout);
        }

        public void GetAccountInfoAsync(object data)
        {
            // Create a new async context
            var context = new AccountInfoAsyncContext();
            context.Data = data;

            GetAccountInfoInternal(context);
        }

        public Task<TickTrader.FDK.Common.AccountInfo> GetAccountInfoAsync()
        {
            // Create a new async context
            var context = new AccountInfoAsyncContext();
            context.taskCompletionSource_ = new TaskCompletionSource<TickTrader.FDK.Common.AccountInfo>();

            GetAccountInfoInternal(context);

            return context.taskCompletionSource_.Task;
        }

        void GetAccountInfoInternal(AccountInfoAsyncContext context)
        {
            // Create a request
            var request = new AccountInfoRequest(0)
            {
                Id = Guid.NewGuid().ToString()
            };

            // Send request to the server
            session_.SendAccountInfoRequest(context, request);
        }

        public TickTrader.FDK.Common.SessionInfo GetSessionInfo(int timeout)
        {
            return ConvertToSync(GetSessionInfoAsync(), timeout);
        }

        public void GetSessionInfoAsync(object data)
        {
            // Create a new async context
            var context = new SessionInfoAsyncContext();
            context.Data = data;

            GetSessionInfoInternal(context);
        }

        public Task<TickTrader.FDK.Common.SessionInfo> GetSessionInfoAsync()
        {
            // Create a new async context
            var context = new SessionInfoAsyncContext();
            context.taskCompletionSource_ = new TaskCompletionSource<SessionInfo>();

            GetSessionInfoInternal(context);

            return context.taskCompletionSource_.Task;
        }

        void GetSessionInfoInternal(SessionInfoAsyncContext context)
        {
            // Create a request
            var request = new TradingSessionStatusRequest(0);
            request.Id = Guid.NewGuid().ToString();

            // Send request to the server
            session_.SendTradingSessionStatusRequest(context, request);
        }

        public TickTrader.FDK.Common.ExecutionReport[] GetOrders(int timeout)
        {
            return ConvertToSync(GetOrdersAsync(), timeout);
        }

        public void GetOrdersAsync(object data)
        {
            // Create a new async context
            var context = new OrdersAsyncContext();
            context.Data = data;

            GetOrdersInternal(context);
        }

        public Task<TickTrader.FDK.Common.ExecutionReport[]> GetOrdersAsync()
        {
            // Create a new async context
            var context = new OrdersAsyncContext();
            context.taskCompletionSource_ = new TaskCompletionSource<Common.ExecutionReport[]>();

            GetOrdersInternal(context);

            return context.taskCompletionSource_.Task;
        }

        void GetOrdersInternal(OrdersAsyncContext context)
        {
            // Create a request
            var request = new OrderMassStatusRequest(0);
            request.Id = Guid.NewGuid().ToString();
            request.Type = OrderMassStatusRequestType.All;

            // Send request to the server
            session_.SendOrderMassStatusRequest(context, request);
        }

        public TickTrader.FDK.Common.Position[] GetPositions(int timeout)
        {
            return ConvertToSync(GetPositionsAsync(), timeout);
        }

        public void GetPositionsAsync(object data)
        {
            // Create a new async context
            var context = new PositionsAsyncContext();
            context.Data = data;

            GetPositionsInternal(context);
        }

        public Task<TickTrader.FDK.Common.Position[]> GetPositionsAsync()
        {
            // Create a new async context
            var context = new PositionsAsyncContext();
            context.taskCompletionSource_ = new TaskCompletionSource<Common.Position[]>();

            GetPositionsInternal(context);

            return context.taskCompletionSource_.Task;
        }

        void GetPositionsInternal(PositionsAsyncContext context)
        {
            // Create a request
            var request = new PositionListRequest(0);
            request.Id = Guid.NewGuid().ToString();
            request.Type = PositionListRequestType.All;

            // Send request to the server
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
            return ConvertToSync
            (
                NewOrderAsync
                (
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
                ), 
                timeout
            );
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
            // Create a new async context
            var context = new NewOrderAsyncContext();
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

        public Task<TickTrader.FDK.Common.ExecutionReport[]> NewOrderAsync
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
            int? magic
        )
        {
            // Create a new async context
            var context = new NewOrderAsyncContext();
            context.taskCompletionSource_ = new TaskCompletionSource<Common.ExecutionReport[]>();
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

            return context.taskCompletionSource_.Task;
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
            // Create a request
            NewOrderSingle message = new NewOrderSingle(0);
            message.ClOrdId = clientOrderId;
            OrderAttributes attributes = message.Attributes;
            attributes.SymbolId = symbol;
            attributes.Type = Convert(type);            
            attributes.Side = Convert(side);
            attributes.Qty = qty;
            attributes.MaxVisibleQty = maxVisibleQty;
            attributes.Price = price;
            attributes.StopPrice = stopPrice;

            if (timeInForce.HasValue)
            {
                attributes.TimeInForce = Convert(timeInForce.Value);
            }
            else
                attributes.TimeInForce = null;

            attributes.ExpireTime = expireTime;
            attributes.StopLoss = stopLoss;
            attributes.TakeProfit = takeProfit;
            attributes.Comment = comment;
            attributes.Tag = tag;
            attributes.Magic = magic;

            // Send request to the server
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
            string comment,
            string tag,
            int? magic,
            int timeout
        )
        {
            return ConvertToSync
            (
                ReplaceOrderAsync
                (
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
                    comment,
                    tag,
                    magic
                ), 
                timeout
            );
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
            string comment,
            string tag,
            int? magic
        )
        {
            // Create a new async context
            var context = new ReplaceOrderAsyncContext();
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
                comment,
                tag,
                magic
            );
        }

        public Task<TickTrader.FDK.Common.ExecutionReport[]> ReplaceOrderAsync
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
            string comment,
            string tag,
            int? magic
        )
        {
            // Create a new async context
            var context = new ReplaceOrderAsyncContext();
            context.taskCompletionSource_ = new TaskCompletionSource<Common.ExecutionReport[]>();
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
                comment,
                tag,
                magic
            );

            return context.taskCompletionSource_.Task;
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
            string comment, 
            string tag, 
            int? magic
        )
        {
            // Create a request
            OrderCancelReplaceRequest message = new OrderCancelReplaceRequest(0);
            message.ClOrdId = clientOrderId;
            message.OrigClOrdId = origClientOrderId;

            if (orderId != null)
            {
                message.OrderId = long.Parse(orderId);
            }
            else
                message.OrderId = null;

            OrderAttributes attributes = message.Attributes;
            attributes.SymbolId = symbol;
            attributes.Type = Convert(type);            
            attributes.Side = Convert(side);
            attributes.Qty = qty;
            attributes.MaxVisibleQty = maxVisibleQty;
            attributes.Price = price;
            attributes.StopPrice = stopPrice;

            if (timeInForce.HasValue)
            {
                attributes.TimeInForce = Convert(timeInForce.Value);
            }
            else
                attributes.TimeInForce = null;

            attributes.ExpireTime = expireTime;
            attributes.StopLoss = stopLoss;
            attributes.TakeProfit = takeProfit;
            attributes.Comment = comment;
            attributes.Tag = tag;
            attributes.Magic = magic;

            // Send request to the server
            session_.SendOrderCancelReplaceRequest(context, message);
        }

        public TickTrader.FDK.Common.ExecutionReport[] CancelOrder(string clientOrderId, string origClientOrderId, string orderId, int timeout)
        {
            return ConvertToSync(CancelOrderAsync(clientOrderId, origClientOrderId, orderId), timeout);
        }

        public void CancelOrderAsync(object data, string clientOrderId, string origClientOrderId, string orderId)
        {
            // Create a new async context
            var context = new CancelOrderAsyncContext();
            context.Data = data;

            CancelOrderInternal(context, clientOrderId, origClientOrderId, orderId);
        }

        public Task<TickTrader.FDK.Common.ExecutionReport[]> CancelOrderAsync(string clientOrderId, string origClientOrderId, string orderId)
        {
            // Create a new async context
            var context = new CancelOrderAsyncContext();
            context.taskCompletionSource_ = new TaskCompletionSource<Common.ExecutionReport[]>();
            context.executionReportList_ = new List<Common.ExecutionReport>();

            CancelOrderInternal(context, clientOrderId, origClientOrderId, orderId);

            return context.taskCompletionSource_.Task;
        }

        void CancelOrderInternal(CancelOrderAsyncContext context, string clientOrderId, string origClientOrderId, string orderId)
        {
            // Create a request
            OrderCancelRequest message = new OrderCancelRequest(0);
            message.ClOrdId = clientOrderId;
            message.OrigClOrdId = origClientOrderId;

            if (orderId != null)
            {
                message.OrderId = long.Parse(orderId);
            }
            else
                message.OrderId = null;

            // Send request to the server
            session_.SendOrderCancelRequest(context, message);
        }

        public TickTrader.FDK.Common.ExecutionReport[] ClosePosition(string clientOrderId, string orderId, double? qty, int timeout)
        {
            return ConvertToSync(ClosePositionAsync(clientOrderId, orderId, qty), timeout);
        }

        public void ClosePositionAsync(object data, string clientOrderId, string orderId, double? qty)
        {
            // Create a new async context
            var context = new ClosePositionAsyncContext();
            context.Data = data;

            ClosePositionInternal(context, clientOrderId, orderId, qty);
        }

        public Task<TickTrader.FDK.Common.ExecutionReport[]> ClosePositionAsync(string clientOrderId, string orderId, double? qty)
        {
            // Create a new async context
            var context = new ClosePositionAsyncContext();
            context.taskCompletionSource_ = new TaskCompletionSource<Common.ExecutionReport[]>();
            context.executionReportList_ = new List<Common.ExecutionReport>();

            ClosePositionInternal(context, clientOrderId, orderId, qty);

            return context.taskCompletionSource_.Task;
        }

        void ClosePositionInternal(ClosePositionAsyncContext context, string clientOrderId, string orderId, double? qty)
        {
            // Create a request
            ClosePositionRequest message = new ClosePositionRequest(0);
            message.ClOrdId = clientOrderId;
            message.OrderId = long.Parse(orderId);
            message.Type = ClosePositionRequestType.Close;
            message.Qty = qty;

            // Send request to the server
            session_.SendClosePositionRequest(context, message);
        }

        public TickTrader.FDK.Common.ExecutionReport[] ClosePositionBy(string clientOrderId, string orderId, string byOrderId, int timeout)
        {
            return ConvertToSync(ClosePositionByAsync(clientOrderId, orderId, byOrderId), timeout);
        }

        public void ClosePositionByAsync(object data, string clientOrderId, string orderId, string byOrderId)
        {
            // Create a new async context
            var context = new ClosePositionByAsyncContext();
            context.Data = data;

            ClosePositionByInternal(context, clientOrderId, orderId, byOrderId);
        }
        

        public Task<TickTrader.FDK.Common.ExecutionReport[]> ClosePositionByAsync(string clientOrderId, string orderId, string byOrderId)
        {
            // Create a new async context
            var context = new ClosePositionByAsyncContext();
            context.taskCompletionSource_ = new TaskCompletionSource<Common.ExecutionReport[]>();
            context.executionReportList_ = new List<Common.ExecutionReport>();

            ClosePositionByInternal(context, clientOrderId, orderId, byOrderId);

            return context.taskCompletionSource_.Task;
        }

        void ClosePositionByInternal(ClosePositionByAsyncContext context, string clientOrderId, string orderId, string byOrderId)
        {
            // Create a request
            ClosePositionRequest message = new ClosePositionRequest(0);
            message.ClOrdId = clientOrderId;
            message.OrderId = long.Parse(orderId);
            message.Type = ClosePositionRequestType.CloseBy;
            message.ByOrderId = long.Parse(byOrderId);

            // Send request to the server
            session_.SendClosePositionByRequest(context, message);
        }

        #endregion

        #region Implementation

        SoftFX.Net.OrderEntry.OrderType Convert(TickTrader.FDK.Common.OrderType type)
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

        SoftFX.Net.OrderEntry.OrderSide Convert(TickTrader.FDK.Common.OrderSide side)
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

        SoftFX.Net.OrderEntry.OrderTimeInForce Convert(TickTrader.FDK.Common.OrderTimeInForce time)
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
            void SetDisconnectError(Exception exception);
        }

        class ConnectAsyncContext : ConnectClientContext
        {
            public ConnectAsyncContext() : base(false)
            {
            }

            public TaskCompletionSource<object> taskCompletionSource_;
        }

        class DisconnectAsyncContext : DisconnectClientContext
        {
            public DisconnectAsyncContext() : base(false)
            {
            }

            public TaskCompletionSource<object> taskCompletionSource_;
        }

        class LoginAsyncContext : LoginRequestClientContext, IAsyncContext
        {
            public LoginAsyncContext() : base(false)
            {
            }

            public void SetDisconnectError(Exception exception)
            {
                if (taskCompletionSource_ != null)
                    taskCompletionSource_.SetException(exception);
            }

            public TaskCompletionSource<object> taskCompletionSource_;
        }

        class TwoFactorLoginResponseAsyncContext : TwoFactorLoginResponseClientContext, IAsyncContext
        {
            public TwoFactorLoginResponseAsyncContext() : base(false)
            {
            }

            public void SetDisconnectError(Exception exception)
            {
                if (taskCompletionSource_ != null)
                    taskCompletionSource_.SetException(exception);
            }

            public TaskCompletionSource<DateTime> taskCompletionSource_;
        }

        class TwoFactorLoginResumeAsyncContext : TwoFactorLoginResumeClientContext, IAsyncContext
        {
            public TwoFactorLoginResumeAsyncContext() : base(false)
            {
            }

            public void SetDisconnectError(Exception exception)
            {
                if (taskCompletionSource_ != null)
                    taskCompletionSource_.SetException(exception);
            }

            public TaskCompletionSource<DateTime> taskCompletionSource_;
        }

        class LogoutAsyncContext : LogoutClientContext, IAsyncContext
        {
            public LogoutAsyncContext() : base(false)
            {
            }

            public void SetDisconnectError(Exception exception)
            {
                if (taskCompletionSource_ != null)
                    taskCompletionSource_.SetException(exception);
            }

            public TaskCompletionSource<LogoutInfo> taskCompletionSource_;
        }

        class TradeServerInfoAsyncContext : TradeServerInfoRequestClientContext, IAsyncContext
        {
            public TradeServerInfoAsyncContext() : base(false)
            {
            }

            public void SetDisconnectError(Exception exception)
            {
                if (taskCompletionSource_ != null)
                    taskCompletionSource_.SetException(exception);
            }
            
            public TaskCompletionSource<TradeServerInfo> taskCompletionSource_;
        }

        class AccountInfoAsyncContext : AccountInfoRequestClientContext, IAsyncContext
        {
            public AccountInfoAsyncContext() : base(false)
            {
            }

            public void SetDisconnectError(Exception exception)
            {
                if (taskCompletionSource_ != null)
                    taskCompletionSource_.SetException(exception);
            }

            public TaskCompletionSource<TickTrader.FDK.Common.AccountInfo> taskCompletionSource_;
        }

        class SessionInfoAsyncContext : TradingSessionStatusRequestClientContext, IAsyncContext
        {
            public SessionInfoAsyncContext() : base(false)
            {
            }

            public void SetDisconnectError(Exception exception)
            {
                if (taskCompletionSource_ != null)
                    taskCompletionSource_.SetException(exception);
            }

            public TaskCompletionSource<TickTrader.FDK.Common.SessionInfo> taskCompletionSource_;
        }

        class OrdersAsyncContext : OrderMassStatusRequestClientContext, IAsyncContext
        {
            public OrdersAsyncContext() : base(false)
            {
            }

            public void SetDisconnectError(Exception exception)
            {
                if (taskCompletionSource_ != null)
                    taskCompletionSource_.SetException(exception);
            }

            public TaskCompletionSource<TickTrader.FDK.Common.ExecutionReport[]> taskCompletionSource_;
        }

        class PositionsAsyncContext : PositionListRequestClientContext, IAsyncContext
        {
            public PositionsAsyncContext() : base(false)
            {
            }

            public void SetDisconnectError(Exception exception)
            {
                if (taskCompletionSource_ != null)
                    taskCompletionSource_.SetException(exception);
            }

            public TaskCompletionSource<TickTrader.FDK.Common.Position[]> taskCompletionSource_;
        }

        class NewOrderAsyncContext : NewOrderSingleClientContext, IAsyncContext
        {
            public NewOrderAsyncContext() : base(false)
            {
            }

            public void SetDisconnectError(Exception exception)
            {
                if (taskCompletionSource_ != null)
                    taskCompletionSource_.SetException(exception);
            }

            public TaskCompletionSource<TickTrader.FDK.Common.ExecutionReport[]> taskCompletionSource_;
            public List<TickTrader.FDK.Common.ExecutionReport> executionReportList_;
        }

        class ReplaceOrderAsyncContext : OrderCancelReplaceRequestClientContext, IAsyncContext
        {
            public ReplaceOrderAsyncContext() : base(false)
            {
            }

            public void SetDisconnectError(Exception exception)
            {
                if (taskCompletionSource_ != null)
                    taskCompletionSource_.SetException(exception);
            }

            public TaskCompletionSource<TickTrader.FDK.Common.ExecutionReport[]> taskCompletionSource_;
            public List<TickTrader.FDK.Common.ExecutionReport> executionReportList_;
        }

        class CancelOrderAsyncContext : OrderCancelRequestClientContext, IAsyncContext
        {
            public CancelOrderAsyncContext() : base(false)
            {
            }

            public void SetDisconnectError(Exception exception)
            {
                if (taskCompletionSource_ != null)
                    taskCompletionSource_.SetException(exception);
            }

            public TaskCompletionSource<TickTrader.FDK.Common.ExecutionReport[]> taskCompletionSource_;
            public List<TickTrader.FDK.Common.ExecutionReport> executionReportList_;
        }

        class ClosePositionAsyncContext : ClosePositionRequestClientContext, IAsyncContext
        {
            public ClosePositionAsyncContext() : base(false)
            {
            }

            public void SetDisconnectError(Exception exception)
            {
                if (taskCompletionSource_ != null)
                    taskCompletionSource_.SetException(exception);
            }

            public TaskCompletionSource<TickTrader.FDK.Common.ExecutionReport[]> taskCompletionSource_;
            public List<TickTrader.FDK.Common.ExecutionReport> executionReportList_;
        }

        class ClosePositionByAsyncContext : ClosePositionByRequestClientContext, IAsyncContext
        {
            public ClosePositionByAsyncContext() : base(false)
            {
            }

            public void SetDisconnectError(Exception exception)
            {
                if (taskCompletionSource_ != null)
                    taskCompletionSource_.SetException(exception);
            }

            public TaskCompletionSource<TickTrader.FDK.Common.ExecutionReport[]> taskCompletionSource_;
            public List<TickTrader.FDK.Common.ExecutionReport> executionReportList_;
        }

        class ClientSessionListener : SoftFX.Net.OrderEntry.ClientSessionListener
        {
            public ClientSessionListener(Client client)
            {
                client_ = client;
            }

            public override void OnConnect(ClientSession clientSession, ConnectClientContext connectContext)
            {
                try
                {
                    if (connectContext != null)
                    {
                        ConnectAsyncContext connectAsyncContext = (ConnectAsyncContext)connectContext;

                        if (client_.ConnectEvent != null)
                        {
                            try
                            {
                                client_.ConnectEvent(client_, connectAsyncContext.Data);
                            }
                            catch
                            {
                            }
                        }

                        if (connectAsyncContext.taskCompletionSource_ != null)
                            connectAsyncContext.taskCompletionSource_.SetResult(null);
                    }
                    else
                    {
                        // reconnect

                        if (client_.ConnectEvent != null)
                        {
                            try
                            {
                                client_.ConnectEvent(client_, null);
                            }
                            catch
                            {
                            }
                        }
                    }
                }
                catch
                {
                }
            }

            public override void OnConnectError(ClientSession clientSession, ConnectClientContext connectContext, string text)
            {                
                try
                {
                    if (connectContext != null)
                    {
                        ConnectAsyncContext connectAsyncContext = (ConnectAsyncContext)connectContext;

                        if (client_.ConnectErrorEvent != null)
                        {
                            try
                            {
                                client_.ConnectErrorEvent(client_, connectAsyncContext.Data, text);
                            }
                            catch
                            {
                            }
                        }

                        if (connectAsyncContext.taskCompletionSource_ != null)
                        {
                            Exception exception = new Exception(text);
                            connectAsyncContext.taskCompletionSource_.SetException(exception);
                        }
                    }
                    else
                    {
                        // reconnect

                        if (client_.ConnectErrorEvent != null)
                        {
                            try
                            {
                                client_.ConnectErrorEvent(client_, null, text);
                            }
                            catch
                            {
                            }
                        }
                    }
                }
                catch
                {
                }
            }

            public override void OnDisconnect(ClientSession clientSession, DisconnectClientContext disconnectContext, ClientContext[] contexts, string text)
            {
                try
                {
                    if (disconnectContext != null)
                    {
                        DisconnectAsyncContext disconnectAsyncContext = (DisconnectAsyncContext) disconnectContext;

                        if (client_.DisconnectEvent != null)
                        {
                            try
                            {
                                client_.DisconnectEvent(client_, disconnectAsyncContext.Data, text);
                            }
                            catch
                            {
                            }
                        }

                        if (contexts.Length > 0)
                        {
                            Exception exception = new Exception(text);

                            foreach (ClientContext context in contexts)
                                ((IAsyncContext)context).SetDisconnectError(exception);
                        }

                        if (disconnectAsyncContext.taskCompletionSource_ != null)
                            disconnectAsyncContext.taskCompletionSource_.SetResult(null);
                    }
                    else
                    {
                        // Unsolicited disconnect

                        if (client_.DisconnectEvent != null)
                        {
                            try
                            {
                                client_.DisconnectEvent(client_, null, text);
                            }
                            catch
                            {
                            }
                        }

                        if (contexts.Length > 0)
                        {
                            Exception exception = new Exception(text);

                            foreach (ClientContext context in contexts)
                                ((IAsyncContext)context).SetDisconnectError(exception);
                        }                        
                    }
                }
                catch
                {
                }
            }

            public override void OnLoginReport(ClientSession session, LoginRequestClientContext LoginRequestClientContext, LoginReport message)
            {
                var context = (LoginAsyncContext)LoginRequestClientContext;

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

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetResult(null);
                }
                catch (Exception exception)
                {
                    if (client_.LoginErrorEvent != null)
                    {
                        try
                        {
                            client_.LoginErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetException(exception);
                }
            }

            public override void OnLoginReject(ClientSession session, LoginRequestClientContext LoginRequestClientContext, LoginReject message)
            {
                var context = (LoginAsyncContext)LoginRequestClientContext;

                try
                {
                    string text = message.Text;

                    if (client_.LoginErrorEvent != null)
                    {
                        try
                        {
                            client_.LoginErrorEvent(client_, context.Data, text);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                    {
                        var exception = new Exception(text);
                        context.taskCompletionSource_.SetException(exception);
                    }
                }
                catch (Exception exception)
                {
                    if (client_.LoginErrorEvent != null)
                    {
                        try
                        {
                            client_.LoginErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetException(exception);
                }
            }

            public override void OnTwoFactorLoginRequest(ClientSession session, LoginRequestClientContext LoginRequestClientContext, TwoFactorLogin message)
            {
                var context = (LoginAsyncContext) LoginRequestClientContext;

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
                            client_.LoginErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetException(exception);
                }
            }

            public override void OnTwoFactorLoginSuccess(ClientSession session, LoginRequestClientContext LoginRequestClientContext, TwoFactorLoginResponseClientContext TwoFactorLoginResponseClientContext, TwoFactorLogin message)
            {
                var loginContext = (LoginAsyncContext) LoginRequestClientContext;

                try
                {
                    var responseContext = (TwoFactorLoginResponseAsyncContext) TwoFactorLoginResponseClientContext;

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

                        if (responseContext.taskCompletionSource_ != null)
                            responseContext.taskCompletionSource_.SetResult(expireTime);
                    }
                    catch (Exception exception)
                    {
                        if (client_.TwoFactorLoginErrorEvent != null)
                        {
                            try
                            {
                                client_.TwoFactorLoginErrorEvent(client_, responseContext.Data, exception.Message);
                            }
                            catch
                            {
                            }
                        }

                        if (responseContext.taskCompletionSource_ != null)
                            responseContext.taskCompletionSource_.SetException(exception);

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

                    if (loginContext.taskCompletionSource_ != null)
                        loginContext.taskCompletionSource_.SetResult(null);
                }
                catch (Exception exception)
                {
                    if (client_.LoginErrorEvent != null)
                    {
                        try
                        {
                            client_.LoginErrorEvent(client_, LoginRequestClientContext.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    if (loginContext.taskCompletionSource_ != null)
                        loginContext.taskCompletionSource_.SetException(exception);
                }
            }

            public override void OnTwoFactorLoginReject(ClientSession session, LoginRequestClientContext LoginRequestClientContext, TwoFactorLoginResponseClientContext TwoFactorLoginResponseClientContext, TwoFactorReject message)
            {
                var loginContext = (LoginAsyncContext) LoginRequestClientContext;

                try
                {
                    string text = message.Text;

                    var responseContext = (TwoFactorLoginResponseAsyncContext) TwoFactorLoginResponseClientContext;

                    try
                    {
                        if (client_.TwoFactorLoginErrorEvent != null)
                        {
                            try
                            {
                                client_.TwoFactorLoginErrorEvent(client_, responseContext.Data, text);
                            }
                            catch
                            {
                            }
                        }

                        if (responseContext.taskCompletionSource_ != null)
                        {
                            Exception exception = new Exception(text);
                            responseContext.taskCompletionSource_.SetException(exception);
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.TwoFactorLoginErrorEvent != null)
                        {
                            try
                            {
                                client_.TwoFactorLoginErrorEvent(client_, responseContext.Data, exception.Message);
                            }
                            catch
                            {
                            }
                        }

                        if (responseContext.taskCompletionSource_ != null)
                            responseContext.taskCompletionSource_.SetException(exception);

                        // the login procedure continues..
                    }

                    // the login procedure continues..
                }
                catch (Exception exception)
                {
                    if (client_.LoginErrorEvent != null)
                    {
                        try
                        {
                            client_.LoginErrorEvent(client_, loginContext.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    if (loginContext.taskCompletionSource_ != null)
                        loginContext.taskCompletionSource_.SetException(exception);
                }
            }

            public override void OnTwoFactorLoginError(ClientSession session, LoginRequestClientContext LoginRequestClientContext, TwoFactorLoginResponseClientContext TwoFactorLoginResponseClientContext, TwoFactorLogin message)
            {
                var loginContext = (LoginAsyncContext) LoginRequestClientContext;

                try
                {
                    string text = message.Text;

                    var responseContext = (TwoFactorLoginResponseAsyncContext) TwoFactorLoginResponseClientContext;

                    try
                    {
                        if (client_.TwoFactorLoginErrorEvent != null)
                        {
                            try
                            {
                                client_.TwoFactorLoginErrorEvent(client_, responseContext.Data, text);
                            }
                            catch
                            {
                            }
                        }

                        if (responseContext.taskCompletionSource_ != null)
                        {
                            Exception exception = new Exception(text);
                            responseContext.taskCompletionSource_.SetException(exception);
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.TwoFactorLoginErrorEvent != null)
                        {
                            try
                            {
                                client_.TwoFactorLoginErrorEvent(client_, responseContext.Data, exception.Message);
                            }
                            catch
                            {
                            }
                        }

                        if (responseContext.taskCompletionSource_ != null)
                            responseContext.taskCompletionSource_.SetException(exception);

                        throw;
                    }

                    if (client_.LoginErrorEvent != null)
                    {
                        try
                        {
                            client_.LoginErrorEvent(client_, loginContext.Data, text);
                        }
                        catch
                        {
                        }
                    }

                    if (loginContext.taskCompletionSource_ != null)
                    {
                        Exception exception = new Exception(text);
                        loginContext.taskCompletionSource_.SetException(exception);
                    }
                }
                catch (Exception exception)
                {
                    if (client_.LoginErrorEvent != null)
                    {
                        try
                        {
                            client_.LoginErrorEvent(client_, loginContext.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    if (loginContext.taskCompletionSource_ != null)
                        loginContext.taskCompletionSource_.SetException(exception);
                }
            }

            public override void OnTwoFactorLoginResume(ClientSession session, TwoFactorLoginResumeClientContext TwoFactorLoginResumeClientContext, TwoFactorLogin message)
            {
                var context = (TwoFactorLoginResumeAsyncContext) TwoFactorLoginResumeClientContext;

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

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetResult(expireTime);
                }
                catch (Exception exception)
                {
                    if (client_.TwoFactorLoginErrorEvent != null)
                    {
                        try
                        {
                            client_.TwoFactorLoginErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetException(exception);
                }
            }

            public override void OnLogout(ClientSession session, LogoutClientContext LogoutClientContext, Logout message)
            {
                var context = (LogoutAsyncContext)LogoutClientContext;

                try
                {
                    var result = new LogoutInfo();
                    result.Reason = Convert(message.Reason);
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

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetResult(result);
                }
                catch
                {
                    // on logout we don't throw

                    var result = new LogoutInfo();
                    result.Reason = TickTrader.FDK.Common.LogoutReason.Unknown;

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

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetResult(result);
                }
            }

            public override void OnTradeServerInfoReport(ClientSession session, TradeServerInfoRequestClientContext TradeServerInfoRequestClientContext, TradeServerInfoReport message)
            {
                var context = (TradeServerInfoAsyncContext) TradeServerInfoRequestClientContext;

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
                    resultTradeServerInfo.ServerAddress = message.ServerAddress;
                    resultTradeServerInfo.ServerFullName = message.ServerFullName;
                    resultTradeServerInfo.ServerDescription = message.ServerDescription;
                    resultTradeServerInfo.ServerAddress = message.ServerAddress;

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

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetResult(resultTradeServerInfo);
                }
                catch (Exception exception)
                {
                    if (client_.TradeServerInfoErrorEvent != null)
                    {
                        try
                        {
                            client_.TradeServerInfoErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetException(exception);
                }
            }

            public override void OnTradeServerInfoReject(ClientSession session, TradeServerInfoRequestClientContext TradeServerInfoRequestClientContext, Reject message)
            {
                var context = (TradeServerInfoAsyncContext) TradeServerInfoRequestClientContext;

                try
                {
                    string text = message.Text;

                    if (client_.TradeServerInfoErrorEvent != null)
                    {
                        try
                        {
                            client_.TradeServerInfoErrorEvent(client_, context.Data, text);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                    {
                        var exception = new Exception(text);
                        context.taskCompletionSource_.SetException(exception);
                    }
                }
                catch (Exception exception)
                {
                    if (client_.TradeServerInfoErrorEvent != null)
                    {
                        try
                        {
                            client_.TradeServerInfoErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetException(exception);
                }
            }

            public override void OnAccountInfoReport(ClientSession session, AccountInfoRequestClientContext AccountInfoRequestClientContext, AccountInfoReport message)
            {
                var context = (AccountInfoAsyncContext) AccountInfoRequestClientContext;

                try
                {
                    TickTrader.FDK.Common.AccountInfo resultAccountInfo = new TickTrader.FDK.Common.AccountInfo();
                    SoftFX.Net.OrderEntry.AccountInfo reportAccountInfo = message.AccountInfo;
                    resultAccountInfo.AccountId = reportAccountInfo.Id.ToString();                    
                    resultAccountInfo.Type = Convert(reportAccountInfo.Type);
                    resultAccountInfo.Name = reportAccountInfo.Name;
                    resultAccountInfo.Email = reportAccountInfo.Email;
                    resultAccountInfo.Comment = reportAccountInfo.Description;                    
                    resultAccountInfo.RegistredDate = reportAccountInfo.RegistDate;
                    resultAccountInfo.Leverage = reportAccountInfo.Leverage;

                    BalanceNull reportBalance = reportAccountInfo.Balance;

                    if (reportBalance.HasValue)
                    {
                        resultAccountInfo.Currency = reportBalance.CurrId;   
                        resultAccountInfo.Balance = reportBalance.Total;
                    }
                    else
                    {
                        resultAccountInfo.Currency = "";     // TODO: something in the client calculator crashes if null
                        resultAccountInfo.Balance = 0;
                    }
                    
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

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetResult(resultAccountInfo);
                }
                catch (Exception exception)
                {
                    if (client_.AccountInfoErrorEvent != null)
                    {
                        try
                        {
                            client_.AccountInfoErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetException(exception);
                }
            }

            public override void OnAccountInfoReject(ClientSession session, AccountInfoRequestClientContext AccountInfoRequestClientContext, Reject message)
            {
                var context = (AccountInfoAsyncContext) AccountInfoRequestClientContext;

                try
                {
                    string text = message.Text;

                    if (client_.AccountInfoErrorEvent != null)
                    {
                        try
                        {
                            client_.AccountInfoErrorEvent(client_, context.Data, text);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                    {
                        var exception = new Exception(text);
                        context.taskCompletionSource_.SetException(exception);
                    }
                }
                catch (Exception exception)
                {
                    if (client_.AccountInfoErrorEvent != null)
                    {
                        try
                        {
                            client_.AccountInfoErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetException(exception);
                }
            }

            public override void OnTradingSessionStatusReport(ClientSession session, TradingSessionStatusRequestClientContext TradingSessionStatusRequestClientContext, TradingSessionStatusReport message)
            {
                var context = (SessionInfoAsyncContext) TradingSessionStatusRequestClientContext;

                try
                {
                    TickTrader.FDK.Common.SessionInfo resultStatusInfo = new TickTrader.FDK.Common.SessionInfo();
                    SoftFX.Net.OrderEntry.TradingSessionStatusInfo reportStatusInfo = message.StatusInfo;

                    resultStatusInfo.Status = Convert(reportStatusInfo.Status);
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
                        resultGroup.Status = Convert(reportGroup.Status);
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

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetResult(resultStatusInfo);
                }
                catch (Exception exception)
                {
                    if (client_.SessionInfoErrorEvent != null)
                    {
                        try
                        {
                            client_.SessionInfoErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetException(exception);
                }
            }

            public override void OnTradingSessionStatusReject(ClientSession session, TradingSessionStatusRequestClientContext TradingSessionStatusRequestClientContext, Reject message)
            {
                var context = (SessionInfoAsyncContext) TradingSessionStatusRequestClientContext;

                try
                {
                    string text = message.Text;

                    if (client_.SessionInfoErrorEvent != null)
                    {
                        try
                        {
                            client_.SessionInfoErrorEvent(client_, context.Data, text);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                    {
                        var exception = new Exception(text);
                        context.taskCompletionSource_.SetException(exception);
                    }
                }
                catch (Exception exception)
                {
                    if (client_.SessionInfoErrorEvent != null)
                    {
                        try
                        {
                            client_.SessionInfoErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetException(exception);
                }
            }

            public override void OnOrderMassStatusReport(ClientSession session, OrderMassStatusRequestClientContext OrderMassStatusRequestClientContext, OrderMassStatusReport message)
            {
                var context = (OrdersAsyncContext) OrderMassStatusRequestClientContext;

                try
                {
                    SoftFX.Net.OrderEntry.OrderMassStatusEntryArray reportEntries = message.Entries;
                    int count = reportEntries.Length;
                    TickTrader.FDK.Common.ExecutionReport[] resultExecutionReports = new TickTrader.FDK.Common.ExecutionReport[count];

                    for (int index = 0; index < count; ++index)
                    {
                        SoftFX.Net.OrderEntry.OrderMassStatusEntry reportEntry = reportEntries[index];
                        SoftFX.Net.OrderEntry.OrderAttributes reportEntryAttributes = reportEntry.Attributes;
                        SoftFX.Net.OrderEntry.OrderState reportEntryState = reportEntry.State;

                        TickTrader.FDK.Common.ExecutionReport resultExecutionReport = new TickTrader.FDK.Common.ExecutionReport();
                        resultExecutionReport.ExecutionType = ExecutionType.OrderStatus;
                        resultExecutionReport.OrigClientOrderId = reportEntry.OrigClOrdId;
                        resultExecutionReport.OrderId = reportEntry.OrderId.ToString();
                        resultExecutionReport.Symbol = reportEntryAttributes.SymbolId;
                        resultExecutionReport.OrderSide = Convert(reportEntryAttributes.Side);
                        resultExecutionReport.OrderType = Convert(reportEntryAttributes.Type);                       

                        if (reportEntryAttributes.TimeInForce.HasValue)
                        {
                            resultExecutionReport.OrderTimeInForce = Convert(reportEntryAttributes.TimeInForce.Value);
                        }
                        else
                            resultExecutionReport.OrderTimeInForce = null;

                        resultExecutionReport.InitialVolume = reportEntryAttributes.Qty;
                        resultExecutionReport.Price = reportEntryAttributes.Price;
                        resultExecutionReport.StopPrice = reportEntryAttributes.StopPrice;
                        resultExecutionReport.Expiration = reportEntryAttributes.ExpireTime;
                        resultExecutionReport.TakeProfit = reportEntryAttributes.TakeProfit;
                        resultExecutionReport.StopLoss = reportEntryAttributes.StopLoss;
                        resultExecutionReport.MarketWithSlippage = (reportEntryAttributes.Flags & OrderFlags.Slippage) != 0;
                        resultExecutionReport.OrderStatus = Convert(reportEntryState.Status); 
                        resultExecutionReport.ExecutedVolume = reportEntryState.CumQty;
                        resultExecutionReport.LeavesVolume = reportEntryState.LeavesQty;
                        resultExecutionReport.TradeAmount = reportEntryState.LastQty;
                        resultExecutionReport.TradePrice = reportEntryState.LastPrice;
                        resultExecutionReport.Commission = reportEntry.Commission;
                        resultExecutionReport.AgentCommission = reportEntry.AgentCommission;
                        resultExecutionReport.Swap = reportEntry.Swap;
                        resultExecutionReport.AveragePrice = reportEntryState.AvgPrice;                        
                        resultExecutionReport.Created = reportEntryState.Created;
                        resultExecutionReport.Modified = reportEntryState.Modified;
                        resultExecutionReport.RejectReason = TickTrader.FDK.Common.RejectReason.None;
                        resultExecutionReport.Comment = reportEntryAttributes.Comment;
                        resultExecutionReport.Tag = reportEntryAttributes.Tag;
                        resultExecutionReport.Magic = reportEntryAttributes.Magic;
                        resultExecutionReports[index] = resultExecutionReport;
                    }

                    if (client_.OrdersResultEvent != null)
                    {
                        try
                        {
                            client_.OrdersResultEvent(client_, context.Data, resultExecutionReports);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetResult(resultExecutionReports);
                }
                catch (Exception exception)
                {
                    if (client_.OrdersErrorEvent != null)
                    {
                        try
                        {
                            client_.OrdersErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetException(exception);
                }
            }

            public override void OnOrderMassStatusReject(ClientSession session, OrderMassStatusRequestClientContext OrderMassStatusRequestClientContext, Reject message)
            {
                var context = (OrdersAsyncContext) OrderMassStatusRequestClientContext;

                try
                {
                    string text = message.Text;

                    if (client_.OrdersErrorEvent != null)
                    {
                        try
                        {
                            client_.OrdersErrorEvent(client_, context.Data, text);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                    {
                        var exception = new Exception(text);
                        context.taskCompletionSource_.SetException(exception);
                    }
                }
                catch (Exception exception)
                {
                    if (client_.OrdersErrorEvent != null)
                    {
                        try
                        {
                            client_.OrdersErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetException(exception);
                }
            }

            public override void OnPositionListReport(ClientSession session, PositionListRequestClientContext PositionListRequestClientContext, PositionListReport message)
            {
                var context = (PositionsAsyncContext) PositionListRequestClientContext;

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
                        resultPosition.SettlementPrice = reportPosition.SettltPrice;
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

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetResult(resultPositions);
                }
                catch (Exception exception)
                {
                    if (client_.PositionsErrorEvent != null)
                    {
                        try
                        {
                            client_.PositionsErrorEvent (client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetException(exception);
                }
            }

            public override void OnPositionListReject(ClientSession session, PositionListRequestClientContext PositionListRequestClientContext, Reject message)
            {
                var context = (PositionsAsyncContext) PositionListRequestClientContext;

                try
                {
                    string text = message.Text;

                    if (client_.PositionsErrorEvent != null)
                    {
                        try
                        {
                            client_.PositionsErrorEvent (client_, context.Data, text);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                    {
                        var exception = new Exception(text);
                        context.taskCompletionSource_.SetException(exception);
                    }
                }
                catch (Exception exception)
                {
                    if (client_.PositionsErrorEvent != null)
                    {
                        try
                        {
                            client_.PositionsErrorEvent (client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetException(exception);
                }
            }

            public override void OnNewOrderSingleMarketNewReport(ClientSession session, NewOrderSingleClientContext NewOrderSingleClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                var context = (NewOrderAsyncContext) NewOrderSingleClientContext;

                try
                {
                    TickTrader.FDK.Common.ExecutionReport result = Convert(message);

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

                    if (context.taskCompletionSource_ != null)
                        context.executionReportList_.Add(result);
                }
                catch (Exception exception)
                {
                    TickTrader.FDK.Common.ExecutionReport result = Convert(exception);

                    if (client_.NewOrderErrorEvent != null)
                    {
                        try
                        {
                            client_.NewOrderErrorEvent(client_, context.Data, result);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetException(exception);
                }
            }

            public override void OnNewOrderSingleMarketFillReport(ClientSession session, NewOrderSingleClientContext NewOrderSingleClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                var context = (NewOrderAsyncContext) NewOrderSingleClientContext;

                try
                {
                    TickTrader.FDK.Common.ExecutionReport result = Convert(message);

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

                    if (context.taskCompletionSource_ != null)
                    {
                        context.executionReportList_.Add(result);
                        context.taskCompletionSource_.SetResult(context.executionReportList_.ToArray());
                    }
                }
                catch (Exception exception)
                {
                    TickTrader.FDK.Common.ExecutionReport result = Convert(exception);

                    if (client_.NewOrderErrorEvent != null)
                    {
                        try
                        {
                            client_.NewOrderErrorEvent(client_, context.Data, result);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetException(exception);
                }
            }

            public override void OnNewOrderSingleMarketPartialFillReport(ClientSession session, NewOrderSingleClientContext NewOrderSingleClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                var context = (NewOrderAsyncContext) NewOrderSingleClientContext;

                try
                {
                    TickTrader.FDK.Common.ExecutionReport result = Convert(message);

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

                    if (context.taskCompletionSource_ != null)
                        context.executionReportList_.Add(result);
                }
                catch (Exception exception)
                {
                    TickTrader.FDK.Common.ExecutionReport result = Convert(exception);

                    if (client_.NewOrderErrorEvent != null)
                    {
                        try
                        {
                            client_.NewOrderErrorEvent(client_, context.Data, result);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetException(exception);
                }
            }

            public override void OnNewOrderSingleNewReport(ClientSession session, NewOrderSingleClientContext NewOrderSingleClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                var context = (NewOrderAsyncContext) NewOrderSingleClientContext;

                try
                {
                    TickTrader.FDK.Common.ExecutionReport result = Convert(message);

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

                    if (context.taskCompletionSource_ != null)
                        context.executionReportList_.Add(result);
                }
                catch (Exception exception)
                {
                    TickTrader.FDK.Common.ExecutionReport result = Convert(exception);

                    if (client_.NewOrderErrorEvent != null)
                    {
                        try
                        {
                            client_.NewOrderErrorEvent(client_, context.Data, result);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetException(exception);
                }                
            }

            public override void OnNewOrderSingleCalculatedReport(ClientSession session, NewOrderSingleClientContext NewOrderSingleClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                var context = (NewOrderAsyncContext) NewOrderSingleClientContext;

                try
                {
                    TickTrader.FDK.Common.ExecutionReport result = Convert(message);

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

                    if (context.taskCompletionSource_ != null)
                    {
                        context.executionReportList_.Add(result);
                        context.taskCompletionSource_.SetResult(context.executionReportList_.ToArray());
                    }
                }
                catch (Exception exception)
                {
                    TickTrader.FDK.Common.ExecutionReport result = Convert(exception);

                    if (client_.NewOrderErrorEvent != null)
                    {
                        try
                        {
                            client_.NewOrderErrorEvent(client_, context.Data, result);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetException(exception);
                }
            }

            public override void OnNewOrderSingleRejectReport(ClientSession session, NewOrderSingleClientContext NewOrderSingleClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                var context = (NewOrderAsyncContext) NewOrderSingleClientContext;

                try
                {
                    TickTrader.FDK.Common.ExecutionReport result = Convert(message);

                    if (client_.NewOrderErrorEvent != null)
                    {
                        try
                        {
                            client_.NewOrderErrorEvent(client_, context.Data, result);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                    {
                        context.executionReportList_.Add(result);
                        ExecutionException exception = new ExecutionException(context.executionReportList_.ToArray());
                        context.taskCompletionSource_.SetException(exception);
                    }
                }
                catch (Exception exception)
                {
                    TickTrader.FDK.Common.ExecutionReport result = Convert(exception);

                    if (client_.NewOrderErrorEvent != null)
                    {
                        try
                        {
                            client_.NewOrderErrorEvent(client_, context.Data, result);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetException(exception);
                }
            }

            public override void OnOrderCancelReplacePendingReplaceReport(ClientSession session, OrderCancelReplaceRequestClientContext OrderCancelReplaceRequestClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                var context = (ReplaceOrderAsyncContext) OrderCancelReplaceRequestClientContext;

                try
                {
                    TickTrader.FDK.Common.ExecutionReport result = Convert(message);

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

                    if (context.taskCompletionSource_ != null)
                        context.executionReportList_.Add(result);
                }
                catch (Exception exception)
                {
                    TickTrader.FDK.Common.ExecutionReport result = Convert(exception);

                    if (client_.ReplaceOrderErrorEvent != null)
                    {
                        try
                        {
                            client_.ReplaceOrderErrorEvent(client_, context.Data, result);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetException(exception);
                }
            }
            public override void OnOrderCancelReplaceReplacedReport(ClientSession session, OrderCancelReplaceRequestClientContext OrderCancelReplaceRequestClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                var context = (ReplaceOrderAsyncContext) OrderCancelReplaceRequestClientContext;

                try
                {
                    TickTrader.FDK.Common.ExecutionReport result = Convert(message);

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

                    if (context.taskCompletionSource_ != null)
                    {
                        context.executionReportList_.Add(result);
                        context.taskCompletionSource_.SetResult(context.executionReportList_.ToArray());
                    }
                }
                catch (Exception exception)
                {
                    TickTrader.FDK.Common.ExecutionReport result = Convert(exception);

                    if (client_.ReplaceOrderErrorEvent != null)
                    {
                        try
                        {
                            client_.ReplaceOrderErrorEvent(client_, context.Data, result);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetException(exception);
                }
            }

            public override void OnOrderCancelReplaceRejectReport(ClientSession session, OrderCancelReplaceRequestClientContext OrderCancelReplaceRequestClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                var context = (ReplaceOrderAsyncContext) OrderCancelReplaceRequestClientContext;

                try
                {
                    TickTrader.FDK.Common.ExecutionReport result = Convert(message);

                    if (client_.ReplaceOrderErrorEvent != null)
                    {
                        try
                        {
                            client_.ReplaceOrderErrorEvent(client_, context.Data, result);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                    {
                        context.executionReportList_.Add(result);
                        ExecutionException exception = new ExecutionException(context.executionReportList_.ToArray());
                        context.taskCompletionSource_.SetException(exception);
                    }
                }
                catch (Exception exception)
                {
                    TickTrader.FDK.Common.ExecutionReport result = Convert(exception);

                    if (client_.ReplaceOrderErrorEvent != null)
                    {
                        try
                        {
                            client_.ReplaceOrderErrorEvent(client_, context.Data, result);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetException(exception);
                }
            }

            public override void OnOrderCancelPendingCancelReport(ClientSession session, OrderCancelRequestClientContext OrderCancelRequestClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                var context = (CancelOrderAsyncContext) OrderCancelRequestClientContext;

                try
                {
                    TickTrader.FDK.Common.ExecutionReport result = Convert(message);

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

                    if (context.taskCompletionSource_ != null)
                        context.executionReportList_.Add(result);
                }
                catch (Exception exception)
                {
                    TickTrader.FDK.Common.ExecutionReport result = Convert(exception);

                    if (client_.CancelOrderErrorEvent != null)
                    {
                        try
                        {
                            client_.CancelOrderErrorEvent(client_, context.Data, result);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetException(exception);
                }
            }

            public override void OnOrderCancelCancelledReport(ClientSession session, OrderCancelRequestClientContext OrderCancelRequestClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                var context = (CancelOrderAsyncContext) OrderCancelRequestClientContext;

                try
                {
                    TickTrader.FDK.Common.ExecutionReport result = Convert(message);

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

                    if (context.taskCompletionSource_ != null)
                    {
                        context.executionReportList_.Add(result);
                        context.taskCompletionSource_.SetResult(context.executionReportList_.ToArray());
                    }
                }
                catch (Exception exception)
                {
                    TickTrader.FDK.Common.ExecutionReport result = Convert(exception);

                    if (client_.CancelOrderErrorEvent != null)
                    {
                        try
                        {
                            client_.CancelOrderErrorEvent(client_, context.Data, result);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetException(exception);
                }
            }

            public override void OnOrderCancelRejectReport(ClientSession session, OrderCancelRequestClientContext OrderCancelRequestClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                var context = (CancelOrderAsyncContext) OrderCancelRequestClientContext;

                try
                {
                    TickTrader.FDK.Common.ExecutionReport result = Convert(message);

                    if (client_.CancelOrderErrorEvent != null)
                    {
                        try
                        {
                            client_.CancelOrderErrorEvent(client_, context.Data, result);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                    {
                        context.executionReportList_.Add(result);
                        ExecutionException exception = new ExecutionException(context.executionReportList_.ToArray());
                        context.taskCompletionSource_.SetException(exception);
                    }
                }
                catch (Exception exception)
                {
                    TickTrader.FDK.Common.ExecutionReport result = Convert(exception);

                    if (client_.CancelOrderErrorEvent != null)
                    {
                        try
                        {
                            client_.CancelOrderErrorEvent(client_, context.Data, result);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetException(exception);
                }
            }

            public override void OnClosePositionPendingCloseReport(ClientSession session, ClosePositionRequestClientContext ClosePositionRequestClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                var context = (ClosePositionAsyncContext) ClosePositionRequestClientContext;

                try
                {
                    TickTrader.FDK.Common.ExecutionReport result = Convert(message);

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

                    if (context.taskCompletionSource_ != null)
                        context.executionReportList_.Add(result);
                }
                catch (Exception exception)
                {
                    TickTrader.FDK.Common.ExecutionReport result = Convert(exception);

                    if (client_.ClosePositionErrorEvent != null)
                    {
                        try
                        {
                            client_.ClosePositionErrorEvent(client_, context.Data, result);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetException(exception);
                }
            }

            public override void OnClosePositionFillReport(ClientSession session, ClosePositionRequestClientContext ClosePositionRequestClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                var context = (ClosePositionAsyncContext) ClosePositionRequestClientContext;

                try
                {
                    TickTrader.FDK.Common.ExecutionReport result = Convert(message);

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

                    if (context.taskCompletionSource_ != null)
                    {
                        context.executionReportList_.Add(result);
                        context.taskCompletionSource_.SetResult(context.executionReportList_.ToArray());
                    }
                }
                catch (Exception exception)
                {
                    TickTrader.FDK.Common.ExecutionReport result = Convert(exception);

                    if (client_.ClosePositionErrorEvent != null)
                    {
                        try
                        {
                            client_.ClosePositionErrorEvent(client_, context.Data, result);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetException(exception);
                }
            }

            public override void OnClosePositionRejectReport(ClientSession session, ClosePositionRequestClientContext ClosePositionRequestClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                var context = (ClosePositionAsyncContext) ClosePositionRequestClientContext;

                try
                {
                    TickTrader.FDK.Common.ExecutionReport result = Convert(message);

                    if (client_.ClosePositionErrorEvent != null)
                    {
                        try
                        {
                            client_.ClosePositionErrorEvent(client_, context.Data, result);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                    {
                        context.executionReportList_.Add(result);
                        ExecutionException exception = new ExecutionException(context.executionReportList_.ToArray());
                        context.taskCompletionSource_.SetException(exception);
                    }
                }
                catch (Exception exception)
                {
                    TickTrader.FDK.Common.ExecutionReport result = Convert(exception);

                    if (client_.ClosePositionErrorEvent != null)
                    {
                        try
                        {
                            client_.ClosePositionErrorEvent(client_, context.Data, result);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetException(exception);
                }
            }

            public override void OnClosePositionByFillReport1(ClientSession session, ClosePositionByRequestClientContext ClosePositionByRequestClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                var context = (ClosePositionByAsyncContext) ClosePositionByRequestClientContext;

                try
                {
                    TickTrader.FDK.Common.ExecutionReport result = Convert(message);

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

                    if (context.taskCompletionSource_ != null)
                        context.executionReportList_.Add(result);
                }
                catch (Exception exception)
                {
                    TickTrader.FDK.Common.ExecutionReport result = Convert(exception);

                    if (client_.ClosePositionByErrorEvent != null)
                    {
                        try
                        {
                            client_.ClosePositionByErrorEvent(client_, context.Data, result);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetException(exception);
                }
            }

            public override void OnClosePositionByFillReport2(ClientSession session, ClosePositionByRequestClientContext ClosePositionByRequestClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                var context = (ClosePositionByAsyncContext) ClosePositionByRequestClientContext;

                try
                {
                    TickTrader.FDK.Common.ExecutionReport result = Convert(message);

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

                    if (context.taskCompletionSource_ != null)
                    {
                        context.executionReportList_.Add(result);
                        context.taskCompletionSource_.SetResult(context.executionReportList_.ToArray());
                    }
                }
                catch (Exception exception)
                {
                    TickTrader.FDK.Common.ExecutionReport result = Convert(exception);

                    if (client_.ClosePositionByErrorEvent != null)
                    {
                        try
                        {
                            client_.ClosePositionByErrorEvent(client_, context.Data, result);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetException(exception);
                }
            }

            public override void OnClosePositionByRejectReport1(ClientSession session, ClosePositionByRequestClientContext ClosePositionByRequestClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                var context = (ClosePositionByAsyncContext) ClosePositionByRequestClientContext;

                try
                {
                    TickTrader.FDK.Common.ExecutionReport result = Convert(message);

                    if (client_.ClosePositionByErrorEvent != null)
                    {
                        try
                        {
                            client_.ClosePositionByErrorEvent(client_, context.Data, result);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                        context.executionReportList_.Add(result);
                }
                catch (Exception exception)
                {
                    TickTrader.FDK.Common.ExecutionReport result = Convert(exception);

                    if (client_.ClosePositionByErrorEvent != null)
                    {
                        try
                        {
                            client_.ClosePositionByErrorEvent(client_, context.Data, result);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetException(exception);
                }
            }

            public override void OnClosePositionByRejectReport2(ClientSession session, ClosePositionByRequestClientContext ClosePositionByRequestClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                var context = (ClosePositionByAsyncContext) ClosePositionByRequestClientContext;

                try
                {
                    TickTrader.FDK.Common.ExecutionReport result = Convert(message);

                    if (client_.ClosePositionByErrorEvent != null)
                    {
                        try
                        {
                            client_.ClosePositionByErrorEvent(client_, context.Data, result);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                    {
                        context.executionReportList_.Add(result);
                        ExecutionException exception = new ExecutionException(context.executionReportList_.ToArray());
                        context.taskCompletionSource_.SetException(exception);
                    }
                }
                catch (Exception exception)
                {
                    TickTrader.FDK.Common.ExecutionReport result = Convert(exception);

                    if (client_.ClosePositionByErrorEvent != null)
                    {
                        try
                        {
                            client_.ClosePositionByErrorEvent(client_, context.Data, result);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetException(exception);
                }
            }

            public override void OnExecutionReport(ClientSession session, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                try
                {       
                    TickTrader.FDK.Common.ExecutionReport result = Convert(message);
                                 
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
                }
            }

            public override void OnPositionReport(ClientSession session, PositionReport message)
            {
                try
                {
                    SoftFX.Net.OrderEntry.Position reportPosition = message.Position;
                    TickTrader.FDK.Common.Position resultPosition = new TickTrader.FDK.Common.Position();

                    resultPosition.Symbol = reportPosition.SymbolId;
                    resultPosition.SettlementPrice = reportPosition.SettltPrice;
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
                }
            }

            public override void OnAccountInfoUpdate(ClientSession session, AccountInfoUpdate message)
            {
                try
                {
                    TickTrader.FDK.Common.AccountInfo resultAccountInfo = new TickTrader.FDK.Common.AccountInfo();
                    SoftFX.Net.OrderEntry.AccountInfo reportAccountInfo = message.AccountInfo;
                    resultAccountInfo.AccountId = reportAccountInfo.Id.ToString();                    
                    resultAccountInfo.Type = Convert(reportAccountInfo.Type);
                    resultAccountInfo.Name = reportAccountInfo.Name;
                    resultAccountInfo.Email = reportAccountInfo.Email;
                    resultAccountInfo.Comment = reportAccountInfo.Description;
                    resultAccountInfo.RegistredDate = reportAccountInfo.RegistDate;
                    resultAccountInfo.Leverage = reportAccountInfo.Leverage;

                    BalanceNull reportBalance = reportAccountInfo.Balance;

                    if (reportBalance.HasValue)
                    {
                        resultAccountInfo.Currency = reportBalance.CurrId;   
                        resultAccountInfo.Balance = reportBalance.Total;
                    }
                    else
                    {
                        resultAccountInfo.Currency = "";     // TODO: something in the client calculator crashes if null
                        resultAccountInfo.Balance = 0;
                    }

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
                }
            }

            public override void OnTradingSessionStatusUpdate(ClientSession session, TradingSessionStatusUpdate message)
            {
                try
                {
                    TickTrader.FDK.Common.SessionInfo resultStatusInfo = new TickTrader.FDK.Common.SessionInfo();
                    SoftFX.Net.OrderEntry.TradingSessionStatusInfo reportStatusInfo = message.StatusInfo;

                    resultStatusInfo.Status = Convert(reportStatusInfo.Status);
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
                        resultGroup.Status = Convert(reportGroup.Status);
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
                }
            }

            public override void OnNotification(ClientSession session, SoftFX.Net.OrderEntry.Notification message)
            {
                try
                {
                    TickTrader.FDK.Common.Notification result = new TickTrader.FDK.Common.Notification();
                    result.Id = message.Id;
                    result.Type = Convert(message.Type);
                    result.Severity = Convert(message.Severity);
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
                }
            }

            public override void OnLogout(ClientSession session, Logout message)
            {
                try
                {
                    var result = new LogoutInfo();
                    result.Reason = Convert(message.Reason);
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
                }
            }

            TickTrader.FDK.Common.ExecutionReport Convert(SoftFX.Net.OrderEntry.ExecutionReport report)
            {
                TickTrader.FDK.Common.ExecutionReport result = new TickTrader.FDK.Common.ExecutionReport();

                SoftFX.Net.OrderEntry.OrderAttributes reportAttributes = report.Attributes;
                SoftFX.Net.OrderEntry.OrderState reportState = report.State;

                result.ExecutionType = Convert(report.Type);
                result.ClientOrderId = report.ClOrdId;
                result.OrigClientOrderId = report.OrigClOrdId;

                if (report.OrderId.HasValue)
                {
                    result.OrderId = report.OrderId.Value.ToString();
                }
                else
                    result.OrderId = null;

                result.Symbol = reportAttributes.SymbolId;
                result.OrderType = Convert(reportAttributes.Type);
                result.OrderSide = Convert(reportAttributes.Side);

                if (reportAttributes.TimeInForce.HasValue)
                {
                    result.OrderTimeInForce = Convert(reportAttributes.TimeInForce.Value);
                }
                else
                    result.OrderTimeInForce = null;

                result.InitialVolume = reportAttributes.Qty;
                result.MaxVisibleVolume = reportAttributes.MaxVisibleQty;
                result.Price = reportAttributes.Price;
                result.StopPrice = reportAttributes.StopPrice;
                result.Expiration = reportAttributes.ExpireTime;
                result.TakeProfit = reportAttributes.TakeProfit;
                result.StopLoss = reportAttributes.StopLoss;
                result.MarketWithSlippage = (reportAttributes.Flags & OrderFlags.Slippage) != 0;
                result.OrderStatus = Convert(reportState.Status);                
                result.ExecutedVolume = reportState.CumQty;                
                result.LeavesVolume = reportState.LeavesQty;
                result.TradeAmount = reportState.LastQty;
                result.TradePrice = reportState.LastPrice;
                result.Commission = report.Commission;
                result.AgentCommission = report.AgentCommission;
                result.ReducedOpenCommission = (report.CommissionFlags & OrderCommissionFlags.OpenReduced) != 0;
                result.ReducedCloseCommission = (report.CommissionFlags & OrderCommissionFlags.CloseReduced) != 0;
                result.Swap = report.Swap;                
                result.AveragePrice = reportState.AvgPrice;                
                result.Created = reportState.Created;
                result.Modified = reportState.Modified;

                if (report.RejectReason.HasValue)
                {
                    result.RejectReason = Convert(report.RejectReason.Value);
                }
                else
                    result.RejectReason = TickTrader.FDK.Common.RejectReason.None;

                result.Text = report.Text;
                result.Comment = reportAttributes.Comment;
                result.Tag = reportAttributes.Tag;
                result.Magic = reportAttributes.Magic;

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

                return result;
            }

            TickTrader.FDK.Common.ExecutionReport Convert(Exception exception)
            {
                TickTrader.FDK.Common.ExecutionReport executionReport = new TickTrader.FDK.Common.ExecutionReport();

                executionReport.ExecutionType = ExecutionType.Rejected;
                executionReport.RejectReason = TickTrader.FDK.Common.RejectReason.Other;
                executionReport.Text = exception.Message;

                return executionReport;
            }

            TickTrader.FDK.Common.LogoutReason Convert(SoftFX.Net.OrderEntry.LogoutReason reason)
            {
                switch (reason)
                {
                    case SoftFX.Net.OrderEntry.LogoutReason.ClientLogout:
                        return TickTrader.FDK.Common.LogoutReason.ClientInitiated;

                    case SoftFX.Net.OrderEntry.LogoutReason.ServerLogout:
                        return TickTrader.FDK.Common.LogoutReason.ServerLogout;

                    case SoftFX.Net.OrderEntry.LogoutReason.SlowConnection:
                        return TickTrader.FDK.Common.LogoutReason.SlowConnection;

                    case SoftFX.Net.OrderEntry.LogoutReason.DeletedLogin:
                        return TickTrader.FDK.Common.LogoutReason.LoginDeleted;

                    case SoftFX.Net.OrderEntry.LogoutReason.InternalServerError:
                        return TickTrader.FDK.Common.LogoutReason.ServerError;

                    case SoftFX.Net.OrderEntry.LogoutReason.BlockedLogin:
                        return TickTrader.FDK.Common.LogoutReason.BlockedAccount;

                    default:
                        throw new Exception("Invalid logout reason : " + reason);
                }
            }

            TickTrader.FDK.Common.AccountType Convert(SoftFX.Net.OrderEntry.AccountType type)
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

            TickTrader.FDK.Common.SessionStatus Convert(SoftFX.Net.OrderEntry.TradingSessionStatus status)
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

            TickTrader.FDK.Common.OrderStatus Convert(SoftFX.Net.OrderEntry.OrderStatus status)
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

            TickTrader.FDK.Common.OrderType Convert(SoftFX.Net.OrderEntry.OrderType type)
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

            TickTrader.FDK.Common.OrderSide Convert(SoftFX.Net.OrderEntry.OrderSide side)
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

            TickTrader.FDK.Common.OrderTimeInForce Convert(SoftFX.Net.OrderEntry.OrderTimeInForce timeInForce)
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

            TickTrader.FDK.Common.ExecutionType Convert(SoftFX.Net.OrderEntry.ExecType type)
            {
                switch (type)
                {
                    case SoftFX.Net.OrderEntry.ExecType.New:
                        return TickTrader.FDK.Common.ExecutionType.New;

                    case SoftFX.Net.OrderEntry.ExecType.Fill:
                        return TickTrader.FDK.Common.ExecutionType.Trade;

                    case SoftFX.Net.OrderEntry.ExecType.PartialFill:
                        return TickTrader.FDK.Common.ExecutionType.Trade;

                    case SoftFX.Net.OrderEntry.ExecType.Cancelled:
                        return TickTrader.FDK.Common.ExecutionType.Canceled;

                    case SoftFX.Net.OrderEntry.ExecType.PendingCancel:
                        return TickTrader.FDK.Common.ExecutionType.PendingCancel;

                    case SoftFX.Net.OrderEntry.ExecType.Rejected:
                        return TickTrader.FDK.Common.ExecutionType.Rejected;

                    case SoftFX.Net.OrderEntry.ExecType.Calculated:
                        return TickTrader.FDK.Common.ExecutionType.Calculated;

                    case SoftFX.Net.OrderEntry.ExecType.Expired:
                        return TickTrader.FDK.Common.ExecutionType.Expired;

                    case SoftFX.Net.OrderEntry.ExecType.Replaced:
                        return TickTrader.FDK.Common.ExecutionType.Replace;

                    case SoftFX.Net.OrderEntry.ExecType.PendingReplace:
                        return TickTrader.FDK.Common.ExecutionType.PendingReplace;

                    case SoftFX.Net.OrderEntry.ExecType.PendingClose:
                        return TickTrader.FDK.Common.ExecutionType.PendingClose;

                    default:
                        throw new Exception("Invalid exec type : " + type);
                }
            }

            TickTrader.FDK.Common.RejectReason Convert(SoftFX.Net.OrderEntry.RejectReason reason)
            {
                switch (reason)
                {
                    case SoftFX.Net.OrderEntry.RejectReason.Dealer:
                        return TickTrader.FDK.Common.RejectReason.DealerReject;

                    case SoftFX.Net.OrderEntry.RejectReason.DealerTimeout:
                        return TickTrader.FDK.Common.RejectReason.DealerReject;

                    case SoftFX.Net.OrderEntry.RejectReason.UnknownSymbol:
                        return TickTrader.FDK.Common.RejectReason.UnknownSymbol;

                    case SoftFX.Net.OrderEntry.RejectReason.LimitsExceeded:
                        return TickTrader.FDK.Common.RejectReason.OrderExceedsLImit;

                    case SoftFX.Net.OrderEntry.RejectReason.OffQuotes:
                        return TickTrader.FDK.Common.RejectReason.OffQuotes;

                    case SoftFX.Net.OrderEntry.RejectReason.UnknownOrder:
                        return TickTrader.FDK.Common.RejectReason.UnknownOrder;

                    case SoftFX.Net.OrderEntry.RejectReason.DuplicateOrder:
                        return TickTrader.FDK.Common.RejectReason.DuplicateClientOrderId;

                    case SoftFX.Net.OrderEntry.RejectReason.IncorrectCharacteristics:
                        return TickTrader.FDK.Common.RejectReason.InvalidTradeRecordParameters;

                    case SoftFX.Net.OrderEntry.RejectReason.IncorrectQty:
                        return TickTrader.FDK.Common.RejectReason.IncorrectQuantity;

                    case SoftFX.Net.OrderEntry.RejectReason.TooLate:
                        return TickTrader.FDK.Common.RejectReason.Other;

                    case SoftFX.Net.OrderEntry.RejectReason.InternalServerError:
                        return TickTrader.FDK.Common.RejectReason.Other;

                    case SoftFX.Net.OrderEntry.RejectReason.Other:
                        return TickTrader.FDK.Common.RejectReason.Other;

                    default:
                        throw new Exception("Invalid order reject reason : " + reason);
                }
            }

            TickTrader.FDK.Common.NotificationType Convert(SoftFX.Net.OrderEntry.NotificationType type)
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

            TickTrader.FDK.Common.NotificationSeverity Convert(SoftFX.Net.OrderEntry.NotificationSeverity severity)
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

            Client client_;
        }

        static void ConvertToSync(Task task, int timeout)
        {
            try
            {
                if (!task.Wait(timeout))
                    throw new TimeoutException("Method call timeout");
            }
            catch (AggregateException ex)
            {
                ExceptionDispatchInfo.Capture(ex.Flatten().InnerExceptions[0]).Throw();
            }
        }

        static TResult ConvertToSync<TResult>(Task<TResult> task, int timeout)
        {
            try
            {
                if (!task.Wait(timeout))
                    throw new TimeoutException("Method call timeout");

                return task.Result;
            }
            catch (AggregateException ex)
            {
                ExceptionDispatchInfo.Capture(ex.Flatten().InnerExceptions[0]).Throw();
                // Unreacheble code...
                return default(TResult);
            }
        }

        #endregion
    }
}
