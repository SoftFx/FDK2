namespace TickTrader.FDK.Extended
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Common;
    using OrderEntry;
    using TradeCapture;


    /// <summary>
    /// This class connects to trading platform and provides trading functionality.
    /// </summary>
    public class DataTrade
    {
        #region Construction

        /// <summary>
        /// Creates a new data trade instance.
        /// </summary>
        public DataTrade() : this(null, "DataTrade")
        {
        }

        /// <summary>
        /// Creates and initializes a new data trade instance.
        /// </summary>
        /// <exception cref="System.ArgumentNullException">If connectionString is null.</exception>
        public DataTrade(string connectionString) : this(connectionString, "DataTrade")
        {
        }

        /// <summary>
        /// Creates and initializes a new data trade instance.
        /// </summary>
        /// <exception cref="System.ArgumentNullException">If connectionString is null.</exception>
        public DataTrade(string connectionString, string name)
        {
            name_ = name;
            server_ = new DataTradeServer(this);
            cache_ = new DataTradeCache(this);
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

            int orderEntryPort;
            if (! connectionStringParser.TryGetIntValue("OrderEntryPort", out orderEntryPort))
                orderEntryPort = 5040;

            int tradeCapturePort;
            if (! connectionStringParser.TryGetIntValue("TradeCapturePort", out tradeCapturePort))
                tradeCapturePort = 5060;

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

            orderEntryClient_ = new OrderEntry.Client(name_ + ".OrderEntry", logMessages, orderEntryPort, 1, -1, 10000, 10000, logDirectory);
            orderEntryClient_.ConnectEvent += new OrderEntry.Client.ConnectDelegate(this.OnConnect);
            orderEntryClient_.ConnectErrorEvent += new OrderEntry.Client.ConnectErrorDelegate(this.OnConnectError);
            orderEntryClient_.DisconnectEvent += new OrderEntry.Client.DisconnectDelegate(this.OnDisconnect);
            orderEntryClient_.TwoFactorLoginRequestEvent += new OrderEntry.Client.TwoFactorLoginRequestDelegate(this.OnTwoFactorLoginRequest);
            orderEntryClient_.TwoFactorLoginResultEvent += new OrderEntry.Client.TwoFactorLoginResultDelegate(this.OnTwoFactorLoginResult);
            orderEntryClient_.TwoFactorLoginErrorEvent += new OrderEntry.Client.TwoFactorLoginErrorDelegate(this.OnTwoFactorLoginError);
            orderEntryClient_.TwoFactorLoginResumeEvent += new OrderEntry.Client.TwoFactorLoginResumeDelegate(this.OnTwoFactorLoginResume);
            orderEntryClient_.LoginResultEvent += new OrderEntry.Client.LoginResultDelegate(this.OnLoginResult);
            orderEntryClient_.LoginErrorEvent += new OrderEntry.Client.LoginErrorDelegate(this.OnLoginError);
            orderEntryClient_.LogoutResultEvent += new OrderEntry.Client.LogoutResultDelegate(this.OnLogoutResult);
            orderEntryClient_.LogoutEvent += new OrderEntry.Client.LogoutDelegate(this.OnLogout);
            orderEntryClient_.TradeServerInfoResultEvent += new OrderEntry.Client.TradeServerInfoResultDelegate(this.OnTradeServerInfoResult);
            orderEntryClient_.TradeServerInfoErrorEvent += new OrderEntry.Client.TradeServerInfoErrorDelegate(this.OnTradeServerInfoError);
            orderEntryClient_.SessionInfoResultEvent += new OrderEntry.Client.SessionInfoResultDelegate(this.OnSessionInfoResult);
            orderEntryClient_.SessionInfoErrorEvent += new OrderEntry.Client.SessionInfoErrorDelegate(this.OnSessionInfoError);
            orderEntryClient_.AccountInfoResultEvent += new OrderEntry.Client.AccountInfoResultDelegate(this.OnAccountInfoResult);
            orderEntryClient_.AccountInfoErrorEvent += new OrderEntry.Client.AccountInfoErrorDelegate(this.OnAccountInfoError);
            orderEntryClient_.PositionsResultEvent += new OrderEntry.Client.PositionsResultDelegate(this.OnPositionsResult);
            orderEntryClient_.PositionsErrorEvent += new OrderEntry.Client.PositionsErrorDelegate(this.OnPositionsError);
            orderEntryClient_.OrdersResultEvent += new OrderEntry.Client.OrdersResultDelegate(this.OnOrdersResult);
            orderEntryClient_.OrdersErrorEvent += new OrderEntry.Client.OrdersErrorDelegate(this.OnOrdersError);
            orderEntryClient_.NewOrderResultEvent += new OrderEntry.Client.NewOrderResultDelegate(this.OnNewOrderResult);
            orderEntryClient_.NewOrderErrorEvent += new OrderEntry.Client.NewOrderErrorDelegate(this.OnNewOrderError);
            orderEntryClient_.ReplaceOrderResultEvent += new OrderEntry.Client.ReplaceOrderResultDelegate(this.OnReplaceOrderResult);
            orderEntryClient_.ReplaceOrderErrorEvent += new OrderEntry.Client.ReplaceOrderErrorDelegate(this.OnReplaceOrderError);
            orderEntryClient_.CancelOrderResultEvent += new OrderEntry.Client.CancelOrderResultDelegate(this.OnCancelOrderResult);
            orderEntryClient_.CancelOrderErrorEvent += new OrderEntry.Client.CancelOrderErrorDelegate(this.OnCancelOrderError);
            orderEntryClient_.ClosePositionResultEvent += new OrderEntry.Client.ClosePositionResultDelegate(this.OnClosePositionResult);
            orderEntryClient_.ClosePositionErrorEvent += new OrderEntry.Client.ClosePositionErrorDelegate(this.OnClosePositionError);
            orderEntryClient_.ClosePositionByResultEvent += new OrderEntry.Client.ClosePositionByResultDelegate(this.OnClosePositionByResult);
            orderEntryClient_.ClosePositionByErrorEvent += new OrderEntry.Client.ClosePositionByErrorDelegate(this.OnClosePositionByError);
            orderEntryClient_.SessionInfoUpdateEvent += new OrderEntry.Client.SessionInfoUpdateDelegate(this.OnSessionInfoUpdate);
            orderEntryClient_.AccountInfoUpdateEvent += new OrderEntry.Client.AccountInfoUpdateDelegate(this.OnAccountInfoUpdate);
            orderEntryClient_.OrderUpdateEvent += new OrderEntry.Client.OrderUpdateDelegate(this.OnOrderUpdate);
            orderEntryClient_.PositionUpdateEvent += new OrderEntry.Client.PositionUpdateDelegate(this.OnPositionUpdate);
            orderEntryClient_.BalanceUpdateEvent += new OrderEntry.Client.BalanceUpdateDelegate(this.OnBalanceUpdate);
            orderEntryClient_.NotificationEvent += new OrderEntry.Client.NotificationDelegate(this.OnNotification);

            tradeCaptureClient_ = new TradeCapture.Client(name_ + ".TradeCapture", logMessages, tradeCapturePort, 1, -1, 10000, 10000, logDirectory);
            tradeCaptureClient_.ConnectEvent += new TradeCapture.Client.ConnectDelegate(this.OnConnect);
            tradeCaptureClient_.ConnectErrorEvent += new TradeCapture.Client.ConnectErrorDelegate(this.OnConnectError);
            tradeCaptureClient_.DisconnectEvent += new TradeCapture.Client.DisconnectDelegate(this.OnDisconnect);
            tradeCaptureClient_.TwoFactorLoginRequestEvent += new TradeCapture.Client.TwoFactorLoginRequestDelegate(this.OnTwoFactorLoginRequest);
            tradeCaptureClient_.TwoFactorLoginResultEvent += new TradeCapture.Client.TwoFactorLoginResultDelegate(this.OnTwoFactorLoginResult);
            tradeCaptureClient_.TwoFactorLoginErrorEvent += new TradeCapture.Client.TwoFactorLoginErrorDelegate(this.OnTwoFactorLoginError);
            tradeCaptureClient_.TwoFactorLoginResumeEvent += new TradeCapture.Client.TwoFactorLoginResumeDelegate(this.OnTwoFactorLoginResume);
            tradeCaptureClient_.LoginResultEvent += new TradeCapture.Client.LoginResultDelegate(this.OnLoginResult);
            tradeCaptureClient_.LoginErrorEvent += new TradeCapture.Client.LoginErrorDelegate(this.OnLoginError);
            tradeCaptureClient_.LogoutResultEvent += new TradeCapture.Client.LogoutResultDelegate(this.OnLogoutResult);
            tradeCaptureClient_.LogoutEvent += new TradeCapture.Client.LogoutDelegate(this.OnLogout);
            tradeCaptureClient_.TradeUpdateEvent += new TradeCapture.Client.TradeUpdateDelegate(this.OnTradeUpdate);
            tradeCaptureClient_.NotificationEvent += new TradeCapture.Client.NotificationDelegate(this.OnNotification);

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
        public DataTradeServer Server
        {
            get { return server_; }
        }

        /// <summary>
        /// Gets object, which encapsulates client cache methods.
        /// </summary>
        public DataTradeCache Cache
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
        /// Occurs when session info received or changed.
        /// </summary>
        public event SessionInfoHandler SessionInfo;

        /// <summary>
        /// Occurs when account information is changed.
        /// </summary>
        public event AccountInfoHandler AccountInfo;

        /// <summary>
        /// Occurs when a trade operation is executing.
        /// </summary>
        public event ExecutionReportHandler ExecutionReport;

        /// <summary>
        /// The event is supported by Net account only.
        /// </summary>
        public event PositionReportHandler PositionReport;

        /// <summary>
        /// Occurs when a notification of balance operation is received.
        /// </summary>
        public event NotifyHandler<BalanceOperation> BalanceOperation;

        /// <summary>
        /// Occurs when a trade transaction report is received.
        /// </summary>
        public event TradeTransactionReportHandler TradeTransactionReport;

        /// <summary>
        /// Occurs when a notification is received.
        /// </summary>
        public event NotifyHandler Notify;

        #endregion

        #region Methods

        /// <summary>
        /// Starts data feed instance.
        /// </summary>
        public void Start()
        {
            OrderEntry.Client orderEntryClient = null;
            TradeCapture.Client tradeCaptureClient = null;
            Thread eventThread = null;

            try
            {
                lock (synchronizer_)
                {
                    if (started_)
                        throw new Exception(string.Format("Data trade is already started : {0}", name_));

                    orderEntryTwoFactorLogin_ = false;
                    tradeCaptureTwoFactorLogin_ = false;

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
                        orderEntryClient_.ConnectAsync(this, address_);

                        try
                        {
                            tradeCaptureClient_.ConnectAsync(this, address_);

                            started_ = true;
                        }
                        catch
                        {
                            orderEntryClient_.DisconnectAsync(this, "Client disconnect");
                            orderEntryClient = orderEntryClient_;

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

                if (tradeCaptureClient != null)
                    tradeCaptureClient.Join();

                if (orderEntryClient != null)
                    orderEntryClient.Join();

                throw;
            }
        }

        /// <summary>
        /// Stops data trade instance. The method can not be called into any feed/trade event handler.
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
                        tradeCaptureClient_.LogoutAsync(this, "Client logout");
                    }
                    catch
                    {                        
                        tradeCaptureClient_.DisconnectAsync(this, "Client disconnect");
                    }

                    try
                    {
                        orderEntryClient_.LogoutAsync(this, "Client logout");
                    }
                    catch
                    {                        
                        orderEntryClient_.DisconnectAsync(this, "Client disconnect");
                    }
                }
            }

            tradeCaptureClient_.Join();
            orderEntryClient_.Join();

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
            tradeCaptureClient_.Dispose();
            orderEntryClient_.Dispose();

            GC.SuppressFinalize(this);
        }

        #endregion

        #region Obsoletes

        /// <summary>
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
            this.Start();
            return this.WaitForLogonEx(timeoutInMilliseconds);
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

        void OnConnect(OrderEntry.Client client, object data)
        {
            try
            {
                orderEntryClient_.LoginAsync(null, login_, password_, deviceId_, appId_, appSessionId_);
            }
            catch
            {
                tradeCaptureClient_.DisconnectAsync(this, "Client disconnect");
                orderEntryClient_.DisconnectAsync(this, "Client disconnect");
            }
        }

        void OnConnectError(OrderEntry.Client client, object data, string text)
        {
            try
            {
                lock (synchronizer_)
                {
                    tradeCaptureClient_.DisconnectAsync(this, "Client disconnect");

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

        void OnDisconnect(OrderEntry.Client client, object data, string text)
        {
            try
            {
                lock (synchronizer_)
                {
                    initFlags_ &= ~(InitFlags.TradeServerInfo | InitFlags.AccountInfo | InitFlags.SessionInfo | InitFlags.TradeRecords | InitFlags.Positions);

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

        void OnTwoFactorLoginRequest(OrderEntry.Client client, string text)
        {
            try
            {
                lock (synchronizer_)
                {
                    orderEntryTwoFactorLogin_ = true;
                }

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

        void OnTwoFactorLoginResult(OrderEntry.Client client, object data, DateTime expireTime)
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

        void OnTwoFactorLoginError(OrderEntry.Client client, object data, string text)
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

        void OnTwoFactorLoginResume(OrderEntry.Client client, object data, DateTime expireTime)
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

        void OnLoginResult(OrderEntry.Client client, object data)
        {
            try
            {
                orderEntryClient_.GetTradeServerInfoAsync(this);                
                orderEntryClient_.GetAccountInfoAsync(this);
                orderEntryClient_.GetSessionInfoAsync(this);
                orderEntryClient_.GetOrdersAsync(this);
            }
            catch
            {
                tradeCaptureClient_.DisconnectAsync(this, "Client disconnect");
                orderEntryClient_.DisconnectAsync(this, "Client disconnect");
            }
        }

        void OnLoginError(OrderEntry.Client client, object data, string text)
        {
            try
            {
                lock (synchronizer_)
                {
                    tradeCaptureClient_.DisconnectAsync(this, "Client disconnect");

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

        void OnTradeServerInfoResult(OrderEntry.Client client, object data, TradeServerInfo tradeServerInfo)
        {
            try
            {
                if (data == this)
                {
                    lock (synchronizer_)
                    {
                        lock (cache_.mutex_)
                        {
                            cache_.tradeServerInfo_ = tradeServerInfo;
                        }

                        if (initFlags_ != InitFlags.All)
                        {
                            initFlags_ |= InitFlags.TradeServerInfo;

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
                            reloadFlags_ |= ReloadFlags.TradeServerInfo;

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

        void OnTradeServerInfoError(OrderEntry.Client client, object data, string message)
        {
            try
            {
                if (data == this)
                {
                    tradeCaptureClient_.DisconnectAsync(this, "Client disconnect");
                    orderEntryClient_.DisconnectAsync(this, "Client disconnect");
                }
            }
            catch
            {
            }
        }

        void OnAccountInfoResult(OrderEntry.Client client, object data, AccountInfo accountInfo)
        {
            try
            {
                if (data == this)
                {
                    lock (synchronizer_)
                    {
                        lock (cache_.mutex_)
                        {
                            cache_.accountInfo_ = accountInfo;
                        }

                        if (initFlags_ != InitFlags.All)
                        {
                            initFlags_ |= InitFlags.AccountInfo;

                            if (accountInfo.Type == AccountType.Net)
                            {
                                orderEntryClient_.GetPositionsAsync(this);
                            }
                            else
                            {
                                initFlags_ |= InitFlags.Positions;

                                if (initFlags_ == InitFlags.All)
                                {
                                    logout_ = false;
                                    PushLoginEvents();

                                    loginException_ = null;
                                    loginEvent_.Set();
                                }
                            }
                        }
                        else if (reloadFlags_ != ReloadFlags.All)
                        {
                            reloadFlags_ |= ReloadFlags.AccountInfo;

                            if (accountInfo.Type == AccountType.Net)
                            {
                                orderEntryClient_.GetPositionsAsync(this);
                            }
                            else
                            {
                                reloadFlags_ |= ReloadFlags.Positions;

                                if (reloadFlags_ == ReloadFlags.All)
                                {
                                    PushConfigUpdateEvents();
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }

        void OnAccountInfoError(OrderEntry.Client client, object data, string message)
        {
            try
            {
                if (data == this)
                {
                    tradeCaptureClient_.DisconnectAsync(this, "Client disconnect");
                    orderEntryClient_.DisconnectAsync(this, "Client disconnect");
                }
            }
            catch
            {
            }
        }

        void OnSessionInfoResult(OrderEntry.Client client, object data, SessionInfo sessionInfo)
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

        void OnSessionInfoError(OrderEntry.Client client, object data, string message)
        {
            try
            {
                if (data == this)
                {
                    tradeCaptureClient_.DisconnectAsync(this, "Client disconnect");
                    orderEntryClient_.DisconnectAsync(this, "Client disconnect");
                }
            }
            catch
            {
            }
        }

        void OnPositionsResult(OrderEntry.Client client, object data, Position[] positions)
        {
            try
            {
                if (data == this)
                {
                    lock (synchronizer_)
                    {
                        lock (cache_.mutex_)
                        {
                            cache_.positions_ = new Dictionary<string, Position>(positions.Length);

                            foreach (Position position in positions)
                                cache_.positions_.Add(position.Symbol, position);
                        }

                        if (initFlags_ != InitFlags.All)
                        {
                            initFlags_ |= InitFlags.Positions;

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
                            reloadFlags_ |= ReloadFlags.Positions;

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

        void OnPositionsError(OrderEntry.Client client, object data, string message)
        {
            try
            {
                if (data == this)
                {
                    tradeCaptureClient_.DisconnectAsync(this, "Client disconnect");
                    orderEntryClient_.DisconnectAsync(this, "Client disconnect");
                }
            }
            catch
            {
            }
        }

        void OnOrdersResult(OrderEntry.Client client, object data, ExecutionReport[] executionReports)
        {
            try
            {
                if (data == this)
                {
                    lock (synchronizer_)
                    {
                        lock (cache_.mutex_)
                        {
                            cache_.tradeRecords_ = new Dictionary<string, TradeRecord>(executionReports.Length);

                            foreach (ExecutionReport executionReport in executionReports)
                            {
                                TradeRecord tradeRecord = GetTradeRecord(executionReport);

                                cache_.tradeRecords_.Add(tradeRecord.OrderId, tradeRecord);
                            }
                        }

                        if (initFlags_ != InitFlags.All)
                        {
                            initFlags_ |= InitFlags.TradeRecords;

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
                            reloadFlags_ |= ReloadFlags.TradeRecords;

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

        void OnOrdersError(OrderEntry.Client client, object data, string message)
        {
            try
            {
                if (data == this)
                {
                    tradeCaptureClient_.DisconnectAsync(this, "Client disconnect");
                    orderEntryClient_.DisconnectAsync(this, "Client disconnect");
                }
            }
            catch
            {
            }
        }

        void OnLogoutResult(OrderEntry.Client client, object data, LogoutInfo logoutInfo)
        {
            try
            {
                lock (synchronizer_)
                {
                    if (!logout_)
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

        void OnLogout(OrderEntry.Client client, LogoutInfo logoutInfo)
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

        void OnNewOrderResult(OrderEntry.Client client, object data, ExecutionReport report)
        {
            try
            {
                lock (cache_.mutex_)
                {
                    UpdateCacheData(report);
                }

                ExecutionReportEventArgs args = new ExecutionReportEventArgs();
                args.Report = report;
                eventQueue_.PushEvent(args);
            }
            catch
            {
            }
        }

        void OnNewOrderError(OrderEntry.Client client, object data, ExecutionReport report)
        {
            try
            {
                lock (cache_.mutex_)
                {
                    UpdateCacheData(report);
                }

                ExecutionReportEventArgs args = new ExecutionReportEventArgs();
                args.Report = report;
                eventQueue_.PushEvent(args);
            }
            catch
            {
            }
        }

        void OnReplaceOrderResult(OrderEntry.Client client, object data, ExecutionReport report)
        {
            try
            {
                lock (cache_.mutex_)
                {
                    UpdateCacheData(report);
                }

                ExecutionReportEventArgs args = new ExecutionReportEventArgs();
                args.Report = report;
                eventQueue_.PushEvent(args);
            }
            catch
            {
            }
        }

        void OnReplaceOrderError(OrderEntry.Client client, object data, ExecutionReport report)
        {
            try
            {
                lock (cache_.mutex_)
                {
                    UpdateCacheData(report);
                }

                ExecutionReportEventArgs args = new ExecutionReportEventArgs();
                args.Report = report;
                eventQueue_.PushEvent(args);
            }
            catch
            {
            }
        }

        void OnCancelOrderResult(OrderEntry.Client client, object data, ExecutionReport report)
        {
            try
            {
                lock (cache_.mutex_)
                {
                    UpdateCacheData(report);
                }

                ExecutionReportEventArgs args = new ExecutionReportEventArgs();
                args.Report = report;
                eventQueue_.PushEvent(args);
            }
            catch
            {
            }
        }

        void OnCancelOrderError(OrderEntry.Client client, object data, ExecutionReport report)
        {
            try
            {
                lock (cache_.mutex_)
                {
                    UpdateCacheData(report);
                }

                ExecutionReportEventArgs args = new ExecutionReportEventArgs();
                args.Report = report;
                eventQueue_.PushEvent(args);
            }
            catch
            {
            }
        }

        void OnClosePositionResult(OrderEntry.Client client, object data, ExecutionReport report)
        {
            try
            {
                lock (cache_.mutex_)
                {
                    UpdateCacheData(report);
                }

                ExecutionReportEventArgs args = new ExecutionReportEventArgs();
                args.Report = report;
                eventQueue_.PushEvent(args);
            }
            catch
            {
            }
        }

        void OnClosePositionError(OrderEntry.Client client, object data, ExecutionReport report)
        {
            try
            {
                lock (cache_.mutex_)
                {
                    UpdateCacheData(report);
                }

                ExecutionReportEventArgs args = new ExecutionReportEventArgs();
                args.Report = report;
                eventQueue_.PushEvent(args);
            }
            catch
            {
            }
        }

        void OnClosePositionByResult(OrderEntry.Client client, object data, ExecutionReport report)
        {
            try
            {
                lock (cache_.mutex_)
                {
                    UpdateCacheData(report);
                }

                ExecutionReportEventArgs args = new ExecutionReportEventArgs();
                args.Report = report;
                eventQueue_.PushEvent(args);
            }
            catch
            {
            }
        }

        void OnClosePositionByError(OrderEntry.Client client, object data, ExecutionReport report)
        {
            try
            {
                lock (cache_.mutex_)
                {
                    UpdateCacheData(report);
                }

                ExecutionReportEventArgs args = new ExecutionReportEventArgs();
                args.Report = report;
                eventQueue_.PushEvent(args);
            }
            catch
            {
            }
        }

        void OnSessionInfoUpdate(OrderEntry.Client client, SessionInfo info)
        {
            try
            {
                lock (cache_.mutex_)
                {
                    cache_.sessionInfo_ = info;
                }

                SessionInfoEventArgs args = new SessionInfoEventArgs();
                args.Information = info;
                eventQueue_.PushEvent(args);
            }
            catch
            {
            }
        }

        void OnAccountInfoUpdate(OrderEntry.Client client, AccountInfo info)
        {
            try
            {
                lock (cache_.mutex_)
                {
                    cache_.accountInfo_ = info;
                }

                AccountInfoEventArgs args = new AccountInfoEventArgs();
                args.Information = info;
                eventQueue_.PushEvent(args);
            }
            catch
            {
            }
        }

        void OnOrderUpdate(OrderEntry.Client client, ExecutionReport executionReport)
        {
            try
            {
                lock (cache_.mutex_)
                {
                    UpdateCacheData(executionReport);
                }
                
                ExecutionReportEventArgs args = new ExecutionReportEventArgs();
                args.Report = executionReport;
                eventQueue_.PushEvent(args);
            }
            catch
            {
            }
        }

        void OnPositionUpdate(OrderEntry.Client client, Position position)
        {
            try
            {
                lock (cache_.mutex_)
                {
                    if (cache_.positions_ != null)
                    {
                        cache_.positions_[position.Symbol] = position;
                    }
                }

                PositionReportEventArgs args = new PositionReportEventArgs();
                args.Report = position;
                eventQueue_.PushEvent(args);
            }
            catch
            {
            }
        }

        void OnBalanceUpdate(OrderEntry.Client client, BalanceOperation balanceOperation)
        {
            try
            {
                lock (cache_.mutex_)
                {
                    UpdateCacheData(balanceOperation);
                }

                NotificationEventArgs<BalanceOperation> args = new NotificationEventArgs<Common.BalanceOperation>();
                args.Type = NotificationType.Balance;
                args.Severity = NotificationSeverity.Information;
                args.Data = balanceOperation;
                eventQueue_.PushEvent(args);
            }
            catch
            {
            }
        }

        void OnNotification(OrderEntry.Client client, Notification notification)
        {
            try
            {
                if (notification.Type == NotificationType.ConfigUpdated)
                {
                    lock (synchronizer_)
                    {
                        // reload everything that might have changed

                        reloadFlags_ &= ~(ReloadFlags.TradeServerInfo | ReloadFlags.AccountInfo | ReloadFlags.SessionInfo | ReloadFlags.TradeRecords | ReloadFlags.Positions);

                        try
                        {
                            orderEntryClient_.GetTradeServerInfoAsync(this);                
                            orderEntryClient_.GetAccountInfoAsync(this);
                            orderEntryClient_.GetSessionInfoAsync(this);
                            orderEntryClient_.GetOrdersAsync(this);
                        }
                        catch
                        {
                            tradeCaptureClient_.DisconnectAsync(this, "Client disconnect");
                            orderEntryClient_.DisconnectAsync(this, "Client disconnect");
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

        void OnConnect(TradeCapture.Client client, object data)
        {
            try
            {
                tradeCaptureClient_.LoginAsync(null, login_, password_, deviceId_, appId_, appSessionId_);
            }
            catch
            {
                tradeCaptureClient_.DisconnectAsync(this, "Client disconnect");
                orderEntryClient_.DisconnectAsync(this, "Client disconnect");
            }
        }

        void OnConnectError(TradeCapture.Client client, object data, string text)
        {
            try
            {
                lock (synchronizer_)
                {
                    orderEntryClient_.DisconnectAsync(this, "Client disconnect");

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

        void OnDisconnect(TradeCapture.Client client, object data, string text)
        {
            try
            {
                lock (synchronizer_)
                {
                    initFlags_ &= ~InitFlags.TradeCaptureLogin;

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

        void OnTwoFactorLoginRequest(TradeCapture.Client client, string text)
        {
            try
            {
                lock (synchronizer_)
                {
                    tradeCaptureTwoFactorLogin_ = true;
                }

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

        void OnTwoFactorLoginResult(TradeCapture.Client client, object data, DateTime expireTime)
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

        void OnTwoFactorLoginError(TradeCapture.Client client, object data, string text)
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

        void OnTwoFactorLoginResume(TradeCapture.Client client, object data, DateTime expireTime)
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

        void OnLoginResult(TradeCapture.Client client, object data)
        {
            try
            {
                lock (synchronizer_)
                {
                    initFlags_ |= InitFlags.TradeCaptureLogin;

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
                tradeCaptureClient_.DisconnectAsync(this, "Client disconnect");
                orderEntryClient_.DisconnectAsync(this, "Client disconnect");
            }
        }

        void OnLoginError(TradeCapture.Client client, object data, string text)
        {
            try
            {
                lock (synchronizer_)
                {
                    orderEntryClient_.DisconnectAsync(this, "Client disconnect");

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

        void OnLogoutResult(TradeCapture.Client client, object data, LogoutInfo logoutInfo)
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

        void OnLogout(TradeCapture.Client client, LogoutInfo logoutInfo)
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

        void OnTradeUpdate(TradeCapture.Client client, TradeTransactionReport tradeTransactionReport)
        {
            try
            {
                TradeTransactionReportEventArgs args = new TradeTransactionReportEventArgs();
                args.Report = tradeTransactionReport;
                eventQueue_.PushEvent(args);
            }
            catch
            {
            }
        }

        void OnNotification(TradeCapture.Client client, Notification notification)
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

        void UpdateCacheData(ExecutionReport executionReport)
        {
            if (cache_.tradeRecords_ != null)
            {
                if (executionReport.ExecutionType == ExecutionType.Rejected)
                {
                    cache_.tradeRecords_.Remove(executionReport.OrderId);
                }
                else if (executionReport.ExecutionType == ExecutionType.Trade && executionReport.OrderStatus == OrderStatus.Filled)
                {
                    cache_.tradeRecords_.Remove(executionReport.OrderId);
                }
                else if (executionReport.ExecutionType == ExecutionType.Trade && executionReport.OrderStatus == OrderStatus.Activated)
                {
                    cache_.tradeRecords_.Remove(executionReport.OrderId);
                }
                else if (executionReport.ExecutionType == ExecutionType.Canceled) 
                {
                    cache_.tradeRecords_.Remove(executionReport.OrderId);
                }
                else if (executionReport.ExecutionType == ExecutionType.Expired)
                {
                    cache_.tradeRecords_.Remove(executionReport.OrderId);
                }
                else
                {
                    TradeRecord tradeRecord = GetTradeRecord(executionReport);

                    cache_.tradeRecords_[tradeRecord.OrderId] = tradeRecord;
                }
            }

            if (cache_.accountInfo_ != null)
            {
                AssetInfo[] reportAssets = executionReport.Assets;

                for (int reportIndex = 0; reportIndex < reportAssets.Length; ++reportIndex)
                {
                    AssetInfo reportAsset = reportAssets[reportIndex];

                    AssetInfo[] cacheAssets = cache_.accountInfo_.Assets;

                    int cacheIndex;
                    for (cacheIndex = 0; cacheIndex < cacheAssets.Length; ++cacheIndex)
                    {
                        AssetInfo cacheAsset = cacheAssets[cacheIndex];

                        if (cacheAsset.Currency == reportAsset.Currency)
                        {
                            cacheAssets[cacheIndex] = reportAsset;
                            break;
                        }
                    }

                    if (cacheIndex == cacheAssets.Length)
                    {
                        AssetInfo[] assets = new AssetInfo[cacheAssets.Length + 1];
                        Array.Copy(cacheAssets, assets, cacheAssets.Length);
                        assets[cacheAssets.Length] = reportAsset;

                        cache_.accountInfo_.Assets = assets;
                    }
                }

                if (executionReport.Balance.HasValue)
                {
                    cache_.accountInfo_.Balance = executionReport.Balance.Value;
                }
            }
        }

        void UpdateCacheData(BalanceOperation balanceOperation)
        {
            if (cache_.accountInfo_ != null)
            {
                if (cache_.accountInfo_.Type == AccountType.Cash)
                {
                    AssetInfo[] cacheAssets = cache_.accountInfo_.Assets;

                    int cacheIndex;
                    for (cacheIndex = 0; cacheIndex < cacheAssets.Length; ++cacheIndex)
                    {
                        AssetInfo cacheAsset = cacheAssets[cacheIndex];

                        if (cacheAsset.Currency == balanceOperation.TransactionCurrency)
                        {
                            cacheAsset.Balance = balanceOperation.Balance;
                            cacheAsset.TradeAmount = balanceOperation.TransactionAmount;

                            break;
                        }
                    }

                    if (cacheIndex == cacheAssets.Length)
                    {
                        AssetInfo[] assets = new AssetInfo[cacheAssets.Length + 1];
                        Array.Copy(cacheAssets, assets, cacheAssets.Length);

                        AssetInfo assetInfo = new AssetInfo();
                        assetInfo.Currency = balanceOperation.TransactionCurrency;
                        assetInfo.Balance = balanceOperation.Balance;
                        assetInfo.TradeAmount = balanceOperation.TransactionAmount;

                        assets[cacheAssets.Length] = assetInfo;

                        cache_.accountInfo_.Assets = assets;
                    }
                }
                else
                {
                    cache_.accountInfo_.Balance = balanceOperation.Balance;
                }
            }
        }

        void PushLoginEvents()
        {
            LogonEventArgs args = new LogonEventArgs();
            args.ProtocolVersion = "";
            eventQueue_.PushEvent(args);

            AccountInfo accountInfo;
            SessionInfo sessionInfo;
            Position[] positions;            

            lock (cache_)
            {                
                accountInfo = cache_.accountInfo_;
                sessionInfo = cache_.sessionInfo_;

                if (cache_.positions_ != null)
                {
                    positions = new Position[cache_.positions_.Count];

                    int index = 0;
                    foreach (KeyValuePair<string, Position> item in cache_.positions_)
                        positions[index++] = item.Value;
                }
                else
                    positions = null;
            }

            // For backward comapatibility
            AccountInfoEventArgs accountInfoArgs = new AccountInfoEventArgs();
            accountInfoArgs.Information = accountInfo;
            eventQueue_.PushEvent(accountInfoArgs);

            // For backward comapatibility
            SessionInfoEventArgs sessionArgs = new SessionInfoEventArgs();
            sessionArgs.Information = sessionInfo;
            eventQueue_.PushEvent(sessionArgs);

            // For backward comapatibility
            if (positions != null)
            {                
                for (int index = 0; index < positions.Length; ++ index)
                {
                    PositionReportEventArgs positionArgs = new PositionReportEventArgs();
                    positionArgs.Report = positions[index];
                    eventQueue_.PushEvent(positionArgs);
                }
            }

            // For backward comapatibility
            CacheEventArgs cacheArgs = new CacheEventArgs();
            eventQueue_.PushEvent(cacheArgs);
        }

        void PushConfigUpdateEvents()
        {
            NotificationEventArgs args = new NotificationEventArgs();
            args.Type = NotificationType.ConfigUpdated;
            args.Severity = NotificationSeverity.Information;
            args.Text = "Data trade configuration changed";
            eventQueue_.PushEvent(args);

            AccountInfo accountInfo;
            SessionInfo sessionInfo;
            Position[] positions;            

            lock (cache_)
            {                
                accountInfo = cache_.accountInfo_;
                sessionInfo = cache_.sessionInfo_;

                if (cache_.positions_ != null)
                {
                    positions = new Position[cache_.positions_.Count];

                    int index = 0;
                    foreach (KeyValuePair<string, Position> item in cache_.positions_)
                        positions[index++] = item.Value;
                }
                else
                    positions = null;
            }

            // For backward comapatibility
            AccountInfoEventArgs accountInfoArgs = new AccountInfoEventArgs();
            accountInfoArgs.Information = accountInfo;
            eventQueue_.PushEvent(accountInfoArgs);

            // For backward comapatibility
            SessionInfoEventArgs sessionArgs = new SessionInfoEventArgs();
            sessionArgs.Information = sessionInfo;
            eventQueue_.PushEvent(sessionArgs);

            // For backward comapatibility
            if (positions != null)
            {                
                for (int index = 0; index < positions.Length; ++ index)
                {
                    PositionReportEventArgs positionArgs = new PositionReportEventArgs();
                    positionArgs.Report = positions[index];
                    eventQueue_.PushEvent(positionArgs);
                }
            }
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

            AccountInfoEventArgs accountInfoEventArgs = eventArgs as AccountInfoEventArgs;

            if (accountInfoEventArgs != null)
            {
                if (AccountInfo != null)
                {
                    try
                    {
                        AccountInfo(this, accountInfoEventArgs);
                    }
                    catch
                    {
                    }
                }

                return;
            }

            ExecutionReportEventArgs executionReportEventArgs = eventArgs as ExecutionReportEventArgs;

            if (executionReportEventArgs != null)
            {
                if (ExecutionReport != null)
                {
                    try
                    {
                        ExecutionReport(this, executionReportEventArgs);
                    }
                    catch
                    {
                    }
                }

                return;
            }

            PositionReportEventArgs positionReportEventArgs = eventArgs as PositionReportEventArgs;

            if (positionReportEventArgs != null)
            {
                if (PositionReport != null)
                {
                    try
                    {
                        PositionReport(this, positionReportEventArgs);
                    }
                    catch
                    {
                    }
                }

                return;
            }

            NotificationEventArgs<BalanceOperation> balanceOperationNotificationEventArgs = eventArgs as NotificationEventArgs<BalanceOperation>;

            if (balanceOperationNotificationEventArgs != null)
            {
                if (BalanceOperation != null)
                {
                    try
                    {
                        BalanceOperation(this, balanceOperationNotificationEventArgs);
                    }
                    catch
                    {
                    }
                }

                return;
            }

            TradeTransactionReportEventArgs tradeTransactionReportEventArgs = eventArgs as TradeTransactionReportEventArgs;

            if (tradeTransactionReportEventArgs != null)
            {
                if (TradeTransactionReport != null)
                {
                    try
                    {
                        TradeTransactionReport(this, tradeTransactionReportEventArgs);
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

        internal TradeRecord GetTradeRecord(ExecutionReport executionReport)
        {
            TradeRecord tradeRecord = new TradeRecord(this);
            tradeRecord.OrderId = executionReport.OrderId;
            tradeRecord.ClientOrderId = executionReport.ClientOrderId;
            tradeRecord.Symbol = executionReport.Symbol;
            tradeRecord.InitialVolume = executionReport.InitialVolume.GetValueOrDefault();
            tradeRecord.Volume = executionReport.LeavesVolume;
            tradeRecord.MaxVisibleVolume = executionReport.MaxVisibleVolume;
            tradeRecord.Price = executionReport.Price;
            tradeRecord.StopPrice = executionReport.StopPrice;
            tradeRecord.TakeProfit = executionReport.TakeProfit;
            tradeRecord.StopLoss = executionReport.StopLoss;
            tradeRecord.Commission = executionReport.Commission;
            tradeRecord.AgentCommission = executionReport.AgentCommission;
            tradeRecord.Swap = executionReport.Swap;
            tradeRecord.Type = executionReport.OrderType;
            tradeRecord.Side = executionReport.OrderSide;
            tradeRecord.IsReducedOpenCommission = executionReport.ReducedOpenCommission;
            tradeRecord.IsReducedCloseCommission = executionReport.ReducedCloseCommission;
            tradeRecord.ImmediateOrCancel = executionReport.OrderTimeInForce == OrderTimeInForce.ImmediateOrCancel;
            tradeRecord.MarketWithSlippage = executionReport.MarketWithSlippage;
            tradeRecord.Expiration = executionReport.Expiration;
            tradeRecord.Created = executionReport.Created;
            tradeRecord.Modified = executionReport.Modified;
            tradeRecord.Comment = executionReport.Comment;
            tradeRecord.Tag = executionReport.Tag;
            tradeRecord.Magic = executionReport.Magic;

            return tradeRecord;
        }

        internal enum InitFlags
        {
            None = 0x00,
            TradeServerInfo = 0x01,            
            AccountInfo = 0x02,
            SessionInfo = 0x04,
            TradeRecords = 0x08,
            Positions = 0x10,
            TradeCaptureLogin = 0x20,
            All = TradeServerInfo | SessionInfo | AccountInfo | TradeRecords | Positions | TradeCaptureLogin
        }

        internal enum ReloadFlags
        {
            None = 0x00,
            TradeServerInfo = 0x01,            
            AccountInfo = 0x02,
            SessionInfo = 0x04,
            TradeRecords = 0x08,
            Positions = 0x10,
            All = TradeServerInfo | SessionInfo | AccountInfo | TradeRecords | Positions
        }

        internal string name_;
        internal string address_;
        internal string login_;
        internal string password_;
        internal string deviceId_;
        internal string appId_;
        internal string appSessionId_;
        internal int synchOperationTimeout_;

        internal DataTradeServer server_;
        internal DataTradeCache cache_;
        internal Network network_;
        internal OrderEntry.Client orderEntryClient_;
        internal TradeCapture.Client tradeCaptureClient_;

        internal object synchronizer_;        
        internal bool started_;

        internal bool orderEntryTwoFactorLogin_;
        internal bool tradeCaptureTwoFactorLogin_;
        
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
