﻿using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using SoftFX.Net.QuoteStore;
using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.Zip;
using TickTrader.FDK.Common;

namespace TickTrader.FDK.QuoteStore
{
    public class Client : IDisposable
    {
        #region Constructors

        public Client
        (
            string name,
            bool logMessages =  false,
            int port = 5050,
            int connectAttempts = -1,
            int reconnectAttempts = -1,
            int connectInterval = 10000,
            int heartbeatInterval = 10000,
            string logDirectory = "Logs"            
        )
        {
            ClientSessionOptions options = new ClientSessionOptions(port);
            options.ConnectionType = SoftFX.Net.Core.ConnectionType.Socket;
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
        public delegate void LogoutResultDelegate(Client client, object data, LogoutInfo logoutInfo);
        public delegate void LogoutErrorDelegate(Client client, object data, Exception exception);
        public delegate void LogoutDelegate(Client client, LogoutInfo logoutInfo);

        public event LoginResultDelegate LoginResultEvent;
        public event LoginErrorDelegate LoginErrorEvent;
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

        #region Quote Store

        public delegate void SymbolListResultDelegate(Client client, object data, string[] symbols);
        public delegate void SymbolListErrorDelegate(Client client, object data, Exception exception);
        public delegate void PeriodicityListResultDelegate(Client client, object data, BarPeriod[] barPeriods);
        public delegate void PeriodicityListErrorDelegate(Client client, object data, Exception exception);
        public delegate void BarListResultDelegate(Client client, object data, TickTrader.FDK.Common.Bar[] bars);
        public delegate void BarListErrorDelegate(Client client, object data, Exception exception);
        public delegate void QuoteListResultDelegate(Client client, object data, Quote[] bars);
        public delegate void QuoteListErrorDelegate(Client client, object data, Exception exception);

        public delegate void BarDownloadResultBeginDelegate(Client client, object data, string downloadId, DateTime availFrom, DateTime availTo);
        public delegate void BarDownloadResultDelegate(Client client, object data, string downloadId, TickTrader.FDK.Common.Bar bar);
        public delegate void BarDownloadResultEndDelegate(Client client, object data, string downloadId);
        public delegate void BarDownloadErrorDelegate(Client client, object data, string downloadId, Exception exception);
        public delegate void QuoteDownloadResultBeginDelegate(Client client, object data, string downloadId, DateTime availFrom, DateTime availTo);
        public delegate void QuoteDownloadResultDelegate(Client client, object data, string downloadId, Quote quote);
        public delegate void QuoteDownloadResultEndDelegate(Client client, object data, string downloadId);
        public delegate void QuoteDownloadErrorDelegate(Client client, object data, string downloadId, Exception exception);
        public delegate void NotificationDelegate(Client client, Common.Notification notification);

        public event SymbolListResultDelegate SymbolListResultEvent;
        public event SymbolListErrorDelegate SymbolListErrorEvent;
        public event PeriodicityListResultDelegate PeriodicityListResultEvent;
        public event PeriodicityListErrorDelegate PeriodicityListErrorEvent;
        public event BarListResultDelegate BarListResultEvent;
        public event BarListErrorDelegate BarListErrorEvent;
        public event QuoteListResultDelegate QuoteListResultEvent;
        public event QuoteListErrorDelegate QuoteListErrorEvent;
        public event BarDownloadResultBeginDelegate BarDownloadResultBeginEvent;
        public event BarDownloadResultDelegate BarDownloadResultEvent;
        public event BarDownloadResultEndDelegate BarDownloadResultEndEvent;
        public event BarDownloadErrorDelegate BarDownloadErrorEvent;
        public event QuoteDownloadResultBeginDelegate QuoteDownloadResultBeginEvent;
        public event QuoteDownloadResultDelegate QuoteDownloadResultEvent;
        public event QuoteDownloadResultEndDelegate QuoteDownloadResultEndEvent;
        public event QuoteDownloadErrorDelegate QuoteDownloadErrorEvent;
        public event NotificationDelegate NotificationEvent;

        public string[] GetSymbolList(int timeout)
        {
            SymbolListAsyncContext context = new SymbolListAsyncContext(true);

            GetSymbolListInternal(context);

            if (!context.Wait(timeout))
                throw new Common.TimeoutException("Method call timed out");

            if (context.exception_ != null)
                throw context.exception_;

            return context.symbols_;
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

            session_.SendSymbolListRequest(context, request);
        }

        public BarPeriod[] GetPeriodicityList(string symbol, int timeout)
        {
            PeriodictityListAsyncContext context = new PeriodictityListAsyncContext(true);

            GetPeriodicityListInternal(context, symbol);

            if (!context.Wait(timeout))
                throw new Common.TimeoutException("Method call timed out");

            if (context.exception_ != null)
                throw context.exception_;

            return context.barPeriods_;
        }

        public void GetPeriodicityListAsync(object data, string symbol)
        {
            PeriodictityListAsyncContext context = new PeriodictityListAsyncContext(false);
            context.Data = data;

            GetPeriodicityListInternal(context, symbol);
        }

        void GetPeriodicityListInternal(PeriodictityListAsyncContext context, string symbol)
        {
            PeriodicityListRequest request = new PeriodicityListRequest(0);
            request.Id = Guid.NewGuid().ToString();
            request.SymbolId = symbol;

            session_.SendPeriodicityListRequest(context, request);
        }

        public TickTrader.FDK.Common.Bar[] GetBarList(string symbol, TickTrader.FDK.Common.PriceType priceType, BarPeriod barPeriod, DateTime from, int count, int timeout)
        {
            BarListAsyncContext context = new BarListAsyncContext(true);

            GetBarListInternal(context, symbol, priceType, barPeriod, from, count);

            if (!context.Wait(timeout))
                throw new Common.TimeoutException("Method call timed out");

            if (context.exception_ != null)
                throw context.exception_;

            return context.bars_;
        }

        public void GetBarListAsync(object data, string symbol, TickTrader.FDK.Common.PriceType priceType, BarPeriod barPeriod, DateTime from, int count)
        {
            BarListAsyncContext context = new BarListAsyncContext(false);
            context.Data = data;

            GetBarListInternal(context, symbol, priceType, barPeriod, from, count);
        }

        void GetBarListInternal(BarListAsyncContext context, string symbol, TickTrader.FDK.Common.PriceType priceType, BarPeriod barPeriod, DateTime from, int count)
        {
            context.barPeriod_ = barPeriod;

            BarListRequest request = new BarListRequest(0);
            request.Id = Guid.NewGuid().ToString();
            request.SymbolId = symbol;
            request.PriceType = Convert(priceType);
            request.Periodicity = barPeriod.ToString();
            request.From = from;
            request.Count = count;

            session_.SendBarListRequest(context, request);
        }

        public Quote[] GetQuoteList(string symbol, QuoteDepth depth, DateTime from, int count, int timeout)
        {
            QuoteListAsyncContext context = new QuoteListAsyncContext(true);

            GetQuoteListInternal(context, symbol, depth, from, count);

            if (! context.Wait(timeout))
                throw new Common.TimeoutException("Method call timed out");

            if (context.exception_ != null)
                throw context.exception_;

            return context.quotes_;
        }

        public void GetQuoteListAsync(object data, string symbol, QuoteDepth depth, DateTime from, int count)
        {
            QuoteListAsyncContext context = new QuoteListAsyncContext(false);
            context.Data = data;

            GetQuoteListInternal(context, symbol, depth, from, count);
        }

        void GetQuoteListInternal(QuoteListAsyncContext context, string symbol, QuoteDepth depth, DateTime from, int count)
        {
            TickListRequest request = new TickListRequest(0);
            request.Id = Guid.NewGuid().ToString();
            request.SymbolId = symbol;
            request.Depth = Convert(depth);
            request.From = from;
            request.Count = count;

            session_.SendTickListRequest(context, request);
        }

        public BarEnumerator DownloadBars(string downloadId, string symbol, TickTrader.FDK.Common.PriceType priceType, BarPeriod barPeriod, DateTime from, DateTime to, int timeout)
        {
            BarDownloadAsyncContext context = new BarDownloadAsyncContext(true);
            context.event_ = new AutoResetEvent(false);

            DownloadBarsInternal(context, downloadId, symbol, priceType, barPeriod, from, to);

            if (! context.event_.WaitOne(timeout))
                throw new Common.TimeoutException("Method call timed out");

            if (context.exception_ != null)
                throw context.exception_;

            return context.barEnumerator_;
        }

        public void DownloadBarsAsync(object data, string downloadId, string symbol, TickTrader.FDK.Common.PriceType priceType, BarPeriod barPeriod, DateTime from, DateTime to)
        {
            BarDownloadAsyncContext context = new BarDownloadAsyncContext(false);
            context.Data = data;

            DownloadBarsInternal(context, downloadId, symbol, priceType, barPeriod, from, to);
        }

        void DownloadBarsInternal(BarDownloadAsyncContext context, string downloadId, string symbol, TickTrader.FDK.Common.PriceType priceType, BarPeriod barPeriod, DateTime from, DateTime to)
        {
            context.downloadId_ = downloadId;
            context.priceType_ = priceType;
            context.barPeriod_ = barPeriod;
            context.from_ = from;
            context.to_ = to;

            BarDownloadRequest request = new BarDownloadRequest(0);
            request.Id = downloadId;
            request.SymbolId = symbol;
            request.PriceType = Convert(priceType);
            request.Periodicity = barPeriod.ToString();
            request.From = from;
            request.To = to;

            session_.SendDownloadRequest(context, request);
        }

        public QuoteEnumerator DownloadQuotes(string downloadId, string symbol, QuoteDepth depth, DateTime from, DateTime to, int timeout)
        {
            QuoteDownloadAsyncContext context = new QuoteDownloadAsyncContext(true);
            context.event_ = new AutoResetEvent(false);

            DownloadQuotesInternal(context, downloadId, symbol, depth, from, to);

            if (! context.event_.WaitOne(timeout))
                throw new Common.TimeoutException("Method call timed out");

            if (context.exception_ != null)
                throw context.exception_;

            return context.quoteEnumerator_;
        }

        public void DownloadQuotesAsync(object data, string downloadId, string symbol, QuoteDepth depth, DateTime from, DateTime to)
        {
            QuoteDownloadAsyncContext context = new QuoteDownloadAsyncContext(false);
            context.Data = data;

            DownloadQuotesInternal(context, downloadId, symbol, depth, from, to);
        }

        void DownloadQuotesInternal(QuoteDownloadAsyncContext context, string downloadId, string symbol, QuoteDepth depth, DateTime from, DateTime to)
        {
            context.downloadId_ = downloadId;
            context.quoteDepth_ = depth;
            context.from_ = from;
            context.to_ = to;

            TickDownloadRequest request = new TickDownloadRequest(0);
            request.Id = downloadId;
            request.SymbolId = symbol;
            request.Depth = Convert(depth);
            request.From = from;
            request.To = to;

            session_.SendDownloadRequest(context, request);
        }

        public void SendDownloadCancel(string downloadId)
        {
            DownloadCancel downloadCancel = new DownloadCancel(0);
            downloadCancel.Id = downloadId;

            session_.SendDownloadCancel(null, downloadCancel);
        }

        #endregion

        #region Implementation

        SoftFX.Net.QuoteStore.PriceType Convert(TickTrader.FDK.Common.PriceType priceType)
        {
            switch (priceType)
            {
                case TickTrader.FDK.Common.PriceType.Bid:
                    return SoftFX.Net.QuoteStore.PriceType.Bid;

                case TickTrader.FDK.Common.PriceType.Ask:
                    return SoftFX.Net.QuoteStore.PriceType.Ask;

                default:
                    throw new Exception("Invalid price type: " + priceType);
            }
        }

        SoftFX.Net.QuoteStore.TickDepth Convert(TickTrader.FDK.Common.QuoteDepth quoteDepth)
        {
            switch (quoteDepth)
            {
                case TickTrader.FDK.Common.QuoteDepth.Top:
                    return SoftFX.Net.QuoteStore.TickDepth.Top;

                case TickTrader.FDK.Common.QuoteDepth.Level2:
                    return SoftFX.Net.QuoteStore.TickDepth.Level2;

                default:
                    throw new Exception("Invalid quote depth: " + quoteDepth);
            }
        }

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

        class SymbolListAsyncContext : SymbolListRequestClientContext, IAsyncContext
        {
            public SymbolListAsyncContext(bool waitable) : base(waitable)
            {
            }

            public void ProcessDisconnect(Client client, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (client.SymbolListErrorEvent != null)
                {
                    try
                    {
                        client.SymbolListErrorEvent(client, Data, exception);
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
            public string[] symbols_;
        }

        class PeriodictityListAsyncContext : PeriodicityListRequestClientContext, IAsyncContext
        {
            public PeriodictityListAsyncContext(bool waitable) : base(waitable)
            {
            }

            public void ProcessDisconnect(Client client, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (client.PeriodicityListErrorEvent != null)
                {
                    try
                    {
                        client.PeriodicityListErrorEvent(client, Data, exception);
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
            public BarPeriod[] barPeriods_;
        }

        class BarListAsyncContext : BarListRequestClientContext, IAsyncContext
        {
            public BarListAsyncContext(bool waitable) : base(waitable)
            {
            }

            public void ProcessDisconnect(Client client, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (client.BarListErrorEvent != null)
                {
                    try
                    {
                        client.BarListErrorEvent(client, Data, exception);
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

            public BarPeriod barPeriod_;
            public Exception exception_;            
            public TickTrader.FDK.Common.Bar[] bars_;
        }

        class QuoteListAsyncContext : TickListRequestClientContext, IAsyncContext
        {
            public QuoteListAsyncContext(bool waitable) : base(waitable)
            {
            }

            public void ProcessDisconnect(Client client, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (client.QuoteListErrorEvent != null)
                {
                    try
                    {
                        client.QuoteListErrorEvent(client, Data, exception);
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

        class BarDownloadAsyncContext : DownloadRequestClientContext, IAsyncContext
        {
            public BarDownloadAsyncContext(bool waitable) : base(waitable)
            {
            }

            ~BarDownloadAsyncContext()
            {
                if (event_ != null)
                    event_.Close();
            }

            public void ProcessDisconnect(Client client, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (client.BarDownloadErrorEvent != null)
                {
                    try
                    {
                        client.BarDownloadErrorEvent(client, Data, downloadId_, exception);
                    }
                    catch
                    {
                    }
                }

                if (Waitable)
                {
                    if (barEnumerator_ != null)
                    {
                        barEnumerator_.SetError(exception);
                    }
                    else
                    {
                        exception_ = exception;
                        event_.Set();
                    }
                }
            }

            public string downloadId_;
            public TickTrader.FDK.Common.PriceType priceType_;
            public BarPeriod barPeriod_;
            public DateTime from_;
            public DateTime to_;            
            public byte[] fileData_;
            public int fileSize_;
            public TickTrader.FDK.Common.Bar bar_;
            public AutoResetEvent event_;
            public Exception exception_;
            public BarEnumerator barEnumerator_;
        }

        class QuoteDownloadAsyncContext : DownloadRequestClientContext, IAsyncContext
        {
            public QuoteDownloadAsyncContext(bool waitable) : base(waitable)
            {
            }

            ~QuoteDownloadAsyncContext()
            {
                if (event_ != null)
                    event_.Close();
            }

            public void ProcessDisconnect(Client client, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (client.QuoteDownloadErrorEvent != null)
                {
                    try
                    {
                        client.QuoteDownloadErrorEvent(client, Data, downloadId_, exception);
                    }
                    catch
                    {
                    }
                }

                if (Waitable)
                {
                    if (quoteEnumerator_ != null)
                    {
                        quoteEnumerator_.SetError(exception);
                    }
                    else
                    {
                        exception_ = exception;
                        event_.Set();
                    }
                }
            }
                        
            public string downloadId_;
            public QuoteDepth quoteDepth_;
            public DateTime from_;
            public DateTime to_;
            public byte[] fileData_;
            public int fileSize_;
            public Quote quote_;
            public AutoResetEvent event_;
            public Exception exception_;
            public QuoteEnumerator quoteEnumerator_;            
        }       

        class ClientSessionListener : SoftFX.Net.QuoteStore.ClientSessionListener
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
                catch
                {
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
                }
            }

            public override void OnConnectError(ClientSession clientSession, ConnectClientContext connectContext, string text)
            {                
                try
                {
                    ConnectAsyncContext connectAsyncContext = (ConnectAsyncContext)connectContext;

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
                catch
                {
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
                catch
                {
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
                }
            }

            public override void OnLoginReport(ClientSession session, LoginRequestClientContext LoginRequestClientContext, LoginReport message)
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

            public override void OnLoginReject(ClientSession session, LoginRequestClientContext LoginRequestClientContext, LoginReject message)
            {
                LoginAsyncContext context = (LoginAsyncContext) LoginRequestClientContext;

                try
                {
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

            public override void OnLogout(ClientSession session, LogoutClientContext LogoutClientContext, Logout message)
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

            public override void OnSymbolListReport(ClientSession session, SymbolListRequestClientContext SymbolListRequestClientContext, SymbolListReport message)
            {
                SymbolListAsyncContext context = (SymbolListAsyncContext)SymbolListRequestClientContext;

                try
                {
                    StringArray reportSymbolIds = message.SymbolIds;
                    int count = reportSymbolIds.Length;
                    string[] resultSymbols = new string[count];

                    for (int index = 0; index < count; ++index)
                    {
                        resultSymbols[index] = reportSymbolIds[index];
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
                        context.symbols_ = resultSymbols;
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

            public override void OnSymbolListReject(ClientSession session, SymbolListRequestClientContext SymbolListRequestClientContext, Reject message)
            {
                var context = (SymbolListAsyncContext) SymbolListRequestClientContext;

                try
                {
                    RejectException exception = new RejectException(Common.RejectReason.None, message.Text);

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

            public override void OnPeriodicityListReport(ClientSession session, PeriodicityListRequestClientContext PeriodicityListRequestClientContext, PeriodicityListReport message)
            {
                PeriodictityListAsyncContext context = (PeriodictityListAsyncContext) PeriodicityListRequestClientContext;

                try
                {
                    StringArray reportPeriodicities = message.Periodicities;
                    int count = reportPeriodicities.Length;
                    BarPeriod[] resultPeriodicities = new BarPeriod[count];

                    for (int index = 0; index < count; ++index)
                    {
                        string reportPeriodicity = reportPeriodicities[index];
                        BarPeriod resultPeriodicity = new BarPeriod(reportPeriodicity); 

                        resultPeriodicities[index] = resultPeriodicity;
                    }

                    if (client_.PeriodicityListResultEvent != null)
                    {
                        try
                        {
                            client_.PeriodicityListResultEvent(client_, context.Data, resultPeriodicities);
                        }
                        catch
                        {
                        }
                    }

                    if (context.Waitable)
                    {
                        context.barPeriods_ = resultPeriodicities;
                    }
                }
                catch (Exception exception)
                {
                    if (client_.PeriodicityListErrorEvent != null)
                    {
                        try
                        {
                            client_.PeriodicityListErrorEvent(client_, context.Data, exception);
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

            public override void OnPeriodicityListReject(ClientSession session, PeriodicityListRequestClientContext PeriodicityListRequestClientContext, Reject message)
            {
                PeriodictityListAsyncContext context = (PeriodictityListAsyncContext) PeriodicityListRequestClientContext;

                try
                {
                    RejectException exception = new RejectException(Common.RejectReason.None, message.Text);

                    if (client_.PeriodicityListErrorEvent != null)
                    {
                        try
                        {
                            client_.PeriodicityListErrorEvent(client_, context.Data, exception);
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
                    if (client_.PeriodicityListErrorEvent != null)
                    {
                        try
                        {
                            client_.PeriodicityListErrorEvent(client_, context.Data, exception);
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

            public override void OnBarListReport(ClientSession session, BarListRequestClientContext BarListRequestClientContext, BarListReport message)
            {
                BarListAsyncContext context = (BarListAsyncContext) BarListRequestClientContext;

                try
                {
                    SoftFX.Net.QuoteStore.BarArray reportBars = message.Bars;
                    int count = reportBars.Length;
                    TickTrader.FDK.Common.Bar[] resultBars = new TickTrader.FDK.Common.Bar[count];

                    for (int index = 0; index < count; ++index)
                    {
                        SoftFX.Net.QuoteStore.Bar reportBar = reportBars[index];
                        TickTrader.FDK.Common.Bar resultBar = new TickTrader.FDK.Common.Bar();

                        resultBar.From = reportBar.Time;
                        resultBar.To = reportBar.Time + context.barPeriod_;
                        resultBar.Open = reportBar.Open;
                        resultBar.Close = reportBar.Close;
                        resultBar.High = reportBar.High;
                        resultBar.Low = reportBar.Low;
                        resultBar.Volume = reportBar.Volume;

                        resultBars[index] = resultBar;
                    }

                    if (client_.BarListResultEvent != null)
                    {
                        try
                        {
                            client_.BarListResultEvent(client_, context.Data, resultBars);
                        }
                        catch
                        {
                        }
                    }

                    if (context.Waitable)
                    {
                        context.bars_ = resultBars;
                    }
                }
                catch (Exception exception)
                {
                    if (client_.BarListErrorEvent != null)
                    {
                        try
                        {
                            client_.BarListErrorEvent(client_, context.Data, exception);
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

            public override void OnBarListReject(ClientSession session, BarListRequestClientContext BarListRequestClientContext, Reject message)
            {
                BarListAsyncContext context = (BarListAsyncContext) BarListRequestClientContext;

                try
                {
                    RejectException exception = new RejectException(Common.RejectReason.None, message.Text);

                    if (client_.BarListErrorEvent != null)
                    {
                        try
                        {
                            client_.BarListErrorEvent(client_, context.Data, exception);
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
                    if (client_.BarListErrorEvent != null)
                    {
                        try
                        {
                            client_.BarListErrorEvent(client_, context.Data, exception);
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

            public override void OnTickListReport(ClientSession session, TickListRequestClientContext TickListRequestClientContext, TickListReport message)
            {
                QuoteListAsyncContext context = (QuoteListAsyncContext) TickListRequestClientContext;

                try
                {
                    SoftFX.Net.QuoteStore.TickArray reportTicks = message.Ticks;
                    int count = reportTicks.Length;
                    TickTrader.FDK.Common.Quote[] resultQuotes = new TickTrader.FDK.Common.Quote[count];

                    for (int index = 0; index < count; ++index)
                    {
                        SoftFX.Net.QuoteStore.Tick reportTick = reportTicks[index];
                        TickTrader.FDK.Common.Quote resultQuote = new TickTrader.FDK.Common.Quote();

                        DateTime time = reportTick.Time;

                        if (reportTick.Index != 0)
                        {
                            resultQuote.Id = string.Format("{0}.{1}.{2} {3}:{4}:{5}.{6}-{7}", time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second, time.Millisecond, reportTick.Index);
                        }
                        else
                            resultQuote.Id = string.Format("{0}.{1}.{2} {3}:{4}:{5}.{6}", time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second, time.Millisecond);

                        resultQuote.CreatingTime = time;                        

                        SoftFX.Net.QuoteStore.PriceLevelArray reportBids = reportTick.Bids;
                        int bidCount = reportBids.Length;
                        List<QuoteEntry> resultBids = new List<QuoteEntry>(bidCount);

                        QuoteEntry resultBid = new QuoteEntry();

                        for (int bidIndex = 0; bidIndex < bidCount; ++bidIndex)
                        {
                            SoftFX.Net.QuoteStore.PriceLevel reportBid = reportBids[bidIndex];

                            resultBid.Price = reportBid.Price;
                            resultBid.Volume = reportBid.Size;

                            resultBids.Add(resultBid);
                        }
                        
                        SoftFX.Net.QuoteStore.PriceLevelArray reportAsks = reportTick.Asks;
                        int askCount = reportAsks.Length;
                        List<QuoteEntry> resultAsks = new List<QuoteEntry>(askCount);

                        QuoteEntry resultAsk = new QuoteEntry();

                        for (int askIndex = 0; askIndex < askCount; ++askIndex)
                        {
                            SoftFX.Net.QuoteStore.PriceLevel reportAsk = reportAsks[askIndex];

                            resultAsk.Price = reportAsk.Price;
                            resultAsk.Volume = reportAsk.Size;

                            resultAsks.Add(resultAsk);
                        }

                        resultQuote.Bids = resultBids;
                        resultQuote.Asks = resultAsks;

                        resultQuotes[index] = resultQuote;
                    }

                    if (client_.QuoteListResultEvent != null)
                    {
                        try
                        {
                            client_.QuoteListResultEvent(client_, context.Data, resultQuotes);
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
                    if (client_.QuoteListErrorEvent != null)
                    {
                        try
                        {
                            client_.QuoteListErrorEvent(client_, context.Data, exception);
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

            public override void OnTickListReject(ClientSession session, TickListRequestClientContext TickListRequestClientContext, Reject message)
            {
                QuoteListAsyncContext context = (QuoteListAsyncContext) TickListRequestClientContext;

                try
                {
                    RejectException exception = new RejectException(Common.RejectReason.None, message.Text);

                    if (client_.QuoteListErrorEvent != null)
                    {
                        try
                        {
                            client_.QuoteListErrorEvent(client_, context.Data, exception);
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
                    if (client_.QuoteListErrorEvent != null)
                    {
                        try
                        {
                            client_.QuoteListErrorEvent(client_, context.Data, exception);
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

            public override void OnDownloadBeginReport(ClientSession session, DownloadRequestClientContext DownloadRequestClientContext, DownloadBeginReport message)
            {
                if (DownloadRequestClientContext is BarDownloadAsyncContext)
                {
                    BarDownloadAsyncContext context = (BarDownloadAsyncContext) DownloadRequestClientContext;

                    try
                    {
                        ulong maxFileSize = 0;

                        FileInfoArray files = message.Files;
                        int count = files.Length;
                        for (int index = 0; index < count; ++ index)
                        {
                            SoftFX.Net.QuoteStore.FileInfo file = files[index];

                            if (file.Size > maxFileSize)
                                maxFileSize = file.Size;
                        }

                        context.fileData_ = new byte[maxFileSize];
                        context.fileSize_ = 0;
                        context.bar_ = new TickTrader.FDK.Common.Bar();

                        string requestId = message.RequestId;
                        DateTime availFrom = message.AvailFrom;
                        DateTime availTo = message.AvailTo;

                        if (client_.BarDownloadResultBeginEvent != null)
                        {
                            try
                            {
                                client_.BarDownloadResultBeginEvent(client_, context.Data, requestId, availFrom, availTo);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.barEnumerator_ = new BarEnumerator(client_, requestId, availFrom, availTo);
                            context.event_.Set();
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.BarDownloadErrorEvent != null)
                        {
                            try
                            {
                                client_.BarDownloadErrorEvent(client_, context.Data, message.RequestId, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.exception_ = exception;
                            context.event_.Set();
                        }
                    }
                }
                else
                {
                    QuoteDownloadAsyncContext context = (QuoteDownloadAsyncContext) DownloadRequestClientContext;

                    try
                    {
                        ulong maxFileSize = 0;

                        FileInfoArray files = message.Files;
                        int count = files.Length;                        
                        for (int index = 0; index < count; ++ index)
                        {
                            SoftFX.Net.QuoteStore.FileInfo file = files[index];

                            if (file.Size > maxFileSize)
                                maxFileSize = file.Size;
                        }

                        context.fileData_ = new byte[maxFileSize];
                        context.fileSize_ = 0;
                        context.quote_ = new Quote();

                        string requestId = message.RequestId;
                        DateTime availFrom = message.AvailFrom;
                        DateTime availTo = message.AvailTo;

                        if (client_.QuoteDownloadResultBeginEvent != null)
                        {
                            try
                            {
                                client_.QuoteDownloadResultBeginEvent(client_, context.Data, requestId, availFrom, availTo);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.quoteEnumerator_ = new QuoteEnumerator(client_, requestId, availFrom, availTo);
                            context.event_.Set();
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.QuoteDownloadErrorEvent != null)
                        {
                            try
                            {
                                client_.QuoteDownloadErrorEvent(client_, context.Data, message.RequestId, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.exception_ = exception;
                            context.event_.Set();
                        }
                    }
                }
            }

            public override void OnDownloadDataReport(ClientSession session, DownloadRequestClientContext DownloadRequestClientContext, DownloadDataReport message)
            {
                if (DownloadRequestClientContext is BarDownloadAsyncContext)
                {                    
                    BarDownloadAsyncContext context = (BarDownloadAsyncContext) DownloadRequestClientContext;

                    try
                    {
                        int chunkSize = message.GetChunkSize();
                        message.GetChunk(context.fileData_, context.fileSize_);
                        context.fileSize_ += chunkSize;

                        if (message.Last)
                        {
                            ProcessBarDownloadFile(context, message.RequestId);

                            context.fileSize_ = 0;
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.BarDownloadErrorEvent != null)
                        {
                            try
                            {
                                client_.BarDownloadErrorEvent(client_, context.Data, message.RequestId, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.barEnumerator_.SetError(exception);
                        }
                    }
                }
                else
                {
                    QuoteDownloadAsyncContext context = (QuoteDownloadAsyncContext) DownloadRequestClientContext;

                    try
                    {
                        int chunkSize = message.GetChunkSize();
                        message.GetChunk(context.fileData_, context.fileSize_);
                        context.fileSize_ += chunkSize;

                        if (message.Last)
                        {
                            ProcessQuoteDownloadFile(context, message.RequestId);

                            context.fileSize_ = 0;
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.QuoteDownloadErrorEvent != null)
                        {
                            try
                            {
                                client_.QuoteDownloadErrorEvent (client_, context.Data, message.RequestId, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.quoteEnumerator_.SetError(exception);
                        }
                    }
                }
            }

            void ProcessBarDownloadFile(BarDownloadAsyncContext context, string downloadId)
            {
                if (context.fileSize_ >= 4 && 
                    context.fileData_[0] == 0x50 && 
                    context.fileData_[1] == 0x4b && 
                    context.fileData_[2] == 0x03 && 
                    context.fileData_[3] == 0x04)
                {
                    using (MemoryStream memoryStream = new MemoryStream(context.fileData_, 0, context.fileSize_))
                    {
                        using (ZipFile zipFile = new ZipFile(memoryStream))
                        {
                            string fileName = context.barPeriod_.ToString() + " " + context.priceType_.ToString("g").ToLowerInvariant() + ".txt";
                            ZipEntry zipEntry = zipFile.GetEntry(fileName);

                            if (zipEntry == null)
                                throw new Exception(string.Format("Could not find file {0} inside zip archive", fileName));

                            using (Stream zipInputStream = zipFile.GetInputStream(zipEntry))
                            {
                                Serialization.BarFormatter barFormatter = new Serialization.BarFormatter(zipInputStream);

                                while (! barFormatter.IsEnd)
                                {
                                    barFormatter.Deserialize(context.barPeriod_, context.bar_);

                                    if (context.bar_.From < context.from_)
                                        continue;

                                    if (context.bar_.From > context.to_)
                                        break;

                                    if (client_.BarDownloadResultEvent != null)
                                    {
                                        try
                                        {
                                            client_.BarDownloadResultEvent(client_, context.Data, downloadId, context.bar_);
                                        }
                                        catch
                                        {
                                        }
                                    }

                                    if (context.Waitable)
                                    {
                                        TickTrader.FDK.Common.Bar bar = context.bar_.Clone();

                                        context.barEnumerator_.SetResult(bar);
                                    }
                                }                                    
                            }
                        }
                    }
                }
                else
                {
                    using (MemoryStream memoryStream = new MemoryStream(context.fileData_, 0, context.fileSize_))
                    {
                        Serialization.BarFormatter barFormatter = new Serialization.BarFormatter(memoryStream);

                        while (! barFormatter.IsEnd)
                        {
                            barFormatter.Deserialize(context.barPeriod_, context.bar_);

                            if (context.bar_.From < context.from_)
                                continue;

                            if (context.bar_.From > context.to_)
                                break;

                            if (client_.BarDownloadResultEvent != null)
                            {
                                try
                                {
                                    client_.BarDownloadResultEvent(client_, context.Data, downloadId, context.bar_);
                                }
                                catch
                                {
                                }
                            }

                            if (context.Waitable)
                            {
                                TickTrader.FDK.Common.Bar bar = context.bar_.Clone();

                                context.barEnumerator_.SetResult(bar);
                            }
                        }
                    }
                }
            }

            void ProcessQuoteDownloadFile(QuoteDownloadAsyncContext context, string downloadId)
            {
                if (context.fileSize_ >= 4 && 
                    context.fileData_[0] == 0x50 && 
                    context.fileData_[1] == 0x4b && 
                    context.fileData_[2] == 0x03 && 
                    context.fileData_[3] == 0x04)
                {
                    using (MemoryStream memoryStream = new MemoryStream(context.fileData_, 0, context.fileSize_))
                    {
                        using (ZipFile zipFile = new ZipFile(memoryStream))
                        {
                            string fileName = (context.quoteDepth_ == QuoteDepth.Level2 ? "ticks level2" : "ticks") + ".txt";
                            ZipEntry zipEntry = zipFile.GetEntry(fileName);

                            if (zipEntry == null)
                                throw new Exception(string.Format("Could not find file {0} inside zip archive", fileName));

                            using (Stream zipInputStream = zipFile.GetInputStream(zipEntry))
                            {
                                Serialization.TickFormatter tickFormatter = new Serialization.TickFormatter(context.quoteDepth_, zipInputStream);

                                while (! tickFormatter.IsEnd)
                                {
                                    tickFormatter.Deserialize(context.quote_);

                                    if (context.quote_.CreatingTime < context.from_)
                                        continue;

                                    if (context.quote_.CreatingTime > context.to_)
                                        break;

                                    if (client_.QuoteDownloadResultEvent != null)
                                    {
                                        try
                                        {
                                            client_.QuoteDownloadResultEvent(client_, context.Data, downloadId, context.quote_);
                                        }
                                        catch
                                        {
                                        }
                                    }

                                    if (context.Waitable)
                                    {
                                        Quote quote = context.quote_.Clone();

                                        context.quoteEnumerator_.SetResult(quote);
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    using (MemoryStream memoryStream = new MemoryStream(context.fileData_, 0, context.fileSize_))
                    {
                        Serialization.TickFormatter tickFormatter = new Serialization.TickFormatter(context.quoteDepth_, memoryStream);

                        while (! tickFormatter.IsEnd)
                        {
                            tickFormatter.Deserialize(context.quote_);

                            if (context.quote_.CreatingTime < context.from_)
                                continue;

                            if (context.quote_.CreatingTime > context.to_)
                                break;

                            if (client_.QuoteDownloadResultEvent != null)
                            {
                                try
                                {
                                    client_.QuoteDownloadResultEvent(client_, context.Data, downloadId, context.quote_);
                                }
                                catch
                                {
                                }
                            }

                            if (context.Waitable)
                            {
                                Quote quote = context.quote_.Clone();

                                context.quoteEnumerator_.SetResult(quote);
                            }
                        }                                    
                    }
                }
            }

            public override void OnDownloadEndReport(ClientSession session, DownloadRequestClientContext DownloadRequestClientContext, DownloadEndReport message)
            {
                if (DownloadRequestClientContext is BarDownloadAsyncContext)
                {
                    BarDownloadAsyncContext context = (BarDownloadAsyncContext) DownloadRequestClientContext;

                    try
                    {
                        if (client_.BarDownloadResultEndEvent != null)
                        {
                            try
                            {
                                client_.BarDownloadResultEndEvent(client_, context.Data, message.RequestId);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.barEnumerator_.SetEnd();
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.BarDownloadErrorEvent != null)
                        {
                            try
                            {
                                client_.BarDownloadErrorEvent(client_, context.Data, message.RequestId, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.barEnumerator_.SetError(exception);
                        }
                    }
                }
                else
                {
                    QuoteDownloadAsyncContext context = (QuoteDownloadAsyncContext) DownloadRequestClientContext;

                    try
                    {
                        if (client_.QuoteDownloadResultEndEvent != null)
                        {
                            try
                            {
                                client_.QuoteDownloadResultEndEvent(client_, context.Data, message.RequestId);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.quoteEnumerator_.SetEnd();
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.QuoteDownloadErrorEvent != null)
                        {
                            try
                            {
                                client_.QuoteDownloadErrorEvent(client_, context.Data, message.RequestId, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.quoteEnumerator_.SetError(exception);
                        }
                    }
                }
            }

            public override void OnDownloadReject(ClientSession session, DownloadRequestClientContext DownloadRequestClientContext, Reject message)
            {
                if (DownloadRequestClientContext is BarDownloadAsyncContext)
                {
                    BarDownloadAsyncContext context = (BarDownloadAsyncContext) DownloadRequestClientContext;

                    try
                    {
                        RejectException exception = new RejectException(Common.RejectReason.None, message.Text);

                        if (client_.BarDownloadErrorEvent != null)
                        {
                            try
                            {
                                client_.BarDownloadErrorEvent(client_, context.Data, message.RequestId, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            if (context.barEnumerator_ != null)
                            {
                                context.barEnumerator_.SetError(exception);
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
                        if (client_.BarDownloadErrorEvent != null)
                        {
                            try
                            {
                                client_.BarDownloadErrorEvent(client_, context.Data, message.RequestId, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            if (context.barEnumerator_ != null)
                            {
                                context.barEnumerator_.SetError(exception);
                            }
                            else
                            {
                                context.exception_ = exception;
                                context.event_.Set();
                            }
                        }
                    }
                }
                else
                {
                    QuoteDownloadAsyncContext context = (QuoteDownloadAsyncContext) DownloadRequestClientContext;

                    try
                    {
                        RejectException exception = new RejectException(Common.RejectReason.None, message.Text);

                        if (client_.QuoteDownloadErrorEvent != null)
                        {
                            try
                            {
                                client_.QuoteDownloadErrorEvent(client_, context.Data, message.RequestId, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            if (context.quoteEnumerator_ != null)
                            {
                                context.quoteEnumerator_.SetError(exception);
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
                        if (client_.QuoteDownloadErrorEvent != null)
                        {
                            try
                            {
                                client_.QuoteDownloadErrorEvent(client_, context.Data, message.RequestId, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            if (context.quoteEnumerator_ != null)
                            {
                                context.quoteEnumerator_.SetError(exception);
                            }
                            else
                            {
                                context.exception_ = exception;
                                context.event_.Set();
                            }
                        }
                    }
                }
            }

            public override void OnLogout(ClientSession session, Logout message)
            {
                try
                {
                    var result = new LogoutInfo();
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
                catch
                {
                }
            }

            public override void OnNotification(ClientSession session, SoftFX.Net.QuoteStore.Notification message)
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
                catch
                {
                }
            }

            TickTrader.FDK.Common.LogoutReason Convert(SoftFX.Net.QuoteStore.LoginRejectReason reason)
            {
                switch (reason)
                {
                    case SoftFX.Net.QuoteStore.LoginRejectReason.IncorrectCredentials:
                        return TickTrader.FDK.Common.LogoutReason.InvalidCredentials;

                    case SoftFX.Net.QuoteStore.LoginRejectReason.ThrottlingLimits:
                        return TickTrader.FDK.Common.LogoutReason.Unknown;

                    case SoftFX.Net.QuoteStore.LoginRejectReason.BlockedLogin:
                        return TickTrader.FDK.Common.LogoutReason.BlockedAccount;

                    case SoftFX.Net.QuoteStore.LoginRejectReason.InternalServerError:
                        return TickTrader.FDK.Common.LogoutReason.ServerError;

                    case SoftFX.Net.QuoteStore.LoginRejectReason.Other:
                        return TickTrader.FDK.Common.LogoutReason.Unknown;

                    default:
                        throw new Exception("Invalid login reject reason : " + reason);
                }
            }

            TickTrader.FDK.Common.LogoutReason Convert(SoftFX.Net.QuoteStore.LogoutReason reason)
            {
                switch (reason)
                {
                    case SoftFX.Net.QuoteStore.LogoutReason.ClientLogout:
                        return TickTrader.FDK.Common.LogoutReason.ClientInitiated;

                    case SoftFX.Net.QuoteStore.LogoutReason.ServerLogout:
                        return TickTrader.FDK.Common.LogoutReason.ServerLogout;

                    case SoftFX.Net.QuoteStore.LogoutReason.DeletedLogin:
                        return TickTrader.FDK.Common.LogoutReason.LoginDeleted;

                    case SoftFX.Net.QuoteStore.LogoutReason.InternalServerError:
                        return TickTrader.FDK.Common.LogoutReason.ServerError;

                    case SoftFX.Net.QuoteStore.LogoutReason.BlockedLogin:
                        return TickTrader.FDK.Common.LogoutReason.BlockedAccount;

                    case SoftFX.Net.QuoteStore.LogoutReason.Other:
                        return TickTrader.FDK.Common.LogoutReason.Unknown;

                    default:
                        throw new Exception("Invalid logout reason : " + reason);
                }
            }

            TickTrader.FDK.Common.NotificationType Convert(SoftFX.Net.QuoteStore.NotificationType type)
            {
                switch (type)
                {
                    case SoftFX.Net.QuoteStore.NotificationType.ConfigUpdate:
                        return TickTrader.FDK.Common.NotificationType.ConfigUpdated;

                    default:
                        throw new Exception("Invalid notification type : " + type);
                }
            }

            TickTrader.FDK.Common.NotificationSeverity Convert(SoftFX.Net.QuoteStore.NotificationSeverity severity)
            {
                switch (severity)
                {
                    case SoftFX.Net.QuoteStore.NotificationSeverity.Info:
                        return TickTrader.FDK.Common.NotificationSeverity.Information;

                    case SoftFX.Net.QuoteStore.NotificationSeverity.Warning:
                        return TickTrader.FDK.Common.NotificationSeverity.Warning;

                    case SoftFX.Net.QuoteStore.NotificationSeverity.Error:
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
