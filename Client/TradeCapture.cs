﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.ExceptionServices;
using System.Threading;
using SoftFX.Net.Core;
using SoftFX.Net.TradeCapture;
using TickTrader.FDK.Common;
using ClientSession = SoftFX.Net.TradeCapture.ClientSession;
//using ClientSessionOptions = SoftFX.Net.TradeCapture.ClientSessionOptions;

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
            int port = 5044,
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
            string proxyPassword = null,
            OptimizationType? optimizationType = null
        )
        {
            ClientSessionOptions options = new ClientSessionOptions(port, validateClientCertificate);
            options.ConnectionType = SoftFX.Net.Core.ConnectionType.SecureSocket;
            options.ServerCertificateName = serverCertificateName;
            options.ConnectMaxCount = connectAttempts;
            options.ReconnectMaxCount = reconnectAttempts;
            options.ConnectInterval = connectInterval;
            options.HeartbeatInterval = heartbeatInterval;
            options.SendBufferSize = 20 * 1024 * 1024;
            options.Log.Directory = logDirectory;
            options.Log.Events = logEvents;
            options.Log.States = logStates;
            options.Log.Messages = logMessages;
            options.ProxyType = proxyType;
            options.ProxyAddress = proxyAddress;
            options.ProxyPort = proxyPort;
            options.Username = proxyUsername;
            options.Password = proxyPassword;
            if (optimizationType.HasValue)
                options.OptimizationType = optimizationType.Value;

            session_ = new ClientSession(name, options);
            sessionListener_ = new ClientSessionListener(this);
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
                DisconnectInternal(null, Reason.ClientError("Connect timeout"));
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

        public string Disconnect(Reason reason)
        {
            string result;

            DisconnectAsyncContext context = new DisconnectAsyncContext(true);

            if (DisconnectInternal(context, reason))
            {
                context.Wait(-1);

                result = context.Reason.Text;
            }
            else
                result = null;

            return result;
        }

        public bool DisconnectAsync(object data, Reason reason)
        {
            DisconnectAsyncContext context = new DisconnectAsyncContext(false);
            context.Data = data;

            return DisconnectInternal(context, reason);
        }

        bool DisconnectInternal(DisconnectAsyncContext context, Reason reason)
        {
            return session_.Disconnect(context, reason);
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
            protocolSpec_.InitTradeCaptureVersion(new ProtocolVersion(session_.MajorVersion, session_.MinorVersion));

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
        public delegate void SubscribeTriggerReportResultBeginDelegate(TradeCapture tradeCapture, object data, int count);
        public delegate void SubscribeTriggerReportResultDelegate(TradeCapture tradeCapture, object data, TickTrader.FDK.Common.ContingentOrderTriggerReport triggerReport);
        public delegate void SubscribeTriggerReportResultEndDelegate(TradeCapture tradeCapture, object data);
        public delegate void SubscribeTriggerReportErrorDelegate(TradeCapture tradeCapture, object data, Exception exception);
        public delegate void UnsubscribeTriggerReportResultDelegate(TradeCapture tradeCapture, object data);
        public delegate void UnsubscribeTriggerReportErrorDelegate(TradeCapture tradeCapture, object data, Exception exception);
        public delegate void DownloadTriggerReportResultBeginDelegate(TradeCapture tradeCapture, object data, string id, int totalCount);
        public delegate void DownloadTriggerReportResultDelegate(TradeCapture tradeCapture, object data, TickTrader.FDK.Common.ContingentOrderTriggerReport triggerReport);
        public delegate void DownloadTriggerReportResultEndDelegate(TradeCapture tradeCapture, object data);
        public delegate void DownloadTriggerReportErrorDelegate(TradeCapture tradeCapture, object data, Exception exception);
        public delegate void CancelDownloadTriggerReportResultDelegate(TradeCapture tradeCapture, object data);
        public delegate void CancelDownloadTriggerReportErrorDelegate(TradeCapture tradeCapture, object data, Exception exception);
        public delegate void TradeUpdateDelegate(TradeCapture tradeCapture, TickTrader.FDK.Common.TradeTransactionReport tradeTransactionReport);
        public delegate void NotificationDelegate(TradeCapture tradeCapture, TickTrader.FDK.Common.Notification notification);
        public delegate void TriggerReportUpdateDelegate(TradeCapture tradeCapture, TickTrader.FDK.Common.ContingentOrderTriggerReport triggerReport);

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
        public event SubscribeTriggerReportResultBeginDelegate SubscribeTriggerReportResultBeginEvent;
        public event SubscribeTriggerReportResultDelegate SubscribeTriggerReportResultEvent;
        public event SubscribeTriggerReportResultEndDelegate SubscribeTriggerReportResultEndEvent;
        public event SubscribeTriggerReportErrorDelegate SubscribeTriggerReportErrorEvent;
        public event UnsubscribeTriggerReportResultDelegate UnsubscribeTriggerReportResultEvent;
        public event UnsubscribeTriggerReportErrorDelegate UnsubscribeTriggerReportErrorEvent;
        public event DownloadTriggerReportResultBeginDelegate DownloadTriggerReportResultBeginEvent;
        public event DownloadTriggerReportResultDelegate DownloadTriggerReportResultEvent;
        public event DownloadTriggerReportResultEndDelegate DownloadTriggerReportResultEndEvent;
        public event DownloadTriggerReportErrorDelegate DownloadTriggerReportErrorEvent;
        public event CancelDownloadTriggerReportResultDelegate CancelDownloadTriggerReportResultEvent;
        public event CancelDownloadTriggerReportErrorDelegate CancelDownloadTriggerReportErrorEvent;
        public event TradeUpdateDelegate TradeUpdateEvent;
        public event NotificationDelegate NotificationEvent;
        public event TriggerReportUpdateDelegate TriggerReportUpdateEvent;

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

        public SubscribeTriggerReportsEnumerator SubscribeTriggerReports(DateTime? from, bool skipFailed, int timeout)
        {
            SubscribeTriggerReportsAsyncContext context = new SubscribeTriggerReportsAsyncContext(true);
            context.enumerator_ = new SubscribeTriggerReportsEnumerator(this);

            SubscribeTriggerReportsInternal(context, from, skipFailed);

            context.enumerator_.Begin(timeout);

            return context.enumerator_;
        }

        public void SubscribeTriggerReportsAsync(object data, DateTime? from, bool skipFailed)
        {
            SubscribeTriggerReportsAsyncContext context = new SubscribeTriggerReportsAsyncContext(false);
            context.Data = data;

            SubscribeTriggerReportsInternal(context, from, skipFailed);
        }

        void SubscribeTriggerReportsInternal(SubscribeTriggerReportsAsyncContext context, DateTime? from, bool skipFailed)
        {
            TriggerHistorySubscribeRequest request = new TriggerHistorySubscribeRequest(0);
            request.Id = Guid.NewGuid().ToString();
            request.From = from;
            request.SkipFailed = skipFailed;

            session_.SendTriggerHistorySubscribeRequest(context, request);
        }

        public void UnsubscribeTriggerReports(int timeout)
        {
            UnsubscribeTriggerReportsAsyncContext context = new UnsubscribeTriggerReportsAsyncContext(true);

            UnsubscribeTriggerReportsInternal(context);

            if (!context.Wait(timeout))
                throw new Common.TimeoutException("Method call timed out");

            if (context.exception_ != null)
                throw context.exception_;
        }

        public void UnsubscribeTriggerReportsAsync(object data)
        {
            UnsubscribeTriggerReportsAsyncContext context = new UnsubscribeTriggerReportsAsyncContext(false);
            context.Data = data;

            UnsubscribeTriggerReportsInternal(context);
        }

        void UnsubscribeTriggerReportsInternal(UnsubscribeTriggerReportsAsyncContext context)
        {
            TriggerHistoryUnsubscribeRequest request = new TriggerHistoryUnsubscribeRequest(0);
            request.Id = Guid.NewGuid().ToString();

            session_.SendTriggerHistoryUnsubscribeRequest(context, request);
        }

        public DownloadTriggerReportsEnumerator DownloadTriggerReports(TimeDirection timeDirection, DateTime? from, DateTime? to, bool skipFailed, int timeout)
        {
            DownloadTriggerReportsAsyncContext context = new DownloadTriggerReportsAsyncContext(true);
            context.enumerator_ = new DownloadTriggerReportsEnumerator(this);

            DownloadTriggerReportsInternal(context, timeDirection, from, to, skipFailed);

            context.enumerator_.Begin(timeout);

            return context.enumerator_;
        }

        public void DownloadTriggerReportsAsync(object data, TimeDirection timeDirection, DateTime? from, DateTime? to, bool skipFailed)
        {
            DownloadTriggerReportsAsyncContext context = new DownloadTriggerReportsAsyncContext(false);
            context.Data = data;

            DownloadTriggerReportsInternal(context, timeDirection, from, to, skipFailed);
        }

        void DownloadTriggerReportsInternal(DownloadTriggerReportsAsyncContext context, TimeDirection timeDirection, DateTime? from, DateTime? to, bool skipFailed)
        {
            TriggerHistoryDownloadRequest request = new TriggerHistoryDownloadRequest(0);
            request.Id = Guid.NewGuid().ToString();
            request.Direction = GetTradeHistoryDirection(timeDirection);
            request.From = from;
            request.To = to;
            request.SkipFailed = skipFailed;

            session_.SendTriggerHistoryDownloadRequest(context, request);
        }

        public void CancelDownloadTriggerReports(string id, int timeout)
        {
            CancelDownloadTriggerReportsAsyncContext context = new CancelDownloadTriggerReportsAsyncContext(true);

            CancelDownloadTriggerReportsInternal(context, id);

            if (!context.Wait(timeout))
                throw new Common.TimeoutException("Method call timed out");

            if (context.exception_ != null)
                throw context.exception_;
        }

        public void CancelDownloadTriggerReportsAsync(object data, string id)
        {
            CancelDownloadTriggerReportsAsyncContext context = new CancelDownloadTriggerReportsAsyncContext(false);
            context.Data = data;

            CancelDownloadTriggerReportsInternal(context, id);
        }

        void CancelDownloadTriggerReportsInternal(CancelDownloadTriggerReportsAsyncContext context, string id)
        {
            TriggerHistoryDownloadCancelRequest request = new TriggerHistoryDownloadCancelRequest(0);
            request.Id = Guid.NewGuid().ToString();
            request.RequestId = id;

            session_.SendTriggerHistoryDownloadCancelRequest(context, request);
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
            void ProcessDisconnect(TradeCapture tradeCapture, Reason reason);
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

            public Reason Reason;
        }

        class LoginAsyncContext : LoginRequestClientContext, IAsyncContext
        {
            public LoginAsyncContext(bool waitable) : base(waitable)
            {
            }

            public void ProcessDisconnect(TradeCapture tradeCapture, Reason reason)
            {
                DisconnectException exception = new DisconnectException(reason.ToString());

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

            public void ProcessDisconnect(TradeCapture tradeCapture, Reason reason)
            {
                DisconnectException exception = new DisconnectException(reason.ToString());

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

            public void ProcessDisconnect(TradeCapture tradeCapture, Reason reason)
            {
                DisconnectException exception = new DisconnectException(reason.ToString());

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

            public void ProcessDisconnect(TradeCapture tradeCapture, Reason reason)
            {
                DisconnectException exception = new DisconnectException(reason.ToString());

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

            public void ProcessDisconnect(TradeCapture tradeCapture, Reason reason)
            {
                DisconnectException exception = new DisconnectException(reason.ToString());

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

            public void ProcessDisconnect(TradeCapture tradeCapture, Reason reason)
            {
                DisconnectException exception = new DisconnectException(reason.ToString());

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

            public void ProcessDisconnect(TradeCapture tradeCapture, Reason reason)
            {
                DisconnectException exception = new DisconnectException(reason.ToString());

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

            public void ProcessDisconnect(TradeCapture tradeCapture, Reason reason)
            {
                DisconnectException exception = new DisconnectException(reason.ToString());

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

            public void ProcessDisconnect(TradeCapture tradeCapture, Reason reason)
            {
                DisconnectException exception = new DisconnectException(reason.ToString());

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

            public void ProcessDisconnect(TradeCapture tradeCapture, Reason reason)
            {
                DisconnectException exception = new DisconnectException(reason.ToString());

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

        class SubscribeTriggerReportsAsyncContext : TriggerHistorySubscribeRequestClientContext, IAsyncContext
        {
            public SubscribeTriggerReportsAsyncContext(bool waitbale) : base(waitbale)
            {
            }

            public void ProcessDisconnect(TradeCapture tradeCapture, Reason reason)
            {
                DisconnectException exception = new DisconnectException(reason.ToString());

                if (tradeCapture.SubscribeTriggerReportErrorEvent != null)
                {
                    try
                    {
                        tradeCapture.SubscribeTriggerReportErrorEvent(tradeCapture, Data, exception);
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

            public SubscribeTriggerReportsEnumerator enumerator_;
        }

        class UnsubscribeTriggerReportsAsyncContext : TriggerHistoryUnsubscribeRequestClientContext, IAsyncContext
        {
            public UnsubscribeTriggerReportsAsyncContext(bool waitable) : base(waitable)
            {
            }

            public void ProcessDisconnect(TradeCapture tradeCapture, Reason reason)
            {
                DisconnectException exception = new DisconnectException(reason.ToString());

                if (tradeCapture.UnsubscribeTriggerReportErrorEvent != null)
                {
                    try
                    {
                        tradeCapture.UnsubscribeTriggerReportErrorEvent(tradeCapture, Data, exception);
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

        class DownloadTriggerReportsAsyncContext : TriggerHistoryDownloadRequestClientContext, IAsyncContext
        {
            public DownloadTriggerReportsAsyncContext(bool waitbale) : base(waitbale)
            {
            }

            public void ProcessDisconnect(TradeCapture tradeCapture, Reason reason)
            {
                DisconnectException exception = new DisconnectException(reason.ToString());

                if (tradeCapture.DownloadTriggerReportErrorEvent != null)
                {
                    try
                    {
                        tradeCapture.DownloadTriggerReportErrorEvent(tradeCapture, Data, exception);
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

            public DownloadTriggerReportsEnumerator enumerator_;
        }

        class CancelDownloadTriggerReportsAsyncContext : TriggerHistoryDownloadCancelRequestClientContext, IAsyncContext
        {
            public CancelDownloadTriggerReportsAsyncContext(bool waitable) : base(waitable)
            {
            }

            public void ProcessDisconnect(TradeCapture tradeCapture, Reason reason)
            {
                DisconnectException exception = new DisconnectException(reason.ToString());

                if (tradeCapture.CancelDownloadTriggerReportErrorEvent != null)
                {
                    try
                    {
                        tradeCapture.CancelDownloadTriggerReportErrorEvent(tradeCapture, Data, exception);
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

            public override void OnConnectError(ClientSession clientSession, ConnectClientContext connectContext, Reason reason)
            {
                try
                {
                    ConnectAsyncContext connectAsyncContext = (ConnectAsyncContext) connectContext;

                    ConnectException exception = new ConnectException(reason.Text);

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

            public override void OnConnectError(ClientSession clientSession, Reason reason)
            {
                try
                {
                    ConnectException exception = new ConnectException(reason.Text);

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

            public override void OnDisconnect(ClientSession clientSession, DisconnectClientContext disconnectContext, ClientContext[] contexts, Reason reason)
            {
                try
                {
                    DisconnectAsyncContext disconnectAsyncContext = (DisconnectAsyncContext) disconnectContext;

                    if (contexts != null)
                    {
                        foreach (ClientContext context in contexts)
                        {
                            try
                            {
                                ((IAsyncContext)context).ProcessDisconnect(client_, reason);
                            }
                            catch
                            {
                            }
                        }
                    }

                    if (client_.DisconnectResultEvent != null)
                    {
                        try
                        {
                            client_.DisconnectResultEvent(client_, disconnectAsyncContext.Data, reason.Text);
                        }
                        catch
                        {
                        }
                    }

                    if (disconnectAsyncContext.Waitable)
                    {
                        disconnectAsyncContext.Reason = reason;
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnDisconnect(ClientSession clientSession, ClientContext[] contexts, Reason reason)
            {
                try
                {
                    if (contexts != null)
                    {
                        foreach (ClientContext context in contexts)
                        {
                            try
                            {
                                ((IAsyncContext)context).ProcessDisconnect(client_, reason);
                            }
                            catch
                            {
                            }
                        }
                    }

                    if (client_.DisconnectEvent != null)
                    {
                        try
                        {
                            client_.DisconnectEvent(client_, reason.Text);
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

            public override void OnReceive(ClientSession clientSession, Message message)
            {
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

            public override void OnLogout(ClientSession session, LoginRequestClientContext LoginRequestClientContext, Logout message)
            {
                try
                {
                    LoginAsyncContext loginContext = (LoginAsyncContext)LoginRequestClientContext;

                    try
                    {
                        LogoutInfo result = new LogoutInfo();
                        result.Reason = GetLogoutReason(message.Reason);
                        result.Message = message.Text;

                        if (client_.LogoutResultEvent != null)
                        {
                            try
                            {
                                client_.LogoutResultEvent(client_, loginContext.Data, result);
                            }
                            catch
                            {
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.LogoutErrorEvent != null)
                        {
                            try
                            {
                                client_.LogoutErrorEvent(client_, loginContext.Data, exception);
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

            public override void OnTriggerHistoryUpdateReport(ClientSession session, TriggerHistoryUpdateReport message)
            {
                try
                {
                    var result = GetTriggerReport(message.Report);

                    if (client_.TriggerReportUpdateEvent != null)
                    {
                        try
                        {
                            client_.TriggerReportUpdateEvent(client_, result);
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

            public override void OnTriggerHistoryUpdateReport(ClientSession session, TriggerHistoryDownloadRequestClientContext context, TriggerHistoryUpdateReport message)
            {
                try
                {
                    var asyncContext = (DownloadTriggerReportsAsyncContext)context;

                    try
                    {
                        var result = GetTriggerReport(message.Report);

                        if (client_.DownloadTriggerReportResultEvent != null)
                        {
                            try
                            {
                                client_.DownloadTriggerReportResultEvent(client_, asyncContext.Data, result);
                            }
                            catch
                            {
                            }
                        }

                        if (asyncContext.Waitable)
                        {
                            asyncContext.enumerator_.SetResult(result);
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.DownloadTriggerReportErrorEvent != null)
                        {
                            try
                            {
                                client_.DownloadTriggerReportErrorEvent(client_, asyncContext.Data, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (asyncContext.Waitable)
                        {
                            asyncContext.enumerator_.SetError(exception);
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnTriggerHistoryUpdateReport(ClientSession session, TriggerHistorySubscribeRequestClientContext context, TriggerHistoryUpdateReport message)
            {
                try
                {
                    var asyncContext = (SubscribeTriggerReportsAsyncContext)context;

                    try
                    {
                        var result = GetTriggerReport(message.Report);

                        if (client_.SubscribeTriggerReportResultEvent != null)
                        {
                            try
                            {
                                client_.SubscribeTriggerReportResultEvent(client_, asyncContext.Data, result);
                            }
                            catch
                            {
                            }
                        }

                        if (asyncContext.Waitable)
                        {
                            asyncContext.enumerator_.SetResult(result);
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.SubscribeTriggerReportErrorEvent != null)
                        {
                            try
                            {
                                client_.SubscribeTriggerReportErrorEvent(client_, asyncContext.Data, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (asyncContext.Waitable)
                        {
                            asyncContext.enumerator_.SetError(exception);
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnTriggerHistoryDownloadBeginReport(ClientSession session, TriggerHistoryDownloadRequestClientContext context, TriggerHistoryDownloadBeginReport message)
            {
                try
                {
                    var asyncContext = (DownloadTriggerReportsAsyncContext)context;

                    try
                    {

                        if (client_.DownloadTriggerReportResultBeginEvent != null)
                        {
                            try
                            {
                                client_.DownloadTriggerReportResultBeginEvent(client_, asyncContext.Data, message.RequestId, message.TotalCount);
                            }
                            catch
                            {
                            }
                        }

                        if (asyncContext.Waitable)
                        {
                            asyncContext.enumerator_.SetBegin(message.RequestId, message.TotalCount);
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.DownloadTriggerReportErrorEvent != null)
                        {
                            try
                            {
                                client_.DownloadTriggerReportErrorEvent(client_, asyncContext.Data, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (asyncContext.Waitable)
                        {
                            asyncContext.enumerator_.SetError(exception);
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnTriggerHistoryDownloadCancelReject(ClientSession session, TriggerHistoryDownloadCancelRequestClientContext context, Reject message)
            {
                try
                {
                    var asyncContext = (CancelDownloadTriggerReportsAsyncContext)context;

                    Common.RejectReason rejectReason = GetRejectReason(message.Reason);
                    RejectException exception = new RejectException(rejectReason, message.Text);

                    if (client_.CancelDownloadTriggerReportErrorEvent != null)
                    {
                        try
                        {
                            client_.CancelDownloadTriggerReportErrorEvent(client_, asyncContext.Data, exception);
                        }
                        catch
                        {
                        }
                    }

                    if (asyncContext.Waitable)
                    {
                        asyncContext.exception_ = exception;
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnTriggerHistoryDownloadCancelReport(ClientSession session, TriggerHistoryDownloadCancelRequestClientContext context, TriggerHistoryDownloadCancelReport message)
            {
                try
                {
                    var asyncContext = (CancelDownloadTriggerReportsAsyncContext)context;

                    try
                    {
                        if (client_.CancelDownloadTriggerReportResultEvent != null)
                        {
                            try
                            {
                                client_.CancelDownloadTriggerReportResultEvent(client_, asyncContext.Data);
                            }
                            catch
                            {
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.CancelDownloadTriggerReportErrorEvent != null)
                        {
                            try
                            {
                                client_.CancelDownloadTriggerReportErrorEvent(client_, asyncContext.Data, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (asyncContext.Waitable)
                        {
                            asyncContext.exception_ = exception;
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnTriggerHistoryDownloadEndReport(ClientSession session, TriggerHistoryDownloadRequestClientContext context, TriggerHistoryDownloadEndReport message)
            {
                try
                {
                    var asyncContext = (DownloadTriggerReportsAsyncContext)context;

                    try
                    {
                        if (client_.DownloadTriggerReportResultEndEvent != null)
                        {
                            try
                            {
                                client_.DownloadTriggerReportResultEndEvent(client_, asyncContext.Data);
                            }
                            catch
                            {
                            }
                        }

                        if (asyncContext.Waitable)
                        {
                            asyncContext.enumerator_.SetEnd();
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.DownloadTriggerReportErrorEvent != null)
                        {
                            try
                            {
                                client_.DownloadTriggerReportErrorEvent(client_, asyncContext.Data, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (asyncContext.Waitable)
                        {
                            asyncContext.enumerator_.SetError(exception);
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnTriggerHistoryDownloadReject(ClientSession session, TriggerHistoryDownloadRequestClientContext context, Reject message)
            {
                try
                {
                    var asyncContext = (DownloadTriggerReportsAsyncContext)context;

                    TickTrader.FDK.Common.RejectReason rejectReason = GetRejectReason(message.Reason);
                    RejectException exception = new RejectException(rejectReason, message.Text);

                    if (client_.DownloadTriggerReportErrorEvent != null)
                    {
                        try
                        {
                            client_.DownloadTriggerReportErrorEvent(client_, asyncContext.Data, exception);
                        }
                        catch
                        {
                        }
                    }

                    if (asyncContext.Waitable)
                    {
                        asyncContext.enumerator_.SetError(exception);
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnTriggerHistorySubscribeBeginReport(ClientSession session, TriggerHistorySubscribeRequestClientContext context, TriggerHistorySubscribeBeginReport message)
            {
                try
                {
                    var asyncContext = (SubscribeTriggerReportsAsyncContext)context;

                    try
                    {

                        if (client_.SubscribeTriggerReportResultBeginEvent != null)
                        {
                            try
                            {
                                client_.SubscribeTriggerReportResultBeginEvent(client_, asyncContext.Data, message.TotalCount);
                            }
                            catch
                            {
                            }
                        }

                        if (asyncContext.Waitable)
                        {
                            asyncContext.enumerator_.SetBegin(message.TotalCount);
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.SubscribeTriggerReportErrorEvent != null)
                        {
                            try
                            {
                                client_.SubscribeTriggerReportErrorEvent(client_, asyncContext.Data, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (asyncContext.Waitable)
                        {
                            asyncContext.enumerator_.SetError(exception);
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnTriggerHistorySubscribeEndReport(ClientSession session, TriggerHistorySubscribeRequestClientContext context, TriggerHistorySubscribeEndReport message)
            {
                try
                {
                    var asyncContext = (SubscribeTriggerReportsAsyncContext)context;

                    try
                    {
                        if (client_.SubscribeTriggerReportResultEndEvent != null)
                        {
                            try
                            {
                                client_.SubscribeTriggerReportResultEndEvent(client_, asyncContext.Data);
                            }
                            catch
                            {
                            }
                        }

                        if (asyncContext.Waitable)
                        {
                            asyncContext.enumerator_.SetEnd();
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.SubscribeTriggerReportErrorEvent != null)
                        {
                            try
                            {
                                client_.SubscribeTriggerReportErrorEvent(client_, asyncContext.Data, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (asyncContext.Waitable)
                        {
                            asyncContext.enumerator_.SetError(exception);
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnTriggerHistorySubscribeReject(ClientSession session, TriggerHistorySubscribeRequestClientContext context, Reject message)
            {
                try
                {
                    var asyncContext = (SubscribeTriggerReportsAsyncContext)context;

                    TickTrader.FDK.Common.RejectReason rejectReason = GetRejectReason(message.Reason);
                    RejectException exception = new RejectException(rejectReason, message.Text);

                    if (client_.SubscribeTriggerReportErrorEvent != null)
                    {
                        try
                        {
                            client_.SubscribeTriggerReportErrorEvent(client_, asyncContext.Data, exception);
                        }
                        catch
                        {
                        }
                    }

                    if (asyncContext.Waitable)
                    {
                        asyncContext.enumerator_.SetError(exception);
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnTriggerHistoryUnsubscribeReject(ClientSession session, TriggerHistoryUnsubscribeRequestClientContext context, Reject message)
            {
                try
                {
                    var asyncContext = (UnsubscribeTriggerReportsAsyncContext)context;

                    TickTrader.FDK.Common.RejectReason rejectReason = GetRejectReason(message.Reason);
                    RejectException exception = new RejectException(rejectReason, message.Text);

                    if (client_.UnsubscribeTriggerReportErrorEvent != null)
                    {
                        try
                        {
                            client_.UnsubscribeTriggerReportErrorEvent(client_, asyncContext.Data, exception);
                        }
                        catch
                        {
                        }
                    }

                    if (asyncContext.Waitable)
                    {
                        asyncContext.exception_ = exception;
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnTriggerHistoryUnsubscribeReport(ClientSession session, TriggerHistoryUnsubscribeRequestClientContext context, TriggerHistoryUnsubscribeReport message)
            {
                try
                {
                    var asyncContext = (UnsubscribeTriggerReportsAsyncContext)context;

                    try
                    {
                        if (client_.UnsubscribeTriggerReportResultEvent != null)
                        {
                            try
                            {
                                client_.UnsubscribeTriggerReportResultEvent(client_, asyncContext.Data);
                            }
                            catch
                            {
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.UnsubscribeTriggerReportErrorEvent != null)
                        {
                            try
                            {
                                client_.UnsubscribeTriggerReportErrorEvent(client_, asyncContext.Data, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (asyncContext.Waitable)
                        {
                            asyncContext.exception_ = exception;
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
                    tradeTransactionReport.TransactionCurrency = balance.CurrencyId;
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
                tradeTransactionReport.CommCurrency = trade.CommissionCurrencyId;
                tradeTransactionReport.StopLoss = trade.StopLoss.GetValueOrDefault();
                tradeTransactionReport.TakeProfit = trade.TakeProfit.GetValueOrDefault();
                tradeTransactionReport.TransactionTime = trade.TransactTime;
                tradeTransactionReport.OrderFillPrice = trade.LastPrice;
                tradeTransactionReport.OrderLastFillAmount = trade.LastQty;
                tradeTransactionReport.ActionId = trade.ActionId.GetValueOrDefault();
                tradeTransactionReport.Expiration = trade.ExpireTime;
                tradeTransactionReport.MarginCurrency = trade.MarginCurrencyId;
                tradeTransactionReport.ProfitCurrency = trade.ProfitCurrencyId;
                tradeTransactionReport.MinCommissionCurrency = trade.MinCommissionCurrencyId;
                tradeTransactionReport.ImmediateOrCancel = (trade.OrderFlags & OrderFlags.ImmediateOrCancel) != 0;
                tradeTransactionReport.Slippage = trade.Slippage;
                tradeTransactionReport.Tax = trade.Tax.GetValueOrDefault();
                tradeTransactionReport.TaxValue = trade.TaxValue.GetValueOrDefault();
                tradeTransactionReport.Rebate = trade.Rebate.GetValueOrDefault();
                tradeTransactionReport.RebateCurrency = trade.RebateCurrencyId;
                tradeTransactionReport.RelatedOrderId = trade.RelatedOrderId;

                TradeAssetNull asset1 = trade.SrcAsset;
                if (asset1.HasValue)
                {
                    tradeTransactionReport.SrcAssetCurrency = asset1.CurrencyId;
                    tradeTransactionReport.SrcAssetAmount = asset1.Total;
                    tradeTransactionReport.SrcAssetMovement = asset1.Move;
                }
                else
                {
                    tradeTransactionReport.SrcAssetCurrency = null;
                    tradeTransactionReport.SrcAssetAmount = null;
                    tradeTransactionReport.SrcAssetMovement = null;
                }

                TradeAssetNull asset2 = trade.DstAsset;
                if (asset2.HasValue)
                {
                    tradeTransactionReport.DstAssetCurrency = asset2.CurrencyId;
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

                if (trade.OpenConversionRate.HasValue)
                    tradeTransactionReport.OpenConversionRate = trade.OpenConversionRate.Value;

                if (trade.CloseConversionRate.HasValue)
                    tradeTransactionReport.CloseConversionRate = trade.CloseConversionRate.Value;

                if (trade.MarginCurrencyToUsdConversionRate.HasValue)
                    tradeTransactionReport.MarginCurrencyToUsdConversionRate = trade.MarginCurrencyToUsdConversionRate.Value;

                if (trade.UsdToMarginCurrencyConversionRate.HasValue)
                    tradeTransactionReport.UsdToMarginCurrencyConversionRate = trade.UsdToMarginCurrencyConversionRate.Value;

                if (trade.ProfitCurrencyToUsdConversionRate.HasValue)
                    tradeTransactionReport.ProfitCurrencyToUsdConversionRate = trade.ProfitCurrencyToUsdConversionRate.Value;

                if (trade.UsdToProfitCurrencyConversionRate.HasValue)
                    tradeTransactionReport.UsdToProfitCurrencyConversionRate = trade.UsdToProfitCurrencyConversionRate.Value;

                if (trade.SrcAssetToUsdConversionRate.HasValue)
                    tradeTransactionReport.SrcAssetToUsdConversionRate = trade.SrcAssetToUsdConversionRate.Value;

                if (trade.UsdToSrcAssetConversionRate.HasValue)
                    tradeTransactionReport.UsdToSrcAssetConversionRate = trade.UsdToSrcAssetConversionRate.Value;

                if (trade.DstAssetToUsdConversionRate.HasValue)
                    tradeTransactionReport.DstAssetToUsdConversionRate = trade.DstAssetToUsdConversionRate.Value;

                if (trade.UsdToDstAssetConversionRate.HasValue)
                    tradeTransactionReport.UsdToDstAssetConversionRate = trade.UsdToDstAssetConversionRate.Value;

                if (trade.MinCommissionConversionRate.HasValue)
                    tradeTransactionReport.MinCommissionConversionRate = trade.MinCommissionConversionRate.Value;

                if (trade.MarginCurrencyToReportConversionRate.HasValue)
                    tradeTransactionReport.MarginCurrencyToReportConversionRate = trade.MarginCurrencyToReportConversionRate.Value;

                if (trade.ReportToMarginCurrencyConversionRate.HasValue)
                    tradeTransactionReport.ReportToMarginCurrencyConversionRate = trade.ReportToMarginCurrencyConversionRate.Value;

                if (trade.ProfitCurrencyToReportConversionRate.HasValue)
                    tradeTransactionReport.ProfitCurrencyToReportConversionRate = trade.ProfitCurrencyToReportConversionRate.Value;

                if (trade.ReportToProfitCurrencyConversionRate.HasValue)
                    tradeTransactionReport.ReportToProfitCurrencyConversionRate = trade.ReportToProfitCurrencyConversionRate.Value;

                if (trade.SrcAssetToReportConversionRate.HasValue)
                    tradeTransactionReport.SrcAssetToReportConversionRate = trade.SrcAssetToReportConversionRate.Value;

                if (trade.ReportToSrcAssetConversionRate.HasValue)
                    tradeTransactionReport.ReportToSrcAssetConversionRate = trade.ReportToSrcAssetConversionRate.Value;

                if (trade.DstAssetToReportConversionRate.HasValue)
                    tradeTransactionReport.DstAssetToReportConversionRate = trade.DstAssetToReportConversionRate.Value;

                if (trade.ReportToDstAssetConversionRate.HasValue)
                    tradeTransactionReport.ReportToDstAssetConversionRate = trade.ReportToDstAssetConversionRate.Value;

                tradeTransactionReport.ReportCurrency = trade.ReportCurrency;
                tradeTransactionReport.TokenCommissionCurrency = trade.TokenCommissionCurrency;

                if (trade.TokenCommissionCurrencyDiscount.HasValue)
                    tradeTransactionReport.TokenCommissionCurrencyDiscount = trade.TokenCommissionCurrencyDiscount.Value;

                if (trade.TokenCommissionConversionRate.HasValue)
                    tradeTransactionReport.TokenCommissionConversionRate = trade.TokenCommissionConversionRate.Value;

                if (trade.SplitRatio.HasValue)
                    tradeTransactionReport.SplitRatio = trade.SplitRatio;

                if (trade.DividendGrossRate.HasValue)
                    tradeTransactionReport.DividendGrossRate = trade.DividendGrossRate;

                if (trade.DividendToBalanceConversionRate.HasValue)
                    tradeTransactionReport.DividendToBalanceConversionRate = trade.DividendToBalanceConversionRate;
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
                    accountReport.BalanceCurrency = balance.CurrencyId;
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
                    accountPosition.PosReportType = PosReportType.Response;
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
                    accountPosition.PosId = position.PosId;
                    accountPosition.Rebate = position.Rebate;
                    accountPosition.Created = position.Created;

                    accountReport.Positions[index] = accountPosition;
                }

                AccountAssetArray assets = account.Assets;
                int assetCount = assets.Length;

                accountReport.Assets = new AssetInfo[assetCount];

                for (int index = 0; index < assetCount; ++index)
                {
                    AccountAsset asset = assets[index];
                    AssetInfo assetInfo = new AssetInfo();

                    assetInfo.Currency = asset.CurrencyId;
                    assetInfo.LockedAmount = asset.Locked;
                    assetInfo.Balance = asset.Total;
                    assetInfo.SrcAssetToUsdConversionRate = asset.SrcAssetToUsdConversionRate;
                    assetInfo.UsdToSrcAssetConversionRate = asset.UsdToSrcAssetConversionRate;
                    if (asset.SrcAssetToReportConversionRate.HasValue)
                        assetInfo.SrcAssetToReportConversionRate = asset.SrcAssetToReportConversionRate;
                    if (asset.ReportToSrcAssetConversionRate.HasValue)
                        assetInfo.ReportToSrcAssetConversionRate = asset.ReportToSrcAssetConversionRate;

                    accountReport.Assets[index] = assetInfo;
                }

                AccountOrderArray orders = account.Orders;
                int orderCount = orders.Length;
                accountReport.Orders = new Order[orderCount];
                for (int index = 0; index < orderCount; index++)
                {
                    SoftFX.Net.TradeCapture.AccountOrder order = orders[index];
                    TickTrader.FDK.Common.Order accountOrder = new TickTrader.FDK.Common.Order();

                    accountOrder.OrderId = order.OrderId.ToString();
                    accountOrder.ClientOrderId = order.OrigClOrdId;
                    accountOrder.ParentOrderId = order.ParentOrderId.HasValue ? order.ParentOrderId.ToString() : null;
                    accountOrder.Symbol = order.SymbolId;
                    accountOrder.Volume = order.LeavesQty;
                    accountOrder.MaxVisibleVolume = order.MaxVisibleQty;
                    accountOrder.Type = GetOrderType(order.Type);
                    accountOrder.Side = GetOrderSide(order.Side);
                    accountOrder.Price = order.Price;
                    accountOrder.StopPrice = order.StopPrice;
                    accountOrder.Slippage = order.Slippage;
                    accountOrder.TakeProfit = order.TakeProfit;
                    accountOrder.StopLoss = order.StopLoss;
                    accountOrder.Margin = order.Margin;
                    accountOrder.ImmediateOrCancel = order.ImmediateOrCancelFlag;

                    accountOrder.InitialType = GetOrderType(order.ReqType);
                    accountOrder.InitialPrice = order.ReqPrice;
                    accountOrder.InitialVolume = order.ReqQty;

                    accountOrder.IsReducedOpenCommission = (order.CommissionFlags & CommissionFlags.OpenReduced) == CommissionFlags.OpenReduced;
                    accountOrder.IsReducedCloseCommission = (order.CommissionFlags & CommissionFlags.CloseReduced) == CommissionFlags.CloseReduced;
                    accountOrder.Commission = order.Commission;
                    accountOrder.AgentCommission = order.AgentCommission;
                    accountOrder.Swap = order.Swap;
                    accountOrder.Rebate = order.Rebate;

                    accountOrder.Expiration = order.ExpireTime;
                    accountOrder.Created = order.Created;
                    accountOrder.Modified = order.Modified;
                    accountOrder.ExecutionExpired = order.ExecutionExpired;

                    accountOrder.Comment = order.Comment;
                    accountOrder.Tag = order.Tag;
                    accountOrder.Magic = order.Magic;

                    accountOrder.OneCancelsTheOtherFlag = order.OneCancelsTheOtherFlag;
                    accountOrder.RelatedOrderId = order.RelatedOrderId;

                    accountOrder.ContingentOrderFlag = order.ContingentOrderFlag;
                    accountOrder.TriggerType = order.TriggerType.HasValue ? GetTriggerType(order.TriggerType.Value) : default(Common.ContingentOrderTriggerType?);
                    accountOrder.OrderIdTriggeredBy = order.OrderIdTriggeredBy;
                    accountOrder.TriggerTime = order.TriggerTime;

                    accountReport.Orders[index] = accountOrder;
                }

                accountReport.BalanceCurrencyToUsdConversionRate = null;
                accountReport.UsdToBalanceCurrencyConversionRate = null;
                accountReport.ProfitCurrencyToUsdConversionRate = null;
                accountReport.UsdToProfitCurrencyConversionRate = null;

                if (account.ProfitCurrencyToUsdConversionRate.HasValue)
                    accountReport.ProfitCurrencyToUsdConversionRate = account.ProfitCurrencyToUsdConversionRate.Value;

                if (account.UsdToProfitCurrencyConversionRate.HasValue)
                    accountReport.UsdToProfitCurrencyConversionRate = account.UsdToProfitCurrencyConversionRate.Value;

                if (account.BalanceCurrencyToUsdConversionRate.HasValue)
                    accountReport.BalanceCurrencyToUsdConversionRate = account.BalanceCurrencyToUsdConversionRate.Value;

                if (account.UsdToBalanceCurrencyConversionRate.HasValue)
                    accountReport.UsdToBalanceCurrencyConversionRate = account.UsdToBalanceCurrencyConversionRate.Value;

                if (account.ProfitCurrencyToReportConversionRate.HasValue)
                    accountReport.ProfitCurrencyToReportConversionRate = account.ProfitCurrencyToReportConversionRate.Value;

                if (account.ReportToProfitCurrencyConversionRate.HasValue)
                    accountReport.ReportToProfitCurrencyConversionRate = account.ReportToProfitCurrencyConversionRate.Value;

                if (account.BalanceCurrencyToReportConversionRate.HasValue)
                    accountReport.BalanceCurrencyToReportConversionRate = account.BalanceCurrencyToReportConversionRate.Value;

                if (account.ReportToBalanceCurrencyConversionRate.HasValue)
                    accountReport.ReportToBalanceCurrencyConversionRate = account.ReportToBalanceCurrencyConversionRate.Value;

                accountReport.ReportCurrency = account.ReportCurrency;
                accountReport.TokenCommissionCurrency = account.TokenCommissionCurrency;

                if (account.TokenCommissionCurrencyDiscount.HasValue)
                    accountReport.TokenCommissionCurrencyDiscount = account.TokenCommissionCurrencyDiscount.Value;

                accountReport.IsTokenCommissionEnabled = account.TokenCommissionEnabled;
                accountReport.Rebate = account.Rebate;
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
                    default:
                        return Common.RejectReason.Other;
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

                    case SoftFX.Net.TradeCapture.LoginRejectReason.MustChangePassword:
                        return TickTrader.FDK.Common.LogoutReason.MustChangePassword;

                    case SoftFX.Net.TradeCapture.LoginRejectReason.TimeoutLogin:
                        return TickTrader.FDK.Common.LogoutReason.LoginTimeout;
                    
                    case SoftFX.Net.TradeCapture.LoginRejectReason.Other:
                    default:
                        return TickTrader.FDK.Common.LogoutReason.Unknown;
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

                    case SoftFX.Net.TradeCapture.LogoutReason.MustChangePassword:
                        return TickTrader.FDK.Common.LogoutReason.MustChangePassword;

                    case SoftFX.Net.TradeCapture.LogoutReason.Other:
                    default:
                        return TickTrader.FDK.Common.LogoutReason.Unknown;
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

                    case SoftFX.Net.TradeCapture.TradeType.TradeModified:
                        return TickTrader.FDK.Common.TradeTransactionReportType.TradeModified;

                    default:
                        return TickTrader.FDK.Common.TradeTransactionReportType.None;
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

                    case SoftFX.Net.TradeCapture.TradeReason.TransferMoney:
                        return TickTrader.FDK.Common.TradeTransactionReason.TransferMoney;

                    case SoftFX.Net.TradeCapture.TradeReason.Split:
                        return TickTrader.FDK.Common.TradeTransactionReason.Split;

                    case SoftFX.Net.TradeCapture.TradeReason.Dividend:
                        return TickTrader.FDK.Common.TradeTransactionReason.Dividend;

                    case SoftFX.Net.TradeCapture.TradeReason.OneCancelsTheOther:
                        return TickTrader.FDK.Common.TradeTransactionReason.OneCancelsTheOther;

                    default:
                        return TickTrader.FDK.Common.TradeTransactionReason.None;
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
                        return TickTrader.FDK.Common.OrderType.None;
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
                        return TickTrader.FDK.Common.OrderSide.None;
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

                    case SoftFX.Net.TradeCapture.OrderTimeInForce.OneCancelsTheOther:
                        return TickTrader.FDK.Common.OrderTimeInForce.OneCancelsTheOther;

                    default:
                        return TickTrader.FDK.Common.OrderTimeInForce.Other;
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
                        return TickTrader.FDK.Common.OrderSide.None;
                }
            }

            TickTrader.FDK.Common.NotificationType GetNotificationType(SoftFX.Net.TradeCapture.NotificationType type)
            {
                switch (type)
                {
                    case SoftFX.Net.TradeCapture.NotificationType.ConfigUpdate:
                        return TickTrader.FDK.Common.NotificationType.ConfigUpdated;

                    default:
                        return TickTrader.FDK.Common.NotificationType.Unknown;
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
                        return TickTrader.FDK.Common.NotificationSeverity.Unknown;
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
                        return TickTrader.FDK.Common.AccountType.None;
                }
            }

            private ContingentOrderTriggerReport GetTriggerReport(ContingentOrderTriggerHistoryReport report)
            {
                var result = new ContingentOrderTriggerReport();
                result.ContingentOrderId = report.ContingentOrderId;
                result.Id = report.Id;
                result.OrderIdTriggeredBy = report.OrderIdTriggeredBy;
                result.TriggerState = GetResultState(report.TriggerState);
                result.TransactionTime = report.TransactionTime;
                result.TriggerTime = report.TriggerTime;
                result.TriggerType = GetTriggerType(report.TriggerType);
                result.Symbol = report.Symbol;
                result.Type = GetOrderType(report.Type);
                result.Side = GetOrderSide(report.Side);
                result.Price = report.Price;
                result.StopPrice = report.StopPrice;
                result.Amount = report.Amount;
                result.RelatedOrderId = report.RelatedOrderId;

                return result;
            }

            private Common.TriggerResultState GetResultState(SoftFX.Net.TradeCapture.TriggerResultState resultState)
            {
                switch (resultState)
                {
                    case SoftFX.Net.TradeCapture.TriggerResultState.Failed:
                        return Common.TriggerResultState.Failed;
                    case SoftFX.Net.TradeCapture.TriggerResultState.Successful:
                        return Common.TriggerResultState.Successful;
                    default:
                        return Common.TriggerResultState.Successful;
                }
            }

            Common.ContingentOrderTriggerType GetTriggerType(SoftFX.Net.TradeCapture.ContingentOrderTriggerType type)
            {
                switch (type)
                {
                    case SoftFX.Net.TradeCapture.ContingentOrderTriggerType.OnPendingOrderExpired:
                        return Common.ContingentOrderTriggerType.OnPendingOrderExpired;
                    case SoftFX.Net.TradeCapture.ContingentOrderTriggerType.OnPendingOrderPartiallyFilled:
                        return Common.ContingentOrderTriggerType.OnPendingOrderPartiallyFilled;
                    case SoftFX.Net.TradeCapture.ContingentOrderTriggerType.OnTime:
                        return Common.ContingentOrderTriggerType.OnTime;
                    default:
                        return (Common.ContingentOrderTriggerType)type;
                }
            }

            TradeCapture client_;
        }

        #endregion
    }
}
