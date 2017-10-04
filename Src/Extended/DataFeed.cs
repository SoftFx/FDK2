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

            bool decodeLogMessages;
            if (! connectionStringParser.TryGetBoolValue("DecodeLogMessages", out decodeLogMessages))
                decodeLogMessages = false;

            synchronizer_ = new object();

            quoteFeedClient_ = new QuoteFeed.Client(name_ + "_QuoteFeed", quoteFeedPort, true, logDirectory, decodeLogMessages);
            quoteFeedClient_.ConnectEvent += new QuoteFeed.Client.ConnectDelegate(this.OnConnect);
            quoteFeedClient_.ConnectErrorEvent += new QuoteFeed.Client.ConnectErrorDelegate(this.OnConnectError);
            quoteFeedClient_.DisconnectEvent += new QuoteFeed.Client.DisconnectDelegate(this.OnDisconnect);
            quoteFeedClient_.OneTimePasswordRequestEvent += new QuoteFeed.Client.OneTimePasswordRequestDelegate(this.OnOneTimePasswordRequest);
            quoteFeedClient_.OneTimePasswordRejectEvent += new QuoteFeed.Client.OneTimePasswordRejectDelegate(this.OnOneTimePasswordReject);
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
            quoteFeedClient_.QuotesBeginEvent += new QuoteFeed.Client.QuotesBeginDelegate(this.OnQuotesBegin);
            quoteFeedClient_.QuotesEndEvent += new QuoteFeed.Client.QuotesEndDelegate(this.OnQuotesEnd);
            quoteFeedClient_.QuoteUpdateEvent += new QuoteFeed.Client.QuoteUpdateDelegate(this.OnQuoteUpdate);

            quoteStoreClient_ = new QuoteStore.Client(name_ + "_QuoteStore", quoteStorePort, true, logDirectory, decodeLogMessages);
            quoteStoreClient_.ConnectEvent += new QuoteStore.Client.ConnectDelegate(this.OnConnect);
            quoteStoreClient_.ConnectErrorEvent += new QuoteStore.Client.ConnectErrorDelegate(this.OnConnectError);
            quoteStoreClient_.DisconnectEvent += new QuoteStore.Client.DisconnectDelegate(this.OnDisconnect);
            quoteStoreClient_.LoginResultEvent += new QuoteStore.Client.LoginResultDelegate(this.OnLoginResult);
            quoteStoreClient_.LoginErrorEvent += new QuoteStore.Client.LoginErrorDelegate(this.OnLoginError);
            quoteStoreClient_.LogoutResultEvent += new QuoteStore.Client.LogoutResultDelegate(this.OnLogoutResult);
            quoteStoreClient_.LogoutEvent += new QuoteStore.Client.LogoutDelegate(this.OnLogout);

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
        /// Gets or sets default synchronous operation timeout in milliseconds.
        /// </summary>
        [Obsolete("Please use OperationTimeout connection string parameter")]
        public int SynchOperationTimeout
        {
            set { synchOperationTimeout_ = value;  }

            get { return synchOperationTimeout_; }
        }

/*
        /// Gets or sets queue size for quotes.
        /// Note: FDK uses a separated queue for every symbol.
        /// </summary>
        public int QuotesQueueThresholdSize
        {
            get
            {
                throw new Exception("Not impled");
            }

            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Quotes queue size must be positive");

                throw new Exception("Not impled");
            }
        }
*/
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
        /// Occurs when subscribed to a new symbol.
        /// </summary>
        public event SubscribedHandler Subscribed;

        /// <summary>
        /// Occurs when unsubscribed from the symbol.
        /// </summary>
        public event UnsubscribedHandler Unsubscribed;

        /// <summary>
        /// Occurs when session info received or changed.
        /// </summary>
        public event SessionInfoHandler SessionInfo;

        /// <summary>
        /// Occurs when a new quote is received.
        /// </summary>
        public event TickHandler Tick;

        /// <summary>
        /// Occurs when a notification is received.
        /// </summary>
        public event NotifyHandler Notify;

        /// <summary>
        /// Occurs when currencies information is initialized.
        /// </summary>
        [Obsolete("Please use Logon event")]
        public event CurrencyInfoHandler CurrencyInfo;

        /// <summary>
        /// Occurs when symbols information is initialized.
        /// </summary>
        [Obsolete("Please use Logon event")]
        public event SymbolInfoHandler SymbolInfo;

        /// <summary>
        /// Occurs when local cache initialized.
        /// </summary>
        [Obsolete("Please use Logon event")]
        public event CacheHandler CacheInitialized;

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
                        quoteFeedClient_.ConnectAsync(address_);

                        try
                        {
                            quoteStoreClient_.ConnectAsync(address_);

                            started_ = true;
                        }
                        catch
                        {
                            quoteFeedClient_.DisconnectAsync("");
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
                // have to wait here since we don't have Join()

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
        /// Starts data feed/trade instance and waits for logon event.
        /// </summary>
        /// <param name="timeoutInMilliseconds">Timeout of logon waiting.</param>
        /// <returns>true, if logon event is occurred, otherwise false</returns>
        [Obsolete("Please use WaitForLogonEx()")]
        public bool Start(int timeoutInMilliseconds)
        {
            Start();

            return WaitForLogon(timeoutInMilliseconds);
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
                        quoteStoreClient_.LogoutAsync(null, "Client logout");
                    }
                    catch
                    {                        
                        quoteStoreClient_.DisconnectAsync("Client disconnect");
                    }

                    try
                    {
                        quoteFeedClient_.LogoutAsync(null, "Client logout");
                    }
                    catch
                    {                        
                        quoteFeedClient_.DisconnectAsync("Client disconnect");
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
        /// Blocks the calling thread until logon succeeds or fails.
        /// </summary>
        /// <param name="timeoutInMilliseconds"></param>
        /// <returns></returns>
        [Obsolete("Please use WaitForLogonEx()")]
        public bool WaitForLogon(int timeoutInMilliseconds)
        {
            return WaitForLogonEx(timeoutInMilliseconds);
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

        #region Private

        void OnConnect(QuoteFeed.Client client)
        {
            try
            {
                quoteFeedClient_.LoginAsync(null, login_, password_, deviceId_, appSessionId_);
            }
            catch
            {
                quoteStoreClient_.DisconnectAsync("Client disconnect");
                quoteFeedClient_.DisconnectAsync("Client disconnect");                
            }
        }

        void OnConnectError(QuoteFeed.Client client, string text)
        {
            try
            {
                lock (synchronizer_)
                {
                    quoteStoreClient_.DisconnectAsync("Client disconnect");

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

        void OnOneTimePasswordRequest(QuoteFeed.Client client, string text)
        {
            try
            {
                TwoFactorAuthEventArgs args = new TwoFactorAuthEventArgs();                    
                TwoFactorAuth twoFactorAuth = new TwoFactorAuth();
                twoFactorAuth.Reason = TwoFactorReason.ServerRequest;
                twoFactorAuth.Text = text;
                args.TwoFactorAuth = twoFactorAuth;
                eventQueue_.PushEvent(args);
            }
            catch
            {
            }
        }

        void OnOneTimePasswordReject(QuoteFeed.Client client, string text)
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

        void OnLoginResult(QuoteFeed.Client client, object data)
        {
            try
            {
                quoteFeedClient_.GetSessionInfoAsync(this);
                quoteFeedClient_.GetCurrencyListAsync(this);
                quoteFeedClient_.GetSymbolListAsync(this);
            }
            catch
            {
                quoteStoreClient_.DisconnectAsync("Client disconnect");
                quoteFeedClient_.DisconnectAsync("Client disconnect");                
            }
        }

        void OnLoginError(QuoteFeed.Client client, object data, string text)
        {
            try
            {
                lock (synchronizer_)
                {
                    quoteStoreClient_.DisconnectAsync("Client disconnect");

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
                            if (cache_.sessionInfo_ == null)
                                cache_.sessionInfo_ = sessionInfo;
                        }

                        initFlags_ |= InitFlags.SessionInfo;

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
            }
        }

        void OnSessionInfoError(QuoteFeed.Client client, object data, string message)
        {
            try
            {
                if (data == this)
                {
                    quoteStoreClient_.DisconnectAsync("Client disconnect");
                    quoteFeedClient_.DisconnectAsync("Client disconnect");                    
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
                            if (cache_.currencies_ == null)
                                cache_.currencies_ = currencies;
                        }

                        initFlags_ |= InitFlags.Currencies;

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
            }
        }

        void OnCurrencyListError(QuoteFeed.Client client, object data, string message)
        {
            try
            {
                if (data == this)
                {
                    quoteStoreClient_.DisconnectAsync("Client disconnect");
                    quoteFeedClient_.DisconnectAsync("Client disconnect");                    
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
                            if (cache_.symbols_ == null)
                                cache_.symbols_ = symbols;
                        }

                        initFlags_ |= InitFlags.Symbols;

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
            }
        }

        void OnSymbolListError(QuoteFeed.Client client, object data, string message)
        {
            try
            {
                if (data == this)
                {
                    quoteStoreClient_.DisconnectAsync("Client disconnect");
                    quoteFeedClient_.DisconnectAsync("Client disconnect");                    
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

        void OnQuotesBegin(QuoteFeed.Client client, Quote[] quotes)
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
                    SubscribedEventArgs args = new SubscribedEventArgs();
                    args.Tick = quotes[index];
                    eventQueue_.PushEvent(args);
                }
            }
            catch
            {
            }
        }

        void OnQuotesEnd(QuoteFeed.Client client, string[] symbolIds)
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

        void OnQuoteUpdate(QuoteFeed.Client client, Quote quote)
        {
            try
            {
                lock (cache_.mutex_)
                {
                    cache_.quotes_[quote.Symbol] = quote;
                }

                TickEventArgs args = new TickEventArgs();
                args.Tick = quote;
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

        void OnConnect(QuoteStore.Client client)
        {
            try
            {
                quoteStoreClient_.LoginAsync(null, login_, password_, deviceId_, appSessionId_);
            }
            catch
            {
                quoteStoreClient_.DisconnectAsync("Client disconnect");
                quoteFeedClient_.DisconnectAsync("Client disconnect");
            }
        }

        void OnConnectError(QuoteStore.Client client, string text)
        {
            try
            {
                lock (synchronizer_)
                {
                    quoteFeedClient_.DisconnectAsync("Client disconnect");

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

        void OnLoginResult(QuoteStore.Client client, object data)
        {
            try
            {
                lock (synchronizer_)
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
            catch
            {
                quoteStoreClient_.DisconnectAsync("Client disconnect");
                quoteFeedClient_.DisconnectAsync("Client disconnect");
            }
        }

        void OnLoginError(QuoteStore.Client client, object data, string text)
        {
            try
            {
                lock (synchronizer_)
                {
                    quoteFeedClient_.DisconnectAsync("Client disconnect");

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

        void PushLoginEvents()
        {
            LogonEventArgs args = new LogonEventArgs();
            args.ProtocolVersion = "";
            eventQueue_.PushEvent(args);

            CurrencyInfo[] currencies;
            SymbolInfo[] symbols;

            lock (cache_)
            {
                currencies = cache_.currencies_;
                symbols = cache_.symbols_;
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
            CacheEventArgs cacheArgs = new CacheEventArgs();
            eventQueue_.PushEvent(cacheArgs);
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

        enum InitFlags
        {
            None = 0x00,            
            SessionInfo = 0x01,
            Currencies = 0x02,
            Symbols = 0x4,
            StoreLogin = 0x08,
            All = SessionInfo | Currencies | Symbols | StoreLogin
        }

        string name_;
        string address_;
        string login_;
        string password_;
        string deviceId_;
        string appSessionId_;
        internal int synchOperationTimeout_;

        internal DataFeedServer server_;
        internal DataFeedCache cache_;
        internal Network network_;
        internal QuoteFeed.Client quoteFeedClient_;
        internal QuoteStore.Client quoteStoreClient_;

        object synchronizer_;        
        bool started_;
        
        ManualResetEvent loginEvent_;
        Exception loginException_;
        InitFlags initFlags_;
        bool logout_;

        // We employ a queue to allow the client call sync functions from event handlers
        Thread eventThread_;
        EventQueue eventQueue_;

        #endregion
    }
}
