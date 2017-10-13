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

        // TODO: return Task ?
        public void ConnectAsync(string address)
        {
            session_.Connect(address);
        }

        public void Disconnect(string text)
        {
            session_.Disconnect(text);
            session_.Join();
        }

        // TODO: return Task ?
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
        public delegate void LoginErrorDelegate(Client client, object data, string message);
        public delegate void OneTimePasswordRequestDelegate(Client client, string message);
        public delegate void OneTimePasswordRejectDelegate(Client client, string message);
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
            Task result;

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
                AppSessionId = appSessionId
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

        #region Quote Feed

        public delegate void CurrencyListResultDelegate(Client client, object data, CurrencyInfo[] infos);
        public delegate void CurrencyListErrorDelegate(Client client, object data, string message);
        public delegate void SymbolListResultDelegate(Client client, object data, SymbolInfo[] infos);
        public delegate void SymbolListErrorDelegate(Client client, object data, string message);
        public delegate void SessionInfoResultDelegate(Client client, object data, SessionInfo info);
        public delegate void SessionInfoErrorDelegate(Client client, object data, string message);
        public delegate void SubscribeQuotesResultDelegate(Client client, object data);
        public delegate void SubscribeQuotesErrorDelegate(Client client, object data, string message);
        public delegate void UnsubscribeQuotesResultDelegate(Client client, object data);
        public delegate void UnsubscribeQuotesErrorDelegate(Client client, object data, string message);
        public delegate void QuotesResultDelegate(Client client, object data, Quote[] quotes);
        public delegate void QuotesErrorDelegate(Client client, object data, string message);
        public delegate void SessionInfoUpdateDelegate(Client client, SessionInfo info);
        public delegate void QuotesBeginDelegate(Client client, Quote[] quotes);
        public delegate void QuotesEndDelegate(Client client, string[] symbolIds);
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
        public event QuotesBeginDelegate QuotesBeginEvent;
        public event QuotesEndDelegate QuotesEndEvent;
        public event QuoteUpdateDelegate QuoteUpdateEvent;
        public event NotificationDelegate NotificationEvent;

        public CurrencyInfo[] GetCurrencyList(int timeout)
        {
            return ConvertToSync(GetCurrencyListAsync(null), timeout);
        }

        public Task<CurrencyInfo[]> GetCurrencyListAsync(object data)
        {
            Task<CurrencyInfo[]> result;

            // Create a new async context
            var context = new CurrencyListAsyncContext();
            context.Data = data;

            if (data == null)
            {
                context.taskCompletionSource_ = new TaskCompletionSource<CurrencyInfo[]>();
                result = context.taskCompletionSource_.Task;
            }
            else
                result = null;

            // Create a request
            var request = new CurrencyListRequest(0)
            {
                Id = Guid.NewGuid().ToString(),
                Type = CurrencyListRequestType.All
            };

            // Send request to the server
            session_.SendCurrencyListRequest(context, request);

            // Return result task
            return result;
        }

        public SymbolInfo[] GetSymbolList(int timeout)
        {
            return ConvertToSync(GetSymbolListAsync(null), timeout);
        }

        public Task<SymbolInfo[]> GetSymbolListAsync(object data)
        {
            Task<SymbolInfo[]> result;

            // Create a new async context
            var context = new SymbolListAsyncContext();
            context.Data = data;

            if (data == null)
            {
                context.taskCompletionSource_ = new TaskCompletionSource<SymbolInfo[]>();
                result = context.taskCompletionSource_.Task;
            }
            else
                result = null;

            // Create a request
            var request = new SecurityListRequest(0)
            {
                Id = Guid.NewGuid().ToString(),
                Type = SecurityListRequestType.All
            };

            // Send request to the server
            session_.SendSecurityListRequest(context, request);

            // Return result task
            return result;
        }

        public SessionInfo GetSessionInfo(int timeout)
        {
            return ConvertToSync(GetSessionInfoAsync(null), timeout);
        }

        public Task<SessionInfo> GetSessionInfoAsync(object data)
        {
            Task<SessionInfo> result;

            // Create a new async context
            var context = new SessionInfoAsyncContext();
            context.Data = data;

            if (data == null)
            {
                context.taskCompletionSource_ = new TaskCompletionSource<SessionInfo>();
                result = context.taskCompletionSource_.Task;
            }
            else
                result = null;

            // Create a request
            var request = new TradingSessionStatusRequest(0);
            request.Id = Guid.NewGuid().ToString();

            // Send request to the server
            session_.SendTradingSessionStatusRequest(context, request);

            // Return result task
            return result;
        }

        public void SubscribeQuotes(string[] symbolIds, int marketDepth, int timeout)
        {
            ConvertToSync(SubscribeQuotesAsync(null, symbolIds, marketDepth), timeout);
        }

        public Task SubscribeQuotesAsync(object data, string[] symbolIds, int marketDepth)
        {
            Task result;

            // Create a new async context
            var context = new SubscribeQuotesAsyncContext();
            context.Data = data;

            if (data == null)
            {
                context.taskCompletionSource_ = new TaskCompletionSource<object>();
                result = context.taskCompletionSource_.Task;
            }
            else
                result = null;

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

            // Return result task
            return result;
        }

        public void UnsbscribeQuotes(string[] symbolIds, int timeout)
        {
            ConvertToSync(UnsubscribeQuotesAsync(null, symbolIds), timeout);
        }

        public Task UnsubscribeQuotesAsync(object data, string[] symbolIds)
        {
            Task result;

            // Create a new async context
            var context = new UnsubscribeQuotesAsyncContext();
            context.Data = data;
            context.SymbolIds = symbolIds;

            if (data == null)
            {
                context.taskCompletionSource_ = new TaskCompletionSource<object>();
                result = context.taskCompletionSource_.Task;
            }
            else
                result = null;

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

            // Return result task
            return result;
        }

        public Quote[] GetQuotes(string[] symbolIds, int marketDepth, int timeout)
        {
            return ConvertToSync(GetQuotesAsync(null, symbolIds, marketDepth), timeout);
        }

        public Task<Quote[]> GetQuotesAsync(object data, string[] symbolIds, int marketDepth)
        {
            Task<Quote[]> result;

            // Create a new async context
            var context = new GetQuotesAsyncContext();
            context.Data = data;

            if (data == null)
            {
                context.taskCompletionSource_ = new TaskCompletionSource<Quote[]>();
                result = context.taskCompletionSource_.Task;
            }
            else
                result = null;

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

            // Return result task
            return result;
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

        class SymbolListAsyncContext : SecurityListRequestClientContext, IAsyncContext
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

            public override void OnSecurityListReport(ClientSession session, SecurityListRequestClientContext SecurityListRequestClientContext, SecurityListReport message)
            {
                var context = (SymbolListAsyncContext)SecurityListRequestClientContext;

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
                        resultSymbol.Currency = reportSymbol.CurrencyId;
                        resultSymbol.SettlementCurrency = reportSymbol.SettlCurrencyId;
                        resultSymbol.Description = reportSymbol.Description;
                        resultSymbol.Precision = (int) Math.Log(reportSymbol.ContractMultiplier, 10);
                        resultSymbol.RoundLot = reportSymbol.RoundLot;
                        resultSymbol.MinTradeVolume = reportSymbol.MinTradeVol;
                        resultSymbol.MaxTradeVolume = reportSymbol.MaxTradeVol;
                        resultSymbol.TradeVolumeStep = reportSymbol.TradeVolStep;
                        resultSymbol.ProfitCalcMode = Convert(reportSymbol.SettlCalcMode);
                        resultSymbol.MarginCalcMode = Convert(reportSymbol.CalcMode); 
                        resultSymbol.MarginHedge = reportSymbol.MarginHedge;
                        resultSymbol.MarginFactorFractional = reportSymbol.MarginFactor;
                        resultSymbol.ContractMultiplier = reportSymbol.ContractMultiplier;
                        resultSymbol.Color = (int) reportSymbol.Color;
                        resultSymbol.CommissionType = Convert(reportSymbol.CommissionType);
                        resultSymbol.CommissionChargeType = Convert(reportSymbol.CommissionChargeType);
                        resultSymbol.CommissionChargeMethod = Convert(reportSymbol.CommissionChargeMethod);
                        resultSymbol.LimitsCommission = reportSymbol.LimitsCommission;
                        resultSymbol.Commission = reportSymbol.Commission;
                        resultSymbol.SwapSizeShort = reportSymbol.SwapSizeShort;
                        resultSymbol.SwapSizeLong = reportSymbol.SwapSizeLong;
                        resultSymbol.DefaultSlippage = reportSymbol.DefaultSlippage;
                        resultSymbol.IsTradeEnabled = reportSymbol.TradeEnabled;
                        resultSymbol.GroupSortOrder = reportSymbol.SecuritySortOrder;
                        resultSymbol.SortOrder = reportSymbol.SortOrder;
                        resultSymbol.CurrencySortOrder = reportSymbol.CurrencySortOrder;
                        resultSymbol.SettlementCurrencySortOrder = reportSymbol.SettlCurrencySortOrder;
                        resultSymbol.CurrencyPrecision = reportSymbol.CurrencyPrecision;
                        resultSymbol.SettlementCurrencyPrecision = reportSymbol.SettlCurrencyPrecision;
                        resultSymbol.StatusGroupId = reportSymbol.StatusGroupId;
                        resultSymbol.SecurityName = reportSymbol.SecurityId;
                        resultSymbol.SecurityDescription = reportSymbol.SecurityDescription;
                        resultSymbol.StopOrderMarginReduction = reportSymbol.StopOrderMarginReduction;

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

            public override void OnSecurityListReject(ClientSession session, SecurityListRequestClientContext SecurityListRequestClientContext, Reject message)
            {
                var context = (SymbolListAsyncContext)SecurityListRequestClientContext;

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
                                client_.SubscribeQuotesResultEvent(client_, context.Data);
                            }
                            catch
                            {
                            }
                        }

                        if (client_.QuotesBeginEvent != null)
                        {
                            try
                            {
                                client_.QuotesBeginEvent(client_, resultQuotes);
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
                        if (client_.QuotesEndEvent != null)
                        {
                            try
                            {
                                client_.QuotesEndEvent(client_, context.SymbolIds);
                            }
                            catch
                            {
                            }
                        }

                        if (client_.UnsubscribeQuotesResultEvent != null)
                        {
                            try
                            {
                                client_.UnsubscribeQuotesResultEvent(client_, context.Data);
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

            TickTrader.FDK.Common.MarginCalcMode Convert(SoftFX.Net.QuoteFeed.CalcMode mode)
            {
                switch (mode)
                {
                    case SoftFX.Net.QuoteFeed.CalcMode.Forex:
                        return TickTrader.FDK.Common.MarginCalcMode.Forex;

                    case SoftFX.Net.QuoteFeed.CalcMode.Cfd:
                        return TickTrader.FDK.Common.MarginCalcMode.Cfd;

                    case SoftFX.Net.QuoteFeed.CalcMode.Futures:
                        return TickTrader.FDK.Common.MarginCalcMode.Futures;

                    case SoftFX.Net.QuoteFeed.CalcMode.CfdIndex:
                        return TickTrader.FDK.Common.MarginCalcMode.CfdIndex;

                    case SoftFX.Net.QuoteFeed.CalcMode.CfdLeverage:
                        return TickTrader.FDK.Common.MarginCalcMode.CfdLeverage;

                    default:
                        throw new Exception("Invalid calculation mode : " + mode);
                }
            }

            TickTrader.FDK.Common.ProfitCalcMode Convert(SoftFX.Net.QuoteFeed.SettlCalcMode mode)
            {
                switch (mode)
                {
                    case SoftFX.Net.QuoteFeed.SettlCalcMode.Forex:
                        return TickTrader.FDK.Common.ProfitCalcMode.Forex;

                    case SoftFX.Net.QuoteFeed.SettlCalcMode.Cfd:
                        return TickTrader.FDK.Common.ProfitCalcMode.Cfd;

                    case SoftFX.Net.QuoteFeed.SettlCalcMode.Futures:
                        return TickTrader.FDK.Common.ProfitCalcMode.Futures;

                    case SoftFX.Net.QuoteFeed.SettlCalcMode.CfdIndex:
                        return TickTrader.FDK.Common.ProfitCalcMode.CfdIndex;

                    case SoftFX.Net.QuoteFeed.SettlCalcMode.CfdLeverage:
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
