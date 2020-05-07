using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using SoftFX.Net.QuoteStore;
using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.Zip;
using TickTrader.FDK.Common;
using System.Net;
using PriceType = TickTrader.FDK.Common.PriceType;
using TickTrader.FDK.Client.Splits;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace TickTrader.FDK.Client
{
    public class QuoteStore : IDisposable
    {
        #region Constructors

        public QuoteStore
        (
            string name,
            bool logEvents = false,
            bool logStates = false,
            bool logMessages = false,
            int port = 5042,
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
            string proxyPassword = null
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

            session_ = new ClientSession(name, options);
            sessionListener_ = new ClientSessionListener(this);
            session_.Listener = sessionListener_;
            protocolSpec_ = new ProtocolSpec();

            modifiedBarsCache_ = new SEModifiedBarsCache();
            modifiedBarsSlowGetter_ = new SEModifiedBarsFromServerGetter(this);
            historyModifier_ = new SEHistoryModifier(modifiedBarsSlowGetter_, modifiedBarsCache_);
            symbolToLastEventId_ = new ConcurrentDictionary<string, long>();
        }

        ClientSession session_;
        ClientSessionListener sessionListener_;
        ProtocolSpec protocolSpec_;

        ConcurrentDictionary<string, long> symbolToLastEventId_;

        SEModifiedBarsCache modifiedBarsCache_;
        SEModifiedBarsFromServerGetter modifiedBarsSlowGetter_;
        SEHistoryModifier historyModifier_;

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

        public delegate void ConnectResultDelegate(QuoteStore quoteStore, object data);
        public delegate void ConnectErrorDelegate(QuoteStore quoteStore, object data, Exception exception);
        public delegate void DisconnectResultDelegate(QuoteStore quoteStore, object data, string text);
        public delegate void DisconnectDelegate(QuoteStore quoteStore, string text);
        public delegate void ReconnectDelegate(QuoteStore quoteStore);
        public delegate void ReconnectErrorDelegate(QuoteStore quoteStore, Exception exception);
        public delegate void SendDelegate(QuoteStore quoteStore);

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

            if (!context.Wait(timeout))
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

        public delegate void LoginResultDelegate(QuoteStore quoteStore, object data);
        public delegate void LoginErrorDelegate(QuoteStore quoteStore, object data, Exception exception);
        public delegate void LogoutResultDelegate(QuoteStore quoteStore, object data, LogoutInfo logoutInfo);
        public delegate void LogoutErrorDelegate(QuoteStore quoteStore, object data, Exception exception);
        public delegate void LogoutDelegate(QuoteStore quoteStore, LogoutInfo logoutInfo);

        public event LoginResultDelegate LoginResultEvent;
        public event LoginErrorDelegate LoginErrorEvent;
        public event LogoutResultDelegate LogoutResultEvent;
        public event LogoutErrorDelegate LogoutErrorEvent;
        public event LogoutDelegate LogoutEvent;

        public void Login(string username, string password, string deviceId, string appId, string sessionId, int timeout)
        {
            LoginAsyncContext context = new LoginAsyncContext(true);

            LoginInternal(context, username, password, deviceId, appId, sessionId);

            if (!context.Wait(timeout))
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
            protocolSpec_.InitQuoteStoreVersion(new ProtocolVersion(session_.MajorVersion, session_.MinorVersion));

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

        public delegate void SymbolListResultDelegate(QuoteStore quoteStore, object data, string[] symbols);
        public delegate void SymbolListErrorDelegate(QuoteStore quoteStore, object data, Exception exception);
        public delegate void PeriodicityListResultDelegate(QuoteStore quoteStore, object data, BarPeriod[] barPeriods);
        public delegate void PeriodicityListErrorDelegate(QuoteStore quoteStore, object data, Exception exception);
        public delegate void StockEventQHModifierListResultDelegate(QuoteStore quoteStore, object data, SEQHModifier[] symbols);
        public delegate void StockEventQHModifierListErrorDelegate(QuoteStore quoteStore, object data, Exception exception);
        public delegate void BarListResultDelegate(QuoteStore quoteStore, object data, TickTrader.FDK.Common.Bar[] bars);
        public delegate void BarListErrorDelegate(QuoteStore quoteStore, object data, Exception exception);
        public delegate void QuoteListResultDelegate(QuoteStore quoteStore, object data, Quote[] bars);
        public delegate void QuoteListErrorDelegate(QuoteStore quoteStore, object data, Exception exception);
        public delegate void HistoryInfoResultDelegate(QuoteStore quoteStore, object data, HistoryInfo historyInfo);
        public delegate void HistoryInfoErrorDelegate(QuoteStore quoteStore, object data, Exception exception);

        public delegate void BarDownloadResultBeginDelegate(QuoteStore quoteStore, object data, string id, DateTime availFrom, DateTime availTo);
        public delegate void BarDownloadResultDelegate(QuoteStore quoteStore, object data, TickTrader.FDK.Common.Bar bar);
        public delegate void BarDownloadResultEndDelegate(QuoteStore quoteStore, object data);
        public delegate void BarDownloadErrorDelegate(QuoteStore quoteStore, object data, Exception exception);
        public delegate void CancelDownloadBarsResultDelegate(QuoteStore quoteStore, object data);
        public delegate void CancelDownloadBarsErrorDelegate(QuoteStore quoteStore, object data, Exception exception);
        public delegate void QuoteDownloadResultBeginDelegate(QuoteStore quoteStore, object data, string id, DateTime availFrom, DateTime availTo);
        public delegate void QuoteDownloadResultDelegate(QuoteStore quoteStore, object data, Quote quote);
        public delegate void QuoteDownloadResultEndDelegate(QuoteStore quoteStore, object data);
        public delegate void QuoteDownloadErrorDelegate(QuoteStore quoteStore, object data, Exception exception);
        public delegate void CancelDownloadQuotesResultDelegate(QuoteStore quoteStore, object data);
        public delegate void CancelDownloadQuotesErrorDelegate(QuoteStore quoteStore, object data, Exception exception);
        public delegate void NotificationDelegate(QuoteStore quoteStore, Common.Notification notification);

        public event SymbolListResultDelegate SymbolListResultEvent;
        public event SymbolListErrorDelegate SymbolListErrorEvent;
        public event StockEventQHModifierListResultDelegate StockEventQHModifierListResultEvent;
        public event StockEventQHModifierListErrorDelegate StockEventQHModifierListErrorEvent;
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
        public event CancelDownloadBarsResultDelegate CancelDownloadBarsResultEvent;
        public event CancelDownloadBarsErrorDelegate CancelDownloadBarsErrorEvent;
        public event QuoteDownloadResultBeginDelegate QuoteDownloadResultBeginEvent;
        public event QuoteDownloadResultDelegate QuoteDownloadResultEvent;
        public event QuoteDownloadResultEndDelegate QuoteDownloadResultEndEvent;
        public event QuoteDownloadErrorDelegate QuoteDownloadErrorEvent;
        public event CancelDownloadQuotesResultDelegate CancelDownloadQuotesResultEvent;
        public event CancelDownloadQuotesErrorDelegate CancelDownloadQuotesErrorEvent;
        public event NotificationDelegate NotificationEvent;
        public event HistoryInfoResultDelegate HistoryInfoResultEvent;
        public event HistoryInfoErrorDelegate HistoryInfoErrorEvent;

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

        public SEQHModifier[] GetStockEventQHModifierList(string symbol, int timeout)
        {
            //protocolSpec_.CheckSupportedStockEventQHModifierList();
            if (!protocolSpec_.SupportsStockEventQHModifierList)
                return new SEQHModifier[0];

            StockEventQHModifierListAsyncContext context = new StockEventQHModifierListAsyncContext(true);

            GetStockEventQHModifierListInternal(context, symbol);

            if (!context.Wait(timeout))
                throw new Common.TimeoutException("Method call timed out");

            if (context.exception_ != null)
                throw context.exception_;

            return context.stockEvents_;
        }

        public void GetStockEventQHModifierListAsync(object data, string symbol)
        {
            protocolSpec_.CheckSupportedStockEventQHModifierList();

            StockEventQHModifierListAsyncContext context = new StockEventQHModifierListAsyncContext(false);
            context.Data = data;

            GetStockEventQHModifierListInternal(context, symbol);
        }

        void GetStockEventQHModifierListInternal(StockEventQHModifierListAsyncContext context, string symbol)
        {
            StockEventQHModifierListRequest request = new StockEventQHModifierListRequest(0);
            request.Id = Guid.NewGuid().ToString();
            request.Symbol = symbol;
            session_.SendStockEventQHModifierListRequest(context, request);
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
            request.PriceType = GetPriceType(priceType);
            request.Periodicity = barPeriod.ToString();
            request.From = from;
            request.Count = count;

            session_.SendBarListRequest(context, request);
        }

        public Quote[] GetQuoteList(string symbol, QuoteDepth depth, DateTime from, int count, int timeout)
        {
            QuoteListAsyncContext context = new QuoteListAsyncContext(true);

            GetQuoteListInternal(context, symbol, depth, from, count);

            if (!context.Wait(timeout))
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
            request.Depth = GetTickDepth(depth);
            request.From = from;
            request.Count = count;

            session_.SendTickListRequest(context, request);
        }

        public Quote[] GetVWAPQuoteList(string symbol, short degree, DateTime from, int count, int timeout)
        {
            VWAPQuoteListAsyncContext context = new VWAPQuoteListAsyncContext(true);

            GetVWAPQuoteListInternal(context, symbol, degree, from, count);

            if (!context.Wait(timeout))
                throw new Common.TimeoutException("Method call timed out");

            if (context.exception_ != null)
                throw context.exception_;

            return context.quotes_;
        }

        public void GetVWAPQuoteListAsync(object data, string symbol, short degree, DateTime from, int count)
        {
            VWAPQuoteListAsyncContext context = new VWAPQuoteListAsyncContext(false);
            context.Data = data;

            GetVWAPQuoteListInternal(context, symbol, degree, from, count);
        }

        void GetVWAPQuoteListInternal(VWAPQuoteListAsyncContext context, string symbol, short degree, DateTime from, int count)
        {
            protocolSpec_.CheckSupportedVWAPTickList();

            VWAPTickListRequest request = new VWAPTickListRequest(0);
            request.Id = Guid.NewGuid().ToString();
            request.SymbolId = symbol;
            request.Degree = degree;
            request.From = from;
            request.Count = count;

            session_.SendVWAPTickListRequest(context, request);
        }

        public DownloadBarsEnumerator DownloadBars(string symbol, TickTrader.FDK.Common.PriceType priceType, BarPeriod barPeriod, DateTime from, DateTime to, int timeout)
        {
            GetBarPeriodicity(ref from, ref to, barPeriod);

            long periodMilliseconds = barPeriod.ToMilliseconds();
            long m1PeriodMilliseconds = BarPeriod.M1.ToMilliseconds();

            var splits = GetStockEventQHModifierList(symbol, timeout);

            var periodicity = Periodicity.Parse(barPeriod.ToString());

            if (periodMilliseconds >= m1PeriodMilliseconds)
            {
                BarDownloadAsyncContext context = new BarDownloadAsyncContext(historyModifier_.GetRangeModifier(symbol, periodicity, priceType, new ReadOnlyCollection<SEQHModifier>(splits)), true);
                context.enumerartor_ = new DownloadBarsEnumerator(this);

                DownloadBarsInternal(context, symbol, priceType, barPeriod, from, to);

                context.enumerartor_.Begin(timeout);

                return context.enumerartor_;
            }
            else
            {
                BarQuoteDownloadAsyncContext context = new BarQuoteDownloadAsyncContext(historyModifier_.GetRangeModifier(symbol, periodicity, priceType, new ReadOnlyCollection<SEQHModifier>(splits)), true);
                context.enumerartor_ = new DownloadBarsEnumerator(this);

                DownloadBarsInternal(context, symbol, priceType, barPeriod, from, to);

                context.enumerartor_.Begin(timeout);

                return context.enumerartor_;
            }
        }

        public void DownloadBarsAsync(object data, string symbol, TickTrader.FDK.Common.PriceType priceType, BarPeriod barPeriod, DateTime from, DateTime to)
        {
            GetBarPeriodicity(ref from, ref to, barPeriod);

            long periodMilliseconds = barPeriod.ToMilliseconds();
            long m1PeriodMilliseconds = BarPeriod.M1.ToMilliseconds();

            var splits = GetStockEventQHModifierList(symbol, 10000);

            var periodicity = Periodicity.Parse(barPeriod.ToString());

            if (periodMilliseconds >= m1PeriodMilliseconds)
            {
                BarDownloadAsyncContext context = new BarDownloadAsyncContext(historyModifier_.GetRangeModifier(symbol, periodicity, priceType, new ReadOnlyCollection<SEQHModifier>(splits)), false);
                context.Data = data;

                DownloadBarsInternal(context, symbol, priceType, barPeriod, from, to);
            }
            else
            {
                BarQuoteDownloadAsyncContext context = new BarQuoteDownloadAsyncContext(historyModifier_.GetRangeModifier(symbol, periodicity, priceType, new ReadOnlyCollection<SEQHModifier>(splits)), false);
                context.Data = data;

                DownloadBarsInternal(context, symbol, priceType, barPeriod, from, to);
            }
        }

        void DownloadBarsInternal(BarDownloadAsyncContext context, string symbol, TickTrader.FDK.Common.PriceType priceType, BarPeriod barPeriod, DateTime from, DateTime to)
        {
            long calcRangeMilliseconds = (long)((to - from).TotalMilliseconds);
            long calcPeriodMilliseconds = barPeriod.ToMilliseconds();

            string id = Guid.NewGuid().ToString();

            context.downloadId_ = id;
            context.priceType_ = priceType;
            context.calcBarPeriod_ = barPeriod;

            long h1PeriodMilliseconds = BarPeriod.H1.ToMilliseconds();

            if (calcPeriodMilliseconds >= h1PeriodMilliseconds)
            {
                context.barPeriod_ = BarPeriod.H1;
            }
            else
                context.barPeriod_ = BarPeriod.M1;

            context.from_ = from;
            context.to_ = to;

            BarDownloadRequest request = new BarDownloadRequest(0);
            request.Id = id;
            request.SymbolId = symbol;
            request.PriceType = GetPriceType(priceType);
            request.Periodicity = context.barPeriod_.ToString();
            request.From = from;
            request.To = to;

            session_.SendDownloadRequest(context, request);
        }

        void DownloadBarsInternal(BarQuoteDownloadAsyncContext context, string symbol, TickTrader.FDK.Common.PriceType priceType, BarPeriod barPeriod, DateTime from, DateTime to)
        {
            long calcRangeMilliseconds = (long)((to - from).TotalMilliseconds);
            long calcPeriodMilliseconds = barPeriod.ToMilliseconds();

            string id = Guid.NewGuid().ToString();

            context.downloadId_ = id;
            context.priceType_ = priceType;
            context.calcBarPeriod_ = barPeriod;
            context.from_ = from;
            context.to_ = to;

            TickDownloadRequest request = new TickDownloadRequest(0);
            request.Id = id;
            request.SymbolId = symbol;
            request.Depth = TickDepth.Top;
            request.From = from;
            request.To = to;

            session_.SendDownloadRequest(context, request);
        }

        public void CancelDownloadBars(string id, int timeout)
        {
            CancelDownloadBarsAsyncContext context = new CancelDownloadBarsAsyncContext(true);

            CancelDownloadBarsInternal(context, id);

            if (!context.Wait(timeout))
                throw new Common.TimeoutException("Method call timed out");

            if (context.exception_ != null)
                throw context.exception_;
        }

        public void CancelDownloadBarsAsync(object data, string id)
        {
            CancelDownloadBarsAsyncContext context = new CancelDownloadBarsAsyncContext(false);
            context.Data = data;

            CancelDownloadBarsInternal(context, id);
        }

        void CancelDownloadBarsInternal(CancelDownloadBarsAsyncContext context, string id)
        {
            DownloadCancelRequest request = new DownloadCancelRequest(0);
            request.Id = Guid.NewGuid().ToString();
            request.RequestId = id;

            session_.SendDownloadCancelRequest(context, request);
        }

        public DownloadQuotesEnumerator DownloadQuotes(string symbol, QuoteDepth depth, DateTime from, DateTime to, int timeout)
        {
            var splits = GetStockEventQHModifierList(symbol, 10000);

            QuoteDownloadAsyncContext context = new QuoteDownloadAsyncContext(historyModifier_.GetRangeModifier(symbol, Periodicity.None, new PriceType(), new ReadOnlyCollection<SEQHModifier>(splits)), true);
            context.enumerartor_ = new DownloadQuotesEnumerator(this);

            DownloadQuotesInternal(context, symbol, depth, from, to);

            context.enumerartor_.Begin(timeout);

            return context.enumerartor_;
        }

        public void DownloadQuotesAsync(object data, string symbol, QuoteDepth depth, DateTime from, DateTime to)
        {
            var splits = GetStockEventQHModifierList(symbol, 10000);
            QuoteDownloadAsyncContext context = new QuoteDownloadAsyncContext(historyModifier_.GetRangeModifier(symbol, Periodicity.None, new PriceType(), new ReadOnlyCollection<SEQHModifier>(splits)), false);
            context.Data = data;

            DownloadQuotesInternal(context, symbol, depth, from, to);
        }

        public DownloadQuotesEnumerator DownloadVWAPQuotes(string symbol, short degree, DateTime from, DateTime to, int timeout)
        {
            var splits = GetStockEventQHModifierList(symbol, 10000);
            VWAPQuoteDownloadAsyncContext context = new VWAPQuoteDownloadAsyncContext(historyModifier_.GetRangeModifier(symbol, Periodicity.None, new PriceType(), new ReadOnlyCollection<SEQHModifier>(splits)), true);
            context.enumerartor_ = new DownloadQuotesEnumerator(this);

            DownloadVWAPQuotesInternal(context, symbol, degree, from, to);

            context.enumerartor_.Begin(timeout);

            return context.enumerartor_;
        }
        public void DownloadVWAPQuotesAsync(object data, string symbol, short degree, DateTime from, DateTime to)
        {
            var splits = GetStockEventQHModifierList(symbol, 10000);
            VWAPQuoteDownloadAsyncContext context = new VWAPQuoteDownloadAsyncContext(historyModifier_.GetRangeModifier(symbol, Periodicity.None, new PriceType(), new ReadOnlyCollection<SEQHModifier>(splits)), false);
            context.Data = data;

            DownloadVWAPQuotesInternal(context, symbol, degree, from, to);
        }

        void DownloadQuotesInternal(QuoteDownloadAsyncContext context, string symbol, QuoteDepth depth, DateTime from, DateTime to)
        {

            string id = Guid.NewGuid().ToString();

            context.downloadId_ = id;
            context.quoteDepth_ = depth;
            context.from_ = from;
            context.to_ = to;

            TickDownloadRequest request = new TickDownloadRequest(0);
            request.Id = id;
            request.SymbolId = symbol;
            request.Depth = GetTickDepth(depth);
            request.From = from;
            request.To = to;

            session_.SendDownloadRequest(context, request);
        }

        void DownloadVWAPQuotesInternal(VWAPQuoteDownloadAsyncContext context, string symbol, short degree, DateTime from, DateTime to)
        {
            protocolSpec_.CheckSupportedVWAPTickList();

            string id = Guid.NewGuid().ToString();

            context.downloadId_ = id;
            context.degree_ = degree;
            context.from_ = from;
            context.to_ = to;

            VWAPTickDownloadRequest request = new VWAPTickDownloadRequest(0);
            request.Id = id;
            request.SymbolId = symbol;
            request.Degree = degree;
            request.From = from;
            request.To = to;

            session_.SendDownloadRequest(context, request);
        }

        public void CancelDownloadQuotes(string id, int timeout)
        {
            CancelDownloadQuotesAsyncContext context = new CancelDownloadQuotesAsyncContext(true);

            CancelDownloadQuotesInternal(context, id);

            if (!context.Wait(timeout))
                throw new Common.TimeoutException("Method call timed out");

            if (context.exception_ != null)
                throw context.exception_;
        }

        public void CancelDownloadQuotesAsync(object data, string id)
        {
            CancelDownloadQuotesAsyncContext context = new CancelDownloadQuotesAsyncContext(false);
            context.Data = data;

            CancelDownloadQuotesInternal(context, id);
        }

        void CancelDownloadQuotesInternal(CancelDownloadQuotesAsyncContext context, string id)
        {
            DownloadCancelRequest request = new DownloadCancelRequest(0);
            request.Id = Guid.NewGuid().ToString();
            request.RequestId = id;

            session_.SendDownloadCancelRequest(context, request);
        }

        public HistoryInfo GetBarsHistoryInfo(string symbol, BarPeriod period, PriceType priceType, int timeout)
        {
            HistoryInfoAsyncContext context = new HistoryInfoAsyncContext(true);

            HistoryInfoRequest request = new HistoryInfoRequest(0);
            request.Id = Guid.NewGuid().ToString();
            request.SymbolId = symbol;
            request.HistoryType = HistoryType.Bars;
            request.Periodicity = period.ToString();
            request.PriceType = GetPriceType(priceType);

            session_.SendHistoryInfoRequest(context, request);

            if (!context.Wait(timeout))
                throw new Common.TimeoutException("Method call timed out");

            if (context.exception_ != null)
                throw context.exception_;

            return context.historyInfo_;
        }

        public void GetBarsHistoryInfoAsync(object data, string symbol, BarPeriod period, PriceType priceType)
        {
            HistoryInfoAsyncContext context = new HistoryInfoAsyncContext(true);
            context.Data = data;

            HistoryInfoRequest request = new HistoryInfoRequest(0);
            request.Id = Guid.NewGuid().ToString();
            request.SymbolId = symbol;
            request.HistoryType = HistoryType.Bars;
            request.Periodicity = period.ToString();
            request.PriceType = GetPriceType(priceType);

            session_.SendHistoryInfoRequest(context, request);
        }

        public HistoryInfo GetQuotesHistoryInfo(string symbol, bool level2, int timeout)
        {
            HistoryInfoAsyncContext context = new HistoryInfoAsyncContext(true);

            HistoryInfoRequest request = new HistoryInfoRequest(0);
            request.Id = Guid.NewGuid().ToString();
            request.SymbolId = symbol;
            request.HistoryType = level2 ? HistoryType.TicksLevel2 : HistoryType.Ticks;

            session_.SendHistoryInfoRequest(context, request);

            if (!context.Wait(timeout))
                throw new Common.TimeoutException("Method call timed out");

            if (context.exception_ != null)
                throw context.exception_;

            return context.historyInfo_;
        }

        public void GetQuotesHistoryInfoAsync(object data, string symbol, bool level2)
        {
            HistoryInfoAsyncContext context = new HistoryInfoAsyncContext(true);
            context.Data = data;

            HistoryInfoRequest request = new HistoryInfoRequest(0);
            request.Id = Guid.NewGuid().ToString();
            request.SymbolId = symbol;
            request.HistoryType = level2 ? HistoryType.TicksLevel2 : HistoryType.Ticks;

            session_.SendHistoryInfoRequest(context, request);
        }

        public HistoryInfo GetVWAPQuotesHistoryInfo(string symbol, short degree, int timeout)
        {
            protocolSpec_.CheckSupportedVWAPTickList();

            VWAPHistoryInfoAsyncContext context = new VWAPHistoryInfoAsyncContext(true);

            VWAPHistoryInfoRequest request = new VWAPHistoryInfoRequest(0);
            request.Id = Guid.NewGuid().ToString();
            request.SymbolId = symbol;
            request.Degree = degree;

            session_.SendVWAPHistoryInfoRequest(context, request);

            if (!context.Wait(timeout))
                throw new Common.TimeoutException("Method call timed out");

            if (context.exception_ != null)
                throw context.exception_;

            return context.historyInfo_;
        }

        public void GetQuotesHistoryInfoAsync(object data, string symbol, short degree)
        {
            protocolSpec_.CheckSupportedVWAPTickList();

            VWAPHistoryInfoAsyncContext context = new VWAPHistoryInfoAsyncContext(true);
            context.Data = data;

            VWAPHistoryInfoRequest request = new VWAPHistoryInfoRequest(0);
            request.Id = Guid.NewGuid().ToString();
            request.SymbolId = symbol;
            request.Degree = degree;

            session_.SendVWAPHistoryInfoRequest(context, request);
        }

        #endregion

        #region Implementation

        SoftFX.Net.QuoteStore.PriceType GetPriceType(TickTrader.FDK.Common.PriceType priceType)
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

        SoftFX.Net.QuoteStore.TickDepth GetTickDepth(TickTrader.FDK.Common.QuoteDepth quoteDepth)
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
            void ProcessDisconnect(QuoteStore quoteStore, string text);
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

            public void ProcessDisconnect(QuoteStore quoteStore, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (quoteStore.LoginErrorEvent != null)
                {
                    try
                    {
                        quoteStore.LoginErrorEvent(quoteStore, Data, exception);
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

            public void ProcessDisconnect(QuoteStore quoteStore, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (quoteStore.LogoutErrorEvent != null)
                {
                    try
                    {
                        quoteStore.LogoutErrorEvent(quoteStore, Data, exception);
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

            public void ProcessDisconnect(QuoteStore quoteStore, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (quoteStore.SymbolListErrorEvent != null)
                {
                    try
                    {
                        quoteStore.SymbolListErrorEvent(quoteStore, Data, exception);
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

            public void ProcessDisconnect(QuoteStore quoteStore, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (quoteStore.PeriodicityListErrorEvent != null)
                {
                    try
                    {
                        quoteStore.PeriodicityListErrorEvent(quoteStore, Data, exception);
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

        class StockEventQHModifierListAsyncContext : StockEventQHModifierListRequestClientContext, IAsyncContext
        {
            public StockEventQHModifierListAsyncContext(bool waitable) : base(waitable)
            {
            }

            public void ProcessDisconnect(QuoteStore quoteStore, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (quoteStore.SymbolListErrorEvent != null)
                {
                    try
                    {
                        quoteStore.SymbolListErrorEvent(quoteStore, Data, exception);
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
            public SEQHModifier[] stockEvents_;
        }


        class BarListAsyncContext : BarListRequestClientContext, IAsyncContext
        {
            public BarListAsyncContext(bool waitable) : base(waitable)
            {
            }

            public void ProcessDisconnect(QuoteStore quoteStore, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (quoteStore.BarListErrorEvent != null)
                {
                    try
                    {
                        quoteStore.BarListErrorEvent(quoteStore, Data, exception);
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

            public void ProcessDisconnect(QuoteStore quoteStore, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (quoteStore.QuoteListErrorEvent != null)
                {
                    try
                    {
                        quoteStore.QuoteListErrorEvent(quoteStore, Data, exception);
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

        class VWAPQuoteListAsyncContext : VWAPTickListRequestClientContext, IAsyncContext
        {
            public VWAPQuoteListAsyncContext(bool waitable) : base(waitable)
            {
            }

            public void ProcessDisconnect(QuoteStore quoteStore, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (quoteStore.QuoteListErrorEvent != null)
                {
                    try
                    {
                        quoteStore.QuoteListErrorEvent(quoteStore, Data, exception);
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

        class HistoryModifyingDownloadContext : DownloadRequestClientContext
        {
            public SEHistoryRangeModifier modifier_;
            public HistoryModifyingDownloadContext(SEHistoryRangeModifier modifier, bool waitable) : base(waitable)
            {
                modifier_ = modifier;
            }
        }


        class BarDownloadAsyncContext : HistoryModifyingDownloadContext, IAsyncContext
        {
            public BarDownloadAsyncContext(SEHistoryRangeModifier modifier, bool waitable) : base(modifier, waitable)
            {
            }

            public void ProcessDisconnect(QuoteStore quoteStore, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (quoteStore.BarDownloadErrorEvent != null)
                {
                    try
                    {
                        quoteStore.BarDownloadErrorEvent(quoteStore, Data, exception);
                    }
                    catch
                    {
                    }
                }

                if (Waitable)
                {
                    enumerartor_.SetError(exception);
                }
            }

            public string downloadId_;
            public TickTrader.FDK.Common.PriceType priceType_;
            public BarPeriod calcBarPeriod_;
            public BarPeriod barPeriod_;
            public DateTime from_;
            public DateTime to_;
            public byte[] fileData_;
            public int fileSize_;
            public TickTrader.FDK.Common.Bar calcBar_;
            public TickTrader.FDK.Common.Bar bar_;
            public DownloadBarsEnumerator enumerartor_;
        }

        class BarQuoteDownloadAsyncContext : HistoryModifyingDownloadContext, IAsyncContext
        {
            public BarQuoteDownloadAsyncContext(SEHistoryRangeModifier modifier, bool waitable) : base(modifier, waitable)
            {
            }

            ~BarQuoteDownloadAsyncContext()
            {
                if (event_ != null)
                    event_.Close();
            }

            public void ProcessDisconnect(QuoteStore quoteStore, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (quoteStore.BarDownloadErrorEvent != null)
                {
                    try
                    {
                        quoteStore.BarDownloadErrorEvent(quoteStore, Data, exception);
                    }
                    catch
                    {
                    }
                }

                if (Waitable)
                {
                    if (enumerartor_ != null)
                    {
                        enumerartor_.SetError(exception);
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
            public BarPeriod calcBarPeriod_;
            public DateTime from_;
            public DateTime to_;
            public byte[] fileData_;
            public int fileSize_;
            public TickTrader.FDK.Common.Bar calcBar_;
            public Quote quote_;
            public AutoResetEvent event_;
            public Exception exception_;
            public DownloadBarsEnumerator enumerartor_;
        }

        class CancelDownloadBarsAsyncContext : DownloadCancelRequestClientContext, IAsyncContext
        {
            public CancelDownloadBarsAsyncContext(bool waitable) : base(waitable)
            {
            }

            public void ProcessDisconnect(QuoteStore quoteStore, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (quoteStore.CancelDownloadBarsErrorEvent != null)
                {
                    try
                    {
                        quoteStore.CancelDownloadBarsErrorEvent(quoteStore, Data, exception);
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

        class QuoteDownloadAsyncContext : HistoryModifyingDownloadContext, IAsyncContext
        {
            public QuoteDownloadAsyncContext(SEHistoryRangeModifier modifier, bool waitable) : base(modifier, waitable)
            {
            }

            public void ProcessDisconnect(QuoteStore quoteStore, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (quoteStore.QuoteDownloadErrorEvent != null)
                {
                    try
                    {
                        quoteStore.QuoteDownloadErrorEvent(quoteStore, Data, exception);
                    }
                    catch
                    {
                    }
                }

                if (Waitable)
                {
                    enumerartor_.SetError(exception);
                }
            }

            public string downloadId_;
            public QuoteDepth quoteDepth_;
            public DateTime from_;
            public DateTime to_;
            public byte[] fileData_;
            public int fileSize_;
            public Quote quote_;
            public DownloadQuotesEnumerator enumerartor_;
        }

        class VWAPQuoteDownloadAsyncContext : HistoryModifyingDownloadContext, IAsyncContext
        {
            public VWAPQuoteDownloadAsyncContext(SEHistoryRangeModifier modifier, bool waitable) : base(modifier, waitable)
            {
            }

            public void ProcessDisconnect(QuoteStore quoteStore, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (quoteStore.QuoteDownloadErrorEvent != null)
                {
                    try
                    {
                        quoteStore.QuoteDownloadErrorEvent(quoteStore, Data, exception);
                    }
                    catch
                    {
                    }
                }

                if (Waitable)
                {
                    enumerartor_.SetError(exception);
                }
            }

            public string downloadId_;
            public short degree_;
            public DateTime from_;
            public DateTime to_;
            public byte[] fileData_;
            public int fileSize_;
            public Quote quote_;
            public DownloadQuotesEnumerator enumerartor_;
        }

        class CancelDownloadQuotesAsyncContext : DownloadCancelRequestClientContext, IAsyncContext
        {
            public CancelDownloadQuotesAsyncContext(bool waitable) : base(waitable)
            {
            }

            public void ProcessDisconnect(QuoteStore quoteStore, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (quoteStore.CancelDownloadQuotesErrorEvent != null)
                {
                    try
                    {
                        quoteStore.CancelDownloadQuotesErrorEvent(quoteStore, Data, exception);
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

        class HistoryInfoAsyncContext : HistoryInfoRequestClientContext, IAsyncContext
        {
            public HistoryInfoAsyncContext(bool waitable) : base(waitable)
            {
            }

            public void ProcessDisconnect(QuoteStore quoteStore, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (quoteStore.HistoryInfoErrorEvent != null)
                {
                    try
                    {
                        quoteStore.HistoryInfoErrorEvent(quoteStore, Data, exception);
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
            public HistoryInfo historyInfo_;
        }

        class VWAPHistoryInfoAsyncContext : VWAPHistoryInfoRequestClientContext, IAsyncContext
        {
            public VWAPHistoryInfoAsyncContext(bool waitable) : base(waitable)
            {
            }

            public void ProcessDisconnect(QuoteStore quoteStore, string text)
            {
                DisconnectException exception = new DisconnectException(text);

                if (quoteStore.HistoryInfoErrorEvent != null)
                {
                    try
                    {
                        quoteStore.HistoryInfoErrorEvent(quoteStore, Data, exception);
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
            public HistoryInfo historyInfo_;
        }

        class SEModifiedBarsFromServerGetter : IModifiedBarSlowGetter
        {
            QuoteStore client_;

            public SEModifiedBarsFromServerGetter(QuoteStore client)
            {
                client_ = client;
            }

            public bool TryGetBar(DateTime time, string symbol, Periodicity periodicity, PriceType priceType, ref Common.Bar bar)
            {
                var res = client_.GetBarList(symbol, priceType, new BarPeriod(periodicity.ToString()), time, 1, 10000);
                if (res.Length > 0 && res[0].From == time)
                {
                    bar = res[0];
                    return true;
                }
                return false;
            }
        }

        class ClientSessionListener : SoftFX.Net.QuoteStore.ClientSessionListener
        {
            public ClientSessionListener(QuoteStore quoteStore)
            {
                client_ = quoteStore;
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
                    ConnectAsyncContext connectAsyncContext = (ConnectAsyncContext)connectContext;

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
                    DisconnectAsyncContext disconnectAsyncContext = (DisconnectAsyncContext)disconnectContext;

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

            public override void OnSymbolListReport(ClientSession session, SymbolListRequestClientContext SymbolListRequestClientContext, SymbolListReport message)
            {
                try
                {
                    SymbolListAsyncContext context = (SymbolListAsyncContext)SymbolListRequestClientContext;

                    try
                    {
                        SoftFX.Net.Core.StringArray reportSymbolIds = message.SymbolIds;
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
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnSymbolListReject(ClientSession session, SymbolListRequestClientContext SymbolListRequestClientContext, Reject message)
            {
                try
                {
                    var context = (SymbolListAsyncContext)SymbolListRequestClientContext;

                    Common.RejectReason rejectReason = GetRejectReason(message.Reason);
                    RejectException exception = new RejectException(rejectReason, message.Text);

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
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnPeriodicityListReport(ClientSession session, PeriodicityListRequestClientContext PeriodicityListRequestClientContext, PeriodicityListReport message)
            {
                try
                {
                    PeriodictityListAsyncContext context = (PeriodictityListAsyncContext)PeriodicityListRequestClientContext;

                    try
                    {
                        SoftFX.Net.Core.StringArray reportPeriodicities = message.Periodicities;
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
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnPeriodicityListReject(ClientSession session, PeriodicityListRequestClientContext PeriodicityListRequestClientContext, Reject message)
            {
                try
                {
                    PeriodictityListAsyncContext context = (PeriodictityListAsyncContext)PeriodicityListRequestClientContext;

                    Common.RejectReason rejectReason = GetRejectReason(message.Reason);
                    RejectException exception = new RejectException(rejectReason, message.Text);

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
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnStockEventQHModifierListReport(ClientSession session, StockEventQHModifierListRequestClientContext stockEventListRequestClientContext, StockEventQHModifierListReport message)
            {
                try
                {
                    StockEventQHModifierListAsyncContext context = (StockEventQHModifierListAsyncContext)stockEventListRequestClientContext;

                    try
                    {
                        StockEventQHModifierArray reportStockEventQHModifiers = message.StockEventQHModifierList;
                        int count = reportStockEventQHModifiers.Length;
                        SEQHModifier[] resultStockEventQHModifiers = new SEQHModifier[count];

                        for (int index = 0; index < count; ++index)
                        {
                            StockEventQHModifier reportStockEventQHModifier = reportStockEventQHModifiers[index];
                            SEQHModifier resultStockEventQHModifier = new SEQHModifier()
                            {
                                Id = reportStockEventQHModifier.Id,
                                StartTime = reportStockEventQHModifier.StartTime,
                                Ratio = reportStockEventQHModifier.Ratio
                            };

                            resultStockEventQHModifiers[index] = resultStockEventQHModifier;
                        }

                        if (client_.StockEventQHModifierListResultEvent != null)
                        {
                            try
                            {
                                client_.StockEventQHModifierListResultEvent(client_, context.Data, resultStockEventQHModifiers);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.stockEvents_ = resultStockEventQHModifiers;
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.StockEventQHModifierListErrorEvent != null)
                        {
                            try
                            {
                                client_.StockEventQHModifierListErrorEvent(client_, context.Data, exception);
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

            public override void OnStockEventQHModifierListReject(ClientSession session, StockEventQHModifierListRequestClientContext PeriodicityListRequestClientContext, Reject message)
            {
                try
                {
                    StockEventQHModifierListAsyncContext context = (StockEventQHModifierListAsyncContext)PeriodicityListRequestClientContext;

                    Common.RejectReason rejectReason = GetRejectReason(message.Reason);
                    RejectException exception = new RejectException(rejectReason, message.Text);

                    if (client_.StockEventQHModifierListErrorEvent != null)
                    {
                        try
                        {
                            client_.StockEventQHModifierListErrorEvent(client_, context.Data, exception);
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

            public override void OnBarListReport(ClientSession session, BarListRequestClientContext BarListRequestClientContext, BarListReport message)
            {
                try
                {
                    BarListAsyncContext context = (BarListAsyncContext)BarListRequestClientContext;

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
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnBarListReject(ClientSession session, BarListRequestClientContext BarListRequestClientContext, Reject message)
            {
                try
                {
                    BarListAsyncContext context = (BarListAsyncContext)BarListRequestClientContext;

                    Common.RejectReason rejectReason = GetRejectReason(message.Reason);
                    RejectException exception = new RejectException(rejectReason, message.Text);

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
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnTickListReport(ClientSession session, TickListRequestClientContext TickListRequestClientContext, TickListReport message)
            {
                try
                {
                    QuoteListAsyncContext context = (QuoteListAsyncContext)TickListRequestClientContext;

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

                            bool haveIndicativeBid = false;
                            bool haveIndicativeAsk = false;

                            for (int bidIndex = 0; bidIndex < bidCount; ++bidIndex)
                            {
                                SoftFX.Net.QuoteStore.PriceLevel reportBid = reportBids[bidIndex];

                                resultBid.Price = reportBid.Price;
                                resultBid.Volume = Math.Abs(reportBid.Size);

                                if (reportBid.Size <= 0)
                                    haveIndicativeBid = true;

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
                                resultAsk.Volume = Math.Abs(reportAsk.Size);

                                if (reportAsk.Size <= 0)
                                    haveIndicativeAsk = true;

                                resultAsks.Add(resultAsk);
                            }

                            resultQuote.Bids = resultBids;
                            resultQuote.Asks = resultAsks;

                            if (client_.protocolSpec_.SupportsQuoteStoreTickType)
                            {
                                resultQuote.TickType = GetTickType(reportTick.Type);
                                resultQuote.IndicativeTick = resultQuote.TickType > TickTypes.Normal;
                            }
                            else
                            {
                                if (!(resultQuote.IndicativeTick || haveIndicativeBid || haveIndicativeAsk))
                                    resultQuote.TickType = TickTypes.Normal;
                                else
                                {
                                    if (resultQuote.IndicativeTick || (haveIndicativeBid && haveIndicativeAsk))
                                        resultQuote.TickType = TickTypes.IndicativeBidAsk;

                                    if (haveIndicativeBid || haveIndicativeAsk)
                                        resultQuote.IndicativeTick = true;

                                    if (haveIndicativeBid && !haveIndicativeAsk)
                                        resultQuote.TickType = TickTypes.IndicativeBid;
                                    if (!haveIndicativeBid && haveIndicativeAsk)
                                        resultQuote.TickType = TickTypes.IndicativeAsk;
                                }
                            }

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
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnTickListReport(ClientSession session, VWAPTickListRequestClientContext VWAPTickListRequestClientContext, TickListReport message)
            {
                try
                {
                    VWAPQuoteListAsyncContext context = (VWAPQuoteListAsyncContext)VWAPTickListRequestClientContext;

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
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnTickListReject(ClientSession session, TickListRequestClientContext TickListRequestClientContext, Reject message)
            {
                try
                {
                    QuoteListAsyncContext context = (QuoteListAsyncContext)TickListRequestClientContext;

                    Common.RejectReason rejectReason = GetRejectReason(message.Reason);
                    RejectException exception = new RejectException(rejectReason, message.Text);

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
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnTickListReject(ClientSession session, VWAPTickListRequestClientContext VWAPTickListRequestClientContext, Reject message)
            {
                try
                {
                    VWAPQuoteListAsyncContext context = (VWAPQuoteListAsyncContext)VWAPTickListRequestClientContext;

                    Common.RejectReason rejectReason = GetRejectReason(message.Reason);
                    RejectException exception = new RejectException(rejectReason, message.Text);

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
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnDownloadBeginReport(ClientSession session, DownloadRequestClientContext DownloadRequestClientContext, DownloadBeginReport message)
            {
                try
                {
                    if (DownloadRequestClientContext is BarDownloadAsyncContext)
                    {
                        BarDownloadAsyncContext context = (BarDownloadAsyncContext)DownloadRequestClientContext;

                        try
                        {
                            ulong maxFileSize = 0;

                            FileInfoArray files = message.Files;
                            int count = files.Length;
                            for (int index = 0; index < count; ++index)
                            {
                                SoftFX.Net.QuoteStore.FileInfo file = files[index];

                                if (file.Size > maxFileSize)
                                    maxFileSize = file.Size;
                            }

                            context.fileData_ = new byte[maxFileSize];
                            context.fileSize_ = 0;
                            context.calcBar_ = new TickTrader.FDK.Common.Bar();
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
                                context.enumerartor_.SetBegin(requestId, availFrom, availTo);
                            }
                        }
                        catch (Exception exception)
                        {
                            if (client_.BarDownloadErrorEvent != null)
                            {
                                try
                                {
                                    client_.BarDownloadErrorEvent(client_, context.Data, exception);
                                }
                                catch
                                {
                                }
                            }

                            if (context.Waitable)
                            {
                                context.enumerartor_.SetError(exception);
                            }
                        }
                    }
                    else
                    if (DownloadRequestClientContext is VWAPQuoteDownloadAsyncContext)
                    {
                        VWAPQuoteDownloadAsyncContext context = (VWAPQuoteDownloadAsyncContext)DownloadRequestClientContext;

                        try
                        {
                            ulong maxFileSize = 0;

                            FileInfoArray files = message.Files;
                            int count = files.Length;
                            for (int index = 0; index < count; ++index)
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
                                context.enumerartor_.SetBegin(requestId, availFrom, availTo);
                            }
                        }
                        catch (Exception exception)
                        {
                            if (client_.QuoteDownloadErrorEvent != null)
                            {
                                try
                                {
                                    client_.QuoteDownloadErrorEvent(client_, context.Data, exception);
                                }
                                catch
                                {
                                }
                            }

                            if (context.Waitable)
                            {
                                context.enumerartor_.SetError(exception);
                            }
                        }
                    }
                    else if (DownloadRequestClientContext is BarQuoteDownloadAsyncContext)
                    {
                        BarQuoteDownloadAsyncContext context = (BarQuoteDownloadAsyncContext)DownloadRequestClientContext;

                        try
                        {
                            ulong maxFileSize = 0;

                            FileInfoArray files = message.Files;
                            int count = files.Length;
                            for (int index = 0; index < count; ++index)
                            {
                                SoftFX.Net.QuoteStore.FileInfo file = files[index];

                                if (file.Size > maxFileSize)
                                    maxFileSize = file.Size;
                            }

                            context.fileData_ = new byte[maxFileSize];
                            context.fileSize_ = 0;
                            context.calcBar_ = new TickTrader.FDK.Common.Bar();
                            context.quote_ = new Quote();

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
                                context.enumerartor_.SetBegin(requestId, availFrom, availTo);
                            }
                        }
                        catch (Exception exception)
                        {
                            if (client_.BarDownloadErrorEvent != null)
                            {
                                try
                                {
                                    client_.BarDownloadErrorEvent(client_, context.Data, exception);
                                }
                                catch
                                {
                                }
                            }

                            if (context.Waitable)
                            {
                                context.enumerartor_.SetError(exception);
                            }
                        }
                    }
                    else
                    {
                        QuoteDownloadAsyncContext context = (QuoteDownloadAsyncContext)DownloadRequestClientContext;

                        try
                        {
                            ulong maxFileSize = 0;

                            FileInfoArray files = message.Files;
                            int count = files.Length;
                            for (int index = 0; index < count; ++index)
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
                                context.enumerartor_.SetBegin(requestId, availFrom, availTo);
                            }
                        }
                        catch (Exception exception)
                        {
                            if (client_.QuoteDownloadErrorEvent != null)
                            {
                                try
                                {
                                    client_.QuoteDownloadErrorEvent(client_, context.Data, exception);
                                }
                                catch
                                {
                                }
                            }

                            if (context.Waitable)
                            {
                                context.enumerartor_.SetError(exception);
                            }
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnDownloadDataReport(ClientSession session, DownloadRequestClientContext DownloadRequestClientContext, DownloadDataReport message)
            {
                try
                {
                    if (DownloadRequestClientContext is BarDownloadAsyncContext)
                    {
                        BarDownloadAsyncContext context = (BarDownloadAsyncContext)DownloadRequestClientContext;

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
                                    client_.BarDownloadErrorEvent(client_, context.Data, exception);
                                }
                                catch
                                {
                                }
                            }

                            if (context.Waitable)
                            {
                                context.enumerartor_.SetError(exception);
                            }
                        }
                    }
                    else
                    if (DownloadRequestClientContext is VWAPQuoteDownloadAsyncContext)
                    {
                        VWAPQuoteDownloadAsyncContext context = (VWAPQuoteDownloadAsyncContext)DownloadRequestClientContext;

                        try
                        {
                            int chunkSize = message.GetChunkSize();
                            message.GetChunk(context.fileData_, context.fileSize_);
                            context.fileSize_ += chunkSize;

                            if (message.Last)
                            {
                                ProcessVWAPQuoteDownloadFile(context, message.RequestId);

                                context.fileSize_ = 0;
                            }
                        }
                        catch (Exception exception)
                        {
                            if (client_.QuoteDownloadErrorEvent != null)
                            {
                                try
                                {
                                    client_.QuoteDownloadErrorEvent(client_, context.Data, exception);
                                }
                                catch
                                {
                                }
                            }

                            if (context.Waitable)
                            {
                                context.enumerartor_.SetError(exception);
                            }
                        }
                    }
                    else if (DownloadRequestClientContext is BarQuoteDownloadAsyncContext)
                    {
                        BarQuoteDownloadAsyncContext context = (BarQuoteDownloadAsyncContext)DownloadRequestClientContext;

                        try
                        {
                            int chunkSize = message.GetChunkSize();
                            message.GetChunk(context.fileData_, context.fileSize_);
                            context.fileSize_ += chunkSize;

                            if (message.Last)
                            {
                                ProcessBarQuoteDownloadFile(context, message.RequestId);

                                context.fileSize_ = 0;
                            }
                        }
                        catch (Exception exception)
                        {
                            if (client_.BarDownloadErrorEvent != null)
                            {
                                try
                                {
                                    client_.BarDownloadErrorEvent(client_, context.Data, exception);
                                }
                                catch
                                {
                                }
                            }

                            if (context.Waitable)
                            {
                                context.enumerartor_.SetError(exception);
                            }
                        }
                    }
                    else
                    {
                        QuoteDownloadAsyncContext context = (QuoteDownloadAsyncContext)DownloadRequestClientContext;

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
                                    client_.QuoteDownloadErrorEvent(client_, context.Data, exception);
                                }
                                catch
                                {
                                }
                            }

                            if (context.Waitable)
                            {
                                context.enumerartor_.SetError(exception);
                            }
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
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
                                ProcessBarDownloadStream(context, downloadId, zipInputStream);
                            }
                        }
                    }
                }
                else
                {
                    using (MemoryStream memoryStream = new MemoryStream(context.fileData_, 0, context.fileSize_))
                    {
                        ProcessBarDownloadStream(context, downloadId, memoryStream);
                    }
                }
            }

            void ProcessBarDownloadStream(BarDownloadAsyncContext context, string downloadId, Stream stream)
            {
                Serialization.BarFormatter barFormatter = new Serialization.BarFormatter(stream);

                Periodicity periodicitty = Periodicity.Parse(context.calcBarPeriod_.ToString());

                long calcPeriodMilliseconds = context.calcBarPeriod_.ToMilliseconds();
                DateTime calcFrom = context.from_;
                while (!barFormatter.IsEnd)
                {
                    barFormatter.Deserialize(context.barPeriod_, context.bar_);

                    if (context.bar_.From < context.from_)
                        continue;

                    if (context.bar_.From >= context.to_)
                        break;

                    if (context.calcBarPeriod_.Prefix == BarPeriodPrefix.MN)
                    {
                        calcFrom = context.from_.AddMonths(context.bar_.From.Month - context.from_.Month + 12 * (context.bar_.From.Year - context.from_.Year));
                    }
                    else
                    {
                        long milliseconds = (long)((context.bar_.From - context.from_).TotalMilliseconds);
                        milliseconds = milliseconds / calcPeriodMilliseconds * calcPeriodMilliseconds;
                        calcFrom = context.from_.AddMilliseconds(milliseconds);
                    }

                    if (context.calcBar_.From == new DateTime())
                    {
                        context.calcBar_.From = calcFrom;
                        context.calcBar_.To = calcFrom + context.calcBarPeriod_;
                        context.calcBar_.Open = context.bar_.Open;
                        context.calcBar_.Close = context.bar_.Close;
                        context.calcBar_.Low = context.bar_.Low;
                        context.calcBar_.High = context.bar_.High;
                        context.calcBar_.Volume = context.bar_.Volume;
                    }
                    else if (calcFrom == context.calcBar_.From)
                    {
                        context.calcBar_.Close = context.bar_.Close;

                        if (context.bar_.Low < context.calcBar_.Low)
                            context.calcBar_.Low = context.bar_.Low;

                        if (context.bar_.High > context.calcBar_.High)
                            context.calcBar_.High = context.bar_.High;

                        context.calcBar_.Volume += context.bar_.Volume;
                    }
                    else
                    {
                        if (client_.BarDownloadResultEvent != null)
                        {
                            try
                            {
                                context.modifier_.ModifyBar(ref context.calcBar_);
                                client_.BarDownloadResultEvent(client_, context.Data, context.calcBar_);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            TickTrader.FDK.Common.Bar bar = context.calcBar_.Clone();
                            context.modifier_.ModifyBar(ref bar);
                            context.enumerartor_.SetResult(bar);
                        }

                        context.calcBar_.From = calcFrom;
                        context.calcBar_.To = calcFrom + context.calcBarPeriod_;
                        context.calcBar_.Open = context.bar_.Open;
                        context.calcBar_.Close = context.bar_.Close;
                        context.calcBar_.Low = context.bar_.Low;
                        context.calcBar_.High = context.bar_.High;
                        context.calcBar_.Volume = context.bar_.Volume;
                    }
                }

                if (context.calcBar_.From != new DateTime() && !(context.calcBarPeriod_.Prefix == BarPeriodPrefix.W && context.calcBar_.To.Month != context.bar_.From.Month))
                {
                    if (client_.BarDownloadResultEvent != null)
                    {
                        try
                        {
                            context.modifier_.ModifyBar(ref context.calcBar_);
                            client_.BarDownloadResultEvent(client_, context.Data, context.calcBar_);
                        }
                        catch
                        {
                        }
                    }

                    if (context.Waitable)
                    {
                        TickTrader.FDK.Common.Bar bar = context.calcBar_.Clone();
                        context.modifier_.ModifyBar(ref bar);

                        context.enumerartor_.SetResult(bar);
                    }

                    context.calcBar_.From = new DateTime();
                }
            }

            void ProcessBarQuoteDownloadFile(BarQuoteDownloadAsyncContext context, string downloadId)
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
                            ZipEntry zipEntry = zipFile.GetEntry("ticks.txt");

                            if (zipEntry == null)
                                throw new Exception(string.Format("Could not find file ticks.txt inside zip archive"));

                            using (Stream zipInputStream = zipFile.GetInputStream(zipEntry))
                            {
                                ProcessBarQuoteDownloadStream(context, downloadId, zipInputStream);
                            }
                        }
                    }
                }
                else
                {
                    using (MemoryStream memoryStream = new MemoryStream(context.fileData_, 0, context.fileSize_))
                    {
                        ProcessBarQuoteDownloadStream(context, downloadId, memoryStream);
                    }
                }
            }

            void ProcessBarQuoteDownloadStream(BarQuoteDownloadAsyncContext context, string downloadId, Stream stream)
            {
                Periodicity periodicitty = Periodicity.Parse(context.calcBarPeriod_.ToString());
                Serialization.TickFormatter tickFormatter = new Serialization.TickFormatter(QuoteDepth.Top, stream);

                long calcPeriodMilliseconds = context.calcBarPeriod_.ToMilliseconds();

                while (!tickFormatter.IsEnd)
                {
                    tickFormatter.Deserialize(context.quote_);

                    if (context.quote_.CreatingTime < context.from_)
                        continue;

                    if (context.quote_.CreatingTime >= context.to_)
                        break;

                    long milliseconds = (long)((context.quote_.CreatingTime - context.from_).TotalMilliseconds);
                    milliseconds = milliseconds / calcPeriodMilliseconds * calcPeriodMilliseconds;
                    DateTime calcFrom = context.from_.AddMilliseconds(milliseconds);
                    List<QuoteEntry> quoteEntryList = context.priceType_ == Common.PriceType.Ask ? context.quote_.Asks : context.quote_.Bids;

                    if (context.calcBar_.From == new DateTime())
                    {
                        if (quoteEntryList.Count > 0)
                        {
                            QuoteEntry quoteEntry = quoteEntryList[0];

                            context.calcBar_.From = calcFrom;
                            context.calcBar_.To = calcFrom + context.calcBarPeriod_;
                            context.calcBar_.Open = quoteEntry.Price;
                            context.calcBar_.Close = quoteEntry.Price;
                            context.calcBar_.Low = quoteEntry.Price;
                            context.calcBar_.High = quoteEntry.Price;
                            context.calcBar_.Volume = Math.Abs(quoteEntry.Volume);
                        }
                    }
                    else if (calcFrom == context.calcBar_.From)
                    {
                        if (quoteEntryList.Count > 0)
                        {
                            QuoteEntry quoteEntry = quoteEntryList[0];

                            context.calcBar_.Close = quoteEntry.Price;

                            if (quoteEntry.Price < context.calcBar_.Low)
                                context.calcBar_.Low = quoteEntry.Price;

                            if (quoteEntry.Price > context.calcBar_.High)
                                context.calcBar_.High = quoteEntry.Price;

                            context.calcBar_.Volume += Math.Abs(quoteEntry.Volume);
                        }
                    }
                    else
                    {
                        if (quoteEntryList.Count > 0)
                        {
                            if (client_.BarDownloadResultEvent != null)
                            {
                                try
                                {
                                    context.modifier_.ModifyBar(ref context.calcBar_);
                                    client_.BarDownloadResultEvent(client_, context.Data, context.calcBar_);
                                }
                                catch
                                {
                                }
                            }

                            if (context.Waitable)
                            {
                                TickTrader.FDK.Common.Bar bar = context.calcBar_.Clone();
                                context.modifier_.ModifyBar(ref bar);
                                context.enumerartor_.SetResult(bar);
                            }

                            QuoteEntry quoteEntry = quoteEntryList[0];

                            context.calcBar_.From = calcFrom;
                            context.calcBar_.To = calcFrom + context.calcBarPeriod_;
                            context.calcBar_.Open = quoteEntry.Price;
                            context.calcBar_.Close = quoteEntry.Price;
                            context.calcBar_.Low = quoteEntry.Price;
                            context.calcBar_.High = quoteEntry.Price;
                            context.calcBar_.Volume = Math.Abs(quoteEntry.Volume);
                        }
                    }
                }

                if (context.calcBar_.From != new DateTime())
                {
                    if (client_.BarDownloadResultEvent != null)
                    {
                        try
                        {
                            context.modifier_.ModifyBar(ref context.calcBar_);
                            client_.BarDownloadResultEvent(client_, context.Data, context.calcBar_);
                        }
                        catch
                        {
                        }
                    }

                    if (context.Waitable)
                    {
                        TickTrader.FDK.Common.Bar bar = context.calcBar_.Clone();
                        context.modifier_.ModifyBar(ref bar);
                        context.enumerartor_.SetResult(bar);
                    }

                    context.calcBar_.From = new DateTime();
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
                                ProcessQuoteDownloadStream(context, downloadId, zipInputStream);
                            }
                        }
                    }
                }
                else
                {
                    using (MemoryStream memoryStream = new MemoryStream(context.fileData_, 0, context.fileSize_))
                    {
                        ProcessQuoteDownloadStream(context, downloadId, memoryStream);
                    }
                }
            }

            void ProcessQuoteDownloadStream(QuoteDownloadAsyncContext context, string downloadId, Stream stream)
            {
                Serialization.TickFormatter tickFormatter = new Serialization.TickFormatter(context.quoteDepth_, stream);

                while (!tickFormatter.IsEnd)
                {
                    tickFormatter.Deserialize(context.quote_);

                    if (context.quote_.CreatingTime < context.from_)
                        continue;

                    if (context.quote_.CreatingTime >= context.to_)
                        break;

                    if (client_.QuoteDownloadResultEvent != null)
                    {
                        try
                        {
                            context.modifier_.ModifyTick(ref context.quote_);
                            client_.QuoteDownloadResultEvent(client_, context.Data, context.quote_);
                        }
                        catch
                        {
                        }
                    }

                    if (context.Waitable)
                    {
                        Quote quote = context.quote_.Clone();
                        context.modifier_.ModifyTick(ref quote);
                        context.enumerartor_.SetResult(quote);
                    }
                }
            }

            void ProcessVWAPQuoteDownloadFile(VWAPQuoteDownloadAsyncContext context, string downloadId)
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
                            string fileName = ("ticks vwap 1e" + context.degree_.ToString("+00;-00")) + ".txt";
                            ZipEntry zipEntry = zipFile.GetEntry(fileName);

                            if (zipEntry == null)
                                throw new Exception(string.Format("Could not find file {0} inside zip archive", fileName));

                            using (Stream zipInputStream = zipFile.GetInputStream(zipEntry))
                            {
                                ProcessVWAPQuoteDownloadStream(context, downloadId, zipInputStream);
                            }
                        }
                    }
                }
                else
                {
                    using (MemoryStream memoryStream = new MemoryStream(context.fileData_, 0, context.fileSize_))
                    {
                        ProcessVWAPQuoteDownloadStream(context, downloadId, memoryStream);
                    }
                }
            }

            void ProcessVWAPQuoteDownloadStream(VWAPQuoteDownloadAsyncContext context, string downloadId, Stream stream)
            {
                Serialization.TickFormatter tickFormatter = new Serialization.TickFormatter(QuoteDepth.Top, stream);

                while (!tickFormatter.IsEnd)
                {
                    tickFormatter.Deserialize(context.quote_);

                    if (context.quote_.CreatingTime < context.from_)
                        continue;

                    if (context.quote_.CreatingTime >= context.to_)
                        break;

                    if (client_.QuoteDownloadResultEvent != null)
                    {
                        try
                        {
                            client_.QuoteDownloadResultEvent(client_, context.Data, context.quote_);
                        }
                        catch
                        {
                        }
                    }

                    if (context.Waitable)
                    {
                        Quote quote = context.quote_.Clone();

                        context.enumerartor_.SetResult(quote);
                    }
                }
            }


            public override void OnDownloadEndReport(ClientSession session, DownloadRequestClientContext DownloadRequestClientContext, DownloadEndReport message)
            {
                try
                {
                    if (DownloadRequestClientContext is BarDownloadAsyncContext)
                    {
                        BarDownloadAsyncContext context = (BarDownloadAsyncContext)DownloadRequestClientContext;

                        try
                        {
                            if (client_.BarDownloadResultEndEvent != null)
                            {
                                try
                                {
                                    client_.BarDownloadResultEndEvent(client_, context.Data);
                                }
                                catch
                                {
                                }
                            }

                            if (context.Waitable)
                            {
                                context.enumerartor_.SetEnd();
                            }
                        }
                        catch (Exception exception)
                        {
                            if (client_.BarDownloadErrorEvent != null)
                            {
                                try
                                {
                                    client_.BarDownloadErrorEvent(client_, context.Data, exception);
                                }
                                catch
                                {
                                }
                            }

                            if (context.Waitable)
                            {
                                context.enumerartor_.SetError(exception);
                            }
                        }
                    }
                    else if (DownloadRequestClientContext is VWAPQuoteDownloadAsyncContext)
                    {
                        VWAPQuoteDownloadAsyncContext context = (VWAPQuoteDownloadAsyncContext)DownloadRequestClientContext;

                        try
                        {
                            if (client_.QuoteDownloadResultEndEvent != null)
                            {
                                try
                                {
                                    client_.QuoteDownloadResultEndEvent(client_, context.Data);
                                }
                                catch
                                {
                                }
                            }

                            if (context.Waitable)
                            {
                                context.enumerartor_.SetEnd();
                            }
                        }
                        catch (Exception exception)
                        {
                            if (client_.QuoteDownloadErrorEvent != null)
                            {
                                try
                                {
                                    client_.QuoteDownloadErrorEvent(client_, context.Data, exception);
                                }
                                catch
                                {
                                }
                            }

                            if (context.Waitable)
                            {
                                context.enumerartor_.SetError(exception);
                            }
                        }
                    }
                    else if (DownloadRequestClientContext is BarQuoteDownloadAsyncContext)
                    {
                        BarQuoteDownloadAsyncContext context = (BarQuoteDownloadAsyncContext)DownloadRequestClientContext;

                        try
                        {
                            if (client_.BarDownloadResultEndEvent != null)
                            {
                                try
                                {
                                    client_.BarDownloadResultEndEvent(client_, context.Data);
                                }
                                catch
                                {
                                }
                            }

                            if (context.Waitable)
                            {
                                context.enumerartor_.SetEnd();
                            }
                        }
                        catch (Exception exception)
                        {
                            if (client_.BarDownloadErrorEvent != null)
                            {
                                try
                                {
                                    client_.BarDownloadErrorEvent(client_, context.Data, exception);
                                }
                                catch
                                {
                                }
                            }

                            if (context.Waitable)
                            {
                                context.enumerartor_.SetError(exception);
                            }
                        }
                    }
                    else
                    {
                        QuoteDownloadAsyncContext context = (QuoteDownloadAsyncContext)DownloadRequestClientContext;

                        try
                        {
                            if (client_.QuoteDownloadResultEndEvent != null)
                            {
                                try
                                {
                                    client_.QuoteDownloadResultEndEvent(client_, context.Data);
                                }
                                catch
                                {
                                }
                            }

                            if (context.Waitable)
                            {
                                context.enumerartor_.SetEnd();
                            }
                        }
                        catch (Exception exception)
                        {
                            if (client_.QuoteDownloadErrorEvent != null)
                            {
                                try
                                {
                                    client_.QuoteDownloadErrorEvent(client_, context.Data, exception);
                                }
                                catch
                                {
                                }
                            }

                            if (context.Waitable)
                            {
                                context.enumerartor_.SetError(exception);
                            }
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnDownloadReject(ClientSession session, DownloadRequestClientContext DownloadRequestClientContext, Reject message)
            {
                try
                {
                    if (DownloadRequestClientContext is BarDownloadAsyncContext)
                    {
                        BarDownloadAsyncContext context = (BarDownloadAsyncContext)DownloadRequestClientContext;

                        Common.RejectReason rejectReason = GetRejectReason(message.Reason);
                        RejectException exception = new RejectException(rejectReason, message.Text);

                        if (client_.BarDownloadErrorEvent != null)
                        {
                            try
                            {
                                client_.BarDownloadErrorEvent(client_, context.Data, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.enumerartor_.SetError(exception);
                        }
                    }
                    else if (DownloadRequestClientContext is BarQuoteDownloadAsyncContext)
                    {
                        BarQuoteDownloadAsyncContext context = (BarQuoteDownloadAsyncContext)DownloadRequestClientContext;

                        Common.RejectReason rejectReason = GetRejectReason(message.Reason);
                        RejectException exception = new RejectException(rejectReason, message.Text);

                        if (client_.BarDownloadErrorEvent != null)
                        {
                            try
                            {
                                client_.BarDownloadErrorEvent(client_, context.Data, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.enumerartor_.SetError(exception);
                        }
                    }
                    else
                    {
                        QuoteDownloadAsyncContext context = (QuoteDownloadAsyncContext)DownloadRequestClientContext;

                        Common.RejectReason rejectReason = GetRejectReason(message.Reason);
                        RejectException exception = new RejectException(rejectReason, message.Text);

                        if (client_.QuoteDownloadErrorEvent != null)
                        {
                            try
                            {
                                client_.QuoteDownloadErrorEvent(client_, context.Data, exception);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.enumerartor_.SetError(exception);
                        }
                    }
                }
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }


            public override void OnDownloadCancelReport(ClientSession session, DownloadCancelRequestClientContext DownloadCancelRequestClientContext, DownloadCancelReport message)
            {
                try
                {
                    if (DownloadCancelRequestClientContext is CancelDownloadBarsAsyncContext)
                    {
                        CancelDownloadBarsAsyncContext context = (CancelDownloadBarsAsyncContext)DownloadCancelRequestClientContext;

                        try
                        {
                            if (client_.CancelDownloadBarsResultEvent != null)
                            {
                                try
                                {
                                    client_.CancelDownloadBarsResultEvent(client_, context.Data);
                                }
                                catch
                                {
                                }
                            }
                        }
                        catch (Exception exception)
                        {
                            if (client_.CancelDownloadBarsErrorEvent != null)
                            {
                                try
                                {
                                    client_.CancelDownloadBarsErrorEvent(client_, context.Data, exception);
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
                        CancelDownloadQuotesAsyncContext context = (CancelDownloadQuotesAsyncContext)DownloadCancelRequestClientContext;

                        try
                        {
                            if (client_.CancelDownloadQuotesResultEvent != null)
                            {
                                try
                                {
                                    client_.CancelDownloadQuotesResultEvent(client_, context.Data);
                                }
                                catch
                                {
                                }
                            }
                        }
                        catch (Exception exception)
                        {
                            if (client_.CancelDownloadQuotesErrorEvent != null)
                            {
                                try
                                {
                                    client_.CancelDownloadQuotesErrorEvent(client_, context.Data, exception);
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
                catch
                {
                    // client_.session_.LogError(exception.Message);
                }
            }

            public override void OnDownloadCancelReject(ClientSession session, DownloadCancelRequestClientContext DownloadCancelRequestClientContext, Reject message)
            {
                try
                {
                    if (DownloadCancelRequestClientContext is CancelDownloadBarsAsyncContext)
                    {
                        CancelDownloadBarsAsyncContext context = (CancelDownloadBarsAsyncContext)DownloadCancelRequestClientContext;

                        Common.RejectReason rejectReason = GetRejectReason(message.Reason);
                        RejectException exception = new RejectException(rejectReason, message.Text);

                        if (client_.CancelDownloadBarsErrorEvent != null)
                        {
                            try
                            {
                                client_.CancelDownloadBarsErrorEvent(client_, context.Data, exception);
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
                        CancelDownloadQuotesAsyncContext context = (CancelDownloadQuotesAsyncContext)DownloadCancelRequestClientContext;

                        Common.RejectReason rejectReason = GetRejectReason(message.Reason);
                        RejectException exception = new RejectException(rejectReason, message.Text);

                        if (client_.CancelDownloadQuotesErrorEvent != null)
                        {
                            try
                            {
                                client_.CancelDownloadQuotesErrorEvent(client_, context.Data, exception);
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

            public override void OnNotification(ClientSession session, SoftFX.Net.QuoteStore.Notification message)
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

            public override void OnHistoryInfoReport(ClientSession session, HistoryInfoRequestClientContext HistoryInfoRequestClientContext, HistoryInfoReport message)
            {
                HistoryInfoAsyncContext context = (HistoryInfoAsyncContext)HistoryInfoRequestClientContext;

                try
                {
                    try
                    {
                        HistoryInfo historyInfo = new HistoryInfo();
                        historyInfo.Symbol = message.SymbolId;
                        historyInfo.AvailFrom = message.AvailFrom;
                        historyInfo.AvailTo = message.AvailTo;
                        historyInfo.LastTickId = message.LastTickId;

                        if (client_.HistoryInfoResultEvent != null)
                        {
                            try
                            {
                                client_.HistoryInfoResultEvent(client_, context.Data, historyInfo);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.historyInfo_ = historyInfo;
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.HistoryInfoErrorEvent != null)
                        {
                            try
                            {
                                client_.HistoryInfoErrorEvent(client_, context.Data, exception);
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

            public override void OnHistoryInfoReport(ClientSession session, VWAPHistoryInfoRequestClientContext HistoryInfoRequestClientContext, HistoryInfoReport message)
            {
                VWAPHistoryInfoAsyncContext context = (VWAPHistoryInfoAsyncContext)HistoryInfoRequestClientContext;

                try
                {
                    try
                    {
                        HistoryInfo historyInfo = new HistoryInfo();
                        historyInfo.Symbol = message.SymbolId;
                        historyInfo.AvailFrom = message.AvailFrom;
                        historyInfo.AvailTo = message.AvailTo;
                        historyInfo.LastTickId = message.LastTickId;

                        if (client_.HistoryInfoResultEvent != null)
                        {
                            try
                            {
                                client_.HistoryInfoResultEvent(client_, context.Data, historyInfo);
                            }
                            catch
                            {
                            }
                        }

                        if (context.Waitable)
                        {
                            context.historyInfo_ = historyInfo;
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.HistoryInfoErrorEvent != null)
                        {
                            try
                            {
                                client_.HistoryInfoErrorEvent(client_, context.Data, exception);
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

            public override void OnHistoryInfoReject(ClientSession session, HistoryInfoRequestClientContext HistoryInfoRequestClientContext, Reject message)
            {
                try
                {
                    HistoryInfoAsyncContext context = (HistoryInfoAsyncContext)HistoryInfoRequestClientContext;

                    Common.RejectReason rejectReason = GetRejectReason(message.Reason);
                    RejectException exception = new RejectException(rejectReason, message.Text);

                    if (client_.HistoryInfoErrorEvent != null)
                    {
                        try
                        {
                            client_.HistoryInfoErrorEvent(client_, context.Data, exception);
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

            public override void OnHistoryInfoReject(ClientSession session, VWAPHistoryInfoRequestClientContext VWAPHistoryInfoRequestClientContext, Reject message)
            {
                try
                {
                    VWAPHistoryInfoAsyncContext context = (VWAPHistoryInfoAsyncContext)VWAPHistoryInfoRequestClientContext;

                    Common.RejectReason rejectReason = GetRejectReason(message.Reason);
                    RejectException exception = new RejectException(rejectReason, message.Text);

                    if (client_.HistoryInfoErrorEvent != null)
                    {
                        try
                        {
                            client_.HistoryInfoErrorEvent(client_, context.Data, exception);
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

            TickTrader.FDK.Common.LogoutReason GetLogoutReason(SoftFX.Net.QuoteStore.LoginRejectReason reason)
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

                    case SoftFX.Net.QuoteStore.LoginRejectReason.MustChangePassword:
                        return TickTrader.FDK.Common.LogoutReason.MustChangePassword;

                    case SoftFX.Net.QuoteStore.LoginRejectReason.Other:
                    default:
                        return TickTrader.FDK.Common.LogoutReason.Unknown;
                }
            }

            TickTrader.FDK.Common.LogoutReason GetLogoutReason(SoftFX.Net.QuoteStore.LogoutReason reason)
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

                    case SoftFX.Net.QuoteStore.LogoutReason.MustChangePassword:
                        return TickTrader.FDK.Common.LogoutReason.MustChangePassword;

                    case SoftFX.Net.QuoteStore.LogoutReason.Other:
                    default:
                        return TickTrader.FDK.Common.LogoutReason.Unknown;
                }
            }

            TickTrader.FDK.Common.RejectReason GetRejectReason(SoftFX.Net.QuoteStore.RejectReason reason)
            {
                switch (reason)
                {
                    case SoftFX.Net.QuoteStore.RejectReason.ThrottlingLimits:
                        return Common.RejectReason.ThrottlingLimits;

                    case SoftFX.Net.QuoteStore.RejectReason.RequestCancelled:
                        return Common.RejectReason.RequestCancelled;

                    case SoftFX.Net.QuoteStore.RejectReason.InternalServerError:
                        return Common.RejectReason.InternalServerError;

                    case SoftFX.Net.QuoteStore.RejectReason.Other:
                    default:
                        return Common.RejectReason.Other;
                }
            }

            TickTrader.FDK.Common.NotificationType GetNotificationType(SoftFX.Net.QuoteStore.NotificationType type)
            {
                switch (type)
                {
                    case SoftFX.Net.QuoteStore.NotificationType.ConfigUpdate:
                        return TickTrader.FDK.Common.NotificationType.ConfigUpdated;

                    default:
                        return TickTrader.FDK.Common.NotificationType.Unknown;
                }
            }

            TickTrader.FDK.Common.NotificationSeverity GetNotificationSeverity(SoftFX.Net.QuoteStore.NotificationSeverity severity)
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
                        return TickTrader.FDK.Common.NotificationSeverity.Unknown;
                }
            }

            TickTrader.FDK.Common.TickTypes GetTickType(SoftFX.Net.QuoteStore.TickType type)
            {
                switch (type)
                {
                    case TickType.IndicativeBid:
                        return TickTypes.IndicativeBid;
                    case TickType.IndicativeAsk:
                        return TickTypes.IndicativeAsk;
                    case TickType.IndicativeBidAsk:
                        return TickTypes.IndicativeBidAsk;

                    case TickType.Normal:
                    default:
                        return TickTypes.Normal;
                }
            }


            QuoteStore client_;
        }

        private void GetBarPeriodicity(ref DateTime from, ref DateTime to, BarPeriod barPeriod)
        {
            int result = DateTime.Compare(from, to);
            if (result > 0)
            {
                DateTime temp = from;
                from = to;
                to = temp;
            }

            Periodicity periodicity = new Periodicity();
            Periodicity.TryParse(barPeriod.ToString(), out periodicity);
            from = periodicity.GetPeriodStartTime(from);
            if (to != periodicity.GetPeriodStartTime(to))
                to = periodicity.GetPeriodStartTime(periodicity.Shift(to, 1));
        }

        #endregion
    }
}
