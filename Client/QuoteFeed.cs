using System;
using System.Net;
using SoftFX.Net.QuoteFeed;
using TickTrader.FDK.Common;

namespace TickTrader.FDK.Client
{
    public class QuoteFeed : IDisposable
    {
        #region Constructors

        public QuoteFeed
        (
            string name,
            bool logEvents = false,
            bool logStates = false,
            bool logMessages = false,
            int port = 5041,
            string serverCertificateName = "CN=*.soft-fx.com",
            int connectAttempts = -1,
            int reconnectAttempts = -1,
            int connectInterval = 10000,
            int heartbeatInterval = 30000,
            string logDirectory = "Logs",
            SoftFX.Net.Core.ClientCertificateValidation validateClientCertificate = null,
            SoftFX.Net.Core.ProxyType proxyType = SoftFX.Net.Core.ProxyType.None,
            IPAddress proxyAddress = null,
            int proxyPort = 0,
            string proxyUsername = null,
            string proxyPassword = null
        )
        {
            ClientSessionOptions options = new ClientSessionOptions(port, validateClientCertificate);
            options.ConnectionType = SoftFX.Net.Core.ConnectionType.SecureSocket;
            options.ServerCertificateName = serverCertificateName;
            options.ConnectMaxCount = connectAttempts;
            options.ReconnectMaxCount = reconnectAttempts;
            options.ConnectInterval = connectInterval;
            options.HeartbeatInterval = heartbeatInterval;
            options.SendBufferSize = 10 * 1024 * 1024;
            options.Log.Directory = logDirectory;
            options.Log.Events = logEvents;
            options.Log.States = logStates;
            options.Log.Messages = logMessages;
            options.ProxyType = proxyType;
            options.ProxyAddress = proxyAddress;
            options.ProxyPort = proxyPort;
            options.Username = proxyUsername;
            options.Password = proxyPassword;

            session_ = new ClientSession(name, options);
            CompressedStreamHandler compressedStreamHandler = new CompressedStreamHandler(session_);
            sessionListener_ = new ClientSessionListener(this, compressedStreamHandler);
            session_.Listener = sessionListener_;
            protocolSpec_ = new ProtocolSpec();
        }

        ClientSession session_;
        ClientSessionListener sessionListener_;
        ProtocolSpec protocolSpec_;

        public ProtocolSpec ProtocolSpec => protocolSpec_;

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

        public delegate void ConnectResultDelegate(QuoteFeed quoteFeed, object data);
        public delegate void ConnectErrorDelegate(QuoteFeed quoteFeed, object data, Exception exception);
        public delegate void DisconnectResultDelegate(QuoteFeed quoteFeed, object data, string text);
        public delegate void DisconnectDelegate(QuoteFeed quoteFeed, string text);
        public delegate void ReconnectDelegate(QuoteFeed quoteFeed);
        public delegate void ReconnectErrorDelegate(QuoteFeed quoteFeed, Exception exception);
        public delegate void SendDelegate(QuoteFeed quoteFeed);

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

            if (!context.Wait(timeout))
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

        public delegate void LoginResultDelegate(QuoteFeed quoteFeed, object data);
        public delegate void LoginErrorDelegate(QuoteFeed quoteFeed, object data, Exception exception);
        public delegate void LogoutResultDelegate(QuoteFeed quoteFeed, object data, LogoutInfo logoutInfo);
        public delegate void LogoutErrorDelegate(QuoteFeed quoteFeed, object data, Exception exception);
        public delegate void LogoutDelegate(QuoteFeed quoteFeed, LogoutInfo logoutInfo);

        public event LoginResultDelegate LoginResultEvent;
        public event LoginErrorDelegate LoginErrorEvent;
        public event LogoutResultDelegate LogoutResultEvent;
        public event LogoutErrorDelegate LogoutErrorEvent;
        public event LogoutDelegate LogoutEvent;

        public void Login(string username, string password, string deviceId, string appId, string sessionId, int timeout)
        {
            LoginAsyncContext context = new LoginAsyncContext(true);

            LoginInternal(context, username, password, deviceId, appId, sessionId);

            if (!context.Wait(timeout))
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
            protocolSpec_.InitQuoteFeedVersion(new ProtocolVersion(session_.MajorVersion, session_.MinorVersion));

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

        public delegate void CurrencyListResultDelegate(QuoteFeed quoteFeed, object data, CurrencyInfo[] currencyInfos);
        public delegate void CurrencyListErrorDelegate(QuoteFeed quoteFeed, object data, Exception exception);
        public delegate void SymbolListResultDelegate(QuoteFeed quoteFeed, object data, SymbolInfo[] symbolInfos);
        public delegate void SymbolListErrorDelegate(QuoteFeed quoteFeed, object data, Exception exception);
        public delegate void SessionInfoResultDelegate(QuoteFeed quoteFeed, object data, SessionInfo sessionInfo);
        public delegate void SessionInfoErrorDelegate(QuoteFeed quoteFeed, object data, Exception exception);
        public delegate void SubscribeQuotesResultDelegate(QuoteFeed quoteFeed, object data, Quote[] quotes);
        public delegate void SubscribeQuotesErrorDelegate(QuoteFeed quoteFeed, object data, Exception exception);
        public delegate void UnsubscribeQuotesResultDelegate(QuoteFeed quoteFeed, object data, string[] symbolIds);
        public delegate void UnsubscribeQuotesErrorDelegate(QuoteFeed quoteFeed, object data, Exception exception);
        public delegate void QuotesResultDelegate(QuoteFeed quoteFeed, object data, Quote[] quotes);
        public delegate void QuotesErrorDelegate(QuoteFeed quoteFeed, object data, Exception exception);
        public delegate void SessionInfoUpdateDelegate(QuoteFeed quoteFeed, SessionInfo sessionInfo);
        public delegate void QuoteUpdateDelegate(QuoteFeed quoteFeed, Quote quote);
        public delegate void NotificationDelegate(QuoteFeed quoteFeed, Common.Notification notification);

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

        public Quote[] SubscribeQuotes(SymbolEntry[] symbolEntries, int timeout)
        {
            SubscribeQuotesAsyncContext context = new SubscribeQuotesAsyncContext(true);

            SubscribeQuotesInternal(context, symbolEntries);

            if (!context.Wait(timeout))
                throw new Common.TimeoutException("Method call timed out");

            if (context.exception_ != null)
                throw context.exception_;

            return context.quotes_;
        }

        public void SubscribeQuotesAsync(object data, SymbolEntry[] symbolEntries)
        {
            SubscribeQuotesAsyncContext context = new SubscribeQuotesAsyncContext(false);
            context.Data = data;

            SubscribeQuotesInternal(context, symbolEntries);
        }

        void SubscribeQuotesInternal(SubscribeQuotesAsyncContext context, SymbolEntry[] symbolEntries)
        {
            MarketDataSubscribeRequest request = new MarketDataSubscribeRequest(0);
            request.Id = Guid.NewGuid().ToString();

            MarketDataSymbolEntryArray requestSymbolEnties = request.SymbolEntries;
            int count = symbolEntries.Length;
            requestSymbolEnties.Resize(count);

            for (int index = 0; index < count; ++index)
            {
                SymbolEntry symbolEntry = symbolEntries[index];
                MarketDataSymbolEntry marketDataSymbolEntry = requestSymbolEnties[index];

                marketDataSymbolEntry.Id = symbolEntry.Id;
                marketDataSymbolEntry.MarketDepth = symbolEntry.MarketDepth;
            }

            session_.SendMarketDataSubscribeRequest(context, request);
        }

        public void UnsubscribeQuotes(string[] symbolIds, int timeout)
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

            MarketDataUnsubscribeRequest request = new MarketDataUnsubscribeRequest(0);
            request.Id = Guid.NewGuid().ToString();

            SoftFX.Net.Core.StringArray requestSymbolIds = request.SymbolIds;
            int count = symbolIds.Length;
            requestSymbolIds.Resize(count);

            for (int index = 0; index < count; ++index)
                requestSymbolIds[index] = symbolIds[index];

            session_.SendMarketDataUnsubscribeRequest(context, request);
        }

        public Quote[] GetQuotes(SymbolEntry[] symbolEntries, int timeout)
        {
            GetQuotesAsyncContext context = new GetQuotesAsyncContext(true);

            GetQuotesInternal(context, symbolEntries);

            if (!context.Wait(timeout))
                throw new Common.TimeoutException("Method call timed out");

            if (context.exception_ != null)
                throw context.exception_;

            return context.quotes_;
        }

        public void GetQuotesAsync(object data, SymbolEntry[] symbolEntries)
        {
            GetQuotesAsyncContext context = new GetQuotesAsyncContext(false);
            context.Data = data;

            GetQuotesInternal(context, symbolEntries);
        }

        void GetQuotesInternal(GetQuotesAsyncContext context, SymbolEntry[] symbolEntries)
        {
            MarketDataRequest request = new MarketDataRequest(0);
            request.Id = Guid.NewGuid().ToString();

            MarketDataSymbolEntryArray requestSymbolEnties = request.SymbolEntries;
            int count = symbolEntries.Length;
            requestSymbolEnties.Resize(count);

            for (int index = 0; index < count; ++index)
            {
                SymbolEntry symbolEntry = symbolEntries[index];
                MarketDataSymbolEntry marketDataSymbolEntry = requestSymbolEnties[index];

                marketDataSymbolEntry.Id = symbolEntry.Id;
                marketDataSymbolEntry.MarketDepth = symbolEntry.MarketDepth;
            }

            session_.SendMarketDataRequest(context, request);
        }

        #endregion

        #region Implementation

        interface IAsyncContext
        {
            void ProcessDisconnect(QuoteFeed quoteFeed, string text);
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

            public void ProcessDisconnect(QuoteFeed quoteFeed, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (quoteFeed.LoginErrorEvent != null)
                {
                    try
                    {
                        quoteFeed.LoginErrorEvent(quoteFeed, Data, exception);
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

        class LogoutAsyncContext : LogoutClientContext, IAsyncContext
        {
            public LogoutAsyncContext(bool waitable) : base(waitable)
            {
            }

            public void ProcessDisconnect(QuoteFeed quoteFeed, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (quoteFeed.LogoutErrorEvent != null)
                {
                    try
                    {
                        quoteFeed.LogoutErrorEvent(quoteFeed, Data, exception);
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

            public void ProcessDisconnect(QuoteFeed quoteFeed, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (quoteFeed.CurrencyListErrorEvent != null)
                {
                    try
                    {
                        quoteFeed.CurrencyListErrorEvent(quoteFeed, Data, exception);
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

            public void ProcessDisconnect(QuoteFeed quoteFeed, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (quoteFeed.SymbolListErrorEvent != null)
                {
                    try
                    {
                        quoteFeed.SymbolListErrorEvent(quoteFeed, Data, exception);
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

            public void ProcessDisconnect(QuoteFeed quoteFeed, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (quoteFeed.SymbolListErrorEvent != null)
                {
                    try
                    {
                        quoteFeed.SymbolListErrorEvent(quoteFeed, Data, exception);
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

        class SubscribeQuotesAsyncContext : MarketDataSubscribeRequestClientContext, IAsyncContext
        {
            public SubscribeQuotesAsyncContext(bool waitable) : base(waitable)
            {
            }

            public void ProcessDisconnect(QuoteFeed quoteFeed, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (quoteFeed.SubscribeQuotesErrorEvent != null)
                {
                    try
                    {
                        quoteFeed.SubscribeQuotesErrorEvent(quoteFeed, Data, exception);
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

        class UnsubscribeQuotesAsyncContext : MarketDataUnsubscribeRequestClientContext, IAsyncContext
        {
            public UnsubscribeQuotesAsyncContext(bool waitable) : base(waitable)
            {
            }

            public void ProcessDisconnect(QuoteFeed quoteFeed, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (quoteFeed.UnsubscribeQuotesErrorEvent != null)
                {
                    try
                    {
                        quoteFeed.UnsubscribeQuotesErrorEvent(quoteFeed, Data, exception);
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

            public void ProcessDisconnect(QuoteFeed quoteFeed, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (quoteFeed.QuotesErrorEvent != null)
                {
                    try
                    {
                        quoteFeed.QuotesErrorEvent(quoteFeed, Data, exception);
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
            public ClientSessionListener(QuoteFeed quoteFeed, CompressedStreamHandler compressedStreamHandler)
            {
                client_ = quoteFeed;
                quote_ = new Quote();
                compressedStreamHandler_ = compressedStreamHandler;
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
                    ConnectAsyncContext connectAsyncContext = (ConnectAsyncContext)connectContext;

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
                    compressedStreamHandler_.Stop();
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
                    compressedStreamHandler_.Stop();
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
                    compressedStreamHandler_.Start(this);
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

            public override void OnLogout(ClientSession session, LogoutClientContext LogoutClientContext, Logout message)
            {
                try
                {
                    compressedStreamHandler_.Stop();

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

            public override void OnCurrencyListReport(ClientSession session, CurrencyListRequestClientContext CurrencyListRequestClientContext, CurrencyListReport message)
            {
                try
                {
                    CurrencyListAsyncContext context = (CurrencyListAsyncContext)CurrencyListRequestClientContext;

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
                            resultCurrency.Type = GetCurrencyType(reportCurrency.Type);
                            resultCurrency.Tax = reportCurrency.Tax.GetValueOrDefault();

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
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnCurrencyListReject(ClientSession session, CurrencyListRequestClientContext CurrencyListRequestClientContext, Reject message)
            {
                try
                {
                    CurrencyListAsyncContext context = (CurrencyListAsyncContext)CurrencyListRequestClientContext;

                    TickTrader.FDK.Common.RejectReason rejectReason = GetRejectReason(message.Reason);
                    RejectException exception = new RejectException(rejectReason, message.Text);

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
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnSymbolListReport(ClientSession session, SymbolListRequestClientContext SymbolListRequestClientContext, SymbolListReport message)
            {
                try
                {
                    SymbolListAsyncContext context = (SymbolListAsyncContext)SymbolListRequestClientContext;

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
                            resultSymbol.Currency = reportSymbol.MarginCurrencyId;
                            resultSymbol.SettlementCurrency = reportSymbol.ProfitCurrencyId;
                            resultSymbol.Description = reportSymbol.Description;
                            resultSymbol.ExtendedName = reportSymbol.ExtendedName;
                            resultSymbol.Precision = reportSymbol.Precision;
                            resultSymbol.RoundLot = reportSymbol.RoundLot;
                            resultSymbol.MinTradeVolume = reportSymbol.MinTradeVol;
                            resultSymbol.MaxTradeVolume = reportSymbol.MaxTradeVol;
                            resultSymbol.TradeVolumeStep = reportSymbol.TradeVolStep;
                            resultSymbol.ProfitCalcMode = GetProfitCalcMode(reportSymbol.ProfitCalcMode);
                            resultSymbol.MarginCalcMode = GetMarginCalcMode(reportSymbol.MarginCalcMode);
                            resultSymbol.MarginHedge = reportSymbol.MarginHedge;
                            resultSymbol.MarginFactorFractional = reportSymbol.MarginFactor;
                            resultSymbol.ContractMultiplier = reportSymbol.ContractMultiplier;
                            resultSymbol.Color = (int)reportSymbol.Color;
                            resultSymbol.CommissionType = GetCommissionType(reportSymbol.CommissionType);
                            resultSymbol.CommissionChargeType = GetCommissionChargeType(reportSymbol.CommissionChargeType);
                            resultSymbol.CommissionChargeMethod = GetCommissionChargeMethod(reportSymbol.CommissionChargeMethod);
                            resultSymbol.LimitsCommission = reportSymbol.LimitsCommission;
                            resultSymbol.Commission = reportSymbol.Commission;
                            resultSymbol.MinCommissionCurrency = reportSymbol.MinCommissionCurrencyId;
                            resultSymbol.MinCommission = reportSymbol.MinCommission;
                            resultSymbol.SwapType = GetSwapType(reportSymbol.SwapType);
                            resultSymbol.TripleSwapDay = reportSymbol.TripleSwapDay;
                            resultSymbol.SwapSizeShort = reportSymbol.SwapSizeShort;
                            resultSymbol.SwapSizeLong = reportSymbol.SwapSizeLong;
                            resultSymbol.SwapEnabled = reportSymbol.SwapEnabled;
                            resultSymbol.DefaultSlippage = reportSymbol.DefaultSlippage;
                            resultSymbol.IsTradeEnabled = reportSymbol.TradeEnabled;
                            resultSymbol.GroupSortOrder = reportSymbol.SecuritySortOrder;
                            resultSymbol.SortOrder = reportSymbol.SortOrder;
                            resultSymbol.CurrencySortOrder = reportSymbol.MarginCurrencySortOrder;
                            resultSymbol.SettlementCurrencySortOrder = reportSymbol.ProfitCurrencySortOrder;
                            resultSymbol.CurrencyPrecision = reportSymbol.MarginCurrencyPrecision;
                            resultSymbol.SettlementCurrencyPrecision = reportSymbol.ProfitCurrencyPrecision;
                            resultSymbol.StatusGroupId = reportSymbol.StatusGroupId;
                            resultSymbol.SecurityName = reportSymbol.SecurityId;
                            resultSymbol.SecurityDescription = reportSymbol.SecurityDescription;
                            resultSymbol.StopOrderMarginReduction = reportSymbol.StopOrderMarginReduction;
                            resultSymbol.HiddenLimitOrderMarginReduction = reportSymbol.HiddenLimitOrderMarginReduction;
                            resultSymbol.IsCloseOnly = reportSymbol.CloseOnly;
                            resultSymbol.IsLongOnly = reportSymbol.LongOnly;
                            if (reportSymbol.Subscription != null)
                                resultSymbol.Subscription = new Common.SubscriptionInfo()
                                {
                                    Name = reportSymbol.Subscription.Value.Name,
                                    FrequencyFilterMs = reportSymbol.Subscription.Value.FrequencyFilterMs,
                                    TotalDepthLimit = reportSymbol.Subscription.Value.TotalDepthLimit,
                                    Compression = GetQuoteStreamCompressionType(reportSymbol.Subscription.Value.Compression)
                                };
                            resultSymbol.ISIN = reportSymbol.ISIN;
                            resultSymbol.SlippageType = GetSlippageType(reportSymbol.SlippageType);
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
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnSymbolListReject(ClientSession session, SymbolListRequestClientContext SymbolListRequestClientContext, Reject message)
            {
                try
                {
                    SymbolListAsyncContext context = (SymbolListAsyncContext)SymbolListRequestClientContext;

                    TickTrader.FDK.Common.RejectReason rejectReason = GetRejectReason(message.Reason);
                    RejectException exception = new RejectException(rejectReason, message.Text);

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
                        SoftFX.Net.QuoteFeed.TradingSessionStatusInfo reportStatusInfo = message.StatusInfo;

                        resultStatusInfo.Status = GetSessionStatus(reportStatusInfo.Status);
                        resultStatusInfo.StartTime = reportStatusInfo.StartTime;
                        resultStatusInfo.EndTime = reportStatusInfo.EndTime;
                        resultStatusInfo.OpenTime = reportStatusInfo.OpenTime;
                        resultStatusInfo.CloseTime = reportStatusInfo.CloseTime;

                        resultStatusInfo.DisabledFeatures = GetOffTimeDisabledFeatures(reportStatusInfo.DisabledFeatures, reportStatusInfo.Status == TradingSessionStatus.Close);

                        TradingSessionStatusGroupArray reportGroups = reportStatusInfo.Groups;
                        int count = reportGroups.Length;
                        TickTrader.FDK.Common.StatusGroupInfo[] resultGroups = new TickTrader.FDK.Common.StatusGroupInfo[count];

                        for (int index = 0; index < count; ++index)
                        {
                            TradingSessionStatusGroup reportGroup = reportGroups[index];
                            TickTrader.FDK.Common.StatusGroupInfo resultGroup = new TickTrader.FDK.Common.StatusGroupInfo();

                            resultGroup.StatusGroupId = reportGroup.Id;
                            resultGroup.Status = GetSessionStatus(reportGroup.Status);
                            resultGroup.StartTime = reportGroup.StartTime;
                            resultGroup.EndTime = reportGroup.EndTime;
                            resultGroup.OpenTime = reportGroup.OpenTime;
                            resultGroup.CloseTime = reportGroup.CloseTime;

                            resultGroup.DisabledFeatures = GetOffTimeDisabledFeatures(reportGroup.DisabledFeatures, reportGroup.Status == TradingSessionStatus.Close);
                            
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

            public override void OnMarketDataSubscribeReport(ClientSession session, MarketDataSubscribeRequestClientContext MarketDataSubscribeRequestClientContext, MarketDataSubscribeReport message)
            {
                try
                {
                    SubscribeQuotesAsyncContext context = (SubscribeQuotesAsyncContext)MarketDataSubscribeRequestClientContext;

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
                            resultQuote.IndicativeTick = reportSnapshot.IndicativeTick;
                            resultQuote.TickType = GetTickType(reportSnapshot.TickType);

                            MarketDataEntryArray reportSnapshotEntries = reportSnapshot.Entries;
                            int count2 = reportSnapshotEntries.Length;

                            resultQuote.Bids.Clear();
                            resultQuote.Asks.Clear();

                            for (int index2 = 0; index2 < count2; ++index2)
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
                                {
                                    resultQuote.Asks.Add(quoteEntry);
                                }
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

                        if (context.Waitable)
                        {
                            context.quotes_ = resultQuotes;
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
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnMarketDataSubscribeReject(ClientSession session, MarketDataSubscribeRequestClientContext MarketDataSubscribeRequestClientContext, Reject message)
            {
                try
                {
                    SubscribeQuotesAsyncContext context = (SubscribeQuotesAsyncContext)MarketDataSubscribeRequestClientContext;

                    TickTrader.FDK.Common.RejectReason rejectReason = GetRejectReason(message.Reason);
                    RejectException exception = new RejectException(rejectReason, message.Text);

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
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnMarketDataUnsubscribeReport(ClientSession session, MarketDataUnsubscribeRequestClientContext MarketDataUnsubscribeRequestClientContext, MarketDataUnsubscribeReport message)
            {
                try
                {
                    UnsubscribeQuotesAsyncContext context = (UnsubscribeQuotesAsyncContext)MarketDataUnsubscribeRequestClientContext;

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
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnMarketDataUnsubscribeReject(ClientSession session, MarketDataUnsubscribeRequestClientContext MarketDataUnsubscribeRequestClientContext, Reject message)
            {
                try
                {
                    UnsubscribeQuotesAsyncContext context = (UnsubscribeQuotesAsyncContext)MarketDataUnsubscribeRequestClientContext;

                    TickTrader.FDK.Common.RejectReason rejectReason = GetRejectReason(message.Reason);
                    RejectException exception = new RejectException(rejectReason, message.Text);

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
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnMarketDataReport(ClientSession session, MarketDataRequestClientContext MarketDataRequestClientContext, MarketDataReport message)
            {
                try
                {
                    GetQuotesAsyncContext context = (GetQuotesAsyncContext)MarketDataRequestClientContext;

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
                            resultQuote.IndicativeTick = reportSnapshot.IndicativeTick;
                            resultQuote.TickType = GetTickType(reportSnapshot.TickType);

                            MarketDataEntryArray reportSnapshotEntries = reportSnapshot.Entries;
                            int count2 = reportSnapshotEntries.Length;

                            resultQuote.Bids.Clear();
                            resultQuote.Asks.Clear();

                            for (int index2 = 0; index2 < count2; ++index2)
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
                                {
                                    resultQuote.Asks.Add(quoteEntry);
                                }
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
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnMarketDataReject(ClientSession session, MarketDataRequestClientContext MarketDataRequestClientContext, Reject message)
            {
                try
                {
                    GetQuotesAsyncContext context = (GetQuotesAsyncContext)MarketDataRequestClientContext;

                    TickTrader.FDK.Common.RejectReason rejectReason = GetRejectReason(message.Reason);
                    RejectException exception = new RejectException(rejectReason, message.Text);

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

            public override void OnTradingSessionStatusUpdate(ClientSession session, TradingSessionStatusUpdate message)
            {
                try
                {
                    TickTrader.FDK.Common.SessionInfo resultStatusInfo = new TickTrader.FDK.Common.SessionInfo();
                    SoftFX.Net.QuoteFeed.TradingSessionStatusInfo reportStatusInfo = message.StatusInfo;

                    resultStatusInfo.Status = GetSessionStatus(reportStatusInfo.Status);
                    resultStatusInfo.StartTime = reportStatusInfo.StartTime;
                    resultStatusInfo.EndTime = reportStatusInfo.EndTime;
                    resultStatusInfo.OpenTime = reportStatusInfo.OpenTime;
                    resultStatusInfo.CloseTime = reportStatusInfo.CloseTime;

                    resultStatusInfo.DisabledFeatures = GetOffTimeDisabledFeatures(reportStatusInfo.DisabledFeatures, reportStatusInfo.Status == TradingSessionStatus.Close);

                    TradingSessionStatusGroupArray reportGroups = reportStatusInfo.Groups;
                    int count = reportGroups.Length;
                    TickTrader.FDK.Common.StatusGroupInfo[] resultGroups = new TickTrader.FDK.Common.StatusGroupInfo[count];

                    for (int index = 0; index < count; ++index)
                    {
                        TradingSessionStatusGroup reportGroup = reportGroups[index];
                        TickTrader.FDK.Common.StatusGroupInfo resultGroup = new TickTrader.FDK.Common.StatusGroupInfo();

                        resultGroup.StatusGroupId = reportGroup.Id;
                        resultGroup.Status = GetSessionStatus(reportGroup.Status);
                        resultGroup.StartTime = reportGroup.StartTime;
                        resultGroup.EndTime = reportGroup.EndTime;
                        resultGroup.OpenTime = reportGroup.OpenTime;
                        resultGroup.CloseTime = reportGroup.CloseTime;

                        resultGroup.DisabledFeatures = GetOffTimeDisabledFeatures(reportGroup.DisabledFeatures, reportGroup.Status == TradingSessionStatus.Close);
                       
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

            public override void OnMarketDataUpdate(ClientSession session, MarketDataUpdate message)
            {
                try
                {
                    MarketDataSnapshot snapshot = message.Snapshot;

                    quote_.Symbol = snapshot.SymbolId;
                    quote_.Id = snapshot.Id;
                    quote_.CreatingTime = snapshot.OrigTime;
                    quote_.IndicativeTick = snapshot.IndicativeTick;
                    quote_.TickType = GetTickType(snapshot.TickType);

                    MarketDataEntryArray snapshotEntries = snapshot.Entries;
                    int count = snapshotEntries.Length;

                    quote_.Bids.Clear();
                    quote_.Asks.Clear();

                    for (int index = 0; index < count; ++index)
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
                        {
                            quote_.Asks.Add(quoteEntry);
                        }
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
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnMarketDataUpdateCompressedBlock(ClientSession session, MarketDataUpdateCompressedBlock message)
            {
                try
                {
                    //Console.WriteLine("Snappy Block Size: " + message.SnapshotBlock.Block.Length);
                    var snap = message.SnapshotBlock;
                    byte[] block = new byte[snap.Block.Length];
                    for (int i = 0; i < snap.Block.Length; i++)
                        block[i] = snap.Block[i];
                    if (snap.CompressionType == MarketDataCompressionType.Snappy)
                    {
                        compressedStreamHandler_.Write(block);
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnNotification(ClientSession session, SoftFX.Net.QuoteFeed.Notification message)
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

            TickTrader.FDK.Common.RejectReason GetRejectReason(SoftFX.Net.QuoteFeed.RejectReason reason)
            {
                switch (reason)
                {
                    case SoftFX.Net.QuoteFeed.RejectReason.ThrottlingLimits:
                        return Common.RejectReason.ThrottlingLimits;

                    case SoftFX.Net.QuoteFeed.RejectReason.InternalServerError:
                        return Common.RejectReason.InternalServerError;

                    case SoftFX.Net.QuoteFeed.RejectReason.Other:
                    default:
                        return Common.RejectReason.Other;
                }
            }

            TickTrader.FDK.Common.LogoutReason GetLogoutReason(SoftFX.Net.QuoteFeed.LoginRejectReason reason)
            {
                switch (reason)
                {
                    case SoftFX.Net.QuoteFeed.LoginRejectReason.IncorrectCredentials:
                        return TickTrader.FDK.Common.LogoutReason.InvalidCredentials;

                    case SoftFX.Net.QuoteFeed.LoginRejectReason.ThrottlingLimits:
                        return TickTrader.FDK.Common.LogoutReason.Unknown;

                    case SoftFX.Net.QuoteFeed.LoginRejectReason.BlockedLogin:
                        return TickTrader.FDK.Common.LogoutReason.BlockedAccount;

                    case SoftFX.Net.QuoteFeed.LoginRejectReason.InternalServerError:
                        return TickTrader.FDK.Common.LogoutReason.ServerError;

                    case SoftFX.Net.QuoteFeed.LoginRejectReason.MustChangePassword:
                        return TickTrader.FDK.Common.LogoutReason.MustChangePassword;

                    case SoftFX.Net.QuoteFeed.LoginRejectReason.Other:
                    default:
                        return TickTrader.FDK.Common.LogoutReason.Unknown;
                }
            }

            TickTrader.FDK.Common.LogoutReason GetLogoutReason(SoftFX.Net.QuoteFeed.LogoutReason reason)
            {
                switch (reason)
                {
                    case SoftFX.Net.QuoteFeed.LogoutReason.ClientLogout:
                        return TickTrader.FDK.Common.LogoutReason.ClientInitiated;

                    case SoftFX.Net.QuoteFeed.LogoutReason.ServerLogout:
                        return TickTrader.FDK.Common.LogoutReason.ServerLogout;

                    case SoftFX.Net.QuoteFeed.LogoutReason.DeletedLogin:
                        return TickTrader.FDK.Common.LogoutReason.LoginDeleted;

                    case SoftFX.Net.QuoteFeed.LogoutReason.InternalServerError:
                        return TickTrader.FDK.Common.LogoutReason.ServerError;

                    case SoftFX.Net.QuoteFeed.LogoutReason.BlockedLogin:
                        return TickTrader.FDK.Common.LogoutReason.BlockedAccount;

                    case SoftFX.Net.QuoteFeed.LogoutReason.MustChangePassword:
                        return TickTrader.FDK.Common.LogoutReason.MustChangePassword;

                    case SoftFX.Net.QuoteFeed.LogoutReason.Other:
                    default:
                        return TickTrader.FDK.Common.LogoutReason.Unknown;
                }
            }

            TickTrader.FDK.Common.MarginCalcMode GetMarginCalcMode(SoftFX.Net.QuoteFeed.MarginCalcMode mode)
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

            TickTrader.FDK.Common.ProfitCalcMode GetProfitCalcMode(SoftFX.Net.QuoteFeed.ProfitCalcMode mode)
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

            TickTrader.FDK.Common.CommissionType GetCommissionType(SoftFX.Net.QuoteFeed.CommissionType type)
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

            TickTrader.FDK.Common.CommissionChargeType GetCommissionChargeType(SoftFX.Net.QuoteFeed.CommissionChargeType type)
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

            TickTrader.FDK.Common.CommissionChargeMethod GetCommissionChargeMethod(SoftFX.Net.QuoteFeed.CommissionChargeMethod method)
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

            TickTrader.FDK.Common.SwapType GetSwapType(SoftFX.Net.QuoteFeed.SwapType swapType)
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

            TickTrader.FDK.Common.SessionStatus GetSessionStatus(SoftFX.Net.QuoteFeed.TradingSessionStatus status)
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

            TickTrader.FDK.Common.NotificationType GetNotificationType(SoftFX.Net.QuoteFeed.NotificationType type)
            {
                switch (type)
                {
                    case SoftFX.Net.QuoteFeed.NotificationType.ConfigUpdate:
                        return TickTrader.FDK.Common.NotificationType.ConfigUpdated;

                    default:
                        return TickTrader.FDK.Common.NotificationType.Unknown;
                }
            }

            TickTrader.FDK.Common.NotificationSeverity GetNotificationSeverity(SoftFX.Net.QuoteFeed.NotificationSeverity severity)
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
                        return TickTrader.FDK.Common.NotificationSeverity.Unknown;
                }
            }

            TickTrader.FDK.Common.CurrencyType GetCurrencyType(SoftFX.Net.QuoteFeed.CurrencyType type)
            {
                switch (type)
                {
                    case SoftFX.Net.QuoteFeed.CurrencyType.Default:
                        return TickTrader.FDK.Common.CurrencyType.Default;

                    case SoftFX.Net.QuoteFeed.CurrencyType.Fiat:
                        return TickTrader.FDK.Common.CurrencyType.Fiat;

                    case SoftFX.Net.QuoteFeed.CurrencyType.Crypto:
                        return TickTrader.FDK.Common.CurrencyType.Crypto;

                    case SoftFX.Net.QuoteFeed.CurrencyType.Index:
                        return TickTrader.FDK.Common.CurrencyType.Index;

                    case SoftFX.Net.QuoteFeed.CurrencyType.Share:
                        return TickTrader.FDK.Common.CurrencyType.Share;

                    case SoftFX.Net.QuoteFeed.CurrencyType.Commodity:
                        return TickTrader.FDK.Common.CurrencyType.Commodity;

                    case SoftFX.Net.QuoteFeed.CurrencyType.Bond:
                        return TickTrader.FDK.Common.CurrencyType.Bond;

                    case SoftFX.Net.QuoteFeed.CurrencyType.ETF:
                        return TickTrader.FDK.Common.CurrencyType.ETF;

                    case SoftFX.Net.QuoteFeed.CurrencyType.MF:
                        return TickTrader.FDK.Common.CurrencyType.MF;

                    case SoftFX.Net.QuoteFeed.CurrencyType.Future:
                        return TickTrader.FDK.Common.CurrencyType.Future;

                    case SoftFX.Net.QuoteFeed.CurrencyType.Option:
                        return TickTrader.FDK.Common.CurrencyType.Option;

                    case SoftFX.Net.QuoteFeed.CurrencyType.CFD:
                        return TickTrader.FDK.Common.CurrencyType.CFD;

                    default:
                        return TickTrader.FDK.Common.CurrencyType.Default;
                }
            }


            TickTrader.FDK.Common.SubscriptionInfo.QuoteStreamCompressionTypes GetQuoteStreamCompressionType(SoftFX.Net.QuoteFeed.MarketDataCompressionType type)
            {
                switch (type)
                {
                    case MarketDataCompressionType.WithoutCompression:
                        return TickTrader.FDK.Common.SubscriptionInfo.QuoteStreamCompressionTypes.WithoutCompression;

                    case MarketDataCompressionType.Snappy:
                        return TickTrader.FDK.Common.SubscriptionInfo.QuoteStreamCompressionTypes.Snappy;

                    default:
                        return TickTrader.FDK.Common.SubscriptionInfo.QuoteStreamCompressionTypes.Unknown;
                }
            }

            TickTrader.FDK.Common.OffTimeDisabledFeatures GetOffTimeDisabledFeatures(SoftFX.Net.QuoteFeed.OffTimeDisabledFeatures features, bool isClosed)
            {
                if (!client_.ProtocolSpec.SupportsOffTimeDisabledFeatures)
                {
                    if (isClosed)
                        return Common.OffTimeDisabledFeatures.QuoteHistory | Common.OffTimeDisabledFeatures.Trade | Common.OffTimeDisabledFeatures.Feed;
                    else return Common.OffTimeDisabledFeatures.None;
                }
                Common.OffTimeDisabledFeatures result = Common.OffTimeDisabledFeatures.None;
                if (features.HasFlag(SoftFX.Net.QuoteFeed.OffTimeDisabledFeatures.QuoteHistory))
                    result |= Common.OffTimeDisabledFeatures.QuoteHistory;
                if (features.HasFlag(SoftFX.Net.QuoteFeed.OffTimeDisabledFeatures.Trade))
                    result |= Common.OffTimeDisabledFeatures.Trade;
                if (features.HasFlag(SoftFX.Net.QuoteFeed.OffTimeDisabledFeatures.Feed))
                    result |= Common.OffTimeDisabledFeatures.Feed;

                return result;
            }

            TickTrader.FDK.Common.TickTypes GetTickType(SoftFX.Net.QuoteFeed.TickType type)
            {
                switch (type)
                {
                    case TickType.IndicativeBid:
                        return TickTypes.IndicativeBid;
                    case TickType.IndicativeAsk:
                        return TickTypes.IndicativeAsk;
                    case TickType.IndicativeBidAsk:
                        return TickTypes.IndicativeBidAsk;

                    case TickType.Normal:
                    default:
                        return TickTypes.Normal;
                }
            }

            TickTrader.FDK.Common.SlippageType GetSlippageType(SoftFX.Net.QuoteFeed.SlippageType type)
            {
                switch (type)
                {
                    case SoftFX.Net.QuoteFeed.SlippageType.Percent:
                        return Common.SlippageType.Percent;
                    case SoftFX.Net.QuoteFeed.SlippageType.Pips:
                    default:
                        return Common.SlippageType.Pips;
                }
            }

            QuoteFeed client_;
            Quote quote_;
            CompressedStreamHandler compressedStreamHandler_;
        }

        #endregion
    }
}
