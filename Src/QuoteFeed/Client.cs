using System; 
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using SoftFX.Net.QuoteFeed;
using TickTrader.FDK.Common;

namespace TickTrader.FDK.QuoteFeed
{
    public class Client : IDisposable
    {
        #region Constructors

        public Client(string name) : this(name, 5030, true, "Logs", false)
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

            DisconnectInternal(context, text);
        }

        public Task DisconnectAsync(string text)
        {
            DisconnectAsyncContext context = new DisconnectAsyncContext();
            context.taskCompletionSource_ = new TaskCompletionSource<object>();

            DisconnectInternal(context, text);

            return context.taskCompletionSource_.Task;
        }

        void DisconnectInternal(DisconnectAsyncContext context, string text)
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
        public delegate void LoginErrorDelegate(Client client, object data, string message);
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
            LoginAsyncContext context = new LoginAsyncContext();
            context.Data = data;

            LoginInternal(context, username, password, deviceId, appId, sessionId);
        }

        public Task LoginAsync(string username, string password, string deviceId, string appId, string sessionId)
        {
            // Create a new async context
            LoginAsyncContext context = new LoginAsyncContext();
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

            // Send message to the server
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
            LogoutAsyncContext context = new LogoutAsyncContext();
            context.Data = data;

            LogoutInternal(context, message);
        }

        public Task<LogoutInfo> LogoutAsync(string message)
        {
            // Create a new async context
            LogoutAsyncContext context = new LogoutAsyncContext();
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

        #region Quote Feed

        public delegate void CurrencyListResultDelegate(Client client, object data, CurrencyInfo[] infos);
        public delegate void CurrencyListErrorDelegate(Client client, object data, string message);
        public delegate void SymbolListResultDelegate(Client client, object data, SymbolInfo[] infos);
        public delegate void SymbolListErrorDelegate(Client client, object data, string message);
        public delegate void SessionInfoResultDelegate(Client client, object data, SessionInfo info);
        public delegate void SessionInfoErrorDelegate(Client client, object data, string message);
        public delegate void SubscribeQuotesResultDelegate(Client client, object data, Quote[] quotes);
        public delegate void SubscribeQuotesErrorDelegate(Client client, object data, string message);
        public delegate void UnsubscribeQuotesResultDelegate(Client client, object data, string[] symbolIds);
        public delegate void UnsubscribeQuotesErrorDelegate(Client client, object data, string message);
        public delegate void QuotesResultDelegate(Client client, object data, Quote[] quotes);
        public delegate void QuotesErrorDelegate(Client client, object data, string message);
        public delegate void SessionInfoUpdateDelegate(Client client, SessionInfo info);
        public delegate void QuoteUpdateDelegate(Client client, Quote quote);        
        public delegate void NotificationDelegate(Client client, Common.Notification notification);

        public event CurrencyListResultDelegate CurrencyListResultEvent;
        public event CurrencyListErrorDelegate CurrencyListErrorEvent;
        public event SymbolListResultDelegate SymbolListResultEvent;
        public event SymbolListErrorDelegate SymbolListErrorEvent;
        public event SessionInfoResultDelegate SessionInfoResultEvent;
        public event SessionInfoErrorDelegate SessionInfoErrorEvent;        
        public event SubscribeQuotesResultDelegate SubscribeQuotesResultEvent;
        public event SubscribeQuotesErrorDelegate SubscribeQuotesErrorEvent;
        public event UnsubscribeQuotesResultDelegate UnsubscribeQuotesResultEvent;
        public event UnsubscribeQuotesErrorDelegate UnsubscribeQuotesErrorEvent;
        public event QuotesResultDelegate QuotesResultEvent;
        public event QuotesErrorDelegate QuotesErrorEvent;
        public event SessionInfoUpdateDelegate SessionInfoUpdateEvent;
        public event QuoteUpdateDelegate QuoteUpdateEvent;
        public event NotificationDelegate NotificationEvent;

        public CurrencyInfo[] GetCurrencyList(int timeout)
        {
            return ConvertToSync(GetCurrencyListAsync(), timeout);
        }

        public void GetCurrencyListAsync(object data)
        {
            // Create a new async context
            CurrencyListAsyncContext context = new CurrencyListAsyncContext();
            context.Data = data;

            GetCurrencyListInternal(context);
        }

        public Task<CurrencyInfo[]> GetCurrencyListAsync()
        {
            // Create a new async context
            CurrencyListAsyncContext context = new CurrencyListAsyncContext();
            context.taskCompletionSource_ = new TaskCompletionSource<CurrencyInfo[]>();

            GetCurrencyListInternal(context);

            return context.taskCompletionSource_.Task;
        }

        void GetCurrencyListInternal(CurrencyListAsyncContext context)
        {
            // Create a request
            var request = new CurrencyListRequest(0)
            {
                Id = Guid.NewGuid().ToString(),
                Type = CurrencyListRequestType.All
            };

            // Send request to the server
            session_.SendCurrencyListRequest(context, request);
        }

        public SymbolInfo[] GetSymbolList(int timeout)
        {
            return ConvertToSync(GetSymbolListAsync(), timeout);
        }

        public void GetSymbolListAsync(object data)
        {
            // Create a new async context
            SymbolListAsyncContext context = new SymbolListAsyncContext();
            context.Data = data;

            GetSymbolListInternal(context);
        }

        public Task<SymbolInfo[]> GetSymbolListAsync()
        {
            // Create a new async context
            var context = new SymbolListAsyncContext();
            context.taskCompletionSource_ = new TaskCompletionSource<SymbolInfo[]>();

            GetSymbolListInternal(context);

            return context.taskCompletionSource_.Task;
        }

        void GetSymbolListInternal(SymbolListAsyncContext context)
        {
            // Create a request
            var request = new SymbolListRequest(0)
            {
                Id = Guid.NewGuid().ToString(),
                Type = SymbolListRequestType.All
            };

            // Send request to the server
            session_.SendSymbolListRequest(context, request);
        }

        public SessionInfo GetSessionInfo(int timeout)
        {
            return ConvertToSync(GetSessionInfoAsync(), timeout);
        }

        public void GetSessionInfoAsync(object data)
        {
            // Create a new async context
            SessionInfoAsyncContext context = new SessionInfoAsyncContext();
            context.Data = data;

            GetSessionInfoInternal(context);
        }

        public Task<SessionInfo> GetSessionInfoAsync()
        {
            // Create a new async context
            SessionInfoAsyncContext context = new SessionInfoAsyncContext();
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

        public void SubscribeQuotes(string[] symbolIds, int marketDepth, int timeout)
        {
            ConvertToSync(SubscribeQuotesAsync(symbolIds, marketDepth), timeout);
        }

        public void SubscribeQuotesAsync(object data, string[] symbolIds, int marketDepth)
        {
            // Create a new async context
            SubscribeQuotesAsyncContext context = new SubscribeQuotesAsyncContext();
            context.Data = data;

            SubscribeQuotesInternal(context, symbolIds, marketDepth);
        }

        public Task SubscribeQuotesAsync(string[] symbolIds, int marketDepth)
        {
            // Create a new async context
            SubscribeQuotesAsyncContext context = new SubscribeQuotesAsyncContext();
            context.taskCompletionSource_ = new TaskCompletionSource<object>();

            SubscribeQuotesInternal(context, symbolIds, marketDepth);

            return context.taskCompletionSource_.Task;
        }

        void SubscribeQuotesInternal(SubscribeQuotesAsyncContext context, string[] symbolIds, int marketDepth)
        {
            // Create a request
            var request = new MarketDataRequest(0);
            request.Id = Guid.NewGuid().ToString();
            request.RequestType = MarketDataRequestType.Subscribe;
            request.UpdateType = SoftFX.Net.QuoteFeed.MarketDataUpdateType.FullRefresh;
            request.MarketDepth = (ushort) marketDepth;

            StringArray requestSymbolIds = request.SymbolIds;
            int count = symbolIds.Length;
            requestSymbolIds.Resize(count);

            for (int index = 0; index < count; ++ index)
                requestSymbolIds[index] = symbolIds[index];

            // Send request to the server
            session_.SendMarketDataRequest(context, request);
        }

        public void UnsbscribeQuotes(string[] symbolIds, int timeout)
        {
            ConvertToSync(UnsubscribeQuotesAsync(symbolIds), timeout);
        }

        public void UnsubscribeQuotesAsync(object data, string[] symbolIds)
        {
            // Create a new async context
            UnsubscribeQuotesAsyncContext context = new UnsubscribeQuotesAsyncContext();
            context.Data = data;

            UnsubscribeQuotesInteral(context, symbolIds);
        }

        public Task UnsubscribeQuotesAsync(string[] symbolIds)
        {
            // Create a new async context
            UnsubscribeQuotesAsyncContext context = new UnsubscribeQuotesAsyncContext();
            context.taskCompletionSource_ = new TaskCompletionSource<object>();

            UnsubscribeQuotesInteral(context, symbolIds);

            return context.taskCompletionSource_.Task;
        }

        void UnsubscribeQuotesInteral(UnsubscribeQuotesAsyncContext context, string[] symbolIds)
        {
            context.SymbolIds = symbolIds;

            // Create a request
            var request = new MarketDataRequest(0);
            request.Id = Guid.NewGuid().ToString();
            request.RequestType = MarketDataRequestType.Unsubscribe;

            StringArray requestSymbolIds = request.SymbolIds;
            int count = symbolIds.Length;
            requestSymbolIds.Resize(count);

            for (int index = 0; index < count; ++index)
                requestSymbolIds[index] = symbolIds[index];

            // Send request to the server
           session_.SendMarketDataRequest(context, request);
        }

        public Quote[] GetQuotes(string[] symbolIds, int marketDepth, int timeout)
        {
            return ConvertToSync(GetQuotesAsync(symbolIds, marketDepth), timeout);
        }

        public void GetQuotesAsync(object data, string[] symbolIds, int marketDepth)
        {
            // Create a new async context
            GetQuotesAsyncContext context = new GetQuotesAsyncContext();
            context.Data = data;

            GetQuotesInternal(context, symbolIds, marketDepth);
        }

        public Task<Quote[]> GetQuotesAsync(string[] symbolIds, int marketDepth)
        {
            // Create a new async context
            GetQuotesAsyncContext context = new GetQuotesAsyncContext();
            context.taskCompletionSource_ = new TaskCompletionSource<Quote[]>();

            GetQuotesInternal(context, symbolIds, marketDepth);

            return context.taskCompletionSource_.Task;
        }

        void GetQuotesInternal(GetQuotesAsyncContext context, string[] symbolIds, int marketDepth)
        {
            // Create a request
            var request = new MarketDataRequest(0);
            request.Id = Guid.NewGuid().ToString();
            request.RequestType = MarketDataRequestType.Snapshot;
            request.UpdateType = SoftFX.Net.QuoteFeed.MarketDataUpdateType.FullRefresh;
            request.MarketDepth = (ushort)marketDepth;

            StringArray requestSymbolIds = request.SymbolIds;
            int count = symbolIds.Length;
            requestSymbolIds.Resize(count);

            for (int index = 0; index < count; ++index)
                requestSymbolIds[index] = symbolIds[index];

            // Send request to the server
            session_.SendMarketDataRequest(context, request);
        }

        #endregion

        #region Implementation

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

        class CurrencyListAsyncContext : CurrencyListRequestClientContext, IAsyncContext
        {
            public CurrencyListAsyncContext() : base(false)
            {
            }

            public void SetDisconnectError(Exception exception)
            {
                if (taskCompletionSource_ != null)
                    taskCompletionSource_.SetException(exception);
            }

            public TaskCompletionSource<CurrencyInfo[]> taskCompletionSource_;
        }

        class SymbolListAsyncContext : SymbolListRequestClientContext, IAsyncContext
        {
            public SymbolListAsyncContext() : base(false)
            {
            }

            public void SetDisconnectError(Exception exception)
            {
                if (taskCompletionSource_ != null)
                    taskCompletionSource_.SetException(exception);
            }

            public TaskCompletionSource<SymbolInfo[]> taskCompletionSource_;
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

            public TaskCompletionSource<SessionInfo> taskCompletionSource_;
        }

        class SubscribeQuotesAsyncContext : MarketDataRequestClientContext, IAsyncContext
        {
            public SubscribeQuotesAsyncContext() : base(false)
            {
            }

            public void SetDisconnectError(Exception exception)
            {
                if (taskCompletionSource_ != null)
                    taskCompletionSource_.SetException(exception);
            }

            public TaskCompletionSource<object> taskCompletionSource_;
        }

        class UnsubscribeQuotesAsyncContext : MarketDataRequestClientContext, IAsyncContext
        {
            public UnsubscribeQuotesAsyncContext() : base(false)
            {
            }

            public string[] SymbolIds;

            public void SetDisconnectError(Exception exception)
            {
                if (taskCompletionSource_ != null)
                    taskCompletionSource_.SetException(exception);
            }

            public TaskCompletionSource<object> taskCompletionSource_;
        }

        class GetQuotesAsyncContext : MarketDataRequestClientContext, IAsyncContext
        {
            public GetQuotesAsyncContext() : base(false)
            {
            }

            public void SetDisconnectError(Exception exception)
            {
                if (taskCompletionSource_ != null)
                    taskCompletionSource_.SetException(exception);
            }

            public TaskCompletionSource<Quote[]> taskCompletionSource_;
        }

        class ClientSessionListener : SoftFX.Net.QuoteFeed.ClientSessionListener
        {
            public ClientSessionListener(Client client)
            {
                client_ = client;
                quote_ = new Quote();
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
                var context = (LoginAsyncContext) LoginRequestClientContext;

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
                        Exception exception = new Exception(text);
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
                var context = (LogoutAsyncContext) LogoutClientContext ;

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

            public override void OnCurrencyListReport(ClientSession session, CurrencyListRequestClientContext CurrencyListRequestClientContext, CurrencyListReport message)
            {
                var context = (CurrencyListAsyncContext)CurrencyListRequestClientContext;

                try
                {
                    CurrencyArray reportCurrencies = message.Currencies;
                    int count = reportCurrencies.Length;
                    TickTrader.FDK.Common.CurrencyInfo[] resultCurrencies = new TickTrader.FDK.Common.CurrencyInfo[count];

                    for (int index = 0; index < count; ++index)
                    {
                        Currency reportCurrency = reportCurrencies[index];
                        TickTrader.FDK.Common.CurrencyInfo resultCurrency = new TickTrader.FDK.Common.CurrencyInfo();

                        resultCurrency.Name = reportCurrency.Id;
                        resultCurrency.Description = reportCurrency.Description;
                        resultCurrency.Precision = reportCurrency.Precision;
                        resultCurrency.SortOrder = reportCurrency.SortOrder;

                        resultCurrencies[index] = resultCurrency;
                    }

                    if (client_.CurrencyListResultEvent != null)
                    {
                        try
                        {
                            client_.CurrencyListResultEvent(client_, context.Data, resultCurrencies);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetResult(resultCurrencies);
                }
                catch (Exception exception)
                {
                    if (client_.CurrencyListErrorEvent != null)
                    {
                        try
                        {
                            client_.CurrencyListErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetException(exception);
                }
            }

            public override void OnCurrencyListReject(ClientSession session, CurrencyListRequestClientContext CurrencyListRequestClientContext, Reject message)
            {
                var context = (CurrencyListAsyncContext)CurrencyListRequestClientContext;

                try
                {
                    string text = message.Text;

                    if (client_.CurrencyListErrorEvent != null)
                    {
                        try
                        {
                            client_.CurrencyListErrorEvent(client_, context.Data, text);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                    {
                        Exception exception = new Exception(text);
                        context.taskCompletionSource_.SetException(exception);
                    }
                }
                catch (Exception exception)
                {
                    if (client_.CurrencyListErrorEvent != null)
                    {
                        try
                        {
                            client_.CurrencyListErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetException(exception);
                }
            }

            public override void OnSymbolListReport(ClientSession session, SymbolListRequestClientContext SymbolListRequestClientContext, SymbolListReport message)
            {
                var context = (SymbolListAsyncContext) SymbolListRequestClientContext;

                try
                {
                    SymbolArray reportSymbols = message.Symbols;
                    int count = reportSymbols.Length;
                    TickTrader.FDK.Common.SymbolInfo[] resultSymbols = new TickTrader.FDK.Common.SymbolInfo[count];

                    for (int index = 0; index < count; ++index)
                    {
                        Symbol reportSymbol = reportSymbols[index];
                        TickTrader.FDK.Common.SymbolInfo resultSymbol = new TickTrader.FDK.Common.SymbolInfo();

                        resultSymbol.Name = reportSymbol.Id;
                        resultSymbol.Currency = reportSymbol.MarginCurrId;
                        resultSymbol.SettlementCurrency = reportSymbol.ProfitCurrId;
                        resultSymbol.Description = reportSymbol.Description;
                        resultSymbol.Precision = reportSymbol.Precision;
                        resultSymbol.RoundLot = reportSymbol.RoundLot;
                        resultSymbol.MinTradeVolume = reportSymbol.MinTradeVol;
                        resultSymbol.MaxTradeVolume = reportSymbol.MaxTradeVol;
                        resultSymbol.TradeVolumeStep = reportSymbol.TradeVolStep;
                        resultSymbol.ProfitCalcMode = Convert(reportSymbol.ProfitCalcMode);
                        resultSymbol.MarginCalcMode = Convert(reportSymbol.MarginCalcMode); 
                        resultSymbol.MarginHedge = reportSymbol.MarginHedge;
                        resultSymbol.MarginFactorFractional = reportSymbol.MarginFactor;
                        resultSymbol.ContractMultiplier = reportSymbol.ContractMultiplier;
                        resultSymbol.Color = (int) reportSymbol.Color;
                        resultSymbol.CommissionType = Convert(reportSymbol.CommissionType);
                        resultSymbol.CommissionChargeType = Convert(reportSymbol.CommissionChargeType);
                        resultSymbol.CommissionChargeMethod = Convert(reportSymbol.CommissionChargeMethod);
                        resultSymbol.LimitsCommission = reportSymbol.LimitsCommission;
                        resultSymbol.Commission = reportSymbol.Commission;
                        resultSymbol.MinCommissionCurrency = reportSymbol.MinCommissionCurrId;
                        resultSymbol.MinCommission = reportSymbol.MinCommission;
                        resultSymbol.SwapType = Convert(reportSymbol.SwapType);
                        resultSymbol.TripleSwapDay = reportSymbol.TripleSwapDay;
                        resultSymbol.SwapSizeShort = reportSymbol.SwapSizeShort;
                        resultSymbol.SwapSizeLong = reportSymbol.SwapSizeLong;
                        resultSymbol.DefaultSlippage = reportSymbol.DefaultSlippage;
                        resultSymbol.IsTradeEnabled = reportSymbol.TradeEnabled;
                        resultSymbol.GroupSortOrder = reportSymbol.SecuritySortOrder;
                        resultSymbol.SortOrder = reportSymbol.SortOrder;
                        resultSymbol.CurrencySortOrder = reportSymbol.MarginCurrSortOrder;
                        resultSymbol.SettlementCurrencySortOrder = reportSymbol.ProfitCurrSortOrder;
                        resultSymbol.CurrencyPrecision = reportSymbol.MarginCurrPrecision;
                        resultSymbol.SettlementCurrencyPrecision = reportSymbol.ProfitCurrPrecision;
                        resultSymbol.StatusGroupId = reportSymbol.StatusGroupId;
                        resultSymbol.SecurityName = reportSymbol.SecurityId;
                        resultSymbol.SecurityDescription = reportSymbol.SecurityDescription;
                        resultSymbol.StopOrderMarginReduction = reportSymbol.StopOrderMarginReduction;
                        resultSymbol.HiddenLimitOrderMarginReduction = reportSymbol.HiddenLimitOrderMarginReduction;

                        resultSymbols[index] = resultSymbol;
                    }

                    if (client_.SymbolListResultEvent != null)
                    {
                        try
                        {
                            client_.SymbolListResultEvent(client_, context.Data, resultSymbols);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetResult(resultSymbols);
                }
                catch (Exception exception)
                {
                    if (client_.SymbolListErrorEvent != null)
                    {
                        try
                        {
                            client_.SymbolListErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetException(exception);
                }
            }

            public override void OnSymbolListReject(ClientSession session, SymbolListRequestClientContext SymbolListRequestClientContext, Reject message)
            {
                var context = (SymbolListAsyncContext) SymbolListRequestClientContext;

                try
                {
                    string text = message.Text;

                    if (client_.SymbolListErrorEvent != null)
                    {
                        try
                        {
                            client_.SymbolListErrorEvent(client_, context.Data, text);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                    {
                        Exception exception = new Exception(text);
                        context.taskCompletionSource_.SetException(exception);
                    }
                }
                catch (Exception exception)
                {
                    if (client_.SymbolListErrorEvent != null)
                    {
                        try
                        {
                            client_.SymbolListErrorEvent(client_, context.Data, exception.Message);
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
                var context = (SessionInfoAsyncContext)TradingSessionStatusRequestClientContext;

                try
                {
                    TickTrader.FDK.Common.SessionInfo resultStatusInfo = new TickTrader.FDK.Common.SessionInfo();
                    SoftFX.Net.QuoteFeed.TradingSessionStatusInfo reportStatusInfo = message.StatusInfo;

                    resultStatusInfo.Status = Convert(reportStatusInfo.Status);
                    resultStatusInfo.StartTime = reportStatusInfo.StartTime;
                    resultStatusInfo.EndTime = reportStatusInfo.EndTime;
                    resultStatusInfo.OpenTime = reportStatusInfo.OpenTime;
                    resultStatusInfo.CloseTime = reportStatusInfo.CloseTime;

                    TradingSessionStatusGroupArray reportGroups = reportStatusInfo.Groups;
                    int count = reportGroups.Length;
                    TickTrader.FDK.Common.StatusGroupInfo[] resultGroups = new TickTrader.FDK.Common.StatusGroupInfo[count];

                    for (int index = 0; index < count; ++index)
                    {
                        TradingSessionStatusGroup reportGroup = reportGroups[index];
                        TickTrader.FDK.Common.StatusGroupInfo resultGroup = new TickTrader.FDK.Common.StatusGroupInfo();

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
                var context = (SessionInfoAsyncContext)TradingSessionStatusRequestClientContext;

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
                        Exception exception = new Exception(text);
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

            public override void OnMarketDataReport(ClientSession session, MarketDataRequestClientContext MarketDataRequestClientContext, MarketDataReport message)
            {
                if (MarketDataRequestClientContext is SubscribeQuotesAsyncContext)
                {
                    // SubscribeQuotes

                    var context = (SubscribeQuotesAsyncContext) MarketDataRequestClientContext;

                    try
                    {
                        MarketDataSnapshotArray reportSnapshots = message.Snapshots;
                        int count = reportSnapshots.Length;
                        TickTrader.FDK.Common.Quote[] resultQuotes = new TickTrader.FDK.Common.Quote[count];

                        for (int index = 0; index < count; ++index)
                        {
                            MarketDataSnapshot reportSnapshot = reportSnapshots[index];

                            TickTrader.FDK.Common.Quote resultQuote = new TickTrader.FDK.Common.Quote();
                            resultQuote.Symbol = reportSnapshot.SymbolId;
                            resultQuote.Id = reportSnapshot.Id;
                            resultQuote.CreatingTime = reportSnapshot.OrigTime;

                            MarketDataEntryArray reportSnapshotEntries = reportSnapshot.Entries;
                            int count2 = reportSnapshotEntries.Length;

                            resultQuote.Bids.Clear();
                            resultQuote.Asks.Clear();

                            for (int index2 = 0; index2 < count2; ++ index2)
                            {
                                MarketDataEntry reportSnapshotEntry = reportSnapshotEntries[index2];

                                TickTrader.FDK.Common.QuoteEntry quoteEntry = new TickTrader.FDK.Common.QuoteEntry();
                                quoteEntry.Volume = reportSnapshotEntry.Size;
                                quoteEntry.Price = reportSnapshotEntry.Price;

                                if (reportSnapshotEntry.Type == MarketDataEntryType.Bid)
                                {
                                    resultQuote.Bids.Add(quoteEntry);
                                }
                                else
                                    resultQuote.Asks.Add(quoteEntry);
                            }

                            resultQuote.Bids.Reverse();

                            resultQuotes[index] = resultQuote;
                        }

                        if (client_.SubscribeQuotesResultEvent != null)
                        {
                            try
                            {
                                client_.SubscribeQuotesResultEvent(client_, context.Data, resultQuotes);
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
                        if (client_.SubscribeQuotesErrorEvent != null)
                        {
                            try
                            {
                                client_.SubscribeQuotesErrorEvent(client_, context.Data, exception.Message);
                            }
                            catch
                            {
                            }
                        }

                        if (context.taskCompletionSource_ != null)
                            context.taskCompletionSource_.SetException(exception);
                    }
                }
                else if (MarketDataRequestClientContext is UnsubscribeQuotesAsyncContext)
                {
                    // UnsubscribeQuotes

                    var context = (UnsubscribeQuotesAsyncContext) MarketDataRequestClientContext;

                    try
                    {
                        if (client_.UnsubscribeQuotesResultEvent != null)
                        {
                            try
                            {
                                client_.UnsubscribeQuotesResultEvent(client_, context.Data, context.SymbolIds);
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
                        if (client_.UnsubscribeQuotesErrorEvent != null)
                        {
                            try
                            {
                                client_.UnsubscribeQuotesErrorEvent(client_, context.Data, exception.Message);
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
                    // GetQuotes

                    var context = (GetQuotesAsyncContext) MarketDataRequestClientContext;

                    try
                    {
                        MarketDataSnapshotArray reportSnapshots = message.Snapshots;
                        int count = reportSnapshots.Length;
                        TickTrader.FDK.Common.Quote[] resultQuotes = new TickTrader.FDK.Common.Quote[count];

                        for (int index = 0; index < count; ++ index)
                        {
                            MarketDataSnapshot reportSnapshot = reportSnapshots[index];

                            TickTrader.FDK.Common.Quote resultQuote = new TickTrader.FDK.Common.Quote();
                            resultQuote.Symbol = reportSnapshot.SymbolId;
                            resultQuote.Id = reportSnapshot.Id;
                            resultQuote.CreatingTime = reportSnapshot.OrigTime;

                            MarketDataEntryArray reportSnapshotEntries = reportSnapshot.Entries;
                            int count2 = reportSnapshotEntries.Length;

                            resultQuote.Bids.Clear();
                            resultQuote.Asks.Clear();

                            for (int index2 = 0; index2 < count2; ++ index2)
                            {
                                MarketDataEntry reportSnapshotEntry = reportSnapshotEntries[index2];
                                TickTrader.FDK.Common.QuoteEntry quoteEntry = new TickTrader.FDK.Common.QuoteEntry();

                                quoteEntry.Volume = reportSnapshotEntry.Size;
                                quoteEntry.Price = reportSnapshotEntry.Price;

                                if (reportSnapshotEntry.Type == MarketDataEntryType.Bid)
                                {
                                    resultQuote.Bids.Add(quoteEntry);
                                }
                                else
                                    resultQuote.Asks.Add(quoteEntry);
                            }

                            resultQuote.Bids.Reverse();

                            resultQuotes[index] = resultQuote;
                        }

                        if (client_.QuotesResultEvent != null)
                        {
                            try
                            {
                                client_.QuotesResultEvent(client_, context.Data, resultQuotes);
                            }
                            catch
                            {
                            }
                        }

                        if (context.taskCompletionSource_ != null)
                            context.taskCompletionSource_.SetResult(resultQuotes);
                    }
                    catch (Exception exception)
                    {
                        if (client_.QuotesErrorEvent != null)
                        {
                            try
                            {
                                client_.QuotesErrorEvent(client_, context.Data, exception.Message);
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

            public override void OnMarketDataReject(ClientSession session, MarketDataRequestClientContext MarketDataRequestClientContext, Reject message)
            {
                if (MarketDataRequestClientContext is SubscribeQuotesAsyncContext)
                {
                    // SubscribeQuotes

                    SubscribeQuotesAsyncContext context = (SubscribeQuotesAsyncContext) MarketDataRequestClientContext;

                    try
                    {
                        string text = message.Text;

                        if (client_.SubscribeQuotesErrorEvent != null)
                        {
                            try
                            {
                                client_.SubscribeQuotesErrorEvent(client_, context.Data, text);
                            }
                            catch
                            {
                            }
                        }

                        if (context.taskCompletionSource_ != null)
                        {
                            Exception exception = new Exception(text);
                            context.taskCompletionSource_.SetException(exception);
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.SubscribeQuotesErrorEvent != null)
                        {
                            try
                            {
                                client_.SubscribeQuotesErrorEvent(client_, context.Data, exception.Message);
                            }
                            catch
                            {
                            }
                        }

                        if (context.taskCompletionSource_ != null)
                            context.taskCompletionSource_.SetException(exception);
                    }
                }
                else if (MarketDataRequestClientContext is UnsubscribeQuotesAsyncContext)
                {
                    // UnsubscribeQuotes

                    UnsubscribeQuotesAsyncContext context = (UnsubscribeQuotesAsyncContext) MarketDataRequestClientContext;

                    try
                    {
                        string text = message.Text;

                        if (client_.UnsubscribeQuotesErrorEvent != null)
                        {
                            try
                            {
                                client_.UnsubscribeQuotesErrorEvent(client_, context.Data, text);
                            }
                            catch
                            {
                            }
                        }

                        if (context.taskCompletionSource_ != null)
                        {
                            Exception exception = new Exception(text);
                            context.taskCompletionSource_.SetException(exception);
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.UnsubscribeQuotesErrorEvent != null)
                        {
                            try
                            {
                                client_.UnsubscribeQuotesErrorEvent(client_, context.Data, exception.Message);
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
                    // GetQuotes

                    GetQuotesAsyncContext context = (GetQuotesAsyncContext) MarketDataRequestClientContext;

                    try
                    {
                        string text = message.Text;

                        if (client_.QuotesErrorEvent != null)
                        {
                            try
                            {
                                client_.QuotesErrorEvent(client_, context.Data, text);
                            }
                            catch
                            {
                            }
                        }

                        if (context.taskCompletionSource_ != null)
                        {
                            Exception exception = new Exception(text);
                            context.taskCompletionSource_.SetException(exception);
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.QuotesErrorEvent != null)
                        {
                            try
                            {
                                client_.QuotesErrorEvent(client_, context.Data, exception.Message);
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

            public override void OnTradingSessionStatusUpdate(ClientSession session, TradingSessionStatusUpdate message)
            {
                try
                {
                    TickTrader.FDK.Common.SessionInfo resultStatusInfo = new TickTrader.FDK.Common.SessionInfo();
                    SoftFX.Net.QuoteFeed.TradingSessionStatusInfo reportStatusInfo = message.StatusInfo;

                    resultStatusInfo.Status = Convert(reportStatusInfo.Status);
                    resultStatusInfo.StartTime = reportStatusInfo.StartTime;
                    resultStatusInfo.EndTime = reportStatusInfo.EndTime;
                    resultStatusInfo.OpenTime = reportStatusInfo.OpenTime;
                    resultStatusInfo.CloseTime = reportStatusInfo.CloseTime;

                    TradingSessionStatusGroupArray reportGroups = reportStatusInfo.Groups;
                    int count = reportGroups.Length;
                    TickTrader.FDK.Common.StatusGroupInfo[] resultGroups = new TickTrader.FDK.Common.StatusGroupInfo[count];

                    for (int index = 0; index < count; ++index)
                    {
                        TradingSessionStatusGroup reportGroup = reportGroups[index];
                        TickTrader.FDK.Common.StatusGroupInfo resultGroup = new TickTrader.FDK.Common.StatusGroupInfo();

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

            public override void OnMarketDataRefresh(ClientSession session, MarketDataSnapshotRefresh message)
            {
                try
                {
                    MarketDataSnapshot snapshot = message.Snapshot;

                    quote_.Symbol = snapshot.SymbolId;
                    quote_.Id = snapshot.Id;
                    quote_.CreatingTime = snapshot.OrigTime;

                    MarketDataEntryArray snapshotEntries = snapshot.Entries;
                    int count = snapshotEntries.Length;

                    quote_.Bids.Clear();
                    quote_.Asks.Clear();

                    for (int index = 0; index < count; ++ index)
                    {
                        MarketDataEntry snapshotEntry = snapshotEntries[index];

                        TickTrader.FDK.Common.QuoteEntry quoteEntry = new TickTrader.FDK.Common.QuoteEntry();
                        quoteEntry.Volume = snapshotEntry.Size;
                        quoteEntry.Price = snapshotEntry.Price;

                        if (snapshotEntry.Type == MarketDataEntryType.Bid)
                        {
                            quote_.Bids.Add(quoteEntry);
                        }
                        else
                            quote_.Asks.Add(quoteEntry);
                    }

                    quote_.Bids.Reverse();

                    if (client_.QuoteUpdateEvent != null)
                    {
                        try
                        {
                            client_.QuoteUpdateEvent(client_, quote_);
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

            public override void OnNotification(ClientSession session, SoftFX.Net.QuoteFeed.Notification message)
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

            TickTrader.FDK.Common.LogoutReason Convert(SoftFX.Net.QuoteFeed.LogoutReason reason)
            {
                switch (reason)
                {
                    case SoftFX.Net.QuoteFeed.LogoutReason.ClientLogout:
                        return TickTrader.FDK.Common.LogoutReason.ClientInitiated;

                    case SoftFX.Net.QuoteFeed.LogoutReason.ServerLogout:
                        return TickTrader.FDK.Common.LogoutReason.ServerLogout;

                    case SoftFX.Net.QuoteFeed.LogoutReason.SlowConnection:
                        return TickTrader.FDK.Common.LogoutReason.SlowConnection;

                    case SoftFX.Net.QuoteFeed.LogoutReason.DeletedLogin:
                        return TickTrader.FDK.Common.LogoutReason.LoginDeleted;

                    case SoftFX.Net.QuoteFeed.LogoutReason.InternalServerError:
                        return TickTrader.FDK.Common.LogoutReason.ServerError;

                    case SoftFX.Net.QuoteFeed.LogoutReason.BlockedLogin:
                        return TickTrader.FDK.Common.LogoutReason.BlockedAccount;

                    default:
                        throw new Exception("Invalid logout reason : " + reason);
                }
            }

            TickTrader.FDK.Common.MarginCalcMode Convert(SoftFX.Net.QuoteFeed.MarginCalcMode mode)
            {
                switch (mode)
                {
                    case SoftFX.Net.QuoteFeed.MarginCalcMode.Forex:
                        return TickTrader.FDK.Common.MarginCalcMode.Forex;

                    case SoftFX.Net.QuoteFeed.MarginCalcMode.Cfd:
                        return TickTrader.FDK.Common.MarginCalcMode.Cfd;

                    case SoftFX.Net.QuoteFeed.MarginCalcMode.Futures:
                        return TickTrader.FDK.Common.MarginCalcMode.Futures;

                    case SoftFX.Net.QuoteFeed.MarginCalcMode.CfdIndex:
                        return TickTrader.FDK.Common.MarginCalcMode.CfdIndex;

                    case SoftFX.Net.QuoteFeed.MarginCalcMode.CfdLeverage:
                        return TickTrader.FDK.Common.MarginCalcMode.CfdLeverage;

                    default:
                        throw new Exception("Invalid calculation mode : " + mode);
                }
            }

            TickTrader.FDK.Common.ProfitCalcMode Convert(SoftFX.Net.QuoteFeed.ProfitCalcMode mode)
            {
                switch (mode)
                {
                    case SoftFX.Net.QuoteFeed.ProfitCalcMode.Forex:
                        return TickTrader.FDK.Common.ProfitCalcMode.Forex;

                    case SoftFX.Net.QuoteFeed.ProfitCalcMode.Cfd:
                        return TickTrader.FDK.Common.ProfitCalcMode.Cfd;

                    case SoftFX.Net.QuoteFeed.ProfitCalcMode.Futures:
                        return TickTrader.FDK.Common.ProfitCalcMode.Futures;

                    case SoftFX.Net.QuoteFeed.ProfitCalcMode.CfdIndex:
                        return TickTrader.FDK.Common.ProfitCalcMode.CfdIndex;

                    case SoftFX.Net.QuoteFeed.ProfitCalcMode.CfdLeverage:
                        return TickTrader.FDK.Common.ProfitCalcMode.CfdLeverage;

                    default:
                        throw new Exception("Invalid calculation mode : " + mode);
                }
            }

            TickTrader.FDK.Common.CommissionType Convert(SoftFX.Net.QuoteFeed.CommissionType type)
            {
                switch (type)
                {
                    case SoftFX.Net.QuoteFeed.CommissionType.Money:
                        return TickTrader.FDK.Common.CommissionType.Absolute;

                    case SoftFX.Net.QuoteFeed.CommissionType.Points:
                        return TickTrader.FDK.Common.CommissionType.PerUnit;

                    case SoftFX.Net.QuoteFeed.CommissionType.Percentage:
                        return TickTrader.FDK.Common.CommissionType.Percent;

                    default:
                        throw new Exception("Invalid commission type : " + type);
                }
            }

            TickTrader.FDK.Common.CommissionChargeType Convert(SoftFX.Net.QuoteFeed.CommissionChargeType type)
            {
                switch (type)
                {
                    case SoftFX.Net.QuoteFeed.CommissionChargeType.PerLot:
                        return TickTrader.FDK.Common.CommissionChargeType.PerLot;

                    case SoftFX.Net.QuoteFeed.CommissionChargeType.PerDeal:
                        return TickTrader.FDK.Common.CommissionChargeType.PerTrade;

                    default:
                        throw new Exception("Invalid commission charge type : " + type);
                }
            }

            TickTrader.FDK.Common.CommissionChargeMethod Convert(SoftFX.Net.QuoteFeed.CommissionChargeMethod method)
            {
                switch (method)
                {
                    case SoftFX.Net.QuoteFeed.CommissionChargeMethod.OneWay:
                        return TickTrader.FDK.Common.CommissionChargeMethod.OneWay;

                    case SoftFX.Net.QuoteFeed.CommissionChargeMethod.RoundTurn:
                        return TickTrader.FDK.Common.CommissionChargeMethod.RoundTurn;

                    default:
                        throw new Exception("Invalid commission charge method : " + method);
                }
            }

            TickTrader.FDK.Common.SwapType Convert(SoftFX.Net.QuoteFeed.SwapType swapType)
            {
                switch (swapType)
                {
                    case SoftFX.Net.QuoteFeed.SwapType.Points:
                        return TickTrader.FDK.Common.SwapType.Points;

                    case SoftFX.Net.QuoteFeed.SwapType.PercentPerYear:
                        return TickTrader.FDK.Common.SwapType.PercentPerYear;

                    default:
                        throw new Exception("Invalid swap type : " + swapType);
                }
            }

            TickTrader.FDK.Common.SessionStatus Convert(SoftFX.Net.QuoteFeed.TradingSessionStatus status)
            {
                switch (status)
                {
                    case SoftFX.Net.QuoteFeed.TradingSessionStatus.Open:
                        return TickTrader.FDK.Common.SessionStatus.Open;

                    case SoftFX.Net.QuoteFeed.TradingSessionStatus.Close:
                        return TickTrader.FDK.Common.SessionStatus.Closed;

                    default:
                        throw new Exception("Invalid trading session status : " + status);
                }
            }

            TickTrader.FDK.Common.NotificationType Convert(SoftFX.Net.QuoteFeed.NotificationType type)
            {
                switch (type)
                {
                    case SoftFX.Net.QuoteFeed.NotificationType.ConfigUpdate:
                        return TickTrader.FDK.Common.NotificationType.ConfigUpdated;

                    default:
                        throw new Exception("Invalid notification type : " + type);
                }
            }

            TickTrader.FDK.Common.NotificationSeverity Convert(SoftFX.Net.QuoteFeed.NotificationSeverity severity)
            {
                switch (severity)
                {
                    case SoftFX.Net.QuoteFeed.NotificationSeverity.Info:
                        return TickTrader.FDK.Common.NotificationSeverity.Information;

                    case SoftFX.Net.QuoteFeed.NotificationSeverity.Warning:
                        return TickTrader.FDK.Common.NotificationSeverity.Warning;

                    case SoftFX.Net.QuoteFeed.NotificationSeverity.Error:
                        return TickTrader.FDK.Common.NotificationSeverity.Error;

                    default:
                        throw new Exception("Invalid notification severity : " + severity);
                }
            }

            Client client_;
            Quote quote_;
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
