namespace TickTrader.FDK.Extended
{
    using System;
    using System.Threading;
    using Common;
    using QuoteFeed;
    using QuoteStore;

    /// <summary>
    /// This class connects to trading platform and receives quotes and other notifications.
    /// </summary>
    public class DataFeed : IDisposable
    {
        #region Construction

        /// <summary>
        /// Creates a new data feed instance. You should use Initialize method to finish the instance initialization.
        /// </summary>
        public DataFeed() : this(null, "DataFeed")
        {
        }

        /// <summary>
        /// Creates and initializes a new data feed instance.
        /// </summary>
        /// <exception cref="System.ArgumentNullException">If connectionString is null.</exception>
        public DataFeed(string connectionString) : this(connectionString, "DataFeed")
        {
        }

        /// <summary>
        /// Creates and initializes a new data feed instance.
        /// </summary>
        /// <exception cref="System.ArgumentNullException">If connectionString is null.</exception>
        public DataFeed(string connectionString, string name)
        {
            name_ = name;
            server_ = new DataFeedServer(this);
            cache_ = new DataFeedCache(this);
            network_ = new Network();

            if (!string.IsNullOrEmpty(connectionString))
                Initialize(connectionString);
        }

        /// <summary>
        /// Initializes the data feed instance; it must be stopped.
        /// </summary>
        /// <param name="connectionString">Can not be null.</param>
        /// <exception cref="System.ArgumentNullException">If connectionString is null.</exception>
        /// <exception cref="System.InvalidOperationException">If the instance is not stopped.</exception>
        public void Initialize(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException(nameof(connectionString), "Connection string can not be null or empty.");

            ConnectionStringParser connectionStringParser = new ConnectionStringParser();

            connectionStringParser.Parse(connectionString);

            if (! connectionStringParser.TryGetStringValue("Address", out address_))
                throw new Exception("Address is not specified");

            int quoteFeedPort;
            if (! connectionStringParser.TryGetIntValue("QuoteFeedPort", out quoteFeedPort))
                quoteFeedPort = 5030;

            int quoteStorePort;
            if (! connectionStringParser.TryGetIntValue("QuoteStorePort", out quoteStorePort))
                quoteStorePort = 5050;

            if (! connectionStringParser.TryGetStringValue("Username", out login_))
                throw new Exception("Username is not specified");

            if (! connectionStringParser.TryGetStringValue("Password", out password_))
                throw new Exception("Password is not specified");

            if (! connectionStringParser.TryGetStringValue("DeviceId", out deviceId_))
                throw new Exception("DeviceId is not specified");

            if (! connectionStringParser.TryGetStringValue("AppId", out appId_))
                throw new Exception("AppId is not specified");

            if (! connectionStringParser.TryGetStringValue("AppSessionId", out appSessionId_))
                throw new Exception("AppSessionId is not specified");

            int eventQueueSize;
            if (! connectionStringParser.TryGetIntValue("EventQueueSize", out eventQueueSize))
                eventQueueSize = 1000;

            if (! connectionStringParser.TryGetIntValue("OperationTimeout", out synchOperationTimeout_))
                synchOperationTimeout_ = 30000;

            string logDirectory;
            if (! connectionStringParser.TryGetStringValue("LogDirectory", out logDirectory))
                logDirectory = "Logs";

            bool logMessages;
            if (! connectionStringParser.TryGetBoolValue("LogMessages", out logMessages))
                logMessages = false;

            synchronizer_ = new object();

            quoteFeedClient_ = new QuoteFeed.Client(name_ + ".QuoteFeed", logMessages, quoteFeedPort, 1, -1, 10000, 10000, logDirectory);
            quoteFeedClient_.ConnectResultEvent += new QuoteFeed.Client.ConnectResultDelegate(this.OnConnectResult);
            quoteFeedClient_.ConnectErrorEvent += new QuoteFeed.Client.ConnectErrorDelegate(this.OnConnectError);
            quoteFeedClient_.DisconnectResultEvent += new QuoteFeed.Client.DisconnectResultDelegate(this.OnDisconnectResult);
            quoteFeedClient_.DisconnectEvent += new QuoteFeed.Client.DisconnectDelegate(this.OnDisconnect);
            quoteFeedClient_.ReconnectEvent += new QuoteFeed.Client.ReconnectDelegate(this.OnReconnect);
            quoteFeedClient_.ReconnectErrorEvent += new QuoteFeed.Client.ReconnectErrorDelegate(this.OnReconnectError);
            quoteFeedClient_.TwoFactorLoginRequestEvent += new QuoteFeed.Client.TwoFactorLoginRequestDelegate(this.OnTwoFactorLoginRequest);
            quoteFeedClient_.TwoFactorLoginResultEvent += new QuoteFeed.Client.TwoFactorLoginResultDelegate(this.OnTwoFactorLoginResult);
            quoteFeedClient_.TwoFactorLoginErrorEvent += new QuoteFeed.Client.TwoFactorLoginErrorDelegate(this.OnTwoFactorLoginError);
            quoteFeedClient_.TwoFactorLoginResumeEvent += new QuoteFeed.Client.TwoFactorLoginResumeDelegate(this.OnTwoFactorLoginResume);
            quoteFeedClient_.LoginResultEvent += new QuoteFeed.Client.LoginResultDelegate(this.OnLoginResult);
            quoteFeedClient_.LoginErrorEvent += new QuoteFeed.Client.LoginErrorDelegate(this.OnLoginError);
            quoteFeedClient_.LogoutResultEvent += new QuoteFeed.Client.LogoutResultDelegate(this.OnLogoutResult);
            quoteFeedClient_.LogoutEvent += new QuoteFeed.Client.LogoutDelegate(this.OnLogout);
            quoteFeedClient_.SessionInfoResultEvent += new QuoteFeed.Client.SessionInfoResultDelegate(this.OnSessionInfoResult);
            quoteFeedClient_.SessionInfoErrorEvent += new QuoteFeed.Client.SessionInfoErrorDelegate(this.OnSessionInfoError);
            quoteFeedClient_.CurrencyListResultEvent += new QuoteFeed.Client.CurrencyListResultDelegate(this.OnCurrencyListResult);
            quoteFeedClient_.CurrencyListErrorEvent += new QuoteFeed.Client.CurrencyListErrorDelegate(this.OnCurrencyListError);
            quoteFeedClient_.SymbolListResultEvent += new QuoteFeed.Client.SymbolListResultDelegate(this.OnSymbolListResult);
            quoteFeedClient_.SymbolListErrorEvent += new QuoteFeed.Client.SymbolListErrorDelegate(this.OnSymbolListError);
            quoteFeedClient_.SessionInfoUpdateEvent += new QuoteFeed.Client.SessionInfoUpdateDelegate(this.OnSessionInfoUpdate);
            quoteFeedClient_.SubscribeQuotesResultEvent += new QuoteFeed.Client.SubscribeQuotesResultDelegate(this.OnSubscribeQuotesResult);
            quoteFeedClient_.UnsubscribeQuotesResultEvent += new QuoteFeed.Client.UnsubscribeQuotesResultDelegate(this.OnUnsubscribeQuotesResult);
            quoteFeedClient_.QuoteUpdateEvent += new QuoteFeed.Client.QuoteUpdateDelegate(this.OnQuoteUpdate);
            quoteFeedClient_.NotificationEvent += new QuoteFeed.Client.NotificationDelegate(this.OnNotification);

            quoteStoreClient_ = new QuoteStore.Client(name_ + ".QuoteStore", logMessages, quoteStorePort, 1, -1, 10000, 10000, logDirectory);
            quoteStoreClient_.ConnectResultEvent += new QuoteStore.Client.ConnectResultDelegate(this.OnConnectResult);
            quoteStoreClient_.ConnectErrorEvent += new QuoteStore.Client.ConnectErrorDelegate(this.OnConnectError);
            quoteStoreClient_.DisconnectResultEvent += new QuoteStore.Client.DisconnectResultDelegate(this.OnDisconnectResult);
            quoteStoreClient_.DisconnectEvent += new QuoteStore.Client.DisconnectDelegate(this.OnDisconnect);
            quoteStoreClient_.ReconnectEvent += new QuoteStore.Client.ReconnectDelegate(this.OnReconnect);
            quoteStoreClient_.ReconnectErrorEvent += new QuoteStore.Client.ReconnectErrorDelegate(this.OnReconnectError);
            quoteStoreClient_.LoginResultEvent += new QuoteStore.Client.LoginResultDelegate(this.OnLoginResult);
            quoteStoreClient_.LoginErrorEvent += new QuoteStore.Client.LoginErrorDelegate(this.OnLoginError);
            quoteStoreClient_.LogoutResultEvent += new QuoteStore.Client.LogoutResultDelegate(this.OnLogoutResult);
            quoteStoreClient_.LogoutEvent += new QuoteStore.Client.LogoutDelegate(this.OnLogout);
            quoteStoreClient_.NotificationEvent += new QuoteStore.Client.NotificationDelegate(this.OnNotification);

            loginEvent_ = new ManualResetEvent(false);

            eventQueue_ = new EventQueue(eventQueueSize);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Name
        /// </summary>
        public string Name
        {
            get { return name_;  }
        }

        /// <summary>
        /// Gets object, which encapsulates server side methods.
        /// </summary>
        public DataFeedServer Server
        {
            get { return server_; }
        }

        /// <summary>
        /// Gets object, which encapsulates client cache methods.
        /// </summary>
        public DataFeedCache Cache
        {
            get { return cache_; }
        }

        /// <summary>
        /// Returns a network information of corresponded client connection; can not be null.
        /// </summary>
        public Network Network
        {
            get { return network_; }
        }

        /// <summary>
        /// Returns true, the data trade/feed object is started, otherwise false.
        /// </summary>
        public bool IsStarted
        {
            get
            {
                lock (synchronizer_)
                {
                    return started_;
                }
            }
        }

        /// <summary>
        /// Returns true, the data trade/feed object is stopped, otherwise false.
        /// </summary>
        public bool IsStopped
        {
            get
            {
                lock (synchronizer_)
                {
                    return ! started_;
                }
            }
        }

        #endregion
        
        #region Events

        /// <summary>
        /// Occurs when data feed is logon.
        /// </summary>
        public event LogonHandler Logon;

        /// <summary>
        /// Occurs when data feed is logout.
        /// </summary>
        public event LogoutHandler Logout;

        /// <summary>
        /// Occurs when data feed is required two factor auth.
        /// </summary>
        public event TwoFactorAuthHandler TwoFactorAuth;

        /// <summary>
        /// Occurs when currencies information received or changed.
        /// </summary>
        public event CurrencyInfoHandler CurrencyInfo;

        /// <summary>
        /// Occurs when symbols information received or changed.
        /// </summary>
        public event SymbolInfoHandler SymbolInfo;

        /// <summary>
        /// Occurs when session info received or changed.
        /// </summary>
        public event SessionInfoHandler SessionInfo;

        /// <summary>
        /// Occurs when subscribed to a new symbol.
        /// </summary>
        public event SubscribedHandler Subscribed;

        /// <summary>
        /// Occurs when unsubscribed from the symbol.
        /// </summary>
        public event UnsubscribedHandler Unsubscribed;

        /// <summary>
        /// Occurs when a new quote is received.
        /// </summary>
        public event TickHandler Tick;

        /// <summary>
        /// Occurs when a notification is received.
        /// </summary>
        public event NotifyHandler Notify;

        #endregion

        #region Methods

        /// <summary>
        /// Starts data feed instance asynchronously.
        /// </summary>
        public void Start()
        {            
            QuoteFeed.Client quoteFeedClient = null;
            QuoteStore.Client quoteStoreClient = null;
            Thread eventThread = null;

            try
            {
                lock (synchronizer_)
                {
                    if (started_)
                        throw new Exception(string.Format("Data feed is already started : {0}", name_));

                    loginEvent_.Reset();
                    loginException_ = null;
                    initFlags_ = InitFlags.None;
                    logout_ = false;

                    eventQueue_.Open();

                    eventThread_ = new Thread(this.EventThread);
                    eventThread_.Name = name_ + " Event Thread";
                    eventThread_.Start();

                    try
                    {
                        quoteFeedClient_.ConnectAsync(this, address_);

                        try
                        {
                            quoteStoreClient_.ConnectAsync(this, address_);

                            started_ = true;
                        }
                        catch
                        {
                            quoteFeedClient_.DisconnectAsync(this, "Client disconnect");
                            quoteFeedClient = quoteFeedClient_;

                            throw;
                        }
                    }
                    catch
                    {
                        eventThread = eventThread_;
                        eventThread_ = null;

                        eventQueue_.Close();

                        throw;
                    }
                }
            }
            catch
            {
                // have to wait here since we don't have Join() and Stop() is synchronous

                if (eventThread != null)
                    eventThread.Join();

                if (quoteStoreClient != null)
                    quoteStoreClient.Join();

                if (quoteFeedClient != null)
                    quoteFeedClient.Join();

                throw;
            }
        }

        /// <summary>
        /// Stops data feed instance synchrnously. The method can not be called into any feed/trade event handler.
        /// </summary>
        public void Stop()
        {
            lock (synchronizer_)
            {
                if (started_)
                {                    
                    started_ = false;

                    try
                    {
                        quoteStoreClient_.LogoutAsync(this, "Client logout");
                    }
                    catch
                    {                        
                        quoteStoreClient_.DisconnectAsync(this, "Client disconnect");
                    }

                    try
                    {
                        quoteFeedClient_.LogoutAsync(this, "Client logout");
                    }
                    catch
                    {                        
                        quoteFeedClient_.DisconnectAsync(this, "Client disconnect");
                    }
                }
            }

            quoteStoreClient_.Join();
            quoteFeedClient_.Join();

            Thread eventThread = null;

            lock (synchronizer_)
            {
                if (eventThread_ != null)
                {
                    eventThread = eventThread_;
                    eventThread_ = null;

                    eventQueue_.Close();
                }
            }           

            if (eventThread != null)
                eventThread.Join();
        }

        /// <summary>
        /// Blocks the calling thread until logon succeeds or fails.
        /// </summary>
        /// <returns></returns>
        public bool WaitForLogon()
        {
            return WaitForLogonEx(synchOperationTimeout_);
        }

        /// <summary>
        /// Blocks the calling thread until logon succeeds or fails.
        /// </summary>
        /// <param name="timeoutInMilliseconds"></param>
        /// <returns></returns>
        public bool WaitForLogonEx(int timeoutInMilliseconds)
        {
            if (! loginEvent_.WaitOne(timeoutInMilliseconds))
                return false;

            if (loginException_ != null)
                throw loginException_;

            return true;
        }

        /// <summary>
        /// The method generates a new unique string ID.
        /// </summary>
        /// <returns>Can not be null.</returns>
        public string GenerateOperationId()
        {
            return Guid.NewGuid().ToString();
        }

        #endregion

        #region Disposing

        /// <summary>
        /// Releases all unmanaged resources.
        /// </summary>
        public virtual void Dispose()
        {
            Stop();

            eventQueue_.Dispose();
            loginEvent_.Dispose();
            quoteStoreClient_.Dispose();
            quoteFeedClient_.Dispose();

            GC.SuppressFinalize(this);
        }

        #endregion

        #region Obsoletes

        /// <summary>
        /// Gets or sets default synchronous operation timeout in milliseconds.
        /// </summary>
        [Obsolete("Please use OperationTimeout connection string parameter")]
        public int SynchOperationTimeout
        {
            set { synchOperationTimeout_ = value;  }

            get { return synchOperationTimeout_; }
        }

        /// <summary>
        /// Occurs when local cache initialized.
        /// </summary>
        [Obsolete("Please use Logon event")]
        public event CacheHandler CacheInitialized;

        /// <summary>
        /// Starts data feed/trade instance and waits for logon event.
        /// </summary>
        /// <param name="timeoutInMilliseconds">Timeout of logon waiting.</param>
        /// <returns>true, if logon event is occurred, otherwise false</returns>
        [Obsolete("Please use WaitForLogonEx()")]
        public bool Start(int timeoutInMilliseconds)
        {
            Start();

            return WaitForLogonEx(timeoutInMilliseconds);
        }

        /// <summary>
        /// Blocks the calling thread until logon succeeds or fails.
        /// </summary>
        /// <param name="timeoutInMilliseconds"></param>
        /// <returns></returns>
        [Obsolete("Please use WaitForLogonEx()")]
        public bool WaitForLogon(int timeoutInMilliseconds)
        {
            return WaitForLogonEx(timeoutInMilliseconds);
        }

        #endregion

        #region Private

        void OnConnectResult(QuoteFeed.Client client, object data)
        {
            try
            {
                quoteFeedClient_.LoginAsync(null, login_, password_, deviceId_, appId_, appSessionId_);
            }
            catch
            {
                quoteStoreClient_.DisconnectAsync(this, "Client disconnect");
                quoteFeedClient_.DisconnectAsync(this, "Client disconnect");                
            }
        }

        void OnConnectError(QuoteFeed.Client client, object data, string text)
        {
            try
            {
                lock (synchronizer_)
                {
                    quoteStoreClient_.DisconnectAsync(this, "Client disconnect");

                    if (! logout_)
                    {
                        logout_ = true;

                        LogoutEventArgs args = new LogoutEventArgs();
                        args.Reason = LogoutReason.NetworkError;
                        args.Text = text;
                        eventQueue_.PushEvent(args);

                        loginException_ = new LogoutException(text);
                        loginEvent_.Set();
                    }
                }
            }
            catch
            {
            }
        }

        void OnDisconnectResult(QuoteFeed.Client client, object data, string text)
        {
            try
            {
                lock (synchronizer_)
                {
                    initFlags_ &= ~(InitFlags.Currencies | InitFlags.Symbols | InitFlags.SessionInfo);

                    if (! logout_)
                    {
                        logout_ = true;

                        LogoutEventArgs args = new LogoutEventArgs();
                        args.Reason = LogoutReason.ClientInitiated;
                        args.Text = text;
                        eventQueue_.PushEvent(args);

                        loginException_ = new LogoutException(text);
                        loginEvent_.Set();
                    }
                }
            }
            catch
            {
            }
        }

        void OnDisconnect(QuoteFeed.Client client, string text)
        {
            try
            {
                lock (synchronizer_)
                {
                    initFlags_ &= ~(InitFlags.Currencies | InitFlags.Symbols | InitFlags.SessionInfo);

                    if (! logout_)
                    {                       
                        logout_ = true;

                        LogoutEventArgs args = new LogoutEventArgs();
                        args.Reason = LogoutReason.NetworkError;
                        args.Text = text;
                        eventQueue_.PushEvent(args);

                        loginException_ = new LogoutException(text);
                        loginEvent_.Set();
                    }
                }
            }
            catch
            {
            }
        }

        void OnReconnect(QuoteFeed.Client client)
        {
            try
            {
                quoteFeedClient_.LoginAsync(null, login_, password_, deviceId_, appId_, appSessionId_);
            }
            catch
            {
                quoteStoreClient_.DisconnectAsync(this, "Client disconnect");
                quoteFeedClient_.DisconnectAsync(this, "Client disconnect");                
            }
        }

        void OnReconnectError(QuoteFeed.Client client, string text)
        {
            try
            {
                quoteStoreClient_.DisconnectAsync(this, "Client disconnect");
            }
            catch
            {
            }
        }

        void OnTwoFactorLoginRequest(QuoteFeed.Client client, string text)
        {
            try
            {
                TwoFactorAuthEventArgs args = new TwoFactorAuthEventArgs();                    
                TwoFactorAuth twoFactorAuth = new TwoFactorAuth();
                twoFactorAuth.Reason = TwoFactorReason.ServerRequest;
                args.TwoFactorAuth = twoFactorAuth;
                eventQueue_.PushEvent(args);
            }
            catch
            {
            }
        }

        void OnTwoFactorLoginResult(QuoteFeed.Client client, object data, DateTime expireTime)
        {
            try
            {
                TwoFactorAuthEventArgs args = new TwoFactorAuthEventArgs();                    
                TwoFactorAuth twoFactorAuth = new TwoFactorAuth();
                twoFactorAuth.Reason = TwoFactorReason.ServerSuccess;
                twoFactorAuth.Expire = expireTime;
                args.TwoFactorAuth = twoFactorAuth;
                eventQueue_.PushEvent(args);
            }
            catch
            {
            }
        }

        void OnTwoFactorLoginError(QuoteFeed.Client client, object data, string text)
        {
            try
            {
                TwoFactorAuthEventArgs args = new TwoFactorAuthEventArgs();                    
                TwoFactorAuth twoFactorAuth = new TwoFactorAuth();
                twoFactorAuth.Reason = TwoFactorReason.ServerError;
                twoFactorAuth.Text = text;
                args.TwoFactorAuth = twoFactorAuth;
                eventQueue_.PushEvent(args);
            }
            catch
            {
            }
        }

        void OnTwoFactorLoginResume(QuoteFeed.Client client, object data, DateTime expireTime)
        {
            try
            {
                TwoFactorAuthEventArgs args = new TwoFactorAuthEventArgs();
                TwoFactorAuth twoFactorAuth = new TwoFactorAuth();
                twoFactorAuth.Reason = TwoFactorReason.ServerSuccess;
                twoFactorAuth.Expire = expireTime;
                args.TwoFactorAuth = twoFactorAuth;
                eventQueue_.PushEvent(args);
            }
            catch
            {
            }
        }

        void OnLoginResult(QuoteFeed.Client client, object data)
        {
            try
            {                
                quoteFeedClient_.GetCurrencyListAsync(this);
                quoteFeedClient_.GetSymbolListAsync(this);
                quoteFeedClient_.GetSessionInfoAsync(this);
            }
            catch
            {
                quoteStoreClient_.DisconnectAsync(this, "Client disconnect");
                quoteFeedClient_.DisconnectAsync(this, "Client disconnect");                
            }
        }

        void OnLoginError(QuoteFeed.Client client, object data, string text)
        {
            try
            {
                lock (synchronizer_)
                {
                    quoteStoreClient_.DisconnectAsync(this, "Client disconnect");

                    if (! logout_)
                    {
                        logout_ = true;

                        LogoutEventArgs args = new LogoutEventArgs();
                        args.Reason = LogoutReason.InvalidCredentials;
                        args.Text = text;
                        eventQueue_.PushEvent(args);

                        loginException_ = new LogoutException(text);
                        loginEvent_.Set();
                    }
                }
            }
            catch
            {
            }
        }

        void OnCurrencyListResult(QuoteFeed.Client client, object data, CurrencyInfo[] currencies)
        {
            try
            {
                if (data == this)
                {
                    lock (synchronizer_)
                    {
                        lock (cache_.mutex_)
                        {
                            cache_.currencies_ = currencies;
                        }

                        if (initFlags_ != InitFlags.All)
                        {
                            initFlags_ |= InitFlags.Currencies;

                            if (initFlags_ == InitFlags.All)
                            {
                                logout_ = false;
                                PushLoginEvents();

                                loginException_ = null;
                                loginEvent_.Set();
                            }
                        }
                        else if (reloadFlags_ != ReloadFlags.All)
                        {
                            reloadFlags_ |= ReloadFlags.Currencies;

                            if (reloadFlags_ == ReloadFlags.All)
                            {
                                PushConfigUpdateEvents();
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }

        void OnCurrencyListError(QuoteFeed.Client client, object data, string message)
        {
            try
            {
                if (data == this)
                {
                    quoteStoreClient_.DisconnectAsync(this, "Client disconnect");
                    quoteFeedClient_.DisconnectAsync(this, "Client disconnect");                    
                }
            }
            catch
            {
            }
        }

        void OnSymbolListResult(QuoteFeed.Client client, object data, SymbolInfo[] symbols)
        {
            try
            {
                if (data == this)
                {
                    lock (synchronizer_)
                    {
                        lock (cache_.mutex_)
                        {
                            cache_.symbols_ = symbols;
                        }

                        if (initFlags_ != InitFlags.All)
                        {
                            initFlags_ |= InitFlags.Symbols;

                            if (initFlags_ == InitFlags.All)
                            {
                                logout_ = false;
                                PushLoginEvents();

                                loginException_ = null;
                                loginEvent_.Set();
                            }
                        }
                        else if (reloadFlags_ != ReloadFlags.All)
                        {
                            reloadFlags_ |= ReloadFlags.Symbols;

                            if (reloadFlags_ == ReloadFlags.All)
                            {
                                PushConfigUpdateEvents();
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }

        void OnSymbolListError(QuoteFeed.Client client, object data, string message)
        {
            try
            {
                if (data == this)
                {
                    quoteStoreClient_.DisconnectAsync(this, "Client disconnect");
                    quoteFeedClient_.DisconnectAsync(this, "Client disconnect");                    
                }
            }
            catch
            {
            }
        }

        void OnSessionInfoResult(QuoteFeed.Client client, object data, SessionInfo sessionInfo)
        {
            try
            {
                if (data == this)
                {
                    lock (synchronizer_)
                    {
                        lock (cache_.mutex_)
                        {
                            cache_.sessionInfo_ = sessionInfo;
                        }

                        if (initFlags_ != InitFlags.All)
                        {
                            initFlags_ |= InitFlags.SessionInfo;

                            if (initFlags_ == InitFlags.All)
                            {
                                logout_ = false;
                                PushLoginEvents();

                                loginException_ = null;
                                loginEvent_.Set();
                            }
                        }
                        else if (reloadFlags_ != ReloadFlags.All)
                        {
                            reloadFlags_ |= ReloadFlags.SessionInfo;

                            if (reloadFlags_ == ReloadFlags.All)
                            {
                                PushConfigUpdateEvents();
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }

        void OnSessionInfoError(QuoteFeed.Client client, object data, string message)
        {
            try
            {
                if (data == this)
                {
                    quoteStoreClient_.DisconnectAsync(this, "Client disconnect");
                    quoteFeedClient_.DisconnectAsync(this, "Client disconnect");                    
                }
            }
            catch
            {
            }
        }

        void OnSubscribeQuotesResult(QuoteFeed.Client client, object data, Quote[] quotes)
        {
            try
            {
                lock (cache_.mutex_)
                {
                    for (int index = 0; index < quotes.Length; ++index)
                    {
                        Quote quote = quotes[index];
                        cache_.quotes_[quote.Symbol] = quote;
                    }
                }

                for (int index = 0; index < quotes.Length; ++index)
                {
                    Quote quote = quotes[index];

                    SubscribedEventArgs subscribedArgs = new SubscribedEventArgs();
                    subscribedArgs.Tick = quote;
                    eventQueue_.PushEvent(subscribedArgs);

                    // For backward compatibility
                    TickEventArgs tickArgs = new TickEventArgs();
                    tickArgs.Tick = quote;
                    eventQueue_.PushEvent(tickArgs);
                }
            }
            catch
            {
            }
        }

        void OnUnsubscribeQuotesResult(QuoteFeed.Client client, object data, string[] symbolIds)
        {
            try
            {
                for (int index = 0; index < symbolIds.Length; ++index)
                {
                    UnsubscribedEventArgs args = new UnsubscribedEventArgs();
                    args.Symbol = symbolIds[index];
                    eventQueue_.PushEvent(args);
                }
            }
            catch
            {
            }
        }

        void OnLogoutResult(QuoteFeed.Client client, object data, LogoutInfo logoutInfo)
        {
            try
            {
                lock (synchronizer_)
                {
                    quoteFeedClient_.DisconnectAsync(this, "Client disconnect");

                    if (! logout_)
                    {
                        logout_ = true;

                        LogoutEventArgs args = new LogoutEventArgs();
                        args.Reason = logoutInfo.Reason;
                        args.Text = logoutInfo.Message;
                        eventQueue_.PushEvent(args);
                    }
                }
            }
            catch
            {
            }
        }

        void OnLogout(QuoteFeed.Client client, LogoutInfo logoutInfo)
        {
            try
            {
                lock (synchronizer_)
                {
                    quoteFeedClient_.DisconnectAsync(this, "Client disconnect");

                    if (! logout_)
                    {
                        logout_ = true;

                        LogoutEventArgs args = new LogoutEventArgs();
                        args.Reason = logoutInfo.Reason;
                        args.Text = logoutInfo.Message;
                        eventQueue_.PushEvent(args);
                    }
                }
            }
            catch
            {
            }
        }

        void OnSessionInfoUpdate(QuoteFeed.Client client, SessionInfo info)
        {
            try
            {
                SessionInfoEventArgs args = new SessionInfoEventArgs();
                args.Information = info;
                eventQueue_.PushEvent(args);
            }
            catch
            {
            }
        }

        void OnQuoteUpdate(QuoteFeed.Client client, Quote quote)
        {
            try
            {
                Quote newQuote = quote.Clone();

                lock (cache_.mutex_)
                {
                    cache_.quotes_[quote.Symbol] = newQuote;
                }

                TickEventArgs args = new TickEventArgs();
                args.Tick = newQuote;
                eventQueue_.PushEvent(args);
            }
            catch
            {
            }
        }

        void OnNotification(QuoteFeed.Client client, Notification notification)
        {
            try
            {
                if (notification.Type == NotificationType.ConfigUpdated)
                {
                    lock (synchronizer_)
                    {
                        // reload everything that might have changed

                        reloadFlags_ &= ~(ReloadFlags.Currencies | ReloadFlags.Symbols | ReloadFlags.SessionInfo);

                        try
                        {
                            quoteFeedClient_.GetCurrencyListAsync(this);
                            quoteFeedClient_.GetSymbolListAsync(this);
                            quoteFeedClient_.GetSessionInfoAsync(this);
                        }
                        catch
                        {
                            quoteStoreClient_.DisconnectAsync(this, "Client disconnect");
                            quoteFeedClient_.DisconnectAsync(this, "Client disconnect");
                        }
                    }

                    return;
                }

                NotificationEventArgs args = new NotificationEventArgs();
                args.Type = notification.Type;
                args.Severity = notification.Severity;
                args.Text = notification.Message;
                eventQueue_.PushEvent(args);
            }
            catch
            {
            }
        }

        void OnConnectResult(QuoteStore.Client client, object data)
        {
            try
            {
                quoteStoreClient_.LoginAsync(null, login_, password_, deviceId_, appId_, appSessionId_);
            }
            catch
            {
                quoteStoreClient_.DisconnectAsync(this, "Client disconnect");
                quoteFeedClient_.DisconnectAsync(this, "Client disconnect");
            }
        }

        void OnConnectError(QuoteStore.Client client, object data, string text)
        {
            try
            {
                lock (synchronizer_)
                {
                    quoteFeedClient_.DisconnectAsync(this, "Client disconnect");

                    if (! logout_)
                    {
                        logout_ = true;

                        LogoutEventArgs args = new LogoutEventArgs();
                        args.Reason = LogoutReason.NetworkError;
                        args.Text = text;
                        eventQueue_.PushEvent(args);

                        loginException_ = new LogoutException(text);
                        loginEvent_.Set();
                    }
                }
            }
            catch
            {
            }
        }

        void OnDisconnectResult(QuoteStore.Client client, object data, string text)
        {
            try
            {
                lock (synchronizer_)
                {
                    initFlags_ &= ~InitFlags.StoreLogin;

                    if (! logout_)
                    {
                        logout_ = true;

                        LogoutEventArgs args = new LogoutEventArgs();
                        args.Reason = LogoutReason.ClientInitiated;
                        args.Text = text;
                        eventQueue_.PushEvent(args);

                        loginException_ = new LogoutException(text);
                        loginEvent_.Set();
                    }
                }
            }
            catch
            {
            }
        }

        void OnDisconnect(QuoteStore.Client client, string text)
        {
            try
            {
                lock (synchronizer_)
                {
                    initFlags_ &= ~InitFlags.StoreLogin;

                    if (! logout_)
                    {
                        logout_ = true;

                        LogoutEventArgs args = new LogoutEventArgs();
                        args.Reason = LogoutReason.NetworkError;
                        args.Text = text;
                        eventQueue_.PushEvent(args);

                        loginException_ = new LogoutException(text);
                        loginEvent_.Set();
                    }
                }
            }
            catch
            {
            }
        }

        void OnReconnect(QuoteStore.Client client)
        {
            try
            {
                quoteStoreClient_.LoginAsync(null, login_, password_, deviceId_, appId_, appSessionId_);
            }
            catch
            {
                quoteStoreClient_.DisconnectAsync(this, "Client disconnect");
                quoteFeedClient_.DisconnectAsync(this, "Client disconnect");
            }
        }

        void OnReconnectError(QuoteStore.Client client, string text)
        {
            try
            {
                quoteFeedClient_.DisconnectAsync(this, "Client disconnect");
            }
            catch
            {
            }
        }

        void OnLoginResult(QuoteStore.Client client, object data)
        {
            try
            {
                lock (synchronizer_)
                {
                    if (initFlags_ != InitFlags.All)
                    {
                        initFlags_ |= InitFlags.StoreLogin;

                        if (initFlags_ == InitFlags.All)
                        {
                            logout_ = false;
                            PushLoginEvents();

                            loginException_ = null;
                            loginEvent_.Set();
                        }
                    }
                }
            }
            catch
            {
                quoteStoreClient_.DisconnectAsync(this, "Client disconnect");
                quoteFeedClient_.DisconnectAsync(this, "Client disconnect");
            }
        }

        void OnLoginError(QuoteStore.Client client, object data, string text)
        {
            try
            {
                lock (synchronizer_)
                {
                    quoteFeedClient_.DisconnectAsync(this, "Client disconnect");

                    if (! logout_)
                    {
                        logout_ = true;

                        LogoutEventArgs args = new LogoutEventArgs();
                        args.Reason = LogoutReason.InvalidCredentials;
                        args.Text = text;
                        eventQueue_.PushEvent(args);

                        loginException_ = new LogoutException(text);
                        loginEvent_.Set();
                    }
                }
            }
            catch
            {
            }
        }

        void OnLogoutResult(QuoteStore.Client client, object data, LogoutInfo logoutInfo)
        {
            try
            {
                lock (synchronizer_)
                {
                    quoteStoreClient_.DisconnectAsync(this, "Client disconnect");

                    if (! logout_)
                    {
                        logout_ = true;

                        LogoutEventArgs args = new LogoutEventArgs();
                        args.Reason = logoutInfo.Reason;
                        args.Text = logoutInfo.Message;
                        eventQueue_.PushEvent(args);
                    }
                }
            }
            catch
            {
            }
        }

        void OnLogout(QuoteStore.Client client, LogoutInfo logoutInfo)
        {
            try
            {
                lock (synchronizer_)
                {
                    quoteStoreClient_.DisconnectAsync(this, "Client disconnect");

                    if (! logout_)
                    {
                        logout_ = true;

                        LogoutEventArgs args = new LogoutEventArgs();
                        args.Reason = logoutInfo.Reason;
                        args.Text = logoutInfo.Message;
                        eventQueue_.PushEvent(args);
                    }
                }
            }
            catch
            {
            }
        }

        void OnNotification(QuoteStore.Client client, Notification notification)
        {
            try
            {
                if (notification.Type == NotificationType.ConfigUpdated)
                {
                    // nothing to reload here

                    return;
                }
            }
            catch
            {
            }            
        }

        void PushLoginEvents()
        {
            LogonEventArgs args = new LogonEventArgs();
            args.ProtocolVersion = "";
            eventQueue_.PushEvent(args);
                        
            CurrencyInfo[] currencies;
            SymbolInfo[] symbols;
            SessionInfo sessionInfo;

            lock (cache_)
            {                
                currencies = cache_.currencies_;
                symbols = cache_.symbols_;
                sessionInfo = cache_.sessionInfo_;
            }

            // For backward comapatibility
            CurrencyInfoEventArgs currencyArgs = new CurrencyInfoEventArgs();
            currencyArgs.Information = currencies;
            eventQueue_.PushEvent(currencyArgs);

            // For backward comapatibility
            SymbolInfoEventArgs symbolArgs = new SymbolInfoEventArgs();
            symbolArgs.Information = symbols;
            eventQueue_.PushEvent(symbolArgs);

            // For backward comapatibility
            SessionInfoEventArgs sessionArgs = new SessionInfoEventArgs();
            sessionArgs.Information = sessionInfo;
            eventQueue_.PushEvent(sessionArgs);

            // For backward comapatibility
            CacheEventArgs cacheArgs = new CacheEventArgs();
            eventQueue_.PushEvent(cacheArgs);
        }

        void PushConfigUpdateEvents()
        {
            NotificationEventArgs args = new NotificationEventArgs();
            args.Type = NotificationType.ConfigUpdated;
            args.Severity = NotificationSeverity.Information;
            args.Text = "Data feed configuration changed";
            eventQueue_.PushEvent(args);
                        
            CurrencyInfo[] currencies;
            SymbolInfo[] symbols;
            SessionInfo sessionInfo;

            lock (cache_)
            {                
                currencies = cache_.currencies_;
                symbols = cache_.symbols_;
                sessionInfo = cache_.sessionInfo_;
            }

            // For backward comapatibility
            CurrencyInfoEventArgs currencyArgs = new CurrencyInfoEventArgs();
            currencyArgs.Information = currencies;
            eventQueue_.PushEvent(currencyArgs);

            // For backward comapatibility
            SymbolInfoEventArgs symbolArgs = new SymbolInfoEventArgs();
            symbolArgs.Information = symbols;
            eventQueue_.PushEvent(symbolArgs);

            // For backward comapatibility
            SessionInfoEventArgs sessionArgs = new SessionInfoEventArgs();
            sessionArgs.Information = sessionInfo;
            eventQueue_.PushEvent(sessionArgs);
        }

        void EventThread()
        {
            try
            {
                while (true)
                {
                    EventArgs eventArgs;
                    if (! eventQueue_.PopEvent(out eventArgs))
                        break;

                    try
                    {
                        DispatchEvent(eventArgs);
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

        void DispatchEvent(EventArgs eventArgs)
        {
            LogonEventArgs logonEventArgs = eventArgs as LogonEventArgs;

            if (logonEventArgs != null)
            {
                if (Logon != null)
                {
                    try
                    {
                        Logon(this, logonEventArgs);
                    }
                    catch
                    {
                    }
                }

                return;
            }

            CacheEventArgs cacheEventArgs = eventArgs as CacheEventArgs;

            if (cacheEventArgs != null)
            {
                if (CacheInitialized != null)
                {
                    try
                    {
                        CacheInitialized(this, cacheEventArgs);
                    }
                    catch
                    {
                    }
                }

                return;
            }

            LogoutEventArgs logoutEventArgs = eventArgs as LogoutEventArgs;

            if (logoutEventArgs != null)
            {
                if (Logout != null)
                {
                    try
                    {
                        Logout(this, logoutEventArgs);
                    }
                    catch
                    {
                    }
                }

                return;
            }

            TwoFactorAuthEventArgs twoFactorAuthEventArgs = eventArgs as TwoFactorAuthEventArgs;

            if (twoFactorAuthEventArgs != null)
            {
                if (TwoFactorAuth != null)
                {
                    try
                    {
                        TwoFactorAuth(this, twoFactorAuthEventArgs);
                    }
                    catch
                    {
                    }
                }

                return;
            }

            CurrencyInfoEventArgs currencyEventArgs = eventArgs as CurrencyInfoEventArgs;

            if (currencyEventArgs != null)
            {
                if (CurrencyInfo != null)
                {
                    try
                    {
                        CurrencyInfo(this, currencyEventArgs);
                    }
                    catch
                    {
                    }
                }

                return;
            }

            SymbolInfoEventArgs symbolEventArgs = eventArgs as SymbolInfoEventArgs;

            if (symbolEventArgs != null)
            {
                if (SymbolInfo != null)
                {
                    try
                    {
                        SymbolInfo(this, symbolEventArgs);
                    }
                    catch
                    {
                    }
                }

                return;
            }

            SubscribedEventArgs subscribedEventArgs = eventArgs as SubscribedEventArgs;

            if (subscribedEventArgs != null)
            {
                if (Subscribed != null)
                {
                    try
                    {
                        Subscribed(this, subscribedEventArgs);
                    }
                    catch
                    {
                    }
                }

                return;
            }

            UnsubscribedEventArgs unsubscribedEventArgs = eventArgs as UnsubscribedEventArgs;

            if (unsubscribedEventArgs != null)
            {
                if (Unsubscribed != null)
                {
                    try
                    {
                        Unsubscribed(this, unsubscribedEventArgs);
                    }
                    catch
                    {
                    }
                }

                return;
            }

            SessionInfoEventArgs sessionInfoEventArgs = eventArgs as SessionInfoEventArgs;

            if (sessionInfoEventArgs != null)
            {
                if (SessionInfo != null)
                {
                    try
                    {
                        SessionInfo(this, sessionInfoEventArgs);
                    }
                    catch
                    {
                    }
                }

                return;
            }

            TickEventArgs tickEventArgs = eventArgs as TickEventArgs;

            if (tickEventArgs != null)
            {
                if (Tick != null)
                {
                    try
                    {
                        Tick(this, tickEventArgs);
                    }
                    catch
                    {
                    }
                }

                return;
            }

            NotificationEventArgs notificationEventArgs = eventArgs as NotificationEventArgs;

            if (notificationEventArgs != null)
            {
                if (Notify != null)
                {
                    try
                    {
                        Notify(this, notificationEventArgs);
                    }
                    catch
                    {
                    }
                }

                return;
            }
        }

        internal enum InitFlags
        {
            None = 0x00,                        
            Currencies = 0x01,
            Symbols = 0x2,
            SessionInfo = 0x04,
            StoreLogin = 0x08,
            All = SessionInfo | Currencies | Symbols | StoreLogin
        }

        internal enum ReloadFlags
        {
            None = 0x00,                        
            Currencies = 0x01,
            Symbols = 0x2,
            SessionInfo = 0x04,
            All = SessionInfo | Currencies | Symbols
        }

        internal string name_;
        internal string address_;
        internal string login_;
        internal string password_;
        internal string deviceId_;
        internal string appId_;
        internal string appSessionId_;
        internal int synchOperationTimeout_;

        internal DataFeedServer server_;
        internal DataFeedCache cache_;
        internal Network network_;
        internal QuoteFeed.Client quoteFeedClient_;
        internal QuoteStore.Client quoteStoreClient_;

        internal object synchronizer_;        
        internal bool started_;
        
        internal ManualResetEvent loginEvent_;
        internal Exception loginException_;
        internal InitFlags initFlags_;
        internal ReloadFlags reloadFlags_;
        internal bool logout_;        

        // We employ a queue to allow the client call sync functions from event handlers
        internal Thread eventThread_;
        internal EventQueue eventQueue_;

        #endregion
    }
}
