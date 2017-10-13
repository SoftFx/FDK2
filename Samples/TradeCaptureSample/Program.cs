using System;
using System.Collections.Generic;
using System.Diagnostics;
using NDesk.Options;
using TickTrader.FDK.Common;
using TickTrader.FDK.TradeCapture;

namespace TradeCaptureSample
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
                int port = 5060;

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
                    Console.Write("TradeCaptureSample: ");
                    Console.WriteLine(e.Message);
                    Console.WriteLine("Try `TradeCaptureSample --help' for more information.");
                    return;
                }

                if (help)
                {
                    Console.WriteLine("TradeCaptureSample usage:");
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
            client_ = new Client("TradeCaptureSample", port, false, "Logs", false);

            client_.LogoutEvent += new Client.LogoutDelegate(this.OnLogout);
            client_.DisconnectEvent += new Client.DisconnectDelegate(this.OnDisconnect);
            client_.TradeUpdateEvent += new Client.TradeUpdateDelegate(this.OnTradeUpdate);

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
                        else if (command == "subscribe_trades" || command == "s")
                        {
                            SubscribeTrades();
                        }
                        else if (command == "unsubscribe_trades" || command == "u")
                        {
                            UnsubscribeTrades();
                        }
                        else if (command == "download_trades" || command == "d")
                        {
                            string timeDirection = GetNextWord(line, ref pos);

                            if (timeDirection == null)
                                throw new Exception("Invalid command : " + line);

                            string from = GetNextWord(line, ref pos);

                            if (from == null)
                                throw new Exception("Invalid command : " + line);

                            string to = GetNextWord(line, ref pos);

                            if (to == null)
                                throw new Exception("Invalid command : " + line);

                            DownloadTrades
                            (
                                (TimeDirection)Enum.Parse(typeof(TimeDirection), timeDirection),
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

            client_.Login(login_, password_, "", "", Timeout);

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

        public void OnLogout(Client client, LogoutInfo info)
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

        void OnDisconnect(Client client, string text)
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

        void PrintCommands()
        {
            Console.WriteLine("help (h) - print commands");
            Console.WriteLine("subscribe_trades (s) - subscribe to trades updates");
            Console.WriteLine("unsubscribe_trades (u) - unsubscribe from trades updates");
            Console.WriteLine("download_trades (d) <direction> <from> <to> - download trade reports");
            Console.WriteLine("exit (e) - exit");
        }

        void SubscribeTrades()
        {
            client_.SubscribeTrades(false, -1);
        }

        void UnsubscribeTrades()
        {
            client_.UnsubscribeTrades(-1);
        }

        public void OnTradeUpdate(Client client, TradeTransactionReport tradeTransactionReport)
        {
            try
            {
                if (tradeTransactionReport.TradeTransactionReportType == TradeTransactionReportType.OrderFilled ||
                    tradeTransactionReport.TradeTransactionReportType == TradeTransactionReportType.PositionClosed)
                {
                    Console.Error.WriteLine
                    (
                        "Trade update : {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}@{9}",
                        tradeTransactionReport.Id,
                        tradeTransactionReport.TransactionTime,
                        tradeTransactionReport.TradeTransactionReportType,
                        tradeTransactionReport.TradeTransactionReason,
                        tradeTransactionReport.ClientId,
                        tradeTransactionReport.OrderType,
                        tradeTransactionReport.Symbol,
                        tradeTransactionReport.OrderSide,
                        tradeTransactionReport.OrderLastFillAmount,
                        tradeTransactionReport.OrderFillPrice
                    );
                }
                else
                {
                    Console.Error.WriteLine
                    (
                        "Trade update : {0}, {1}, {2}, {3}", 
                        tradeTransactionReport.Id,
                        tradeTransactionReport.TransactionTime,
                        tradeTransactionReport.TradeTransactionReportType,
                        tradeTransactionReport.TradeTransactionReason
                    );
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void DownloadTrades(TimeDirection timeDirection, DateTime from, DateTime to)
        {
            TradeTransactionReportEnumerator tradeTransactionReportEnumerator = client_.DownloadTrades(timeDirection, from, to, false, -1);

            try
            {
                Console.Error.WriteLine("--------------------------------------------------------------------------------");

                for
                (
                    TradeTransactionReport tradeTransactionReport = tradeTransactionReportEnumerator.Next(-1);
                    tradeTransactionReport != null;
                    tradeTransactionReport = tradeTransactionReportEnumerator.Next(-1)
                )
                {
                    if (tradeTransactionReport.TradeTransactionReportType == TradeTransactionReportType.OrderFilled ||
                        tradeTransactionReport.TradeTransactionReportType == TradeTransactionReportType.PositionClosed)
                    {
                        Console.Error.WriteLine
                        (
                            "Trade report : {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}@{9}",
                            tradeTransactionReport.Id,
                            tradeTransactionReport.TransactionTime,
                            tradeTransactionReport.TradeTransactionReportType,
                            tradeTransactionReport.TradeTransactionReason,
                            tradeTransactionReport.ClientId,
                            tradeTransactionReport.OrderType,
                            tradeTransactionReport.Symbol,
                            tradeTransactionReport.OrderSide,
                            tradeTransactionReport.OrderLastFillAmount,
                            tradeTransactionReport.OrderFillPrice
                        );
                    }
                    else
                    {
                        Console.Error.WriteLine
                        (
                            "Trade report : {0}, {1}, {2}, {3}", 
                            tradeTransactionReport.Id,
                            tradeTransactionReport.TransactionTime,
                            tradeTransactionReport.TradeTransactionReportType,
                            tradeTransactionReport.TradeTransactionReason
                        );
                    }
                }

                Console.Error.WriteLine("--------------------------------------------------------------------------------");
            }
            finally
            {
                tradeTransactionReportEnumerator.Dispose();
            }
        }

        Client client_;

        string address_;
        string login_;
        string password_;
    }
}

// d Forward "2017.01.01 0:0:0" "2017.11.01 0:0:0"
