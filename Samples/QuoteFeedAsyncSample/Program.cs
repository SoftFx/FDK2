using System;
using System.Collections.Generic;
using System.Diagnostics;
using NDesk.Options;
using SoftFX.Net.Core;
using TickTrader.FDK.Common;
using TickTrader.FDK.Client;
using System.Linq;

namespace QuoteFeedAsyncSample
{
    public class Program : IDisposable
    {
        static string SampleName = typeof(Program).Namespace;

        static void Main(string[] args)
        {
            try
            {
                string address = null;
                int port = 5041;
                string login = null;
                string password = null;
                bool help = false;

#if DEBUG
                address = "localhost";
                login = "5";
                password = "123qwe!";
#endif

                var options = new OptionSet()
                {
                    { "a|address=",  v => address = v },
                    { "p|port=",     v => port = int.Parse(v) },
                    { "l|login=",    v => login = v },
                    { "w|password=", v => password = v },
                    { "h|?|help",    v => help = v != null },
                };

                try
                {
                    options.Parse(args);
                    help = string.IsNullOrEmpty(address) || string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password);
                }
                catch (OptionException e)
                {
                    Console.Write($"{SampleName}: ");
                    Console.WriteLine(e.Message);
                    Console.WriteLine($"Try '{SampleName} --help' for more information.");
                    return;
                }

                if (help)
                {
                    Console.WriteLine($"{SampleName} usage:");
                    options.WriteOptionDescriptions(Console.Out);
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                    return;
                }

                using (Program program = new Program(address, port, login, password))
                {
                    program.Run();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        public Program(string address, int port, string login, string password)
        {
            client_ = new QuoteFeed(SampleName, port: port, logMessages: true,
                validateClientCertificate: (sender, certificate, chain, errors) => true);

            client_.ConnectResultEvent += new QuoteFeed.ConnectResultDelegate(this.OnConnectResult);
            client_.ConnectErrorEvent += new QuoteFeed.ConnectErrorDelegate(this.OnConnectError);
            client_.DisconnectResultEvent += new QuoteFeed.DisconnectResultDelegate(this.OnDisconnectResult);
            client_.DisconnectEvent += new QuoteFeed.DisconnectDelegate(this.OnDisconnect);
            client_.ReconnectEvent += new QuoteFeed.ReconnectDelegate(this.OnReconnect);
            client_.ReconnectErrorEvent += new QuoteFeed.ReconnectErrorDelegate(this.OnReconnectError);
            client_.LoginResultEvent += new QuoteFeed.LoginResultDelegate(this.OnLoginResult);
            client_.LoginErrorEvent += new QuoteFeed.LoginErrorDelegate(this.OnLoginError);
            client_.LogoutResultEvent += new QuoteFeed.LogoutResultDelegate(this.OnLogoutResult);
            client_.LogoutErrorEvent += new QuoteFeed.LogoutErrorDelegate(this.OnLogoutError);
            client_.LogoutEvent += new QuoteFeed.LogoutDelegate(this.OnLogout);
            client_.CurrencyTypeListResultEvent += new QuoteFeed.CurrencyTypeListResultDelegate(this.OnCurrencyTypeListResult);
            client_.CurrencyTypeListErrorEvent += new QuoteFeed.CurrencyTypeListErrorDelegate(this.OnCurrencyTypeListError);
            client_.CurrencyListResultEvent += new QuoteFeed.CurrencyListResultDelegate(this.OnCurrencyListResult);
            client_.CurrencyListErrorEvent += new QuoteFeed.CurrencyListErrorDelegate(this.OnCurrencyListError);
            client_.SymbolListResultEvent += new QuoteFeed.SymbolListResultDelegate(this.OnSymbolListResult);
            client_.SymbolListErrorEvent += new QuoteFeed.SymbolListErrorDelegate(this.OnSymbolListError);
            client_.SessionInfoResultEvent += new QuoteFeed.SessionInfoResultDelegate(this.OnSessionInfoResult);
            client_.SessionInfoErrorEvent += new QuoteFeed.SessionInfoErrorDelegate(this.OnSessionInfoError);
            client_.SubscribeQuotesExtendedResultEvent += new QuoteFeed.SubscribeQuotesExtendedResultDelegate(this.OnSubscribeQuotesResult);
            client_.SubscribeQuotesErrorEvent += new QuoteFeed.SubscribeQuotesErrorDelegate(this.OnSubscribeQuotesError);
            client_.UnsubscribeQuotesResultEvent += new QuoteFeed.UnsubscribeQuotesResultDelegate(this.OnUnsubscribeQuotesResult);
            client_.UnsubscribeQuotesErrorEvent += new QuoteFeed.UnsubscribeQuotesErrorDelegate(this.OnUnsubscribeQuotesError);
            client_.QuotesResultEvent += new QuoteFeed.QuotesResultDelegate(this.OnQuotesResult);
            client_.QuotesErrorEvent += new QuoteFeed.QuotesErrorDelegate(this.OnQuotesError);
            client_.SessionInfoUpdateEvent += new QuoteFeed.SessionInfoUpdateDelegate(this.OnSessionInfoUpdate);
            client_.QuoteUpdateEvent += new QuoteFeed.QuoteUpdateDelegate(this.OnQuoteUpdate);
            client_.BarsUpdateEvent += new QuoteFeed.BarsUpdateDelegate(OnBarUpdate);
            client_.SubscribeBarsResultEvent += new QuoteFeed.SubscribeBarsResultDelegate(OnBarSubscribed);
            client_.SubscribeBarsErrorEvent += new QuoteFeed.SubscribeBarsErrorDelegate(OnQuotesError);
            client_.UnsubscribeBarsResultEvent += new QuoteFeed.UnsubscribeBarsResultDelegate(this.OnUnsubscribeQuotesResult);
            client_.UnsubscribeBarsErrorEvent += new QuoteFeed.UnsubscribeBarsErrorDelegate(this.OnUnsubscribeQuotesError);

            address_ = address;
            login_ = login;
            password_ = password;
        }

        public void Dispose()
        {
            client_.Dispose();

            GC.SuppressFinalize(this);
        }

        string GetNextWord(string line, ref int index)
        {
            while (index < line.Length && line[index] == ' ')
                ++index;

            if (index == line.Length)
                return null;

            int startIndex = index;

            while (index < line.Length && line[index] != ' ')
                ++index;

            return line.Substring(startIndex, index - startIndex);
        }

        public void Run()
        {
            PrintCommands();

            Connect();

            try
            {
                while (true)
                {
                    try
                    {
                        string line = Console.ReadLine();

                        int pos = 0;
                        string command = GetNextWord(line, ref pos);

                        if (command == "help" || command == "h")
                        {
                            PrintCommands();
                        }
                        else if (command == "i")
                        {
                            GetSessionInfo();
                        }
                        else if (command == "ct")
                        {
                            GetCurrencyTypeList();
                        }
                        else if (command == "c")
                        {
                            GetCurrencyList();
                        }
                        else if (command == "s")
                        {
                            GetSymbolList();
                        }
                        else if (command == "sq")
                        {

                            while (true)
                            {
                                string symbolId = GetNextWord(line, ref pos);

                                if (symbolId == null)
                                    break;

                                if (!_subscribedSymbols.ContainsKey(symbolId))
                                {
                                    SymbolEntry symbolEntry = new SymbolEntry();
                                    symbolEntry.Id = symbolId;
                                    symbolEntry.MarketDepth = 1;

                                    _toSubscribe.Add(symbolId, symbolEntry);
                                }
                            }

                            SubscribeQuotes(_toSubscribe.Values.ToList(), -1);
                        }
                        else if (command == "sqf")
                        {
                            var freqPrior = int.Parse(GetNextWord(line, ref pos));
                            while (true)
                            {
                                string symbolId = GetNextWord(line, ref pos);

                                if (symbolId == null)
                                    break;

                                if (!_subscribedSymbols.ContainsKey(symbolId))
                                {
                                    SymbolEntry symbolEntry = new SymbolEntry();
                                    symbolEntry.Id = symbolId;
                                    symbolEntry.MarketDepth = 1;

                                    _toSubscribe.Add(symbolId, symbolEntry);
                                }
                            }

                            SubscribeQuotes(_toSubscribe.Values.ToList(), freqPrior);
                        }
                        else if (command == "sb")
                        {
                            client_.SubscribeBarsAsync(null, ParseBarSubscriptionCommand(line, pos).ToArray());
                        }
                        else if (command == "sqa")
                        {
                            if (!_symbols.Any())
                            {
                                Console.WriteLine("Get symbols first");
                            }
                            else
                            {
                                List<SymbolEntry> toSubscribe = new List<SymbolEntry>();

                                foreach (var s in _symbols.Values)
                                {
                                    if (!_subscribedSymbols.ContainsKey(s.Name))
                                    {
                                        SymbolEntry symbolEntry = new SymbolEntry();
                                        symbolEntry.Id = s.Name;
                                        symbolEntry.MarketDepth = 1;

                                        toSubscribe.Add(symbolEntry);
                                    }
                                }
                                SubscribeQuotes(toSubscribe, -1);

                                foreach (var s in toSubscribe)
                                    _subscribedSymbols.Add(s.Id, s);
                            }
                        }
                        else if (command == "uq")
                        {
                            List<string> toUnsubscribe = new List<string>();

                            while (true)
                            {
                                string symbolId = GetNextWord(line, ref pos);

                                if (symbolId == null)
                                    break;

                                toUnsubscribe.Add(symbolId);
                            }

                            UnsubscribeQuotes(toUnsubscribe);

                            foreach (var s in toUnsubscribe)
                                _subscribedSymbols.Remove(s);
                        }
                        else if (command == "ub")
                        {
                            List<string> toUnsubscribe = new List<string>();

                            while (true)
                            {
                                string symbolId = GetNextWord(line, ref pos);

                                if (symbolId == null)
                                    break;

                                toUnsubscribe.Add(symbolId);
                            }

                            client_.UnsubscribeBarsAsync(null, toUnsubscribe.ToArray());

                            foreach (var s in toUnsubscribe)
                                _barSubscribedSymbols.Remove(s);
                        }
                        else if (command == "uba")
                        {
                            client_.UnsubscribeBarsAsync(null, _barSubscribedSymbols.ToArray());
                            _barSubscribedSymbols.Clear();
                        }
                        else if (command == "uqa")
                        {
                            List<string> toUnsubscribe = _subscribedSymbols.Values.Select(s => s.Id).ToList();
                            UnsubscribeQuotes(toUnsubscribe);
                            _subscribedSymbols.Clear();
                        }
                        else if (command == "gq")
                        {
                            List<SymbolEntry> symbolEnries = new List<SymbolEntry>();

                            while (true)
                            {
                                string symbolId = GetNextWord(line, ref pos);

                                if (symbolId == null)
                                    break;

                                SymbolEntry symbolEntry = new SymbolEntry();
                                symbolEntry.Id = symbolId;
                                symbolEntry.MarketDepth = 5;

                                symbolEnries.Add(symbolEntry);
                            }

                            GetQuotes(symbolEnries);
                        }
                        else if (command == "e")
                        {
                            break;
                        }
                        else
                            throw new Exception(string.Format("Invalid command: {0}", command));
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine("Error: " + exception.Message);
                    }
                }
            }
            finally
            {
                Disconnect();
            }
        }

        void Connect()
        {
            client_.ConnectAsync(null, address_);
        }

        void Disconnect()
        {
            try
            {
                client_.LogoutAsync(null, "Client logout");
            }
            catch
            {
                client_.DisconnectAsync(null, Reason.ClientError("Client disconnect"));
            }

            client_.Join();
        }

        void OnConnectResult(QuoteFeed client, object data)
        {
            try
            {
                Console.WriteLine($"Connected to {address_}");

                client_.LoginAsync(null, login_, password_, "31DBAF09-94E1-4B2D-8ACF-5E6167E0D2D2", SampleName, "");
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error: " + exception.Message);

                client_.DisconnectAsync(null, Reason.ClientError("Client disconnect"));
            }
        }

        void OnConnectError(QuoteFeed client, object data, Exception error)
        {
            try
            {
                Console.WriteLine("Error: " + error.Message);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error: " + exception.Message);
            }
        }

        void OnDisconnectResult(QuoteFeed client, object data, string text)
        {
            try
            {
                Console.WriteLine("Disconnected: " + text);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error: " + exception.Message);
            }
        }

        void OnDisconnect(QuoteFeed client, string text)
        {
            try
            {
                Console.WriteLine("Disconnected: " + text);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error: " + exception.Message);
            }
        }

        void OnReconnect(QuoteFeed client)
        {
            try
            {
                Console.WriteLine("Connected");

                client_.LoginAsync(this, login_, password_, "", "", "");
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error: " + exception.Message);

                client_.DisconnectAsync(null, Reason.ClientError("Client disconnect"));
            }
        }

        void OnReconnectError(QuoteFeed client, Exception error)
        {
            try
            {
                Console.WriteLine("Error: " + error.Message);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error: " + exception.Message);
            }
        }

        void OnLoginResult(QuoteFeed client, object data)
        {
            try
            {
                Console.WriteLine($"{login_}: Login succeeded");
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error: " + exception.Message);
            }
        }

        void OnLoginError(QuoteFeed client, object data, Exception error)
        {
            try
            {
                Console.WriteLine("Error: " + error.Message);

                client_.DisconnectAsync(null, Reason.ClientError(error.Message));
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error: " + exception.Message);
            }
        }

        void OnLogoutResult(QuoteFeed client, object data, LogoutInfo info)
        {
            try
            {
                Console.WriteLine("Logout: " + info.Message);

                client_.DisconnectAsync(null, Reason.ClientRequest("Client logout"));
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error: " + exception.Message);
            }
        }

        void OnLogoutError(QuoteFeed client, object data, Exception error)
        {
            try
            {
                Console.WriteLine("Error: " + error.Message);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error: " + exception.Message);
            }
        }

        public void OnLogout(QuoteFeed client, LogoutInfo info)
        {
            try
            {
                Console.WriteLine("Logout: " + info.Message);

                client_.DisconnectAsync(null, Reason.ClientRequest("Client logout"));
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error: " + exception.Message);
            }
        }

        void PrintCommands()
        {
            Console.WriteLine("help (h) - print commands");
            Console.WriteLine("i - request session info");
            Console.WriteLine("ct - request currency type list");
            Console.WriteLine("c - request currency list");
            Console.WriteLine("s - request symbol list");
            Console.WriteLine("gq <symbol_1> ... <symbol_n> - request quote snapshots");
            Console.WriteLine("sq <symbol_1> ... <symbol_n> - subscribe to quote updates");
            Console.WriteLine("uq <symbol_1> ... <symbol_n> - unsubscribe from quote updates");
            Console.WriteLine("sqa <depth> - subscribe to quote updates with depth for all symbols");
            Console.WriteLine("sqf <freq> <symbol_1> ... <symbol_n> - subscribe to quote updates with selected frequency priority");
            Console.WriteLine("uqa - unsubscribe from all quote updates");
            Console.WriteLine("sb <symbol_1> <m1> <periodicity_1> <pricetype_1> ... <periodicity_m1> <pricetype_m1> ... <symbol_n> <mn> <periodicity_1> <pricetype_1> ... <periodicity_mn> <pricetype_mn> - subscribe to bar updates");
            Console.WriteLine("ub <symbol_1> ... <symbol_n> - unsubscribe from bar updates");
            Console.WriteLine("uba - unsubscribe from all bar updates");
            Console.WriteLine("e - exit");
        }

        void GetCurrencyTypeList()
        {
            client_.GetCurrencyTypeListAsync(null);
        }

        void GetCurrencyList()
        {
            client_.GetCurrencyListAsync(null);
        }

        void OnCurrencyTypeListResult(QuoteFeed client, object data, CurrencyTypeInfo[] currencyTypes)
        {
            try
            {
                int count = currencyTypes.Length;
                for (int index = 0; index < count; ++index)
                {
                    CurrencyTypeInfo currencyType = currencyTypes[index];

                    Console.Error.WriteLine("CurrencyType: {0}, {1}", currencyType.Name, currencyType.Description);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error: " + exception.Message);
            }
        }

        void OnCurrencyTypeListError(QuoteFeed client, object data, Exception error)
        {
            try
            {
                Console.WriteLine("Error: " + error.Message);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error: " + exception.Message);
            }
        }

        void OnCurrencyListResult(QuoteFeed client, object data, CurrencyInfo[] currencies)
        {
            try
            {
                int count = currencies.Length;
                for (int index = 0; index < count; ++index)
                {
                    CurrencyInfo currency = currencies[index];

                    Console.Error.WriteLine($"Currency: {currency.Name}, {currency.Description}, Precision={currency.Precision}, Tax={currency.Tax}, Type={currency.TypeId}");
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error: " + exception.Message);
            }
        }

        void OnCurrencyListError(QuoteFeed client, object data, Exception error)
        {
            try
            {
                Console.WriteLine("Error: " + error.Message);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error: " + exception.Message);
            }
        }

        void GetSymbolList()
        {
            client_.GetSymbolListAsync(null);
        }

        void OnSymbolListResult(QuoteFeed client, object data, SymbolInfo[] symbols)
        {
            try
            {
                _symbols.Clear();
                int count = symbols.Length;
                for (int index = 0; index < count; ++index)
                {
                    SymbolInfo symbol = symbols[index];
                    _symbols[symbol.Name] = symbol;

                    Console.Error.WriteLine("Symbol: {0}, {1}, {2}", symbol.Name, symbol.ExtendedName, symbol.Description);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error: " + exception.Message);
            }
        }

        void OnSymbolListError(QuoteFeed client, object data, Exception error)
        {
            try
            {
                Console.WriteLine("Error: " + error.Message);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error: " + exception.Message);
            }
        }

        void GetSessionInfo()
        {
            client_.GetSessionInfoAsync(null);
        }

        void OnSessionInfoResult(QuoteFeed client, object data, SessionInfo sessionInfo)
        {
            try
            {
                Console.Error.WriteLine("Session info: {0}, {1}-{2}, {3}", sessionInfo.Status, sessionInfo.StartTime, sessionInfo.EndTime, sessionInfo.ServerTimeZoneOffset);

                StatusGroupInfo[] groups = sessionInfo.StatusGroups;

                int count = groups.Length;
                for (int index = 0; index < count; ++index)
                {
                    StatusGroupInfo group = groups[index];

                    Console.Error.WriteLine("Session status group: {0}, {1}, {2}-{3}", group.StatusGroupId, group.Status, group.StartTime, group.EndTime);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error: " + exception.Message);
            }
        }

        void OnSessionInfoError(QuoteFeed client, object data, Exception error)
        {
            try
            {
                Console.WriteLine("Error: " + error.Message);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error: " + exception.Message);
            }
        }

        void SubscribeQuotes(List<SymbolEntry> symbolEntries, int frequencyPriority)
        {
            client_.SubscribeQuotesAsync(null, symbolEntries.ToArray(), frequencyPriority);
        }

        void OnSubscribeQuotesResult(QuoteFeed client, object data, Quote[] quotes, SubscriptionErrors errors)
        {
            try
            {
                Console.Error.WriteLine("Subscribed:");
                for (int index = 0; index < quotes.Length; ++index)
                {
                    Quote quote = quotes[index];
                    Console.Error.WriteLine(quote.ToString());
                    if (_toSubscribe.TryGetValue(quote.Symbol, out var symbolEntry))
                        _subscribedSymbols.Add(quote.Symbol, symbolEntry);
                    else
                        _subscribedSymbols.Add(quote.Symbol, new SymbolEntry { Id = quote.Symbol, MarketDepth = 1 });
                }
                if (!errors.IsEmpty)
                {
                    Console.WriteLine("Errors:");
                    Console.WriteLine(string.Join("\n", errors.Errors.Select(it => $"{it.Key}: {string.Join(", ", it.Value)}")));
                }
                _toSubscribe.Clear();
                Console.WriteLine();
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error: " + exception.Message);
            }
        }

        void OnSubscribeQuotesError(QuoteFeed client, object data, Exception error)
        {
            try
            {
                Console.WriteLine("Error: " + error.Message);
                _toSubscribe.Clear();
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error: " + exception.Message);
            }
        }

        void UnsubscribeQuotes(List<string> symbolIds)
        {
            client_.UnsubscribeQuotesAsync(null, symbolIds.ToArray());
        }

        void OnUnsubscribeQuotesResult(QuoteFeed client, object data, string[] symbolIds)
        {
            for (int index = 0; index < symbolIds.Length; ++index)
            {
                string symbolId = symbolIds[index];

                Console.Error.WriteLine("Unsubscribed {0}", symbolId);
            }
        }

        void OnUnsubscribeQuotesError(QuoteFeed client, object data, Exception error)
        {
            try
            {
                Console.WriteLine("Error: " + error.Message);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error: " + exception.Message);
            }
        }

        void GetQuotes(List<SymbolEntry> symbolEntries)
        {
            client_.GetQuotesAsync(null, symbolEntries.ToArray());
        }

        void OnQuotesResult(QuoteFeed client, object data, Quote[] quotes)
        {
            try
            {
                Console.Error.WriteLine("Snapshot:");
                int count = quotes.Length;
                for (int index = 0; index < count; ++index)
                {
                    Quote quote = quotes[index];
                    Console.Error.WriteLine(quote.ToString());
                }
                Console.WriteLine();
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error: " + exception.Message);
            }
        }

        void OnQuotesError(QuoteFeed client, object data, Exception error)
        {
            try
            {
                Console.WriteLine("Error: " + error.Message);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error: " + exception.Message);
            }
        }

        void OnSessionInfoUpdate(QuoteFeed client, SessionInfo info)
        {
            try
            {
                Console.Error.WriteLine("Session info: {0}, {1}-{2}, {3}", info.Status, info.StartTime, info.EndTime, info.ServerTimeZoneOffset);

                StatusGroupInfo[] groups = info.StatusGroups;

                int count = groups.Length;
                for (int index = 0; index < count; ++index)
                {
                    StatusGroupInfo group = groups[index];

                    Console.Error.WriteLine("Session status group: {0}, {1}, {2}-{3}", group.StatusGroupId, group.Status, group.StartTime, group.EndTime);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error: " + exception.Message);
            }
        }

        void OnQuoteUpdate(QuoteFeed client, Quote quote)
        {
            try
            {
                double delta = (DateTime.UtcNow - quote.CreatingTime).TotalSeconds;

                Console.Error.WriteLine($"{quote.Symbol}, {quote.CreatingTime.ToString("u")}, {quote.Bid}, {quote.Ask}, {quote.TickType}, {delta}");
                Console.Error.WriteLine();
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error: " + exception.Message);
            }
        }

        void OnBarUpdate(QuoteFeed quoteFeed, AggregatedBarUpdate updates)
        {
            try
            {
                Console.Error.WriteLine(updates);
                Console.Error.WriteLine();
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error: " + exception.Message);
            }
        }

        void OnBarSubscribed(QuoteFeed quoteFeed, object data, AggregatedBarUpdate[] updates)
        {
            Console.WriteLine("Subscribed:");
            foreach (var update in updates)
            {
                Console.WriteLine(update);
                _barSubscribedSymbols.Add(update.Symbol);
            }
        }

        IEnumerable<BarSubscriptionSymbolEntry> ParseBarSubscriptionCommand(string line, int pos)
        {
            while (true)
            {
                string symbolId = GetNextWord(line, ref pos);

                if (symbolId == null)
                    yield break;
                var paramCount = int.Parse(GetNextWord(line, ref pos));
                var param = new BarParameters[paramCount];
                for (int i = 0; i < paramCount; i++)
                {
                    string periodicity = GetNextWord(line, ref pos);
                    string priceType = GetNextWord(line, ref pos);
                    param[i] = new BarParameters(Periodicity.Parse(periodicity), (PriceType)Enum.Parse(typeof(PriceType), priceType));
                }
                yield return new BarSubscriptionSymbolEntry { Symbol = symbolId, Params = param };
            }
        }

        QuoteFeed client_;

        string address_;
        string login_;
        string password_;
        private Dictionary<string, SymbolEntry> _toSubscribe = new Dictionary<string, SymbolEntry>();
        private Dictionary<string, SymbolEntry> _subscribedSymbols = new Dictionary<string, SymbolEntry>();
        private Dictionary<string, SymbolInfo> _symbols = new Dictionary<string, SymbolInfo>();
        private HashSet<string> _barSubscribedSymbols = new HashSet<string>();
    }
}
