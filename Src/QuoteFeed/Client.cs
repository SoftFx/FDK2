using System; 
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using SoftFX.Net.QuoteFeed;
using TickTrader.FDK.Common;

namespace TickTrader.FDK.QuoteFeed
{
    public class Client : IDisposable
    {
        #region Constructors

        public Client
        (
            string name,
            bool logMessages =  false,
            int port = 5030,
            int connectAttempts = -1,
            int reconnectAttempts = -1,
            int connectInterval = 10000,
            int heartbeatInterval = 10000,
            string logDirectory = "Logs"           
        )
        {
            ClientSessionOptions options = new ClientSessionOptions(port);
            options.ConnectionType = SoftFX.Net.Core.ConnectionType.Secure;
            options.ServerCertificateName = "TickTraderManagerService";
            options.ConnectMaxCount = connectAttempts;
            options.ReconnectMaxCount = reconnectAttempts;
            options.ConnectInterval = connectInterval;
            options.HeartbeatInterval = heartbeatInterval;
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

        public delegate void ConnectResultDelegate(Client client, object data);
        public delegate void ConnectErrorDelegate(Client client, object data, Exception exception);
        public delegate void DisconnectResultDelegate(Client client, object data, string text);
        public delegate void DisconnectDelegate(Client client, string text);
        public delegate void ReconnectDelegate(Client client);
        public delegate void ReconnectErrorDelegate(Client client, Exception exception);

        public event ConnectResultDelegate ConnectResultEvent;
        public event ConnectErrorDelegate ConnectErrorEvent;
        public event DisconnectResultDelegate DisconnectResultEvent;
        public event DisconnectDelegate DisconnectEvent;
        public event ReconnectDelegate ReconnectEvent;
        public event ReconnectErrorDelegate ReconnectErrorEvent;

        public void Connect(string address, int timeout)
        {
            ConnectAsyncContext context = new ConnectAsyncContext(true);

            ConnectInternal(context, address);

            if (! context.Wait(timeout))
            {
                DisconnectInternal(null, "Connect timeout");

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

        public bool Disconnect(string text)
        {
            bool result;

            DisconnectAsyncContext context = new DisconnectAsyncContext(true);

            if (DisconnectInternal(context, text))
            {
                context.Wait(-1);

                result = true;
            }
            else
                result = false;

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

        public delegate void LoginResultDelegate(Client client, object data);
        public delegate void LoginErrorDelegate(Client client, object data, Exception exception);
        public delegate void TwoFactorLoginRequestDelegate(Client client, string message);
        public delegate void TwoFactorLoginResultDelegate(Client client, object data, DateTime expireTime);
        public delegate void TwoFactorLoginErrorDelegate(Client client, object data, Exception exception);
        public delegate void TwoFactorLoginResumeDelegate(Client client, object data, DateTime expireTime);
        public delegate void LogoutResultDelegate(Client client, object data, LogoutInfo logoutInfo);
        public delegate void LogoutErrorDelegate(Client client, object data, Exception exception);
        public delegate void LogoutDelegate(Client client, LogoutInfo logoutInfo);

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

        #region Quote Feed

        public delegate void CurrencyListResultDelegate(Client client, object data, CurrencyInfo[] currencyInfos);
        public delegate void CurrencyListErrorDelegate(Client client, object data, Exception exception);
        public delegate void SymbolListResultDelegate(Client client, object data, SymbolInfo[] symbolInfos);
        public delegate void SymbolListErrorDelegate(Client client, object data, Exception exception);
        public delegate void SessionInfoResultDelegate(Client client, object data, SessionInfo sessionInfo);
        public delegate void SessionInfoErrorDelegate(Client client, object data, Exception exception);
        public delegate void SubscribeQuotesResultDelegate(Client client, object data, Quote[] quotes);
        public delegate void SubscribeQuotesErrorDelegate(Client client, object data, Exception exception);
        public delegate void UnsubscribeQuotesResultDelegate(Client client, object data, string[] symbolIds);
        public delegate void UnsubscribeQuotesErrorDelegate(Client client, object data, Exception exception);
        public delegate void QuotesResultDelegate(Client client, object data, Quote[] quotes);
        public delegate void QuotesErrorDelegate(Client client, object data, Exception exception);
        public delegate void SessionInfoUpdateDelegate(Client client, SessionInfo sessionInfo);
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
            CurrencyListAsyncContext context = new CurrencyListAsyncContext(true);

            GetCurrencyListInternal(context);

            if (!context.Wait(timeout))
                throw new Common.TimeoutException("Method call timed out");

            if (context.exception_ != null)
                throw context.exception_;

            return context.currencyInfos_;
        }

        public void GetCurrencyListAsync(object data)
        {
            CurrencyListAsyncContext context = new CurrencyListAsyncContext(false);
            context.Data = data;

            GetCurrencyListInternal(context);
        }

        void GetCurrencyListInternal(CurrencyListAsyncContext context)
        {
            CurrencyListRequest request = new CurrencyListRequest(0);
            request.Id = Guid.NewGuid().ToString();
            request.Type = CurrencyListRequestType.All;

            session_.SendCurrencyListRequest(context, request);
        }

        public SymbolInfo[] GetSymbolList(int timeout)
        {
            SymbolListAsyncContext context = new SymbolListAsyncContext(true);

            GetSymbolListInternal(context);

            if (!context.Wait(timeout))
                throw new Common.TimeoutException("Method call timed out");

            if (context.exception_ != null)
                throw context.exception_;

            return context.symbolInfos_;
        }

        public void GetSymbolListAsync(object data)
        {
            SymbolListAsyncContext context = new SymbolListAsyncContext(false);
            context.Data = data;

            GetSymbolListInternal(context);
        }

        void GetSymbolListInternal(SymbolListAsyncContext context)
        {
            SymbolListRequest request = new SymbolListRequest(0);
            request.Id = Guid.NewGuid().ToString();
            request.Type = SymbolListRequestType.All;

            session_.SendSymbolListRequest(context, request);
        }

        public SessionInfo GetSessionInfo(int timeout)
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

        public void SubscribeQuotes(string[] symbolIds, int marketDepth, int timeout)
        {
            SubscribeQuotesAsyncContext context = new SubscribeQuotesAsyncContext(true);

            SubscribeQuotesInternal(context, symbolIds, marketDepth);

            if (!context.Wait(timeout))
                throw new Common.TimeoutException("Method call timed out");

            if (context.exception_ != null)
                throw context.exception_;
        }

        public void SubscribeQuotesAsync(object data, string[] symbolIds, int marketDepth)
        {
            SubscribeQuotesAsyncContext context = new SubscribeQuotesAsyncContext(false);
            context.Data = data;

            SubscribeQuotesInternal(context, symbolIds, marketDepth);
        }

        void SubscribeQuotesInternal(SubscribeQuotesAsyncContext context, string[] symbolIds, int marketDepth)
        {
            MarketDataRequest request = new MarketDataRequest(0);
            request.Id = Guid.NewGuid().ToString();
            request.RequestType = MarketDataRequestType.Subscribe;
            request.UpdateType = SoftFX.Net.QuoteFeed.MarketDataUpdateType.FullRefresh;
            request.MarketDepth = (ushort) marketDepth;

            StringArray requestSymbolIds = request.SymbolIds;
            int count = symbolIds.Length;
            requestSymbolIds.Resize(count);

            for (int index = 0; index < count; ++ index)
                requestSymbolIds[index] = symbolIds[index];

            session_.SendMarketDataRequest(context, request);
        }

        public void UnsbscribeQuotes(string[] symbolIds, int timeout)
        {
            UnsubscribeQuotesAsyncContext context = new UnsubscribeQuotesAsyncContext(true);

            UnsubscribeQuotesInteral(context, symbolIds);

            if (!context.Wait(timeout))
                throw new Common.TimeoutException("Method call timed out");

            if (context.exception_ != null)
                throw context.exception_;
        }

        public void UnsubscribeQuotesAsync(object data, string[] symbolIds)
        {
            UnsubscribeQuotesAsyncContext context = new UnsubscribeQuotesAsyncContext(false);
            context.Data = data;

            UnsubscribeQuotesInteral(context, symbolIds);
        }

        void UnsubscribeQuotesInteral(UnsubscribeQuotesAsyncContext context, string[] symbolIds)
        {
            context.SymbolIds = symbolIds;

            MarketDataRequest request = new MarketDataRequest(0);
            request.Id = Guid.NewGuid().ToString();
            request.RequestType = MarketDataRequestType.Unsubscribe;

            StringArray requestSymbolIds = request.SymbolIds;
            int count = symbolIds.Length;
            requestSymbolIds.Resize(count);

            for (int index = 0; index < count; ++index)
                requestSymbolIds[index] = symbolIds[index];

           session_.SendMarketDataRequest(context, request);
        }

        public Quote[] GetQuotes(string[] symbolIds, int marketDepth, int timeout)
        {
            GetQuotesAsyncContext context = new GetQuotesAsyncContext(true);

            GetQuotesInternal(context, symbolIds, marketDepth);

            if (!context.Wait(timeout))
                throw new Common.TimeoutException("Method call timed out");

            if (context.exception_ != null)
                throw context.exception_;

            return context.quotes_;
        }

        public void GetQuotesAsync(object data, string[] symbolIds, int marketDepth)
        {
            GetQuotesAsyncContext context = new GetQuotesAsyncContext(false);
            context.Data = data;

            GetQuotesInternal(context, symbolIds, marketDepth);
        }

        void GetQuotesInternal(GetQuotesAsyncContext context, string[] symbolIds, int marketDepth)
        {
            MarketDataRequest request = new MarketDataRequest(0);
            request.Id = Guid.NewGuid().ToString();
            request.RequestType = MarketDataRequestType.Snapshot;
            request.UpdateType = SoftFX.Net.QuoteFeed.MarketDataUpdateType.FullRefresh;
            request.MarketDepth = (ushort)marketDepth;

            StringArray requestSymbolIds = request.SymbolIds;
            int count = symbolIds.Length;
            requestSymbolIds.Resize(count);

            for (int index = 0; index < count; ++index)
                requestSymbolIds[index] = symbolIds[index];

            session_.SendMarketDataRequest(context, request);
        }

        #endregion

        #region Implementation

        interface IAsyncContext
        {
            void ProcessDisconnect(Client client, string text);
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

            public void ProcessDisconnect(Client client, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (client.LoginErrorEvent != null)
                {
                    try
                    {
                        client.LoginErrorEvent(client, Data, exception);
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

            public void ProcessDisconnect(Client client, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (client.LoginErrorEvent != null)
                {
                    try
                    {
                        client.LoginErrorEvent(client, Data, exception);
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

            public void ProcessDisconnect(Client client, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (client.LoginErrorEvent != null)
                {
                    try
                    {
                        client.LoginErrorEvent(client, Data, exception);
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

            public void ProcessDisconnect(Client client, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (client.LogoutErrorEvent != null)
                {
                    try
                    {
                        client.LogoutErrorEvent(client, Data, exception);
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

        class CurrencyListAsyncContext : CurrencyListRequestClientContext, IAsyncContext
        {
            public CurrencyListAsyncContext(bool waitable) : base(waitable)
            {
            }

            public void ProcessDisconnect(Client client, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (client.CurrencyListErrorEvent != null)
                {
                    try
                    {
                        client.CurrencyListErrorEvent(client, Data, exception);
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
            public CurrencyInfo[] currencyInfos_;
        }

        class SymbolListAsyncContext : SymbolListRequestClientContext, IAsyncContext
        {
            public SymbolListAsyncContext(bool waitable) : base(waitable)
            {
            }

            public void ProcessDisconnect(Client client, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (client.SymbolListErrorEvent != null)
                {
                    try
                    {
                        client.SymbolListErrorEvent(client, Data, exception);
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
            public SymbolInfo[] symbolInfos_;
        }

        class SessionInfoAsyncContext : TradingSessionStatusRequestClientContext, IAsyncContext
        {
            public SessionInfoAsyncContext(bool waitable) : base(waitable)
            {
            }

            public void ProcessDisconnect(Client client, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (client.SymbolListErrorEvent != null)
                {
                    try
                    {
                        client.SymbolListErrorEvent(client, Data, exception);
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
            public SessionInfo sessionInfo_;
        }

        class SubscribeQuotesAsyncContext : MarketDataRequestClientContext, IAsyncContext
        {
            public SubscribeQuotesAsyncContext(bool waitable) : base(waitable)
            {
            }

            public void ProcessDisconnect(Client client, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (client.SubscribeQuotesErrorEvent != null)
                {
                    try
                    {
                        client.SubscribeQuotesErrorEvent(client, Data, exception);
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

        class UnsubscribeQuotesAsyncContext : MarketDataRequestClientContext, IAsyncContext
        {
            public UnsubscribeQuotesAsyncContext(bool waitable) : base(waitable)
            {
            }            

            public void ProcessDisconnect(Client client, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (client.UnsubscribeQuotesErrorEvent != null)
                {
                    try
                    {
                        client.UnsubscribeQuotesErrorEvent(client, Data, exception);
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

            public string[] SymbolIds;
            public Exception exception_;
        }

        class GetQuotesAsyncContext : MarketDataRequestClientContext, IAsyncContext
        {
            public GetQuotesAsyncContext(bool waitable) : base(waitable)
            {
            }

            public void ProcessDisconnect(Client client, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (client.QuotesErrorEvent != null)
                {
                    try
                    {
                        client.QuotesErrorEvent(client, Data, exception);
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
            public Quote[] quotes_;
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
                }
            }

            public override void OnConnectError(ClientSession clientSession, ConnectClientContext connectContext, string text)
            {                
                try
                {
                    ConnectAsyncContext connectAsyncContext = (ConnectAsyncContext)connectContext;

                    Exception exception = new Exception(text);

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
                }
            }

            public override void OnConnectError(ClientSession clientSession, string text)
            {                
                try
                {
                    Exception exception = new Exception(text);

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
                }
            }

            public override void OnDisconnect(ClientSession clientSession, DisconnectClientContext disconnectContext, ClientContext[] contexts, string text)
            {
                try
                {
                    DisconnectAsyncContext disconnectAsyncContext = (DisconnectAsyncContext) disconnectContext;

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
                }
            }

            public override void OnLoginReport(ClientSession session, LoginRequestClientContext LoginRequestClientContext, LoginReport message)
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

            public override void OnLoginReject(ClientSession session, LoginRequestClientContext LoginRequestClientContext, LoginReject message)
            {
                LoginAsyncContext context = (LoginAsyncContext) LoginRequestClientContext;

                try
                {
                    TickTrader.FDK.Common.LoginRejectReason reason = Convert(message.Reason);

                    LoginRejectException exception = new LoginRejectException(reason, message.Text);

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

            public override void OnTwoFactorLoginRequest(ClientSession session, LoginRequestClientContext LoginRequestClientContext, TwoFactorLogin message)
            {
                LoginAsyncContext context = (LoginAsyncContext) LoginRequestClientContext;

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

            public override void OnTwoFactorLoginSuccess(ClientSession session, LoginRequestClientContext LoginRequestClientContext, TwoFactorLoginResponseClientContext TwoFactorLoginResponseClientContext, TwoFactorLogin message)
            {
                LoginAsyncContext loginContext = (LoginAsyncContext) LoginRequestClientContext;

                try
                {
                    TwoFactorLoginResponseAsyncContext responseContext = (TwoFactorLoginResponseAsyncContext) TwoFactorLoginResponseClientContext;

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

            public override void OnTwoFactorLoginReject(ClientSession session, LoginRequestClientContext LoginRequestClientContext, TwoFactorLoginResponseClientContext TwoFactorLoginResponseClientContext, TwoFactorReject message)
            {
                LoginAsyncContext loginContext = (LoginAsyncContext) LoginRequestClientContext;

                try
                {
                    TwoFactorLoginResponseAsyncContext responseContext = (TwoFactorLoginResponseAsyncContext) TwoFactorLoginResponseClientContext;

                    try
                    {
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

            public override void OnTwoFactorLoginError(ClientSession session, LoginRequestClientContext LoginRequestClientContext, TwoFactorLoginResponseClientContext TwoFactorLoginResponseClientContext, TwoFactorLogin message)
            {
                LoginAsyncContext loginContext = (LoginAsyncContext) LoginRequestClientContext;

                try
                {
                    TwoFactorLoginResponseAsyncContext responseContext = (TwoFactorLoginResponseAsyncContext) TwoFactorLoginResponseClientContext;                                       

                    Exception exception1 = new Exception(message.Text);

                    try
                    {
                        if (client_.TwoFactorLoginErrorEvent != null)
                        {
                            try
                            {
                                client_.TwoFactorLoginErrorEvent(client_, responseContext.Data, exception1);
                            }
                            catch
                            {
                            }
                        }

                        if (responseContext.Waitable)
                        {
                            responseContext.exception_ = exception1;
                        }
                    }
                    catch (Exception exception2)
                    {
                        if (client_.TwoFactorLoginErrorEvent != null)
                        {
                            try
                            {
                                client_.TwoFactorLoginErrorEvent(client_, responseContext.Data, exception2);
                            }
                            catch
                            {
                            }
                        }

                        if (responseContext.Waitable)
                        {
                            responseContext.exception_ = exception2;
                        }

                        throw;
                    }                    

                    if (client_.LoginErrorEvent != null)
                    {
                        try
                        {
                            client_.LoginErrorEvent(client_, loginContext.Data, exception1);
                        }
                        catch
                        {
                        }
                    }

                    if (loginContext.Waitable)
                    {
                        loginContext.exception_ = exception1;
                    }
                }
                catch (Exception exception2)
                {
                    if (client_.LoginErrorEvent != null)
                    {
                        try
                        {
                            client_.LoginErrorEvent(client_, loginContext.Data, exception2);
                        }
                        catch
                        {
                        }
                    }

                    if (loginContext.Waitable)
                    {
                        loginContext.exception_ = exception2;
                    }
                }
            }

            public override void OnTwoFactorLoginResume(ClientSession session, TwoFactorLoginResumeClientContext TwoFactorLoginResumeClientContext, TwoFactorLogin message)
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

            public override void OnLogout(ClientSession session, LogoutClientContext LogoutClientContext, Logout message)
            {
                LogoutAsyncContext context = (LogoutAsyncContext)LogoutClientContext;

                try
                {
                    LogoutInfo result = new LogoutInfo();
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

            public override void OnCurrencyListReport(ClientSession session, CurrencyListRequestClientContext CurrencyListRequestClientContext, CurrencyListReport message)
            {
                CurrencyListAsyncContext context = (CurrencyListAsyncContext) CurrencyListRequestClientContext;

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

                    if (context.Waitable)
                    {
                        context.currencyInfos_ = resultCurrencies;
                    }
                }
                catch (Exception exception)
                {
                    if (client_.CurrencyListErrorEvent != null)
                    {
                        try
                        {
                            client_.CurrencyListErrorEvent(client_, context.Data, exception);
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

            public override void OnCurrencyListReject(ClientSession session, CurrencyListRequestClientContext CurrencyListRequestClientContext, Reject message)
            {
                CurrencyListAsyncContext context = (CurrencyListAsyncContext) CurrencyListRequestClientContext;

                try
                {
                    RejectException exception = new RejectException(RejectReason.None, message.Text);

                    if (client_.CurrencyListErrorEvent != null)
                    {
                        try
                        {
                            client_.CurrencyListErrorEvent(client_, context.Data, exception);
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
                catch (Exception exception)
                {
                    if (client_.CurrencyListErrorEvent != null)
                    {
                        try
                        {
                            client_.CurrencyListErrorEvent(client_, context.Data, exception);
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

            public override void OnSymbolListReport(ClientSession session, SymbolListRequestClientContext SymbolListRequestClientContext, SymbolListReport message)
            {
                SymbolListAsyncContext context = (SymbolListAsyncContext) SymbolListRequestClientContext;

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

                    if (context.Waitable)
                    {
                        context.symbolInfos_ = resultSymbols;
                    }
                }
                catch (Exception exception)
                {
                    if (client_.SymbolListErrorEvent != null)
                    {
                        try
                        {
                            client_.SymbolListErrorEvent(client_, context.Data, exception);
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

            public override void OnSymbolListReject(ClientSession session, SymbolListRequestClientContext SymbolListRequestClientContext, Reject message)
            {
                SymbolListAsyncContext context = (SymbolListAsyncContext) SymbolListRequestClientContext;

                try
                {
                    RejectException exception = new RejectException(RejectReason.None, message.Text);

                    if (client_.SymbolListErrorEvent != null)
                    {
                        try
                        {
                            client_.SymbolListErrorEvent(client_, context.Data, exception);
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
                catch (Exception exception)
                {
                    if (client_.SymbolListErrorEvent != null)
                    {
                        try
                        {
                            client_.SymbolListErrorEvent(client_, context.Data, exception);
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

            public override void OnTradingSessionStatusReport(ClientSession session, TradingSessionStatusRequestClientContext TradingSessionStatusRequestClientContext, TradingSessionStatusReport message)
            {
                SessionInfoAsyncContext context = (SessionInfoAsyncContext)TradingSessionStatusRequestClientContext;

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

            public override void OnTradingSessionStatusReject(ClientSession session, TradingSessionStatusRequestClientContext TradingSessionStatusRequestClientContext, Reject message)
            {
                SessionInfoAsyncContext context = (SessionInfoAsyncContext)TradingSessionStatusRequestClientContext;

                try
                {
                    RejectException exception = new RejectException(RejectReason.None, message.Text);

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

            public override void OnMarketDataReport(ClientSession session, MarketDataRequestClientContext MarketDataRequestClientContext, MarketDataReport message)
            {
                if (MarketDataRequestClientContext is SubscribeQuotesAsyncContext)
                {
                    // SubscribeQuotes

                    SubscribeQuotesAsyncContext context = (SubscribeQuotesAsyncContext) MarketDataRequestClientContext;

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
                    }
                    catch (Exception exception)
                    {
                        if (client_.SubscribeQuotesErrorEvent != null)
                        {
                            try
                            {
                                client_.SubscribeQuotesErrorEvent(client_, context.Data, exception);
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
                else if (MarketDataRequestClientContext is UnsubscribeQuotesAsyncContext)
                {
                    // UnsubscribeQuotes

                    UnsubscribeQuotesAsyncContext context = (UnsubscribeQuotesAsyncContext) MarketDataRequestClientContext;

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
                    }
                    catch (Exception exception)
                    {
                        if (client_.UnsubscribeQuotesErrorEvent != null)
                        {
                            try
                            {
                                client_.UnsubscribeQuotesErrorEvent(client_, context.Data, exception);
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
                else
                {
                    // GetQuotes

                    GetQuotesAsyncContext context = (GetQuotesAsyncContext) MarketDataRequestClientContext;

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

                        if (context.Waitable)
                        {
                            context.quotes_ = resultQuotes;
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.QuotesErrorEvent != null)
                        {
                            try
                            {
                                client_.QuotesErrorEvent(client_, context.Data, exception);
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
            }

            public override void OnMarketDataReject(ClientSession session, MarketDataRequestClientContext MarketDataRequestClientContext, Reject message)
            {
                if (MarketDataRequestClientContext is SubscribeQuotesAsyncContext)
                {
                    // SubscribeQuotes

                    SubscribeQuotesAsyncContext context = (SubscribeQuotesAsyncContext) MarketDataRequestClientContext;

                    try
                    {
                        RejectException exception = new RejectException(RejectReason.None, message.Text);

                        if (client_.SubscribeQuotesErrorEvent != null)
                        {
                            try
                            {
                                client_.SubscribeQuotesErrorEvent(client_, context.Data, exception);
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
                    catch (Exception exception)
                    {
                        if (client_.SubscribeQuotesErrorEvent != null)
                        {
                            try
                            {
                                client_.SubscribeQuotesErrorEvent(client_, context.Data, exception);
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
                else if (MarketDataRequestClientContext is UnsubscribeQuotesAsyncContext)
                {
                    // UnsubscribeQuotes

                    UnsubscribeQuotesAsyncContext context = (UnsubscribeQuotesAsyncContext) MarketDataRequestClientContext;

                    try
                    {
                        RejectException exception = new RejectException(RejectReason.None, message.Text);

                        if (client_.UnsubscribeQuotesErrorEvent != null)
                        {
                            try
                            {
                                client_.UnsubscribeQuotesErrorEvent(client_, context.Data, exception);
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
                    catch (Exception exception)
                    {
                        if (client_.UnsubscribeQuotesErrorEvent != null)
                        {
                            try
                            {
                                client_.UnsubscribeQuotesErrorEvent(client_, context.Data, exception);
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
                else
                {
                    // GetQuotes

                    GetQuotesAsyncContext context = (GetQuotesAsyncContext) MarketDataRequestClientContext;

                    try
                    {
                        RejectException exception = new RejectException(RejectReason.None, message.Text);

                        if (client_.QuotesErrorEvent != null)
                        {
                            try
                            {
                                client_.QuotesErrorEvent(client_, context.Data, exception);
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
                    catch (Exception exception)
                    {
                        if (client_.QuotesErrorEvent != null)
                        {
                            try
                            {
                                client_.QuotesErrorEvent(client_, context.Data, exception);
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

            TickTrader.FDK.Common.LoginRejectReason Convert(SoftFX.Net.QuoteFeed.LoginRejectReason reason)
            {
                switch (reason)
                {
                    case SoftFX.Net.QuoteFeed.LoginRejectReason.IncorrectCredentials:
                        return TickTrader.FDK.Common.LoginRejectReason.InvalidCredentials;

                    case SoftFX.Net.QuoteFeed.LoginRejectReason.AccountIsBlocked:
                        return TickTrader.FDK.Common.LoginRejectReason.BlockedAccount;

                    case SoftFX.Net.QuoteFeed.LoginRejectReason.AccountIsBlockedByAccessList:
                        return TickTrader.FDK.Common.LoginRejectReason.BlockedAccount;

                    case SoftFX.Net.QuoteFeed.LoginRejectReason.InvalidSessionId:
                        return TickTrader.FDK.Common.LoginRejectReason.Other;

                    case SoftFX.Net.QuoteFeed.LoginRejectReason.InvalidSpec:
                        return TickTrader.FDK.Common.LoginRejectReason.Other;

                    case SoftFX.Net.QuoteFeed.LoginRejectReason.NotEnoughRights:
                        return TickTrader.FDK.Common.LoginRejectReason.Other;

                    case SoftFX.Net.QuoteFeed.LoginRejectReason.TimeoutLogin:
                        return TickTrader.FDK.Common.LoginRejectReason.Other;

                    case SoftFX.Net.QuoteFeed.LoginRejectReason.WebApiDisabled:
                        return TickTrader.FDK.Common.LoginRejectReason.Other;

                    case SoftFX.Net.QuoteFeed.LoginRejectReason.Throttling:
                        return TickTrader.FDK.Common.LoginRejectReason.Throttling;

                    case SoftFX.Net.QuoteFeed.LoginRejectReason.InternalServerError:
                        return TickTrader.FDK.Common.LoginRejectReason.InternalServerError;

                    default:
                        throw new Exception("Invalid login reject reason : " + reason);
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

        #endregion
    }
}
