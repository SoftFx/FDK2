namespace TickTrader.FDK.Extended
{
    using System;
    using System.Threading;
    using Objects;
    using OrderEntry;

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

            int port;
            if (! connectionStringParser.TryGetIntValue("Port", out port))
                port = 5030;

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

            client_ = new Client(name_, port, true, logDirectory, decodeLogMessages);
            client_.ConnectEvent += new Client.ConnectDelegate(this.OnConnect);
            client_.ConnectErrorEvent += new Client.ConnectErrorDelegate(this.OnConnectError);
            client_.DisconnectEvent += new Client.DisconnectDelegate(this.OnDisconnect);
            client_.OneTimePasswordRequestEvent += new Client.OneTimePasswordRequestDelegate(this.OnOneTimePasswordRequest);
            client_.OneTimePasswordRejectEvent += new Client.OneTimePasswordRejectDelegate(this.OnOneTimePasswordReject);
            client_.LoginResultEvent += new Client.LoginResultDelegate(this.OnLoginResult);
            client_.LoginErrorEvent += new Client.LoginErrorDelegate(this.OnLoginError);
            client_.LogoutResultEvent += new Client.LogoutResultDelegate(this.OnLogoutResult);
            client_.LogoutEvent += new Client.LogoutDelegate(this.OnLogout);
            client_.SessionInfoUpdateEvent += new Client.SessionInfoUpdateDelegate(this.OnSessionInfoUpdate);
            client_.AccountInfoUpdateEvent += new Client.AccountInfoUpdateDelegate(this.OnAccountInfoUpdate);
            client_.ExecutionReportEvent += new Client.ExecutionReportDelegate(this.OnExecutionReport);
            client_.PositionUpdateEvent += new Client.PositionUpdateDelegate(this.OnPositionUpdate);
            client_.BalanceInfoUpdateEvent += new Client.BalanceInfoUpdateDelegate(this.OnBalanceInfoUpdate);
            client_.NotificationEvent += new Client.NotificationDelegate(this.OnNotification);

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
        ///
        /// </summary>
        public event TradeTransactionReportHandler TradeTransactionReport;

        /// <summary>
        /// The event is supported by Net account only.
        /// </summary>
        public event PositionReportHandler PositionReport;

        /// <summary>
        /// Occurs when a notification of balance operation is received.
        /// </summary>
        public event NotifyHandler<BalanceOperation> BalanceOperation;

        /// <summary>
        /// Occurs when a notification is received.
        /// </summary>
        public event NotifyHandler Notify;

        /// <summary>
        /// Occurs when local cache initialized.
        /// </summary>
        [Obsolete("No longer used")]
        public event CacheHandler CacheInitialized;

        #endregion

        #region Methods

        /// <summary>
        /// Starts data feed instance.
        /// </summary>
        public void Start()
        {
            lock (synchronizer_)
            {
                if (started_)
                    throw new Exception(string.Format("Data trade is already started : {0}", name_));                               
                
                loginEvent_.Reset();
                loginException_ = null;
                logout_ = false;

                eventQueue_.Open();

                eventThread_ = new Thread(this.EventThread);
                eventThread_.Name = name_ + " Event Thread";
                eventThread_.Start();

                try
                {
                    client_.ConnectAsync(address_);

                    started_ = true;
                }
                catch
                {
                    eventQueue_.Close();

                    eventThread_.Join();
                    eventThread_ = null;

                    throw;
                }
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
            this.Start();
            return this.WaitForLogon(timeoutInMilliseconds);
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
                        client_.LogoutAsync(null, "Client logout");
                    }
                    catch
                    {                        
                        client_.DisconnectAsync("Client disconnect");
                    }
                }
            }

            client_.Join();

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
            client_.Dispose();

            GC.SuppressFinalize(this);
        }

        #endregion

        #region Private

        void OnConnect(Client client)
        {
            try
            {
                client_.LoginAsync(null, login_, password_, deviceId_, appSessionId_);
            }
            catch
            {
                client_.DisconnectAsync("Client disconnect");
            }
        }

        void OnConnectError(Client client, string text)
        {
            try
            {
                logout_ = true;

                LogoutEventArgs args = new LogoutEventArgs();
                args.Reason = LogoutReason.NetworkError;
                args.Text = text;
                eventQueue_.PushEvent(args);                    

                loginException_ = new LogoutException(text);
                loginEvent_.Set();                
            }
            catch
            {
            }
        }

        void OnDisconnect(Client client, string text)
        {
            try
            {
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
            catch
            {
            }
        }

        void OnOneTimePasswordRequest(Client client, string text)
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

        void OnOneTimePasswordReject(Client client, string text)
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

        void OnLoginResult(Client client, object data)
        {
            try
            {                
                LogonEventArgs args = new LogonEventArgs();
                args.ProtocolVersion = "";
                eventQueue_.PushEvent(args);

                CacheEventArgs cacheArgs = new CacheEventArgs();
                eventQueue_.PushEvent(cacheArgs);

                loginException_ = null;
                loginEvent_.Set();
            }
            catch
            {
            }
        }

        void OnLoginError(Client client, object data, string text)
        {
            try
            {
                logout_ = true;

                LogoutEventArgs args = new LogoutEventArgs();
                args.Reason = LogoutReason.InvalidCredentials;
                args.Text = text;
                eventQueue_.PushEvent(args);

                loginException_ = new LogoutException(text);
                loginEvent_.Set();
            }
            catch
            {
            }
        }

        void OnLogoutResult(Client client, object data, LogoutInfo logoutInfo)
        {
            try
            {
                logout_ = true;

                LogoutEventArgs args = new LogoutEventArgs();
                args.Reason = logoutInfo.Reason;
                args.Text = logoutInfo.Message;
                eventQueue_.PushEvent(args);
            }
            catch
            {
            }
        }

        void OnLogout(Client client, LogoutInfo logoutInfo)
        {
            try
            {
                logout_ = true;

                LogoutEventArgs args = new LogoutEventArgs();
                args.Reason = logoutInfo.Reason;
                args.Text = logoutInfo.Message;
                eventQueue_.PushEvent(args);
            }
            catch
            {
            }
        }

        void OnSessionInfoUpdate(Client client, SessionInfo info)
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

        void OnAccountInfoUpdate(Client client, AccountInfo info)
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

        void OnExecutionReport(Client client, ExecutionReport executionReport)
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
                    TradeRecord tradeRecord = server_.GetTradeRecord(executionReport);

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

        void OnPositionUpdate(Client client, Position[] positions)
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

        void OnBalanceInfoUpdate(Client client, BalanceOperation balanceOperation)
        {
            try
            {
                NotificationEventArgs<BalanceOperation> args = new NotificationEventArgs<Objects.BalanceOperation>();
                args.Type = NotificationType.Balance;
                args.Severity = NotificationSeverity.Information;
                args.Data = balanceOperation;
                eventQueue_.PushEvent(args);
            }
            catch
            {
            }
        }

        void OnNotification(Client client, Notification notification)
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
        internal Client client_;

        object synchronizer_;        
        bool started_;
        
        ManualResetEvent loginEvent_;
        Exception loginException_;
        bool logout_;

        // We employ a queue to allow the client call sync functions from event handlers
        Thread eventThread_;
        EventQueue eventQueue_;

        #endregion
    }
}
