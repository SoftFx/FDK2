﻿using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using SoftFX.Net.OrderEntry;
using TickTrader.FDK.Objects;

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
            _session = new ClientSession(name, options);
            _sessionListener = new ClientSessionListener(this);
            _session.Listener = _sessionListener;
        }

        private readonly ClientSession _session;
        private readonly ClientSessionListener _sessionListener;

        #endregion

        #region IDisposable        

        public void Dispose()
        {
            Disconnect("Client disconnect");

            GC.SuppressFinalize(this);
        }

        #endregion

        #region Connect / disconnect

        public delegate void ConnectDelegate(Client client);
        public delegate void ConnectErrorDelegate(Client client, string text);
        public delegate void DisconnectDelegate(Client client, string text);

        public event ConnectDelegate ConnectEvent;
        public event ConnectErrorDelegate ConnectErrorEvent;
        public event DisconnectDelegate DisconnectEvent;

        public bool IsConnected { get; private set; }

        public void Connect(string address, int timeout)
        {
            _session.Connect(address);

            if (!_session.WaitConnect(timeout))
            {
                _session.Disconnect("Connect timeout");
                _session.Join();

                throw new TimeoutException("Connect timeout");
            }
        }

        public void ConnectAsync(string address)
        {
            _session.Connect(address);
        }

        public void Disconnect(string text)
        {
            _session.Disconnect(text);
            _session.Join();
        }

        public void DisconnectAsync(string text)
        {
            _session.Disconnect(text);
        }

        public void Join()
        {
            _session.Join();
        }

        #endregion

        #region Login / logout

        public delegate void LoginResultDelegate(Client client, object data);
        public delegate void LoginErrorDelegate(Client client, object data, string text);
        public delegate void OneTimePasswordRequestDelegate(Client client, string text);
        public delegate void OneTimePasswordRejectDelegate(Client client, string text);
        public delegate void LogoutResultDelegate(Client client, object data, LogoutInfo logoutInfo);
        public delegate void LogoutDelegate(Client client, LogoutInfo logoutInfo);

        public event LoginResultDelegate LoginResultEvent;
        public event LoginErrorDelegate LoginErrorEvent;
        public event OneTimePasswordRequestDelegate OneTimePasswordRequestEvent;
        public event OneTimePasswordRejectDelegate OneTimePasswordRejectEvent;
        public event LogoutResultDelegate LogoutResultEvent;
        public event LogoutDelegate LogoutEvent;

        public void Login(string username, string password, string deviceId, string appSessionId, int timeout)
        {
            ConvertToSync(LoginAsync(null, username, password, deviceId, appSessionId), timeout);
        }

        public Task LoginAsync(object data, string username, string password, string deviceId, string appSessionId)
        {
            // Create a new async context
            var context = new LoginAsyncContext();
            context.Data = data;

            // Create a request
            var request = new LoginRequest(0)
            {
                Username = username,
                Password = password,
                DeviceId = deviceId,
                AppSessionId = appSessionId
            };

            // Send request to the server
            _session.SendLoginRequest(context, request);

            // Return result task
            return context.Tcs.Task;
        }

        public void SendOneTimePassword(string oneTimePassword)
        {
            // Create a message
            var message = new TwoFactorLogin(0)
            {
                Reason = TwoFactorReason.ClientResponse,
                OneTimePassword = oneTimePassword
            };

            // Send message to the server
            _session.Send(message);
        }

        public LogoutInfo Logout(string message, int timeout)
        {
            return ConvertToSync(LogoutAsync(null, message), timeout);
        }

        public Task<LogoutInfo> LogoutAsync(object data, string message)
        {
            // Create a new async context
            var context = new LogoutAsyncContext();
            context.Data = data;

            // Create a request
            var request = new Logout(0)
            {
                Text = message
            };

            // Send request to the server
            _session.SendLogout(context, request);

            // Return result task
            return context.Tcs.Task;
        }

        #endregion

        #region Order Entry
                
        public delegate void TradeServerInfoResultDelegate(Client client, object data, TickTrader.FDK.Objects.TradeServerInfo info);
        public delegate void TradeServerInfoErrorDelegate(Client client, object data, string message);
        public delegate void AccountInfoResultDelegate(Client client, object data, TickTrader.FDK.Objects.AccountInfo info);
        public delegate void AccountInfoErrorDelegate(Client client, object data, string message);
        public delegate void SessionInfoResultDelegate(Client client, object data, TickTrader.FDK.Objects.SessionInfo info);
        public delegate void SessionInfoErrorDelegate(Client client, object data, string message);
        public delegate void OrdersResultDelegate(Client client, object data, TickTrader.FDK.Objects.ExecutionReport[] reports);
        public delegate void OrdersErrorDelegate(Client client, object data, string message);
        public delegate void PositionsResultDelegate(Client client, object data, TickTrader.FDK.Objects.Position[] positions);
        public delegate void PositionsErrorDelegate(Client client, object data, string message);
        public delegate void NewOrderResultDelegate(Client client, object data, TickTrader.FDK.Objects.ExecutionReport report);
        public delegate void NewOrderErrorDelegate(Client client, object data, string message);
        public delegate void ReplaceOrderResultDelegate(Client client, object data, TickTrader.FDK.Objects.ExecutionReport report);
        public delegate void ReplaceOrderErrorDelegate(Client client, object data, string message);
        public delegate void CancelOrderResultDelegate(Client client, object data, TickTrader.FDK.Objects.ExecutionReport report);
        public delegate void CancelOrderErrorDelegate(Client client, object data, string message);
        public delegate void ClosePositionResultDelegate(Client client, object data, TickTrader.FDK.Objects.ExecutionReport report);
        public delegate void ClosePositionErrorDelegate(Client client, object data, string message);
        public delegate void ClosePositionByResultDelegate(Client client, object data, TickTrader.FDK.Objects.ExecutionReport report);
        public delegate void ClosePositionByErrorDelegate(Client client, object data, string message);
        public delegate void ExecutionReportDelegate(Client client, TickTrader.FDK.Objects.ExecutionReport executionReport);
        public delegate void PositionUpdateDelegate(Client client, TickTrader.FDK.Objects.Position[] positions);
        public delegate void AccountInfoUpdateDelegate(Client client, TickTrader.FDK.Objects.AccountInfo accountInfo);
        public delegate void SessionInfoUpdateDelegate(Client client, SessionInfo sessionInfo);
        public delegate void BalanceInfoUpdateDelegate(Client client, BalanceOperation balanceOperation);
        public delegate void NotificationDelegate(Client client, TickTrader.FDK.Objects.Notification notification);
                
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
        public event ExecutionReportDelegate ExecutionReportEvent;
        public event PositionUpdateDelegate PositionUpdateEvent;
        public event AccountInfoUpdateDelegate AccountInfoUpdateEvent;
        public event SessionInfoUpdateDelegate SessionInfoUpdateEvent;
        public event BalanceInfoUpdateDelegate BalanceInfoUpdateEvent;
        public event NotificationDelegate NotificationEvent;

        public TickTrader.FDK.Objects.TradeServerInfo GetTradeServerInfo(int timeout)
        {
            return ConvertToSync(GetTradeServerInfoAsync(null), timeout);
        }

        public Task<TickTrader.FDK.Objects.TradeServerInfo> GetTradeServerInfoAsync(object data)
        {
            // Create a new async context
            var context = new TradeServerInfoAsyncContext();
            context.Data = data;

            // Create a request
            var request = new TradeServerInfoRequest(0)
            {
                Id = Guid.NewGuid().ToString()
            };

            // Send request to the server
            _session.SendTradeServerInfoRequest(context, request);

            // Return result task
            return context.Tcs.Task;
        }

        public TickTrader.FDK.Objects.AccountInfo GetAccountInfo(int timeout)
        {
            return ConvertToSync(GetAccountInfoAsync(null), timeout);
        }

        public Task<TickTrader.FDK.Objects.AccountInfo> GetAccountInfoAsync(object data)
        {
            // Create a new async context
            var context = new AccountInfoAsyncContext();
            context.Data = data;                 

            // Create a request
            var request = new AccountInfoRequest(0)
            {
                Id = Guid.NewGuid().ToString()
            };

            // Send request to the server
            _session.SendAccountInfoRequest(context, request);

            // Return result task
            return context.Tcs.Task;
        }

        public TickTrader.FDK.Objects.SessionInfo GetSessionInfo(int timeout)
        {
            return ConvertToSync(GetSessionInfoAsync(null), timeout);
        }

        public Task<TickTrader.FDK.Objects.SessionInfo> GetSessionInfoAsync(object data)
        {
            // Create a new async context
            var context = new SessionInfoAsyncContext();
            context.Data = data;

            // Create a request
            var request = new TradingSessionStatusRequest(0);
            request.Id = Guid.NewGuid().ToString();

            // Send request to the server
            _session.SendTradingSessionStatusRequest(context, request);

            // Return result task
            return context.Tcs.Task;
        }

        public TickTrader.FDK.Objects.ExecutionReport[] GetOrders(int timeout)
        {
            return ConvertToSync(GetOrdersAsync(null), timeout);
        }

        public Task<TickTrader.FDK.Objects.ExecutionReport[]> GetOrdersAsync(object data)
        {
            // Create a new async context
            var context = new OrdersAsyncContext();
            context.Data = data;

            // Create a request
            var request = new OrderMassStatusRequest(0);
            request.Id = Guid.NewGuid().ToString();
            request.Type = OrderMassStatusRequestType.All;

            // Send request to the server
            _session.SendOrderMassStatusRequest(context, request);

            // Return result task
            return context.Tcs.Task;
        }

        public TickTrader.FDK.Objects.Position[] GetPositions(int timeout)
        {
            return ConvertToSync(GetPositionsAsync(null), timeout);
        }

        public Task<TickTrader.FDK.Objects.Position[]> GetPositionsAsync(object data)
        {
            // Create a new async context
            var context = new PositionsAsyncContext();
            context.Data = data;

            // Create a request
            var request = new PositionListRequest(0);
            request.Id = Guid.NewGuid().ToString();
            request.Type = PositionListRequestType.All;

            // Send request to the server
            _session.SendPositionListRequest(context, request);

            // Return result task
            return context.Tcs.Task;
        }

        public TickTrader.FDK.Objects.ExecutionReport[] NewOrder
        (
            string clientOrderId,
            string symbol,
            TickTrader.FDK.Objects.OrderType type,
            TickTrader.FDK.Objects.OrderSide side,
            double qty,
            double? maxVisibleQty,
            double? price,
            double? stopPrice,
            TickTrader.FDK.Objects.OrderTimeInForce? time,
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
                    null,
                    clientOrderId,
                    symbol,
                    type,
                    side,
                    qty,
                    maxVisibleQty,
                    price,
                    stopPrice,
                    time,
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

        public Task<TickTrader.FDK.Objects.ExecutionReport[]> NewOrderAsync
        (
            object data,
            string clientOrderId,
            string symbol, 
            TickTrader.FDK.Objects.OrderType type, 
            TickTrader.FDK.Objects.OrderSide side,            
            double qty, 
            double? maxVisibleQty, 
            double? price, 
            double? stopPrice,
            TickTrader.FDK.Objects.OrderTimeInForce? time,      
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

            if (time.HasValue)
            {
                attributes.TimeInForce = Convert(time.Value);
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
            _session.SendNewOrderSingle(context, message);

            // Return result task
            return context.Tcs.Task;
        }

        public TickTrader.FDK.Objects.ExecutionReport[] ReplaceOrder
        (
            string clientOrderId,
            string origClientOrderId,
            string orderId,
            string symbol,
            TickTrader.FDK.Objects.OrderType type,
            TickTrader.FDK.Objects.OrderSide side,
            double qty,
            double? maxVisibleQty,
            double? price,
            double? stopPrice,
            TickTrader.FDK.Objects.OrderTimeInForce? time,
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
                    null,
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
                    time,
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

        public Task<TickTrader.FDK.Objects.ExecutionReport[]> ReplaceOrderAsync
        (
            object data,
            string clientOrderId,
            string origClientOrderId,
            string orderId,
            string symbol, 
            TickTrader.FDK.Objects.OrderType type, 
            TickTrader.FDK.Objects.OrderSide side,            
            double qty, 
            double? maxVisibleQty, 
            double? price, 
            double? stopPrice,       
            TickTrader.FDK.Objects.OrderTimeInForce? time,      
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

            if (time.HasValue)
            {
                attributes.TimeInForce = Convert(time.Value);
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
            _session.SendOrderCancelReplaceRequest(context, message);

            // Return result task
            return context.Tcs.Task;
        }

        public TickTrader.FDK.Objects.ExecutionReport[] CancelOrder(string clientOrderId, string origClientOrderId, string orderId, int timeout)
        {
            return ConvertToSync(CancelOrderAsync(null, clientOrderId, origClientOrderId, orderId), timeout);
        }

        public Task<TickTrader.FDK.Objects.ExecutionReport[]> CancelOrderAsync(object data, string clientOrderId, string origClientOrderId, string orderId)
        {
            // Create a new async context
            var context = new CancelOrderAsyncContext();
            context.Data = data;

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
            _session.SendOrderCancelRequest(context, message);

            // Return result task
            return context.Tcs.Task;
        }

        public TickTrader.FDK.Objects.ExecutionReport[] ClosePosition(string clientOrderId, string orderId, double? qty, int timeout)
        {
            return ConvertToSync(ClosePositionAsync(null, clientOrderId, orderId, qty), timeout);
        }

        public Task<TickTrader.FDK.Objects.ExecutionReport[]> ClosePositionAsync(object data, string clientOrderId, string orderId, double? qty)
        {
            // Create a new async context
            var context = new ClosePositionAsyncContext();
            context.Data = data;

            // Create a request
            ClosePositionRequest message = new ClosePositionRequest(0);
            message.ClOrdId = clientOrderId;
            message.OrderId = long.Parse(orderId);
            message.Type = ClosePositionRequestType.Close;
            message.Qty = qty;

            // Send request to the server
            _session.SendClosePositionRequest(context, message);

            // Return result task
            return context.Tcs.Task;
        }

        public TickTrader.FDK.Objects.ExecutionReport[] ClosePositionBy(string clientOrderId, string orderId, string byOrderId, int timeout)
        {
            return ConvertToSync(ClosePositionByAsync(null, clientOrderId, orderId, byOrderId), timeout);
        }

        public Task<TickTrader.FDK.Objects.ExecutionReport[]> ClosePositionByAsync(object data, string clientOrderId, string orderId, string byOrderId)
        {
            // Create a new async context
            var context = new ClosePositionByAsyncContext();
            context.Data = data;

            // Create a request
            ClosePositionRequest message = new ClosePositionRequest(0);
            message.ClOrdId = clientOrderId;
            message.OrderId = long.Parse(orderId);
            message.Type = ClosePositionRequestType.CloseBy;
            message.ByOrderId = long.Parse(byOrderId);

            // Send request to the server
            _session.SendClosePositionByRequest(context, message);

            // Return result task
            return context.Tcs.Task;
        }

        SoftFX.Net.OrderEntry.OrderType Convert(TickTrader.FDK.Objects.OrderType type)
        {
            switch (type)
            {
                case TickTrader.FDK.Objects.OrderType.Market:
                    return SoftFX.Net.OrderEntry.OrderType.Market;

                case TickTrader.FDK.Objects.OrderType.MarketWithSlippage:
                    return SoftFX.Net.OrderEntry.OrderType.MarketWithSlippage;

                case TickTrader.FDK.Objects.OrderType.Limit:
                    return SoftFX.Net.OrderEntry.OrderType.Limit;

                case TickTrader.FDK.Objects.OrderType.Stop:
                    return SoftFX.Net.OrderEntry.OrderType.Stop;

                case TickTrader.FDK.Objects.OrderType.Position:
                    return SoftFX.Net.OrderEntry.OrderType.Position;

                case TickTrader.FDK.Objects.OrderType.StopLimit:
                    return SoftFX.Net.OrderEntry.OrderType.StopLimit;

                default:
                    throw new Exception("Invalid order type : " + type);
            }
        }

        SoftFX.Net.OrderEntry.OrderSide Convert(TickTrader.FDK.Objects.OrderSide side)
        {
            switch (side)
            {
                case TickTrader.FDK.Objects.OrderSide.Buy:
                    return SoftFX.Net.OrderEntry.OrderSide.Buy;

                case TickTrader.FDK.Objects.OrderSide.Sell:
                    return SoftFX.Net.OrderEntry.OrderSide.Sell;

                default:
                    throw new Exception("Invalid order side : " + side);
            }
        }

        SoftFX.Net.OrderEntry.OrderTimeInForce Convert(TickTrader.FDK.Objects.OrderTimeInForce time)
        {
            switch (time)
            {
                case TickTrader.FDK.Objects.OrderTimeInForce.GoodTillCancel:
                    return SoftFX.Net.OrderEntry.OrderTimeInForce.GoodTillCancel;

                case TickTrader.FDK.Objects.OrderTimeInForce.ImmediateOrCancel:
                    return SoftFX.Net.OrderEntry.OrderTimeInForce.ImmediateOrCancel;

                case TickTrader.FDK.Objects.OrderTimeInForce.GoodTillDate:
                    return SoftFX.Net.OrderEntry.OrderTimeInForce.GoodTillDate;

                default:
                    throw new Exception("Invalid order time : " + time);
            }
        }

        #endregion

        #region Async contexts

        private interface IAsyncContext
        {
            void SetException(Exception ex);
        }

        private class LoginAsyncContext : LoginRequestClientContext, IAsyncContext
        {
            public LoginAsyncContext() : base(false) { }

            public void SetException(Exception ex) { Tcs.SetException(ex); }

            public readonly TaskCompletionSource<object> Tcs = new TaskCompletionSource<object>();
        }

        private class LogoutAsyncContext : LogoutClientContext, IAsyncContext
        {
            public LogoutAsyncContext() : base(false) { }

            public void SetException(Exception ex) { Tcs.SetException(ex); }

            public readonly TaskCompletionSource<LogoutInfo> Tcs = new TaskCompletionSource<LogoutInfo>();
        }

        private class TradeServerInfoAsyncContext : TradeServerInfoRequestClientContext, IAsyncContext
        {
            public TradeServerInfoAsyncContext() : base(false) { }

            public void SetException(Exception ex) { Tcs.SetException(ex); }

            public readonly TaskCompletionSource<TradeServerInfo> Tcs = new TaskCompletionSource<TradeServerInfo>();
        }

        private class AccountInfoAsyncContext : AccountInfoRequestClientContext, IAsyncContext
        {
            public AccountInfoAsyncContext() : base(false) { }

            public void SetException(Exception ex) { Tcs.SetException(ex); }

            public readonly TaskCompletionSource<TickTrader.FDK.Objects.AccountInfo> Tcs = new TaskCompletionSource<TickTrader.FDK.Objects.AccountInfo>();
        }

        private class SessionInfoAsyncContext : TradingSessionStatusRequestClientContext, IAsyncContext
        {
            public SessionInfoAsyncContext() : base(false) { }

            public void SetException(Exception ex) { Tcs.SetException(ex); }

            public readonly TaskCompletionSource<TickTrader.FDK.Objects.SessionInfo> Tcs = new TaskCompletionSource<TickTrader.FDK.Objects.SessionInfo>();
        }

        private class OrdersAsyncContext : OrderMassStatusRequestClientContext, IAsyncContext
        {
            public OrdersAsyncContext() : base(false) { }

            public void SetException(Exception ex) { Tcs.SetException(ex); }

            public readonly TaskCompletionSource<TickTrader.FDK.Objects.ExecutionReport[]> Tcs = new TaskCompletionSource<TickTrader.FDK.Objects.ExecutionReport[]>();
        }

        private class PositionsAsyncContext : PositionListRequestClientContext, IAsyncContext
        {
            public PositionsAsyncContext() : base(false) { }

            public void SetException(Exception ex) { Tcs.SetException(ex); }

            public readonly TaskCompletionSource<TickTrader.FDK.Objects.Position[]> Tcs = new TaskCompletionSource<TickTrader.FDK.Objects.Position[]>();
        }

        private class NewOrderAsyncContext : NewOrderSingleClientContext, IAsyncContext
        {
            public NewOrderAsyncContext() : base(false) { }

            public void SetException(Exception ex) { Tcs.SetException(ex); }

            public readonly TaskCompletionSource<TickTrader.FDK.Objects.ExecutionReport[]> Tcs = new TaskCompletionSource<TickTrader.FDK.Objects.ExecutionReport[]>();

            public List<TickTrader.FDK.Objects.ExecutionReport> ExecutionReportList = new List<TickTrader.FDK.Objects.ExecutionReport>();
        }

        private class ReplaceOrderAsyncContext : OrderCancelReplaceRequestClientContext, IAsyncContext
        {
            public ReplaceOrderAsyncContext() : base(false) { }

            public void SetException(Exception ex) { Tcs.SetException(ex); }

            public readonly TaskCompletionSource<TickTrader.FDK.Objects.ExecutionReport[]> Tcs = new TaskCompletionSource<TickTrader.FDK.Objects.ExecutionReport[]>();

            public List<TickTrader.FDK.Objects.ExecutionReport> ExecutionReportList = new List<TickTrader.FDK.Objects.ExecutionReport>();
        }

        private class CancelOrderAsyncContext : OrderCancelRequestClientContext, IAsyncContext
        {
            public CancelOrderAsyncContext() : base(false) { }

            public void SetException(Exception ex) { Tcs.SetException(ex); }

            public readonly TaskCompletionSource<TickTrader.FDK.Objects.ExecutionReport[]> Tcs = new TaskCompletionSource<TickTrader.FDK.Objects.ExecutionReport[]>();

            public List<TickTrader.FDK.Objects.ExecutionReport> ExecutionReportList = new List<TickTrader.FDK.Objects.ExecutionReport>();
        }

        private class ClosePositionAsyncContext : ClosePositionRequestClientContext, IAsyncContext
        {
            public ClosePositionAsyncContext() : base(false) { }

            public void SetException(Exception ex) { Tcs.SetException(ex); }

            public readonly TaskCompletionSource<TickTrader.FDK.Objects.ExecutionReport[]> Tcs = new TaskCompletionSource<TickTrader.FDK.Objects.ExecutionReport[]>();

            public List<TickTrader.FDK.Objects.ExecutionReport> ExecutionReportList = new List<TickTrader.FDK.Objects.ExecutionReport>();
        }

        private class ClosePositionByAsyncContext : ClosePositionByRequestClientContext, IAsyncContext
        {
            public ClosePositionByAsyncContext() : base(false) { }

            public void SetException(Exception ex) { Tcs.SetException(ex); }

            public readonly TaskCompletionSource<TickTrader.FDK.Objects.ExecutionReport[]> Tcs = new TaskCompletionSource<TickTrader.FDK.Objects.ExecutionReport[]>();

            public List<TickTrader.FDK.Objects.ExecutionReport> ExecutionReportList = new List<TickTrader.FDK.Objects.ExecutionReport>();
        }

        #endregion

        #region Session listener

        private class ClientSessionListener : SoftFX.Net.OrderEntry.ClientSessionListener
        {
            public ClientSessionListener(Client client)
            {
                client_ = client;
            }

            public override void OnConnect(ClientSession clientSession)
            {
                try
                {
                    client_.IsConnected = true;

                    if (client_.ConnectEvent != null)
                    {
                        try
                        {
                            client_.ConnectEvent(client_);
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

            public override void OnConnectError(ClientSession clientSession)
            {
                try
                {
                    client_.IsConnected = false;

                    if (client_.ConnectErrorEvent != null)
                    {
                        try
                        {
                            // TODO: text
                            client_.ConnectErrorEvent(client_, "Connect error");
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

            public override void OnDisconnect(ClientSession clientSession, ClientContext[] contexts, string text)
            {
                try
                {
                    string message = "Client disconnected";
                    if (text != null)
                    {
                        message += " : ";
                        message += text;
                    }
                    Exception exception = new Exception(message);

                    foreach (ClientContext context in contexts)
                        ((IAsyncContext)context).SetException(exception);

                    client_.IsConnected = false;

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

                    context.Tcs.SetResult(null);
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

                    context.Tcs.SetException(exception);
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
                    
                    var exception = new Exception(text);
                    context.Tcs.SetException(exception);
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

                    context.Tcs.SetException(exception);
                }
            }

            public override void OnTwoFactorLoginRequest(ClientSession session, LoginRequestClientContext LoginRequestClientContext, TwoFactorLogin message)
            {
                var context = (LoginAsyncContext) LoginRequestClientContext;

                try
                {
                    string text = message.Text;

                    if (client_.OneTimePasswordRequestEvent != null)
                    {
                        try
                        {
                            client_.OneTimePasswordRequestEvent(client_, text);
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

                    context.Tcs.SetException(exception);
                }
            }

            public override void OnTwoFactorLoginSuccess(ClientSession session, LoginRequestClientContext LoginRequestClientContext, TwoFactorLogin message)
            {
                var context = (LoginAsyncContext) LoginRequestClientContext;

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

                    context.Tcs.SetResult(null);
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

                    context.Tcs.SetException(exception);
                }
            }

            public override void OnTwoFactorLoginReject(ClientSession session, LoginRequestClientContext LoginRequestClientContext, TwoFactorReject message)
            {
                var context = (LoginAsyncContext) LoginRequestClientContext;

                try
                {
                    string text = message.Text;

                    if (client_.OneTimePasswordRejectEvent != null)
                    {
                        try
                        {
                            client_.OneTimePasswordRejectEvent(client_, text);
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

                    context.Tcs.SetException(exception);
                }
            }

            public override void OnTwoFactorLoginError(ClientSession session, LoginRequestClientContext LoginRequestClientContext, TwoFactorLogin message)
            {
                var context = (LoginAsyncContext) LoginRequestClientContext;

                try
                {
                    string text = message.Text;

                    if (client_.LoginErrorEvent != null)
                    {
                        try
                        {
                            client_.LoginErrorEvent(client_, LoginRequestClientContext.Data, text);
                        }
                        catch
                        {
                        }
                    }

                    var exception = new Exception(text);
                    context.Tcs.SetException(exception);
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

                    context.Tcs.SetException(exception);
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

                    context.Tcs.SetResult(result);
                }
                catch
                {
                    // on logout we don't throw

                    var result = new LogoutInfo();
                    result.Reason = TickTrader.FDK.Objects.LogoutReason.Unknown;

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

                    context.Tcs.SetResult(result);
                }
            }

            public override void OnTradeServerInfoReport(ClientSession session, TradeServerInfoRequestClientContext TradeServerInfoRequestClientContext, TradeServerInfoReport message)
            {
                var context = (TradeServerInfoAsyncContext) TradeServerInfoRequestClientContext;

                try
                {
                    TickTrader.FDK.Objects.TradeServerInfo resultTradeServerInfo = new TickTrader.FDK.Objects.TradeServerInfo();
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

                    context.Tcs.SetResult(resultTradeServerInfo);
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

                    context.Tcs.SetException(exception);
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

                    var exception = new Exception(text);
                    context.Tcs.SetException(exception);
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

                    context.Tcs.SetException(exception);
                }
            }

            public override void OnAccountInfoReport(ClientSession session, AccountInfoRequestClientContext AccountInfoRequestClientContext, AccountInfoReport message)
            {
                var context = (AccountInfoAsyncContext) AccountInfoRequestClientContext;

                try
                {
                    TickTrader.FDK.Objects.AccountInfo resultAccountInfo = new TickTrader.FDK.Objects.AccountInfo();
                    SoftFX.Net.OrderEntry.AccountInfo reportAccountInfo = message.AccountInfo;
                    resultAccountInfo.AccountId = reportAccountInfo.Id.ToString();                    
                    resultAccountInfo.Type = Convert(reportAccountInfo.Type);
                    resultAccountInfo.Email = reportAccountInfo.RegistEmail;
                    resultAccountInfo.Comment = reportAccountInfo.Description;
                    resultAccountInfo.Currency = reportAccountInfo.CurrId;
                    resultAccountInfo.RegistredDate = reportAccountInfo.RegistDate;
                    resultAccountInfo.Leverage = reportAccountInfo.Leverage;
                    resultAccountInfo.Balance = reportAccountInfo.Balance;
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

                    AccountAssetArray reportAssets = reportAccountInfo.Assets;                   

                    int count = reportAssets.Length;
                    AssetInfo[] resultAssets = new AssetInfo[count];

                    for (int index = 0; index < count; ++index)
                    {
                        AccountAsset reportAsset = reportAssets[index];

                        AssetInfo resultAsset = new AssetInfo();
                        resultAsset.Currency = reportAsset.CurrId;
                        resultAsset.Balance = reportAsset.Balance;
                        resultAsset.LockedAmount = reportAsset.Locked;

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

                    context.Tcs.SetResult(resultAccountInfo);
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

                    context.Tcs.SetException(exception);
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

                    var exception = new Exception(text);
                    context.Tcs.SetException(exception);
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

                    context.Tcs.SetException(exception);
                }
            }

            public override void OnTradingSessionStatusReport(ClientSession session, TradingSessionStatusRequestClientContext TradingSessionStatusRequestClientContext, TradingSessionStatusReport message)
            {
                var context = (SessionInfoAsyncContext) TradingSessionStatusRequestClientContext;

                try
                {
                    TickTrader.FDK.Objects.SessionInfo resultStatusInfo = new TickTrader.FDK.Objects.SessionInfo();
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

                    context.Tcs.SetResult(resultStatusInfo);
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

                    context.Tcs.SetException(exception);
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

                    var exception = new Exception(text);
                    context.Tcs.SetException(exception);
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

                    context.Tcs.SetException(exception);
                }
            }

            public override void OnOrderMassStatusReport(ClientSession session, OrderMassStatusRequestClientContext OrderMassStatusRequestClientContext, OrderMassStatusReport message)
            {
                var context = (OrdersAsyncContext) OrderMassStatusRequestClientContext;

                try
                {
                    SoftFX.Net.OrderEntry.OrderMassStatusEntryArray reportEntries = message.Entries;
                    int count = reportEntries.Length;
                    TickTrader.FDK.Objects.ExecutionReport[] resultExecutionReports = new TickTrader.FDK.Objects.ExecutionReport[count];

                    for (int index = 0; index < count; ++index)
                    {
                        SoftFX.Net.OrderEntry.OrderMassStatusEntry reportEntry = reportEntries[index];
                        SoftFX.Net.OrderEntry.OrderAttributes reportEntryAttributes = reportEntry.Attributes;
                        SoftFX.Net.OrderEntry.OrderState reportEntryState = reportEntry.State;

                        TickTrader.FDK.Objects.ExecutionReport resultExecutionReport = new TickTrader.FDK.Objects.ExecutionReport();
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
                        resultExecutionReport.OrderStatus = Convert(reportEntryState.Status); 
                        resultExecutionReport.ExecutedVolume = reportEntryState.CumQty;
                        resultExecutionReport.LeavesVolume = reportEntryState.LeavesQty;
                        resultExecutionReport.TradeAmount = reportEntryState.LastQty;
                        resultExecutionReport.TradePrice = reportEntryState.LastPrice;
                        resultExecutionReport.Commission = reportEntryState.Commission;
                        resultExecutionReport.AgentCommission = reportEntryState.AgentCommission;
                        resultExecutionReport.Swap = reportEntryState.Swap;                        
                        resultExecutionReport.AveragePrice = reportEntryState.AvgPrice;                        
                        resultExecutionReport.Created = reportEntryState.Created;
                        resultExecutionReport.Modified = reportEntryState.Modified;
                        resultExecutionReport.RejectReason = RejectReason.None;
                        resultExecutionReport.Comment = reportEntryAttributes.Comment;
                        resultExecutionReport.Tag = reportEntryAttributes.Tag;
                        resultExecutionReport.Magic = reportEntryAttributes.Magic;
#pragma warning disable 618
                        resultExecutionReport.TradeRecordSide = ConvertToTradeRecordSide(reportEntryAttributes.Side);
                        resultExecutionReport.TradeRecordType = ConvertToTradeRecordType(reportEntryAttributes.Type, reportEntryAttributes.TimeInForce);
#pragma warning restore 618
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

                    context.Tcs.SetResult(resultExecutionReports);
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

                    context.Tcs.SetException(exception);
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

                    var exception = new Exception(text);
                    context.Tcs.SetException(exception);
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

                    context.Tcs.SetException(exception);
                }
            }

            public override void OnPositionListReport(ClientSession session, PositionListRequestClientContext PositionListRequestClientContext, PositionListReport message)
            {
                var context = (PositionsAsyncContext) PositionListRequestClientContext;

                try
                {
                    SoftFX.Net.OrderEntry.PositionArray reportPositions = message.Positions;
                    int count = reportPositions.Length;
                    TickTrader.FDK.Objects.Position[] resultPositions = new TickTrader.FDK.Objects.Position[count];

                    for (int index = 0; index < count; ++index)
                    {
                        SoftFX.Net.OrderEntry.Position reportPosition = reportPositions[index];

                        TickTrader.FDK.Objects.Position resultPosition = new TickTrader.FDK.Objects.Position();
                        resultPosition.Symbol = reportPosition.SymbolId;
                        resultPosition.SettlementPrice = reportPosition.SettltPrice;
                        resultPosition.BuyAmount = reportPosition.LongQty;
                        resultPosition.SellAmount = reportPosition.ShortQty;
                        resultPosition.Commission = reportPosition.Commission;
                        resultPosition.AgentCommission = reportPosition.AgentCommission;
                        resultPosition.Swap = reportPosition.Swap;
                        resultPosition.BuyPrice = reportPosition.LongPrice;
                        resultPosition.SellPrice = reportPosition.ShortPrice;                        

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

                    context.Tcs.SetResult(resultPositions);
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

                    context.Tcs.SetException(exception);
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

                    var exception = new Exception(text);
                    context.Tcs.SetException(exception);
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

                    context.Tcs.SetException(exception);
                }
            }

            public override void OnExecutionReportNewMarket(ClientSession session, NewOrderSingleClientContext NewOrderSingleClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                var context = (NewOrderAsyncContext) NewOrderSingleClientContext;

                try
                {
                    TickTrader.FDK.Objects.ExecutionReport result = Convert(message);

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

                    context.ExecutionReportList.Add(result);
                }
                catch (Exception exception)
                {
                    if (client_.NewOrderErrorEvent != null)
                    {
                        try
                        {
                            client_.NewOrderErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    context.Tcs.SetException(exception);
                }
            }

            public override void OnExecutionReportTrade(ClientSession session, NewOrderSingleClientContext NewOrderSingleClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                var context = (NewOrderAsyncContext) NewOrderSingleClientContext;

                try
                {
                    TickTrader.FDK.Objects.ExecutionReport result = Convert(message);

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

                    context.ExecutionReportList.Add(result);
                    context.Tcs.SetResult(context.ExecutionReportList.ToArray());
                }
                catch (Exception exception)
                {
                    if (client_.NewOrderErrorEvent != null)
                    {
                        try
                        {
                            client_.NewOrderErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    context.Tcs.SetException(exception);
                }
            }

            public override void OnExecutionReportNew(ClientSession session, NewOrderSingleClientContext NewOrderSingleClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                var context = (NewOrderAsyncContext) NewOrderSingleClientContext;

                try
                {
                    TickTrader.FDK.Objects.ExecutionReport result = Convert(message);

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

                    context.ExecutionReportList.Add(result);
                }
                catch (Exception exception)
                {
                    if (client_.NewOrderErrorEvent != null)
                    {
                        try
                        {
                            client_.NewOrderErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    context.Tcs.SetException(exception);
                }                
            }

            public override void OnExecutionReportCalculated(ClientSession session, NewOrderSingleClientContext NewOrderSingleClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                var context = (NewOrderAsyncContext) NewOrderSingleClientContext;

                try
                {
                    TickTrader.FDK.Objects.ExecutionReport result = Convert(message);

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

                    context.ExecutionReportList.Add(result);
                    context.Tcs.SetResult(context.ExecutionReportList.ToArray());
                }
                catch (Exception exception)
                {
                    if (client_.NewOrderErrorEvent != null)
                    {
                        try
                        {
                            client_.NewOrderErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    context.Tcs.SetException(exception);
                }
            }

            public override void OnNewOrderSingleReject1(ClientSession session, NewOrderSingleClientContext NewOrderSingleClientContext, OrderReject message)
            {
                var context = (NewOrderAsyncContext) NewOrderSingleClientContext;

                try
                {
                    string text = message.Text;

                    if (client_.NewOrderErrorEvent != null)
                    {
                        try
                        {
                            client_.NewOrderErrorEvent(client_, context.Data, text);
                        }
                        catch
                        {
                        }
                    }

                    var exception = new Exception(text);
                    context.Tcs.SetException(exception);
                }
                catch (Exception exception)
                {
                    if (client_.NewOrderErrorEvent != null)
                    {
                        try
                        {
                            client_.NewOrderErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    context.Tcs.SetException(exception);
                }
            }

            public override void OnNewOrderSingleReject2(ClientSession session, NewOrderSingleClientContext NewOrderSingleClientContext, OrderReject message)
            {
                var context = (NewOrderAsyncContext) NewOrderSingleClientContext;

                try
                {
                    string text = message.Text;

                    if (client_.NewOrderErrorEvent != null)
                    {
                        try
                        {
                            client_.NewOrderErrorEvent(client_, context.Data, text);
                        }
                        catch
                        {
                        }
                    }

                    var exception = new Exception(text);
                    context.Tcs.SetException(exception);
                }
                catch (Exception exception)
                {
                    if (client_.NewOrderErrorEvent != null)
                    {
                        try
                        {
                            client_.NewOrderErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    context.Tcs.SetException(exception);
                }
            }

            public override void OnNewOrderSingleReject3(ClientSession session, NewOrderSingleClientContext NewOrderSingleClientContext, OrderReject message)
            {
                var context = (NewOrderAsyncContext) NewOrderSingleClientContext;

                try
                {
                    string text = message.Text;

                    if (client_.NewOrderErrorEvent != null)
                    {
                        try
                        {
                            client_.NewOrderErrorEvent(client_, context.Data, text);
                        }
                        catch
                        {
                        }
                    }

                    var exception = new Exception(text);
                    context.Tcs.SetException(exception);
                }
                catch (Exception exception)
                {
                    if (client_.NewOrderErrorEvent != null)
                    {
                        try
                        {
                            client_.NewOrderErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    context.Tcs.SetException(exception);
                }
            }

            public override void OnExecutionReportPendingReplace(ClientSession session, OrderCancelReplaceRequestClientContext OrderCancelReplaceRequestClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                var context = (ReplaceOrderAsyncContext) OrderCancelReplaceRequestClientContext;

                try
                {
                    TickTrader.FDK.Objects.ExecutionReport result = Convert(message);

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

                    context.ExecutionReportList.Add(result);
                }
                catch (Exception exception)
                {
                    if (client_.ReplaceOrderErrorEvent != null)
                    {
                        try
                        {
                            client_.ReplaceOrderErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    context.Tcs.SetException(exception);
                }
            }
            public override void OnExecutionReportReplaced(ClientSession session, OrderCancelReplaceRequestClientContext OrderCancelReplaceRequestClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                var context = (ReplaceOrderAsyncContext) OrderCancelReplaceRequestClientContext;

                try
                {
                    TickTrader.FDK.Objects.ExecutionReport result = Convert(message);

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

                    context.ExecutionReportList.Add(result);
                    context.Tcs.SetResult(context.ExecutionReportList.ToArray());
                }
                catch (Exception exception)
                {
                    if (client_.ReplaceOrderErrorEvent != null)
                    {
                        try
                        {
                            client_.ReplaceOrderErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    context.Tcs.SetException(exception);
                }
            }

            public override void OnOrderCancelReplaceReject1(ClientSession session, OrderCancelReplaceRequestClientContext OrderCancelReplaceRequestClientContext, OrderReject message)
            {
                var context = (ReplaceOrderAsyncContext) OrderCancelReplaceRequestClientContext;

                try
                {
                    string text = message.Text;

                    if (client_.ReplaceOrderErrorEvent != null)
                    {
                        try
                        {
                            client_.ReplaceOrderErrorEvent(client_, context.Data, text);
                        }
                        catch
                        {
                        }
                    }

                    var exception = new Exception(text);
                    context.Tcs.SetException(exception);
                }
                catch (Exception exception)
                {
                    if (client_.ReplaceOrderErrorEvent != null)
                    {
                        try
                        {
                            client_.ReplaceOrderErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    context.Tcs.SetException(exception);
                }
            }

            public override void OnOrderCancelReplaceReject2(ClientSession session, OrderCancelReplaceRequestClientContext OrderCancelReplaceRequestClientContext, OrderReject message)
            {
                var context = (ReplaceOrderAsyncContext) OrderCancelReplaceRequestClientContext;

                try
                {
                    string text = message.Text;

                    if (client_.ReplaceOrderErrorEvent != null)
                    {
                        try
                        {
                            client_.ReplaceOrderErrorEvent(client_, context.Data, text);
                        }
                        catch
                        {
                        }
                    }

                    var exception = new Exception(text);
                    context.Tcs.SetException(exception);
                }
                catch (Exception exception)
                {
                    if (client_.ReplaceOrderErrorEvent != null)
                    {
                        try
                        {
                            client_.ReplaceOrderErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    context.Tcs.SetException(exception);
                }
            }

            public override void OnExecutionReportPendingCancel(ClientSession session, OrderCancelRequestClientContext OrderCancelRequestClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                var context = (CancelOrderAsyncContext) OrderCancelRequestClientContext;

                try
                {
                    TickTrader.FDK.Objects.ExecutionReport result = Convert(message);

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

                    context.ExecutionReportList.Add(result);
                }
                catch (Exception exception)
                {
                    if (client_.CancelOrderErrorEvent != null)
                    {
                        try
                        {
                            client_.CancelOrderErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    context.Tcs.SetException(exception);
                }
            }

            public override void OnExecutionReportCancelled(ClientSession session, OrderCancelRequestClientContext OrderCancelRequestClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                var context = (CancelOrderAsyncContext) OrderCancelRequestClientContext;

                try
                {
                    TickTrader.FDK.Objects.ExecutionReport result = Convert(message);

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

                    context.ExecutionReportList.Add(result);
                    context.Tcs.SetResult(context.ExecutionReportList.ToArray());
                }
                catch (Exception exception)
                {
                    if (client_.CancelOrderErrorEvent != null)
                    {
                        try
                        {
                            client_.CancelOrderErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    context.Tcs.SetException(exception);
                }
            }

            public override void OnOrderCancelReject1(ClientSession session, OrderCancelRequestClientContext OrderCancelRequestClientContext, OrderReject message)
            {
                var context = (CancelOrderAsyncContext) OrderCancelRequestClientContext;

                try
                {
                    string text = message.Text;

                    if (client_.CancelOrderErrorEvent != null)
                    {
                        try
                        {
                            client_.CancelOrderErrorEvent(client_, context.Data, text);
                        }
                        catch
                        {
                        }
                    }

                    var exception = new Exception(text);
                    context.Tcs.SetException(exception);
                }
                catch (Exception exception)
                {
                    if (client_.CancelOrderErrorEvent != null)
                    {
                        try
                        {
                            client_.CancelOrderErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    context.Tcs.SetException(exception);
                }
            }

            public override void OnOrderCancelReject2(ClientSession session, OrderCancelRequestClientContext OrderCancelRequestClientContext, OrderReject message)
            {
                var context = (CancelOrderAsyncContext) OrderCancelRequestClientContext;

                try
                {
                    string text = message.Text;

                    if (client_.CancelOrderErrorEvent != null)
                    {
                        try
                        {
                            client_.CancelOrderErrorEvent(client_, context.Data, text);
                        }
                        catch
                        {
                        }
                    }

                    var exception = new Exception(text);
                    context.Tcs.SetException(exception);
                }
                catch (Exception exception)
                {
                    if (client_.CancelOrderErrorEvent != null)
                    {
                        try
                        {
                            client_.CancelOrderErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    context.Tcs.SetException(exception);
                }
            }

            public override void OnExecutionReportPendingClose(ClientSession session, ClosePositionRequestClientContext ClosePositionRequestClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                var context = (ClosePositionAsyncContext) ClosePositionRequestClientContext;

                try
                {
                    TickTrader.FDK.Objects.ExecutionReport result = Convert(message);

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

                    context.ExecutionReportList.Add(result);
                }
                catch (Exception exception)
                {
                    if (client_.ClosePositionErrorEvent != null)
                    {
                        try
                        {
                            client_.ClosePositionErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    context.Tcs.SetException(exception);
                }
            }

            public override void OnExecutionReportTradePartial(ClientSession session, ClosePositionRequestClientContext ClosePositionRequestClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                var context = (ClosePositionAsyncContext) ClosePositionRequestClientContext;

                try
                {
                    TickTrader.FDK.Objects.ExecutionReport result = Convert(message);

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

                    context.ExecutionReportList.Add(result);
                }
                catch (Exception exception)
                {
                    if (client_.ClosePositionErrorEvent != null)
                    {
                        try
                        {
                            client_.ClosePositionErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    context.Tcs.SetException(exception);
                }
            }

            public override void OnExecutionReportTradeCanclulated(ClientSession session, ClosePositionRequestClientContext ClosePositionRequestClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                var context = (ClosePositionAsyncContext) ClosePositionRequestClientContext;

                try
                {
                    TickTrader.FDK.Objects.ExecutionReport result = Convert(message);

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

                    context.ExecutionReportList.Add(result);
                    context.Tcs.SetResult(context.ExecutionReportList.ToArray());
                }
                catch (Exception exception)
                {
                    if (client_.ClosePositionErrorEvent != null)
                    {
                        try
                        {
                            client_.ClosePositionErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    context.Tcs.SetException(exception);
                }
            }

            public override void OnExecutionReportTrade(ClientSession session, ClosePositionRequestClientContext ClosePositionRequestClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                var context = (ClosePositionAsyncContext) ClosePositionRequestClientContext;

                try
                {
                    TickTrader.FDK.Objects.ExecutionReport result = Convert(message);

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

                    context.ExecutionReportList.Add(result);
                    context.Tcs.SetResult(context.ExecutionReportList.ToArray());
                }
                catch (Exception exception)
                {
                    if (client_.ClosePositionErrorEvent != null)
                    {
                        try
                        {
                            client_.ClosePositionErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    context.Tcs.SetException(exception);
                }
            }

            public override void OnClosePositionReject1(ClientSession session, ClosePositionRequestClientContext ClosePositionRequestClientContext, OrderReject message)
            {
                var context = (ClosePositionAsyncContext) ClosePositionRequestClientContext;

                try
                {
                    string text = message.Text;

                    if (client_.ClosePositionErrorEvent != null)
                    {
                        try
                        {
                            client_.ClosePositionErrorEvent(client_, context.Data, text);
                        }
                        catch
                        {
                        }
                    }

                    var exception = new Exception(text);
                    context.Tcs.SetException(exception);
                }
                catch (Exception exception)
                {
                    if (client_.ClosePositionErrorEvent != null)
                    {
                        try
                        {
                            client_.ClosePositionErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    context.Tcs.SetException(exception);
                }
            }

            public override void OnClosePositionReject2(ClientSession session, ClosePositionRequestClientContext ClosePositionRequestClientContext, OrderReject message)
            {
                var context = (ClosePositionAsyncContext) ClosePositionRequestClientContext;

                try
                {
                    string text = message.Text;

                    if (client_.ClosePositionErrorEvent != null)
                    {
                        try
                        {
                            client_.ClosePositionErrorEvent(client_, context.Data, text);
                        }
                        catch
                        {
                        }
                    }

                    var exception = new Exception(text);
                    context.Tcs.SetException(exception);
                }
                catch (Exception exception)
                {
                    if (client_.ClosePositionErrorEvent != null)
                    {
                        try
                        {
                            client_.ClosePositionErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    context.Tcs.SetException(exception);
                }
            }

            public override void OnClosePositionReject3(ClientSession session, ClosePositionRequestClientContext ClosePositionRequestClientContext, OrderReject message)
            {
                var context = (ClosePositionAsyncContext) ClosePositionRequestClientContext;

                try
                {
                    string text = message.Text;

                    if (client_.ClosePositionErrorEvent != null)
                    {
                        try
                        {
                            client_.ClosePositionErrorEvent(client_, context.Data, text);
                        }
                        catch
                        {
                        }
                    }

                    var exception = new Exception(text);
                    context.Tcs.SetException(exception);
                }
                catch (Exception exception)
                {
                    if (client_.ClosePositionErrorEvent != null)
                    {
                        try
                        {
                            client_.ClosePositionErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    context.Tcs.SetException(exception);
                }
            }

            public override void OnClosePositionReject4(ClientSession session, ClosePositionRequestClientContext ClosePositionRequestClientContext, OrderReject message)
            {
                var context = (ClosePositionAsyncContext) ClosePositionRequestClientContext;

                try
                {
                    string text = message.Text;

                    if (client_.ClosePositionErrorEvent != null)
                    {
                        try
                        {
                            client_.ClosePositionErrorEvent(client_, context.Data, text);
                        }
                        catch
                        {
                        }
                    }

                    var exception = new Exception(text);
                    context.Tcs.SetException(exception);
                }
                catch (Exception exception)
                {
                    if (client_.ClosePositionErrorEvent != null)
                    {
                        try
                        {
                            client_.ClosePositionErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    context.Tcs.SetException(exception);
                }
            }

            public override void OnExecutionReportCalculated(ClientSession session, ClosePositionByRequestClientContext ClosePositionByRequestClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                var context = (ClosePositionByAsyncContext) ClosePositionByRequestClientContext;

                try
                {
                    TickTrader.FDK.Objects.ExecutionReport result = Convert(message);

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

                    context.ExecutionReportList.Add(result);
                }
                catch (Exception exception)
                {
                    if (client_.ClosePositionByErrorEvent != null)
                    {
                        try
                        {
                            client_.ClosePositionByErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    context.Tcs.SetException(exception);
                }
            }

            public override void OnExecutionReportTrade1(ClientSession session, ClosePositionByRequestClientContext ClosePositionByRequestClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                var context = (ClosePositionByAsyncContext) ClosePositionByRequestClientContext;

                try
                {
                    TickTrader.FDK.Objects.ExecutionReport result = Convert(message);

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

                    context.ExecutionReportList.Add(result);
                }
                catch (Exception exception)
                {
                    if (client_.ClosePositionByErrorEvent != null)
                    {
                        try
                        {
                            client_.ClosePositionByErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    context.Tcs.SetException(exception);
                }
            }

            public override void OnExecutionReportTrade2(ClientSession session, ClosePositionByRequestClientContext ClosePositionByRequestClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                var context = (ClosePositionByAsyncContext) ClosePositionByRequestClientContext;

                try
                {
                    TickTrader.FDK.Objects.ExecutionReport result = Convert(message);

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

                    context.ExecutionReportList.Add(result);
                    context.Tcs.SetResult(context.ExecutionReportList.ToArray());
                }
                catch (Exception exception)
                {
                    if (client_.ClosePositionByErrorEvent != null)
                    {
                        try
                        {
                            client_.ClosePositionByErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    context.Tcs.SetException(exception);
                }
            }

            public override void OnExecutionReportTrade3(ClientSession session, ClosePositionByRequestClientContext ClosePositionByRequestClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                var context = (ClosePositionByAsyncContext) ClosePositionByRequestClientContext;

                try
                {
                    TickTrader.FDK.Objects.ExecutionReport result = Convert(message);

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

                    context.ExecutionReportList.Add(result);
                }
                catch (Exception exception)
                {
                    if (client_.ClosePositionByErrorEvent != null)
                    {
                        try
                        {
                            client_.ClosePositionByErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    context.Tcs.SetException(exception);
                }
            }

            public override void OnExecutionReportTrade4(ClientSession session, ClosePositionByRequestClientContext ClosePositionByRequestClientContext, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                var context = (ClosePositionByAsyncContext) ClosePositionByRequestClientContext;

                try
                {
                    TickTrader.FDK.Objects.ExecutionReport result = Convert(message);

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

                    context.ExecutionReportList.Add(result);
                    context.Tcs.SetResult(context.ExecutionReportList.ToArray());
                }
                catch (Exception exception)
                {
                    if (client_.ClosePositionByErrorEvent != null)
                    {
                        try
                        {
                            client_.ClosePositionByErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    context.Tcs.SetException(exception);
                }
            }

            public override void OnClosePositionByReject1(ClientSession session, ClosePositionByRequestClientContext ClosePositionByRequestClientContext, OrderReject message)
            {
                var context = (ClosePositionByAsyncContext) ClosePositionByRequestClientContext;

                try
                {
                    string text = message.Text;

                    if (client_.ClosePositionByErrorEvent != null)
                    {
                        try
                        {
                            client_.ClosePositionByErrorEvent(client_, context.Data, text);
                        }
                        catch
                        {
                        }
                    }

                    var exception = new Exception(text);
                    context.Tcs.SetException(exception);
                }
                catch (Exception exception)
                {
                    if (client_.ClosePositionByErrorEvent != null)
                    {
                        try
                        {
                            client_.ClosePositionByErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    context.Tcs.SetException(exception);
                }
            }

            public override void OnClosePositionByReject2(ClientSession session, ClosePositionByRequestClientContext ClosePositionByRequestClientContext, OrderReject message)
            {
                var context = (ClosePositionByAsyncContext) ClosePositionByRequestClientContext;

                try
                {
                    string text = message.Text;

                    if (client_.ClosePositionByErrorEvent != null)
                    {
                        try
                        {
                            client_.ClosePositionByErrorEvent(client_, context.Data, text);
                        }
                        catch
                        {
                        }
                    }

                    var exception = new Exception(text);
                    context.Tcs.SetException(exception);
                }
                catch (Exception exception)
                {
                    if (client_.ClosePositionByErrorEvent != null)
                    {
                        try
                        {
                            client_.ClosePositionByErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    context.Tcs.SetException(exception);
                }
            }

            public override void OnClosePositionByReject3(ClientSession session, ClosePositionByRequestClientContext ClosePositionByRequestClientContext, OrderReject message)
            {
                var context = (ClosePositionByAsyncContext) ClosePositionByRequestClientContext;

                try
                {
                    string text = message.Text;

                    if (client_.ClosePositionByErrorEvent != null)
                    {
                        try
                        {
                            client_.ClosePositionByErrorEvent(client_, context.Data, text);
                        }
                        catch
                        {
                        }
                    }

                    var exception = new Exception(text);
                    context.Tcs.SetException(exception);
                }
                catch (Exception exception)
                {
                    if (client_.ClosePositionByErrorEvent != null)
                    {
                        try
                        {
                            client_.ClosePositionByErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    context.Tcs.SetException(exception);
                }
            }

            public override void OnClosePositionByReject4(ClientSession session, ClosePositionByRequestClientContext ClosePositionByRequestClientContext, OrderReject message)
            {
                var context = (ClosePositionByAsyncContext) ClosePositionByRequestClientContext;

                try
                {
                    string text = message.Text;

                    if (client_.ClosePositionByErrorEvent != null)
                    {
                        try
                        {
                            client_.ClosePositionByErrorEvent(client_, context.Data, text);
                        }
                        catch
                        {
                        }
                    }

                    var exception = new Exception(text);
                    context.Tcs.SetException(exception);
                }
                catch (Exception exception)
                {
                    if (client_.ClosePositionByErrorEvent != null)
                    {
                        try
                        {
                            client_.ClosePositionByErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    context.Tcs.SetException(exception);
                }
            }

            public override void OnExecutionReport(ClientSession session, SoftFX.Net.OrderEntry.ExecutionReport message)
            {
                try
                {       
                    TickTrader.FDK.Objects.ExecutionReport result = Convert(message);
                                 
                    if (client_.ExecutionReportEvent != null)
                    {
                        try
                        {
                            client_.ExecutionReportEvent(client_, result);
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
                    SoftFX.Net.OrderEntry.PositionArray reportPositions = message.Positions;
                    int count = reportPositions.Length;
                    TickTrader.FDK.Objects.Position[] resultPositions = new TickTrader.FDK.Objects.Position[count];

                    for (int index = 0; index < count; ++index)
                    {
                        SoftFX.Net.OrderEntry.Position reportPosition = reportPositions[index];

                        TickTrader.FDK.Objects.Position resultPosition = new TickTrader.FDK.Objects.Position();
                        resultPosition.Symbol = reportPosition.SymbolId;
                        resultPosition.SettlementPrice = reportPosition.SettltPrice;
                        resultPosition.BuyAmount = reportPosition.LongQty;
                        resultPosition.SellAmount = reportPosition.ShortQty;
                        resultPosition.Commission = reportPosition.Commission;
                        resultPosition.AgentCommission = reportPosition.AgentCommission;
                        resultPosition.Swap = reportPosition.Swap;
                        resultPosition.BuyPrice = reportPosition.LongPrice;
                        resultPosition.SellPrice = reportPosition.ShortPrice;                        

                        resultPositions[index] = resultPosition;
                    }

                    if (client_.PositionUpdateEvent != null)
                    {
                        try
                        {
                            client_.PositionUpdateEvent(client_, resultPositions);
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
                    TickTrader.FDK.Objects.AccountInfo resultAccountInfo = new TickTrader.FDK.Objects.AccountInfo();
                    SoftFX.Net.OrderEntry.AccountInfo reportAccountInfo = message.AccountInfo;
                    resultAccountInfo.AccountId = reportAccountInfo.Id.ToString();                    
                    resultAccountInfo.Type = Convert(reportAccountInfo.Type);
                    resultAccountInfo.Email = reportAccountInfo.RegistEmail;
                    resultAccountInfo.Comment = reportAccountInfo.Description;
                    resultAccountInfo.Currency = reportAccountInfo.CurrId;
                    resultAccountInfo.RegistredDate = reportAccountInfo.RegistDate;
                    resultAccountInfo.Leverage = reportAccountInfo.Leverage;
                    resultAccountInfo.Balance = reportAccountInfo.Balance;
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

                    AccountAssetArray reportAssets = reportAccountInfo.Assets;                   

                    int count = reportAssets.Length;
                    AssetInfo[] resultAssets = new AssetInfo[count];

                    for (int index = 0; index < count; ++index)
                    {
                        AccountAsset reportAsset = reportAssets[index];

                        AssetInfo resultAsset = new AssetInfo();
                        resultAsset.Currency = reportAsset.CurrId;
                        resultAsset.Balance = reportAsset.Balance;
                        resultAsset.LockedAmount = reportAsset.Locked;

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
                    TickTrader.FDK.Objects.SessionInfo resultStatusInfo = new TickTrader.FDK.Objects.SessionInfo();
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

            public override void OnBalanceInfoUpdate(ClientSession session, BalanceInfoUpdate update)
            {
                try
                {
                    TickTrader.FDK.Objects.BalanceOperation result = new TickTrader.FDK.Objects.BalanceOperation();

                    SoftFX.Net.OrderEntry.BalanceInfo updateBalanceInfo = update.BalanceInfo;
                    result.Balance = updateBalanceInfo.Balance.Value;
                    result.TransactionAmount = updateBalanceInfo.Trade.Value;
                    result.TransactionCurrency = updateBalanceInfo.CurrId;

                    if (client_.BalanceInfoUpdateEvent != null)
                    {
                        try
                        {
                            client_.BalanceInfoUpdateEvent(client_, result);
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
                    TickTrader.FDK.Objects.Notification result = new TickTrader.FDK.Objects.Notification();
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

            TickTrader.FDK.Objects.ExecutionReport Convert(SoftFX.Net.OrderEntry.ExecutionReport report)
            {
                TickTrader.FDK.Objects.ExecutionReport result = new TickTrader.FDK.Objects.ExecutionReport();

                SoftFX.Net.OrderEntry.OrderAttributes reportAttributes = report.Attributes;
                SoftFX.Net.OrderEntry.OrderState reportState = report.State;

                result.ExecutionType = Convert(report.Type);
                result.ClientOrderId = report.ClOrdId;
                result.OrigClientOrderId = report.OrigClOrdId;
                result.OrderId = report.OrderId.ToString();
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
                result.OrderStatus = Convert(reportState.Status);                
                result.ExecutedVolume = reportState.CumQty;                
                result.LeavesVolume = reportState.LeavesQty;
                result.TradeAmount = reportState.LastQty;
                result.TradePrice = reportState.LastPrice;
                result.Commission = reportState.Commission;
                result.AgentCommission = reportState.AgentCommission;
                result.ReducedOpenCommission = (reportState.CommissionFlags & OrderCommissionFlags.OpenReduced) != 0;
                result.ReducedCloseCommission = (reportState.CommissionFlags & OrderCommissionFlags.CloseReduced) != 0;
                result.Swap = reportState.Swap;                
                result.AveragePrice = reportState.AvgPrice;                
                result.Created = reportState.Created;
                result.Modified = reportState.Modified;
                result.RejectReason = RejectReason.None;
                result.Comment = reportAttributes.Comment;
                result.Tag = reportAttributes.Tag;
                result.Magic = reportAttributes.Magic;                    
#pragma warning disable 618
                result.TradeRecordSide = ConvertToTradeRecordSide(reportAttributes.Side);
                result.TradeRecordType = ConvertToTradeRecordType(reportAttributes.Type, reportAttributes.TimeInForce);
#pragma warning restore 618

                SoftFX.Net.OrderEntry.ExecutionAssetArray reportAssets = report.Assets;
                int count = reportAssets.Length;
                TickTrader.FDK.Objects.AssetInfo[] resultAssets = new TickTrader.FDK.Objects.AssetInfo[count];

                for (int index = 0; index < count; ++ index)
                {
                    SoftFX.Net.OrderEntry.ExecutionAsset reportAsset = reportAssets[index];
                    TickTrader.FDK.Objects.AssetInfo resultAsset = new AssetInfo();

                    resultAsset.Currency = reportAsset.CurrId;
                    resultAsset.Balance = reportAsset.Balance;
                    resultAsset.LockedAmount = reportAsset.Locked;
                    resultAsset.TradeAmount = reportAsset.Trade;

                    resultAssets[index] = resultAsset;
                }

                result.Assets = resultAssets;
                result.Balance = report.BalanceInfo.Balance;

                return result;
            }

            TickTrader.FDK.Objects.LogoutReason Convert(SoftFX.Net.OrderEntry.LogoutReason reason)
            {
                switch (reason)
                {
                    case SoftFX.Net.OrderEntry.LogoutReason.ClientLogout:
                        return TickTrader.FDK.Objects.LogoutReason.ClientInitiated;

                    case SoftFX.Net.OrderEntry.LogoutReason.ServerLogout:
                        return TickTrader.FDK.Objects.LogoutReason.ServerLogout;

                    case SoftFX.Net.OrderEntry.LogoutReason.SlowConnection:
                        return TickTrader.FDK.Objects.LogoutReason.SlowConnection;

                    case SoftFX.Net.OrderEntry.LogoutReason.DeletedLogin:
                        return TickTrader.FDK.Objects.LogoutReason.LoginDeleted;

                    case SoftFX.Net.OrderEntry.LogoutReason.InternalServerError:
                        return TickTrader.FDK.Objects.LogoutReason.ServerError;

                    case SoftFX.Net.OrderEntry.LogoutReason.BlockedLogin:
                        return TickTrader.FDK.Objects.LogoutReason.BlockedAccount;

                    default:
                        throw new Exception("Invalid logout reason : " + reason);
                }
            }

            TickTrader.FDK.Objects.AccountType Convert(SoftFX.Net.OrderEntry.AccountType type)
            {
                switch (type)
                {
                    case SoftFX.Net.OrderEntry.AccountType.Gross:
                        return TickTrader.FDK.Objects.AccountType.Gross;

                    case SoftFX.Net.OrderEntry.AccountType.Net:
                        return TickTrader.FDK.Objects.AccountType.Net;

                    case SoftFX.Net.OrderEntry.AccountType.Cash:
                        return TickTrader.FDK.Objects.AccountType.Cash;

                    default:
                        throw new Exception("Invalid account type : " + type);
                }
            }

            TickTrader.FDK.Objects.SessionStatus Convert(SoftFX.Net.OrderEntry.TradingSessionStatus status)
            {
                switch (status)
                {
                    case SoftFX.Net.OrderEntry.TradingSessionStatus.Close:
                        return TickTrader.FDK.Objects.SessionStatus.Closed;

                    case SoftFX.Net.OrderEntry.TradingSessionStatus.Open:
                        return TickTrader.FDK.Objects.SessionStatus.Open;

                    default:
                        throw new Exception("Invalid trading session status : " + status);
                }
            }

            TickTrader.FDK.Objects.OrderStatus Convert(SoftFX.Net.OrderEntry.OrderStatus status)
            {
                switch (status)
                {
                    case SoftFX.Net.OrderEntry.OrderStatus.New:
                        return TickTrader.FDK.Objects.OrderStatus.New;

                    case SoftFX.Net.OrderEntry.OrderStatus.PartiallyFilled:
                        return TickTrader.FDK.Objects.OrderStatus.PartiallyFilled;

                    case SoftFX.Net.OrderEntry.OrderStatus.Filled:
                        return TickTrader.FDK.Objects.OrderStatus.Filled;

                    case SoftFX.Net.OrderEntry.OrderStatus.Cancelled:
                        return TickTrader.FDK.Objects.OrderStatus.Canceled;

                    case SoftFX.Net.OrderEntry.OrderStatus.PendingCancel:
                        return TickTrader.FDK.Objects.OrderStatus.PendingCancel;

                    case SoftFX.Net.OrderEntry.OrderStatus.Rejected:
                        return TickTrader.FDK.Objects.OrderStatus.Rejected;

                    case SoftFX.Net.OrderEntry.OrderStatus.Calculated:
                        return TickTrader.FDK.Objects.OrderStatus.Calculated;

                    case SoftFX.Net.OrderEntry.OrderStatus.Expired:
                        return TickTrader.FDK.Objects.OrderStatus.Expired;

                    case SoftFX.Net.OrderEntry.OrderStatus.PendingReplace:
                        return TickTrader.FDK.Objects.OrderStatus.PendingReplace;

                    case SoftFX.Net.OrderEntry.OrderStatus.PendingClose:
                        return TickTrader.FDK.Objects.OrderStatus.PendingClose;

                    case SoftFX.Net.OrderEntry.OrderStatus.Activated:
                        return TickTrader.FDK.Objects.OrderStatus.Activated;

                    default:
                        throw new Exception("Invalid order status : " + status);
                }
            }

            TickTrader.FDK.Objects.OrderType Convert(SoftFX.Net.OrderEntry.OrderType type)
            {
                switch (type)
                {
                    case SoftFX.Net.OrderEntry.OrderType.Market:
                        return TickTrader.FDK.Objects.OrderType.Market;

                    case SoftFX.Net.OrderEntry.OrderType.MarketWithSlippage:
                        return TickTrader.FDK.Objects.OrderType.MarketWithSlippage;

                    case SoftFX.Net.OrderEntry.OrderType.Limit:
                        return TickTrader.FDK.Objects.OrderType.Limit;

                    case SoftFX.Net.OrderEntry.OrderType.Stop:
                        return TickTrader.FDK.Objects.OrderType.Stop;

                    case SoftFX.Net.OrderEntry.OrderType.Position:
                        return TickTrader.FDK.Objects.OrderType.Position;

                    case SoftFX.Net.OrderEntry.OrderType.StopLimit:
                        return TickTrader.FDK.Objects.OrderType.StopLimit;

                    default:
                        throw new Exception("Invalid order type : " + type);
                }
            }

            TickTrader.FDK.Objects.OrderSide Convert(SoftFX.Net.OrderEntry.OrderSide side)
            {
                switch (side)
                {
                    case SoftFX.Net.OrderEntry.OrderSide.Buy:
                        return TickTrader.FDK.Objects.OrderSide.Buy;

                    case SoftFX.Net.OrderEntry.OrderSide.Sell:
                        return TickTrader.FDK.Objects.OrderSide.Sell;

                    default:
                        throw new Exception("Invalid order side : " + side);
                }
            }

            TickTrader.FDK.Objects.OrderTimeInForce Convert(SoftFX.Net.OrderEntry.OrderTimeInForce timeInForce)
            {
                switch (timeInForce)
                {
                    case SoftFX.Net.OrderEntry.OrderTimeInForce.GoodTillCancel:
                        return TickTrader.FDK.Objects.OrderTimeInForce.GoodTillCancel;

                    case SoftFX.Net.OrderEntry.OrderTimeInForce.ImmediateOrCancel:
                        return TickTrader.FDK.Objects.OrderTimeInForce.ImmediateOrCancel;

                    case SoftFX.Net.OrderEntry.OrderTimeInForce.GoodTillDate:
                        return TickTrader.FDK.Objects.OrderTimeInForce.GoodTillDate;

                    default:
                        throw new Exception("Invalid order time in force : " + timeInForce);
                }
            }

            TickTrader.FDK.Objects.ExecutionType Convert(SoftFX.Net.OrderEntry.ExecType type)
            {
                switch (type)
                {
                    case SoftFX.Net.OrderEntry.ExecType.New:
                        return TickTrader.FDK.Objects.ExecutionType.New;

                    case SoftFX.Net.OrderEntry.ExecType.Trade:
                        return TickTrader.FDK.Objects.ExecutionType.Trade;

                    case SoftFX.Net.OrderEntry.ExecType.Cancelled:
                        return TickTrader.FDK.Objects.ExecutionType.Canceled;

                    case SoftFX.Net.OrderEntry.ExecType.PendingCancel:
                        return TickTrader.FDK.Objects.ExecutionType.PendingCancel;

                    case SoftFX.Net.OrderEntry.ExecType.Rejected:
                        return TickTrader.FDK.Objects.ExecutionType.Rejected;

                    case SoftFX.Net.OrderEntry.ExecType.Calculated:
                        return TickTrader.FDK.Objects.ExecutionType.Calculated;

                    case SoftFX.Net.OrderEntry.ExecType.Expired:
                        return TickTrader.FDK.Objects.ExecutionType.Expired;

                    case SoftFX.Net.OrderEntry.ExecType.Replaced:
                        return TickTrader.FDK.Objects.ExecutionType.Replace;

                    case SoftFX.Net.OrderEntry.ExecType.PendingReplace:
                        return TickTrader.FDK.Objects.ExecutionType.PendingReplace;

                    case SoftFX.Net.OrderEntry.ExecType.PendingClose:
                        return TickTrader.FDK.Objects.ExecutionType.PendingClose;

                    default:
                        throw new Exception("Invalid exec type : " + type);
                }
            }

            TickTrader.FDK.Objects.RejectReason Convert(SoftFX.Net.OrderEntry.OrderRejectReason reason)
            {
                switch (reason)
                {
                    case SoftFX.Net.OrderEntry.OrderRejectReason.Dealer:
                        return TickTrader.FDK.Objects.RejectReason.DealerReject;

                    case SoftFX.Net.OrderEntry.OrderRejectReason.DealerTimeout:
                        return TickTrader.FDK.Objects.RejectReason.DealerReject;

                    case SoftFX.Net.OrderEntry.OrderRejectReason.UnknownSymbol:
                        return TickTrader.FDK.Objects.RejectReason.UnknownSymbol;

                    case SoftFX.Net.OrderEntry.OrderRejectReason.LimitsExceeded:
                        return TickTrader.FDK.Objects.RejectReason.OrderExceedsLImit;

                    case SoftFX.Net.OrderEntry.OrderRejectReason.OffQuotes:
                        return TickTrader.FDK.Objects.RejectReason.OffQuotes;

                    case SoftFX.Net.OrderEntry.OrderRejectReason.UnknownOrder:
                        return TickTrader.FDK.Objects.RejectReason.UnknownOrder;

                    case SoftFX.Net.OrderEntry.OrderRejectReason.DuplicateOrder:
                        return TickTrader.FDK.Objects.RejectReason.DuplicateClientOrderId;

                    case SoftFX.Net.OrderEntry.OrderRejectReason.IncorrectCharacteristics:
                        return TickTrader.FDK.Objects.RejectReason.InvalidTradeRecordParameters;

                    case SoftFX.Net.OrderEntry.OrderRejectReason.IncorrectQty:
                        return TickTrader.FDK.Objects.RejectReason.IncorrectQuantity;

                    case SoftFX.Net.OrderEntry.OrderRejectReason.TooLate:
                        return TickTrader.FDK.Objects.RejectReason.Other;

                    case SoftFX.Net.OrderEntry.OrderRejectReason.InternalServerError:
                        return TickTrader.FDK.Objects.RejectReason.Other;

                    case SoftFX.Net.OrderEntry.OrderRejectReason.Other:
                        return TickTrader.FDK.Objects.RejectReason.Other;

                    default:
                        throw new Exception("Invalid order reject reason : " + reason);
                }
            }

            TickTrader.FDK.Objects.TradeRecordType ConvertToTradeRecordType(SoftFX.Net.OrderEntry.OrderType type, SoftFX.Net.OrderEntry.OrderTimeInForce? timeInForce)
            {
                if (timeInForce == SoftFX.Net.OrderEntry.OrderTimeInForce.ImmediateOrCancel)
                    return TickTrader.FDK.Objects.TradeRecordType.IoC;

                switch (type)
                {
                    case SoftFX.Net.OrderEntry.OrderType.Market:
                        return TickTrader.FDK.Objects.TradeRecordType.Market;

                    case SoftFX.Net.OrderEntry.OrderType.MarketWithSlippage:
                        return TickTrader.FDK.Objects.TradeRecordType.MarketWithSlippage;

                    case SoftFX.Net.OrderEntry.OrderType.Limit:
                        return TickTrader.FDK.Objects.TradeRecordType.Limit;

                    case SoftFX.Net.OrderEntry.OrderType.Stop:
                        return TickTrader.FDK.Objects.TradeRecordType.Stop;

                    case SoftFX.Net.OrderEntry.OrderType.Position:
                        return TickTrader.FDK.Objects.TradeRecordType.Position;

                    case SoftFX.Net.OrderEntry.OrderType.StopLimit:
                        return TickTrader.FDK.Objects.TradeRecordType.StopLimit;

                    default:
                        throw new Exception("Invalid order type : " + type);
                }
            }

            TickTrader.FDK.Objects.TradeRecordSide ConvertToTradeRecordSide(SoftFX.Net.OrderEntry.OrderSide side)
            {
                switch (side)
                {
                    case SoftFX.Net.OrderEntry.OrderSide.Buy:
                        return TickTrader.FDK.Objects.TradeRecordSide.Buy;

                    case SoftFX.Net.OrderEntry.OrderSide.Sell:
                        return TickTrader.FDK.Objects.TradeRecordSide.Sell;

                    default:
                        throw new Exception("Invalid order side : " + side);
                }
            }

            TickTrader.FDK.Objects.NotificationType Convert(SoftFX.Net.OrderEntry.NotificationType type)
            {
                switch (type)
                {
                    case SoftFX.Net.OrderEntry.NotificationType.MarginCall:
                        return TickTrader.FDK.Objects.NotificationType.MarginCall;

                    case SoftFX.Net.OrderEntry.NotificationType.MarginCallRevocation:
                        return TickTrader.FDK.Objects.NotificationType.MarginCallRevocation;

                    case SoftFX.Net.OrderEntry.NotificationType.StopOut:
                        return TickTrader.FDK.Objects.NotificationType.StopOut;

                    case SoftFX.Net.OrderEntry.NotificationType.ConfigUpdate:
                        return TickTrader.FDK.Objects.NotificationType.ConfigUpdated;

                    default:
                        throw new Exception("Invalid notification type : " + type);
                }
            }

            TickTrader.FDK.Objects.NotificationSeverity Convert(SoftFX.Net.OrderEntry.NotificationSeverity severity)
            {
                switch (severity)
                {
                    case SoftFX.Net.OrderEntry.NotificationSeverity.Info:
                        return TickTrader.FDK.Objects.NotificationSeverity.Information;

                    case SoftFX.Net.OrderEntry.NotificationSeverity.Warning:
                        return TickTrader.FDK.Objects.NotificationSeverity.Warning;

                    case SoftFX.Net.OrderEntry.NotificationSeverity.Error:
                        return TickTrader.FDK.Objects.NotificationSeverity.Error;

                    default:
                        throw new Exception("Invalid notification severity : " + severity);
                }
            }

            Client client_;
        }

        #endregion

        #region Async helpers        

        private static void ConvertToSync(Task task, int timeout)
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

        private static TResult ConvertToSync<TResult>(Task<TResult> task, int timeout)
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