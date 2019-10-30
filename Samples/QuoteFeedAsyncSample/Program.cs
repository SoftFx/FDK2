using System;
using System.Collections.Generic;
using System.Diagnostics;
using NDesk.Options;
using TickTrader.FDK.Common;
using TickTrader.FDK.Client;

namespace QuoteFeedAsyncSample
{
    public class Program : IDisposable
    {
        static void Main(string[] args)
        {
            try
            {
                bool help = false;

                string address = "localhost";
                string login = "5";
                string password = "123qwe!";
                int port = 5041;

                var options = new OptionSet()
                {
                    { "a|address=", v => address = v },
                    { "l|login=", v => login = v },
                    { "w|password=", v => password = v },
                    { "p|port=", v => port = int.Parse(v) },
                    { "h|?|help",   v => help = v != null },
                };

                try
                {
                    options.Parse(args);
                }
                catch (OptionException e)
                {
                    Console.Write("QuoteFeedAsyncSample: ");
                    Console.WriteLine(e.Message);
                    Console.WriteLine("Try `QuoteFeedAsyncSample --help' for more information.");
                    return;
                }

                if (help)
                {
                    Console.WriteLine("QuoteFeedAsyncSample usage:");
                    options.WriteOptionDescriptions(Console.Out);
                    return;
                }

                using (Program program = new Program(address, port, login, password))
                {
                    program.Run();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error : " + ex.Message);
            }
        }

        public Program(string address, int port, string login, string password)
        {
            client_ = new QuoteFeed("QuoteFeedAsyncSample", port : port, logMessages : true,
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
            client_.CurrencyListResultEvent += new QuoteFeed.CurrencyListResultDelegate(this.OnCurrencyListResult);
            client_.CurrencyListErrorEvent += new QuoteFeed.CurrencyListErrorDelegate(this.OnCurrencyListError);
            client_.SymbolListResultEvent += new QuoteFeed.SymbolListResultDelegate(this.OnSymbolListResult);
            client_.SymbolListErrorEvent += new QuoteFeed.SymbolListErrorDelegate(this.OnSymbolListError);
            client_.SessionInfoResultEvent += new QuoteFeed.SessionInfoResultDelegate(this.OnSessionInfoResult);
            client_.SessionInfoErrorEvent += new QuoteFeed.SessionInfoErrorDelegate(this.OnSessionInfoError);
            client_.SubscribeQuotesResultEvent += new QuoteFeed.SubscribeQuotesResultDelegate(this.OnSubscribeQuotesResult);
            client_.SubscribeQuotesErrorEvent += new QuoteFeed.SubscribeQuotesErrorDelegate(this.OnSubscribeQuotesError);
            client_.UnsubscribeQuotesResultEvent += new QuoteFeed.UnsubscribeQuotesResultDelegate(this.OnUnsubscribeQuotesResult);
            client_.UnsubscribeQuotesErrorEvent += new QuoteFeed.UnsubscribeQuotesErrorDelegate(this.OnUnsubscribeQuotesError);
            client_.QuotesResultEvent += new QuoteFeed.QuotesResultDelegate(this.OnQuotesResult);
            client_.QuotesErrorEvent += new QuoteFeed.QuotesErrorDelegate(this.OnQuotesError);
            client_.SessionInfoUpdateEvent += new QuoteFeed.SessionInfoUpdateDelegate(this.OnSessionInfoUpdate);
            client_.QuoteUpdateEvent += new QuoteFeed.QuoteUpdateDelegate(this.OnQuoteUpdate);

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
                ++ index;

            if (index == line.Length)
                return null;

            int startIndex = index;

            while (index < line.Length && line[index] != ' ')
                ++ index;

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
                            GetSymbolList();
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
                                symbolEntry.MarketDepth = 5;

                                symbolEnries.Add(symbolEntry);
                            }

                            SubscribeQuotes(symbolEnries);
                        }
                        else if (command == "unsubscribe_quotes" || command == "uq")
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
                        else if (command == "exit" || command == "e")
                        {
                            break;
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
                client_.DisconnectAsync(null, "Client disconnect");
            }

            client_.Join();
        }

        void OnConnectResult(QuoteFeed client, object data)
        {
            try
            {
                Console.WriteLine("Connected");

                client_.LoginAsync(null, login_, password_, "", "", "");
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);

                client_.DisconnectAsync(null, "Client disconnect");
            }
        }

        void OnConnectError(QuoteFeed client, object data, Exception error)
        {
            try
            {
                Console.WriteLine("Error : " + error.Message);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnDisconnectResult(QuoteFeed client, object data, string text)
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

        void OnReconnect(QuoteFeed client)
        {
            try
            {
                Console.WriteLine("Connected");

                client_.LoginAsync(this, login_, password_, "", "", "");
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);

                client_.DisconnectAsync(null, "Client disconnect");
            }
        }

        void OnReconnectError(QuoteFeed client, Exception error)
        {
            try
            {
                Console.WriteLine("Error : " + error.Message);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnLoginResult(QuoteFeed client, object data)
        {
            try
            {
                Console.WriteLine("Login succeeded");
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnLoginError(QuoteFeed client, object data, Exception error)
        {
            try
            {
                Console.WriteLine("Error : " + error.Message);

                client_.DisconnectAsync(null, "Client disconnect");
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnLogoutResult(QuoteFeed client, object data, LogoutInfo info)
        {
            try
            {
                Console.WriteLine("Logout : " + info.Message);

                client_.DisconnectAsync(null, "Client disconnect");
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnLogoutError(QuoteFeed client, object data, Exception error)
        {
            try
            {
                Console.WriteLine("Error : " + error.Message);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        public void OnLogout(QuoteFeed client, LogoutInfo info)
        {
            try
            {
                Console.WriteLine("Logout : " + info.Message);

                client_.DisconnectAsync(null, "Client disconnect");
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
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
            Console.WriteLine("exit (e) - exit");
        }

        void GetCurrencyList()
        {
            client_.GetCurrencyListAsync(null);
        }

        void OnCurrencyListResult(QuoteFeed client, object data, CurrencyInfo[] currencies)
        {
            try
            {
                int count = currencies.Length;
                for (int index = 0; index < count; ++index)
                {
                    CurrencyInfo currency = currencies[index];

                    Console.Error.WriteLine("Currency : {0}, {1}", currency.Name, currency.Description);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnCurrencyListError(QuoteFeed client, object data, Exception error)
        {
            try
            {
                Console.WriteLine("Error : " + error.Message);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
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
                int count = symbols.Length;
                for (int index = 0; index < count; ++index)
                {
                    SymbolInfo symbol = symbols[index];

                    Console.Error.WriteLine("Symbol : {0}, {1}", symbol.Name, symbol.Description);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnSymbolListError(QuoteFeed client, object data, Exception error)
        {
            try
            {
                Console.WriteLine("Error : " + error.Message);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
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
                Console.Error.WriteLine("Session info : {0}, {1}-{2}, {3}", sessionInfo.Status, sessionInfo.StartTime, sessionInfo.EndTime, sessionInfo.ServerTimeZoneOffset);

                StatusGroupInfo[] groups = sessionInfo.StatusGroups;

                int count = groups.Length;
                for (int index = 0; index < count; ++index)
                {
                    StatusGroupInfo group = groups[index];

                    Console.Error.WriteLine("Session status group : {0}, {1}, {2}-{3}", group.StatusGroupId, group.Status, group.StartTime, group.EndTime);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnSessionInfoError(QuoteFeed client, object data, Exception error)
        {
            try
            {
                Console.WriteLine("Error : " + error.Message);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void SubscribeQuotes(List<SymbolEntry> symbolEntries)
        {
            client_.SubscribeQuotesAsync(null, symbolEntries.ToArray());
            client_.SubscribeQuotesAsync(null, symbolEntries.ToArray());
            client_.SubscribeQuotesAsync(null, symbolEntries.ToArray());
            client_.SubscribeQuotesAsync(null, symbolEntries.ToArray());
            client_.SubscribeQuotesAsync(null, symbolEntries.ToArray());
            client_.SubscribeQuotesAsync(null, symbolEntries.ToArray());
            client_.SubscribeQuotesAsync(null, symbolEntries.ToArray());
        }

        void OnSubscribeQuotesResult(QuoteFeed client, object data, Quote[] quotes)
        {
            try
            {
                for (int index = 0; index < quotes.Length; ++ index)
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

        void OnSubscribeQuotesError(QuoteFeed client, object data, Exception error)
        {
            try
            {
                Console.WriteLine("Error : " + error.Message);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
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
                Console.WriteLine("Error : " + error.Message);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
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
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnQuotesError(QuoteFeed client, object data, Exception error)
        {
            try
            {
                Console.WriteLine("Error : " + error.Message);
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
                Console.Error.WriteLine("Session info : {0}, {1}-{2}, {3}", info.Status, info.StartTime, info.EndTime, info.ServerTimeZoneOffset);

                StatusGroupInfo[] groups = info.StatusGroups;

                int count = groups.Length;
                for (int index = 0; index < count; ++index)
                {
                    StatusGroupInfo group = groups[index];

                    Console.Error.WriteLine("Session status group : {0}, {1}, {2}-{3}", group.StatusGroupId, group.Status, group.StartTime, group.EndTime);
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

        QuoteFeed client_;

        string address_;
        string login_;
        string password_;
    }
}
