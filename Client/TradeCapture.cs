using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using SoftFX.Net.TradeCapture;
using TickTrader.FDK.Common;

namespace TickTrader.FDK.Client
{
    public class TradeCapture : IDisposable
    {
        #region Constructors

        public TradeCapture
        (
            string name,
            bool logEvents = false,
            bool logStates = false,
            bool logMessages = false,
            int port = 5060,
            string serverCertificateName = null,
            int connectAttempts = -1,
            int reconnectAttempts = -1,
            int connectInterval = 10000,
            int heartbeatInterval = 10000,
            string logDirectory = "Logs"            
        )
        {
            ClientSessionOptions options = new ClientSessionOptions(port);
            options.ConnectionType = SoftFX.Net.Core.ConnectionType.SecureSocket;
            options.ServerCertificateName = serverCertificateName;
            options.ServerMinMinorVersion = Info.TradeCapture.MinorVersion;
            options.ConnectMaxCount = connectAttempts;
            options.ReconnectMaxCount = reconnectAttempts;
            options.ConnectInterval = connectInterval;
            options.HeartbeatInterval = heartbeatInterval;
            options.Log.Directory = logDirectory;
            options.Log.Events = logEvents;
            options.Log.States = logStates;
            options.Log.Messages = logMessages;
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

        public delegate void ConnectResultDelegate(TradeCapture tradeCapture, object data);
        public delegate void ConnectErrorDelegate(TradeCapture tradeCapture, object data, Exception exception);
        public delegate void DisconnectResultDelegate(TradeCapture tradeCapture, object data, string text);
        public delegate void DisconnectDelegate(TradeCapture tradeCapture, string text);
        public delegate void ReconnectDelegate(TradeCapture tradeCapture);
        public delegate void ReconnectErrorDelegate(TradeCapture tradeCapture, Exception exception);
        public delegate void SendDelegate(TradeCapture tradeCapture);

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

            if (! context.Wait(timeout))
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

        public delegate void LoginResultDelegate(TradeCapture tradeCapture, object data);
        public delegate void LoginErrorDelegate(TradeCapture tradeCapture, object data, Exception exception);
        public delegate void TwoFactorLoginRequestDelegate(TradeCapture tradeCapture, string message);
        public delegate void TwoFactorLoginResultDelegate(TradeCapture tradeCapture, object data, DateTime expireTime);
        public delegate void TwoFactorLoginErrorDelegate(TradeCapture tradeCapture, object data, Exception exception);
        public delegate void TwoFactorLoginResumeDelegate(TradeCapture tradeCapture, object data, DateTime expireTime);
        public delegate void LogoutResultDelegate(TradeCapture tradeCapture, object data, LogoutInfo logoutInfo);
        public delegate void LogoutErrorDelegate(TradeCapture tradeCapture, object data, Exception exception);
        public delegate void LogoutDelegate(TradeCapture tradeCapture, LogoutInfo logoutInfo);

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

        #region Trade Capture
        
        public delegate void SubscribeTradesResultBeginDelegate(TradeCapture tradeCapture, object data, int count);        
        public delegate void SubscribeTradesResultDelegate(TradeCapture tradeCapture, object data, TickTrader.FDK.Common.TradeTransactionReport tradeTransactionReport);
        public delegate void SubscribeTradesResultEndDelegate(TradeCapture tradeCapture, object data);
        public delegate void SubscribeTradesErrorDelegate(TradeCapture tradeCapture, object data, Exception exception);
        public delegate void UnsubscribeTradesResultDelegate(TradeCapture tradeCapture, object data);
        public delegate void UnsubscribeTradesErrorDelegate(TradeCapture tradeCapture, object data, Exception exception);
        public delegate void DownloadTradesResultBeginDelegate(TradeCapture tradeCapture, object data, string id, int totalCount);
        public delegate void DownloadTradesResultDelegate(TradeCapture tradeCapture, object data, TickTrader.FDK.Common.TradeTransactionReport tradeTransactionReport);
        public delegate void DownloadTradesResultEndDelegate(TradeCapture tradeCapture, object data);
        public delegate void DownloadTradesErrorDelegate(TradeCapture tradeCapture, object data, Exception exception);
        public delegate void CancelDownloadTradesResultDelegate(TradeCapture tradeCapture, object data);
        public delegate void CancelDownloadTradesErrorDelegate(TradeCapture tradeCapture, object data, Exception exception);
        public delegate void DownloadAccountReportsResultBeginDelegate(TradeCapture tradeCapture, object data, string id, int count);
        public delegate void DownloadAccountReportsResultDelegate(TradeCapture tradeCapture, object data, AccountReport accountReport);
        public delegate void DownloadAccountReportsResultEndDelegate(TradeCapture tradeCapture, object data);
        public delegate void DownloadAccountReportsErrorDelegate(TradeCapture tradeCapture, object data, Exception exception);
        public delegate void CancelDownloadAccountReportsResultDelegate(TradeCapture tradeCapture, object data);
        public delegate void CancelDownloadAccountReportsErrorDelegate(TradeCapture tradeCapture, object data, Exception exception);
        public delegate void TradeUpdateDelegate(TradeCapture tradeCapture, TickTrader.FDK.Common.TradeTransactionReport tradeTransactionReport);
        public delegate void NotificationDelegate(TradeCapture tradeCapture, TickTrader.FDK.Common.Notification notification);
        
        public event SubscribeTradesResultBeginDelegate SubscribeTradesResultBeginEvent;
        public event SubscribeTradesResultDelegate SubscribeTradesResultEvent;
        public event SubscribeTradesResultEndDelegate SubscribeTradesResultEndEvent;
        public event SubscribeTradesErrorDelegate SubscribeTradesErrorEvent;
        public event UnsubscribeTradesResultDelegate UnsubscribeTradesResultEvent;
        public event UnsubscribeTradesErrorDelegate UnsubscribeTradesErrorEvent;
        public event DownloadTradesResultBeginDelegate DownloadTradesResultBeginEvent;
        public event DownloadTradesResultDelegate DownloadTradesResultEvent;
        public event DownloadTradesResultEndDelegate DownloadTradesResultEndEvent;
        public event DownloadTradesErrorDelegate DownloadTradesErrorEvent;
        public event CancelDownloadTradesResultDelegate CancelDownloadTradesResultEvent;
        public event CancelDownloadTradesErrorDelegate CancelDownloadTradesErrorEvent;
        public event DownloadAccountReportsResultBeginDelegate DownloadAccountReportsResultBeginEvent;
        public event DownloadAccountReportsResultDelegate DownloadAccountReportsResultEvent;
        public event DownloadAccountReportsResultEndDelegate DownloadAccountReportsResultEndEvent;
        public event DownloadAccountReportsErrorDelegate DownloadAccountReportsErrorEvent;
        public event CancelDownloadAccountReportsResultDelegate CancelDownloadAccountReportsResultEvent;
        public event CancelDownloadAccountReportsErrorDelegate CancelDownloadAccountReportsErrorEvent;
        public event TradeUpdateDelegate TradeUpdateEvent;
        public event NotificationDelegate NotificationEvent;

        public SubscribeTradesEnumerator SubscribeTrades(DateTime? from, bool skipCancel, int timeout)
        {
            SubscribeTradesAsyncContext context = new SubscribeTradesAsyncContext(true);
            context.enumerator_ = new SubscribeTradesEnumerator(this);

            SubscribeTradesInternal(context, from, skipCancel);

            context.enumerator_.Begin(timeout);

            return context.enumerator_;
        }

        public void SubscribeTradesAsync(object data, DateTime? from, bool skipCancel)
        {
            SubscribeTradesAsyncContext context = new SubscribeTradesAsyncContext(false);
            context.Data = data;

            SubscribeTradesInternal(context, from, skipCancel);
        }

        void SubscribeTradesInternal(SubscribeTradesAsyncContext context, DateTime? from, bool skipCancel)
        {
            TradeSubscribeRequest request = new TradeSubscribeRequest(0);
            request.Id = Guid.NewGuid().ToString();
            request.From = from;
            request.SkipCancel = skipCancel;

            session_.SendTradeSubscribeRequest(context, request);
        }

        public void UnsubscribeTrades(int timeout)
        {
            UnsubscribeTradesAsyncContext context = new UnsubscribeTradesAsyncContext(true);

            UnsubscribeTradesInternal(context);

            if (! context.Wait(timeout))
                throw new Common.TimeoutException("Method call timed out");

            if (context.exception_ != null)
                throw context.exception_;
        }

        public void UnsubscribeTradesAsync(object data)
        {
            UnsubscribeTradesAsyncContext context = new UnsubscribeTradesAsyncContext(false);
            context.Data = data;

            UnsubscribeTradesInternal(context);
        }

        void UnsubscribeTradesInternal(UnsubscribeTradesAsyncContext context)
        {
            TradeUnsubscribeRequest request = new TradeUnsubscribeRequest(0);
            request.Id = Guid.NewGuid().ToString();

            session_.SendTradeUnsubscribeRequest(context, request);
        }

        public DownloadTradesEnumerator DownloadTrades(TimeDirection timeDirection, DateTime? from, DateTime? to, bool skipCancel, int timeout)
        {
            TradeDownloadAsyncContext context = new TradeDownloadAsyncContext(true);
            context.enumerator_ = new DownloadTradesEnumerator(this);

            DownloadTradesInternal(context, timeDirection, from, to, skipCancel);

            context.enumerator_.Begin(timeout);

            return context.enumerator_;
        }

        public void DownloadTradesAsync(object data, TimeDirection timeDirection, DateTime? from, DateTime? to, bool skipCancel)
        {
            TradeDownloadAsyncContext context = new TradeDownloadAsyncContext(false);
            context.Data = data;

            DownloadTradesInternal(context, timeDirection, from, to, skipCancel);
        }

        void DownloadTradesInternal(TradeDownloadAsyncContext context, TimeDirection timeDirection, DateTime? from, DateTime? to, bool skipCancel)
        {
            TradeDownloadRequest request = new TradeDownloadRequest(0);
            request.Id = Guid.NewGuid().ToString();
            request.Direction = GetTradeHistoryDirection(timeDirection);
            request.From = from;
            request.To = to;
            request.SkipCancel = skipCancel;

            session_.SendTradeDownloadRequest(context, request);
        }

        public void CancelDownloadTrades(string id, int timeout)
        {
            CancelDownloadTradesAsyncContext context = new CancelDownloadTradesAsyncContext(true);

            CancelDownloadTradesInternal(context, id);

            if (! context.Wait(timeout))
                throw new Common.TimeoutException("Method call timed out");

            if (context.exception_ != null)
                throw context.exception_;
        }

        public void CancelDownloadTradesAsync(object data, string id)
        {
            CancelDownloadTradesAsyncContext context = new CancelDownloadTradesAsyncContext(false);
            context.Data = data;

            CancelDownloadTradesInternal(context, id);
        }

        void CancelDownloadTradesInternal(CancelDownloadTradesAsyncContext context, string id)
        {
            TradeDownloadCancelRequest request = new TradeDownloadCancelRequest(0);
            request.Id = Guid.NewGuid().ToString();
            request.RequestId = id;

            session_.SendTradeDownloadCancelRequest(context, request);
        }

        public DownloadAccountReportsEnumerator DownloadAccountReports(TimeDirection timeDirection, DateTime? from, DateTime? to, int timeout)
        {
            DownloadAccountReportsAsyncContext context = new DownloadAccountReportsAsyncContext(true);
            context.enumerator_ = new DownloadAccountReportsEnumerator(this);

            DownloadAccountReportsInternal(context, timeDirection, from, to);

            context.enumerator_.Begin(timeout);

            return context.enumerator_;
        }

        public void DownloadAccountReportsAsync(object data, TimeDirection timeDirection, DateTime? from, DateTime? to)
        {
            DownloadAccountReportsAsyncContext context = new DownloadAccountReportsAsyncContext(false);
            context.Data = data;

            DownloadAccountReportsInternal(context, timeDirection, from, to);
        }

        void DownloadAccountReportsInternal(DownloadAccountReportsAsyncContext context, TimeDirection timeDirection, DateTime? from, DateTime? to)
        {
            AccountDownloadRequest request = new AccountDownloadRequest(0);
            request.Id = Guid.NewGuid().ToString();
            request.Direction = GetAccountHistoryDirection(timeDirection);
            request.From = from;
            request.To = to;

            session_.SendAccountDownloadRequest(context, request);
        }

        public void CancelDownloadAccountReports(string id, int timeout)
        {
            CancelDownloadAccountReportsAsyncContext context = new CancelDownloadAccountReportsAsyncContext(true);

            CancelDownloadAccountReportsInternal(context, id);

            if (! context.Wait(timeout))
                throw new Common.TimeoutException("Method call timed out");

            if (context.exception_ != null)
                throw context.exception_;
        }

        public void CancelDownloadAccountReportsAsync(object data, string id)
        {
            CancelDownloadAccountReportsAsyncContext context = new CancelDownloadAccountReportsAsyncContext(false);
            context.Data = data;

            CancelDownloadAccountReportsInternal(context, id);
        }

        void CancelDownloadAccountReportsInternal(CancelDownloadAccountReportsAsyncContext context, string id)
        {
            AccountDownloadCancelRequest request = new AccountDownloadCancelRequest(0);
            request.Id = Guid.NewGuid().ToString();
            request.RequestId = id;

            session_.SendAccountDownloadCancelRequest(context, request);
        }

        SoftFX.Net.TradeCapture.TradeHistoryDirection GetTradeHistoryDirection(TickTrader.FDK.Common.TimeDirection timeDirection)
        {
            switch (timeDirection)
            {
                case TickTrader.FDK.Common.TimeDirection.Forward:
                    return SoftFX.Net.TradeCapture.TradeHistoryDirection.Forward;

                case TickTrader.FDK.Common.TimeDirection.Backward:
                    return SoftFX.Net.TradeCapture.TradeHistoryDirection.Backward;

                default:
                    throw new Exception("Invalid time direction : " + timeDirection);
            }
        }

        SoftFX.Net.TradeCapture.AccountHistoryDirection GetAccountHistoryDirection(TickTrader.FDK.Common.TimeDirection timeDirection)
        {
            switch (timeDirection)
            {
                case TickTrader.FDK.Common.TimeDirection.Forward:
                    return SoftFX.Net.TradeCapture.AccountHistoryDirection.Forward;

                case TickTrader.FDK.Common.TimeDirection.Backward:
                    return SoftFX.Net.TradeCapture.AccountHistoryDirection.Backward;

                default:
                    throw new Exception("Invalid time direction : " + timeDirection);
            }
        }

        #endregion

        #region Implementation

        interface IAsyncContext
        {
            void ProcessDisconnect(TradeCapture tradeCapture, string text);
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

            public void ProcessDisconnect(TradeCapture tradeCapture, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (tradeCapture.LoginErrorEvent != null)
                {
                    try
                    {
                        tradeCapture.LoginErrorEvent(tradeCapture, Data, exception);
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

            public void ProcessDisconnect(TradeCapture tradeCapture, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (tradeCapture.LoginErrorEvent != null)
                {
                    try
                    {
                        tradeCapture.LoginErrorEvent(tradeCapture, Data, exception);
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

            public void ProcessDisconnect(TradeCapture tradeCapture, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (tradeCapture.LoginErrorEvent != null)
                {
                    try
                    {
                        tradeCapture.LoginErrorEvent(tradeCapture, Data, exception);
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

            public void ProcessDisconnect(TradeCapture tradeCapture, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (tradeCapture.LogoutErrorEvent != null)
                {
                    try
                    {
                        tradeCapture.LogoutErrorEvent(tradeCapture, Data, exception);
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

        class SubscribeTradesAsyncContext : TradeSubscribeRequestClientContext, IAsyncContext
        {
            public SubscribeTradesAsyncContext(bool waitbale) : base(waitbale)
            {
            }

            public void ProcessDisconnect(TradeCapture tradeCapture, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (tradeCapture.SubscribeTradesErrorEvent != null)
                {
                    try
                    {
                        tradeCapture.SubscribeTradesErrorEvent(tradeCapture, Data, exception);
                    }
                    catch
                    {
                    }
                }

                if (Waitable)
                {
                    enumerator_.SetError(exception);
                }
            }

            public TradeTransactionReport tradeTransactionReport_;
            public SubscribeTradesEnumerator enumerator_;            
        }

        class UnsubscribeTradesAsyncContext : TradeUnsubscribeRequestClientContext, IAsyncContext
        {
            public UnsubscribeTradesAsyncContext(bool waitable) : base(waitable)
            {
            }

            public void ProcessDisconnect(TradeCapture tradeCapture, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (tradeCapture.UnsubscribeTradesErrorEvent != null)
                {
                    try
                    {
                        tradeCapture.UnsubscribeTradesErrorEvent(tradeCapture, Data, exception);
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

        class TradeDownloadAsyncContext : TradeDownloadRequestClientContext, IAsyncContext
        {
            public TradeDownloadAsyncContext(bool waitable) : base(waitable)
            {
            }

            public void ProcessDisconnect(TradeCapture tradeCapture, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (tradeCapture.DownloadTradesErrorEvent != null)
                {
                    try
                    {
                        tradeCapture.DownloadTradesErrorEvent(tradeCapture, Data, exception);
                    }
                    catch
                    {
                    }
                }

                if (Waitable)
                {
                    enumerator_.SetError(exception);
                }
            }

            public TradeTransactionReport tradeTransactionReport_;
            public DownloadTradesEnumerator enumerator_;            
        }

        class CancelDownloadTradesAsyncContext : TradeDownloadCancelRequestClientContext, IAsyncContext
        {
            public CancelDownloadTradesAsyncContext(bool waitable) : base(waitable)
            {
            }

            public void ProcessDisconnect(TradeCapture tradeCapture, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (tradeCapture.CancelDownloadTradesErrorEvent != null)
                {
                    try
                    {
                        tradeCapture.CancelDownloadTradesErrorEvent(tradeCapture, Data, exception);
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

        class DownloadAccountReportsAsyncContext : AccountDownloadRequestClientContext, IAsyncContext
        {
            public DownloadAccountReportsAsyncContext(bool waitable) : base(waitable)
            {
            }

            public void ProcessDisconnect(TradeCapture tradeCapture, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (tradeCapture.DownloadAccountReportsErrorEvent != null)
                {
                    try
                    {
                        tradeCapture.DownloadAccountReportsErrorEvent(tradeCapture, Data, exception);
                    }
                    catch
                    {
                    }
                }

                if (Waitable)
                {
                    enumerator_.SetError(exception);
                }
            }

            public AccountReport accountReport_;
            public DownloadAccountReportsEnumerator enumerator_;
        }

        class CancelDownloadAccountReportsAsyncContext : AccountDownloadCancelRequestClientContext, IAsyncContext
        {
            public CancelDownloadAccountReportsAsyncContext(bool waitable) : base(waitable)
            {
            }

            public void ProcessDisconnect(TradeCapture tradeCapture, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (tradeCapture.CancelDownloadAccountReportsErrorEvent != null)
                {
                    try
                    {
                        tradeCapture.CancelDownloadAccountReportsErrorEvent(tradeCapture, Data, exception);
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

        class ClientSessionListener : SoftFX.Net.TradeCapture.ClientSessionListener
        {
            public ClientSessionListener(TradeCapture tradeCapture)
            {
                client_ = tradeCapture;
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
                    ConnectAsyncContext connectAsyncContext = (ConnectAsyncContext) connectContext;

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
                    // client_.session_.LogError(exception.Message);
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

            public override void OnTwoFactorLoginRequest(ClientSession session, LoginRequestClientContext LoginRequestClientContext, TwoFactorLogin message)
            {
                try
                {
                    LoginAsyncContext context = (LoginAsyncContext)LoginRequestClientContext;

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
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnTwoFactorLoginSuccess(ClientSession session, LoginRequestClientContext LoginRequestClientContext, TwoFactorLoginResponseClientContext TwoFactorLoginResponseClientContext, TwoFactorLogin message)
            {
                try
                {
                    LoginAsyncContext loginContext = (LoginAsyncContext)LoginRequestClientContext;

                    try
                    {
                        TwoFactorLoginResponseAsyncContext responseContext = (TwoFactorLoginResponseAsyncContext)TwoFactorLoginResponseClientContext;

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
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnTwoFactorLoginReject(ClientSession session, LoginRequestClientContext LoginRequestClientContext, TwoFactorLoginResponseClientContext TwoFactorLoginResponseClientContext, TwoFactorReject message)
            {
                try
                {
                    LoginAsyncContext loginContext = (LoginAsyncContext) LoginRequestClientContext;
                    TwoFactorLoginResponseAsyncContext responseContext = (TwoFactorLoginResponseAsyncContext)TwoFactorLoginResponseClientContext;

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

                    // the login procedure continues..
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnTwoFactorLoginError(ClientSession session, LoginRequestClientContext LoginRequestClientContext, TwoFactorLoginResponseClientContext TwoFactorLoginResponseClientContext, TwoFactorLogin message)
            {
                try
                {
                    LoginAsyncContext loginContext = (LoginAsyncContext)LoginRequestClientContext;
                    TwoFactorLoginResponseAsyncContext responseContext = (TwoFactorLoginResponseAsyncContext)TwoFactorLoginResponseClientContext;

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
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnTwoFactorLoginResume(ClientSession session, TwoFactorLoginResumeClientContext TwoFactorLoginResumeClientContext, TwoFactorLogin message)
            {
                try
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
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnLogout(ClientSession session, LogoutClientContext LogoutClientContext, Logout message)
            {
                try
                {
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

            public override void OnTradeSubscribeBeginReport(ClientSession session, TradeSubscribeRequestClientContext TradeSubscribeRequestClientContext, TradeSubscribeBeginReport message)
            {
                try
                {
                    SubscribeTradesAsyncContext context = (SubscribeTradesAsyncContext)TradeSubscribeRequestClientContext;

                    try
                    {
                        context.tradeTransactionReport_ = new TradeTransactionReport();

                        if (client_.SubscribeTradesResultBeginEvent != null)
                        {
                            try
                            {
                                client_.SubscribeTradesResultBeginEvent(client_, context.Data, message.TotalCount);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.enumerator_.SetBegin(message.TotalCount);
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.SubscribeTradesErrorEvent != null)
                        {
                            try
                            {
                                client_.SubscribeTradesErrorEvent(client_, context.Data, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.enumerator_.SetError(exception);
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnTradeSubscribeReport(ClientSession session, TradeSubscribeRequestClientContext TradeSubscribeRequestClientContext, TradeSubscribeReport message)
            {
                try
                {
                    SubscribeTradesAsyncContext context = (SubscribeTradesAsyncContext)TradeSubscribeRequestClientContext;

                    try
                    {
                        FillTradeTransactionReport(context.tradeTransactionReport_, message.Trade);

                        if (client_.SubscribeTradesResultEvent != null)
                        {
                            try
                            {
                                client_.SubscribeTradesResultEvent(client_, context.Data, context.tradeTransactionReport_);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            TradeTransactionReport tradeTransactionReport = context.tradeTransactionReport_.Clone();

                            context.enumerator_.SetResult(tradeTransactionReport);
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.SubscribeTradesErrorEvent != null)
                        {
                            try
                            {
                                client_.SubscribeTradesErrorEvent(client_, context.Data, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.enumerator_.SetError(exception);
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnTradeSubscribeEndReport(ClientSession session, TradeSubscribeRequestClientContext TradeSubscribeRequestClientContext, TradeSubscribeEndReport message)
            {
                try
                {
                    SubscribeTradesAsyncContext context = (SubscribeTradesAsyncContext)TradeSubscribeRequestClientContext;

                    try
                    {
                        if (client_.SubscribeTradesResultEndEvent != null)
                        {
                            try
                            {
                                client_.SubscribeTradesResultEndEvent(client_, context.Data);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.enumerator_.SetEnd();
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.SubscribeTradesErrorEvent != null)
                        {
                            try
                            {
                                client_.SubscribeTradesErrorEvent(client_, context.Data, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.enumerator_.SetError(exception);
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                } 
            }

            public override void OnTradeSubscribeReject(ClientSession session, TradeSubscribeRequestClientContext TradeSubscribeRequestClientContext, Reject message)
            {
                try
                {
                    SubscribeTradesAsyncContext context = (SubscribeTradesAsyncContext)TradeSubscribeRequestClientContext;

                    TickTrader.FDK.Common.RejectReason rejectReason = GetRejectReason(message.Reason);
                    RejectException exception = new RejectException(rejectReason, message.Text);

                    if (client_.SubscribeTradesErrorEvent != null)
                    {
                        try
                        {
                            client_.SubscribeTradesErrorEvent(client_, context.Data, exception);
                        }
                        catch
                        {
                        }
                    }

                    if (context.Waitable)
                    {
                        context.enumerator_.SetError(exception);
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnTradeUnsubscribeReport(ClientSession session, TradeUnsubscribeRequestClientContext TradeUnsubscribeRequestClientContext, TradeUnsubscribeReport message)
            {
                try
                {
                    UnsubscribeTradesAsyncContext context = (UnsubscribeTradesAsyncContext) TradeUnsubscribeRequestClientContext;

                    try
                    {
                        if (client_.UnsubscribeTradesResultEvent != null)
                        {
                            try
                            {
                                client_.UnsubscribeTradesResultEvent(client_, context.Data);
                            }
                            catch
                            {
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.UnsubscribeTradesErrorEvent != null)
                        {
                            try
                            {
                                client_.UnsubscribeTradesErrorEvent(client_, context.Data, exception);
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

            public override void OnTradeUnsubscribeReject(ClientSession session, TradeUnsubscribeRequestClientContext TradeUnsubscribeRequestClientContext, Reject message)
            {
                try
                {
                    UnsubscribeTradesAsyncContext context = (UnsubscribeTradesAsyncContext)TradeUnsubscribeRequestClientContext;

                    TickTrader.FDK.Common.RejectReason rejectReason = GetRejectReason(message.Reason);
                    RejectException exception = new RejectException(rejectReason, message.Text);

                    if (client_.UnsubscribeTradesErrorEvent != null)
                    {
                        try
                        {
                            client_.UnsubscribeTradesErrorEvent(client_, context.Data, exception);
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

            public override void OnTradeDownloadBeginReport(ClientSession session, TradeDownloadRequestClientContext TradeDownloadRequestClientContext, TradeDownloadBeginReport message)
            {
                try
                {
                    TradeDownloadAsyncContext context = (TradeDownloadAsyncContext) TradeDownloadRequestClientContext;

                    try
                    {
                        context.tradeTransactionReport_ = new TradeTransactionReport();

                        if (client_.DownloadTradesResultBeginEvent != null)
                        {
                            try
                            {
                                client_.DownloadTradesResultBeginEvent(client_, context.Data, message.RequestId, message.TotalCount);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.enumerator_.SetBegin(message.RequestId, message.TotalCount);
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.DownloadTradesErrorEvent != null)
                        {
                            try
                            {
                                client_.DownloadTradesErrorEvent(client_, context.Data, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.enumerator_.SetError(exception);
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnTradeDownloadReport(ClientSession session, TradeDownloadRequestClientContext TradeDownloadRequestClientContext, TradeDownloadReport message)
            {
                try
                {
                    TradeDownloadAsyncContext context = (TradeDownloadAsyncContext) TradeDownloadRequestClientContext;

                    try
                    {
                        FillTradeTransactionReport(context.tradeTransactionReport_, message.Trade);

                        if (client_.DownloadTradesResultEvent != null)
                        {
                            try
                            {
                                client_.DownloadTradesResultEvent(client_, context.Data, context.tradeTransactionReport_);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            TradeTransactionReport tradeTransactionReport = context.tradeTransactionReport_.Clone();

                            context.enumerator_.SetResult(tradeTransactionReport);
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.DownloadTradesErrorEvent != null)
                        {
                            try
                            {
                                client_.DownloadTradesErrorEvent(client_, context.Data, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.enumerator_.SetError(exception);
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnTradeDownloadEndReport(ClientSession session, TradeDownloadRequestClientContext TradeDownloadRequestClientContext, TradeDownloadEndReport message)
            {
                try
                {
                    TradeDownloadAsyncContext context = (TradeDownloadAsyncContext) TradeDownloadRequestClientContext;

                    try
                    {
                        if (client_.DownloadTradesResultEndEvent != null)
                        {
                            try
                            {
                                client_.DownloadTradesResultEndEvent(client_, context.Data);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.enumerator_.SetEnd();
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.DownloadTradesErrorEvent != null)
                        {
                            try
                            {
                                client_.DownloadTradesErrorEvent(client_, context.Data, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.enumerator_.SetError(exception);
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnTradeDownloadReject(ClientSession session, TradeDownloadRequestClientContext TradeDownloadRequestClientContext, Reject message)
            {
                try
                {
                    TradeDownloadAsyncContext context = (TradeDownloadAsyncContext)TradeDownloadRequestClientContext;

                    TickTrader.FDK.Common.RejectReason rejectReason = GetRejectReason(message.Reason);
                    RejectException exception = new RejectException(rejectReason, message.Text);

                    if (client_.DownloadTradesErrorEvent != null)
                    {
                        try
                        {
                            client_.DownloadTradesErrorEvent(client_, context.Data, exception);
                        }
                        catch
                        {
                        }
                    }

                    if (context.Waitable)
                    {
                        context.enumerator_.SetError(exception);
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnTradeDownloadCancelReport(ClientSession session, TradeDownloadCancelRequestClientContext TradeDownloadCancelRequestClientContext, TradeDownloadCancelReport message)
            {
                try
                {
                    CancelDownloadTradesAsyncContext context = (CancelDownloadTradesAsyncContext) TradeDownloadCancelRequestClientContext;

                    try
                    {
                        if (client_.CancelDownloadTradesResultEvent != null)
                        {
                            try
                            {
                                client_.CancelDownloadTradesResultEvent(client_, context.Data);
                            }
                            catch
                            {
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.CancelDownloadTradesErrorEvent != null)
                        {
                            try
                            {
                                client_.CancelDownloadTradesErrorEvent(client_, context.Data, exception);
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

            public override void OnTradeDownloadCancelReject(ClientSession session, TradeDownloadCancelRequestClientContext TradeDownloadCancelRequestClientContext, Reject message)
            {
                try
                {
                    CancelDownloadTradesAsyncContext context = (CancelDownloadTradesAsyncContext) TradeDownloadCancelRequestClientContext;

                    Common.RejectReason rejectReason = GetRejectReason(message.Reason);
                    RejectException exception = new RejectException(rejectReason, message.Text);

                    if (client_.CancelDownloadTradesErrorEvent != null)
                    {
                        try
                        {
                            client_.CancelDownloadTradesErrorEvent(client_, context.Data, exception);
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

            public override void OnAccountDownloadBeginReport(ClientSession session, AccountDownloadRequestClientContext AccountDownloadRequestClientContext, AccountDownloadBeginReport message)
            {
                try
                {
                    DownloadAccountReportsAsyncContext context = (DownloadAccountReportsAsyncContext) AccountDownloadRequestClientContext;

                    try
                    {
                        context.accountReport_ = new AccountReport();

                        if (client_.DownloadAccountReportsResultBeginEvent != null)
                        {
                            try
                            {
                                client_.DownloadAccountReportsResultBeginEvent(client_, context.Data, message.RequestId, message.TotalCount);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.enumerator_.SetBegin(message.RequestId, message.TotalCount);
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.DownloadAccountReportsErrorEvent != null)
                        {
                            try
                            {
                                client_.DownloadAccountReportsErrorEvent(client_, context.Data, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.enumerator_.SetError(exception);
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnAccountDownloadReport(ClientSession session, AccountDownloadRequestClientContext AccountDownloadRequestClientContext, AccountDownloadReport message)
            {
                try
                {
                    DownloadAccountReportsAsyncContext context = (DownloadAccountReportsAsyncContext) AccountDownloadRequestClientContext;

                    try
                    {
                        FillAccountReport(context.accountReport_, message.Account);

                        if (client_.DownloadAccountReportsResultEvent != null)
                        {
                            try
                            {
                                client_.DownloadAccountReportsResultEvent(client_, context.Data, context.accountReport_);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            AccountReport accountReportClone = context.accountReport_.Clone();

                            context.enumerator_.SetResult(accountReportClone);
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.DownloadAccountReportsErrorEvent != null)
                        {
                            try
                            {
                                client_.DownloadAccountReportsErrorEvent(client_, context.Data, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.enumerator_.SetError(exception);
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnAccountDownloadEndReport(ClientSession session, AccountDownloadRequestClientContext AccountDownloadRequestClientContext, AccountDownloadEndReport message)
            {
                try
                {
                    DownloadAccountReportsAsyncContext context = (DownloadAccountReportsAsyncContext) AccountDownloadRequestClientContext;

                    try
                    {
                        if (client_.DownloadAccountReportsResultEndEvent != null)
                        {
                            try
                            {
                                client_.DownloadAccountReportsResultEndEvent(client_, context.Data);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.enumerator_.SetEnd();
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.DownloadAccountReportsErrorEvent != null)
                        {
                            try
                            {
                                client_.DownloadAccountReportsErrorEvent(client_, context.Data, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.enumerator_.SetError(exception);
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnAccountDownloadReject(ClientSession session, AccountDownloadRequestClientContext AccountDownloadRequestClientContext, Reject message)
            {
                try
                {
                    DownloadAccountReportsAsyncContext context = (DownloadAccountReportsAsyncContext)AccountDownloadRequestClientContext;

                    TickTrader.FDK.Common.RejectReason rejectReason = GetRejectReason(message.Reason);
                    RejectException exception = new RejectException(rejectReason, message.Text);

                    if (client_.DownloadAccountReportsErrorEvent != null)
                    {
                        try
                        {
                            client_.DownloadAccountReportsErrorEvent(client_, context.Data, exception);
                        }
                        catch
                        {
                        }
                    }

                    if (context.Waitable)
                    {
                        context.enumerator_.SetError(exception);
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnAccountDownloadCancelReport(ClientSession session, AccountDownloadCancelRequestClientContext AccountDownloadCancelRequestClientContext, AccountDownloadCancelReport message)
            {
                try
                {
                    CancelDownloadAccountReportsAsyncContext context = (CancelDownloadAccountReportsAsyncContext) AccountDownloadCancelRequestClientContext;

                    try
                    {
                        if (client_.CancelDownloadAccountReportsResultEvent != null)
                        {
                            try
                            {
                                client_.CancelDownloadAccountReportsResultEvent(client_, context.Data);
                            }
                            catch
                            {
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.CancelDownloadAccountReportsErrorEvent != null)
                        {
                            try
                            {
                                client_.CancelDownloadAccountReportsErrorEvent(client_, context.Data, exception);
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

            public override void OnAccountDownloadCancelReject(ClientSession session, AccountDownloadCancelRequestClientContext AccountDownloadCancelRequestClientContext, Reject message)
            {
                try
                {
                    CancelDownloadAccountReportsAsyncContext context = (CancelDownloadAccountReportsAsyncContext) AccountDownloadCancelRequestClientContext;

                    Common.RejectReason rejectReason = GetRejectReason(message.Reason);
                    RejectException exception = new RejectException(rejectReason, message.Text);

                    if (client_.CancelDownloadAccountReportsErrorEvent != null)
                    {
                        try
                        {
                            client_.CancelDownloadAccountReportsErrorEvent(client_, context.Data, exception);
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

            public override void OnTradeUpdateReport(ClientSession session, TradeUpdateReport message)
            {
                try
                {
                    TradeTransactionReport tradeTransactionReport = new TradeTransactionReport();

                    FillTradeTransactionReport(tradeTransactionReport, message.Trade);

                    if (client_.TradeUpdateEvent != null)
                    {
                        try
                        {
                            client_.TradeUpdateEvent(client_, tradeTransactionReport);
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

            public override void OnNotification(ClientSession session, SoftFX.Net.TradeCapture.Notification message)
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

            void FillTradeTransactionReport(TickTrader.FDK.Common.TradeTransactionReport tradeTransactionReport, SoftFX.Net.TradeCapture.Trade trade)
            {
                tradeTransactionReport.TradeTransactionId = trade.Id;
                tradeTransactionReport.TradeTransactionReportType = GetTradeTransactionReportType(trade.Type);
                tradeTransactionReport.TradeTransactionReason = GetTradeTransactionReason(trade.Reason);

                TradeBalanceNull balance = trade.Balance;
                if (balance.HasValue)
                {
                    tradeTransactionReport.AccountBalance = balance.Total;
                    tradeTransactionReport.TransactionAmount = balance.Move;
                    tradeTransactionReport.TransactionCurrency = balance.CurrId;
                }
                else
                {
                    tradeTransactionReport.AccountBalance = 0;
                    tradeTransactionReport.TransactionAmount = 0;
                    tradeTransactionReport.TransactionCurrency = null;
                }

                tradeTransactionReport.Id = trade.OrderId.GetValueOrDefault(0).ToString();
                tradeTransactionReport.ClientId = trade.ClOrdId;
                tradeTransactionReport.Quantity = trade.Qty.GetValueOrDefault();
                tradeTransactionReport.MaxVisibleQuantity = trade.MaxVisibleQty;
                tradeTransactionReport.LeavesQuantity = trade.LeavesQty.GetValueOrDefault();
                tradeTransactionReport.Price = trade.Price.GetValueOrDefault();
                tradeTransactionReport.StopPrice = trade.StopPrice.GetValueOrDefault();

                if (trade.OrderType.HasValue)
                {
                    tradeTransactionReport.OrderType = GetOrderType(trade.OrderType.Value);
                }
                else
                    tradeTransactionReport.OrderType = TickTrader.FDK.Common.OrderType.Market;

                if (trade.ParentOrderType.HasValue)
                {
                    tradeTransactionReport.ReqOrderType = GetOrderType(trade.ParentOrderType.Value);
                }
                else
                    tradeTransactionReport.ReqOrderType = null;

                if (trade.OrderSide.HasValue)
                {
                    tradeTransactionReport.OrderSide = GetOrderSide(trade.OrderSide.Value);
                }
                else
                    tradeTransactionReport.OrderSide = TickTrader.FDK.Common.OrderSide.Buy;

                if (trade.TimeInForce.HasValue)
                {
                    tradeTransactionReport.TimeInForce = GetOrderTimeInForce(trade.TimeInForce.Value);
                }
                else
                    tradeTransactionReport.TimeInForce = null;

                tradeTransactionReport.Symbol = trade.SymbolId;
                tradeTransactionReport.Comment = trade.Comment;
                tradeTransactionReport.Tag = trade.Tag;
                tradeTransactionReport.Magic = trade.Magic;
                tradeTransactionReport.ReducedOpenCommission = (trade.CommissionFlags & CommissionFlags.OpenReduced) != 0;
                tradeTransactionReport.ReducedCloseCommission = (trade.CommissionFlags & CommissionFlags.CloseReduced) != 0;
                tradeTransactionReport.MarketWithSlippage = (trade.OrderFlags & OrderFlags.Slippage) != 0;
                tradeTransactionReport.OrderCreated = trade.Created.GetValueOrDefault();
                tradeTransactionReport.OrderModified = trade.Modified.GetValueOrDefault();

                if (trade.PosId.HasValue)
                {
                    tradeTransactionReport.PositionId = trade.PosId.ToString();
                }
                else
                    tradeTransactionReport.PositionId = null;

                if (trade.PosById.HasValue)
                {
                    tradeTransactionReport.PositionById = trade.PosById.ToString();
                }
                else
                    tradeTransactionReport.PositionById = null;

                tradeTransactionReport.PositionOpened = trade.PosOpened.GetValueOrDefault();
                tradeTransactionReport.PosOpenPrice = trade.PosOpenPrice.GetValueOrDefault();
                tradeTransactionReport.PositionQuantity = trade.PosQty.GetValueOrDefault();
                tradeTransactionReport.PositionLastQuantity = trade.PosLastQty.GetValueOrDefault();
                tradeTransactionReport.PositionLeavesQuantity = trade.PosLeavesQty.GetValueOrDefault();
                tradeTransactionReport.PositionClosePrice = trade.PosClosePrice.GetValueOrDefault();
                tradeTransactionReport.PositionClosed = trade.PosClosed.GetValueOrDefault();
                tradeTransactionReport.PosRemainingPrice = trade.PosPrice;

                if (trade.PosType.HasValue)
                {
                    tradeTransactionReport.PosRemainingSide = GetOrderSide(trade.PosType.Value);
                }
                else
                    tradeTransactionReport.PosRemainingSide = Common.OrderSide.Buy;
                
                tradeTransactionReport.ReqOpenQuantity = trade.ReqOpenQty;
                tradeTransactionReport.ReqOpenPrice = trade.ReqOpenPrice;
                tradeTransactionReport.ReqClosePrice = trade.ReqClosePrice;
                tradeTransactionReport.ReqCloseQuantity = trade.ReqCloseQty;               
                tradeTransactionReport.Commission = trade.Commission.GetValueOrDefault();
                tradeTransactionReport.AgentCommission = trade.AgentCommission.GetValueOrDefault();
                tradeTransactionReport.Swap = trade.Swap.GetValueOrDefault();                
                tradeTransactionReport.CommCurrency = trade.CommissionCurrId;
                tradeTransactionReport.StopLoss = trade.StopLoss.GetValueOrDefault();
                tradeTransactionReport.TakeProfit = trade.TakeProfit.GetValueOrDefault();
                tradeTransactionReport.TransactionTime = trade.TransactTime;
                tradeTransactionReport.OrderFillPrice = trade.LastPrice;
                tradeTransactionReport.OrderLastFillAmount = trade.LastQty;
                tradeTransactionReport.ActionId = trade.ActionId.GetValueOrDefault();
                tradeTransactionReport.Expiration = trade.ExpireTime;
                tradeTransactionReport.MarginCurrency = trade.MarginCurrId;
                tradeTransactionReport.ProfitCurrency = trade.ProfitCurrId;
                tradeTransactionReport.MinCommissionCurrency = trade.MinCommissionCurrId;

                TradeAssetNull asset1 = trade.Asset1;
                if (asset1.HasValue)
                {
                    tradeTransactionReport.SrcAssetCurrency = asset1.CurrId;
                    tradeTransactionReport.SrcAssetAmount = asset1.Total;
                    tradeTransactionReport.SrcAssetMovement = asset1.Move;
                }
                else
                {
                    tradeTransactionReport.SrcAssetCurrency = null;
                    tradeTransactionReport.SrcAssetAmount = null;
                    tradeTransactionReport.SrcAssetMovement = null;
                }

                TradeAssetNull asset2 = trade.Asset2;
                if (asset2.HasValue)
                {
                    tradeTransactionReport.DstAssetCurrency = asset2.CurrId;
                    tradeTransactionReport.DstAssetAmount = asset2.Total;
                    tradeTransactionReport.DstAssetMovement = asset2.Move;
                }
                else
                {
                    tradeTransactionReport.DstAssetCurrency = null;
                    tradeTransactionReport.DstAssetAmount = null;
                    tradeTransactionReport.DstAssetMovement = null;
                }

                tradeTransactionReport.OpenConversionRate = null;
                tradeTransactionReport.CloseConversionRate = null;
                tradeTransactionReport.MarginCurrencyToUsdConversionRate = null;
                tradeTransactionReport.UsdToMarginCurrencyConversionRate = null;
                tradeTransactionReport.ProfitCurrencyToUsdConversionRate = null;
                tradeTransactionReport.UsdToProfitCurrencyConversionRate = null;
                tradeTransactionReport.SrcAssetToUsdConversionRate = null;
                tradeTransactionReport.UsdToSrcAssetConversionRate = null;
                tradeTransactionReport.DstAssetToUsdConversionRate = null;
                tradeTransactionReport.UsdToDstAssetConversionRate = null;
                tradeTransactionReport.MinCommissionConversionRate = null;

                ConversionArray conversions = trade.Conversions;
                int conversionCount = conversions.Length;

                for (int conversionIndex = 0; conversionIndex < conversionCount; ++conversionIndex)
                {
                    Conversion conversion = conversions[conversionIndex];

                    switch (conversion.Type)
                    {
                        case ConversionType.MarginToBalance:
                            tradeTransactionReport.OpenConversionRate = conversion.Rate;
                            break;

                        case ConversionType.MarginToUsd:
                            tradeTransactionReport.MarginCurrencyToUsdConversionRate = conversion.Rate;
                            break;

                        case ConversionType.ProfitToBalance:
                            tradeTransactionReport.CloseConversionRate = conversion.Rate;
                            break;

                        case ConversionType.ProfitToUsd:
                            tradeTransactionReport.ProfitCurrencyToUsdConversionRate = conversion.Rate;
                            break;

                        case ConversionType.Asset1ToUsd:
                            tradeTransactionReport.SrcAssetToUsdConversionRate = conversion.Rate;
                            break;

                        case ConversionType.Asset2ToUsd:
                            tradeTransactionReport.DstAssetToUsdConversionRate = conversion.Rate;
                            break;

                        case ConversionType.MinCommissionToBalance:
                            tradeTransactionReport.MinCommissionConversionRate = conversion.Rate;
                            break;

                        case ConversionType.UsdToMargin:
                            tradeTransactionReport.UsdToMarginCurrencyConversionRate = conversion.Rate;
                            break;

                        case ConversionType.UsdToProfit:
                            tradeTransactionReport.UsdToProfitCurrencyConversionRate = conversion.Rate;
                            break;

                        case ConversionType.UsdToAsset1:
                            tradeTransactionReport.UsdToSrcAssetConversionRate = conversion.Rate;
                            break;

                        case ConversionType.UsdToAsset2:
                            tradeTransactionReport.UsdToDstAssetConversionRate = conversion.Rate;
                            break;
                    }
                }
            }

            void FillAccountReport(TickTrader.FDK.Common.AccountReport accountReport, SoftFX.Net.TradeCapture.Account account)
            {
                accountReport.Timestamp = account.TransactTime;
                accountReport.AccountId = account.Id.ToString();
                accountReport.Type = GetAccountType(account.Type);
                accountReport.Leverage = account.Leverage;

                AccountBalanceNull balance = account.Balance;

                if (balance.HasValue)
                {
                    accountReport.BalanceCurrency = balance.CurrId;
                    accountReport.Balance = balance.Total;
                }
                else
                {
                    accountReport.BalanceCurrency = null;
                    accountReport.Balance = 0;
                }

                accountReport.Profit = account.Profit;
                accountReport.Commission = account.Commission;
                accountReport.AgentCommission = account.AgentCommission;
                accountReport.Swap = account.Swap;
                accountReport.Equity = account.Equity;
                accountReport.Margin = account.Margin;
                accountReport.MarginLevel = account.MarginLevel;
                AccountFlags accountFlags = account.Flags;
                accountReport.IsBlocked = (accountFlags & AccountFlags.Blocked) != 0;
                accountReport.IsReadOnly = (accountFlags & AccountFlags.Investor) != 0;
                accountReport.IsValid = (accountFlags & AccountFlags.Valid) != 0;

                AccountPositionArray positions = account.Positions;
                int positionCount = positions.Length;

                accountReport.Positions = new TickTrader.FDK.Common.Position[positionCount];

                for (int index = 0; index < positionCount; ++index)
                {
                    SoftFX.Net.TradeCapture.AccountPosition position = positions[index];
                    TickTrader.FDK.Common.Position accountPosition = new TickTrader.FDK.Common.Position();

                    accountPosition.Symbol = position.SymbolId;
                    if (position.Type == PosType.Long)
                    {
                        accountPosition.BuyAmount = position.Qty;
                        accountPosition.BuyPrice = position.Price;
                        accountPosition.SellAmount = 0;
                        accountPosition.SellPrice = 0;
                    }
                    else
                    {
                        accountPosition.BuyAmount = 0;
                        accountPosition.BuyPrice = 0;
                        accountPosition.SellAmount = position.Qty;
                        accountPosition.SellPrice = position.Price;
                    }
                    accountPosition.Margin = position.Margin;
                    accountPosition.Profit = position.Profit;                    
                    accountPosition.Swap = position.Swap;
                    accountPosition.Commission = position.Commission;
                    accountPosition.Modified = position.Modified;
                    accountPosition.BidPrice = position.Bid;
                    accountPosition.AskPrice = position.Ask;

                    accountReport.Positions[index] = accountPosition;
                }

                AccountAssetArray assets = account.Assets;
                int assetCount = assets.Length;

                accountReport.Assets = new AssetInfo[assetCount];

                for (int index = 0; index < assetCount; ++ index)
                {
                    AccountAsset asset = assets[index];
                    AssetInfo assetInfo = new AssetInfo();

                    assetInfo.Currency = asset.CurrId;
                    assetInfo.LockedAmount = asset.Locked;
                    assetInfo.Balance = asset.Total;

                    accountReport.Assets[index] = assetInfo;
                }

                accountReport.BalanceCurrencyToUsdConversionRate = null;
                accountReport.UsdToBalanceCurrencyConversionRate = null;
                accountReport.ProfitCurrencyToUsdConversionRate = null;
                accountReport.UsdToProfitCurrencyConversionRate = null;

                ConversionArray conversions = account.Conversions;
                int conversionCount = conversions.Length;

                for (int conversionIndex = 0; conversionIndex < conversionCount; ++conversionIndex)
                {
                    Conversion conversion = conversions[conversionIndex];

                    switch (conversion.Type)
                    {
                        case ConversionType.ProfitToUsd:
                            accountReport.ProfitCurrencyToUsdConversionRate = conversion.Rate;
                            break;

                        case ConversionType.UsdToProfit:
                            accountReport.UsdToProfitCurrencyConversionRate = conversion.Rate;
                            break;

                        case ConversionType.BalanceToUsd:
                            accountReport.BalanceCurrencyToUsdConversionRate = conversion.Rate;
                            break;

                        case ConversionType.UsdToBalance:
                            accountReport.UsdToBalanceCurrencyConversionRate = conversion.Rate;
                            break;
                    }
                }
            }

            TickTrader.FDK.Common.RejectReason GetRejectReason(SoftFX.Net.TradeCapture.RejectReason reason)
            {
                switch (reason)
                {
                    case SoftFX.Net.TradeCapture.RejectReason.ThrottlingLimits:
                        return Common.RejectReason.ThrottlingLimits;

                    case SoftFX.Net.TradeCapture.RejectReason.RequestCancelled:
                        return Common.RejectReason.RequestCancelled;

                    case SoftFX.Net.TradeCapture.RejectReason.InternalServerError:
                        return Common.RejectReason.InternalServerError;

                    case SoftFX.Net.TradeCapture.RejectReason.Other:
                        return Common.RejectReason.Other;

                    default:
                        throw new Exception("Invalid reject reason : " + reason);
                }
            }

            TickTrader.FDK.Common.LogoutReason GetLogoutReason(SoftFX.Net.TradeCapture.LoginRejectReason reason)
            {
                switch (reason)
                {
                    case SoftFX.Net.TradeCapture.LoginRejectReason.IncorrectCredentials:
                        return TickTrader.FDK.Common.LogoutReason.InvalidCredentials;

                    case SoftFX.Net.TradeCapture.LoginRejectReason.ThrottlingLimits:
                        return TickTrader.FDK.Common.LogoutReason.Unknown;

                    case SoftFX.Net.TradeCapture.LoginRejectReason.BlockedLogin:
                        return TickTrader.FDK.Common.LogoutReason.BlockedAccount;

                    case SoftFX.Net.TradeCapture.LoginRejectReason.InternalServerError:
                        return TickTrader.FDK.Common.LogoutReason.ServerError;

                    case SoftFX.Net.TradeCapture.LoginRejectReason.Other:
                        return TickTrader.FDK.Common.LogoutReason.Unknown;

                    default:
                        throw new Exception("Invalid login reject reason : " + reason);
                }
            }

            TickTrader.FDK.Common.LogoutReason GetLogoutReason(SoftFX.Net.TradeCapture.LogoutReason reason)
            {
                switch (reason)
                {
                    case SoftFX.Net.TradeCapture.LogoutReason.ClientLogout:
                        return TickTrader.FDK.Common.LogoutReason.ClientInitiated;

                    case SoftFX.Net.TradeCapture.LogoutReason.ServerLogout:
                        return TickTrader.FDK.Common.LogoutReason.ServerLogout;

                    case SoftFX.Net.TradeCapture.LogoutReason.DeletedLogin:
                        return TickTrader.FDK.Common.LogoutReason.LoginDeleted;

                    case SoftFX.Net.TradeCapture.LogoutReason.InternalServerError:
                        return TickTrader.FDK.Common.LogoutReason.ServerError;

                    case SoftFX.Net.TradeCapture.LogoutReason.BlockedLogin:
                        return TickTrader.FDK.Common.LogoutReason.BlockedAccount;

                    case SoftFX.Net.TradeCapture.LogoutReason.Other:
                        return TickTrader.FDK.Common.LogoutReason.Unknown;

                    default:
                        throw new Exception("Invalid logout reason : " + reason);
                }
            }

            TickTrader.FDK.Common.TradeTransactionReportType GetTradeTransactionReportType(SoftFX.Net.TradeCapture.TradeType type)
            {
                switch (type)
                {
                    case SoftFX.Net.TradeCapture.TradeType.OrderFilled:
                        return TickTrader.FDK.Common.TradeTransactionReportType.OrderFilled;

                    case SoftFX.Net.TradeCapture.TradeType.PositionClosed:
                        return TickTrader.FDK.Common.TradeTransactionReportType.PositionClosed;

                    case SoftFX.Net.TradeCapture.TradeType.OrderCancelled:
                        return TickTrader.FDK.Common.TradeTransactionReportType.OrderCanceled;

                    case SoftFX.Net.TradeCapture.TradeType.OrderExpired:
                        return TickTrader.FDK.Common.TradeTransactionReportType.OrderExpired;

                    case SoftFX.Net.TradeCapture.TradeType.OrderActivated:
                        return TickTrader.FDK.Common.TradeTransactionReportType.OrderActivated;

                    case SoftFX.Net.TradeCapture.TradeType.Balance:
                        return TickTrader.FDK.Common.TradeTransactionReportType.BalanceTransaction;

                    default:
                        throw new Exception("Invalid trade type : " + type);
                }
            }

            TickTrader.FDK.Common.TradeTransactionReason GetTradeTransactionReason(SoftFX.Net.TradeCapture.TradeReason reason)
            {
                switch (reason)
                {
                    case SoftFX.Net.TradeCapture.TradeReason.ClientRequest:
                        return TickTrader.FDK.Common.TradeTransactionReason.ClientRequest;

                    case SoftFX.Net.TradeCapture.TradeReason.OrderActivated:
                        return TickTrader.FDK.Common.TradeTransactionReason.PendingOrderActivation;

                    case SoftFX.Net.TradeCapture.TradeReason.TakeProfitActivated:
                        return TickTrader.FDK.Common.TradeTransactionReason.TakeProfitActivation;

                    case SoftFX.Net.TradeCapture.TradeReason.StopLossActivated:
                        return TickTrader.FDK.Common.TradeTransactionReason.StopLossActivation;

                    case SoftFX.Net.TradeCapture.TradeReason.OrderExpired:
                        return TickTrader.FDK.Common.TradeTransactionReason.Expired;

                    case SoftFX.Net.TradeCapture.TradeReason.Dealer:
                        return TickTrader.FDK.Common.TradeTransactionReason.DealerDecision;

                    case SoftFX.Net.TradeCapture.TradeReason.StopOut:
                        return TickTrader.FDK.Common.TradeTransactionReason.StopOut;


                    case SoftFX.Net.TradeCapture.TradeReason.Rollover:
                        return TickTrader.FDK.Common.TradeTransactionReason.Rollover;

                    case SoftFX.Net.TradeCapture.TradeReason.AccountDeleted:
                        return TickTrader.FDK.Common.TradeTransactionReason.DeleteAccount;

                    default:
                        throw new Exception("Invalid trade reason : " + reason);
                }
            }

            TickTrader.FDK.Common.OrderType GetOrderType(SoftFX.Net.TradeCapture.OrderType type)
            {
                switch (type)
                {
                    case SoftFX.Net.TradeCapture.OrderType.Market:
                        return TickTrader.FDK.Common.OrderType.Market;

                    case SoftFX.Net.TradeCapture.OrderType.Limit:
                        return TickTrader.FDK.Common.OrderType.Limit;

                    case SoftFX.Net.TradeCapture.OrderType.Stop:
                        return TickTrader.FDK.Common.OrderType.Stop;

                    case SoftFX.Net.TradeCapture.OrderType.Position:
                        return TickTrader.FDK.Common.OrderType.Position;

                    case SoftFX.Net.TradeCapture.OrderType.StopLimit:
                        return TickTrader.FDK.Common.OrderType.StopLimit;

                    default:
                        throw new Exception("Invalid order type : " + type);
                }
            }

            TickTrader.FDK.Common.OrderSide GetOrderSide(SoftFX.Net.TradeCapture.OrderSide side)
            {
                switch (side)
                {
                    case SoftFX.Net.TradeCapture.OrderSide.Buy:
                        return TickTrader.FDK.Common.OrderSide.Buy;

                    case SoftFX.Net.TradeCapture.OrderSide.Sell:
                        return TickTrader.FDK.Common.OrderSide.Sell;

                    default:
                        throw new Exception("Invalid order side : " + side);
                }
            }

            TickTrader.FDK.Common.OrderTimeInForce GetOrderTimeInForce(SoftFX.Net.TradeCapture.OrderTimeInForce timeInForce)
            {
                switch (timeInForce)
                {
                    case SoftFX.Net.TradeCapture.OrderTimeInForce.GoodTillCancel:
                        return TickTrader.FDK.Common.OrderTimeInForce.GoodTillCancel;

                    case SoftFX.Net.TradeCapture.OrderTimeInForce.ImmediateOrCancel:
                        return TickTrader.FDK.Common.OrderTimeInForce.ImmediateOrCancel;

                    case SoftFX.Net.TradeCapture.OrderTimeInForce.GoodTillDate:
                        return TickTrader.FDK.Common.OrderTimeInForce.GoodTillDate;

                    default:
                        throw new Exception("Invalid order time in force : " + timeInForce);
                }
            }

            TickTrader.FDK.Common.OrderSide GetOrderSide(SoftFX.Net.TradeCapture.PosType posType)
            {
                switch (posType)
                {
                    case SoftFX.Net.TradeCapture.PosType.Long:
                        return TickTrader.FDK.Common.OrderSide.Buy;

                    case SoftFX.Net.TradeCapture.PosType.Short:
                        return TickTrader.FDK.Common.OrderSide.Sell;

                    default:
                        throw new Exception("Invalid position type : " + posType);
                }
            }

            TickTrader.FDK.Common.NotificationType GetNotificationType(SoftFX.Net.TradeCapture.NotificationType type)
            {
                switch (type)
                {
                    case SoftFX.Net.TradeCapture.NotificationType.ConfigUpdate:
                        return TickTrader.FDK.Common.NotificationType.ConfigUpdated;

                    default:
                        throw new Exception("Invalid notification type : " + type);
                }
            }

            TickTrader.FDK.Common.NotificationSeverity GetNotificationSeverity(SoftFX.Net.TradeCapture.NotificationSeverity severity)
            {
                switch (severity)
                {
                    case SoftFX.Net.TradeCapture.NotificationSeverity.Info:
                        return TickTrader.FDK.Common.NotificationSeverity.Information;

                    case SoftFX.Net.TradeCapture.NotificationSeverity.Warning:
                        return TickTrader.FDK.Common.NotificationSeverity.Warning;

                    case SoftFX.Net.TradeCapture.NotificationSeverity.Error:
                        return TickTrader.FDK.Common.NotificationSeverity.Error;

                    default:
                        throw new Exception("Invalid notification severity : " + severity);
                }
            }

            TickTrader.FDK.Common.AccountType GetAccountType(SoftFX.Net.TradeCapture.AccountType type)
            {
                switch (type)
                {
                    case SoftFX.Net.TradeCapture.AccountType.Gross:
                        return TickTrader.FDK.Common.AccountType.Gross;

                    case SoftFX.Net.TradeCapture.AccountType.Net:
                        return TickTrader.FDK.Common.AccountType.Net;

                    case SoftFX.Net.TradeCapture.AccountType.Cash:
                        return TickTrader.FDK.Common.AccountType.Cash;

                    default:
                        throw new Exception("Invalid account type : " + type);
                }
            }

            TradeCapture client_;
        }

        #endregion
    }
}
