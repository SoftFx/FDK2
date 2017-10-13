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
            client_ = new Client("QuoteFeedAsyncSample", port, true, "Logs", false);

            client_.ConnectEvent += new Client.ConnectDelegate(this.OnConnect);
            client_.ConnectErrorEvent += new Client.ConnectErrorDelegate(this.OnConnectError);
            client_.LoginResultEvent += new Client.LoginResultDelegate(this.OnLoginResult);
            client_.LoginErrorEvent += new Client.LoginErrorDelegate(this.OnLoginError);
            client_.LogoutResultEvent += new Client.LogoutResultDelegate(this.OnLogoutResult);
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

            client_.LogoutEvent += new Client.LogoutDelegate(this.OnLogout);
            client_.DisconnectEvent += new Client.DisconnectDelegate(this.OnDisconnect);
            client_.SessionInfoUpdateEvent += new Client.SessionInfoUpdateDelegate(this.OnSessionInfoUpdate);
            client_.QuotesBeginEvent += new Client.QuotesBeginDelegate(this.OnQuotesBegin);
            client_.QuoteUpdateEvent += new Client.QuoteUpdateDelegate(this.OnQuoteUpdate);

            address_ = address;
            login_ = login;
            password_ = password;
        }

        public void Dispose()
        {
            client_.Dispose();
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
                        else if (command == "currency_list" || command == "c")
                        {
                            GetCurrencyList();
                        }
                        else if (command == "security_list" || command == "s")
                        {
                            GetSymbolList();
                        }
                        else if (command == "session_info" || command == "i")
                        {
                            GetSessionInfo();
                        }
                        else if (command == "subscribe_quotes" || command == "sq")
                        {
                            List<string> symbolIds = new List<string>();

                            while (true)
                            {
                                string symbolId = GetNextWord(line, ref pos);

                                if (symbolId == null)
                                    break;

                                symbolIds.Add(symbolId);
                            }

                            SubscribeQuotes(symbolIds);
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
                        else if (command == "quotes" || command == "q")
                        {
                            List<string> symbolIds = new List<string>();

                            while (true)
                            {
                                string symbolId = GetNextWord(line, ref pos);

                                if (symbolId == null)
                                    break;

                                symbolIds.Add(symbolId);
                            }

                            GetQuotes(symbolIds);
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
            client_.ConnectAsync(address_);
        }

        void OnConnect(Client client)
        {
            try
            {
                Console.WriteLine("Connected");

                client_.LoginAsync(null, login_, password_, "", "");
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnConnectError(Client client, string text)
        {
            try
            {
                Console.WriteLine("Error : " + text);
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

        void OnLoginError(Client client, object data, string message)
        {
            try
            {
                Console.WriteLine("Error : " + message);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void Disconnect()
        {
            try
            {
                client_.LogoutAsync(null, "Client logout");
            }
            catch
            {
                client_.DisconnectAsync("Client disconnect");
            }
        }

        void OnLogoutResult(Client client, object data, LogoutInfo info)
        {
            try
            {
                Console.WriteLine("Logout : {0}", info.Message);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void PrintCommands()
        {
            Console.WriteLine("help (h) - print commands");
            Console.WriteLine("currency_list (c) - request currency list");
            Console.WriteLine("security_list (s) - request security list");
            Console.WriteLine("session_info (i) - request session info");
            Console.WriteLine("subscribe_quotes (sq) <symbol_id_1> ... <symbol_id_n> - subscribe to quote updates");
            Console.WriteLine("unsubscribe_quotes (uq) <symbol_id_1> ... <symbol_id_n> - unsubscribe from quote updates");
            Console.WriteLine("quotes (q) <symbol_id_1> ... <symbol_id_n> - request quote snapshots");
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

        void OnCurrencyListError(Client client, object data, string message)
        {
            try
            {
                Console.WriteLine("Error : " + message);
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

        void OnSymbolListError(Client client, object data, string message)
        {
            try
            {
                Console.WriteLine("Error : " + message);
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

        void OnSessionInfoError(Client client, object data, string message)
        {
            try
            {
                Console.WriteLine("Error : " + message);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void SubscribeQuotes(List<string> symbolIds)
        {
            client_.SubscribeQuotesAsync(null, symbolIds.ToArray(), 5);
        }

        void OnSubscribeQuotesResult(Client client, object data)
        {
        }

        void OnSubscribeQuotesError(Client client, object data, string message)
        {
            try
            {
                Console.WriteLine("Error : " + message);
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

        void OnUnsubscribeQuotesResult(Client client, object data)
        {
        }

        void OnUnsubscribeQuotesError(Client client, object data, string message)
        {
            try
            {
                Console.WriteLine("Error : " + message);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void GetQuotes(List<string> symbolIds)
        {
            client_.GetQuotesAsync(null, symbolIds.ToArray(), 5);
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

        void OnQuotesError(Client client, object data, string message)
        {
            try
            {
                Console.WriteLine("Error : " + message);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        public void OnLogout(Client quoteFeedClient, LogoutInfo info)
        {
            try
            {
                Console.WriteLine("Logout : {0}", info.Message);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnDisconnect(Client quoteFeedClient, string text)
        {
            try
            {
                Console.WriteLine("Disconnected : {0}", text);
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

        void OnQuotesBegin(Client quoteFeedClient, Quote[] quotes)
        {
            try
            {
                for (int index = 0; index < quotes.Length; ++ index)
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

        void OnQuoteUpdate(Client quoteFeedClient, Quote quote)
        {
            try
            {
                Console.Error.WriteLine("Refresh : {0}, {1}", quote.Symbol, quote.CreatingTime);
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
