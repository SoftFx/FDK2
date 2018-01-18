using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using SoftFX.Net.TradeCapture;
using TickTrader.FDK.Common;

namespace TickTrader.FDK.TradeCapture
{
    public class Client : IDisposable
    {
        #region Constructors

        public Client
        (
            string name,
            bool logMessages =  false,
            int port = 5060,
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

        #region Trade Capture
        
        public delegate void SubscribeTradesResultDelegate(Client client, object data);
        public delegate void SubscribeTradesErrorDelegate(Client client, object data, Exception exception);
        public delegate void UnsubscribeTradesResultDelegate(Client client, object data);
        public delegate void UnsubscribeTradesErrorDelegate(Client client, object data, Exception exception);
        public delegate void TradeDownloadResultBeginDelegate(Client client, object data);
        public delegate void TradeDownloadResultDelegate(Client client, object data, TickTrader.FDK.Common.TradeTransactionReport tradeTransactionReport);
        public delegate void TradeDownloadResultEndDelegate(Client client, object data);
        public delegate void TradeDownloadErrorDelegate(Client client, object data, Exception exception);
        public delegate void TradeUpdateDelegate(Client client, TickTrader.FDK.Common.TradeTransactionReport tradeTransactionReport);
        public delegate void NotificationDelegate(Client client, TickTrader.FDK.Common.Notification notification);
        
        public event SubscribeTradesResultDelegate SubscribeTradesResultEvent;
        public event SubscribeTradesErrorDelegate SubscribeTradesErrorEvent;
        public event UnsubscribeTradesResultDelegate UnsubscribeTradesResultEvent;
        public event UnsubscribeTradesErrorDelegate UnsubscribeTradesErrorEvent;
        public event TradeDownloadResultBeginDelegate TradeDownloadResultBeginEvent;
        public event TradeDownloadResultDelegate TradeDownloadResultEvent;
        public event TradeDownloadResultEndDelegate TradeDownloadResultEndEvent;
        public event TradeDownloadErrorDelegate TradeDownloadErrorEvent;
        public event TradeUpdateDelegate TradeUpdateEvent;
        public event NotificationDelegate NotificationEvent;

        public void SubscribeTrades(bool skipCancel, int timeout)
        {
            SubscribeTradesAsyncContext context = new SubscribeTradesAsyncContext(true);

            SubscribeTradesInternal(context, skipCancel);

            if (! context.Wait(timeout))
                throw new Common.TimeoutException("Method call timed out");

            if (context.exception_ != null)
                throw context.exception_;
        }

        public void SubscribeTradesAsync(object data, bool skipCancel)
        {
            SubscribeTradesAsyncContext context = new SubscribeTradesAsyncContext(false);
            context.Data = data;

            SubscribeTradesInternal(context, skipCancel);
        }

        void SubscribeTradesInternal(SubscribeTradesAsyncContext context, bool skipCancel)
        {
            TradeCaptureRequest request = new TradeCaptureRequest(0);
            request.Id = Guid.NewGuid().ToString();
            request.Type = TradeCaptureRequestType.Subscribe;
            request.SkipCancel = skipCancel;

            session_.SendTradeCaptureRequest(context, request);
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
            TradeCaptureRequest request = new TradeCaptureRequest(0);
            request.Id = Guid.NewGuid().ToString();
            request.Type = TradeCaptureRequestType.Unsubscribe;

            session_.SendTradeCaptureRequest(context, request);
        }

        public TradeTransactionReportEnumerator DownloadTrades(TimeDirection timeDirection, DateTime? from, DateTime? to, bool skipCancel, int timeout)
        {
            TradeDownloadAsyncContext context = new TradeDownloadAsyncContext(true);
            context.event_ = new AutoResetEvent(false);

            DownloadTradesInternal(context, timeDirection, from, to, skipCancel);

            if (! context.event_.WaitOne(timeout))
                throw new Common.TimeoutException("Method call timed out");

            if (context.exception_ != null)
                throw context.exception_;

            return context.tradeTransactionReportEnumerator_;
        }

        public void DownloadTradesAsync(object data, TimeDirection timeDirection, DateTime? from, DateTime? to, bool skipCancel)
        {
            TradeDownloadAsyncContext context = new TradeDownloadAsyncContext(false);
            context.Data = data;

            DownloadTradesInternal(context, timeDirection, from, to, skipCancel);
        }

        void DownloadTradesInternal(TradeDownloadAsyncContext context, TimeDirection timeDirection, DateTime? from, DateTime? to, bool skipCancel)
        {
            context.timeDirection_ = timeDirection;
            context.from_ = from;
            context.to_ = to;
            context.skipCancel_ = skipCancel;
            context.reportId_ = null;

            TradeDownloadRequest request = new TradeDownloadRequest(0);
            request.Id = Guid.NewGuid().ToString();
            request.Direction = Convert(timeDirection);
            request.From = from;
            request.To = to;
            request.SkipCancel = skipCancel;

            session_.SendTradeDownloadRequest(context, request);
        }

        SoftFX.Net.TradeCapture.TradeHistoryDirection Convert(TickTrader.FDK.Common.TimeDirection timeDirection)
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

        class SubscribeTradesAsyncContext : TradeCaptureRequestClientContext, IAsyncContext
        {
            public SubscribeTradesAsyncContext(bool waitbale) : base(waitbale)
            {
            }

            public void ProcessDisconnect(Client client, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (client.SubscribeTradesErrorEvent != null)
                {
                    try
                    {
                        client.SubscribeTradesErrorEvent(client, Data, exception);
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

        class UnsubscribeTradesAsyncContext : TradeCaptureRequestClientContext, IAsyncContext
        {
            public UnsubscribeTradesAsyncContext(bool waitable) : base(waitable)
            {
            }

            public void ProcessDisconnect(Client client, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (client.UnsubscribeTradesErrorEvent != null)
                {
                    try
                    {
                        client.UnsubscribeTradesErrorEvent(client, Data, exception);
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

            ~TradeDownloadAsyncContext()
            {
                if (event_ != null)
                    event_.Close();
            }

            public void ProcessDisconnect(Client client, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (client.TradeDownloadErrorEvent != null)
                {
                    try
                    {
                        client.TradeDownloadErrorEvent(client, Data, exception);
                    }
                    catch
                    {
                    }
                }

                if (Waitable)
                {
                    if (tradeTransactionReportEnumerator_ != null)
                    {
                        tradeTransactionReportEnumerator_.SetError(exception);
                    }
                    else
                    {
                        exception_ = exception;
                        event_.Set();
                    }
                }
            }

            public TimeDirection timeDirection_;
            public DateTime? from_;
            public DateTime? to_;
            public bool skipCancel_;
            public string reportId_;            
            public TradeTransactionReport tradeTransactionReport_;
            public AutoResetEvent event_;
            public Exception exception_;
            public TradeTransactionReportEnumerator tradeTransactionReportEnumerator_;            
        }

        class ClientSessionListener : SoftFX.Net.TradeCapture.ClientSessionListener
        {
            public ClientSessionListener(Client client)
            {
                client_ = client;
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
                catch (Exception exception)
                {
                    client_.session_.LogError(exception.Message);
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
                catch (Exception exception)
                {
                    client_.session_.LogError(exception.Message);
                }
            }

            public override void OnConnectError(ClientSession clientSession, ConnectClientContext connectContext, string text)
            {               
                try
                {
                    ConnectAsyncContext connectAsyncContext = (ConnectAsyncContext) connectContext;

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
                catch (Exception exception)
                {
                    client_.session_.LogError(exception.Message);
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
                catch (Exception exception)
                {
                    client_.session_.LogError(exception.Message);
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
                catch (Exception exception)
                {
                    client_.session_.LogError(exception.Message);
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
                catch (Exception exception)
                {
                    client_.session_.LogError(exception.Message);
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
                catch (Exception exception)
                {
                    client_.session_.LogError(exception.Message);
                }
            }

            public override void OnLoginReject(ClientSession session, LoginRequestClientContext LoginRequestClientContext, LoginReject message)
            {
                try
                {
                    LoginAsyncContext context = (LoginAsyncContext)LoginRequestClientContext;

                    TickTrader.FDK.Common.LogoutReason reason = Convert(message.Reason);

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
                catch (Exception exception)
                {
                    client_.session_.LogError(exception.Message);
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
                catch (Exception exception)
                {
                    client_.session_.LogError(exception.Message);
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
                catch (Exception exception)
                {
                    client_.session_.LogError(exception.Message);
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
                catch (Exception exception)
                {
                    client_.session_.LogError(exception.Message);
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
                catch (Exception exception)
                {
                    client_.session_.LogError(exception.Message);
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
                catch (Exception exception)
                {
                    client_.session_.LogError(exception.Message);
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
                catch (Exception exception)
                {
                    client_.session_.LogError(exception.Message);
                }
            }

            public override void OnTradeCaptureReport(ClientSession session, TradeCaptureRequestClientContext TradeCaptureRequestClientContext, TradeCaptureReport message)
            {
                try
                {
                    if (TradeCaptureRequestClientContext is SubscribeTradesAsyncContext)
                    {
                        // SubscribeTrades

                        SubscribeTradesAsyncContext context = (SubscribeTradesAsyncContext)TradeCaptureRequestClientContext;

                        try
                        {
                            if (client_.SubscribeTradesResultEvent != null)
                            {
                                try
                                {
                                    client_.SubscribeTradesResultEvent(client_, context.Data);
                                }
                                catch
                                {
                                }
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
                                context.exception_ = exception;
                            }
                        }
                    }
                    else
                    {
                        // UnsubscribeTrades

                        UnsubscribeTradesAsyncContext context = (UnsubscribeTradesAsyncContext)TradeCaptureRequestClientContext;

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
                }
                catch (Exception exception)
                {
                    client_.session_.LogError(exception.Message);
                }
            }            

            public override void OnTradeCaptureReject(ClientSession session, TradeCaptureRequestClientContext TradeCaptureRequestClientContext, Reject message)
            {
                try
                {
                    if (TradeCaptureRequestClientContext is SubscribeTradesAsyncContext)
                    {
                        // SubscribeTrades

                        SubscribeTradesAsyncContext context = (SubscribeTradesAsyncContext)TradeCaptureRequestClientContext;

                        RejectException exception = new RejectException(RejectReason.None, message.Text);

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
                            context.exception_ = exception;
                        }
                    }
                    else
                    {
                        // UnsubscribeTrades

                        UnsubscribeTradesAsyncContext context = (UnsubscribeTradesAsyncContext)TradeCaptureRequestClientContext;

                        RejectException exception = new RejectException(RejectReason.None, message.Text);

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
                catch (Exception exception)
                {
                    client_.session_.LogError(exception.Message);
                }
            }

            public override void OnTradeDownloadReport(ClientSession session, TradeDownloadRequestClientContext TradeDownloadRequestClientContext, TradeDownloadReport message)
            {
                try
                {
                    TradeDownloadAsyncContext context = (TradeDownloadAsyncContext)TradeDownloadRequestClientContext;

                    try
                    {
                        if (context.reportId_ == null)
                        {
                            context.tradeTransactionReport_ = new TradeTransactionReport();

                            if (client_.TradeDownloadResultBeginEvent != null)
                            {
                                try
                                {
                                    client_.TradeDownloadResultBeginEvent(client_, context.Data);
                                }
                                catch
                                {
                                }
                            }

                            if (context.Waitable)
                            {
                                context.tradeTransactionReportEnumerator_ = new TradeTransactionReportEnumerator(client_);
                                context.event_.Set();
                            }
                        }

                        context.reportId_ = message.Id;

                        if (!message.Last)
                        {
                            // Sending the next request ahead of current response processing

                            if (context.Waitable)
                            {
                                lock (context.tradeTransactionReportEnumerator_.mutex_)
                                {
                                    if (!context.tradeTransactionReportEnumerator_.completed_)
                                    {
                                        TradeDownloadRequest request = new TradeDownloadRequest(0);
                                        request.Id = Guid.NewGuid().ToString();
                                        request.Direction = client_.Convert(context.timeDirection_);
                                        request.From = context.from_;
                                        request.To = context.to_;
                                        request.SkipCancel = context.skipCancel_;
                                        request.ReportId = context.reportId_;

                                        client_.session_.SendTradeDownloadRequest(context, request);
                                    }
                                }
                            }
                            else
                            {
                                TradeDownloadRequest request = new TradeDownloadRequest(0);
                                request.Id = Guid.NewGuid().ToString();
                                request.Direction = client_.Convert(context.timeDirection_);
                                request.From = context.from_;
                                request.To = context.to_;
                                request.SkipCancel = context.skipCancel_;
                                request.ReportId = context.reportId_;

                                client_.session_.SendTradeDownloadRequest(context, request);
                            }
                        }

                        TradeArray trades = message.Trades;
                        int count = trades.Length;

                        for (int index = 0; index < count; ++index)
                        {
                            Trade trade = trades[index];

                            Convert(context.tradeTransactionReport_, trade);

                            if (client_.TradeDownloadResultEvent != null)
                            {
                                try
                                {
                                    client_.TradeDownloadResultEvent(client_, context.Data, context.tradeTransactionReport_);
                                }
                                catch
                                {
                                }
                            }

                            if (context.Waitable)
                            {
                                TradeTransactionReport tradeTransactionReport = context.tradeTransactionReport_.Clone();

                                context.tradeTransactionReportEnumerator_.SetResult(tradeTransactionReport);
                            }
                        }

                        if (message.Last)
                        {
                            if (client_.TradeDownloadResultEndEvent != null)
                            {
                                try
                                {
                                    client_.TradeDownloadResultEndEvent(client_, context.Data);
                                }
                                catch
                                {
                                }
                            }

                            if (context.Waitable)
                            {
                                context.tradeTransactionReportEnumerator_.SetEnd();
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.TradeDownloadErrorEvent != null)
                        {
                            try
                            {
                                client_.TradeDownloadErrorEvent(client_, context.Data, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            if (context.tradeTransactionReportEnumerator_ != null)
                            {
                                context.tradeTransactionReportEnumerator_.SetError(exception);
                            }
                            else
                            {
                                context.exception_ = exception;
                                context.event_.Set();
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    client_.session_.LogError(exception.Message);
                }
            }

            public override void OnTradeDownloadReject(ClientSession session, TradeDownloadRequestClientContext TradeDownloadRequestClientContext, Reject message)
            {
                try
                {
                    TradeDownloadAsyncContext context = (TradeDownloadAsyncContext)TradeDownloadRequestClientContext;

                    RejectException exception = new RejectException(RejectReason.None, message.Text);

                    if (client_.TradeDownloadErrorEvent != null)
                    {
                        try
                        {
                            client_.TradeDownloadErrorEvent(client_, context.Data, exception);
                        }
                        catch
                        {
                        }
                    }

                    if (context.Waitable)
                    {
                        if (context.tradeTransactionReportEnumerator_ != null)
                        {
                            context.tradeTransactionReportEnumerator_.SetError(exception);
                        }
                        else
                        {
                            context.exception_ = exception;
                            context.event_.Set();
                        }
                    }
                }
                catch (Exception exception)
                {
                    client_.session_.LogError(exception.Message);
                }
            }

            public override void OnLogout(ClientSession session, Logout message)
            {
                try
                {
                    LogoutInfo result = new LogoutInfo();
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
                catch (Exception exception)
                {
                    client_.session_.LogError(exception.Message);
                }
            }

            public override void OnTradeCaptureUpdate(ClientSession session, TradeCaptureUpdate message)
            {
                try
                {
                    SoftFX.Net.TradeCapture.Trade trade = message.Trade;

                    TradeTransactionReport tradeTransactionReport = new TradeTransactionReport();
                    Convert(tradeTransactionReport, trade);

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
                catch (Exception exception)
                {
                    client_.session_.LogError(exception.Message);
                }
            }

            public override void OnNotification(ClientSession session, SoftFX.Net.TradeCapture.Notification message)
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
                catch (Exception exception)
                {
                    client_.session_.LogError(exception.Message);
                }
            }

            void Convert(TickTrader.FDK.Common.TradeTransactionReport tradeTransactionReport, SoftFX.Net.TradeCapture.Trade trade)
            {
                tradeTransactionReport.TradeTransactionId = trade.Id;
                tradeTransactionReport.TradeTransactionReportType = Convert(trade.Type);
                tradeTransactionReport.TradeTransactionReason = Convert(trade.Reason);

                BalanceNull balance = trade.Balance;
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
                tradeTransactionReport.Quantity = trade.Qty.GetValueOrDefault(0);
                tradeTransactionReport.MaxVisibleQuantity = trade.MaxVisibleQty;
                tradeTransactionReport.LeavesQuantity = trade.LeavesQty.GetValueOrDefault(0);
                tradeTransactionReport.Price = trade.Price.GetValueOrDefault(0);
                tradeTransactionReport.StopPrice = trade.StopPrice.GetValueOrDefault(0);

                if (trade.OrderType.HasValue)
                {
                    tradeTransactionReport.OrderType = Convert(trade.OrderType.Value);
                }
                else
                    tradeTransactionReport.OrderType = TickTrader.FDK.Common.OrderType.Market;

                if (trade.OrderSide.HasValue)
                {
                    tradeTransactionReport.OrderSide = Convert(trade.OrderSide.Value);
                }
                else
                    tradeTransactionReport.OrderSide = TickTrader.FDK.Common.OrderSide.Buy;

                if (trade.TimeInForce.HasValue)
                {
                    tradeTransactionReport.TimeInForce = Convert(trade.TimeInForce.Value);
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

                if (trade.ParentOrderType.HasValue)
                {
                    tradeTransactionReport.ReqOrderType = Convert(trade.ParentOrderType.Value);
                }
                else
                    tradeTransactionReport.ReqOrderType = null;

                tradeTransactionReport.ReqOpenQuantity = trade.ParentQty;
                tradeTransactionReport.ReqOpenPrice = trade.ParentPrice;
                tradeTransactionReport.ReqClosePrice = null;
                tradeTransactionReport.ReqCloseQuantity = null;

                if (trade.Type == TradeType.PositionClosed)
                {
                    tradeTransactionReport.PositionId = trade.OrderId.ToString();

                    if (trade.ByOrderId.HasValue)
                    {
                        tradeTransactionReport.PositionById = trade.ByOrderId.ToString();
                    }
                    else
                        tradeTransactionReport.PositionById = null;

                    tradeTransactionReport.PositionOpened = trade.Created.GetValueOrDefault();
                    tradeTransactionReport.PosOpenReqPrice = 0;
                    tradeTransactionReport.PosOpenPrice = trade.Price.Value;
                    tradeTransactionReport.PositionQuantity = trade.Qty.Value;
                    tradeTransactionReport.PositionLastQuantity = trade.LastQty.Value;
                    tradeTransactionReport.PositionLeavesQuantity = trade.LeavesQty.Value;
                    tradeTransactionReport.PositionCloseRequestedPrice = 0;
                    tradeTransactionReport.PositionClosePrice = trade.LastPrice.Value;
                    tradeTransactionReport.PositionClosed = trade.TransactTime;
                    tradeTransactionReport.PositionModified = trade.Modified.Value;
                }
                else
                {
                    tradeTransactionReport.PositionId = null;
                    tradeTransactionReport.PositionById = null;
                    tradeTransactionReport.PositionOpened = new DateTime();
                    tradeTransactionReport.PosOpenReqPrice = 0;
                    tradeTransactionReport.PosOpenPrice = 0;
                    tradeTransactionReport.PositionQuantity = 0;
                    tradeTransactionReport.PositionLastQuantity = 0;
                    tradeTransactionReport.PositionLeavesQuantity = 0;
                    tradeTransactionReport.PositionCloseRequestedPrice = 0;
                    tradeTransactionReport.PositionClosePrice = 0;
                    tradeTransactionReport.PositionClosed = new DateTime();
                    tradeTransactionReport.PositionModified = new DateTime();
                }

                SoftFX.Net.TradeCapture.PositionNull position = trade.Position;
                if (position.HasValue)
                {
                    tradeTransactionReport.PosRemainingSide = Convert(position.Type);
                    tradeTransactionReport.PosRemainingPrice = position.Price;
                    tradeTransactionReport.PositionLeavesQuantity = position.Qty;
                }
                else
                {
                    tradeTransactionReport.PosRemainingSide = Common.OrderSide.Buy;
                    tradeTransactionReport.PosRemainingPrice = null;
                    tradeTransactionReport.PositionLeavesQuantity = 0;
                }
                
                tradeTransactionReport.Commission = trade.Commission.GetValueOrDefault(0);
                tradeTransactionReport.AgentCommission = trade.AgentCommission.GetValueOrDefault(0);
                tradeTransactionReport.Swap = trade.Swap.GetValueOrDefault(0);                
                tradeTransactionReport.CommCurrency = trade.CommissionCurrId;
                tradeTransactionReport.StopLoss = trade.StopLoss.GetValueOrDefault(0);
                tradeTransactionReport.TakeProfit = trade.TakeProfit.GetValueOrDefault(0);
                tradeTransactionReport.TransactionTime = trade.TransactTime;
                tradeTransactionReport.OrderFillPrice = trade.LastPrice;
                tradeTransactionReport.OrderLastFillAmount = trade.LastQty;
                tradeTransactionReport.ActionId = trade.ActionId.GetValueOrDefault(0);
                tradeTransactionReport.Expiration = trade.ExpireTime;
                tradeTransactionReport.MarginCurrency = trade.MarginCurrId;
                tradeTransactionReport.ProfitCurrency = trade.ProfitCurrId;
                tradeTransactionReport.MinCommissionCurrency = trade.MinCommissionCurrId;

                AssetNull asset1 = trade.Asset1;
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

                AssetNull asset2 = trade.Asset2;
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

            TickTrader.FDK.Common.LogoutReason Convert(SoftFX.Net.TradeCapture.LoginRejectReason reason)
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

            TickTrader.FDK.Common.LogoutReason Convert(SoftFX.Net.TradeCapture.LogoutReason reason)
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

            TickTrader.FDK.Common.TradeTransactionReportType Convert(SoftFX.Net.TradeCapture.TradeType type)
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

            TickTrader.FDK.Common.TradeTransactionReason Convert(SoftFX.Net.TradeCapture.TradeReason reason)
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

            TickTrader.FDK.Common.OrderType Convert(SoftFX.Net.TradeCapture.OrderType type)
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

            TickTrader.FDK.Common.OrderSide Convert(SoftFX.Net.TradeCapture.OrderSide side)
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

            TickTrader.FDK.Common.OrderTimeInForce Convert(SoftFX.Net.TradeCapture.OrderTimeInForce timeInForce)
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

            TickTrader.FDK.Common.OrderSide Convert(SoftFX.Net.TradeCapture.PosType posType)
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

            TickTrader.FDK.Common.NotificationType Convert(SoftFX.Net.TradeCapture.NotificationType type)
            {
                switch (type)
                {
                    case SoftFX.Net.TradeCapture.NotificationType.ConfigUpdate:
                        return TickTrader.FDK.Common.NotificationType.ConfigUpdated;

                    default:
                        throw new Exception("Invalid notification type : " + type);
                }
            }

            TickTrader.FDK.Common.NotificationSeverity Convert(SoftFX.Net.TradeCapture.NotificationSeverity severity)
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

            Client client_;
        }

        #endregion
    }
}
