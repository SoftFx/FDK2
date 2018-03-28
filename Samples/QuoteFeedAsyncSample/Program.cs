using System;
using System.Collections.Generic;
using System.Diagnostics;
using NDesk.Options;
using TickTrader.FDK.Common;
using TickTrader.FDK.QuoteFeed;

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
                int port = 5030;

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
            client_ = new Client("QuoteFeedAsyncSample", port : port, logMessages : true);

            client_.ConnectResultEvent += new Client.ConnectResultDelegate(this.OnConnectResult);
            client_.ConnectErrorEvent += new Client.ConnectErrorDelegate(this.OnConnectError);
            client_.DisconnectResultEvent += new Client.DisconnectResultDelegate(this.OnDisconnectResult);
            client_.DisconnectEvent += new Client.DisconnectDelegate(this.OnDisconnect);
            client_.ReconnectEvent += new Client.ReconnectDelegate(this.OnReconnect);
            client_.ReconnectErrorEvent += new Client.ReconnectErrorDelegate(this.OnReconnectError);
            client_.LoginResultEvent += new Client.LoginResultDelegate(this.OnLoginResult);
            client_.LoginErrorEvent += new Client.LoginErrorDelegate(this.OnLoginError);
            client_.LogoutResultEvent += new Client.LogoutResultDelegate(this.OnLogoutResult);
            client_.LogoutErrorEvent += new Client.LogoutErrorDelegate(this.OnLogoutError);
            client_.LogoutEvent += new Client.LogoutDelegate(this.OnLogout);
            client_.CurrencyListResultEvent += new Client.CurrencyListResultDelegate(this.OnCurrencyListResult);
            client_.CurrencyListErrorEvent += new Client.CurrencyListErrorDelegate(this.OnCurrencyListError);
            client_.SymbolListResultEvent += new Client.SymbolListResultDelegate(this.OnSymbolListResult);
            client_.SymbolListErrorEvent += new Client.SymbolListErrorDelegate(this.OnSymbolListError);
            client_.SessionInfoResultEvent += new Client.SessionInfoResultDelegate(this.OnSessionInfoResult);
            client_.SessionInfoErrorEvent += new Client.SessionInfoErrorDelegate(this.OnSessionInfoError);
            client_.SubscribeQuotesResultEvent += new Client.SubscribeQuotesResultDelegate(this.OnSubscribeQuotesResult);
            client_.SubscribeQuotesErrorEvent += new Client.SubscribeQuotesErrorDelegate(this.OnSubscribeQuotesError);
            client_.UnsubscribeQuotesResultEvent += new Client.UnsubscribeQuotesResultDelegate(this.OnUnsubscribeQuotesResult);
            client_.UnsubscribeQuotesErrorEvent += new Client.UnsubscribeQuotesErrorDelegate(this.OnUnsubscribeQuotesError);
            client_.QuotesResultEvent += new Client.QuotesResultDelegate(this.OnQuotesResult);
            client_.QuotesErrorEvent += new Client.QuotesErrorDelegate(this.OnQuotesError);            
            client_.SessionInfoUpdateEvent += new Client.SessionInfoUpdateDelegate(this.OnSessionInfoUpdate);
            client_.QuoteUpdateEvent += new Client.QuoteUpdateDelegate(this.OnQuoteUpdate);

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

        void OnConnectResult(Client client, object data)
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

        void OnConnectError(Client client, object data, Exception error)
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

        void OnDisconnectResult(Client client, object data, string text)
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

        void OnDisconnect(Client client, string text)
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

        void OnReconnect(Client client)
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

        void OnReconnectError(Client client, Exception error)
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

        void OnLoginResult(Client client, object data)
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

        void OnLoginError(Client client, object data, Exception error)
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

        void OnLogoutResult(Client client, object data, LogoutInfo info)
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

        void OnLogoutError(Client client, object data, Exception error)
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

        public void OnLogout(Client client, LogoutInfo info)
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

        void OnCurrencyListResult(Client client, object data, CurrencyInfo[] currencies)
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

        void OnCurrencyListError(Client client, object data, Exception error)
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

        void OnSymbolListResult(Client client, object data, SymbolInfo[] symbols)
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

        void OnSymbolListError(Client client, object data, Exception error)
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
                
        void OnSessionInfoResult(Client client, object data, SessionInfo sessionInfo)
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

        void OnSessionInfoError(Client client, object data, Exception error)
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
        }

        void OnSubscribeQuotesResult(Client client, object data, Quote[] quotes)
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
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnSubscribeQuotesError(Client client, object data, Exception error)
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

        void OnUnsubscribeQuotesResult(Client client, object data, string[] symbolIds)
        {
            for (int index = 0; index < symbolIds.Length; ++index)
            {
                string symbolId = symbolIds[index];

                Console.Error.WriteLine("Unsubscribed {0}", symbolId);
            }
        }

        void OnUnsubscribeQuotesError(Client client, object data, Exception error)
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

        void OnQuotesResult(Client client, object data, Quote[] quotes)
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
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnQuotesError(Client client, object data, Exception error)
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

        void OnSessionInfoUpdate(Client quoteFeedClient, SessionInfo info)
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


        void OnQuoteUpdate(Client quoteFeedClient, Quote quote)
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
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        Client client_;

        string address_;
        string login_;
        string password_;
    }
}
