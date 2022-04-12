﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using NDesk.Options;
using SoftFX.Net.Core;
using TickTrader.FDK.Common;
using TickTrader.FDK.Client;

namespace QuoteStoreSyncSample
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
            client_ = new QuoteStore(SampleName, port : port, reconnectAttempts : 0, logMessages : true,
                validateClientCertificate: (sender, certificate, chain, errors) => true);

            client_.LogoutEvent += new QuoteStore.LogoutDelegate(this.OnLogout);
            client_.DisconnectEvent += new QuoteStore.DisconnectDelegate(this.OnDisconnect);

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

                            DownloadQuotes
                            (
                                symbol,
                                short.Parse(degree),
                                DateTime.Parse(from + "Z", null, DateTimeStyles.AdjustToUniversal),
                                DateTime.Parse(to + "Z", null, DateTimeStyles.AdjustToUniversal)
                            );
                        }
                        else if (command == "bars_history_info" || command == "bi")
                        {
                            string symbol = GetNextWord(line, ref pos);
                            if (symbol == null)
                                throw new Exception("Invalid command : " + line);
                            string periodicity = GetNextWord(line, ref pos);

                            if (periodicity == null)
                                throw new Exception("Invalid command : " + line);

                            string pricetype = GetNextWord(line, ref pos);

                            if (pricetype == null)
                                throw new Exception("Invalid command : " + line);
                            GetBarsHistoryInfo
                            (
                                symbol,
                                periodicity,
                                (PriceType)Enum.Parse(typeof(PriceType), pricetype)
                            );
                        }
                        else if (command == "ticks_history_info" || command == "ti")
                        {
                            string symbol = GetNextWord(line, ref pos);
                            if (symbol == null)
                                throw new Exception("Invalid command : " + line);

                            string level2 = GetNextWord(line, ref pos);

                            GetTicksHistoryInfo
                            (
                                symbol,
                                level2 != null && level2.ToLower() == "level2"
                            );
                        }
                        else if (command == "vwap_ticks_history_info" || command == "vwapti")
                        {
                            string symbol = GetNextWord(line, ref pos);
                            if (symbol == null)
                                throw new Exception("Invalid command : " + line);

                            string degree = GetNextWord(line, ref pos);

                            GetVWAPTicksHistoryInfo
                            (
                                symbol,
                                short.Parse(degree)
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
            Console.WriteLine("symbol_list (s) - request symbol list");
            Console.WriteLine("periodicity_list (p) <symbol> - request symbol periodicity list");
            Console.WriteLine("bar_list (bl) <symbol> <side> <periodicity> <from> <count> - request symbol bar list");
            Console.WriteLine("bar_history (bh) <symbols> <side> <periodicity> <from> <count> - request symbols bar history");
            Console.WriteLine("bar_download (bd) <symbol> <side> <periodicity> <from> <to> - download symbol bars");
            Console.WriteLine("quote_list (ql) <symbol> <depth> <from> <count> - request symbol quote list");
            Console.WriteLine("quote_download (qd) <symbol> <depth> <from> <to> - download symbol quotes");
            Console.WriteLine("vwap_quote_list (vwapql) <symbol> <degree> <from> <count> - request symbol quote list");
            Console.WriteLine("vwap_quote_download (vwapqd) <symbol> <degree> <from> <to> - download symbol quotes");
            Console.WriteLine("bars_history_info (bi) <symbol> <periodicity> <pricetype> - request bars history info");
            Console.WriteLine("ticks_history_info (ti) <symbol> <level2> - request ticks history info");
            Console.WriteLine("vwap_ticks_history_info (vwapti) <symbol> <level2> - request ticks history info");
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
            BarPeriod[] periodicities = client_.GetPeriodicityList(symbol, -1);

            int count = periodicities.Length;
            for (int index = 0; index < count; ++index)
            {
                BarPeriod periodicity = periodicities[index];

                Console.Error.WriteLine("Periodicity : {0}", periodicity);
            }
        }

        void GetBarList(string symbol, PriceType priceType, BarPeriod periodicity, DateTime from, int count)
        {
            Bar[] bars = client_.GetBarList(symbol, priceType, periodicity, from, count, -1);

            for (int index = 0; index < bars.Length; ++index)
            {
                Bar bar = bars[index];

                Console.WriteLine("Bar : {0}, {1}, {2}, {3}, {4}, {5}, {6}", bar.From, bar.To, bar.Open, bar.Close, bar.Low, bar.High, bar.Volume);
            }
        }

        void GetBarList(string[] symbols, PriceType priceType, BarPeriod periodicity, DateTime from, int count)
        {
            Bar[] bars = client_.GetBarList(symbols, priceType, periodicity, from, count, -1);

            for (int index = 0; index < bars.Length; ++index)
            {
                Bar bar = bars[index];

                Console.WriteLine("Bar : {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}", bar.From, bar.To, bar.Open, bar.Close, bar.Low, bar.High, bar.Volume, bar.Symbol);
            }
        }

        void DownloadBars(string symbol, PriceType priceType, BarPeriod periodicity, DateTime from, DateTime to)
        {
            DownloadBarsEnumerator enumerator = client_.DownloadBars(symbol, priceType, periodicity, from, to, -1);

            try
            {
                Console.Error.WriteLine("--------------------------------------------------------------------------------");

                for (Bar bar = enumerator.Next(-1); bar != null; bar = enumerator.Next(-1))
                    Console.WriteLine("Bar : {0}, {1}, {2}, {3}, {4}, {5}, {6}", bar.From, bar.To, bar.Open, bar.Close, bar.Low, bar.High, bar.Volume);

                Console.Error.WriteLine("--------------------------------------------------------------------------------");
            }
            finally
            {
                enumerator.Close();
            }
        }

        void GetQuoteList(string symbol, QuoteDepth depth, DateTime from, int count)
        {
            Quote[] quotes = client_.GetQuoteList(symbol, depth, from, count, -1);

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

        void GetVWAPQuoteList(string symbol, short degree, DateTime from, int count)
        {
            Quote[] quotes = client_.GetVWAPQuoteList(symbol, degree, from, count, -1);

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

        void DownloadQuotes(string symbol, QuoteDepth depth, DateTime from, DateTime to)
        {
            DownloadQuotesEnumerator enumerator = client_.DownloadQuotes(symbol, depth, from, to, -1);

            try
            {
                Console.Error.WriteLine("--------------------------------------------------------------------------------");

                for (Quote quote = enumerator.Next(-1); quote != null; quote = enumerator.Next(-1))
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

                Console.Error.WriteLine("--------------------------------------------------------------------------------");
            }
            finally
            {
                enumerator.Close();
            }
        }
        void DownloadQuotes(string symbol, short degree, DateTime from, DateTime to)
        {
            DownloadQuotesEnumerator enumerator = client_.DownloadVWAPQuotes(symbol, degree, from, to, -1);

            try
            {
                Console.Error.WriteLine("--------------------------------------------------------------------------------");

                for (Quote quote = enumerator.Next(-1); quote != null; quote = enumerator.Next(-1))
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

                Console.Error.WriteLine("--------------------------------------------------------------------------------");
            }
            finally
            {
                enumerator.Close();
            }
        }

        void GetBarsHistoryInfo(string symbol, string periodicity, PriceType priceType)
        {
            HistoryInfo historyInfo = client_.GetBarsHistoryInfo(symbol, new BarPeriod(periodicity), priceType, -1);
            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine("Bars History Info: {0}", historyInfo);
            Console.WriteLine("--------------------------------------------------------------------------------");
        }

        void GetTicksHistoryInfo(string symbol, bool level2)
        {
            HistoryInfo historyInfo = client_.GetQuotesHistoryInfo(symbol, level2, -1);
            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine("Ticks History Info: {0}", historyInfo);
            Console.WriteLine("--------------------------------------------------------------------------------");
        }

        void GetVWAPTicksHistoryInfo(string symbol, short degree)
        {
            HistoryInfo historyInfo = client_.GetVWAPQuotesHistoryInfo(symbol, degree, -1);
            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine("VWAP Ticks History Info: {0}", historyInfo);
            Console.WriteLine("--------------------------------------------------------------------------------");
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

        public void OnLogout(QuoteStore client, LogoutInfo info)
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

        QuoteStore client_;

        string address_;
        string login_;
        string password_;
        const int Timeout = 30000;
    }
}

// bl EURUSD Ask M1 "2016.06.01 08:00:00" 100
// bd EURUSD Ask M1 "2016.06.01 08:00:00" "2016.06.01 08:20:00"
// ql EURUSD Top "2016.06.01 08:00:00" 100
// qd EURUSD Top "2016.06.01 08:00:00" "2016.06.01 08:01:00"