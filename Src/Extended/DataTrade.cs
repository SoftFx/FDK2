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

            orderEntryClient_ = new OrderEntry.Client(name_ + "_OrderEntry", orderEntryPort, true, logDirectory, decodeLogMessages);
            orderEntryClient_.ConnectEvent += new OrderEntry.Client.ConnectDelegate(this.OnConnect);
            orderEntryClient_.ConnectErrorEvent += new OrderEntry.Client.ConnectErrorDelegate(this.OnConnectError);
            orderEntryClient_.DisconnectEvent += new OrderEntry.Client.DisconnectDelegate(this.OnDisconnect);
            orderEntryClient_.OneTimePasswordRequestEvent += new OrderEntry.Client.OneTimePasswordRequestDelegate(this.OnOneTimePasswordRequest);
            orderEntryClient_.OneTimePasswordRejectEvent += new OrderEntry.Client.OneTimePasswordRejectDelegate(this.OnOneTimePasswordReject);
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
            orderEntryClient_.ReplaceOrderResultEvent += new OrderEntry.Client.ReplaceOrderResultDelegate(this.OnReplaceOrderResult);
            orderEntryClient_.CancelOrderResultEvent += new OrderEntry.Client.CancelOrderResultDelegate(this.OnCancelOrderResult);
            orderEntryClient_.ClosePositionResultEvent += new OrderEntry.Client.ClosePositionResultDelegate(this.OnClosePositionResult);
            orderEntryClient_.ClosePositionByResultEvent += new OrderEntry.Client.ClosePositionByResultDelegate(this.OnClosePositionByResult);
            orderEntryClient_.SessionInfoUpdateEvent += new OrderEntry.Client.SessionInfoUpdateDelegate(this.OnSessionInfoUpdate);
            orderEntryClient_.AccountInfoUpdateEvent += new OrderEntry.Client.AccountInfoUpdateDelegate(this.OnAccountInfoUpdate);
            orderEntryClient_.ExecutionReportEvent += new OrderEntry.Client.ExecutionReportDelegate(this.OnExecutionReport);
            orderEntryClient_.PositionUpdateEvent += new OrderEntry.Client.PositionUpdateDelegate(this.OnPositionUpdate);
            orderEntryClient_.BalanceInfoUpdateEvent += new OrderEntry.Client.BalanceInfoUpdateDelegate(this.OnBalanceInfoUpdate);
            orderEntryClient_.NotificationEvent += new OrderEntry.Client.NotificationDelegate(this.OnNotification);

            tradeCaptureClient_ = new TradeCapture.Client(name_ + "_TradeCapture", tradeCapturePort, true, logDirectory, decodeLogMessages);
            tradeCaptureClient_.ConnectEvent += new TradeCapture.Client.ConnectDelegate(this.OnConnect);
            tradeCaptureClient_.ConnectErrorEvent += new TradeCapture.Client.ConnectErrorDelegate(this.OnConnectError);
            tradeCaptureClient_.DisconnectEvent += new TradeCapture.Client.DisconnectDelegate(this.OnDisconnect);
            tradeCaptureClient_.OneTimePasswordRequestEvent += new TradeCapture.Client.OneTimePasswordRequestDelegate(this.OnOneTimePasswordRequest);
            tradeCaptureClient_.OneTimePasswordRejectEvent += new TradeCapture.Client.OneTimePasswordRejectDelegate(this.OnOneTimePasswordReject);
            tradeCaptureClient_.LoginResultEvent += new TradeCapture.Client.LoginResultDelegate(this.OnLoginResult);
            tradeCaptureClient_.LoginErrorEvent += new TradeCapture.Client.LoginErrorDelegate(this.OnLoginError);
            tradeCaptureClient_.LogoutResultEvent += new TradeCapture.Client.LogoutResultDelegate(this.OnLogoutResult);
            tradeCaptureClient_.LogoutEvent += new TradeCapture.Client.LogoutDelegate(this.OnLogout);
            tradeCaptureClient_.TradeUpdateEvent += new TradeCapture.Client.TradeUpdateDelegate(this.OnTradeUpdate);

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
                        orderEntryClient_.ConnectAsync(address_);

                        try
                        {
                            tradeCaptureClient_.ConnectAsync(address_);

                            started_ = true;
                        }
                        catch
                        {
                            orderEntryClient_.DisconnectAsync("Client disconnect");
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
                        tradeCaptureClient_.LogoutAsync(null, "Client logout");
                    }
                    catch
                    {                        
                        tradeCaptureClient_.DisconnectAsync("Client disconnect");
                    }

                    try
                    {
                        orderEntryClient_.LogoutAsync(null, "Client logout");
                    }
                    catch
                    {                        
                        orderEntryClient_.DisconnectAsync("Client disconnect");
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

        void OnConnect(OrderEntry.Client client)
        {
            try
            {
                orderEntryClient_.LoginAsync(null, login_, password_, deviceId_, appSessionId_);
            }
            catch
            {
                tradeCaptureClient_.DisconnectAsync("Client disconnect");
                orderEntryClient_.DisconnectAsync("Client disconnect");
            }
        }

        void OnConnectError(OrderEntry.Client client, string text)
        {
            try
            {
                lock (synchronizer_)
                {
                    tradeCaptureClient_.DisconnectAsync("Client disconnect");

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

        void OnDisconnect(OrderEntry.Client client, string text)
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

        void OnOneTimePasswordRequest(OrderEntry.Client client, string text)
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

        void OnOneTimePasswordReject(OrderEntry.Client client, string text)
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

        void OnLoginResult(OrderEntry.Client client, object data)
        {
            try
            {
                orderEntryClient_.GetTradeServerInfoAsync(this);
                orderEntryClient_.GetSessionInfoAsync(this);
                orderEntryClient_.GetAccountInfoAsync(this);
                orderEntryClient_.GetOrdersAsync(this);
            }
            catch
            {
                tradeCaptureClient_.DisconnectAsync("Client disconnect");
                orderEntryClient_.DisconnectAsync("Client disconnect");
            }
        }

        void OnLoginError(OrderEntry.Client client, object data, string text)
        {
            try
            {
                lock (synchronizer_)
                {
                    tradeCaptureClient_.DisconnectAsync("Client disconnect");

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
                            if (cache_.tradeServerInfo_ == null)
                                cache_.tradeServerInfo_ = tradeServerInfo;
                        }

                        initFlags_ |= InitFlags.TradeServerInfo;

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

        void OnTradeServerInfoError(OrderEntry.Client client, object data, string message)
        {
            try
            {
                if (data == this)
                {
                    tradeCaptureClient_.DisconnectAsync("Client disconnect");
                    orderEntryClient_.DisconnectAsync("Client disconnect");
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

        void OnSessionInfoError(OrderEntry.Client client, object data, string message)
        {
            try
            {
                if (data == this)
                {
                    tradeCaptureClient_.DisconnectAsync("Client disconnect");
                    orderEntryClient_.DisconnectAsync("Client disconnect");
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
                            if (cache_.accountInfo_ == null)
                                cache_.accountInfo_ = accountInfo;
                        }

                        initFlags_ |= InitFlags.AccountInfo;

                        if (accountInfo.Type == AccountType.Net)
                        {
                            orderEntryClient_.GetPositionsAsync(this);
                        }
                        else
                        {
                            lock (cache_.mutex_)
                            {
                                if (cache_.positions_ == null)
                                    cache_.positions_ = new Dictionary<string, Position>();
                            }

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
                    tradeCaptureClient_.DisconnectAsync("Client disconnect");
                    orderEntryClient_.DisconnectAsync("Client disconnect");
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
                            if (cache_.positions_ == null)
                            {
                                cache_.positions_ = new Dictionary<string, Position>(positions.Length);

                                foreach (Position position in positions)
                                    cache_.positions_.Add(position.Symbol, position);
                            }
                        }

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
                    tradeCaptureClient_.DisconnectAsync("Client disconnect");
                    orderEntryClient_.DisconnectAsync("Client disconnect");
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
                            if (cache_.tradeRecords_ == null)
                            {
                                cache_.tradeRecords_ = new Dictionary<string, TradeRecord>(executionReports.Length);

                                foreach (ExecutionReport executionReport in executionReports)
                                {
                                    TradeRecord tradeRecord = GetTradeRecord(executionReport);

                                    cache_.tradeRecords_.Add(tradeRecord.OrderId, tradeRecord);
                                }
                            }
                        }

                        initFlags_ |= InitFlags.TradeRecords;

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

        void OnOrdersError(OrderEntry.Client client, object data, string message)
        {
            try
            {
                if (data == this)
                {
                    tradeCaptureClient_.DisconnectAsync("Client disconnect");
                    orderEntryClient_.DisconnectAsync("Client disconnect");
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
                if (report.ExecutionType == ExecutionType.Trade && report.OrderStatus == OrderStatus.Filled)
                {
                    lock (cache_.mutex_)
                    {
                        if (cache_.tradeRecords_ != null)
                            cache_.tradeRecords_.Remove(report.OrderId);
                    }
                }
                else
                {
                    TradeRecord tradeRecord = GetTradeRecord(report);

                    lock (cache_.mutex_)
                    {
                        if (cache_.tradeRecords_ != null)
                            cache_.tradeRecords_[tradeRecord.OrderId] = tradeRecord;
                    }
                }
            }
            catch
            {
            }
        }

        void OnReplaceOrderResult(OrderEntry.Client client, object data, ExecutionReport report)
        {
            try
            {
                TradeRecord tradeRecord = GetTradeRecord(report);

                lock (cache_.mutex_)
                {
                    if (cache_.tradeRecords_ != null)
                        cache_.tradeRecords_[tradeRecord.OrderId] = tradeRecord;
                }
            }
            catch
            {
            }
        }

        void OnCancelOrderResult(OrderEntry.Client client, object data, ExecutionReport report)
        {
            try
            {
                if (report.ExecutionType == ExecutionType.Canceled)
                {
                    lock (cache_.mutex_)
                    {
                        if (cache_.tradeRecords_ != null)
                            cache_.tradeRecords_.Remove(report.OrderId);
                    }
                }
                else
                {
                    TradeRecord tradeRecord = GetTradeRecord(report);

                    lock (cache_.mutex_)
                    {
                        if (cache_.tradeRecords_ != null)
                            cache_.tradeRecords_[tradeRecord.OrderId] = tradeRecord;
                    }
                }
            }
            catch
            {
            }
        }

        void OnClosePositionResult(OrderEntry.Client client, object data, ExecutionReport report)
        {
            try
            {
                if (report.ExecutionType == ExecutionType.Trade && report.OrderStatus == OrderStatus.Filled)
                {
                    lock (cache_.mutex_)
                    {
                        if (cache_.tradeRecords_ != null)
                            cache_.tradeRecords_.Remove(report.OrderId);
                    }
                }
                else
                {
                    TradeRecord tradeRecord = GetTradeRecord(report);

                    lock (cache_.mutex_)
                    {
                        if (cache_.tradeRecords_ != null)
                            cache_.tradeRecords_[tradeRecord.OrderId] = tradeRecord;
                    }
                }
            }
            catch
            {
            }
        }

        void OnClosePositionByResult(OrderEntry.Client client, object data, ExecutionReport report)
        {
            try
            {
                if (report.ExecutionType == ExecutionType.Trade && report.OrderStatus == OrderStatus.Filled)
                {
                    lock (cache_.mutex_)
                    {
                        if (cache_.tradeRecords_ != null)
                            cache_.tradeRecords_.Remove(report.OrderId);
                    }
                }
                else
                {
                    TradeRecord tradeRecord = GetTradeRecord(report);

                    lock (cache_.mutex_)
                    {
                        if (cache_.tradeRecords_ != null)
                            cache_.tradeRecords_[tradeRecord.OrderId] = tradeRecord;
                    }
                }
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

        void OnExecutionReport(OrderEntry.Client client, ExecutionReport executionReport)
        {
            try
            {
                if (executionReport.ExecutionType == ExecutionType.Trade && executionReport.OrderStatus == OrderStatus.Filled)
                {
                    lock (cache_.mutex_)
                    {
                        if (cache_.tradeRecords_ != null)
                            cache_.tradeRecords_.Remove(executionReport.OrderId);
                    }
                }
                else
                {
                    TradeRecord tradeRecord = GetTradeRecord(executionReport);

                    lock (cache_.mutex_)
                    {
                        if (cache_.tradeRecords_ != null)
                            cache_.tradeRecords_[tradeRecord.OrderId] = tradeRecord;
                    }
                }

                ExecutionReportEventArgs args = new ExecutionReportEventArgs();
                args.Report = executionReport;
                eventQueue_.PushEvent(args);
            }
            catch
            {
            }
        }

        void OnPositionUpdate(OrderEntry.Client client, Position[] positions)
        {
            try
            {
                lock (cache_.mutex_)
                {
                    if (cache_.positions_ != null)
                    {
                        for (int index = 0; index < positions.Length; ++index)
                        {
                            Position position = positions[index];

                            cache_.positions_[position.Symbol] = position;
                        }
                    }
                }

                for (int index = 0; index < positions.Length; ++index)
                {
                    Position position = positions[index];

                    PositionReportEventArgs args = new PositionReportEventArgs();
                    args.Report = positions[index];
                    eventQueue_.PushEvent(args);
                }
            }
            catch
            {
            }
        }

        void OnBalanceInfoUpdate(OrderEntry.Client client, BalanceOperation balanceOperation)
        {
            try
            {
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

        void OnConnect(TradeCapture.Client client)
        {
            try
            {
                tradeCaptureClient_.LoginAsync(null, login_, password_, deviceId_, appSessionId_);
            }
            catch
            {
                tradeCaptureClient_.DisconnectAsync("Client disconnect");
                orderEntryClient_.DisconnectAsync("Client disconnect");
            }
        }

        void OnConnectError(TradeCapture.Client client, string text)
        {
            try
            {
                lock (synchronizer_)
                {
                    orderEntryClient_.DisconnectAsync("Client disconnect");

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

        void OnDisconnect(TradeCapture.Client client, string text)
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

        void OnOneTimePasswordRequest(TradeCapture.Client client, string text)
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

        void OnOneTimePasswordReject(TradeCapture.Client client, string text)
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
                tradeCaptureClient_.DisconnectAsync("Client disconnect");
                orderEntryClient_.DisconnectAsync("Client disconnect");
            }
        }

        void OnLoginError(TradeCapture.Client client, object data, string text)
        {
            try
            {
                lock (synchronizer_)
                {
                    orderEntryClient_.DisconnectAsync("Client disconnect");

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

        void PushLoginEvents()
        {
            LogonEventArgs args = new LogonEventArgs();
            args.ProtocolVersion = "";
            eventQueue_.PushEvent(args);

            AccountInfo accountInfo;

            lock (cache_)
            {
                accountInfo = cache_.accountInfo_;
            }

            // For backward comapatibility
            AccountInfoEventArgs accountInfoArgs = new AccountInfoEventArgs();
            accountInfoArgs.Information = accountInfo;
            eventQueue_.PushEvent(accountInfoArgs);

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

            if (executionReport.OrderTimeInForce == OrderTimeInForce.ImmediateOrCancel)
            {
                tradeRecord.Type = TradeRecordType.IoC;
                tradeRecord.ImmediateOrCancel = true;
            }
            else
            {
                tradeRecord.Type = GetTradeRecordType(executionReport.OrderType);
                tradeRecord.ImmediateOrCancel = false;
            }

            tradeRecord.Side = GetTradeRecordSide(executionReport.OrderSide);
            tradeRecord.IsReducedOpenCommission = executionReport.ReducedOpenCommission;
            tradeRecord.IsReducedCloseCommission = executionReport.ReducedCloseCommission;

            if (executionReport.OrderType == OrderType.MarketWithSlippage)
            {
                tradeRecord.MarketWithSlippage = true;
            }
            else
                tradeRecord.MarketWithSlippage = false;

            tradeRecord.Expiration = executionReport.Expiration;
            tradeRecord.Created = executionReport.Created;
            tradeRecord.Modified = executionReport.Modified;
            tradeRecord.Comment = executionReport.Comment;
            tradeRecord.Tag = executionReport.Tag;
            tradeRecord.Magic = executionReport.Magic;

            return tradeRecord;
        }

        TradeRecordType GetTradeRecordType(OrderType orderType)
        {
            switch (orderType)
            {
                case OrderType.Market:
                    return TradeRecordType.Market;

                case OrderType.MarketWithSlippage:
                    return TradeRecordType.MarketWithSlippage;

                case OrderType.Position:
                    return TradeRecordType.Position;

                case OrderType.Limit:
                    return TradeRecordType.Limit;

                case OrderType.Stop:
                    return TradeRecordType.Stop;

                case OrderType.StopLimit:
                    return TradeRecordType.StopLimit;

                default:
                    throw new Exception("Invalid order type : " + orderType);
            }
        }


        TradeRecordSide GetTradeRecordSide(OrderSide orderSide)
        {
            switch (orderSide)
            {
                case OrderSide.Buy:
                    return TradeRecordSide.Buy;

                case OrderSide.Sell:
                    return TradeRecordSide.Sell;

                default:
                    throw new Exception("Invalid order side : " + orderSide);
            }
        }

        enum InitFlags
        {
            None = 0x00,
            TradeServerInfo = 0x01,
            SessionInfo = 0x02,
            AccountInfo = 0x04,
            TradeRecords = 0x08,
            Positions = 0x10,
            TradeCaptureLogin = 0x20,
            All = TradeServerInfo | SessionInfo | AccountInfo | TradeRecords | Positions | TradeCaptureLogin
        }

        string name_;
        string address_;
        string login_;
        string password_;
        string deviceId_;
        string appSessionId_;
        internal int synchOperationTimeout_;

        internal DataTradeServer server_;
        internal DataTradeCache cache_;
        internal Network network_;
        internal OrderEntry.Client orderEntryClient_;
        internal TradeCapture.Client tradeCaptureClient_;

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
