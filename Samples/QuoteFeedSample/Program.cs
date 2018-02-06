using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using NDesk.Options;
using TickTrader.FDK.Common;
using TickTrader.FDK.QuoteFeed;

namespace QuoteFeedSample
{
    public class Program : IDisposable
    {
        const int Timeout = 30000;

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
                    Console.Write("QuoteFeedSample: ");
                    Console.WriteLine(e.Message);
                    Console.WriteLine("Try `QuoteFeedSample --help' for more information.");
                    return;
                }

                if (help)
                {
                    Console.WriteLine("QuoteFeedSample usage:");
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
            client_ = new Client("QuoteFeedSample", port : port, reconnectAttempts : 0, logMessages : true);

            client_.LogoutEvent += new Client.LogoutDelegate(this.OnLogout);            
            client_.DisconnectEvent += new Client.DisconnectDelegate(this.OnDisconnect);
            client_.SessionInfoUpdateEvent += new Client.SessionInfoUpdateDelegate(this.OnSessionInfoUpdate);
            client_.SubscribeQuotesResultEvent += new Client.SubscribeQuotesResultDelegate(this.OnSubscribeQuotesResult);
            client_.UnsubscribeQuotesResultEvent += new Client.UnsubscribeQuotesResultDelegate(this.OnUnsubscribeQuotesResult);
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
                        else if (command == "currency_list" || command == "c")
                        {
                            GetCurrencyList();
                        }
                        else if (command == "symbol_list" || command == "s")
                        {
                            GetSecurityList();
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
            client_.Connect(address_, Timeout);

            try
            {
                Console.WriteLine("Connected");

                client_.Login(login_, password_, "", "", "", Timeout);

                Console.WriteLine("Login succeeded");
            }
            catch
            {
                string text = client_.Disconnect("Client disconnect");

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

            string text = client_.Disconnect("Client disconnect");

            if (text != null)
                Console.WriteLine("Disconnected : {0}", text);
        }

        void PrintCommands()
        {
            Console.WriteLine("help (h) - print commands");
            Console.WriteLine("currency_list (c) - request currency list");
            Console.WriteLine("symbol_list (s) - request symbol list");
            Console.WriteLine("session_info (i) - request session info");
            Console.WriteLine("subscribe_quotes (sq) <symbol_id_1> ... <symbol_id_n> - subscribe to quote updates");
            Console.WriteLine("unsubscribe_quotes (uq) <symbol_id_1> ... <symbol_id_n> - unsubscribe from quote updates");
            Console.WriteLine("quotes (q) <symbol_id_1> ... <symbol_id_n> - request quote snapshots");
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

                Console.Error.WriteLine("Symbol : {0}, {1}", symbol.Name, symbol.Description);
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

        void SubscribeQuotes(List<string> symbolIds)
        {
            client_.SubscribeQuotes(symbolIds.ToArray(), 5, Timeout);
        }

        void UnsubscribeQuotes(List<string> symbolIds)
        {
            client_.UnsbscribeQuotes(symbolIds.ToArray(), Timeout);
        }

        void GetQuotes(List<string> symbolIds)
        {
            Quote[] quotes = client_.GetQuotes(symbolIds.ToArray(), 5, Timeout);

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

        public void OnLogout(Client client, LogoutInfo info)
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

        void OnSessionInfoUpdate(Client client, SessionInfo info)
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

        void OnSubscribeQuotesResult(Client client, object data, Quote[] quotes)
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
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnUnsubscribeQuotesResult(Client client, object data, string[] symbolIds)
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

        void OnQuoteUpdate(Client client, Quote quote)
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
