using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using SoftFX.Net.QuoteStore;
using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.Zip;
using TickTrader.FDK.Common;

namespace TickTrader.FDK.QuoteStore
{
    public class Client : IDisposable
    {
        #region Constructors

        public Client(string name) : this(name, 5050, true, "Logs", false)
        {
        }

        public Client(string name, int port, bool reconnect, string logDirectory, bool logMessages)
        {
            ClientSessionOptions options = new ClientSessionOptions(port);
            options.ConnectionType = SoftFX.Net.Core.ConnectionType.Socket;
            options.ReconnectMaxCount = reconnect ? -1 : 0;
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
            _session = new ClientSession(name, options);
            _sessionListener = new ClientSessionListener(this);
            _session.Listener = _sessionListener;
        }

        ClientSession _session;
        ClientSessionListener _sessionListener;

        #endregion

        #region IDisposable        

        public void Dispose()
        {
            Disconnect("Client disconnect");

            GC.SuppressFinalize(this);
        }

        #endregion

        #region Connect / disconnect

        public delegate void ConnectDelegate(Client client);
        public delegate void ConnectErrorDelegate(Client client, string text);
        public delegate void DisconnectDelegate(Client client, string text);        

        public event ConnectDelegate ConnectEvent;
        public event ConnectErrorDelegate ConnectErrorEvent;
        public event DisconnectDelegate DisconnectEvent;

        public void Connect(string address, int timeout)
        {
            _session.Connect(address);

            if (!_session.WaitConnect(timeout))
            {
                _session.Disconnect("Connect timeout");
                _session.Join();

                throw new TimeoutException("Connect timeout");
            }
        }

        // TODO: return Task ?
        public void ConnectAsync(string address)
        {
            _session.Connect(address);
        }

        public void Disconnect(string text)
        {
            _session.Disconnect(text);
            _session.Join();
        }

        // TODO: return Task ?
        public void DisconnectAsync(string text)
        {
            _session.Disconnect(text);
        }

        public void Join()
        {
            _session.Join();
        }

        #endregion

        #region Login / logout

        public delegate void LoginResultDelegate(Client client, object data);
        public delegate void LoginErrorDelegate(Client client, object data, string message);
        public delegate void LogoutResultDelegate(Client client, object data, LogoutInfo logoutInfo);
        public delegate void LogoutDelegate(Client client, LogoutInfo logoutInfo);
        
        public event LoginResultDelegate LoginResultEvent;
        public event LoginErrorDelegate LoginErrorEvent;
        public event LogoutResultDelegate LogoutResultEvent;
        public event LogoutDelegate LogoutEvent;

        public void Login(string username, string password, string deviceId, string appSessionId, int timeout)
        {
            ConvertToSync(LoginAsync(null, username, password, deviceId, appSessionId), timeout);
        }

        public Task LoginAsync(object data, string username, string password, string deviceId, string appSessionId)
        {
            Task result;

            // Create a new async context
            var context = new LoginAsyncContext();
            context.Data = data;

            if (data == null)
            {
                context.taskCompletionSource_ = new TaskCompletionSource<object>();
                result = context.taskCompletionSource_.Task;
            }
            else
                result = null;

            // Create a request
            var request = new LoginRequest(0)
            {
                Username = username,
                Password = password,
                DeviceId = deviceId,
                AppSessionId = appSessionId
            };

            // Send request to the server
            _session.SendLoginRequest(context, request);

            // Return result task
            return result;
        }

        public LogoutInfo Logout(string message, int timeout)
        {
            return ConvertToSync(LogoutAsync(null, message), timeout);
        }

        public Task<LogoutInfo> LogoutAsync(object data, string message)
        {
            Task<LogoutInfo> result;

            // Create a new async context
            var context = new LogoutAsyncContext();
            context.Data = data;

            if (data == null)
            {
                context.taskCompletionSource_ = new TaskCompletionSource<LogoutInfo>();
                result = context.taskCompletionSource_.Task;
            }
            else
                result = null;

            // Create a request
            var request = new Logout(0)
            {
                Text = message
            };

            // Send request to the server
            _session.SendLogout(context, request);

            // Return result task
            return result;
        }

        #endregion

        #region Quote Store

        public delegate void SymbolListResultDelegate(Client client, object data, string[] symbols);
        public delegate void SymbolListErrorDelegate(Client client, object data, string message);
        public delegate void PeriodicityListResultDelegate(Client client, object data, string[] periodicities);
        public delegate void PeriodicityListErrorDelegate(Client client, object data, string message);
        public delegate void BarDownloadResultBeginDelegate(Client client, object data, string downloadId);
        public delegate void BarDownloadResultDelegate(Client client, object data, string downloadId, Bar bar);
        public delegate void BarDownloadResultEndDelegate(Client client, object data, string downloadId);
        public delegate void BarDownloadErrorDelegate(Client client, object data, string downloadId, string message);
        public delegate void QuoteDownloadResultBeginDelegate(Client client, object data, string downloadId);
        public delegate void QuoteDownloadResultDelegate(Client client, object data, string downloadId, Quote quote);
        public delegate void QuoteDownloadResultEndDelegate(Client client, object data, string downloadId);
        public delegate void QuoteDownloadErrorDelegate(Client client, object data, string downloadId, string message);
        public delegate void NotificationDelegate(Client client, Common.Notification notification);

        public event SymbolListResultDelegate SymbolListResultEvent;
        public event SymbolListErrorDelegate SymbolListErrorEvent;
        public event PeriodicityListResultDelegate PeriodicityListResultEvent;
        public event PeriodicityListErrorDelegate PeriodicityListErrorEvent;
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
            return ConvertToSync(GetSymbolListAsync(null), timeout);
        }

        public Task<string[]> GetSymbolListAsync(object data)
        {
            Task<string[]> result;

            // Create a new async context
            SymbolListAsyncContext context = new SymbolListAsyncContext();
            context.Data = data;

            if (data == null)
            {
                context.taskCompletionSource_ = new TaskCompletionSource<string[]>();
                result = context.taskCompletionSource_.Task;
            }
            else
                result = null;

            // Create a request
            SymbolListRequest request = new SymbolListRequest(0);
            request.Id = Guid.NewGuid().ToString();

            // Send request to the server
            _session.SendSymbolListRequest(context, request);

            // Return result task
            return result;
        }

        public string[] GetPeriodicityList(string symbol, int timeout)
        {
            return ConvertToSync(GetPeriodicityListAsync(null, symbol), timeout);
        }

        public Task<string[]> GetPeriodicityListAsync(object data, string symbol)
        {
            Task<string[]> result;

            // Create a new async context
            PeriodictityListAsyncContext context = new PeriodictityListAsyncContext();
            context.Data = data;

            if (data == null)
            {
                context.taskCompletionSource_ = new TaskCompletionSource<string[]>();
                result = context.taskCompletionSource_.Task;
            }
            else
                result = null;

            // Create a request
            PeriodicityListRequest request = new PeriodicityListRequest(0);
            request.Id = Guid.NewGuid().ToString();
            request.SymbolId = symbol;

            // Send request to the server
            _session.SendPeriodicityListRequest(context, request);

            // Return result task
            return result;
        }

        public BarEnumerator DownloadBars(string downloadId, string symbol, TickTrader.FDK.Common.PriceType priceType, string periodicity, DateTime from, DateTime to, int timeout)
        {
            return ConvertToSync(DownloadBarsAsync(null, downloadId, symbol, priceType, periodicity, from, to), timeout);
        }

        public Task<BarEnumerator> DownloadBarsAsync(object data, string downloadId, string symbol, TickTrader.FDK.Common.PriceType priceType, string periodicity, DateTime from, DateTime to)
        {
            Task<BarEnumerator> result;

            // Create a new async context
            BarDownloadAsyncContext context = new BarDownloadAsyncContext();
            context.Data = data;
            context.priceType_ = priceType;
            context.periodicity_ = periodicity;
            context.from_ = from;
            context.to_ = to;

            if (data == null)
            {
                context.taskCompletionSource_ = new TaskCompletionSource<BarEnumerator>();
                result = context.taskCompletionSource_.Task;
            }
            else
                result = null;

            // Create a request
            BarDownloadRequest request = new BarDownloadRequest(0);
            request.Id = downloadId;
            request.SymbolId = symbol;
            request.PriceType = Convert(priceType);
            request.Periodicity = periodicity;
            request.From = from;
            request.To = to;

            // Send request to the server
            _session.SendDownloadRequest(context, request);

            // Return result task
            return result;
        }

        public QuoteEnumerator DownloadQuotes(string downloadId, string symbol, QuoteDepth depth, DateTime from, DateTime to, int timeout)
        {
            return ConvertToSync(DownloadQuotesAsync(null, downloadId, symbol, depth, from, to), timeout);
        }

        public Task<QuoteEnumerator> DownloadQuotesAsync(object data, string downloadId, string symbol, QuoteDepth depth, DateTime from, DateTime to)
        {
            Task<QuoteEnumerator> result;

            // Create a new async context
            TickDownloadAsyncContext context = new TickDownloadAsyncContext();
            context.Data = data;
            context.quoteDepth_ = depth;
            context.from_ = from;
            context.to_ = to;

            if (data == null)
            {
                context.taskCompletionSource_ = new TaskCompletionSource<QuoteEnumerator>();
                result = context.taskCompletionSource_.Task;
            }
            else
                result = null;

            // Create a request
            TickDownloadRequest request = new TickDownloadRequest(0);
            request.Id = downloadId;
            request.SymbolId = symbol;
            request.Depth = Convert(depth);
            request.From = from;
            request.To = to;

            // Send request to the server
            _session.SendDownloadRequest(context, request);

            // Return result task
            return result;
        }

        public void SendDownloadCancel(string downloadId)
        {
            // Create a message
            DownloadCancel downloadCancel = new DownloadCancel(0);
            downloadCancel.Id = downloadId;

            // Send message to the server
            _session.SendDownloadCancel(null, downloadCancel);
        }

        SoftFX.Net.QuoteStore.PriceType Convert(TickTrader.FDK.Common.PriceType priceType)
        {
            SoftFX.Net.QuoteStore.PriceType result = 0;

            if ((priceType & TickTrader.FDK.Common.PriceType.Bid) != 0)
                result |= SoftFX.Net.QuoteStore.PriceType.Bid;
            if ((priceType & TickTrader.FDK.Common.PriceType.Ask) != 0)
                result |= SoftFX.Net.QuoteStore.PriceType.Ask;

            return result;
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

        #endregion

        #region Async contexts

        interface IAsyncContext
        {
            void SetDisconnectError(Exception exception);
        }

        class LoginAsyncContext : LoginRequestClientContext, IAsyncContext
        {
            public LoginAsyncContext() : base(false)
            {
            }

            public void SetDisconnectError(Exception exception)
            {
                if (taskCompletionSource_ != null)
                    taskCompletionSource_.SetException(exception);
            }

            public TaskCompletionSource<object> taskCompletionSource_;
        }

        class LogoutAsyncContext : LogoutClientContext, IAsyncContext
        {
            public LogoutAsyncContext() : base(false)
            {
            }

            public void SetDisconnectError(Exception exception)
            {
                if (taskCompletionSource_ != null)
                    taskCompletionSource_.SetException(exception);
            }

            public TaskCompletionSource<LogoutInfo> taskCompletionSource_;
        }

        class SymbolListAsyncContext : SymbolListRequestClientContext, IAsyncContext
        {
            public SymbolListAsyncContext() : base(false)
            {
            }

            public void SetDisconnectError(Exception exception)
            {
                if (taskCompletionSource_ != null)
                    taskCompletionSource_.SetException(exception);
            }

            public TaskCompletionSource<string[]> taskCompletionSource_;
        }

        class PeriodictityListAsyncContext : PeriodicityListRequestClientContext, IAsyncContext
        {
            public PeriodictityListAsyncContext() : base(false)
            {
            }

            public void SetDisconnectError(Exception exception)
            {
                if (taskCompletionSource_ != null)
                    taskCompletionSource_.SetException(exception);
            }

            public TaskCompletionSource<string[]> taskCompletionSource_;
        }

        class BarDownloadAsyncContext : DownloadRequestClientContext, IAsyncContext
        {
            public BarDownloadAsyncContext() : base(false)
            {
            }

            public void SetDisconnectError(Exception exception)
            {
                if (taskCompletionSource_ != null)
                {
                    if (barEnumerator_ != null)
                    {
                        lock (barEnumerator_.mutex_)
                        {
                            barEnumerator_.completed_ = true;

                            if (barEnumerator_.taskCompletionSource_ != null)
                            {
                                barEnumerator_.taskCompletionSource_.SetException(exception);
                                barEnumerator_.taskCompletionSource_ = null;
                            }
                        }
                    }
                    else
                        taskCompletionSource_.SetException(exception);
                }
            }
                        
            public TickTrader.FDK.Common.PriceType priceType_;
            public string periodicity_;
            public DateTime from_;
            public DateTime to_;            
            public byte[] fileData_;
            public int fileSize_;
            public TaskCompletionSource<BarEnumerator> taskCompletionSource_;
            public BarEnumerator barEnumerator_;
            public Bar bar_;            
        }

        class TickDownloadAsyncContext : DownloadRequestClientContext, IAsyncContext
        {
            public TickDownloadAsyncContext() : base(false)
            {
            }

            public void SetDisconnectError(Exception exception)
            {
                if (taskCompletionSource_ != null)
                {
                    if (quoteEnumerator_ != null)
                    {
                        lock (quoteEnumerator_.mutex_)
                        {
                            quoteEnumerator_.completed_ = true;

                            if (quoteEnumerator_.taskCompletionSource_ != null)
                            {
                                quoteEnumerator_.taskCompletionSource_.SetException(exception);
                                quoteEnumerator_.taskCompletionSource_ = null;
                            }                            
                        }
                    }
                    else
                        taskCompletionSource_.SetException(exception);
                }
            }
                        
            public QuoteDepth quoteDepth_;
            public DateTime from_;
            public DateTime to_;
            public byte[] fileData_;
            public int fileSize_;
            public TaskCompletionSource<QuoteEnumerator> taskCompletionSource_;
            public QuoteEnumerator quoteEnumerator_;
            public Quote quote_;            
        }       

        #endregion

        #region Session listener

        class ClientSessionListener : SoftFX.Net.QuoteStore.ClientSessionListener
        {
            public ClientSessionListener(Client client)
            {
                client_ = client;
            }

            public override void OnConnect(ClientSession clientSession)
            {
                try
                {
                    if (client_.ConnectEvent != null)
                    {
                        try
                        {
                            client_.ConnectEvent(client_);
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

            public override void OnConnectError(ClientSession clientSession)
            {
                try
                {
                    if (client_.ConnectErrorEvent != null)
                    {
                        try
                        {
                            // TODO: text
                            client_.ConnectErrorEvent(client_, "Connect error");
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

            public override void OnDisconnect(ClientSession clientSession, ClientContext[] contexts, string text)
            {
                try
                {
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

                    string message = "Client disconnected";

                    if (text != null)
                    {
                        message += " : ";
                        message += text;
                    }

                    Exception exception = new Exception(message);

                    foreach (ClientContext context in contexts)
                        ((IAsyncContext)context).SetDisconnectError(exception);                                        
                }
                catch
                {
                }
            }

            public override void OnLoginReport(ClientSession session, LoginRequestClientContext LoginRequestClientContext, LoginReport message)
            {
                var context = (LoginAsyncContext) LoginRequestClientContext;

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

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetResult(null);
                }
                catch (Exception exception)
                {
                    if (client_.LoginErrorEvent != null)
                    {
                        try
                        {
                            client_.LoginErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetException(exception);
                }
            }

            public override void OnLoginReject(ClientSession session, LoginRequestClientContext LoginRequestClientContext, LoginReject message)
            {
                var context = (LoginAsyncContext) LoginRequestClientContext;

                try
                {
                    string text = message.Text;

                    if (client_.LoginErrorEvent != null)
                    {
                        try
                        {
                            client_.LoginErrorEvent(client_, context.Data, text);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                    {
                        Exception exception = new Exception(text);
                        context.taskCompletionSource_.SetException(exception);
                    }
                }
                catch (Exception exception)
                {
                    if (client_.LoginErrorEvent != null)
                    {
                        try
                        {
                            client_.LoginErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetException(exception);
                }
            }

            public override void OnLogout(ClientSession session, LogoutClientContext LogoutClientContext, Logout message)
            {
                var context = (LogoutAsyncContext) LogoutClientContext ;

                try
                {
                    var result = new LogoutInfo();
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

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetResult(result);
                }
                catch
                {
                    // on logout we don't throw

                    var result = new LogoutInfo();
                    result.Reason = TickTrader.FDK.Common.LogoutReason.Unknown;

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

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetResult(result);
                }
            }

            public override void OnSymbolListReport(ClientSession session, SymbolListRequestClientContext SymbolListRequestClientContext, SymbolListReport message)
            {
                var context = (SymbolListAsyncContext)SymbolListRequestClientContext;

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

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetResult(resultSymbols);
                }
                catch (Exception exception)
                {
                    if (client_.SymbolListErrorEvent != null)
                    {
                        try
                        {
                            client_.SymbolListErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetException(exception);
                }
            }

            public override void OnSymbolListReject(ClientSession session, SymbolListRequestClientContext SymbolListRequestClientContext, Reject message)
            {
                var context = (SymbolListAsyncContext) SymbolListRequestClientContext;

                try
                {
                    string text = message.Text;

                    if (client_.SymbolListErrorEvent != null)
                    {
                        try
                        {
                            client_.SymbolListErrorEvent(client_, context.Data, text);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                    {
                        Exception exception = new Exception(text);
                        context.taskCompletionSource_.SetException(exception);
                    }
                }
                catch (Exception exception)
                {
                    if (client_.SymbolListErrorEvent != null)
                    {
                        try
                        {
                            client_.SymbolListErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetException(exception);
                }
            }

            public override void OnPeriodicityListReport(ClientSession session, PeriodicityListRequestClientContext PeriodicityListRequestClientContext, PeriodicityListReport message)
            {
                PeriodictityListAsyncContext context = (PeriodictityListAsyncContext) PeriodicityListRequestClientContext;

                try
                {
                    StringArray reportPeriodicities = message.Periodicities;
                    int count = reportPeriodicities.Length;
                    string[] resultPeriodicities = new string[count];

                    for (int index = 0; index < count; ++index)
                    {
                        resultPeriodicities[index] = reportPeriodicities[index];
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

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetResult(resultPeriodicities);
                }
                catch (Exception exception)
                {
                    if (client_.SymbolListErrorEvent != null)
                    {
                        try
                        {
                            client_.SymbolListErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetException(exception);
                }
            }

            public override void OnPeriodicityListReject(ClientSession session, PeriodicityListRequestClientContext PeriodicityListRequestClientContext, Reject message)
            {
                var context = (PeriodictityListAsyncContext) PeriodicityListRequestClientContext;

                try
                {
                    string text = message.Text;

                    if (client_.PeriodicityListErrorEvent != null)
                    {
                        try
                        {
                            client_.PeriodicityListErrorEvent(client_, context.Data, text);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                    {
                        Exception exception = new Exception(text);
                        context.taskCompletionSource_.SetException(exception);
                    }
                }
                catch (Exception exception)
                {
                    if (client_.PeriodicityListErrorEvent != null)
                    {
                        try
                        {
                            client_.PeriodicityListErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    if (context.taskCompletionSource_ != null)
                        context.taskCompletionSource_.SetException(exception);
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
                        context.bar_ = new Bar();

                        if (client_.BarDownloadResultBeginEvent != null)
                        {
                            try
                            {
                                client_.BarDownloadResultBeginEvent(client_, context.Data, message.RequestId);
                            }
                            catch
                            {
                            }
                        }

                        if (context.taskCompletionSource_ != null)
                        {
                            context.barEnumerator_ = new BarEnumerator(client_, message.RequestId);
                            context.taskCompletionSource_.SetResult(context.barEnumerator_);
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.BarDownloadErrorEvent != null)
                        {
                            try
                            {
                                client_.BarDownloadErrorEvent(client_, context.Data, message.RequestId, exception.Message);
                            }
                            catch
                            {
                            }
                        }

                        if (context.taskCompletionSource_ != null)
                            context.taskCompletionSource_.SetException(exception);
                    }
                }
                else
                {
                    TickDownloadAsyncContext context = (TickDownloadAsyncContext) DownloadRequestClientContext;

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

                        if (client_.QuoteDownloadResultBeginEvent != null)
                        {
                            try
                            {
                                client_.QuoteDownloadResultBeginEvent(client_, context.Data, message.RequestId);
                            }
                            catch
                            {
                            }
                        }

                        if (context.taskCompletionSource_ != null)
                        {
                            context.quoteEnumerator_ = new QuoteEnumerator(client_, message.RequestId);
                            context.taskCompletionSource_.SetResult(context.quoteEnumerator_);
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.QuoteDownloadErrorEvent != null)
                        {
                            try
                            {
                                client_.QuoteDownloadErrorEvent(client_, context.Data, message.RequestId, exception.Message);
                            }
                            catch
                            {
                            }
                        }

                        if (context.taskCompletionSource_ != null)
                            context.taskCompletionSource_.SetException(exception);
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
                                client_.BarDownloadErrorEvent(client_, context.Data, message.RequestId, exception.Message);
                            }
                            catch
                            {
                            }
                        }

                        if (context.taskCompletionSource_ != null)
                            SetBarEnumeratorError(context, exception);
                    }
                }
                else
                {
                    TickDownloadAsyncContext context = (TickDownloadAsyncContext) DownloadRequestClientContext;

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
                                client_.QuoteDownloadErrorEvent (client_, context.Data, message.RequestId, exception.Message);
                            }
                            catch
                            {
                            }
                        }

                        if (context.taskCompletionSource_ != null)
                            SetQuoteEnumeratorError(context, exception);
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
                            string fileName = context.periodicity_ + " " + context.priceType_.ToString("g").ToLowerInvariant() + ".txt";
                            ZipEntry zipEntry = zipFile.GetEntry(fileName);

                            if (zipEntry == null)
                                throw new Exception(string.Format("Could not find file {0} inside zip archive", fileName));

                            using (Stream zipInputStream = zipFile.GetInputStream(zipEntry))
                            {
                                Serialization.BarFormatter barFormatter = new Serialization.BarFormatter(zipInputStream);

                                while (! barFormatter.IsEnd)
                                {
                                    barFormatter.Deserialize(context.bar_);

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

                                    if (context.taskCompletionSource_ != null)
                                        SetBarEnumeratorResult(context);
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
                            barFormatter.Deserialize(context.bar_);

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

                            if (context.taskCompletionSource_ != null)
                                SetBarEnumeratorResult(context);
                        }
                    }
                }
            }

            void ProcessQuoteDownloadFile(TickDownloadAsyncContext context, string downloadId)
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

                                    if (context.taskCompletionSource_ != null)
                                        SetQuoteEnumeratorResult(context);
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

                            if (context.taskCompletionSource_ != null)
                                SetQuoteEnumeratorResult(context);
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

                        if (context.taskCompletionSource_ != null)
                            SetBarEnumeratorEnd(context);
                    }
                    catch (Exception exception)
                    {
                        if (client_.BarDownloadErrorEvent != null)
                        {
                            try
                            {
                                client_.BarDownloadErrorEvent(client_, context.Data, message.RequestId, exception.Message);
                            }
                            catch
                            {
                            }
                        }

                        if (context.taskCompletionSource_ != null)
                            SetBarEnumeratorError(context, exception);
                    }
                }
                else
                {
                    TickDownloadAsyncContext context = (TickDownloadAsyncContext) DownloadRequestClientContext;

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

                        if (context.taskCompletionSource_ != null)
                            SetQuoteEnumeratorEnd(context);
                    }
                    catch (Exception exception)
                    {
                        if (client_.QuoteDownloadErrorEvent != null)
                        {
                            try
                            {
                                client_.QuoteDownloadErrorEvent(client_, context.Data, message.RequestId, exception.Message);
                            }
                            catch
                            {
                            }
                        }

                        if (context.taskCompletionSource_ != null)
                            SetQuoteEnumeratorError(context, exception);
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
                        string text = message.Text;

                        if (client_.BarDownloadErrorEvent != null)
                        {
                            try
                            {
                                client_.BarDownloadErrorEvent(client_, context.Data, message.RequestId, text);
                            }
                            catch
                            {
                            }
                        }

                        if (context.taskCompletionSource_ != null)
                        {
                            Exception exception = new Exception(text);

                            if (context.barEnumerator_ != null)
                            {
                                SetBarEnumeratorError(context, exception);
                            }
                            else
                                context.taskCompletionSource_.SetException(exception);
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.BarDownloadErrorEvent != null)
                        {
                            try
                            {
                                client_.BarDownloadErrorEvent(client_, context.Data, message.RequestId, exception.Message);
                            }
                            catch
                            {
                            }
                        }

                        if (context.taskCompletionSource_ != null)
                        {
                            if (context.barEnumerator_ != null)
                            {
                                SetBarEnumeratorError(context, exception);
                            }
                            else
                                context.taskCompletionSource_.SetException(exception);
                        }
                    }
                }
                else
                {
                    TickDownloadAsyncContext context = (TickDownloadAsyncContext) DownloadRequestClientContext;

                    try
                    {
                        string text = message.Text;

                        if (client_.QuoteDownloadErrorEvent != null)
                        {
                            try
                            {
                                client_.QuoteDownloadErrorEvent(client_, context.Data, message.RequestId, text);
                            }
                            catch
                            {
                            }
                        }

                        if (context.taskCompletionSource_ != null)
                        {
                            Exception exception = new Exception(text);

                            if (context.quoteEnumerator_ != null)
                            {
                                SetQuoteEnumeratorError(context, exception);
                            }
                            else
                                context.taskCompletionSource_.SetException(exception);
                        }
                    }
                    catch (Exception exception)
                    {
                        if (client_.QuoteDownloadErrorEvent != null)
                        {
                            try
                            {
                                client_.QuoteDownloadErrorEvent(client_, context.Data, message.RequestId, exception.Message);
                            }
                            catch
                            {
                            }
                        }

                        if (context.taskCompletionSource_ != null)
                        {
                            if (context.quoteEnumerator_ != null)
                            {
                                SetQuoteEnumeratorError(context, exception);
                            }
                            else
                                context.taskCompletionSource_.SetException(exception);
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

            void SetBarEnumeratorResult(BarDownloadAsyncContext context)
            {
                while (true)
                {
                    lock (context.barEnumerator_.mutex_)
                    {
                        if (context.barEnumerator_.taskCompletionSource_ != null)
                        {
                            Bar bar = context.bar_;
                            context.bar_ = context.barEnumerator_.bar_;
                            context.barEnumerator_.bar_ = bar;

                            context.barEnumerator_.completed_ = false;
                            context.barEnumerator_.taskCompletionSource_.SetResult(bar);
                            context.barEnumerator_.taskCompletionSource_ = null;

                            break;
                        }

                        if (context.barEnumerator_.completed_)
                            break;
                    }

                    context.barEnumerator_.event_.WaitOne();
                }
            }

            void SetBarEnumeratorEnd(BarDownloadAsyncContext context)
            {
                while (true)
                {
                    lock (context.barEnumerator_.mutex_)
                    {
                        if (context.barEnumerator_.taskCompletionSource_ != null)
                        {
                            context.barEnumerator_.completed_ = true;
                            context.barEnumerator_.taskCompletionSource_.SetResult(null);
                            context.barEnumerator_.taskCompletionSource_ = null;

                            break;
                        }

                        if (context.barEnumerator_.completed_)
                            break;
                    }

                    context.barEnumerator_.event_.WaitOne();
                }
            }

            void SetBarEnumeratorError(BarDownloadAsyncContext context, Exception exception)
            {
                while (true)
                {
                    lock (context.barEnumerator_.mutex_)
                    {
                        if (context.barEnumerator_.taskCompletionSource_ != null)
                        {
                            context.barEnumerator_.completed_ = true;
                            context.barEnumerator_.taskCompletionSource_.SetException(exception);
                            context.barEnumerator_.taskCompletionSource_ = null;

                            break;
                        }

                        if (context.barEnumerator_.completed_)
                            break;
                    }

                    context.barEnumerator_.event_.WaitOne();
                }
            }

            void SetQuoteEnumeratorResult(TickDownloadAsyncContext context)
            {
                while (true)
                {
                    lock (context.quoteEnumerator_.mutex_)
                    {
                        if (context.quoteEnumerator_.taskCompletionSource_ != null)
                        {
                            Quote quote = context.quote_;
                            context.quote_ = context.quoteEnumerator_.quote_;
                            context.quoteEnumerator_.quote_ = quote;

                            context.quoteEnumerator_.completed_ = false;
                            context.quoteEnumerator_.taskCompletionSource_.SetResult(quote);
                            context.quoteEnumerator_.taskCompletionSource_ = null;                            

                            break;
                        }

                        if (context.quoteEnumerator_.completed_)
                            break;
                    }

                    context.quoteEnumerator_.event_.WaitOne();
                }
            }

            void SetQuoteEnumeratorEnd(TickDownloadAsyncContext context)
            {
                while (true)
                {
                    lock (context.quoteEnumerator_.mutex_)
                    {
                        if (context.quoteEnumerator_.taskCompletionSource_ != null)
                        {
                            context.quoteEnumerator_.completed_ = true;
                            context.quoteEnumerator_.taskCompletionSource_.SetResult(null);
                            context.quoteEnumerator_.taskCompletionSource_ = null;                            

                            break;
                        }

                        if (context.quoteEnumerator_.completed_)
                            break;
                    }

                    context.quoteEnumerator_.event_.WaitOne();
                }
            }

            void SetQuoteEnumeratorError(TickDownloadAsyncContext context, Exception exception)
            {
                while (true)
                {
                    lock (context.quoteEnumerator_.mutex_)
                    {
                        if (context.quoteEnumerator_.taskCompletionSource_ != null)
                        {
                            context.quoteEnumerator_.completed_ = true;
                            context.quoteEnumerator_.taskCompletionSource_.SetException(exception);
                            context.quoteEnumerator_.taskCompletionSource_ = null;                            

                            break;
                        }

                        if (context.quoteEnumerator_.completed_)
                            break;
                    }

                    context.quoteEnumerator_.event_.WaitOne();
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

                    case SoftFX.Net.QuoteStore.LogoutReason.SlowConnection:
                        return TickTrader.FDK.Common.LogoutReason.SlowConnection;

                    case SoftFX.Net.QuoteStore.LogoutReason.DeletedLogin:
                        return TickTrader.FDK.Common.LogoutReason.LoginDeleted;

                    case SoftFX.Net.QuoteStore.LogoutReason.InternalServerError:
                        return TickTrader.FDK.Common.LogoutReason.ServerError;

                    case SoftFX.Net.QuoteStore.LogoutReason.BlockedLogin:
                        return TickTrader.FDK.Common.LogoutReason.BlockedAccount;

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

        #region Async helpers        

        internal static void ConvertToSync(Task task, int timeout)
        {
            try
            {
                if (!task.Wait(timeout))
                    throw new TimeoutException("Method call timeout");
            }
            catch (AggregateException ex)
            {
                ExceptionDispatchInfo.Capture(ex.Flatten().InnerExceptions[0]).Throw();
            }
        }

        internal static TResult ConvertToSync<TResult>(Task<TResult> task, int timeout)
        {
            try
            {
                if (!task.Wait(timeout))
                    throw new TimeoutException("Method call timeout");

                return task.Result;
            }
            catch (AggregateException ex)
            {
                ExceptionDispatchInfo.Capture(ex.Flatten().InnerExceptions[0]).Throw();
                // Unreacheble code...
                return default(TResult);
            }
        }

        #endregion
    }
}
