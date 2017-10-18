using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using SoftFX.Net.TradeCapture;
using TickTrader.FDK.Common;

namespace TickTrader.FDK.TradeCapture
{
    public class Client : IDisposable
    {
        #region Constructors

        public Client(string name) : this(name, 5060, true, "Logs", false)
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

        public void Connect(string address, int timeout)
        {
            session_.Connect(address);

            if (!session_.WaitConnect(timeout))
            {
                session_.Disconnect("Connect timeout");
                session_.Join();

                throw new TimeoutException("Connect timeout");
            }
        }

        public void ConnectAsync(string address)
        {
            session_.Connect(address);
        }

        public void Disconnect(string text)
        {
            session_.Disconnect(text);
            session_.Join();
        }

        public void DisconnectAsync(string text)
        {
            session_.Disconnect(text);
        }

        public void Join()
        {
            session_.Join();
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

        public void Login(string username, string password, string deviceId, string appId, string sessionId, int timeout)
        {
            ConvertToSync(LoginAsync(null, username, password, deviceId, appId, sessionId), timeout);
        }

        public Task LoginAsync(object data, string username, string password, string deviceId, string appId, string sessionId)
        {
            Task result;

            if (string.IsNullOrEmpty(appId))
                appId = "FDK2";

            // Create a new async context
            var context = new LoginAsyncContext();
            context.Data = data;

            if (data == null)
            {
                context.taskCompletionSource_ = new TaskCompletionSource<object>();
                result = context.taskCompletionSource_.Task;
            }
            else
                result = null;

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

            // Return result task
            return result;
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
            session_.Send(message);
        }

        public LogoutInfo Logout(string message, int timeout)
        {
            return ConvertToSync(LogoutAsync(null, message), timeout);
        }

        public Task<LogoutInfo> LogoutAsync(object data, string message)
        {
            Task<LogoutInfo> result;

            // Create a new async context
            var context = new LogoutAsyncContext();
            context.Data = data;

            if (data == null)
            {
                context.taskCompletionSource_ = new TaskCompletionSource<LogoutInfo>();
                result = context.taskCompletionSource_.Task;
            }
            else
                result = null;

            // Create a request
            var request = new Logout(0)
            {
                Text = message
            };

            // Send request to the server
            session_.SendLogout(context, request);

            // Return result task
            return result;
        }

        #endregion

        #region Trade Capture
        
        public delegate void SubscribeTradesResultDelegate(Client client, object data);
        public delegate void SubscribeTradesErrorDelegate(Client client, object data, string message);
        public delegate void UnsubscribeTradesResultDelegate(Client client, object data);
        public delegate void UnsubscribeTradesErrorDelegate(Client client, object data, string message);
        public delegate void TradeDownloadResultBeginDelegate(Client client, object data);
        public delegate void TradeDownloadResultDelegate(Client client, object data, TickTrader.FDK.Common.TradeTransactionReport tradeTransactionReport);
        public delegate void TradeDownloadResultEndDelegate(Client client, object data);
        public delegate void TradeDownloadErrorDelegate(Client client, object data, string message);
        public delegate void TradeUpdateDelegate(Client client, TickTrader.FDK.Common.TradeTransactionReport tradeTransactionReport);
        public delegate void NotificationDelegate(Client client, TickTrader.FDK.Common.Notification notification);
        
        public event SubscribeTradesResultDelegate SubscribeTradesResultEvent;
        public event SubscribeTradesErrorDelegate SubscribeTradesErrorEvent;
        public event UnsubscribeTradesResultDelegate UnsubscribeTradesResultEvent;
        public event UnsubscribeTradesErrorDelegate UnsubscribeTradesErrorEvent;
        public event TradeDownloadResultBeginDelegate TradeDownloadResultBeginEvent;
        public event TradeDownloadResultDelegate TradeDownloadResultEvent;
        public event TradeDownloadResultEndDelegate TradeDownloadResultEndEvent;
        public event TradeDownloadErrorDelegate TradeDownloadErrorEvent;
        public event TradeUpdateDelegate TradeUpdateEvent;
        public event NotificationDelegate NotificationEvent;

        public void SubscribeTrades(bool skipCancel, int timeout)
        {
            ConvertToSync(SubscribeTradesAsync(null, skipCancel), timeout);
        }

        public Task SubscribeTradesAsync(object data, bool skipCancel)
        {
            Task result;

            // Create a new async context
            var context = new SubscribeTradesAsyncContext();
            context.Data = data;

            if (data == null)
            {
                context.taskCompletionSource_ = new TaskCompletionSource<object>();
                result = context.taskCompletionSource_.Task;
            }
            else
                result = null;

            // Create a request
            var request = new TradeCaptureRequest(0);
            request.Id = Guid.NewGuid().ToString();
            request.Type = TradeCaptureRequestType.Subscribe;
            request.SkipCancel = skipCancel;

            // Send request to the server
            session_.SendTradeCaptureRequest(context, request);

            // Return result task
            return result;
        }

        public void UnsubscribeTrades(int timeout)
        {
            ConvertToSync(UnsubscribeTradesAsync(null), timeout);
        }

        public Task UnsubscribeTradesAsync(object data)
        {
            Task result;

            // Create a new async context
            var context = new UnsubscribeTradesAsyncContext();
            context.Data = data;

            if (data == null)
            {
                context.taskCompletionSource_ = new TaskCompletionSource<object>();
                result = context.taskCompletionSource_.Task;
            }
            else
                result = null;

            // Create a request
            var request = new TradeCaptureRequest(0);
            request.Id = Guid.NewGuid().ToString();
            request.Type = TradeCaptureRequestType.Unsubscribe;

            // Send request to the server
            session_.SendTradeCaptureRequest(context, request);

            // Return result task
            return result;
        }

        public TradeTransactionReportEnumerator DownloadTrades(TimeDirection timeDirection, DateTime? from, DateTime? to, bool skipCancel, int timeout)
        {
            return ConvertToSync(DownloadTradesAsync(null, timeDirection, from, to, skipCancel), timeout);
        }

        public Task<TradeTransactionReportEnumerator> DownloadTradesAsync(object data, TimeDirection timeDirection, DateTime? from, DateTime? to, bool skipCancel)
        {
            Task<TradeTransactionReportEnumerator> result;

            // Create a new async context
            var context = new TradeDownloadAsyncContext();
            context.Data = data;
            context.timeDirection_ = timeDirection;
            context.from_ = from;
            context.to_ = to;
            context.skipCancel_ = skipCancel;
            context.reportId_ = null;

            if (data == null)
            {
                context.taskCompletionSource_ = new TaskCompletionSource<TradeTransactionReportEnumerator>();
                result = context.taskCompletionSource_.Task;
            }
            else
                result = null;

            // Create a request
            TradeDownloadRequest request = new TradeDownloadRequest(0);
            request.Id = Guid.NewGuid().ToString();
            request.Direction = Convert(timeDirection);
            request.From = from;
            request.To = to;
            request.SkipCancel = skipCancel;

            // Send request to the server
            session_.SendTradeDownloadRequest(context, request);

            // Return result task
            return result;
        }

        SoftFX.Net.TradeCapture.TradeHistoryDirection Convert(TickTrader.FDK.Common.TimeDirection timeDirection)
        {
            switch (timeDirection)
            {
                case TickTrader.FDK.Common.TimeDirection.Forward:
                    return SoftFX.Net.TradeCapture.TradeHistoryDirection.Forward;

                case TickTrader.FDK.Common.TimeDirection.Backward:
                    return SoftFX.Net.TradeCapture.TradeHistoryDirection.Backward;

                default:
                    throw new Exception("Invalid time direction : " + timeDirection);
            }
        }

        #endregion

        #region Implementation

        interface IAsyncContext
        {
            void SetDisconnectError(Exception exception);
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

        class SubscribeTradesAsyncContext : TradeCaptureRequestClientContext, IAsyncContext
        {
            public SubscribeTradesAsyncContext() : base(false)
            {
            }

            public void SetDisconnectError(Exception exception)
            {
                if (taskCompletionSource_ != null)
                    taskCompletionSource_.SetException(exception);
            }

            public TaskCompletionSource<object> taskCompletionSource_;
        }

        class UnsubscribeTradesAsyncContext : TradeCaptureRequestClientContext, IAsyncContext
        {
            public UnsubscribeTradesAsyncContext() : base(false)
            {
            }

            public void SetDisconnectError(Exception exception)
            {
                if (taskCompletionSource_ != null)
                    taskCompletionSource_.SetException(exception);
            }

            public TaskCompletionSource<object> taskCompletionSource_;
        }

        class TradeDownloadAsyncContext : TradeDownloadRequestClientContext, IAsyncContext
        {
            public TradeDownloadAsyncContext() : base(false)
            {
            }

            public void SetDisconnectError(Exception exception)
            {
                if (taskCompletionSource_ != null)
                {
                    if (tradeTransactionReportEnumerator_ != null)
                    {
                        lock (tradeTransactionReportEnumerator_.mutex_)
                        {
                            tradeTransactionReportEnumerator_.completed_ = true;

                            if (tradeTransactionReportEnumerator_.taskCompletionSource_ != null)
                            {
                                tradeTransactionReportEnumerator_.taskCompletionSource_.SetException(exception);
                                tradeTransactionReportEnumerator_.taskCompletionSource_ = null;
                            }
                        }
                    }
                    else
                        taskCompletionSource_.SetException(exception);
                }
            }

            public TimeDirection timeDirection_;
            public DateTime? from_;
            public DateTime? to_;
            public bool skipCancel_;
            public string reportId_;
            public TaskCompletionSource<TradeTransactionReportEnumerator> taskCompletionSource_;
            public TradeTransactionReportEnumerator tradeTransactionReportEnumerator_;
            public TradeTransactionReport tradeTransactionReport_;            
        }

        class ClientSessionListener : SoftFX.Net.TradeCapture.ClientSessionListener
        {
            public ClientSessionListener(Client client)
            {
                client_ = client;
                tradeTransactionReport_ = new TradeTransactionReport();
            }

            public override void OnConnect(ClientSession clientSession)
            {
                try
                {
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

                    string message = "Client disconnected";

                    if (text != null)
                    {
                        message += " : ";
                        message += text;
                    }

                    Exception exception = new Exception(message);

                    foreach (ClientContext context in contexts)
                        ((IAsyncContext)context).SetDisconnectError(exception);
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

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetException(exception);
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

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetResult(null);
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

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetException(exception);
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

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetException(exception);
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
                            client_.LoginErrorEvent(client_, LoginRequestClientContext.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetException(exception);
                }
            }

            public override void OnTradeCaptureReport(ClientSession session, TradeCaptureRequestClientContext TradeCaptureRequestClientContext, TradeCaptureReport message)
            {
                if (TradeCaptureRequestClientContext is SubscribeTradesAsyncContext)
                {
                    // SubscribeTrades

                    SubscribeTradesAsyncContext context = (SubscribeTradesAsyncContext) TradeCaptureRequestClientContext;

                    try
                    {
                        if (client_.SubscribeTradesResultEvent != null)
                        {
                            try
                            {
                                client_.SubscribeTradesResultEvent(client_, context.Data);
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
                        if (client_.SubscribeTradesErrorEvent != null)
                        {
                            try
                            {
                                client_.SubscribeTradesErrorEvent(client_, context.Data, exception.Message);
                            }
                            catch
                            {
                            }
                        }

                        if (context.taskCompletionSource_ != null)
                            context.taskCompletionSource_.SetException(exception);
                    }
                }
                else
                {
                    // UnsubscribeTrades

                    UnsubscribeTradesAsyncContext context = (UnsubscribeTradesAsyncContext) TradeCaptureRequestClientContext;
                                        
                    try
                    {
                        if (client_.UnsubscribeTradesResultEvent != null)
                        {
                            try
                            {
                                client_.UnsubscribeTradesResultEvent(client_, context.Data);
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
                        if (client_.UnsubscribeTradesErrorEvent != null)
                        {
                            try
                            {
                                client_.UnsubscribeTradesErrorEvent(client_, context.Data, exception.Message);
                            }
                            catch
                            {
                            }
                        }

                        if (context.taskCompletionSource_ != null)
                            context.taskCompletionSource_.SetException(exception);
                    }
                }
            }            

            public override void OnTradeCaptureReject(ClientSession session, TradeCaptureRequestClientContext TradeCaptureRequestClientContext, Reject message)
            {
                if (TradeCaptureRequestClientContext is SubscribeTradesAsyncContext)
                {
                    // SubscribeTrades

                    SubscribeTradesAsyncContext context = (SubscribeTradesAsyncContext) TradeCaptureRequestClientContext;

                    try
                    {
                        if (client_.SubscribeTradesErrorEvent != null)
                        {
                            try
                            {
                                client_.SubscribeTradesErrorEvent(client_, context.Data, message.Text);
                            }
                            catch
                            {
                            }
                        }

                        if (context.taskCompletionSource_ != null)
                        {
                            Exception exception = new Exception(message.Text);
                            context.taskCompletionSource_.SetException(exception);
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.SubscribeTradesErrorEvent != null)
                        {
                            try
                            {
                                client_.SubscribeTradesErrorEvent(client_, context.Data, exception.Message);
                            }
                            catch
                            {
                            }
                        }

                        if (context.taskCompletionSource_ != null)
                            context.taskCompletionSource_.SetException(exception);
                    }
                }
                else
                {
                    // UnsubscribeTrades

                    UnsubscribeTradesAsyncContext context = (UnsubscribeTradesAsyncContext) TradeCaptureRequestClientContext;
                                        
                    try
                    {
                        if (client_.UnsubscribeTradesErrorEvent != null)
                        {
                            try
                            {
                                client_.UnsubscribeTradesErrorEvent(client_, context.Data, message.Text);
                            }
                            catch
                            {
                            }
                        }

                        if (context.taskCompletionSource_ != null)
                        {
                            Exception exception = new Exception(message.Text);
                            context.taskCompletionSource_.SetException(exception);
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.UnsubscribeTradesErrorEvent != null)
                        {
                            try
                            {
                                client_.UnsubscribeTradesErrorEvent(client_, context.Data, exception.Message);
                            }
                            catch
                            {
                            }
                        }

                        if (context.taskCompletionSource_ != null)
                            context.taskCompletionSource_.SetException(exception);
                    }
                }
            }

            public override void OnTradeDownloadReport(ClientSession session, TradeDownloadRequestClientContext TradeDownloadRequestClientContext, TradeDownloadReport message)
            {
                TradeDownloadAsyncContext context = (TradeDownloadAsyncContext) TradeDownloadRequestClientContext;

                try
                {
                    if (context.reportId_ == null)
                    {
                        context.tradeTransactionReport_ = new TradeTransactionReport();

                        if (client_.TradeDownloadResultBeginEvent != null)
                        {
                            try
                            {
                                client_.TradeDownloadResultBeginEvent(client_, context.Data);
                            }
                            catch
                            {
                            }
                        }

                        if (context.taskCompletionSource_ != null)
                        {
                            context.tradeTransactionReportEnumerator_ = new TradeTransactionReportEnumerator(client_);
                            context.taskCompletionSource_.SetResult(context.tradeTransactionReportEnumerator_);
                        }
                    }

                    context.reportId_ = message.Id;

                    if (! message.Last)
                    {
                        // Sending the next request ahead of current response processing

                        if (context.taskCompletionSource_ != null)
                        {
                            lock (context.tradeTransactionReportEnumerator_.mutex_)
                            {
                                if (! context.tradeTransactionReportEnumerator_.completed_)
                                {
                                    TradeDownloadRequest request = new TradeDownloadRequest(0);
                                    request.Id = Guid.NewGuid().ToString();
                                    request.Direction = client_.Convert(context.timeDirection_);
                                    request.From = context.from_;
                                    request.To = context.to_;
                                    request.SkipCancel = context.skipCancel_;
                                    request.ReportId = context.reportId_;

                                    client_.session_.SendTradeDownloadRequest(context, request);
                                }
                            }
                        }
                        else
                        {
                            TradeDownloadRequest request = new TradeDownloadRequest(0);
                            request.Id = Guid.NewGuid().ToString();
                            request.Direction = client_.Convert(context.timeDirection_);
                            request.From = context.from_;
                            request.To = context.to_;
                            request.SkipCancel = context.skipCancel_;
                            request.ReportId = context.reportId_;

                            client_.session_.SendTradeDownloadRequest(context, request);
                        }
                    }

                    TradeArray trades = message.Trades;
                    int count = trades.Length;

                    for (int index = 0; index < count; ++index)
                    {
                        Trade trade = trades[index];

                        Convert(context.tradeTransactionReport_, trade);

                        if (client_.TradeDownloadResultEvent != null)
                        {
                            try
                            {
                                client_.TradeDownloadResultEvent(client_, context.Data, context.tradeTransactionReport_);
                            }
                            catch
                            {
                            }
                        }

                        if (context.taskCompletionSource_ != null)
                            SetTradeEnumeratorResult(context);
                    }

                    if (message.Last)
                    {
                        if (client_.TradeDownloadResultEndEvent != null)
                        {
                            try
                            {
                                client_.TradeDownloadResultEndEvent(client_, context.Data);
                            }
                            catch
                            {
                            }
                        }

                        if (context.taskCompletionSource_ != null)
                            SetTradeEnumeratorEnd(context);
                    }
                }
                catch (Exception exception)
                {
                    if (client_.TradeDownloadErrorEvent != null)
                    {
                        try
                        {
                            client_.TradeDownloadErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                    {
                        if (context.tradeTransactionReportEnumerator_ != null)
                        {
                            SetTradeEnumeratorError(context, exception);
                        }
                        else
                            context.taskCompletionSource_.SetException(exception);
                    }
                }
            }

            public override void OnTradeDownloadReject(ClientSession session, TradeDownloadRequestClientContext TradeDownloadRequestClientContext, Reject message)
            {
                TradeDownloadAsyncContext context = (TradeDownloadAsyncContext) TradeDownloadRequestClientContext;

                try
                {
                    string text = message.Text;

                    if (client_.TradeDownloadErrorEvent != null)
                    {
                        try
                        {
                            client_.TradeDownloadErrorEvent(client_, context.Data, text);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                    {
                        Exception exception = new Exception(text);

                        if (context.tradeTransactionReportEnumerator_ != null)
                        {
                            SetTradeEnumeratorError(context, exception);
                        }
                        else
                            context.taskCompletionSource_.SetException(exception);
                    }
                }
                catch (Exception exception)
                {
                    if (client_.TradeDownloadErrorEvent != null)
                    {
                        try
                        {
                            client_.TradeDownloadErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                    {
                        if (context.tradeTransactionReportEnumerator_ != null)
                        {
                            SetTradeEnumeratorError(context, exception);
                        }
                        else
                            context.taskCompletionSource_.SetException(exception);
                    }
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

            public override void OnTradeCaptureRefresh(ClientSession session, TradeCaptureRefresh message)
            {
                try
                {
                    SoftFX.Net.TradeCapture.Trade trade = message.Trade;

                    Convert(tradeTransactionReport_, trade);

                    if (client_.TradeUpdateEvent != null)
                    {
                        try
                        {
                            client_.TradeUpdateEvent(client_, tradeTransactionReport_);
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

            public override void OnNotification(ClientSession session, SoftFX.Net.TradeCapture.Notification message)
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

            void SetTradeEnumeratorResult(TradeDownloadAsyncContext context)
            {
                while (true)
                {
                    lock (context.tradeTransactionReportEnumerator_.mutex_)
                    {
                        if (context.tradeTransactionReportEnumerator_.completed_)
                            break;

                        if (context.tradeTransactionReportEnumerator_.taskCompletionSource_ != null)
                        {
                            TradeTransactionReport tradeTransactionReport = context.tradeTransactionReport_;
                            context.tradeTransactionReport_ = context.tradeTransactionReportEnumerator_.tradeTransactionReport_;
                            context.tradeTransactionReportEnumerator_.tradeTransactionReport_ = tradeTransactionReport;

                            context.tradeTransactionReportEnumerator_.completed_ = false;
                            context.tradeTransactionReportEnumerator_.taskCompletionSource_.SetResult(tradeTransactionReport);
                            context.tradeTransactionReportEnumerator_.taskCompletionSource_ = null;

                            break;
                        }
                    }

                    context.tradeTransactionReportEnumerator_.event_.WaitOne();
                }
            }

            void SetTradeEnumeratorEnd(TradeDownloadAsyncContext context)
            {
                while (true)
                {
                    lock (context.tradeTransactionReportEnumerator_.mutex_)
                    {
                        if (context.tradeTransactionReportEnumerator_.completed_)
                            break;

                        if (context.tradeTransactionReportEnumerator_.taskCompletionSource_ != null)
                        {
                            context.tradeTransactionReportEnumerator_.completed_ = true;
                            context.tradeTransactionReportEnumerator_.taskCompletionSource_.SetResult(null);
                            context.tradeTransactionReportEnumerator_.taskCompletionSource_ = null;

                            break;
                        }
                    }

                    context.tradeTransactionReportEnumerator_.event_.WaitOne();
                }
            }

            void SetTradeEnumeratorError(TradeDownloadAsyncContext context, Exception exception)
            {
                while (true)
                {
                    lock (context.tradeTransactionReportEnumerator_.mutex_)
                    {
                        if (context.tradeTransactionReportEnumerator_.completed_)
                            break;

                        if (context.tradeTransactionReportEnumerator_.taskCompletionSource_ != null)
                        {
                            context.tradeTransactionReportEnumerator_.completed_ = true;
                            context.tradeTransactionReportEnumerator_.taskCompletionSource_.SetException(exception);
                            context.tradeTransactionReportEnumerator_.taskCompletionSource_ = null;

                            break;
                        }
                    }

                    context.tradeTransactionReportEnumerator_.event_.WaitOne();
                }
            }

            void Convert(TickTrader.FDK.Common.TradeTransactionReport tradeTransactionReport, SoftFX.Net.TradeCapture.Trade trade)
            {
                ConversionRates conversionRates = trade.ConversionRates;

                tradeTransactionReport.TradeTransactionReportType = Convert(trade.Type);
                tradeTransactionReport.TradeTransactionReason = Convert(trade.Reason);
                tradeTransactionReport.AccountBalance = trade.Balance.Total.GetValueOrDefault(0);
                tradeTransactionReport.TransactionAmount = trade.Balance.Move.GetValueOrDefault(0);
                tradeTransactionReport.TransactionCurrency = trade.Balance.CurrId;
                tradeTransactionReport.Id = trade.Id;
                tradeTransactionReport.ClientId = trade.ClOrdId;
                tradeTransactionReport.Quantity = trade.Qty;
                tradeTransactionReport.MaxVisibleQuantity = trade.MaxVisibleQty;
                tradeTransactionReport.LeavesQuantity = trade.LeavesQty;
                tradeTransactionReport.Price = trade.Price.GetValueOrDefault(0);
                tradeTransactionReport.StopPrice = trade.StopPrice.GetValueOrDefault(0);
                tradeTransactionReport.OrderType = Convert(trade.OrderType);
                tradeTransactionReport.OrderSide = Convert(trade.OrderSide);

                if (trade.TimeInForce.HasValue)
                {
                    tradeTransactionReport.TimeInForce = Convert(trade.TimeInForce.Value);
                }
                else
                    tradeTransactionReport.TimeInForce = null;

                tradeTransactionReport.Symbol = trade.SymbolId;
                tradeTransactionReport.Comment = trade.Comment;
                tradeTransactionReport.Tag = trade.Tag;
                tradeTransactionReport.Magic = trade.Magic;
                tradeTransactionReport.IsReducedOpenCommission = (trade.CommissionFlags & CommissionFlags.OpenReduced) != 0;
                tradeTransactionReport.IsReducedCloseCommission = (trade.CommissionFlags & CommissionFlags.CloseReduced) != 0;
                tradeTransactionReport.ImmediateOrCancel = trade.TimeInForce == SoftFX.Net.TradeCapture.OrderTimeInForce.ImmediateOrCancel;
                tradeTransactionReport.MarketWithSlippage = trade.OrderType == SoftFX.Net.TradeCapture.OrderType.MarketWithSlippage;
                tradeTransactionReport.OrderCreated = trade.Created;
                tradeTransactionReport.OrderModified = trade.Modified.GetValueOrDefault();

                if (trade.Type == TradeType.PositionClosed)
                {
                    tradeTransactionReport.PositionId = trade.OrderId.ToString();

                    if (trade.ByOrderId.HasValue)
                    {
                        tradeTransactionReport.PositionById = trade.ByOrderId.ToString();
                    }
                    else
                        tradeTransactionReport.PositionById = null;

                    tradeTransactionReport.PositionOpened = trade.Created;
                    tradeTransactionReport.PosOpenPrice = trade.Price.Value;
                    tradeTransactionReport.PositionQuantity = trade.Qty;
                    tradeTransactionReport.PositionLastQuantity = trade.LastQty.GetValueOrDefault(0);
                    tradeTransactionReport.PositionLeavesQuantity = trade.LeavesQty;
                    tradeTransactionReport.PositionClosePrice = trade.LastPrice.GetValueOrDefault(0);
                    tradeTransactionReport.PositionClosed = trade.TransactTime;
                    tradeTransactionReport.PositionModified = trade.Modified.GetValueOrDefault();
                }

                if (trade.PosType.HasValue)
                {
                    tradeTransactionReport.PosRemainingSide = Convert(trade.PosType.Value);                        
                }
                else
                    tradeTransactionReport.PosRemainingSide = 0;

                tradeTransactionReport.PosRemainingPrice = trade.PosPrice;
                tradeTransactionReport.Commission = trade.Commission.GetValueOrDefault(0);
                tradeTransactionReport.AgentCommission = trade.AgentCommission.GetValueOrDefault(0);
                tradeTransactionReport.Swap = trade.Swap.GetValueOrDefault(0);
                tradeTransactionReport.CommCurrency = trade.ComissionCurrId;
                tradeTransactionReport.StopLoss = trade.StopLoss.GetValueOrDefault(0);
                tradeTransactionReport.TakeProfit = trade.TakeProfit.GetValueOrDefault(0);
                tradeTransactionReport.TransactionTime = trade.TransactTime;
                tradeTransactionReport.OrderFillPrice = trade.LastPrice;
                tradeTransactionReport.OrderLastFillAmount = trade.LastQty;                    
                tradeTransactionReport.OpenConversionRate = conversionRates.Open;
                tradeTransactionReport.CloseConversionRate = conversionRates.Close;
                tradeTransactionReport.ActionId = trade.ActionId;
                tradeTransactionReport.Expiration = trade.ExpireTime;
                tradeTransactionReport.SrcAssetCurrency = trade.SrcAsset.CurrId;
                tradeTransactionReport.SrcAssetAmount = trade.SrcAsset.Total;
                tradeTransactionReport.SrcAssetMovement = trade.SrcAsset.Move;
                tradeTransactionReport.DstAssetCurrency = trade.DestAsset.CurrId;
                tradeTransactionReport.DstAssetAmount = trade.DestAsset.Total;
                tradeTransactionReport.DstAssetMovement = trade.DestAsset.Move;
                tradeTransactionReport.MarginCurrencyToUsdConversionRate = conversionRates.MarginToUsd;
                tradeTransactionReport.UsdToMarginCurrencyConversionRate = conversionRates.UsdToMargin;
                tradeTransactionReport.MarginCurrency = trade.MarginCurrId;
                tradeTransactionReport.ProfitCurrencyToUsdConversionRate = conversionRates.ProfitToUsd;
                tradeTransactionReport.UsdToProfitCurrencyConversionRate = conversionRates.UsdToProfit;
                tradeTransactionReport.ProfitCurrency = trade.ProfitCurrId;
                tradeTransactionReport.SrcAssetToUsdConversionRate = conversionRates.SrcToUsd;
                tradeTransactionReport.UsdToSrcAssetConversionRate = conversionRates.UsdToSrc;
                tradeTransactionReport.DstAssetToUsdConversionRate = conversionRates.DestToUsd;
                tradeTransactionReport.UsdToDstAssetConversionRate = conversionRates.UsdToDest;
                tradeTransactionReport.MinCommissionCurrency = trade.MinCommissionCurrId;
                tradeTransactionReport.MinCommissionConversionRate = conversionRates.MinCommission;
#pragma warning disable 618
                tradeTransactionReport.TradeRecordType = ConvertToTradeRecordType(trade.OrderType, trade.TimeInForce);
                tradeTransactionReport.TradeRecordSide = ConvertToTradeRecordSide(trade.OrderSide);
#pragma warning restore 618
            }

            TickTrader.FDK.Common.LogoutReason Convert(SoftFX.Net.TradeCapture.LogoutReason reason)
            {
                switch (reason)
                {
                    case SoftFX.Net.TradeCapture.LogoutReason.ClientLogout:
                        return TickTrader.FDK.Common.LogoutReason.ClientInitiated;

                    case SoftFX.Net.TradeCapture.LogoutReason.ServerLogout:
                        return TickTrader.FDK.Common.LogoutReason.ServerLogout;

                    case SoftFX.Net.TradeCapture.LogoutReason.SlowConnection:
                        return TickTrader.FDK.Common.LogoutReason.SlowConnection;

                    case SoftFX.Net.TradeCapture.LogoutReason.DeletedLogin:
                        return TickTrader.FDK.Common.LogoutReason.LoginDeleted;

                    case SoftFX.Net.TradeCapture.LogoutReason.InternalServerError:
                        return TickTrader.FDK.Common.LogoutReason.ServerError;

                    case SoftFX.Net.TradeCapture.LogoutReason.BlockedLogin:
                        return TickTrader.FDK.Common.LogoutReason.BlockedAccount;

                    default:
                        throw new Exception("Invalid logout reason : " + reason);
                }
            }

            TickTrader.FDK.Common.TradeTransactionReportType Convert(SoftFX.Net.TradeCapture.TradeType type)
            {
                switch (type)
                {
                    case SoftFX.Net.TradeCapture.TradeType.OrderFilled:
                        return TickTrader.FDK.Common.TradeTransactionReportType.OrderFilled;

                    case SoftFX.Net.TradeCapture.TradeType.PositionClosed:
                        return TickTrader.FDK.Common.TradeTransactionReportType.PositionClosed;

                    case SoftFX.Net.TradeCapture.TradeType.OrderCancelled:
                        return TickTrader.FDK.Common.TradeTransactionReportType.OrderCanceled;

                    case SoftFX.Net.TradeCapture.TradeType.OrderExpired:
                        return TickTrader.FDK.Common.TradeTransactionReportType.OrderExpired;

                    case SoftFX.Net.TradeCapture.TradeType.Balance:
                        return TickTrader.FDK.Common.TradeTransactionReportType.BalanceTransaction;

                    case SoftFX.Net.TradeCapture.TradeType.Credit:
                        return TickTrader.FDK.Common.TradeTransactionReportType.Credit;

                    default:
                        throw new Exception("Invalid trade type : " + type);
                }
            }

            TickTrader.FDK.Common.TradeTransactionReason Convert(SoftFX.Net.TradeCapture.TradeReason reason)
            {
                switch (reason)
                {
                    case SoftFX.Net.TradeCapture.TradeReason.ClientRequest:
                        return TickTrader.FDK.Common.TradeTransactionReason.ClientRequest;

                    case SoftFX.Net.TradeCapture.TradeReason.OrderActivated:
                        return TickTrader.FDK.Common.TradeTransactionReason.PendingOrderActivation;

                    case SoftFX.Net.TradeCapture.TradeReason.TakeProfitActivated:
                        return TickTrader.FDK.Common.TradeTransactionReason.TakeProfitActivation;

                    case SoftFX.Net.TradeCapture.TradeReason.StopLossActivated:
                        return TickTrader.FDK.Common.TradeTransactionReason.StopLossActivation;

                    case SoftFX.Net.TradeCapture.TradeReason.OrderExpired:
                        return TickTrader.FDK.Common.TradeTransactionReason.Expired;

                    case SoftFX.Net.TradeCapture.TradeReason.Dealer:
                        return TickTrader.FDK.Common.TradeTransactionReason.DealerDecision;

                    case SoftFX.Net.TradeCapture.TradeReason.StopOut:
                        return TickTrader.FDK.Common.TradeTransactionReason.StopOut;


                    case SoftFX.Net.TradeCapture.TradeReason.Rollover:
                        return TickTrader.FDK.Common.TradeTransactionReason.Rollover;

                    case SoftFX.Net.TradeCapture.TradeReason.AccountDeleted:
                        return TickTrader.FDK.Common.TradeTransactionReason.DeleteAccount;

                    default:
                        throw new Exception("Invalid trade reason : " + reason);
                }
            }

            TickTrader.FDK.Common.OrderType Convert(SoftFX.Net.TradeCapture.OrderType type)
            {
                switch (type)
                {
                    case SoftFX.Net.TradeCapture.OrderType.Market:
                        return TickTrader.FDK.Common.OrderType.Market;

                    case SoftFX.Net.TradeCapture.OrderType.MarketWithSlippage:
                        return TickTrader.FDK.Common.OrderType.MarketWithSlippage;

                    case SoftFX.Net.TradeCapture.OrderType.Limit:
                        return TickTrader.FDK.Common.OrderType.Limit;

                    case SoftFX.Net.TradeCapture.OrderType.Stop:
                        return TickTrader.FDK.Common.OrderType.Stop;

                    case SoftFX.Net.TradeCapture.OrderType.Position:
                        return TickTrader.FDK.Common.OrderType.Position;

                    case SoftFX.Net.TradeCapture.OrderType.StopLimit:
                        return TickTrader.FDK.Common.OrderType.StopLimit;

                    default:
                        throw new Exception("Invalid order type : " + type);
                }
            }

            TickTrader.FDK.Common.OrderSide Convert(SoftFX.Net.TradeCapture.OrderSide side)
            {
                switch (side)
                {
                    case SoftFX.Net.TradeCapture.OrderSide.Buy:
                        return TickTrader.FDK.Common.OrderSide.Buy;

                    case SoftFX.Net.TradeCapture.OrderSide.Sell:
                        return TickTrader.FDK.Common.OrderSide.Sell;

                    default:
                        throw new Exception("Invalid order side : " + side);
                }
            }

            TickTrader.FDK.Common.OrderTimeInForce Convert(SoftFX.Net.TradeCapture.OrderTimeInForce timeInForce)
            {
                switch (timeInForce)
                {
                    case SoftFX.Net.TradeCapture.OrderTimeInForce.GoodTillCancel:
                        return TickTrader.FDK.Common.OrderTimeInForce.GoodTillCancel;

                    case SoftFX.Net.TradeCapture.OrderTimeInForce.ImmediateOrCancel:
                        return TickTrader.FDK.Common.OrderTimeInForce.ImmediateOrCancel;

                    case SoftFX.Net.TradeCapture.OrderTimeInForce.GoodTillDate:
                        return TickTrader.FDK.Common.OrderTimeInForce.GoodTillDate;

                    default:
                        throw new Exception("Invalid order time in force : " + timeInForce);
                }
            }

            TickTrader.FDK.Common.OrderSide Convert(SoftFX.Net.TradeCapture.PosType posType)
            {
                switch (posType)
                {
                    case SoftFX.Net.TradeCapture.PosType.Long:
                        return TickTrader.FDK.Common.OrderSide.Buy;

                    case SoftFX.Net.TradeCapture.PosType.Short:
                        return TickTrader.FDK.Common.OrderSide.Sell;

                    default:
                        throw new Exception("Invalid position type : " + posType);
                }
            }

            TickTrader.FDK.Common.TradeRecordType ConvertToTradeRecordType(SoftFX.Net.TradeCapture.OrderType type, SoftFX.Net.TradeCapture.OrderTimeInForce? timeInForce)
            {
                if (timeInForce == SoftFX.Net.TradeCapture.OrderTimeInForce.ImmediateOrCancel)
                    return TickTrader.FDK.Common.TradeRecordType.IoC;

                switch (type)
                {
                    case SoftFX.Net.TradeCapture.OrderType.Market:
                        return TickTrader.FDK.Common.TradeRecordType.Market;

                    case SoftFX.Net.TradeCapture.OrderType.MarketWithSlippage:
                        return TickTrader.FDK.Common.TradeRecordType.MarketWithSlippage;

                    case SoftFX.Net.TradeCapture.OrderType.Limit:
                        return TickTrader.FDK.Common.TradeRecordType.Limit;

                    case SoftFX.Net.TradeCapture.OrderType.Stop:
                        return TickTrader.FDK.Common.TradeRecordType.Stop;

                    case SoftFX.Net.TradeCapture.OrderType.Position:
                        return TickTrader.FDK.Common.TradeRecordType.Position;

                    case SoftFX.Net.TradeCapture.OrderType.StopLimit:
                        return TickTrader.FDK.Common.TradeRecordType.StopLimit;

                    default:
                        throw new Exception("Invalid order type : " + type);
                }
            }

            TickTrader.FDK.Common.TradeRecordSide ConvertToTradeRecordSide(SoftFX.Net.TradeCapture.OrderSide side)
            {
                switch (side)
                {
                    case SoftFX.Net.TradeCapture.OrderSide.Buy:
                        return TickTrader.FDK.Common.TradeRecordSide.Buy;

                    case SoftFX.Net.TradeCapture.OrderSide.Sell:
                        return TickTrader.FDK.Common.TradeRecordSide.Sell;

                    default:
                        throw new Exception("Invalid order side : " + side);
                }
            }

            TickTrader.FDK.Common.NotificationType Convert(SoftFX.Net.TradeCapture.NotificationType type)
            {
                switch (type)
                {
                    case SoftFX.Net.TradeCapture.NotificationType.MarginCall:
                        return TickTrader.FDK.Common.NotificationType.MarginCall;

                    case SoftFX.Net.TradeCapture.NotificationType.MarginCallRevocation:
                        return TickTrader.FDK.Common.NotificationType.MarginCallRevocation;

                    case SoftFX.Net.TradeCapture.NotificationType.StopOut:
                        return TickTrader.FDK.Common.NotificationType.StopOut;

                    case SoftFX.Net.TradeCapture.NotificationType.ConfigUpdate:
                        return TickTrader.FDK.Common.NotificationType.ConfigUpdated;

                    default:
                        throw new Exception("Invalid notification type : " + type);
                }
            }

            TickTrader.FDK.Common.NotificationSeverity Convert(SoftFX.Net.TradeCapture.NotificationSeverity severity)
            {
                switch (severity)
                {
                    case SoftFX.Net.TradeCapture.NotificationSeverity.Info:
                        return TickTrader.FDK.Common.NotificationSeverity.Information;

                    case SoftFX.Net.TradeCapture.NotificationSeverity.Warning:
                        return TickTrader.FDK.Common.NotificationSeverity.Warning;

                    case SoftFX.Net.TradeCapture.NotificationSeverity.Error:
                        return TickTrader.FDK.Common.NotificationSeverity.Error;

                    default:
                        throw new Exception("Invalid notification severity : " + severity);
                }
            }

            Client client_;
            TradeTransactionReport tradeTransactionReport_;
        }

        internal static void ConvertToSync(Task task, int timeout)
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

        internal static TResult ConvertToSync<TResult>(Task<TResult> task, int timeout)
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
