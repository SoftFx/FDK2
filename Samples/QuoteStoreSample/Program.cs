using System;
using System.Collections.Generic;
using System.Diagnostics;
using NDesk.Options;
using TickTrader.FDK.Common;
using TickTrader.FDK.QuoteStore;

namespace QuoteStoreSample
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
                    Console.Write("QuoteStoreSample: ");
                    Console.WriteLine(e.Message);
                    Console.WriteLine("Try `QuoteStoreSample --help' for more information.");
                    return;
                }

                if (help)
                {
                    Console.WriteLine("QuoteStoreSample usage:");
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
            client_ = new Client("QuoteStoreSample", port, false, "Logs", false);

            client_.LogoutEvent += new Client.LogoutDelegate(this.OnLogout);
            client_.DisconnectEvent += new Client.DisconnectDelegate(this.OnDisconnect);

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
                                periodicity,
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
            client_.Connect(address_, Timeout);

            Console.WriteLine("Connected");

            client_.Login(login_, password_, "", "", "", Timeout);

            Console.WriteLine("Login succeeded");
        }

        void Disconnect()
        {
            try
            {
                client_.Logout("Client logout", Timeout);

                Console.WriteLine("Logout : Client logout");
            }
            catch
            {
                client_.Disconnect("Client disconnect");
            }
        }

        void PrintCommands()
        {
            Console.WriteLine("help (h) - print commands");
            Console.WriteLine("symbol_list (s) - request symbol list");
            Console.WriteLine("periodicity_list (p) <symbol> - request symbol periodicity list");
            Console.WriteLine("bar_download (b) <symbol> <side> <periodicity> <from> <to> - download symbol bars");
            Console.WriteLine("quote_download (q) <symbol> <depth> <from> <to> - download symbol quotes");
            Console.WriteLine("exit (e) - exit");
        }

        void GetSymbolList()
        {
            string[] symbols = client_.GetSymbolList(-1);

            int count = symbols.Length;
            for (int index = 0; index < count; ++index)
            {
                string symbol = symbols[index];

                Console.Error.WriteLine("Symbol : {0}", symbol);
            }
        }

        void GetPeriodicityList(string symbol)
        {
            string[] periodicities = client_.GetPeriodicityList(symbol, -1);

            int count = periodicities.Length;
            for (int index = 0; index < count; ++index)
            {
                string periodicity = periodicities[index];

                Console.Error.WriteLine("Periodicity : {0}", periodicity);
            }
        }

        void DownloadBars(string symbol, PriceType priceType, string periodicity, DateTime from, DateTime to)
        {
            BarEnumerator barEnumerator = client_.DownloadBars(Guid.NewGuid().ToString(), symbol, priceType, periodicity, from, to, -1);

            try
            {
                Console.Error.WriteLine("--------------------------------------------------------------------------------");

                for (Bar bar = barEnumerator.Next(-1); bar != null; bar = barEnumerator.Next(-1))
                    Console.WriteLine("Bar : {0}, {1}, {2}, {3}, {4}, {5}", bar.From, bar.Open, bar.Close, bar.Low, bar.High, bar.Volume);

                Console.Error.WriteLine("--------------------------------------------------------------------------------");
            }
            finally
            {
                barEnumerator.Close();
            }
        }

        void DownloadQuotes(string symbol, QuoteDepth depth, DateTime from, DateTime to)
        {
            QuoteEnumerator quoteEnumerator = client_.DownloadQuotes(Guid.NewGuid().ToString(), symbol, depth, from, to, -1);

            try
            {
                Console.Error.WriteLine("--------------------------------------------------------------------------------");

                for (Quote quote = quoteEnumerator.Next(-1); quote != null; quote = quoteEnumerator.Next(-1))
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

                Console.Error.WriteLine("--------------------------------------------------------------------------------");                
            }
            finally
            {
                quoteEnumerator.Close();
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

        void OnDisconnect(Client quoteFeedClient, object data, string text)
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

        Client client_;

        string address_;
        string login_;
        string password_;
    }
}

// b EURUSD Ask M1 "2016.06.01 08:00:00" "2016.06.01 08:20:00"
// q EURUSD Top "2016.06.01 08:00:00" "2016.06.01 08:01:00"