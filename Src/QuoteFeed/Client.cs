using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using SoftFX.Net.QuoteFeed;
using TickTrader.FDK.Common;

namespace TickTrader.FDK.QuoteFeed
{
    public class Client : IDisposable
    {
        #region Constructors

        public Client(string name) : this(name, 5030, true, "Logs", false)
        {
        }

        public Client(string name, int port, bool reconnect, string logDirectory, bool logMessages)
        {
            ClientSessionOptions options = new ClientSessionOptions(port);
            options.ConnectionType = SoftFX.Net.Core.ConnectionType.Secure;
            options.ServerCertificateName = "TickTraderManagerService";
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

        private readonly ClientSession _session;
        private readonly ClientSessionListener _sessionListener;

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

        public bool IsConnected { get; private set; }

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
        public delegate void OneTimePasswordRequestDelegate(Client client, string message);
        public delegate void OneTimePasswordRejectDelegate(Client client, string message);
        public delegate void LogoutResultDelegate(Client client, object data, LogoutInfo logoutInfo);
        public delegate void LogoutDelegate(Client client, LogoutInfo logoutInfo);
        
        public event LoginResultDelegate LoginResultEvent;
        public event LoginErrorDelegate LoginErrorEvent;
        public event OneTimePasswordRequestDelegate OneTimePasswordRequestEvent;
        public event OneTimePasswordRejectDelegate OneTimePasswordRejectEvent;
        public event LogoutResultDelegate LogoutResultEvent;
        public event LogoutDelegate LogoutEvent;

        public void Login(string username, string password, string deviceId, string appSessionId, int timeout)
        {
            ConvertToSync(LoginAsync(null, username, password, deviceId, appSessionId), timeout);
        }

        public Task LoginAsync(object data, string username, string password, string deviceId, string appSessionId)
        {
            // Create a new async context
            var context = new LoginAsyncContext();
            context.Data = data;

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
            return context.Tcs.Task;
        }

        public void SendOneTimePassword(string oneTimePassword)
        {
            // Create a message
            var message = new TwoFactorLogin(0)
            {
                Reason = TwoFactorReason.ClientResponse,
                OneTimePassword = oneTimePassword
            };

            // Send message to the server
            _session.Send(message);
        }

        public LogoutInfo Logout(string message, int timeout)
        {
            return ConvertToSync(LogoutAsync(null, message), timeout);
        }

        public Task<LogoutInfo> LogoutAsync(object data, string message)
        {
            // Create a new async context
            var context = new LogoutAsyncContext();
            context.Data = data;

            // Create a request
            var request = new Logout(0)
            {
                Text = message
            };

            // Send request to the server
            _session.SendLogout(context, request);

            // Return result task
            return context.Tcs.Task;
        }

        #endregion

        #region Quote Feed

        public delegate void CurrencyListResultDelegate(Client client, object data, CurrencyInfo[] infos);
        public delegate void CurrencyListErrorDelegate(Client client, object data, string message);
        public delegate void SymbolListResultDelegate(Client client, object data, SymbolInfo[] infos);
        public delegate void SymbolListErrorDelegate(Client client, object data, string message);
        public delegate void SessionInfoResultDelegate(Client client, object data, SessionInfo info);
        public delegate void SessionInfoErrorDelegate(Client client, object data, string message);
        public delegate void SubscribeQuotesResultDelegate(Client client, object data);
        public delegate void SubscribeQuotesErrorDelegate(Client client, object data, string message);
        public delegate void UnsubscribeQuotesResultDelegate(Client client, object data);
        public delegate void UnsubscribeQuotesErrorDelegate(Client client, object data, string message);
        public delegate void QuotesResultDelegate(Client client, object data, Quote[] quotes);
        public delegate void QuotesErrorDelegate(Client client, object data, string message);
        public delegate void SessionInfoUpdateDelegate(Client client, SessionInfo info);
        public delegate void QuotesBeginDelegate(Client client, Quote[] quotes);
        public delegate void QuotesEndDelegate(Client client, string[] symbolIds);
        public delegate void QuoteUpdateDelegate(Client client, Quote quote);        
        public delegate void NotificationDelegate(Client client, Common.Notification notification);

        public event CurrencyListResultDelegate CurrencyListResultEvent;
        public event CurrencyListErrorDelegate CurrencyListErrorEvent;
        public event SymbolListResultDelegate SymbolListResultEvent;
        public event SymbolListErrorDelegate SymbolListErrorEvent;
        public event SessionInfoResultDelegate SessionInfoResultEvent;
        public event SessionInfoErrorDelegate SessionInfoErrorEvent;        
        public event SubscribeQuotesResultDelegate SubscribeQuotesResultEvent;
        public event SubscribeQuotesErrorDelegate SubscribeQuotesErrorEvent;
        public event UnsubscribeQuotesResultDelegate UnsubscribeQuotesResultEvent;
        public event UnsubscribeQuotesErrorDelegate UnsubscribeQuotesErrorEvent;
        public event QuotesResultDelegate QuotesResultEvent;
        public event QuotesErrorDelegate QuotesErrorEvent;
        public event SessionInfoUpdateDelegate SessionInfoUpdateEvent;
        public event QuotesBeginDelegate QuotesBeginEvent;
        public event QuotesEndDelegate QuotesEndEvent;
        public event QuoteUpdateDelegate QuoteUpdateEvent;
        public event NotificationDelegate NotificationEvent;

        public CurrencyInfo[] GetCurrencyList(int timeout)
        {
            return ConvertToSync(GetCurrencyListAsync(null), timeout);
        }

        public Task<CurrencyInfo[]> GetCurrencyListAsync(object data)
        {
            // Create a new async context
            var context = new CurrencyListAsyncContext();
            context.Data = data;

            // Create a request
            var request = new CurrencyListRequest(0)
            {
                Id = Guid.NewGuid().ToString(),
                Type = CurrencyListRequestType.All
            };

            // Send request to the server
            _session.SendCurrencyListRequest(context, request);

            // Return result task
            return context.Tcs.Task;
        }

        public SymbolInfo[] GetSymbolList(int timeout)
        {
            return ConvertToSync(GetSymbolListAsync(null), timeout);
        }

        public Task<SymbolInfo[]> GetSymbolListAsync(object data)
        {
            // Create a new async context
            var context = new SymbolListAsyncContext();
            context.Data = data;

            // Create a request
            var request = new SecurityListRequest(0)
            {
                Id = Guid.NewGuid().ToString(),
                Type = SecurityListRequestType.All
            };

            // Send request to the server
            _session.SendSecurityListRequest(context, request);

            // Return result task
            return context.Tcs.Task;
        }

        public SessionInfo GetSessionInfo(int timeout)
        {
            return ConvertToSync(GetSessionInfoAsync(null), timeout);
        }

        public Task<SessionInfo> GetSessionInfoAsync(object data)
        {
            // Create a new async context
            var context = new SessionInfoAsyncContext();
            context.Data = data;

            // Create a request
            var request = new TradingSessionStatusRequest(0);
            request.Id = Guid.NewGuid().ToString();

            // Send request to the server
            _session.SendTradingSessionStatusRequest(context, request);

            // Return result task
            return context.Tcs.Task;
        }

        public void SubscribeQuotes(string[] symbolIds, int marketDepth, int timeout)
        {
            ConvertToSync(SubscribeQuotesAsync(null, symbolIds, marketDepth), timeout);
        }

        public Task SubscribeQuotesAsync(object data, string[] symbolIds, int marketDepth)
        {
            // Create a new async context
            var context = new SubscribeQuotesAsyncContext();
            context.Data = data;

            // Create a request
            var request = new MarketDataRequest(0);
            request.Id = Guid.NewGuid().ToString();
            request.RequestType = MarketDataRequestType.Subscribe;
            request.UpdateType = SoftFX.Net.QuoteFeed.MarketDataUpdateType.FullRefresh;
            request.MarketDepth = (ushort) marketDepth;

            StringArray requestSymbolIds = request.SymbolIds;
            int count = symbolIds.Length;
            requestSymbolIds.Resize(count);

            for (int index = 0; index < count; ++ index)
                requestSymbolIds[index] = symbolIds[index];

            // Send request to the server
            _session.SendMarketDataRequest(context, request);

            // Return result task
            return context.Tcs.Task;
        }

        public void UnsbscribeQuotes(string[] symbolIds, int timeout)
        {
            ConvertToSync(UnsubscribeQuotesAsync(null, symbolIds), timeout);
        }

        public Task UnsubscribeQuotesAsync(object data, string[] symbolIds)
        {
            // Create a new async context
            var context = new UnsubscribeQuotesAsyncContext();
            context.Data = data;
            context.SymbolIds = symbolIds;

            // Create a request
            var request = new MarketDataRequest(0);
            request.Id = Guid.NewGuid().ToString();
            request.RequestType = MarketDataRequestType.Unsubscribe;

            StringArray requestSymbolIds = request.SymbolIds;
            int count = symbolIds.Length;
            requestSymbolIds.Resize(count);

            for (int index = 0; index < count; ++index)
                requestSymbolIds[index] = symbolIds[index];

            // Send request to the server
            _session.SendMarketDataRequest(context, request);

            // Return result task
            return context.Tcs.Task;
        }

        public Quote[] GetQuotes(string[] symbolIds, int marketDepth, int timeout)
        {
            return ConvertToSync(GetQuotesAsync(null, symbolIds, marketDepth), timeout);
        }

        public Task<Quote[]> GetQuotesAsync(object data, string[] symbolIds, int marketDepth)
        {
            // Create a new async context
            var context = new GetQuotesAsyncContext();
            context.Data = data;

            // Create a request
            var request = new MarketDataRequest(0);
            request.Id = Guid.NewGuid().ToString();
            request.RequestType = MarketDataRequestType.Snapshot;
            request.UpdateType = SoftFX.Net.QuoteFeed.MarketDataUpdateType.FullRefresh;
            request.MarketDepth = (ushort)marketDepth;

            StringArray requestSymbolIds = request.SymbolIds;
            int count = symbolIds.Length;
            requestSymbolIds.Resize(count);

            for (int index = 0; index < count; ++index)
                requestSymbolIds[index] = symbolIds[index];

            // Send request to the server
            _session.SendMarketDataRequest(context, request);

            // Return result task
            return context.Tcs.Task;
        }

        #endregion

        #region Async contexts

        private interface IAsyncContext
        {
            void SetException(Exception ex);
        }

        private class LoginAsyncContext : LoginRequestClientContext, IAsyncContext
        {
            public LoginAsyncContext() : base(false) { }

            public void SetException(Exception ex) { Tcs.SetException(ex); }

            public readonly TaskCompletionSource<object> Tcs = new TaskCompletionSource<object>();
        }

        private class LogoutAsyncContext : LogoutClientContext, IAsyncContext
        {
            public LogoutAsyncContext() : base(false) { }

            public void SetException(Exception ex) { Tcs.SetException(ex); }

            public readonly TaskCompletionSource<LogoutInfo> Tcs = new TaskCompletionSource<LogoutInfo>();
        }

        private class CurrencyListAsyncContext : CurrencyListRequestClientContext, IAsyncContext
        {
            public CurrencyListAsyncContext() : base(false) { }

            public void SetException(Exception ex) { Tcs.SetException(ex); }

            public readonly TaskCompletionSource<CurrencyInfo[]> Tcs = new TaskCompletionSource<CurrencyInfo[]>();
        }

        private class SymbolListAsyncContext : SecurityListRequestClientContext, IAsyncContext
        {
            public SymbolListAsyncContext() : base(false) { }

            public void SetException(Exception ex) { Tcs.SetException(ex); }

            public readonly TaskCompletionSource<SymbolInfo[]> Tcs = new TaskCompletionSource<SymbolInfo[]>();
        }

        private class SessionInfoAsyncContext : TradingSessionStatusRequestClientContext, IAsyncContext
        {
            public SessionInfoAsyncContext() : base(false) { }

            public void SetException(Exception ex) { Tcs.SetException(ex); }

            public readonly TaskCompletionSource<SessionInfo> Tcs = new TaskCompletionSource<SessionInfo>();
        }

        private class SubscribeQuotesAsyncContext : MarketDataRequestClientContext, IAsyncContext
        {
            public SubscribeQuotesAsyncContext() : base(false) { }

            public void SetException(Exception ex) { Tcs.SetException(ex); }

            public readonly TaskCompletionSource<object> Tcs = new TaskCompletionSource<object>();
        }

        private class UnsubscribeQuotesAsyncContext : MarketDataRequestClientContext, IAsyncContext
        {
            public UnsubscribeQuotesAsyncContext() : base(false) { }

            public string[] SymbolIds;

            public void SetException(Exception ex) { Tcs.SetException(ex); }

            public readonly TaskCompletionSource<object> Tcs = new TaskCompletionSource<object>();
        }

        private class GetQuotesAsyncContext : MarketDataRequestClientContext, IAsyncContext
        {
            public GetQuotesAsyncContext() : base(false) { }

            public void SetException(Exception ex) { Tcs.SetException(ex); }

            public readonly TaskCompletionSource<Quote[]> Tcs = new TaskCompletionSource<Quote[]>();
        }

        #endregion

        #region Session listener

        private class ClientSessionListener : SoftFX.Net.QuoteFeed.ClientSessionListener
        {
            public ClientSessionListener(Client client)
            {
                client_ = client;
            }

            public override void OnConnect(ClientSession clientSession)
            {
                try
                {
                    client_.IsConnected = true;

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
                    client_.IsConnected = false;

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
                    string message = "Client disconnected";
                    if (text != null)
                    {
                        message += " : ";
                        message += text;
                    }
                    Exception exception = new Exception(message);

                    foreach (ClientContext context in contexts)
                        ((IAsyncContext)context).SetException(exception);
                                        
                    client_.IsConnected = false;

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

                    context.Tcs.SetResult(null);
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

                    context.Tcs.SetException(exception);
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

                    var exception = new Exception(text);
                    context.Tcs.SetException(exception);
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

                    context.Tcs.SetException(exception);
                }
            }

            public override void OnTwoFactorLoginRequest(ClientSession session, LoginRequestClientContext LoginRequestClientContext, TwoFactorLogin message)
            {
                var context = (LoginAsyncContext) LoginRequestClientContext;

                try
                {
                    string text = message.Text;

                    if (client_.OneTimePasswordRequestEvent != null)
                    {
                        try
                        {
                            client_.OneTimePasswordRequestEvent(client_, text);
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
                            client_.LoginErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    context.Tcs.SetException(exception);
                }
            }

            public override void OnTwoFactorLoginSuccess(ClientSession session, LoginRequestClientContext LoginRequestClientContext, TwoFactorLogin message)
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

                    context.Tcs.SetResult(null);
                }
                catch (Exception exception)
                {
                    if (client_.LoginErrorEvent != null)
                    {
                        try
                        {
                            client_.LoginErrorEvent(client_, LoginRequestClientContext.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    context.Tcs.SetException(exception);
                }
            }

            public override void OnTwoFactorLoginReject(ClientSession session, LoginRequestClientContext LoginRequestClientContext, TwoFactorReject message)
            {
                var context = (LoginAsyncContext) LoginRequestClientContext;

                try
                {
                    string text = message.Text;

                    if (client_.OneTimePasswordRejectEvent != null)
                    {
                        try
                        {
                            client_.OneTimePasswordRejectEvent(client_, text);
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
                            client_.LoginErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    context.Tcs.SetException(exception);
                }
            }

            public override void OnTwoFactorLoginError(ClientSession session, LoginRequestClientContext LoginRequestClientContext, TwoFactorLogin message)
            {
                var context = (LoginAsyncContext) LoginRequestClientContext;

                try
                {
                    string text = message.Text;

                    if (client_.LoginErrorEvent != null)
                    {
                        try
                        {
                            client_.LoginErrorEvent(client_, LoginRequestClientContext.Data, text);
                        }
                        catch
                        {
                        }
                    }

                    var exception = new Exception(text);
                    context.Tcs.SetException(exception);
                }
                catch (Exception exception)
                {
                    if (client_.LoginErrorEvent != null)
                    {
                        try
                        {
                            client_.LoginErrorEvent(client_, LoginRequestClientContext.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    context.Tcs.SetException(exception);
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

                    context.Tcs.SetResult(result);
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

                    context.Tcs.SetResult(result);
                }
            }

            public override void OnCurrencyListReport(ClientSession session, CurrencyListRequestClientContext CurrencyListRequestClientContext, CurrencyListReport message)
            {
                var context = (CurrencyListAsyncContext)CurrencyListRequestClientContext;

                try
                {
                    CurrencyArray reportCurrencies = message.Currencies;
                    int count = reportCurrencies.Length;
                    TickTrader.FDK.Common.CurrencyInfo[] resultCurrencies = new TickTrader.FDK.Common.CurrencyInfo[count];

                    for (int index = 0; index < count; ++index)
                    {
                        Currency reportCurrency = reportCurrencies[index];
                        TickTrader.FDK.Common.CurrencyInfo resultCurrency = new TickTrader.FDK.Common.CurrencyInfo();

                        resultCurrency.Name = reportCurrency.Id;
                        resultCurrency.Description = reportCurrency.Description;
                        resultCurrency.Precision = reportCurrency.Precision;
                        resultCurrency.SortOrder = reportCurrency.SortOrder;

                        resultCurrencies[index] = resultCurrency;
                    }

                    if (client_.CurrencyListResultEvent != null)
                    {
                        try
                        {
                            client_.CurrencyListResultEvent(client_, context.Data, resultCurrencies);
                        }
                        catch
                        {
                        }
                    }

                    context.Tcs.SetResult(resultCurrencies);
                }
                catch (Exception exception)
                {
                    if (client_.CurrencyListErrorEvent != null)
                    {
                        try
                        {
                            client_.CurrencyListErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    context.Tcs.SetException(exception);
                }
            }

            public override void OnCurrencyListReject(ClientSession session, CurrencyListRequestClientContext CurrencyListRequestClientContext, Reject message)
            {
                var context = (CurrencyListAsyncContext)CurrencyListRequestClientContext;

                try
                {
                    string text = message.Text;

                    if (client_.CurrencyListErrorEvent != null)
                    {
                        try
                        {
                            client_.CurrencyListErrorEvent(client_, context.Data, text);
                        }
                        catch
                        {
                        }
                    }

                    var exception = new Exception(text);
                    context.Tcs.SetException(exception);
                }
                catch (Exception exception)
                {
                    if (client_.CurrencyListErrorEvent != null)
                    {
                        try
                        {
                            client_.CurrencyListErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    context.Tcs.SetException(exception);
                }
            }

            public override void OnSecurityListReport(ClientSession session, SecurityListRequestClientContext SecurityListRequestClientContext, SecurityListReport message)
            {
                var context = (SymbolListAsyncContext)SecurityListRequestClientContext;

                try
                {
                    SymbolArray reportSymbols = message.Symbols;
                    int count = reportSymbols.Length;
                    TickTrader.FDK.Common.SymbolInfo[] resultSymbols = new TickTrader.FDK.Common.SymbolInfo[count];

                    for (int index = 0; index < count; ++index)
                    {
                        Symbol reportSymbol = reportSymbols[index];
                        TickTrader.FDK.Common.SymbolInfo resultSymbol = new TickTrader.FDK.Common.SymbolInfo();

                        resultSymbol.Name = reportSymbol.Id;
                        resultSymbol.Currency = reportSymbol.CurrencyId;
                        resultSymbol.SettlementCurrency = reportSymbol.SettlCurrencyId;
                        resultSymbol.Description = reportSymbol.Description;
                        resultSymbol.Precision = (int) Math.Log(reportSymbol.ContractMultiplier, 10);
                        resultSymbol.RoundLot = reportSymbol.RoundLot;
                        resultSymbol.MinTradeVolume = reportSymbol.MinTradeVol;
                        resultSymbol.MaxTradeVolume = reportSymbol.MaxTradeVol;
                        resultSymbol.TradeVolumeStep = reportSymbol.TradeVolStep;
                        resultSymbol.ProfitCalcMode = Convert(reportSymbol.SettlCalcMode);
                        resultSymbol.MarginCalcMode = Convert(reportSymbol.CalcMode); 
                        resultSymbol.MarginHedge = reportSymbol.MarginHedge;
                        resultSymbol.MarginFactorFractional = reportSymbol.MarginFactor;
                        resultSymbol.ContractMultiplier = reportSymbol.ContractMultiplier;
                        resultSymbol.Color = (int) reportSymbol.Color;
                        resultSymbol.CommissionType = Convert(reportSymbol.CommissionType);
                        resultSymbol.CommissionChargeType = Convert(reportSymbol.CommissionChargeType);
                        resultSymbol.CommissionChargeMethod = Convert(reportSymbol.CommissionChargeMethod);
                        resultSymbol.LimitsCommission = reportSymbol.LimitsCommission;
                        resultSymbol.Commission = reportSymbol.Commission;
                        resultSymbol.SwapSizeShort = reportSymbol.SwapSizeShort;
                        resultSymbol.SwapSizeLong = reportSymbol.SwapSizeLong;
                        resultSymbol.DefaultSlippage = reportSymbol.DefaultSlippage;
                        resultSymbol.IsTradeEnabled = reportSymbol.TradeEnabled;
                        resultSymbol.GroupSortOrder = reportSymbol.SecuritySortOrder;
                        resultSymbol.SortOrder = reportSymbol.SortOrder;
                        resultSymbol.CurrencySortOrder = reportSymbol.CurrencySortOrder;
                        resultSymbol.SettlementCurrencySortOrder = reportSymbol.SettlCurrencySortOrder;
                        resultSymbol.CurrencyPrecision = reportSymbol.CurrencyPrecision;
                        resultSymbol.SettlementCurrencyPrecision = reportSymbol.SettlCurrencyPrecision;
                        resultSymbol.StatusGroupId = reportSymbol.StatusGroupId;
                        resultSymbol.SecurityName = reportSymbol.SecurityId;
                        resultSymbol.SecurityDescription = reportSymbol.SecurityDescription;
                        resultSymbol.StopOrderMarginReduction = reportSymbol.StopOrderMarginReduction;

                        resultSymbols[index] = resultSymbol;
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

                    context.Tcs.SetResult(resultSymbols);
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

                    context.Tcs.SetException(exception);
                }
            }

            public override void OnSecurityListReject(ClientSession session, SecurityListRequestClientContext SecurityListRequestClientContext, Reject message)
            {
                var context = (SymbolListAsyncContext)SecurityListRequestClientContext;

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

                    var exception = new Exception(text);
                    context.Tcs.SetException(exception);
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

                    context.Tcs.SetException(exception);
                }
            }

            public override void OnTradingSessionStatusReport(ClientSession session, TradingSessionStatusRequestClientContext TradingSessionStatusRequestClientContext, TradingSessionStatusReport message)
            {
                var context = (SessionInfoAsyncContext)TradingSessionStatusRequestClientContext;

                try
                {
                    TickTrader.FDK.Common.SessionInfo resultStatusInfo = new TickTrader.FDK.Common.SessionInfo();
                    SoftFX.Net.QuoteFeed.TradingSessionStatusInfo reportStatusInfo = message.StatusInfo;

                    resultStatusInfo.Status = Convert(reportStatusInfo.Status);
                    resultStatusInfo.StartTime = reportStatusInfo.StartTime;
                    resultStatusInfo.EndTime = reportStatusInfo.EndTime;
                    resultStatusInfo.OpenTime = reportStatusInfo.OpenTime;
                    resultStatusInfo.CloseTime = reportStatusInfo.CloseTime;

                    TradingSessionStatusGroupArray reportGroups = reportStatusInfo.Groups;
                    int count = reportGroups.Length;
                    TickTrader.FDK.Common.StatusGroupInfo[] resultGroups = new TickTrader.FDK.Common.StatusGroupInfo[count];

                    for (int index = 0; index < count; ++index)
                    {
                        TradingSessionStatusGroup reportGroup = reportGroups[index];
                        TickTrader.FDK.Common.StatusGroupInfo resultGroup = new TickTrader.FDK.Common.StatusGroupInfo();

                        resultGroup.StatusGroupId = reportGroup.Id;
                        resultGroup.Status = Convert(reportGroup.Status);
                        resultGroup.StartTime = reportGroup.StartTime;
                        resultGroup.EndTime = reportGroup.EndTime;
                        resultGroup.OpenTime = reportGroup.OpenTime;
                        resultGroup.CloseTime = reportGroup.CloseTime;

                        resultGroups[index] = resultGroup;
                    }

                    resultStatusInfo.StatusGroups = resultGroups;

                    if (client_.SessionInfoResultEvent != null)
                    {
                        try
                        {
                            client_.SessionInfoResultEvent(client_, context.Data, resultStatusInfo);
                        }
                        catch
                        {
                        }
                    }

                    context.Tcs.SetResult(resultStatusInfo);
                }
                catch (Exception exception)
                {
                    if (client_.SessionInfoErrorEvent != null)
                    {
                        try
                        {
                            client_.SessionInfoErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    context.Tcs.SetException(exception);
                }
            }

            public override void OnTradingSessionStatusReject(ClientSession session, TradingSessionStatusRequestClientContext TradingSessionStatusRequestClientContext, Reject message)
            {
                var context = (SessionInfoAsyncContext)TradingSessionStatusRequestClientContext;

                try
                {
                    string text = message.Text;

                    if (client_.SessionInfoErrorEvent != null)
                    {
                        try
                        {
                            client_.SessionInfoErrorEvent(client_, context.Data, text);
                        }
                        catch
                        {
                        }
                    }

                    var exception = new Exception(text);
                    context.Tcs.SetException(exception);
                }
                catch (Exception exception)
                {
                    if (client_.SessionInfoErrorEvent != null)
                    {
                        try
                        {
                            client_.SessionInfoErrorEvent(client_, context.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    context.Tcs.SetException(exception);
                }
            }

            public override void OnMarketDataReport(ClientSession session, MarketDataRequestClientContext MarketDataRequestClientContext, MarketDataReport message)
            {
                if (MarketDataRequestClientContext is SubscribeQuotesAsyncContext)
                {
                    // SubscribeQuotes

                    var context = (SubscribeQuotesAsyncContext) MarketDataRequestClientContext;

                    try
                    {
                        MarketDataSnapshotArray reportSnapshots = message.Snapshots;
                        int count = reportSnapshots.Length;
                        TickTrader.FDK.Common.Quote[] resultQuotes = new TickTrader.FDK.Common.Quote[count];

                        for (int index = 0; index < count; ++index)
                        {
                            MarketDataSnapshot reportSnapshot = reportSnapshots[index];

                            TickTrader.FDK.Common.Quote resultQuote = new TickTrader.FDK.Common.Quote();
                            resultQuote.Symbol = reportSnapshot.SymbolId;
                            resultQuote.Id = reportSnapshot.Id;
                            resultQuote.CreatingTime = reportSnapshot.OrigTime;

                            MarketDataEntryArray reportSnapshotEntries = reportSnapshot.Entries;
                            int count2 = reportSnapshotEntries.Length;
                            TickTrader.FDK.Common.QuoteEntry[] bidEntries = new TickTrader.FDK.Common.QuoteEntry[reportSnapshot.BidCount];
                            TickTrader.FDK.Common.QuoteEntry[] askEntries = new TickTrader.FDK.Common.QuoteEntry[reportSnapshot.AskCount];
                            
                            int bidIndex = bidEntries.Length - 1;
                            int askIndex = 0;

                            for (int index2 = 0; index2 < count2; ++ index2)
                            {
                                MarketDataEntry reportSnapshotEntry = reportSnapshotEntries[index2];
                                TickTrader.FDK.Common.QuoteEntry quoteEntry = new TickTrader.FDK.Common.QuoteEntry();

                                quoteEntry.Volume = reportSnapshotEntry.Size;
                                quoteEntry.Price = reportSnapshotEntry.Price;

                                if (reportSnapshotEntry.Type == MarketDataEntryType.Bid)
                                {
                                    bidEntries[bidIndex --] = quoteEntry;
                                }
                                else
                                    askEntries[askIndex ++] = quoteEntry;
                            }

                            resultQuote.Bids = bidEntries;
                            resultQuote.Asks = askEntries;

                            resultQuotes[index] = resultQuote;
                        }

                        if (client_.SubscribeQuotesResultEvent != null)
                        {
                            try
                            {
                                client_.SubscribeQuotesResultEvent(client_, context.Data);
                            }
                            catch
                            {
                            }
                        }

                        if (client_.QuotesBeginEvent != null)
                        {
                            try
                            {
                                client_.QuotesBeginEvent(client_, resultQuotes);
                            }
                            catch
                            {
                            }
                        }

                        context.Tcs.SetResult(null);
                    }
                    catch (Exception exception)
                    {
                        if (client_.SubscribeQuotesErrorEvent != null)
                        {
                            try
                            {
                                client_.SubscribeQuotesErrorEvent(client_, context.Data, exception.Message);
                            }
                            catch
                            {
                            }
                        }

                        context.Tcs.SetException(exception);
                    }
                }
                else if (MarketDataRequestClientContext is UnsubscribeQuotesAsyncContext)
                {
                    // UnsubscribeQuotes

                    var context = (UnsubscribeQuotesAsyncContext) MarketDataRequestClientContext;

                    try
                    {
                        if (client_.QuotesEndEvent != null)
                        {
                            try
                            {
                                client_.QuotesEndEvent(client_, context.SymbolIds);
                            }
                            catch
                            {
                            }
                        }

                        if (client_.UnsubscribeQuotesResultEvent != null)
                        {
                            try
                            {
                                client_.UnsubscribeQuotesResultEvent(client_, context.Data);
                            }
                            catch
                            {
                            }
                        }

                        context.Tcs.SetResult(null);
                    }
                    catch (Exception exception)
                    {
                        if (client_.UnsubscribeQuotesErrorEvent != null)
                        {
                            try
                            {
                                client_.UnsubscribeQuotesErrorEvent(client_, context.Data, exception.Message);
                            }
                            catch
                            {
                            }
                        }

                        context.Tcs.SetException(exception);
                    }
                }
                else
                {
                    // GetQuotes

                    var context = (GetQuotesAsyncContext) MarketDataRequestClientContext;

                    try
                    {
                        MarketDataSnapshotArray reportSnapshots = message.Snapshots;
                        int count = reportSnapshots.Length;
                        TickTrader.FDK.Common.Quote[] resultQuotes = new TickTrader.FDK.Common.Quote[count];

                        for (int index = 0; index < count; ++ index)
                        {
                            MarketDataSnapshot reportSnapshot = reportSnapshots[index];

                            TickTrader.FDK.Common.Quote resultQuote = new TickTrader.FDK.Common.Quote();
                            resultQuote.Symbol = reportSnapshot.SymbolId;
                            resultQuote.Id = reportSnapshot.Id;
                            resultQuote.CreatingTime = reportSnapshot.OrigTime;

                            MarketDataEntryArray reportSnapshotEntries = reportSnapshot.Entries;
                            int count2 = reportSnapshotEntries.Length;
                            int bidCount = reportSnapshot.BidCount;
                            int askCount = reportSnapshot.AskCount;
                            TickTrader.FDK.Common.QuoteEntry[] bidEntries = new TickTrader.FDK.Common.QuoteEntry[bidCount];
                            TickTrader.FDK.Common.QuoteEntry[] askEntries = new TickTrader.FDK.Common.QuoteEntry[askCount];

                            int bidIndex = bidEntries.Length - 1;
                            int askIndex = 0;

                            for (int index2 = 0; index2 < count2; ++ index2)
                            {
                                MarketDataEntry reportSnapshotEntry = reportSnapshotEntries[index2];
                                TickTrader.FDK.Common.QuoteEntry quoteEntry = new TickTrader.FDK.Common.QuoteEntry();

                                quoteEntry.Volume = reportSnapshotEntry.Size;
                                quoteEntry.Price = reportSnapshotEntry.Price;

                                if (reportSnapshotEntry.Type == MarketDataEntryType.Bid)
                                {
                                    bidEntries[bidIndex --] = quoteEntry;
                                }
                                else
                                    askEntries[askIndex ++] = quoteEntry;
                            }

                            resultQuote.Bids = bidEntries;
                            resultQuote.Asks = askEntries;

                            resultQuotes[index] = resultQuote;
                        }

                        if (client_.QuotesResultEvent != null)
                        {
                            try
                            {
                                client_.QuotesResultEvent(client_, context.Data, resultQuotes);
                            }
                            catch
                            {
                            }
                        }

                        context.Tcs.SetResult(resultQuotes);
                    }
                    catch (Exception exception)
                    {
                        if (client_.QuotesErrorEvent != null)
                        {
                            try
                            {
                                client_.QuotesErrorEvent(client_, context.Data, exception.Message);
                            }
                            catch
                            {
                            }
                        }

                        context.Tcs.SetException(exception);
                    }
                }
            }

            public override void OnMarketDataReject(ClientSession session, MarketDataRequestClientContext MarketDataRequestClientContext, Reject message)
            {
                var context = (IAsyncContext) MarketDataRequestClientContext;

                try
                {
                    string text = message.Text;

                    if (client_.QuotesErrorEvent != null)
                    {
                        try
                        {
                            client_.QuotesErrorEvent(client_, MarketDataRequestClientContext.Data, text);
                        }
                        catch
                        {
                        }
                    }

                    var exception = new Exception(text);
                    context.SetException(exception);
                }
                catch (Exception exception)
                {
                    if (client_.QuotesErrorEvent != null)
                    {
                        try
                        {
                            client_.QuotesErrorEvent(client_, MarketDataRequestClientContext.Data, exception.Message);
                        }
                        catch
                        {
                        }
                    }

                    context.SetException(exception);
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

            public override void OnTradingSessionStatusUpdate(ClientSession session, TradingSessionStatusUpdate message)
            {
                try
                {
                    TickTrader.FDK.Common.SessionInfo resultStatusInfo = new TickTrader.FDK.Common.SessionInfo();
                    SoftFX.Net.QuoteFeed.TradingSessionStatusInfo reportStatusInfo = message.StatusInfo;

                    resultStatusInfo.Status = Convert(reportStatusInfo.Status);
                    resultStatusInfo.StartTime = reportStatusInfo.StartTime;
                    resultStatusInfo.EndTime = reportStatusInfo.EndTime;
                    resultStatusInfo.OpenTime = reportStatusInfo.OpenTime;
                    resultStatusInfo.CloseTime = reportStatusInfo.CloseTime;

                    TradingSessionStatusGroupArray reportGroups = reportStatusInfo.Groups;
                    int count = reportGroups.Length;
                    TickTrader.FDK.Common.StatusGroupInfo[] resultGroups = new TickTrader.FDK.Common.StatusGroupInfo[count];

                    for (int index = 0; index < count; ++index)
                    {
                        TradingSessionStatusGroup reportGroup = reportGroups[index];
                        TickTrader.FDK.Common.StatusGroupInfo resultGroup = new TickTrader.FDK.Common.StatusGroupInfo();

                        resultGroup.StatusGroupId = reportGroup.Id;
                        resultGroup.Status = Convert(reportGroup.Status);
                        resultGroup.StartTime = reportGroup.StartTime;
                        resultGroup.EndTime = reportGroup.EndTime;
                        resultGroup.OpenTime = reportGroup.OpenTime;
                        resultGroup.CloseTime = reportGroup.CloseTime;

                        resultGroups[index] = resultGroup;
                    }

                    resultStatusInfo.StatusGroups = resultGroups;

                    if (client_.SessionInfoUpdateEvent != null)
                    {
                        try
                        {
                            client_.SessionInfoUpdateEvent(client_, resultStatusInfo);
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

            public override void OnMarketDataRefresh(ClientSession session, MarketDataSnapshotRefresh message)
            {
                try
                {
                    MarketDataSnapshot snapshot = message.Snapshot;

                    // TODO: optimize
                    TickTrader.FDK.Common.Quote quote = new TickTrader.FDK.Common.Quote();
                    quote.Symbol = snapshot.SymbolId;
                    quote.Id = snapshot.Id;
                    quote.CreatingTime = snapshot.OrigTime;

                    MarketDataEntryArray snapshotEntries = snapshot.Entries;
                    int count = snapshotEntries.Length;
                    TickTrader.FDK.Common.QuoteEntry[] bidEntries = new TickTrader.FDK.Common.QuoteEntry[snapshot.BidCount];
                    TickTrader.FDK.Common.QuoteEntry[] askEntries = new TickTrader.FDK.Common.QuoteEntry[snapshot.AskCount];
                            
                    int bidIndex = bidEntries.Length - 1;
                    int askIndex = 0;

                    for (int index = 0; index < count; ++ index)
                    {
                        MarketDataEntry snapshotEntry = snapshotEntries[index];
                        // TODO: optimize
                        TickTrader.FDK.Common.QuoteEntry quoteEntry = new TickTrader.FDK.Common.QuoteEntry();

                        quoteEntry.Volume = snapshotEntry.Size;
                        quoteEntry.Price = snapshotEntry.Price;

                        if (snapshotEntry.Type == MarketDataEntryType.Bid)
                        {
                            bidEntries[bidIndex --] = quoteEntry;
                        }
                        else
                            askEntries[askIndex ++] = quoteEntry;
                    }

                    quote.Bids = bidEntries;
                    quote.Asks = askEntries;

                    if (client_.QuoteUpdateEvent != null)
                    {
                        try
                        {
                            client_.QuoteUpdateEvent(client_, quote);
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

            public override void OnNotification(ClientSession session, SoftFX.Net.QuoteFeed.Notification message)
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

            TickTrader.FDK.Common.LogoutReason Convert(SoftFX.Net.QuoteFeed.LogoutReason reason)
            {
                switch (reason)
                {
                    case SoftFX.Net.QuoteFeed.LogoutReason.ClientLogout:
                        return TickTrader.FDK.Common.LogoutReason.ClientInitiated;

                    case SoftFX.Net.QuoteFeed.LogoutReason.ServerLogout:
                        return TickTrader.FDK.Common.LogoutReason.ServerLogout;

                    case SoftFX.Net.QuoteFeed.LogoutReason.SlowConnection:
                        return TickTrader.FDK.Common.LogoutReason.SlowConnection;

                    case SoftFX.Net.QuoteFeed.LogoutReason.DeletedLogin:
                        return TickTrader.FDK.Common.LogoutReason.LoginDeleted;

                    case SoftFX.Net.QuoteFeed.LogoutReason.InternalServerError:
                        return TickTrader.FDK.Common.LogoutReason.ServerError;

                    case SoftFX.Net.QuoteFeed.LogoutReason.BlockedLogin:
                        return TickTrader.FDK.Common.LogoutReason.BlockedAccount;

                    default:
                        throw new Exception("Invalid logout reason : " + reason);
                }
            }

            TickTrader.FDK.Common.MarginCalcMode Convert(SoftFX.Net.QuoteFeed.CalcMode mode)
            {
                switch (mode)
                {
                    case SoftFX.Net.QuoteFeed.CalcMode.Forex:
                        return TickTrader.FDK.Common.MarginCalcMode.Forex;

                    case SoftFX.Net.QuoteFeed.CalcMode.Cfd:
                        return TickTrader.FDK.Common.MarginCalcMode.Cfd;

                    case SoftFX.Net.QuoteFeed.CalcMode.Futures:
                        return TickTrader.FDK.Common.MarginCalcMode.Futures;

                    case SoftFX.Net.QuoteFeed.CalcMode.CfdIndex:
                        return TickTrader.FDK.Common.MarginCalcMode.CfdIndex;

                    case SoftFX.Net.QuoteFeed.CalcMode.CfdLeverage:
                        return TickTrader.FDK.Common.MarginCalcMode.CfdLeverage;

                    default:
                        throw new Exception("Invalid calculation mode : " + mode);
                }
            }

            TickTrader.FDK.Common.ProfitCalcMode Convert(SoftFX.Net.QuoteFeed.SettlCalcMode mode)
            {
                switch (mode)
                {
                    case SoftFX.Net.QuoteFeed.SettlCalcMode.Forex:
                        return TickTrader.FDK.Common.ProfitCalcMode.Forex;

                    case SoftFX.Net.QuoteFeed.SettlCalcMode.Cfd:
                        return TickTrader.FDK.Common.ProfitCalcMode.Cfd;

                    case SoftFX.Net.QuoteFeed.SettlCalcMode.Futures:
                        return TickTrader.FDK.Common.ProfitCalcMode.Futures;

                    case SoftFX.Net.QuoteFeed.SettlCalcMode.CfdIndex:
                        return TickTrader.FDK.Common.ProfitCalcMode.CfdIndex;

                    case SoftFX.Net.QuoteFeed.SettlCalcMode.CfdLeverage:
                        return TickTrader.FDK.Common.ProfitCalcMode.CfdLeverage;

                    default:
                        throw new Exception("Invalid calculation mode : " + mode);
                }
            }

            TickTrader.FDK.Common.CommissionType Convert(SoftFX.Net.QuoteFeed.CommissionType type)
            {
                switch (type)
                {
                    case SoftFX.Net.QuoteFeed.CommissionType.Money:
                        return TickTrader.FDK.Common.CommissionType.Absolute;

                    case SoftFX.Net.QuoteFeed.CommissionType.Points:
                        return TickTrader.FDK.Common.CommissionType.PerUnit;

                    case SoftFX.Net.QuoteFeed.CommissionType.Percentage:
                        return TickTrader.FDK.Common.CommissionType.Percent;

                    default:
                        throw new Exception("Invalid commission type : " + type);
                }
            }

            TickTrader.FDK.Common.CommissionChargeType Convert(SoftFX.Net.QuoteFeed.CommissionChargeType type)
            {
                switch (type)
                {
                    case SoftFX.Net.QuoteFeed.CommissionChargeType.PerLot:
                        return TickTrader.FDK.Common.CommissionChargeType.PerLot;

                    case SoftFX.Net.QuoteFeed.CommissionChargeType.PerDeal:
                        return TickTrader.FDK.Common.CommissionChargeType.PerTrade;

                    default:
                        throw new Exception("Invalid commission charge type : " + type);
                }
            }

            TickTrader.FDK.Common.CommissionChargeMethod Convert(SoftFX.Net.QuoteFeed.CommissionChargeMethod method)
            {
                switch (method)
                {
                    case SoftFX.Net.QuoteFeed.CommissionChargeMethod.OneWay:
                        return TickTrader.FDK.Common.CommissionChargeMethod.OneWay;

                    case SoftFX.Net.QuoteFeed.CommissionChargeMethod.RoundTurn:
                        return TickTrader.FDK.Common.CommissionChargeMethod.RoundTurn;

                    default:
                        throw new Exception("Invalid commission charge method : " + method);
                }
            }

            TickTrader.FDK.Common.SessionStatus Convert(SoftFX.Net.QuoteFeed.TradingSessionStatus status)
            {
                switch (status)
                {
                    case SoftFX.Net.QuoteFeed.TradingSessionStatus.Open:
                        return TickTrader.FDK.Common.SessionStatus.Open;

                    case SoftFX.Net.QuoteFeed.TradingSessionStatus.Close:
                        return TickTrader.FDK.Common.SessionStatus.Closed;

                    default:
                        throw new Exception("Invalid trading session status : " + status);
                }
            }

            TickTrader.FDK.Common.NotificationType Convert(SoftFX.Net.QuoteFeed.NotificationType type)
            {
                switch (type)
                {
                    case SoftFX.Net.QuoteFeed.NotificationType.ConfigUpdate:
                        return TickTrader.FDK.Common.NotificationType.ConfigUpdated;

                    default:
                        throw new Exception("Invalid notification type : " + type);
                }
            }

            TickTrader.FDK.Common.NotificationSeverity Convert(SoftFX.Net.QuoteFeed.NotificationSeverity severity)
            {
                switch (severity)
                {
                    case SoftFX.Net.QuoteFeed.NotificationSeverity.Info:
                        return TickTrader.FDK.Common.NotificationSeverity.Information;

                    case SoftFX.Net.QuoteFeed.NotificationSeverity.Warning:
                        return TickTrader.FDK.Common.NotificationSeverity.Warning;

                    case SoftFX.Net.QuoteFeed.NotificationSeverity.Error:
                        return TickTrader.FDK.Common.NotificationSeverity.Error;

                    default:
                        throw new Exception("Invalid notification severity : " + severity);
                }
            }

            Client client_;
        }

        #endregion

        #region Async helpers        

        private static void ConvertToSync(Task task, int timeout)
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

        private static TResult ConvertToSync<TResult>(Task<TResult> task, int timeout)
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
