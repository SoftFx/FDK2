namespace TickTrader.FDK.Extended
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Common;
    using Client;
    using System.Net;

    /// <summary>
    /// This class connects to trading platform and provides trading functionality.
    /// </summary>
    public class DataTrade
    {
        #region Construction

        /// <summary>
        /// Creates a new data trade instance.
        /// </summary>
        public DataTrade() : this(null)
        {
        }

        /// <summary>
        /// Creates a new data trade instance.
        /// </summary>
        public DataTrade(ClientCertificateValidation validateClientCertificate) : this(null, "DataTrade", validateClientCertificate)
        {
        }

        /// <summary>
        /// Creates and initializes a new data trade instance.
        /// </summary>
        /// <exception cref="System.ArgumentNullException">If connectionString is null.</exception>
        public DataTrade(string connectionString, ClientCertificateValidation validateClientCertificate) : this(connectionString, "DataTrade", validateClientCertificate)
        {
        }

        /// <summary>
        /// Creates and initializes a new data trade instance.
        /// </summary>
        /// <exception cref="System.ArgumentNullException">If connectionString is null.</exception>
        public DataTrade(string connectionString, bool validateClientCertificate = true) : this(connectionString, (sender, certificate, chain, errors, port) => validateClientCertificate)
        {
        }

        /// <summary>
        /// Creates and initializes a new data trade instance.
        /// </summary>
        /// <exception cref="System.ArgumentNullException">If connectionString is null.</exception>
        public DataTrade(string connectionString, string name, ClientCertificateValidation validateClientCertificate)
        {
            name_ = name;
            server_ = new DataTradeServer(this);
            cache_ = new DataTradeCache(this);
            network_ = new DataTradeNetwork(this);

            if (!string.IsNullOrEmpty(connectionString))
                Initialize(connectionString, validateClientCertificate);
        }

        /// <summary>
        /// Initializes the data feed instance; it must be stopped.
        /// </summary>
        /// <param name="connectionString">Can not be null.</param>
        /// <exception cref="System.ArgumentNullException">If connectionString is null.</exception>
        /// <exception cref="System.InvalidOperationException">If the instance is not stopped.</exception>

        public void Initialize(string connectionString)
        {
            this.Initialize(connectionString, (sender, certificate, chain, errors, port) => true);
        }
        /// <summary>
        /// Initializes the data feed instance; it must be stopped.
        /// </summary>
        /// <param name="connectionString">Can not be null.</param>
        /// <exception cref="System.ArgumentNullException">If connectionString is null.</exception>
        /// <exception cref="System.InvalidOperationException">If the instance is not stopped.</exception>
        public void Initialize(string connectionString, ClientCertificateValidation validateClientCertificate)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException(nameof(connectionString), "Connection string can not be null or empty.");

            ConnectionStringParser connectionStringParser = new ConnectionStringParser();

            connectionStringParser.Parse(connectionString);

            if (! connectionStringParser.TryGetStringValue("Address", out address_))
                throw new Exception("Address is not specified");

            int orderEntryPort;
            if (! connectionStringParser.TryGetIntValue("OrderEntryPort", out orderEntryPort))
                orderEntryPort = 5043;

            int tradeCapturePort;
            if (! connectionStringParser.TryGetIntValue("TradeCapturePort", out tradeCapturePort))
                tradeCapturePort = 5044;

            string serverCertificateName;
            if (! connectionStringParser.TryGetStringValue("ServerCertificateName", out serverCertificateName))
                serverCertificateName = "CN=*.soft-fx.com";

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

            bool logEvents;
            if (! connectionStringParser.TryGetBoolValue("LogEvents", out logEvents))
                logEvents = false;

            bool logStates;
            if (! connectionStringParser.TryGetBoolValue("LogStates", out logStates))
                logStates = false;

            bool logMessages;
            if (! connectionStringParser.TryGetBoolValue("LogMessages", out logMessages))
                logMessages = false;

            int proxyTypeFDKnumber;
            if (!connectionStringParser.TryGetIntValue("ProxyType", out proxyTypeFDKnumber))
                proxyTypeFDKnumber = (int)ProxyType.None;
            ProxyType proxyTypeFDK = (ProxyType)proxyTypeFDKnumber;

            string proxyAddressString;
            if (!connectionStringParser.TryGetStringValue("ProxyAddress", out proxyAddressString))
                proxyAddressString = null;
            IPAddress proxyAddress = null;
            IPAddress.TryParse(proxyAddressString, out proxyAddress);

            int proxyPort;
            if (!connectionStringParser.TryGetIntValue("ProxyPort", out proxyPort))
                proxyPort = 0;

            string proxyUsername;
            if (!connectionStringParser.TryGetStringValue("ProxyUsername", out proxyUsername))
                proxyUsername = null;

            string proxyPassword;
            if (!connectionStringParser.TryGetStringValue("ProxyPassword", out proxyPassword))
                proxyPassword = null;

            synchronizer_ = new object();

            SoftFX.Net.Core.ClientCertificateValidation orderEntryClientCertificateValidation;
            SoftFX.Net.Core.ClientCertificateValidation tradeCaptureClientCertificateValidation;
            if (validateClientCertificate == null)
            {
                orderEntryClientCertificateValidation = null;
                tradeCaptureClientCertificateValidation = null;
            }
            else
            {
                orderEntryClientCertificateValidation = (sender, cert, chain, ssl) => validateClientCertificate(sender, cert, chain, ssl, orderEntryPort);
                tradeCaptureClientCertificateValidation = (sender, cert, chain, ssl) => validateClientCertificate(sender, cert, chain, ssl, tradeCapturePort);
            }

            orderEntryClient_ = new OrderEntry(name_ + ".OrderEntry", logEvents, logStates, logMessages, orderEntryPort, serverCertificateName, 1, -1, 10000, 10000, logDirectory,
                orderEntryClientCertificateValidation, (SoftFX.Net.Core.ProxyType)proxyTypeFDK, proxyAddress, proxyPort, proxyUsername, proxyPassword);
            orderEntryClient_.ConnectResultEvent += new OrderEntry.ConnectResultDelegate(this.OnConnectResult);
            orderEntryClient_.ConnectErrorEvent += new OrderEntry.ConnectErrorDelegate(this.OnConnectError);
            orderEntryClient_.DisconnectResultEvent += new OrderEntry.DisconnectResultDelegate(this.OnDisconnectResult);
            orderEntryClient_.DisconnectEvent += new OrderEntry.DisconnectDelegate(this.OnDisconnect);
            orderEntryClient_.ReconnectEvent += new OrderEntry.ReconnectDelegate(this.OnReconnect);
            orderEntryClient_.ReconnectErrorEvent += new OrderEntry.ReconnectErrorDelegate(this.OnReconnectError);
            orderEntryClient_.TwoFactorLoginRequestEvent += new OrderEntry.TwoFactorLoginRequestDelegate(this.OnTwoFactorLoginRequest);
            orderEntryClient_.TwoFactorLoginResultEvent += new OrderEntry.TwoFactorLoginResultDelegate(this.OnTwoFactorLoginResult);
            orderEntryClient_.TwoFactorLoginErrorEvent += new OrderEntry.TwoFactorLoginErrorDelegate(this.OnTwoFactorLoginError);
            orderEntryClient_.TwoFactorLoginResumeEvent += new OrderEntry.TwoFactorLoginResumeDelegate(this.OnTwoFactorLoginResume);
            orderEntryClient_.LoginResultEvent += new OrderEntry.LoginResultDelegate(this.OnLoginResult);
            orderEntryClient_.LoginErrorEvent += new OrderEntry.LoginErrorDelegate(this.OnLoginError);
            orderEntryClient_.LogoutResultEvent += new OrderEntry.LogoutResultDelegate(this.OnLogoutResult);
            orderEntryClient_.LogoutEvent += new OrderEntry.LogoutDelegate(this.OnLogout);
            orderEntryClient_.TradeServerInfoResultEvent += new OrderEntry.TradeServerInfoResultDelegate(this.OnTradeServerInfoResult);
            orderEntryClient_.TradeServerInfoErrorEvent += new OrderEntry.TradeServerInfoErrorDelegate(this.OnTradeServerInfoError);
            orderEntryClient_.SessionInfoResultEvent += new OrderEntry.SessionInfoResultDelegate(this.OnSessionInfoResult);
            orderEntryClient_.SessionInfoErrorEvent += new OrderEntry.SessionInfoErrorDelegate(this.OnSessionInfoError);
            orderEntryClient_.AccountInfoResultEvent += new OrderEntry.AccountInfoResultDelegate(this.OnAccountInfoResult);
            orderEntryClient_.AccountInfoErrorEvent += new OrderEntry.AccountInfoErrorDelegate(this.OnAccountInfoError);
            orderEntryClient_.PositionsResultEvent += new OrderEntry.PositionsResultDelegate(this.OnPositionsResult);
            orderEntryClient_.PositionsErrorEvent += new OrderEntry.PositionsErrorDelegate(this.OnPositionsError);
            orderEntryClient_.OrdersBeginResultEvent += new OrderEntry.OrdersBeginResultDelegate(this.OnOrdersBeginResult);
            orderEntryClient_.OrdersResultEvent += new OrderEntry.OrdersResultDelegate(this.OnOrdersResult);
            orderEntryClient_.OrdersEndResultEvent += new OrderEntry.OrdersEndResultDelegate(this.OnOrdersEndResult);
            orderEntryClient_.OrdersErrorEvent += new OrderEntry.OrdersErrorDelegate(this.OnOrdersError);
            orderEntryClient_.NewOrderResultEvent += new OrderEntry.NewOrderResultDelegate(this.OnNewOrderResult);
            orderEntryClient_.NewOrderErrorEvent += new OrderEntry.NewOrderErrorDelegate(this.OnNewOrderError);
            orderEntryClient_.ReplaceOrderResultEvent += new OrderEntry.ReplaceOrderResultDelegate(this.OnReplaceOrderResult);
            orderEntryClient_.ReplaceOrderErrorEvent += new OrderEntry.ReplaceOrderErrorDelegate(this.OnReplaceOrderError);
            orderEntryClient_.CancelOrderResultEvent += new OrderEntry.CancelOrderResultDelegate(this.OnCancelOrderResult);
            orderEntryClient_.CancelOrderErrorEvent += new OrderEntry.CancelOrderErrorDelegate(this.OnCancelOrderError);
            orderEntryClient_.ClosePositionResultEvent += new OrderEntry.ClosePositionResultDelegate(this.OnClosePositionResult);
            orderEntryClient_.ClosePositionErrorEvent += new OrderEntry.ClosePositionErrorDelegate(this.OnClosePositionError);
            orderEntryClient_.ClosePositionByResultEvent += new OrderEntry.ClosePositionByResultDelegate(this.OnClosePositionByResult);
            orderEntryClient_.ClosePositionByErrorEvent += new OrderEntry.ClosePositionByErrorDelegate(this.OnClosePositionByError);
            orderEntryClient_.SessionInfoUpdateEvent += new OrderEntry.SessionInfoUpdateDelegate(this.OnSessionInfoUpdate);
            orderEntryClient_.AccountInfoUpdateEvent += new OrderEntry.AccountInfoUpdateDelegate(this.OnAccountInfoUpdate);
            orderEntryClient_.OrderUpdateEvent += new OrderEntry.OrderUpdateDelegate(this.OnOrderUpdate);
            orderEntryClient_.PositionUpdateEvent += new OrderEntry.PositionUpdateDelegate(this.OnPositionUpdate);
            orderEntryClient_.BalanceUpdateEvent += new OrderEntry.BalanceUpdateDelegate(this.OnBalanceUpdate);
            orderEntryClient_.NotificationEvent += new OrderEntry.NotificationDelegate(this.OnNotification);

            tradeCaptureClient_ = new TradeCapture(name_ + ".TradeCapture", logEvents, logStates, logMessages, tradeCapturePort, serverCertificateName, 1, -1, 10000, 10000, logDirectory,
                tradeCaptureClientCertificateValidation, (SoftFX.Net.Core.ProxyType)proxyTypeFDK, proxyAddress, proxyPort, proxyUsername, proxyPassword);
            tradeCaptureClient_.ConnectResultEvent += new TradeCapture.ConnectResultDelegate(this.OnConnectResult);
            tradeCaptureClient_.ConnectErrorEvent += new TradeCapture.ConnectErrorDelegate(this.OnConnectError);
            tradeCaptureClient_.DisconnectResultEvent += new TradeCapture.DisconnectResultDelegate(this.OnDisconnectResult);
            tradeCaptureClient_.DisconnectEvent += new TradeCapture.DisconnectDelegate(this.OnDisconnect);
            tradeCaptureClient_.ReconnectEvent += new TradeCapture.ReconnectDelegate(this.OnReconnect);
            tradeCaptureClient_.ReconnectErrorEvent += new TradeCapture.ReconnectErrorDelegate(this.OnReconnectError);
            tradeCaptureClient_.TwoFactorLoginRequestEvent += new TradeCapture.TwoFactorLoginRequestDelegate(this.OnTwoFactorLoginRequest);
            tradeCaptureClient_.TwoFactorLoginResultEvent += new TradeCapture.TwoFactorLoginResultDelegate(this.OnTwoFactorLoginResult);
            tradeCaptureClient_.TwoFactorLoginErrorEvent += new TradeCapture.TwoFactorLoginErrorDelegate(this.OnTwoFactorLoginError);
            tradeCaptureClient_.TwoFactorLoginResumeEvent += new TradeCapture.TwoFactorLoginResumeDelegate(this.OnTwoFactorLoginResume);
            tradeCaptureClient_.LoginResultEvent += new TradeCapture.LoginResultDelegate(this.OnLoginResult);
            tradeCaptureClient_.LoginErrorEvent += new TradeCapture.LoginErrorDelegate(this.OnLoginError);
            tradeCaptureClient_.LogoutResultEvent += new TradeCapture.LogoutResultDelegate(this.OnLogoutResult);
            tradeCaptureClient_.LogoutEvent += new TradeCapture.LogoutDelegate(this.OnLogout);
            tradeCaptureClient_.TradeUpdateEvent += new TradeCapture.TradeUpdateDelegate(this.OnTradeUpdate);
            tradeCaptureClient_.NotificationEvent += new TradeCapture.NotificationDelegate(this.OnNotification);

            twoFactorLoginEvent_ = new AutoResetEvent(false);
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
        public DataTradeNetwork Network
        {
            get { return network_; }
        }

        /// <summary>
        /// Returns order entry protocol specification
        /// </summary>
        public ProtocolSpec OrderEntryProtocolSpec
        {
            get { return orderEntryClient_.ProtocolSpec; }
        }

        /// <summary>
        /// Returns trade capture protocol specification
        /// </summary>
        public ProtocolSpec TradeCaptureProtocolSpec
        {
            get { return tradeCaptureClient_.ProtocolSpec; }
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

        /// <summary>
        /// Occurs when trade cache is changed
        /// </summary>
        public event TradeUpdateHandler TradeUpdate;

        #endregion

        #region Methods

        /// <summary>
        /// Starts data feed instance.
        /// </summary>
        public void Start()
        {
            OrderEntry orderEntryClient = null;
            TradeCapture tradeCaptureClient = null;
            Thread eventThread = null;

            try
            {
                lock (synchronizer_)
                {
                    if (started_)
                        throw new Exception(string.Format("Data trade is already started : {0}", name_));

                    twoFactorLoginState_ = TwoFactorLoginState.None;
                    orderEntryTwoFactorLoginState_ = TwoFactorLoginState.None;
                    tradeCaptureTwoFactorLoginState_ = TwoFactorLoginState.None;

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
                        orderEntryClient_.LogoutAsync(this, "Client logout");
                    }
                    catch
                    {
                        orderEntryClient_.DisconnectAsync(this, "Client disconnect");
                    }

                    try
                    {
                        tradeCaptureClient_.LogoutAsync(this, "Client logout");
                    }
                    catch
                    {
                        tradeCaptureClient_.DisconnectAsync(this, "Client disconnect");
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

        void OnConnectResult(OrderEntry client, object data)
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

        void OnConnectError(OrderEntry client, object data, Exception exception)
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
                        args.Reason = LogoutReason.Unknown;
                        args.Text = exception.Message;
                        eventQueue_.PushEvent(args);

                        loginException_ = new LogoutException(exception.Message);
                        loginEvent_.Set();
                    }
                }
            }
            catch
            {
            }
        }

        void OnDisconnectResult(OrderEntry client, object data, string text)
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
                        args.Reason = LogoutReason.Unknown;
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

        void OnDisconnect(OrderEntry client, string text)
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
                        args.Reason = LogoutReason.Unknown;
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

        void OnReconnect(OrderEntry client)
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

        void OnReconnectError(OrderEntry client, Exception exception)
        {
            try
            {
                tradeCaptureClient_.DisconnectAsync(this, "Client disconnect");
            }
            catch
            {
            }
        }

        void OnTwoFactorLoginRequest(OrderEntry client, string text)
        {
            try
            {
                lock (synchronizer_)
                {
                    orderEntryTwoFactorLoginState_ = TwoFactorLoginState.Request;
                    //Console.WriteLine("orderEntryTwoFactorLoginState_:{0}", orderEntryTwoFactorLoginState_);

                    if (twoFactorLoginState_ == TwoFactorLoginState.None)
                    {
                        twoFactorLoginState_ = TwoFactorLoginState.Request;
                        //Console.WriteLine("twoFactorLoginState_:{0}", twoFactorLoginState_);

                        TwoFactorAuthEventArgs args = new TwoFactorAuthEventArgs();
                        TwoFactorAuth twoFactorAuth = new TwoFactorAuth();
                        twoFactorAuth.Reason = TwoFactorReason.ServerRequest;
                        args.TwoFactorAuth = twoFactorAuth;
                        eventQueue_.PushEvent(args);
                    }
                    else if (twoFactorLoginState_ == TwoFactorLoginState.Error)
                    {
                        twoFactorLoginState_ = TwoFactorLoginState.Request;
                        //Console.WriteLine("twoFactorLoginState_:{0}", twoFactorLoginState_);

                        TwoFactorAuthEventArgs args = new TwoFactorAuthEventArgs();
                        TwoFactorAuth twoFactorAuth = new TwoFactorAuth();
                        twoFactorAuth.Reason = TwoFactorReason.ServerRequest;
                        args.TwoFactorAuth = twoFactorAuth;
                        eventQueue_.PushEvent(args);
                    }
                }
            }
            catch
            {
            }
        }

        void OnTwoFactorLoginResult(OrderEntry client, object data, DateTime expireTime)
        {
            try
            {
                lock (synchronizer_)
                {
                    orderEntryTwoFactorLoginState_ = TwoFactorLoginState.Success;
                    orderEntryTwoFactorLoginException_ = null;
                    orderEntryTwoFactorLoginExpiration_ = expireTime;
                    //Console.WriteLine("orderEntryTwoFactorLoginState_:{0}", orderEntryTwoFactorLoginState_);

                    if (tradeCaptureTwoFactorLoginState_ == TwoFactorLoginState.Success)
                    {
                        twoFactorLoginState_ = TwoFactorLoginState.Success;
                        twoFactorLoginException_ = null;
                        twoFactorLoginExpiration_ = expireTime < tradeCaptureTwoFactorLoginExpiration_ ? expireTime : tradeCaptureTwoFactorLoginExpiration_;
                        //Console.WriteLine("twoFactorLoginState_:{0}", twoFactorLoginState_);

                        TwoFactorAuthEventArgs args = new TwoFactorAuthEventArgs();
                        TwoFactorAuth twoFactorAuth = new TwoFactorAuth();
                        twoFactorAuth.Reason = TwoFactorReason.ServerSuccess;
                        twoFactorAuth.Expire = twoFactorLoginExpiration_;
                        args.TwoFactorAuth = twoFactorAuth;
                        eventQueue_.PushEvent(args);

                        twoFactorLoginEvent_.Set();
                    }
                    else if (tradeCaptureTwoFactorLoginState_ == TwoFactorLoginState.None)
                    {
                        twoFactorLoginState_ = TwoFactorLoginState.Success;
                        twoFactorLoginException_ = null;
                        twoFactorLoginExpiration_ = expireTime;
                        //Console.WriteLine("twoFactorLoginState_:{0}", twoFactorLoginState_);

                        TwoFactorAuthEventArgs args = new TwoFactorAuthEventArgs();
                        TwoFactorAuth twoFactorAuth = new TwoFactorAuth();
                        twoFactorAuth.Reason = TwoFactorReason.ServerSuccess;
                        twoFactorAuth.Expire = expireTime;
                        args.TwoFactorAuth = twoFactorAuth;
                        eventQueue_.PushEvent(args);

                        twoFactorLoginEvent_.Set();
                    }
                }
            }
            catch
            {
            }
        }

        void OnTwoFactorLoginError(OrderEntry client, object data, Exception exception)
        {
            try
            {
                lock (synchronizer_)
                {
                    orderEntryTwoFactorLoginState_ = TwoFactorLoginState.Error;
                    orderEntryTwoFactorLoginException_ = exception;
                    //Console.WriteLine("orderEntryTwoFactorLoginState_:{0}", orderEntryTwoFactorLoginState_);

                    if (tradeCaptureTwoFactorLoginState_ == TwoFactorLoginState.Response)
                    {
                        twoFactorLoginState_ = TwoFactorLoginState.Error;
                        twoFactorLoginException_ = exception;
                        //Console.WriteLine("twoFactorLoginState_:{0}", twoFactorLoginState_);

                        TwoFactorAuthEventArgs args = new TwoFactorAuthEventArgs();
                        TwoFactorAuth twoFactorAuth = new TwoFactorAuth();
                        twoFactorAuth.Reason = TwoFactorReason.ServerError;
                        twoFactorAuth.Text = exception.Message;
                        args.TwoFactorAuth = twoFactorAuth;
                        eventQueue_.PushEvent(args);

                        twoFactorLoginEvent_.Set();
                    }
                    else if (tradeCaptureTwoFactorLoginState_ == TwoFactorLoginState.Success)
                    {
                        twoFactorLoginState_ = TwoFactorLoginState.Error;
                        twoFactorLoginException_ = exception;
                        //Console.WriteLine("twoFactorLoginState_:{0}", twoFactorLoginState_);

                        TwoFactorAuthEventArgs args = new TwoFactorAuthEventArgs();
                        TwoFactorAuth twoFactorAuth = new TwoFactorAuth();
                        twoFactorAuth.Reason = TwoFactorReason.ServerError;
                        twoFactorAuth.Text = exception.Message;
                        args.TwoFactorAuth = twoFactorAuth;
                        eventQueue_.PushEvent(args);

                        twoFactorLoginEvent_.Set();
                    }
                    else if (tradeCaptureTwoFactorLoginState_ == TwoFactorLoginState.None)
                    {
                        twoFactorLoginState_ = TwoFactorLoginState.Error;
                        twoFactorLoginException_ = exception;
                        //Console.WriteLine("twoFactorLoginState_:{0}", twoFactorLoginState_);

                        TwoFactorAuthEventArgs args = new TwoFactorAuthEventArgs();
                        TwoFactorAuth twoFactorAuth = new TwoFactorAuth();
                        twoFactorAuth.Reason = TwoFactorReason.ServerError;
                        twoFactorAuth.Text = exception.Message;
                        args.TwoFactorAuth = twoFactorAuth;
                        eventQueue_.PushEvent(args);

                        twoFactorLoginEvent_.Set();
                    }
                }
            }
            catch
            {
            }
        }

        void OnTwoFactorLoginResume(OrderEntry client, object data, DateTime expireTime)
        {
            try
            {
                lock (synchronizer_)
                {
                    orderEntryTwoFactorLoginState_ = TwoFactorLoginState.Success;
                    orderEntryTwoFactorLoginException_ = null;
                    orderEntryTwoFactorLoginExpiration_ = expireTime;
                    //Console.WriteLine("orderEntryTwoFactorLoginState_:{0}", orderEntryTwoFactorLoginState_);

                    if (tradeCaptureTwoFactorLoginState_ == TwoFactorLoginState.Success)
                    {
                        twoFactorLoginState_ = TwoFactorLoginState.Success;
                        twoFactorLoginException_ = null;
                        twoFactorLoginExpiration_ = expireTime < tradeCaptureTwoFactorLoginExpiration_ ? expireTime : tradeCaptureTwoFactorLoginExpiration_;
                        //Console.WriteLine("twoFactorLoginState_:{0}", twoFactorLoginState_);

                        TwoFactorAuthEventArgs args = new TwoFactorAuthEventArgs();
                        TwoFactorAuth twoFactorAuth = new TwoFactorAuth();
                        twoFactorAuth.Reason = TwoFactorReason.ServerResume;
                        twoFactorAuth.Expire = twoFactorLoginExpiration_;
                        args.TwoFactorAuth = twoFactorAuth;
                        eventQueue_.PushEvent(args);

                        twoFactorLoginEvent_.Set();
                    }
                    else if (tradeCaptureTwoFactorLoginState_ == TwoFactorLoginState.None)
                    {
                        twoFactorLoginState_ = TwoFactorLoginState.Success;
                        twoFactorLoginException_ = null;
                        twoFactorLoginExpiration_ = expireTime;
                        //Console.WriteLine("twoFactorLoginState_:{0}", twoFactorLoginState_);

                        TwoFactorAuthEventArgs args = new TwoFactorAuthEventArgs();
                        TwoFactorAuth twoFactorAuth = new TwoFactorAuth();
                        twoFactorAuth.Reason = TwoFactorReason.ServerResume;
                        twoFactorAuth.Expire = expireTime;
                        args.TwoFactorAuth = twoFactorAuth;
                        eventQueue_.PushEvent(args);

                        twoFactorLoginEvent_.Set();
                    }
                }
            }
            catch
            {
            }
        }

        void OnLoginResult(OrderEntry client, object data)
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

        void OnLoginError(OrderEntry client, object data, Exception exception)
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

                        LoginException loginException = exception as LoginException;
                        if (loginException != null)
                        {
                            args.Reason = loginException.LogoutReason;
                        }
                        else
                            args.Reason = LogoutReason.Unknown;

                        args.Text = exception.Message;
                        eventQueue_.PushEvent(args);

                        loginException_ = new LogoutException(exception.Message);
                        loginEvent_.Set();
                    }
                }
            }
            catch
            {
            }
        }

        void OnTradeServerInfoResult(OrderEntry client, object data, TradeServerInfo tradeServerInfo)
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

        void OnTradeServerInfoError(OrderEntry client, object data, Exception exception)
        {
            try
            {
                if (data == this)
                {
                    string reason = null;
                    if (exception is RejectException)
                        reason = ((RejectException)exception).Reason.ToString();

                    if (reason == null)
                    {
                        tradeCaptureClient_.DisconnectAsync(this, "Client disconnect");
                        orderEntryClient_.DisconnectAsync(this, "Client disconnect");
                    }
                    else
                    {
                        tradeCaptureClient_.DisconnectAsync(this, "Client disconnect: " + reason);
                        orderEntryClient_.DisconnectAsync(this, "Client disconnect: " + reason);
                    }
                }
            }
            catch
            {
            }
        }

        void OnAccountInfoResult(OrderEntry client, object data, AccountInfo accountInfo)
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

        void OnAccountInfoError(OrderEntry client, object data, Exception exception)
        {
            try
            {
                if (data == this)
                {
                    string reason = null;
                    if (exception is RejectException)
                        reason = ((RejectException)exception).Reason.ToString();

                    if (reason == null)
                    {
                        tradeCaptureClient_.DisconnectAsync(this, "Client disconnect");
                        orderEntryClient_.DisconnectAsync(this, "Client disconnect");
                    }
                    else
                    {
                        tradeCaptureClient_.DisconnectAsync(this, "Client disconnect: " + reason);
                        orderEntryClient_.DisconnectAsync(this, "Client disconnect: " + reason);
                    }
                }
            }
            catch
            {
            }
        }

        void OnSessionInfoResult(OrderEntry client, object data, SessionInfo sessionInfo)
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

        void OnSessionInfoError(OrderEntry client, object data, Exception exception)
        {
            try
            {
                if (data == this)
                {
                    string reason = null;
                    if (exception is RejectException)
                        reason = ((RejectException)exception).Reason.ToString();

                    if (reason == null)
                    {
                        tradeCaptureClient_.DisconnectAsync(this, "Client disconnect");
                        orderEntryClient_.DisconnectAsync(this, "Client disconnect");
                    }
                    else
                    {
                        tradeCaptureClient_.DisconnectAsync(this, "Client disconnect: " + reason);
                        orderEntryClient_.DisconnectAsync(this, "Client disconnect: " + reason);
                    }
                }
            }
            catch
            {
            }
        }

        void OnPositionsResult(OrderEntry client, object data, Position[] positions)
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

        void OnPositionsError(OrderEntry client, object data, Exception exception)
        {
            try
            {
                if (data == this)
                {
                    string reason = null;
                    if (exception is RejectException)
                        reason = ((RejectException)exception).Reason.ToString();

                    if (reason == null)
                    {
                        tradeCaptureClient_.DisconnectAsync(this, "Client disconnect");
                        orderEntryClient_.DisconnectAsync(this, "Client disconnect");
                    }
                    else
                    {
                        tradeCaptureClient_.DisconnectAsync(this, "Client disconnect: " + reason);
                        orderEntryClient_.DisconnectAsync(this, "Client disconnect: " + reason);
                    }
                }
            }
            catch
            {
            }
        }

        void OnOrdersBeginResult(OrderEntry client, object data, string id, int orderCount)
        {
            try
            {
                if (data == this)
                {
                    lock (cache_.mutex_)
                    {
                        cache_.tradeRecords_ = new Dictionary<string, TradeRecord>(orderCount);
                    }
                }
            }
            catch
            {
            }
        }

        void OnOrdersResult(OrderEntry client, object data, ExecutionReport executionReport)
        {
            try
            {
                if (data == this)
                {
                    TradeRecord tradeRecord = GetTradeRecord(executionReport);

                    lock (cache_.mutex_)
                    {
                        cache_.tradeRecords_.Add(tradeRecord.OrderId, tradeRecord);
                    }
                }
            }
            catch
            {
            }
        }

        void OnOrdersEndResult(OrderEntry client, object data)
        {
            try
            {
                if (data == this)
                {
                    lock (synchronizer_)
                    {
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

        void OnOrdersError(OrderEntry client, object data, Exception exception)
        {
            try
            {
                if (data == this)
                {
                    string reason = null;
                    if (exception is RejectException)
                        reason = ((RejectException)exception).Reason.ToString();

                    if (reason == null)
                    {
                        tradeCaptureClient_.DisconnectAsync(this, "Client disconnect");
                        orderEntryClient_.DisconnectAsync(this, "Client disconnect");
                    }
                    else
                    {
                        tradeCaptureClient_.DisconnectAsync(this, "Client disconnect: " + reason);
                        orderEntryClient_.DisconnectAsync(this, "Client disconnect: " + reason);
                    }
                }
            }
            catch
            {
            }
        }

        void OnLogoutResult(OrderEntry client, object data, LogoutInfo logoutInfo)
        {
            try
            {
                lock (synchronizer_)
                {
                    orderEntryClient_.DisconnectAsync(this, "Client disconnect");

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

        void OnLogout(OrderEntry client, LogoutInfo logoutInfo)
        {
            try
            {
                lock (synchronizer_)
                {
                    orderEntryClient_.DisconnectAsync(this, !string.IsNullOrEmpty(logoutInfo.Message) ? logoutInfo.Message : "Client disconnect");

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

        void OnNewOrderResult(OrderEntry client, object data, ExecutionReport report)
        {
            try
            {
                TradeUpdate update = null;

                lock (cache_.mutex_)
                {
                    update = UpdateCacheData(report);
                }

                ExecutionReportEventArgs args = new ExecutionReportEventArgs();
                args.Report = report;
                eventQueue_.PushEvent(args);

                PushTradeUpdateEvent(update);
            }
            catch
            {
            }
        }

        void OnNewOrderError(OrderEntry client, object data, Exception exception)
        {
            try
            {
                if (exception is ExecutionException)
                {
                    ExecutionException executionException = (ExecutionException)exception;
                    TradeUpdate update = null;

                    lock (cache_.mutex_)
                    {
                        update = UpdateCacheData(executionException.Report);
                    }

                    ExecutionReportEventArgs args = new ExecutionReportEventArgs();
                    args.Report = executionException.Report;
                    eventQueue_.PushEvent(args);

                    PushTradeUpdateEvent(update);
                }
                else if (exception is RejectException)
                {
                    RejectException rejectException = (RejectException)exception;

                    Common.ExecutionReport executionReport = new Common.ExecutionReport();
                    executionReport.ClientOrderId = rejectException.ClientOrderId;
                    executionReport.OrigClientOrderId = rejectException.ClientOrderId;
                    executionReport.ExecutionType = ExecutionType.Rejected;
                    executionReport.OrderStatus = OrderStatus.Rejected;
                    executionReport.RejectReason = rejectException.Reason;
                    executionReport.Text = exception.Message;

                    ExecutionReportEventArgs args = new ExecutionReportEventArgs();
                    args.Report = executionReport;
                    eventQueue_.PushEvent(args);
                }
                else
                {
                    Common.ExecutionReport executionReport = new Common.ExecutionReport();
                    executionReport.ExecutionType = ExecutionType.None;
                    executionReport.OrderStatus = OrderStatus.None;
                    executionReport.RejectReason = Common.RejectReason.Other;
                    executionReport.Text = exception.Message;

                    ExecutionReportEventArgs args = new ExecutionReportEventArgs();
                    args.Report = executionReport;
                    eventQueue_.PushEvent(args);
                }
            }
            catch
            {
            }
        }

        void OnReplaceOrderResult(OrderEntry client, object data, ExecutionReport report)
        {
            try
            {
                TradeUpdate update = null;

                lock (cache_.mutex_)
                {
                    update = UpdateCacheData(report);
                }

                ExecutionReportEventArgs args = new ExecutionReportEventArgs();
                args.Report = report;
                eventQueue_.PushEvent(args);

                PushTradeUpdateEvent(update);
            }
            catch
            {
            }
        }

        void OnReplaceOrderError(OrderEntry client, object data, Exception exception)
        {
            try
            {
                if (exception is ExecutionException)
                {
                    ExecutionException executionException = (ExecutionException)exception;
                    TradeUpdate update = null;

                    lock (cache_.mutex_)
                    {
                        update = UpdateCacheData(executionException.Report);
                    }

                    ExecutionReportEventArgs args = new ExecutionReportEventArgs();
                    args.Report = executionException.Report;
                    eventQueue_.PushEvent(args);

                    PushTradeUpdateEvent(update);
                }
                else if (exception is RejectException)
                {
                    RejectException rejectException = (RejectException)exception;

                    Common.ExecutionReport executionReport = new Common.ExecutionReport();
                    executionReport.ClientOrderId = rejectException.ClientOrderId;
                    executionReport.OrigClientOrderId = rejectException.ClientOrderId;
                    executionReport.ExecutionType = ExecutionType.Rejected;
                    executionReport.OrderStatus = OrderStatus.Calculated;
                    executionReport.RejectReason = rejectException.Reason;
                    executionReport.Text = exception.Message;

                    ExecutionReportEventArgs args = new ExecutionReportEventArgs();
                    args.Report = executionReport;
                    eventQueue_.PushEvent(args);
                }
                else
                {
                    Common.ExecutionReport executionReport = new Common.ExecutionReport();
                    executionReport.ExecutionType = ExecutionType.None;
                    executionReport.OrderStatus = OrderStatus.None;
                    executionReport.RejectReason = Common.RejectReason.Other;
                    executionReport.Text = exception.Message;

                    ExecutionReportEventArgs args = new ExecutionReportEventArgs();
                    args.Report = executionReport;
                    eventQueue_.PushEvent(args);
                }
            }
            catch
            {
            }
        }

        void OnCancelOrderResult(OrderEntry client, object data, ExecutionReport report)
        {
            try
            {
                TradeUpdate update = null;

                lock (cache_.mutex_)
                {
                    update = UpdateCacheData(report);
                }

                ExecutionReportEventArgs args = new ExecutionReportEventArgs();
                args.Report = report;
                eventQueue_.PushEvent(args);

                PushTradeUpdateEvent(update);
            }
            catch
            {
            }
        }

        void OnCancelOrderError(OrderEntry client, object data, Exception exception)
        {
            try
            {
                if (exception is ExecutionException)
                {
                    ExecutionException executionException = (ExecutionException)exception;
                    TradeUpdate update = null;

                    lock (cache_.mutex_)
                    {
                        update = UpdateCacheData(executionException.Report);
                    }

                    ExecutionReportEventArgs args = new ExecutionReportEventArgs();
                    args.Report = executionException.Report;
                    eventQueue_.PushEvent(args);

                    PushTradeUpdateEvent(update);
                }
                else if (exception is RejectException)
                {
                    RejectException rejectException = (RejectException)exception;

                    Common.ExecutionReport executionReport = new Common.ExecutionReport();
                    executionReport.ClientOrderId = rejectException.ClientOrderId;
                    executionReport.OrigClientOrderId = rejectException.ClientOrderId;
                    executionReport.ExecutionType = ExecutionType.Rejected;
                    executionReport.OrderStatus = OrderStatus.Calculated;
                    executionReport.RejectReason = rejectException.Reason;
                    executionReport.Text = exception.Message;

                    ExecutionReportEventArgs args = new ExecutionReportEventArgs();
                    args.Report = executionReport;
                    eventQueue_.PushEvent(args);
                }
                else
                {
                    Common.ExecutionReport executionReport = new Common.ExecutionReport();
                    executionReport.ExecutionType = ExecutionType.None;
                    executionReport.OrderStatus = OrderStatus.None;
                    executionReport.RejectReason = Common.RejectReason.Other;
                    executionReport.Text = exception.Message;

                    ExecutionReportEventArgs args = new ExecutionReportEventArgs();
                    args.Report = executionReport;
                    eventQueue_.PushEvent(args);
                }
            }
            catch
            {
            }
        }

        void OnClosePositionResult(OrderEntry client, object data, ExecutionReport report)
        {
            try
            {
                TradeUpdate update = null;

                lock (cache_.mutex_)
                {
                    update = UpdateCacheData(report);
                }

                ExecutionReportEventArgs args = new ExecutionReportEventArgs();
                args.Report = report;
                eventQueue_.PushEvent(args);

                PushTradeUpdateEvent(update);
            }
            catch
            {
            }
        }

        void OnClosePositionError(OrderEntry client, object data, Exception exception)
        {
            try
            {
                if (exception is ExecutionException)
                {
                    ExecutionException executionException = (ExecutionException)exception;
                    TradeUpdate update = null;

                    lock (cache_.mutex_)
                    {
                        update = UpdateCacheData(executionException.Report);
                    }

                    ExecutionReportEventArgs args = new ExecutionReportEventArgs();
                    args.Report = executionException.Report;
                    eventQueue_.PushEvent(args);

                    PushTradeUpdateEvent(update);
                }
                else if (exception is RejectException)
                {
                    RejectException rejectException = (RejectException)exception;

                    Common.ExecutionReport executionReport = new Common.ExecutionReport();
                    executionReport.ClientOrderId = rejectException.ClientOrderId;
                    executionReport.OrigClientOrderId = rejectException.ClientOrderId;
                    executionReport.ExecutionType = ExecutionType.Rejected;
                    executionReport.OrderStatus = OrderStatus.Calculated;
                    executionReport.RejectReason = rejectException.Reason;
                    executionReport.Text = exception.Message;

                    ExecutionReportEventArgs args = new ExecutionReportEventArgs();
                    args.Report = executionReport;
                    eventQueue_.PushEvent(args);
                }
                else
                {
                    Common.ExecutionReport executionReport = new Common.ExecutionReport();
                    executionReport.ExecutionType = ExecutionType.None;
                    executionReport.OrderStatus = OrderStatus.None;
                    executionReport.RejectReason = Common.RejectReason.Other;
                    executionReport.Text = exception.Message;

                    ExecutionReportEventArgs args = new ExecutionReportEventArgs();
                    args.Report = executionReport;
                    eventQueue_.PushEvent(args);
                }
            }
            catch
            {
            }
        }

        void OnClosePositionByResult(OrderEntry client, object data, ExecutionReport report)
        {
            try
            {
                TradeUpdate update = null;

                lock (cache_.mutex_)
                {
                    update = UpdateCacheData(report);
                }

                ExecutionReportEventArgs args = new ExecutionReportEventArgs();
                args.Report = report;
                eventQueue_.PushEvent(args);

                PushTradeUpdateEvent(update);
            }
            catch
            {
            }
        }

        void OnClosePositionByError(OrderEntry client, object data, Exception exception)
        {
            try
            {
                if (exception is ExecutionException)
                {
                    ExecutionException executionException = (ExecutionException)exception;
                    TradeUpdate update = null;

                    lock (cache_.mutex_)
                    {
                        update = UpdateCacheData(executionException.Report);
                    }

                    ExecutionReportEventArgs args = new ExecutionReportEventArgs();
                    args.Report = executionException.Report;
                    eventQueue_.PushEvent(args);

                    PushTradeUpdateEvent(update);
                }
                else if (exception is RejectException)
                {
                    RejectException rejectException = (RejectException)exception;

                    Common.ExecutionReport executionReport = new Common.ExecutionReport();
                    executionReport.ClientOrderId = rejectException.ClientOrderId;
                    executionReport.OrigClientOrderId = rejectException.ClientOrderId;
                    executionReport.ExecutionType = ExecutionType.Rejected;
                    executionReport.OrderStatus = OrderStatus.Calculated;
                    executionReport.RejectReason = rejectException.Reason;
                    executionReport.Text = exception.Message;

                    ExecutionReportEventArgs args = new ExecutionReportEventArgs();
                    args.Report = executionReport;
                    eventQueue_.PushEvent(args);
                }
                else
                {
                    Common.ExecutionReport executionReport = new Common.ExecutionReport();
                    executionReport.ExecutionType = ExecutionType.None;
                    executionReport.OrderStatus = OrderStatus.None;
                    executionReport.RejectReason = Common.RejectReason.Other;
                    executionReport.Text = exception.Message;

                    ExecutionReportEventArgs args = new ExecutionReportEventArgs();
                    args.Report = executionReport;
                    eventQueue_.PushEvent(args);
                }
            }
            catch
            {
            }
        }

        void OnSessionInfoUpdate(OrderEntry client, SessionInfo info)
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

        void OnAccountInfoUpdate(OrderEntry client, AccountInfo info)
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

        void OnOrderUpdate(OrderEntry client, ExecutionReport executionReport)
        {
            try
            {
                TradeUpdate update = null;

                lock (cache_.mutex_)
                {
                    update = UpdateCacheData(executionReport);
                }

                ExecutionReportEventArgs args = new ExecutionReportEventArgs();
                args.Report = executionReport;
                eventQueue_.PushEvent(args);

                PushTradeUpdateEvent(update);
            }
            catch
            {
            }
        }

        void OnPositionUpdate(OrderEntry client, Position position)
        {
            try
            {
                Position previous = null;

                lock (cache_.mutex_)
                {
                    if (cache_.positions_ != null)
                    {
                        if (cache_.positions_.ContainsKey(position.Symbol))
                            previous = cache_.positions_[position.Symbol];
                        cache_.positions_[position.Symbol] = position;

                        if (position.BuyAmount == 0 && position.SellAmount == 0)
                            cache_.positions_.Remove(position.Symbol);
                    }
                }

                PositionReportEventArgs args = new PositionReportEventArgs();
                args.Previous = (previous != null) ? previous : position;
                args.Report = position;
                eventQueue_.PushEvent(args);
            }
            catch
            {
            }
        }

        void OnBalanceUpdate(OrderEntry client, BalanceOperation balanceOperation)
        {
            try
            {
                TradeUpdate update;

                lock (cache_.mutex_)
                {
                    update = UpdateCacheData(balanceOperation);
                }

                NotificationEventArgs<BalanceOperation> args = new NotificationEventArgs<Common.BalanceOperation>();
                args.Type = NotificationType.Balance;
                args.Severity = NotificationSeverity.Information;
                args.Data = balanceOperation;
                eventQueue_.PushEvent(args);

                PushTradeUpdateEvent(update);
            }
            catch
            {
            }
        }

        void OnNotification(OrderEntry client, Notification notification)
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

        void OnConnectResult(TradeCapture client, object data)
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

        void OnConnectError(TradeCapture client, object data, Exception exception)
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
                        args.Reason = LogoutReason.Unknown;
                        args.Text = exception.Message;
                        eventQueue_.PushEvent(args);

                        loginException_ = new LogoutException(exception.Message);
                        loginEvent_.Set();
                    }
                }
            }
            catch
            {
            }
        }

        void OnDisconnectResult(TradeCapture client, object data, string text)
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
                        args.Reason = LogoutReason.Unknown;
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

        void OnDisconnect(TradeCapture client, string text)
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
                        args.Reason = LogoutReason.Unknown;
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

        void OnReconnect(TradeCapture client)
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

        void OnReconnectError(TradeCapture client, Exception exception)
        {
            try
            {
                orderEntryClient_.DisconnectAsync(this, "Client disconnect");
            }
            catch
            {
            }
        }

        void OnTwoFactorLoginRequest(TradeCapture client, string text)
        {
            try
            {
                lock (synchronizer_)
                {
                    tradeCaptureTwoFactorLoginState_ = TwoFactorLoginState.Request;
                    //Console.WriteLine("tradeCaptureTwoFactorLoginState_:{0}", tradeCaptureTwoFactorLoginState_);

                    if (twoFactorLoginState_ == TwoFactorLoginState.None)
                    {
                        twoFactorLoginState_ = TwoFactorLoginState.Request;
                        //Console.WriteLine("twoFactorLoginState_:{0}", twoFactorLoginState_);

                        TwoFactorAuthEventArgs args = new TwoFactorAuthEventArgs();
                        TwoFactorAuth twoFactorAuth = new TwoFactorAuth();
                        twoFactorAuth.Reason = TwoFactorReason.ServerRequest;
                        args.TwoFactorAuth = twoFactorAuth;
                        eventQueue_.PushEvent(args);
                    }
                    else if (twoFactorLoginState_ == TwoFactorLoginState.Error)
                    {
                        twoFactorLoginState_ = TwoFactorLoginState.Request;
                        //Console.WriteLine("twoFactorLoginState_:{0}", twoFactorLoginState_);

                        TwoFactorAuthEventArgs args = new TwoFactorAuthEventArgs();
                        TwoFactorAuth twoFactorAuth = new TwoFactorAuth();
                        twoFactorAuth.Reason = TwoFactorReason.ServerRequest;
                        args.TwoFactorAuth = twoFactorAuth;
                        eventQueue_.PushEvent(args);
                    }
                }
            }
            catch
            {
            }
        }

        void OnTwoFactorLoginResult(TradeCapture client, object data, DateTime expireTime)
        {
            try
            {
                lock (synchronizer_)
                {
                    tradeCaptureTwoFactorLoginState_ = TwoFactorLoginState.Success;
                    tradeCaptureTwoFactorLoginException_ = null;
                    tradeCaptureTwoFactorLoginExpiration_ = expireTime;
                    //Console.WriteLine("tradeCaptureTwoFactorLoginState_:{0}", tradeCaptureTwoFactorLoginState_);

                    if (orderEntryTwoFactorLoginState_ == TwoFactorLoginState.Success)
                    {
                        twoFactorLoginState_ = TwoFactorLoginState.Success;
                        twoFactorLoginException_ = null;
                        twoFactorLoginExpiration_ = expireTime < orderEntryTwoFactorLoginExpiration_ ? expireTime : orderEntryTwoFactorLoginExpiration_;
                        //Console.WriteLine("twoFactorLoginState_:{0}", twoFactorLoginState_);

                        TwoFactorAuthEventArgs args = new TwoFactorAuthEventArgs();
                        TwoFactorAuth twoFactorAuth = new TwoFactorAuth();
                        twoFactorAuth.Reason = TwoFactorReason.ServerSuccess;
                        twoFactorAuth.Expire = twoFactorLoginExpiration_;
                        args.TwoFactorAuth = twoFactorAuth;
                        eventQueue_.PushEvent(args);

                        twoFactorLoginEvent_.Set();
                    }
                    else if (orderEntryTwoFactorLoginState_ == TwoFactorLoginState.None)
                    {
                        twoFactorLoginState_ = TwoFactorLoginState.Success;
                        twoFactorLoginException_ = null;
                        twoFactorLoginExpiration_ = expireTime;
                        //Console.WriteLine("twoFactorLoginState_:{0}", twoFactorLoginState_);

                        TwoFactorAuthEventArgs args = new TwoFactorAuthEventArgs();
                        TwoFactorAuth twoFactorAuth = new TwoFactorAuth();
                        twoFactorAuth.Reason = TwoFactorReason.ServerSuccess;
                        twoFactorAuth.Expire = expireTime;
                        args.TwoFactorAuth = twoFactorAuth;
                        eventQueue_.PushEvent(args);

                        twoFactorLoginEvent_.Set();
                    }
                }
            }
            catch
            {
            }
        }

        void OnTwoFactorLoginError(TradeCapture client, object data, Exception exception)
        {
            try
            {
                lock (synchronizer_)
                {
                    tradeCaptureTwoFactorLoginState_ = TwoFactorLoginState.Error;
                    tradeCaptureTwoFactorLoginException_ = exception;
                    //Console.WriteLine("tradeCaptureTwoFactorLoginState_:{0}", tradeCaptureTwoFactorLoginState_);

                    if (orderEntryTwoFactorLoginState_ == TwoFactorLoginState.Response)
                    {
                        twoFactorLoginState_ = TwoFactorLoginState.Error;
                        twoFactorLoginException_ = exception;
                        //Console.WriteLine("twoFactorLoginState_:{0}", twoFactorLoginState_);

                        TwoFactorAuthEventArgs args = new TwoFactorAuthEventArgs();
                        TwoFactorAuth twoFactorAuth = new TwoFactorAuth();
                        twoFactorAuth.Reason = TwoFactorReason.ServerError;
                        twoFactorAuth.Text = exception.Message;
                        args.TwoFactorAuth = twoFactorAuth;
                        eventQueue_.PushEvent(args);

                        twoFactorLoginEvent_.Set();
                    }
                    else if (orderEntryTwoFactorLoginState_ == TwoFactorLoginState.Success)
                    {
                        twoFactorLoginState_ = TwoFactorLoginState.Error;
                        twoFactorLoginException_ = exception;
                        //Console.WriteLine("twoFactorLoginState_:{0}", twoFactorLoginState_);

                        TwoFactorAuthEventArgs args = new TwoFactorAuthEventArgs();
                        TwoFactorAuth twoFactorAuth = new TwoFactorAuth();
                        twoFactorAuth.Reason = TwoFactorReason.ServerError;
                        twoFactorAuth.Text = exception.Message;
                        args.TwoFactorAuth = twoFactorAuth;
                        eventQueue_.PushEvent(args);

                        twoFactorLoginEvent_.Set();
                    }
                    else if (orderEntryTwoFactorLoginState_ == TwoFactorLoginState.None)
                    {
                        twoFactorLoginState_ = TwoFactorLoginState.Error;
                        twoFactorLoginException_ = exception;
                        //Console.WriteLine("twoFactorLoginState_:{0}", twoFactorLoginState_);

                        TwoFactorAuthEventArgs args = new TwoFactorAuthEventArgs();
                        TwoFactorAuth twoFactorAuth = new TwoFactorAuth();
                        twoFactorAuth.Reason = TwoFactorReason.ServerError;
                        twoFactorAuth.Text = exception.Message;
                        args.TwoFactorAuth = twoFactorAuth;
                        eventQueue_.PushEvent(args);

                        twoFactorLoginEvent_.Set();
                    }
                }
            }
            catch
            {
            }
        }

        void OnTwoFactorLoginResume(TradeCapture client, object data, DateTime expireTime)
        {
            try
            {
                lock (synchronizer_)
                {
                    tradeCaptureTwoFactorLoginState_ = TwoFactorLoginState.Success;
                    tradeCaptureTwoFactorLoginException_ = null;
                    tradeCaptureTwoFactorLoginExpiration_ = expireTime;
                    //Console.WriteLine("tradeCaptureTwoFactorLoginState_:{0}", tradeCaptureTwoFactorLoginState_);

                    if (orderEntryTwoFactorLoginState_ == TwoFactorLoginState.Success)
                    {
                        twoFactorLoginState_ = TwoFactorLoginState.Success;
                        twoFactorLoginException_ = null;
                        twoFactorLoginExpiration_ = expireTime < orderEntryTwoFactorLoginExpiration_ ? expireTime : orderEntryTwoFactorLoginExpiration_;
                        //Console.WriteLine("twoFactorLoginState_:{0}", twoFactorLoginState_);

                        TwoFactorAuthEventArgs args = new TwoFactorAuthEventArgs();
                        TwoFactorAuth twoFactorAuth = new TwoFactorAuth();
                        twoFactorAuth.Reason = TwoFactorReason.ServerResume;
                        twoFactorAuth.Expire = twoFactorLoginExpiration_;
                        args.TwoFactorAuth = twoFactorAuth;
                        eventQueue_.PushEvent(args);

                        twoFactorLoginEvent_.Set();
                    }
                    else if (orderEntryTwoFactorLoginState_ == TwoFactorLoginState.None)
                    {
                        twoFactorLoginState_ = TwoFactorLoginState.Success;
                        twoFactorLoginException_ = null;
                        twoFactorLoginExpiration_ = expireTime;
                        //Console.WriteLine("twoFactorLoginState_:{0}", twoFactorLoginState_);

                        TwoFactorAuthEventArgs args = new TwoFactorAuthEventArgs();
                        TwoFactorAuth twoFactorAuth = new TwoFactorAuth();
                        twoFactorAuth.Reason = TwoFactorReason.ServerResume;
                        twoFactorAuth.Expire = expireTime;
                        args.TwoFactorAuth = twoFactorAuth;
                        eventQueue_.PushEvent(args);

                        twoFactorLoginEvent_.Set();
                    }
                }
            }
            catch
            {
            }
        }

        void OnLoginResult(TradeCapture client, object data)
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

        void OnLoginError(TradeCapture client, object data, Exception exception)
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

                        LoginException loginException = exception as LoginException;
                        if (loginException != null)
                        {
                            args.Reason = loginException.LogoutReason;
                        }
                        else
                            args.Reason = LogoutReason.Unknown;

                        args.Text = exception.Message;
                        eventQueue_.PushEvent(args);

                        loginException_ = new LogoutException(exception.Message);
                        loginEvent_.Set();
                    }
                }
            }
            catch
            {
            }
        }

        void OnLogoutResult(TradeCapture client, object data, LogoutInfo logoutInfo)
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

        void OnLogout(TradeCapture client, LogoutInfo logoutInfo)
        {
            try
            {
                lock (synchronizer_)
                {
                    tradeCaptureClient_.DisconnectAsync(this, !string.IsNullOrEmpty(logoutInfo.Message) ? logoutInfo.Message : "Client disconnect");

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

        void OnTradeUpdate(TradeCapture client, TradeTransactionReport tradeTransactionReport)
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

        void OnNotification(TradeCapture client, Notification notification)
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

        TradeUpdate UpdateCacheData(ExecutionReport executionReport)
        {
            var tradeUpdate = new TradeUpdate();
            tradeUpdate.TradeRecordUpdateAction = UpdateActions.None;

            if (cache_.tradeRecords_ != null)
            {
                if (cache_.tradeRecords_.ContainsKey(executionReport.OrderId))
                    tradeUpdate.OldRecord = cache_.tradeRecords_[executionReport.OrderId];

                if (executionReport.ExecutionType == ExecutionType.Trade && executionReport.OrderStatus == OrderStatus.Filled)
                {
                    if (cache_.tradeRecords_.ContainsKey(executionReport.OrderId))
                    {
                        cache_.tradeRecords_.Remove(executionReport.OrderId);

                        tradeUpdate.TradeRecordUpdateAction = UpdateActions.Removed;
                    }
                }
                else if (executionReport.ExecutionType == ExecutionType.Trade && executionReport.OrderStatus == OrderStatus.PartiallyFilled && executionReport.OrderType == OrderType.Market)
                {
                    if (cache_.tradeRecords_.ContainsKey(executionReport.OrderId))
                    {
                        cache_.tradeRecords_.Remove(executionReport.OrderId);

                        tradeUpdate.TradeRecordUpdateAction = UpdateActions.Removed;
                    }
                }
                else if (executionReport.ExecutionType == ExecutionType.Trade && executionReport.OrderStatus == OrderStatus.Activated)
                {
                    cache_.tradeRecords_.Remove(executionReport.OrderId);

                    tradeUpdate.TradeRecordUpdateAction = UpdateActions.Removed;
                }
                else if (executionReport.ExecutionType == ExecutionType.Canceled)
                {
                    cache_.tradeRecords_.Remove(executionReport.OrderId);

                    tradeUpdate.TradeRecordUpdateAction = UpdateActions.Removed;
                }
                else if (executionReport.ExecutionType == ExecutionType.Expired)
                {
                    cache_.tradeRecords_.Remove(executionReport.OrderId);

                    tradeUpdate.TradeRecordUpdateAction = UpdateActions.Removed;
                }
                else if (executionReport.ExecutionType == ExecutionType.Rejected)
                {
                    // Do nothing here...
                }
                else if (executionReport.ExecutionType == ExecutionType.New && executionReport.OrderStatus == OrderStatus.New)
                {
                    // Do nothing here...
                }
                else if (executionReport.LeavesVolume == 0)
                {
                    cache_.tradeRecords_.Remove(executionReport.OrderId);

                    tradeUpdate.TradeRecordUpdateAction = UpdateActions.Removed;
                }
                else
                {
                    TradeRecord tradeRecord = GetTradeRecord(executionReport);

                    if (cache_.tradeRecords_.ContainsKey(executionReport.OrderId))
                        tradeUpdate.TradeRecordUpdateAction = UpdateActions.Replaced;
                    else
                        tradeUpdate.TradeRecordUpdateAction = UpdateActions.Added;
                    tradeUpdate.NewRecord = tradeRecord;

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

                    if (reportAsset.Balance != 0)
                    {
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
                    else
                    {
                        int cacheIndex;
                        for (cacheIndex = 0; cacheIndex < cacheAssets.Length; ++cacheIndex)
                        {
                            AssetInfo cacheAsset = cacheAssets[cacheIndex];

                            if (cacheAsset.Currency == reportAsset.Currency)
                                break;
                        }

                        if (cacheIndex < cacheAssets.Length)
                        {
                            AssetInfo[] assets = new AssetInfo[cacheAssets.Length - 1];
                            Array.Copy(cacheAssets, 0, assets, 0, cacheIndex);
                            Array.Copy(cacheAssets, cacheIndex + 1, assets, cacheIndex, assets.Length - cacheIndex);

                            cache_.accountInfo_.Assets = assets;
                        }
                    }
                }
                tradeUpdate.UpdatedAssets = reportAssets;

                if (executionReport.Balance.HasValue)
                {
                    cache_.accountInfo_.Balance = executionReport.Balance.Value;
                    tradeUpdate.NewBalance = executionReport.Balance.Value;
                }
            }

            return tradeUpdate;
        }

        TradeUpdate UpdateCacheData(BalanceOperation balanceOperation)
        {
            var tradeUpdate = new TradeUpdate();
            tradeUpdate.TradeRecordUpdateAction = UpdateActions.None;

            if (cache_.accountInfo_ != null)
            {
                if (cache_.accountInfo_.Type == AccountType.Cash)
                {
                    AssetInfo[] cacheAssets = cache_.accountInfo_.Assets;

                    if (balanceOperation.Balance != 0)
                    {
                        int cacheIndex;
                        for (cacheIndex = 0; cacheIndex < cacheAssets.Length; ++cacheIndex)
                        {
                            AssetInfo cacheAsset = cacheAssets[cacheIndex];

                            if (cacheAsset.Currency == balanceOperation.TransactionCurrency)
                            {
                                var assetInfoCopy = cacheAsset.Clone();
                                assetInfoCopy.Balance = balanceOperation.Balance;
                                assetInfoCopy.TradeAmount = balanceOperation.TransactionAmount;
                                cacheAssets[cacheIndex] = assetInfoCopy;
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
                        int cacheIndex;
                        for (cacheIndex = 0; cacheIndex < cacheAssets.Length; ++cacheIndex)
                        {
                            AssetInfo cacheAsset = cacheAssets[cacheIndex];

                            if (cacheAsset.Currency == balanceOperation.TransactionCurrency)
                                break;
                        }

                        if (cacheIndex < cacheAssets.Length)
                        {
                            AssetInfo[] assets = new AssetInfo[cacheAssets.Length - 1];
                            Array.Copy(cacheAssets, 0, assets, 0, cacheIndex);
                            Array.Copy(cacheAssets, cacheIndex + 1, assets, cacheIndex, assets.Length - cacheIndex);

                            cache_.accountInfo_.Assets = assets;
                        }
                    }

                    AssetInfo assetInfoUpdated = new AssetInfo();
                    assetInfoUpdated.Currency = balanceOperation.TransactionCurrency;
                    assetInfoUpdated.Balance = balanceOperation.Balance;
                    assetInfoUpdated.TradeAmount = balanceOperation.TransactionAmount;
                    tradeUpdate.UpdatedAssets = new[] {assetInfoUpdated};
                }
                else
                {
                    cache_.accountInfo_.Balance = balanceOperation.Balance;
                    tradeUpdate.NewBalance = balanceOperation.Balance;
                }
            }

            return tradeUpdate;
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
                    positionArgs.Previous = positions[index];
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
                    positionArgs.Previous = positions[index];
                    positionArgs.Report = positions[index];
                    eventQueue_.PushEvent(positionArgs);
                }
            }
        }

        void PushTradeUpdateEvent(TradeUpdate update)
        {
            if (update != null)
            {
                TradeUpdateEventArgs args = new TradeUpdateEventArgs();
                args.Update = update;
                eventQueue_.PushEvent(args);
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

            TradeUpdateEventArgs tradeRecordUpdatedEventArgs = eventArgs as TradeUpdateEventArgs;

            if (tradeRecordUpdatedEventArgs != null)
            {
                if (TradeUpdate != null)
                {
                    try
                    {
                        TradeUpdate(this, tradeRecordUpdatedEventArgs);
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
            tradeRecord.InitialPrice = executionReport.InitialPrice;
            tradeRecord.Price = executionReport.Price;
            tradeRecord.StopPrice = executionReport.StopPrice;
            tradeRecord.TakeProfit = executionReport.TakeProfit;
            tradeRecord.StopLoss = executionReport.StopLoss;
            tradeRecord.Commission = executionReport.Commission;
            tradeRecord.AgentCommission = executionReport.AgentCommission;
            tradeRecord.Swap = executionReport.Swap;
            tradeRecord.InitialType = executionReport.InitialOrderType;
            tradeRecord.Type = executionReport.OrderType;
            tradeRecord.Side = executionReport.OrderSide;
            tradeRecord.IsReducedOpenCommission = executionReport.ReducedOpenCommission;
            tradeRecord.IsReducedCloseCommission = executionReport.ReducedCloseCommission;
            tradeRecord.MarketWithSlippage = executionReport.MarketWithSlippage;
            tradeRecord.Expiration = executionReport.Expiration;
            tradeRecord.Created = executionReport.Created;
            tradeRecord.Modified = executionReport.Modified;
            tradeRecord.Comment = executionReport.Comment;
            tradeRecord.Tag = executionReport.Tag;
            tradeRecord.Magic = executionReport.Magic;
            tradeRecord.ImmediateOrCancel = executionReport.ImmediateOrCancelFlag;
            tradeRecord.Slippage = executionReport.Slippage;

            return tradeRecord;
        }

        internal enum TwoFactorLoginState
        {
            None,
            Request,
            Response,
            Resume,
            Success,
            Error
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
        internal DataTradeNetwork network_;
        internal OrderEntry orderEntryClient_;
        internal TradeCapture tradeCaptureClient_;

        internal object synchronizer_;
        internal bool started_;

        internal TwoFactorLoginState twoFactorLoginState_;
        internal DateTime twoFactorLoginExpiration_;
        internal Exception twoFactorLoginException_;
        internal AutoResetEvent twoFactorLoginEvent_;

        internal TwoFactorLoginState orderEntryTwoFactorLoginState_;
        internal DateTime orderEntryTwoFactorLoginExpiration_;
        internal Exception orderEntryTwoFactorLoginException_;

        internal TwoFactorLoginState tradeCaptureTwoFactorLoginState_;
        internal DateTime tradeCaptureTwoFactorLoginExpiration_;
        internal Exception tradeCaptureTwoFactorLoginException_;

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
