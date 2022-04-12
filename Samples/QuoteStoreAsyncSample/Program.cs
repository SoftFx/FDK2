using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using NDesk.Options;
using SoftFX.Net.Core;
using TickTrader.FDK.Common;
using TickTrader.FDK.Client;

namespace QuoteStoreAsyncSample
{
    public class Program : IDisposable
    {
        static string SampleName = typeof(Program).Namespace;

        static void Main(string[] args)
        {
            try
            {
                string address = null;
                int port = 5042;
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
            client_ = new QuoteStore(SampleName, port : port, logMessages : true,
                validateClientCertificate: (sender, certificate, chain, errors) => true);

            client_.ConnectResultEvent += new QuoteStore.ConnectResultDelegate(this.OnConnectResult);
            client_.ConnectErrorEvent += new QuoteStore.ConnectErrorDelegate(this.OnConnectError);
            client_.DisconnectResultEvent += new QuoteStore.DisconnectResultDelegate(this.OnDisconnectResult);
            client_.DisconnectEvent += new QuoteStore.DisconnectDelegate(this.OnDisconnect);
            client_.ReconnectEvent += new QuoteStore.ReconnectDelegate(this.OnReconnect);
            client_.ReconnectErrorEvent += new QuoteStore.ReconnectErrorDelegate(this.OnReconnectError);
            client_.LoginResultEvent += new QuoteStore.LoginResultDelegate(this.OnLoginResult);
            client_.LoginErrorEvent += new QuoteStore.LoginErrorDelegate(this.OnLoginError);
            client_.LogoutResultEvent += new QuoteStore.LogoutResultDelegate(this.OnLogoutResult);
            client_.LogoutErrorEvent += new QuoteStore.LogoutErrorDelegate(this.OnLogoutError);
            client_.LogoutEvent += new QuoteStore.LogoutDelegate(this.OnLogout);
            client_.SymbolListResultEvent += new QuoteStore.SymbolListResultDelegate(this.OnSymbolListResult);
            client_.SymbolListErrorEvent += new QuoteStore.SymbolListErrorDelegate(this.OnSymbolListError);
            client_.PeriodicityListResultEvent += new QuoteStore.PeriodicityListResultDelegate(this.OnPeriodicityListResult);
            client_.PeriodicityListErrorEvent += new QuoteStore.PeriodicityListErrorDelegate(this.OnPeriodicityListError);
            client_.BarListResultEvent += new QuoteStore.BarListResultDelegate(this.OnBarListResult);
            client_.BarListErrorEvent += new QuoteStore.BarListErrorDelegate(this.OnBarListError);
            client_.BarDownloadResultBeginEvent += new QuoteStore.BarDownloadResultBeginDelegate(this.OnBarDownloadBeginResult);
            client_.BarDownloadResultEvent += new QuoteStore.BarDownloadResultDelegate(this.OnBarDownloadResult);
            client_.BarDownloadResultEndEvent += new QuoteStore.BarDownloadResultEndDelegate(this.OnBarDownloadEndResult);
            client_.BarDownloadErrorEvent += new QuoteStore.BarDownloadErrorDelegate(this.OnBarDownloadError);
            client_.QuoteListResultEvent += new QuoteStore.QuoteListResultDelegate(this.OnQuoteListResult);
            client_.QuoteListErrorEvent += new QuoteStore.QuoteListErrorDelegate(this.OnQuoteListError);
            client_.QuoteDownloadResultBeginEvent += new QuoteStore.QuoteDownloadResultBeginDelegate(this.OnQuoteDownloadBeginResult);
            client_.QuoteDownloadResultEvent += new QuoteStore.QuoteDownloadResultDelegate(this.OnQuoteDownloadResult);
            client_.QuoteDownloadResultEndEvent += new QuoteStore.QuoteDownloadResultEndDelegate(this.OnQuoteDownloadEndResult);
            client_.QuoteDownloadErrorEvent += new QuoteStore.QuoteDownloadErrorDelegate(this.OnQuoteDownloadError);

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
                        else if (command == "bar_list" || command == "bl")
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

                            string count = GetNextWord(line, ref pos);

                            if (count == null)
                                throw new Exception("Invalid command : " + line);

                            GetBarList
                            (
                                symbol,
                                (PriceType)Enum.Parse(typeof(PriceType), priceType),
                                new BarPeriod(periodicity),
                                DateTime.Parse(from + "Z", null, DateTimeStyles.AdjustToUniversal),
                                int.Parse(count)
                            );
                        }
                        else if (command == "bar_history" || command == "bh")
                        {
                            string symbols = GetNextWord(line, ref pos);

                            if (symbols == null)
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

                            string count = GetNextWord(line, ref pos);

                            if (count == null)
                                throw new Exception("Invalid command : " + line);

                            GetBarList
                            (
                                symbols.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries),
                                (PriceType)Enum.Parse(typeof(PriceType), priceType),
                                new BarPeriod(periodicity),
                                DateTime.Parse(from + "Z", null, DateTimeStyles.AdjustToUniversal),
                                int.Parse(count)
                            );
                        }
                        else if (command == "bar_download" || command == "bd")
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
                                DateTime.Parse(from + "Z", null, DateTimeStyles.AdjustToUniversal),
                                DateTime.Parse(to + "Z", null, DateTimeStyles.AdjustToUniversal)
                            );
                        }
                        else if (command == "quote_list" || command == "ql")
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

                            string count = GetNextWord(line, ref pos);

                            if (count == null)
                                throw new Exception("Invalid command : " + line);

                            GetQuoteList
                            (
                                symbol,
                                (QuoteDepth)Enum.Parse(typeof(QuoteDepth), quoteDepth),
                                DateTime.Parse(from + "Z", null, DateTimeStyles.AdjustToUniversal),
                                int.Parse(count)
                            );
                        }
                        else if (command == "vwap_quote_list" || command == "vwapql")
                        {
                            string symbol = GetNextWord(line, ref pos);

                            if (symbol == null)
                                throw new Exception("Invalid command : " + line);

                            string degree = GetNextWord(line, ref pos);

                            if (degree == null)
                                throw new Exception("Invalid command : " + line);

                            string from = GetNextWord(line, ref pos);

                            if (from == null)
                                throw new Exception("Invalid command : " + line);

                            string count = GetNextWord(line, ref pos);

                            if (count == null)
                                throw new Exception("Invalid command : " + line);

                            GetVWAPQuoteList
                            (
                                symbol,
                                short.Parse(degree),
                                DateTime.Parse(from + "Z", null, DateTimeStyles.AdjustToUniversal),
                                int.Parse(count)
                            );
                        }
                        else if (command == "quote_download" || command == "qd")
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
                                DateTime.Parse(from + "Z", null, DateTimeStyles.AdjustToUniversal),
                                DateTime.Parse(to + "Z", null, DateTimeStyles.AdjustToUniversal)
                            );
                        }
                        else if (command == "vwap_quote_download" || command == "vwapqd")
                        {
                            string symbol = GetNextWord(line, ref pos);

                            if (symbol == null)
                                throw new Exception("Invalid command : " + line);

                            string degree = GetNextWord(line, ref pos);

                            if (degree == null)
                                throw new Exception("Invalid command : " + line);

                            string from = GetNextWord(line, ref pos);

                            if (from == null)
                                throw new Exception("Invalid command : " + line);

                            string to = GetNextWord(line, ref pos);

                            if (to == null)
                                throw new Exception("Invalid command : " + line);

                            DownloadVWAPQuotes
                            (
                                symbol,
                                short.Parse(degree),
                                DateTime.Parse(from + "Z", null, DateTimeStyles.AdjustToUniversal),
                                DateTime.Parse(to + "Z", null, DateTimeStyles.AdjustToUniversal)
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
                client_.DisconnectAsync(null, Reason.ClientError("Client disconnect"));
            }

            client_.Join();
        }

        void OnConnectResult(QuoteStore client, object data)
        {
            try
            {
                Console.WriteLine($"Connected to {address_}");

                client_.LoginAsync(null, login_, password_, "31DBAF09-94E1-4B2D-8ACF-5E6167E0D2D2", SampleName, "");
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);

                client_.DisconnectAsync(null, Reason.ClientError("Client disconnect"));
            }
        }

        void OnConnectError(QuoteStore client, object data, Exception error)
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

        void OnDisconnectResult(QuoteStore client, object data, string text)
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

        void OnDisconnect(QuoteStore client, string text)
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

        void OnReconnect(QuoteStore client)
        {
            try
            {
                Console.WriteLine("Connected");

                client_.LoginAsync(this, login_, password_, "", "", "");
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);

                client_.DisconnectAsync(null, Reason.ClientError("Client disconnect"));
            }
        }

        void OnReconnectError(QuoteStore client, Exception error)
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

        void OnLoginResult(QuoteStore client, object data)
        {
            try
            {
                Console.WriteLine($"{login_}: Login succeeded");
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnLoginError(QuoteStore client, object data, Exception error)
        {
            try
            {
                Console.WriteLine("Error : " + error.Message);

                client_.DisconnectAsync(null, Reason.ClientError(error.Message));
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnLogoutResult(QuoteStore client, object data, LogoutInfo info)
        {
            try
            {
                Console.WriteLine("Logout : " + info.Message);

                client_.DisconnectAsync(null, Reason.ClientRequest("Client logout"));
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnLogoutError(QuoteStore client, object data, Exception error)
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

        public void OnLogout(QuoteStore client, LogoutInfo info)
        {
            try
            {
                Console.WriteLine("Logout : " + info.Message);

                client_.DisconnectAsync(null, Reason.ClientRequest("Client logout"));
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
            Console.WriteLine("bar_list (bl) <symbol> <side> <periodicity> <from> <count> - request symbol bar list");
            Console.WriteLine("bar_history (bh) <symbols> <side> <periodicity> <from> <count> - request symbols bar history");
            Console.WriteLine("bar_download (bd) <symbol> <side> <periodicity> <from> <to> - download symbol bars");
            Console.WriteLine("quote_list (ql) <symbol> <depth> <from> <count> - request symbol quote list");
            Console.WriteLine("quote_download (qd) <symbol> <depth> <from> <to> - download symbol quotes");
            Console.WriteLine("vwap_quote_list (vwapql) <symbol> <degree> <from> <count> - request symbol quote list");
            Console.WriteLine("vwap_quote_download (vwapqd) <symbol> <degree> <from> <to> - download symbol quotes");
            Console.WriteLine("exit (e) - exit");
        }

        void GetSymbolList()
        {
            client_.GetSymbolListAsync(null);
        }

        void OnSymbolListResult(QuoteStore client, object data, string[] symbols)
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

        void OnSymbolListError(QuoteStore client, object data, Exception error)
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

        void OnPeriodicityListResult(QuoteStore client, object data, BarPeriod[] periodicities)
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

        void OnPeriodicityListError(QuoteStore client, object data, Exception error)
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

        void GetBarList(string symbol, PriceType priceType, BarPeriod periodicity, DateTime from, int count)
        {
            client_.GetBarListAsync(this, symbol, priceType, periodicity, from, count);
        }

        void GetBarList(string[] symbols, PriceType priceType, BarPeriod periodicity, DateTime from, int count)
        {
            client_.GetBarListAsync(this, symbols, priceType, periodicity, from, count);
        }

        void OnBarListResult(QuoteStore client, object data, Bar[] bars)
        {
            try
            {
                for (int index = 0; index < bars.Length; ++index)
                {
                    Bar bar = bars[index];

                    Console.WriteLine("Bar : {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}", bar.From, bar.To, bar.Open, bar.Close, bar.Low, bar.High, bar.Volume, bar.Symbol);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnBarListError(QuoteStore client, object data, Exception error)
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

        void OnBarDownloadBeginResult(QuoteStore client, object data, string downloadId, DateTime availFrom, DateTime availTo)
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

        void OnBarDownloadResult(QuoteStore client, object data, Bar bar)
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

        void OnBarDownloadEndResult(QuoteStore client, object data)
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

        void OnBarDownloadError(QuoteStore client, object data, Exception error)
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

        void GetQuoteList(string symbol, QuoteDepth depth, DateTime from, int count)
        {
            client_.GetQuoteListAsync(this, symbol, depth, from, count);
        }

        void GetVWAPQuoteList(string symbol, short degree, DateTime from, int count)
        {
            client_.GetVWAPQuoteListAsync(this, symbol, degree, from, count);
        }

        void OnQuoteListResult(QuoteStore client, object data, Quote[] quotes)
        {
            try
            {
                for (int index = 0; index < quotes.Length; ++index)
                {
                    Quote quote = quotes[index];

                    Console.Error.WriteLine("Quote : {0}", quote.CreatingTime);
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

        void OnQuoteListError(QuoteStore client, object data, Exception error)
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

        void DownloadVWAPQuotes(string symbol, short degree, DateTime from, DateTime to)
        {
            client_.DownloadVWAPQuotesAsync(this, symbol, degree, from, to);
        }

        void OnQuoteDownloadBeginResult(QuoteStore client, object data, string id, DateTime availFrom, DateTime availTo)
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

        void OnQuoteDownloadResult(QuoteStore client, object data, Quote quote)
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
                Console.Error.Write("    Indicative Option: " + quote.TickType);

                Console.Error.WriteLine();
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnQuoteDownloadEndResult(QuoteStore client, object data)
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

        void OnQuoteDownloadError(QuoteStore client, object data, Exception error)
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

        QuoteStore client_;

        string address_;
        string login_;
        string password_;
    }
}

// bl EURUSD Ask M1 "2016.06.01 08:00:00" 100
// bd EURUSD Ask M1 "2016.06.01 08:00:00" "2016.06.01 08:20:00"
// ql EURUSD Top "2016.06.01 08:00:00" 100
// qd EURUSD Top "2016.06.01 08:00:00" "2016.06.01 08:01:00"