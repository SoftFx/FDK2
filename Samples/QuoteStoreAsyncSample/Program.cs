using System;
using System.Collections.Generic;
using System.Diagnostics;
using NDesk.Options;
using TickTrader.FDK.Common;
using TickTrader.FDK.QuoteStore;

namespace QuoteStoreAsyncSample
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
                int port = 5050;

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
                    Console.Write("QuoteStoreAsyncSample: ");
                    Console.WriteLine(e.Message);
                    Console.WriteLine("Try `QuoteStoreAsyncSample --help' for more information.");
                    return;
                }

                if (help)
                {
                    Console.WriteLine("QuoteStoreAsyncSample usage:");
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
            client_ = new Client("QuoteStoreAsyncSample", port : port, logMessages : true);

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
            client_.SymbolListResultEvent += new Client.SymbolListResultDelegate(this.OnSymbolListResult);
            client_.SymbolListErrorEvent += new Client.SymbolListErrorDelegate(this.OnSymbolListError);
            client_.PeriodicityListResultEvent += new Client.PeriodicityListResultDelegate(this.OnPeriodicityListResult);
            client_.PeriodicityListErrorEvent += new Client.PeriodicityListErrorDelegate(this.OnPeriodicityListError);
            client_.BarDownloadResultBeginEvent += new Client.BarDownloadResultBeginDelegate(this.OnBarDownloadBeginResult);
            client_.BarDownloadResultEvent += new Client.BarDownloadResultDelegate(this.OnBarDownloadResult);
            client_.BarDownloadResultEndEvent += new Client.BarDownloadResultEndDelegate(this.OnBarDownloadEndResult);
            client_.BarDownloadErrorEvent += new Client.BarDownloadErrorDelegate(this.OnBarDownloadError);
            client_.QuoteDownloadResultBeginEvent += new Client.QuoteDownloadResultBeginDelegate(this.OnQuoteDownloadBeginResult);
            client_.QuoteDownloadResultEvent += new Client.QuoteDownloadResultDelegate(this.OnQuoteDownloadResult);
            client_.QuoteDownloadResultEndEvent += new Client.QuoteDownloadResultEndDelegate(this.OnQuoteDownloadEndResult);
            client_.QuoteDownloadErrorEvent += new Client.QuoteDownloadErrorDelegate(this.OnQuoteDownloadError);            
            
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

            string word;

            if (index < line.Length && line[index] == '"')
            {
                ++ index;

                int startIndex = index;

                while (index < line.Length && line[index] != '"')
                    ++ index;

                if (index == line.Length)
                    throw new Exception("Invalid line");

                word = line.Substring(startIndex, index - startIndex);

                ++ index;
            }
            else
            {
                int startIndex = index;

                while (index < line.Length && line[index] != ' ')
                    ++ index;

                word = line.Substring(startIndex, index - startIndex);
            }

            return word;
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
                        else if (command == "symbol_list" || command == "s")
                        {
                            GetSymbolList();
                        }
                        else if (command == "periodicity_list" || command == "p")
                        {
                            string symbol = GetNextWord(line, ref pos);

                            if (symbol == null)
                                throw new Exception("Invalid command : " + line);

                            GetPeriodicityList(symbol);
                        }
                        else if (command == "bar_download" || command == "b")
                        {
                            string symbol = GetNextWord(line, ref pos);

                            if (symbol == null)
                                throw new Exception("Invalid command : " + line);

                            string priceType = GetNextWord(line, ref pos);

                            if (priceType == null)
                                throw new Exception("Invalid command : " + line);

                            string periodicity = GetNextWord(line, ref pos);

                            if (periodicity == null)
                                throw new Exception("Invalid command : " + line);

                            string from = GetNextWord(line, ref pos);

                            if (from == null)
                                throw new Exception("Invalid command : " + line);

                            string to = GetNextWord(line, ref pos);

                            if (to == null)
                                throw new Exception("Invalid command : " + line);

                            DownloadBars
                            (
                                symbol,
                                (PriceType)Enum.Parse(typeof(PriceType), priceType),
                                new BarPeriod(periodicity),
                                DateTime.Parse(from),
                                DateTime.Parse(to)
                            );
                        }
                        else if (command == "quote_download" || command == "q")
                        {
                            string symbol = GetNextWord(line, ref pos);

                            if (symbol == null)
                                throw new Exception("Invalid command : " + line);

                            string quoteDepth = GetNextWord(line, ref pos);

                            if (quoteDepth == null)
                                throw new Exception("Invalid command : " + line);

                            string from = GetNextWord(line, ref pos);

                            if (from == null)
                                throw new Exception("Invalid command : " + line);

                            string to = GetNextWord(line, ref pos);

                            if (to == null)
                                throw new Exception("Invalid command : " + line);

                            DownloadQuotes
                            (
                                symbol,
                                (QuoteDepth)Enum.Parse(typeof(QuoteDepth), quoteDepth),
                                DateTime.Parse(from),
                                DateTime.Parse(to)
                            );
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

        public void OnLogout(Client quoteFeedClient, LogoutInfo info)
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
            Console.WriteLine("symbol_list (s) - request symbol list");
            Console.WriteLine("periodicity_list (p) <symbol> - request symbol periodicity list");
            Console.WriteLine("bar_download (b) <symbol> <side> <periodicity> <from> <to> - download symbol bars");
            Console.WriteLine("quote_download (q) <symbol> <depth> <from> <to> - download symbol quotes");
            Console.WriteLine("cancel_downloads (c) - cancel all downloads");
            Console.WriteLine("exit (e) - exit");
        }

        void GetSymbolList()
        {
            client_.GetSymbolListAsync(null);
        }

        void OnSymbolListResult(Client client, object data, string[] symbols)
        {
            try
            {
                int count = symbols.Length;
                for (int index = 0; index < count; ++index)
                {
                    string symbol = symbols[index];

                    Console.Error.WriteLine("Symbol : {0}", symbol);
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

        void GetPeriodicityList(string symbol)
        {
            client_.GetPeriodicityListAsync(this, symbol);
        }

        void OnPeriodicityListResult(Client quoteFeedClient, object data, BarPeriod[] periodicities)
        {
            try
            {
                int count = periodicities.Length;
                for (int index = 0; index < count; ++index)
                {
                    BarPeriod periodicity = periodicities[index];

                    Console.Error.WriteLine("Periodicity : {0}", periodicity);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnPeriodicityListError(Client client, object data, Exception error)
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

        void DownloadBars(string symbol, PriceType priceType, BarPeriod periodicity, DateTime from, DateTime to)
        {
            client_.DownloadBarsAsync(this, symbol, priceType, periodicity, from, to);
        }

        void OnBarDownloadBeginResult(Client client, object data, string downloadId, DateTime availFrom, DateTime availTo)
        {
            try
            {
                Console.Error.WriteLine("--------------------------------------------------------------------------------");
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnBarDownloadResult(Client client, object data, Bar bar)
        {
            try
            {
                Console.WriteLine("Bar : {0}, {1}, {2}, {3}, {4}, {5}, {6}", bar.From, bar.To, bar.Open, bar.Close, bar.Low, bar.High, bar.Volume);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnBarDownloadEndResult(Client client, object data)
        {
            try
            {
                Console.Error.WriteLine("--------------------------------------------------------------------------------");
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnBarDownloadError(Client client, object data, Exception error)
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

        void DownloadQuotes(string symbol, QuoteDepth depth, DateTime from, DateTime to)
        {
            client_.DownloadQuotesAsync(this, symbol, depth, from, to);
        }

        void OnQuoteDownloadBeginResult(Client client, object data, string id, DateTime availFrom, DateTime availTo)
        {
            try
            {
                Console.Error.WriteLine("--------------------------------------------------------------------------------");
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnQuoteDownloadResult(Client client, object data, Quote quote)
        {
            try
            {
                Console.Error.WriteLine("Quote : {0}", quote.CreatingTime);
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

        void OnQuoteDownloadEndResult(Client client, object data)
        {
            try
            {
                Console.Error.WriteLine("--------------------------------------------------------------------------------");
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }            
        }

        void OnQuoteDownloadError(Client client, object data, Exception error)
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

        Client client_;

        string address_;
        string login_;
        string password_;
    }
}

// b EURUSD Ask M1 "2016.06.01 08:00:00" "2016.06.01 08:20:00"
// q EURUSD Top "2016.06.01 08:00:00" "2016.06.01 08:01:00"