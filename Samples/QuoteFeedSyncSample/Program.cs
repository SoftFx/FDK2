using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using NDesk.Options;
using SoftFX.Net.Core;
using TickTrader.FDK.Common;
using TickTrader.FDK.Client;
using System.Linq;

namespace QuoteFeedSyncSample
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
            client_ = new QuoteFeed(SampleName, port: port, reconnectAttempts: 0, logMessages: true,
                validateClientCertificate: (sender, certificate, chain, errors) => true);

            client_.LogoutEvent += new QuoteFeed.LogoutDelegate(this.OnLogout);
            client_.DisconnectEvent += new QuoteFeed.DisconnectDelegate(this.OnDisconnect);
            client_.SessionInfoUpdateEvent += new QuoteFeed.SessionInfoUpdateDelegate(this.OnSessionInfoUpdate);
            client_.SubscribeQuotesResultEvent += new QuoteFeed.SubscribeQuotesResultDelegate(this.OnSubscribeQuotesResult);
            client_.UnsubscribeQuotesResultEvent += new QuoteFeed.UnsubscribeQuotesResultDelegate(this.OnUnsubscribeQuotesResult);
            client_.QuoteUpdateEvent += new QuoteFeed.QuoteUpdateDelegate(this.OnQuoteUpdate);
            client_.BarsUpdateEvent += new QuoteFeed.BarsUpdateDelegate(OnBarUpdate);

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
                        else if (command == "get_currency_list" || command == "c")
                        {
                            GetCurrencyList();
                        }
                        else if (command == "get_symbol_list" || command == "s")
                        {
                            GetSecurityList();
                        }
                        else if (command == "get_session_info" || command == "i")
                        {
                            GetSessionInfo();
                        }
                        else if (command == "subscribe_quotes" || command == "sq")
                        {
                            List<SymbolEntry> symbolEnries = new List<SymbolEntry>();

                            while (true)
                            {
                                string symbolId = GetNextWord(line, ref pos);

                                if (symbolId == null)
                                    break;

                                SymbolEntry symbolEntry = new SymbolEntry();
                                symbolEntry.Id = symbolId;
                                symbolEntry.MarketDepth = 500;

                                symbolEnries.Add(symbolEntry);
                            }

                            SubscribeQuotes(symbolEnries, -1);
                        }
                        else if (command == "unsubscribe_quote" || command == "uq")
                        {
                            List<string> symbolIds = new List<string>();

                            while (true)
                            {
                                string symbolId = GetNextWord(line, ref pos);

                                if (symbolId == null)
                                    break;

                                symbolIds.Add(symbolId);
                            }

                            UnsubscribeQuotes(symbolIds);
                        }
                        else if (command == "get_quotes" || command == "gq")
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
                        else if (command == "sqf")
                        {
                            var freqPrior = int.Parse(GetNextWord(line, ref pos));
                            List<SymbolEntry> symbolEnries = new List<SymbolEntry>();

                            while (true)
                            {
                                string symbolId = GetNextWord(line, ref pos);

                                if (symbolId == null)
                                    break;

                                SymbolEntry symbolEntry = new SymbolEntry();
                                symbolEntry.Id = symbolId;
                                symbolEntry.MarketDepth = 500;

                                symbolEnries.Add(symbolEntry);
                            }

                            SubscribeQuotes(symbolEnries, freqPrior);
                        }
                        else if (command == "sb")
                        {
                            try
                            {
                                var result = client_.SubscribeBars(ParseBarSubscriptionCommand(line, pos).ToArray(), Timeout);
                                foreach (var update in result)
                                {
                                    Console.WriteLine(update);
                                }
                            }
                            catch (Exception exception)
                            {
                                Console.WriteLine("Error : " + exception.Message);
                            }
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
                            try
                            {
                                client_.UnsubscribeBars(toUnsubscribe.ToArray(), Timeout);
                            }
                            catch (Exception exception)
                            {
                                Console.WriteLine("Error : " + exception.Message);
                            }
                        }
                        else if (command == "exit" || command == "e")
                        {
                            break;
                        }
                        else if (command == "relogin" || command == "rl")
                        {
                            client_.Logout("relogin", Timeout);
                            Console.WriteLine("Logout");
                            client_.Login(login_, password_, "", "", "", Timeout);
                            Console.WriteLine("Logon");
                        }
                        else
                            throw new Exception(string.Format("Invalid command : {0}", command));
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine("Error : " + exception.Message);
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
            client_.Connect(address_, Timeout);

            try
            {
                Console.WriteLine($"Connected to {address_}");

                client_.Login(login_, password_, "31DBAF09-94E1-4B2D-8ACF-5E6167E0D2D2", SampleName, "", Timeout);

                Console.WriteLine($"{login_}: Login succeeded");
            }
            catch
            {
                string text = client_.Disconnect(Reason.ClientError("Client disconnect"));

                if (text != null)
                    Console.WriteLine("Disconnected : {0}", text);

                throw;
            }
        }

        void Disconnect()
        {
            try
            {
                LogoutInfo logoutInfo = client_.Logout("Client logout", Timeout);

                Console.WriteLine("Logout : " + logoutInfo.Message);
            }
            catch
            {
            }

            string text = client_.Disconnect(Reason.ClientRequest("Client disconnect"));

            if (text != null)
                Console.WriteLine("Disconnected : {0}", text);
        }

        void PrintCommands()
        {
            Console.WriteLine("help (h) - print commands");
            Console.WriteLine("get_currency_list (c) - request currency list");
            Console.WriteLine("get_symbol_list (s) - request symbol list");
            Console.WriteLine("get_session_info (i) - request session info");
            Console.WriteLine("get_quotes (gq) <symbol_id_1> ... <symbol_id_n> - request quote snapshots");
            Console.WriteLine("subscribe_quotes (sq) <symbol_id_1> ... <symbol_id_n> - subscribe to quote updates");
            Console.WriteLine("unsubscribe_quotes (uq) <symbol_id_1> ... <symbol_id_n> - unsubscribe from quote updates");
            Console.WriteLine("sqf <freq> <symbol_1> ... <symbol_n> - subscribe to quote updates with selected frequency priority");
            Console.WriteLine("sb <symbol_1> <m1> <periodicity_1> <pricetype_1> ... <periodicity_m1> <pricetype_m1> ... <symbol_n> <mn> <periodicity_1> <pricetype_1> ... <periodicity_mn> <pricetype_mn> - subscribe to bar updates");
            Console.WriteLine("ub <symbol_1> ... <symbol_n> - unsubscribe from bar updates");
            Console.WriteLine("exit (e) - exit");
        }

        void GetCurrencyList()
        {
            CurrencyInfo[] currencies = client_.GetCurrencyList(Timeout);

            int count = currencies.Length;
            for (int index = 0; index < count; ++index)
            {
                CurrencyInfo currency = currencies[index];

                Console.Error.WriteLine("Currency : {0}, {1}", currency.Name, currency.Description);
            }
        }

        void GetSecurityList()
        {
            SymbolInfo[] symbols = client_.GetSymbolList(Timeout);

            int count = symbols.Length;
            for (int index = 0; index < count; ++index)
            {
                SymbolInfo symbol = symbols[index];

                Console.Error.WriteLine("Symbol : {0}, {1}, Subscription: {2}", symbol.Name, symbol.Description, symbol.Subscription);
            }
        }

        void GetSessionInfo()
        {
            SessionInfo sessionInfo = client_.GetSessionInfo(Timeout);

            Console.Error.WriteLine("Session info : {0}, {1}-{2}, {3}", sessionInfo.Status, sessionInfo.StartTime, sessionInfo.EndTime, sessionInfo.ServerTimeZoneOffset);

            StatusGroupInfo[] groups = sessionInfo.StatusGroups;

            int count = groups.Length;
            for (int index = 0; index < count; ++index)
            {
                StatusGroupInfo group = groups[index];

                Console.Error.WriteLine("Session status group : {0}, {1}, {2}-{3}", group.StatusGroupId, group.Status, group.StartTime, group.EndTime);
            }
        }

        void GetQuotes(List<SymbolEntry> symbolEntries)
        {
            Quote[] quotes = client_.GetQuotes(symbolEntries.ToArray(), Timeout);

            int count = quotes.Length;
            for (int index = 0; index < count; ++index)
            {
                Quote quote = quotes[index];

                Console.Error.WriteLine("Snapshot : {0}, {1}", quote.Symbol, quote.CreatingTime);
                Console.Error.Write("    Bid :");

                foreach (QuoteEntry entry in quote.Bids)
                    Console.Error.Write(" {0}@{1}", entry.Volume, entry.Price);

                Console.Error.WriteLine();
                Console.Error.Write("    Ask :");

                foreach (QuoteEntry entry in quote.Asks)
                    Console.Error.Write(" {0}@{1}", entry.Volume, entry.Price);

                Console.Error.WriteLine();
                Console.Error.Write("    Indicative Option: " + quote.TickType);

                Console.Error.WriteLine();
            }
        }

        void SubscribeQuotes(List<SymbolEntry> symbolEntries, int frequencyPrioirity)
        {
            client_.SubscribeQuotes(symbolEntries.ToArray(), frequencyPrioirity, Timeout, out var errors);
            if (!errors.IsEmpty)
            {
                Console.WriteLine("Errors:");
                Console.WriteLine(string.Join("\n", errors.Errors.Select(it => $"{it.Key}: {string.Join(", ", it.Value)}")));
            }
        }

        void UnsubscribeQuotes(List<string> symbolIds)
        {
            client_.UnsubscribeQuotes(symbolIds.ToArray(), Timeout);
        }

        public void OnLogout(QuoteFeed client, LogoutInfo info)
        {
            try
            {
                Console.WriteLine("Logout : " + info.Message);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnDisconnect(QuoteFeed client, string text)
        {
            try
            {
                Console.WriteLine("Disconnected : " + text);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnSessionInfoUpdate(QuoteFeed client, SessionInfo info)
        {
            try
            {
                Console.Error.WriteLine("Session info : {0}, {1}, {2}-{3}, {4}", info.Status, info.DisabledFeatures, info.StartTime, info.EndTime, info.ServerTimeZoneOffset);

                StatusGroupInfo[] groups = info.StatusGroups;

                int count = groups.Length;
                for (int index = 0; index < count; ++index)
                {
                    StatusGroupInfo group = groups[index];

                    Console.Error.WriteLine("Session status group : {0}, {1}, {2}-{3}, {4}", group.StatusGroupId, group.DisabledFeatures, group.Status, group.StartTime, group.EndTime);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnSubscribeQuotesResult(QuoteFeed client, object data, Quote[] quotes)
        {
            try
            {
                for (int index = 0; index < quotes.Length; ++index)
                {
                    Quote quote = quotes[index];

                    Console.Error.WriteLine("Subscribed : {0}, {1}", quote.Symbol, quote.CreatingTime);
                    Console.Error.Write("    Bid :");

                    foreach (QuoteEntry entry in quote.Bids)
                        Console.Error.Write(" {0}@{1}", entry.Volume, entry.Price);

                    Console.Error.WriteLine();
                    Console.Error.Write("    Ask :");

                    foreach (QuoteEntry entry in quote.Asks)
                        Console.Error.Write(" {0}@{1}", entry.Volume, entry.Price);

                    Console.Error.WriteLine();
                    Console.Error.Write("    Indicative Option: " + quote.TickType);

                    Console.Error.WriteLine();
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnUnsubscribeQuotesResult(QuoteFeed client, object data, string[] symbolIds)
        {
            try
            {
                for (int index = 0; index < symbolIds.Length; ++index)
                {
                    string symbolId = symbolIds[index];

                    Console.Error.WriteLine("Unsubscribed {0}", symbolId);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnQuoteUpdate(QuoteFeed client, Quote quote)
        {
            try
            {
                Console.Error.WriteLine("Update : {0}, {1}", quote.Symbol, quote.CreatingTime);
                Console.Error.Write("    Bid :");

                foreach (QuoteEntry entry in quote.Bids)
                    Console.Error.Write(" {0}@{1}", entry.Volume, entry.Price);

                Console.Error.WriteLine();
                Console.Error.Write("    Ask :");

                foreach (QuoteEntry entry in quote.Asks)
                    Console.Error.Write(" {0}@{1}", entry.Volume, entry.Price);

                Console.Error.WriteLine();
                Console.Error.Write("    Indicative Option: " + quote.TickType);

                Console.Error.WriteLine();
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
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
        const int Timeout = 30000;
    }
}
