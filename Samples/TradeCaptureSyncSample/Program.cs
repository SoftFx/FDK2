using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using NDesk.Options;
using SoftFX.Net.Core;
using TickTrader.FDK.Common;
using TickTrader.FDK.Client;

namespace TradeCaptureSyncSample
{
    public class Program : IDisposable
    {
        static string SampleName = typeof(Program).Namespace;

        static void Main(string[] args)
        {
            try
            {
                string address = null;
                int port = 5044;
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
            client_ = new TradeCapture(SampleName, port : port, reconnectAttempts : 0, logMessages : true,
                validateClientCertificate: (sender, certificate, chain, errors) => true);

            client_.LogoutEvent += new TradeCapture.LogoutDelegate(this.OnLogout);
            client_.DisconnectEvent += new TradeCapture.DisconnectDelegate(this.OnDisconnect);
            client_.SubscribeTradesResultBeginEvent += new TradeCapture.SubscribeTradesResultBeginDelegate(this.OnSubscribeTradesResultBegin);
            client_.SubscribeTradesResultEvent += new TradeCapture.SubscribeTradesResultDelegate(this.OnSubscribeTradesResult);
            client_.SubscribeTradesResultEndEvent += new TradeCapture.SubscribeTradesResultEndDelegate(this.OnSubscribeTradesResultEnd);
            client_.TradeUpdateEvent += new TradeCapture.TradeUpdateDelegate(this.OnTradeUpdate);

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

                        if (command == "h" || command == "?")
                        {
                            PrintCommands();
                        }
                        else if (command == "st")
                        {
                            string from = GetNextWord(line, ref pos);

                            if (from == null)
                                throw new Exception("Invalid command : " + line);

                            SubscribeTrades(DateTime.Parse(from + "Z", null, DateTimeStyles.AdjustToUniversal));
                        }
                        else if (command == "ut")
                        {
                            UnsubscribeTrades();
                        }
                        else if (command == "dt")
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
                                DateTime.Parse(from + "Z", null, DateTimeStyles.AdjustToUniversal),
                                DateTime.Parse(to + "Z", null, DateTimeStyles.AdjustToUniversal)
                            );
                        }
                        else if (command == "da")
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

                            DownloadAccountReports
                            (
                                (TimeDirection)Enum.Parse(typeof(TimeDirection), timeDirection),
                                DateTime.Parse(from + "Z", null, DateTimeStyles.AdjustToUniversal),
                                DateTime.Parse(to + "Z", null, DateTimeStyles.AdjustToUniversal)
                            );
                        }
                        else if (command == "e")
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
            Console.WriteLine("h|? - print commands");
            Console.WriteLine("st <from> - subscribe to trades updates");
            Console.WriteLine("ut - unsubscribe from trades updates");
            Console.WriteLine("dt <direction> <from> <to> - download trade reports");
            Console.WriteLine("da <direction> <from> <to> - download account reports");
            Console.WriteLine("e - exit");
        }

        void SubscribeTrades(DateTime from)
        {
            SubscribeTradesEnumerator subscribeTradesEnumerator = client_.SubscribeTrades(from, false, -1);

            try
            {
                subscribeTradesEnumerator.End(-1);
            }
            finally
            {
                subscribeTradesEnumerator.Close();
            }
        }

        void UnsubscribeTrades()
        {
            client_.UnsubscribeTrades(-1);

            Console.WriteLine("Unsubscribed");
        }

        void DownloadTrades(TimeDirection timeDirection, DateTime from, DateTime to)
        {
            DownloadTradesEnumerator downloadTradesEnumerator = client_.DownloadTrades(timeDirection, from, to, false, -1);

            try
            {
                Console.Error.WriteLine("--------------------------------------------------------------------------------");

                for
                (
                    TradeTransactionReport tradeTransactionReport = downloadTradesEnumerator.Next(-1);
                    tradeTransactionReport != null;
                    tradeTransactionReport = downloadTradesEnumerator.Next(-1)
                )
                {
                    if (tradeTransactionReport.TradeTransactionReportType == TradeTransactionReportType.OrderFilled ||
                        tradeTransactionReport.TradeTransactionReportType == TradeTransactionReportType.PositionClosed)
                    {
                        Console.Error.WriteLine
                        (
                            "Trade report : {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}@{9}",
                            tradeTransactionReport.TradeTransactionId,
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
                            tradeTransactionReport.TradeTransactionId,
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
                downloadTradesEnumerator.Close();
            }
        }

        void DownloadAccountReports(TimeDirection timeDirection, DateTime from, DateTime to)
        {
            DownloadAccountReportsEnumerator downloadAccountReportsEnumerator = client_.DownloadAccountReports(timeDirection, from, to, -1);

            try
            {
                Console.Error.WriteLine("--------------------------------------------------------------------------------");

                for
                (
                    AccountReport report = downloadAccountReportsEnumerator.Next(-1);
                    report != null;
                    report = downloadAccountReportsEnumerator.Next(-1)
                )
                {
                    Console.WriteLine($"Account report: {report}");
                    if (report.Assets.Length > 0)
                    {
                        Console.WriteLine($"\tAssets: {report.Assets.Length}");
                        foreach (var asset in report.Assets)
                        {
                            Console.WriteLine($"\t{asset}");
                        }
                    }
                    if (report.Positions.Length > 0)
                    {
                        Console.WriteLine($"\tPositions: {report.Positions.Length}");
                        foreach (var pos in report.Positions)
                        {
                            Console.WriteLine($"\t{pos}");
                        }
                    }
                    if (report.Orders.Length > 0)
                    {
                        Console.WriteLine($"\tOrders: {report.Orders.Length}");
                        foreach (var order in report.Orders)
                        {
                            Console.WriteLine($"\t{order}");
                        }
                    }
                }

                Console.Error.WriteLine("--------------------------------------------------------------------------------");
            }
            finally
            {
                downloadAccountReportsEnumerator.Close();
            }
        }

        public void OnLogout(TradeCapture client, LogoutInfo info)
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

        void OnDisconnect(TradeCapture client, string text)
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

        public void OnSubscribeTradesResultBegin(TradeCapture client, object data, int count)
        {
            try
            {
                Console.WriteLine("Subscribing");
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        public void OnSubscribeTradesResult(TradeCapture client, object data, TradeTransactionReport tradeTransactionReport)
        {
            try
            {
                if (tradeTransactionReport.TradeTransactionReportType == TradeTransactionReportType.OrderFilled ||
                    tradeTransactionReport.TradeTransactionReportType == TradeTransactionReportType.PositionClosed)
                {
                    Console.Error.WriteLine
                    (
                        "Trade update : {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}@{9}",
                        tradeTransactionReport.TradeTransactionId,
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
                        tradeTransactionReport.TradeTransactionId,
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

        public void OnSubscribeTradesResultEnd(TradeCapture client, object data)
        {
            try
            {
                Console.WriteLine("Subscribed");
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        public void OnTradeUpdate(TradeCapture client, TradeTransactionReport tradeTransactionReport)
        {
            try
            {
                if (tradeTransactionReport.TradeTransactionReportType == TradeTransactionReportType.OrderFilled ||
                    tradeTransactionReport.TradeTransactionReportType == TradeTransactionReportType.PositionClosed)
                {
                    Console.Error.WriteLine
                    (
                        "Trade update : {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}@{9}",
                        tradeTransactionReport.TradeTransactionId,
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
                        tradeTransactionReport.TradeTransactionId,
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

        TradeCapture client_;

        string address_;
        string login_;
        string password_;
        const int Timeout = 30000;
    }
}

// st "2017.01.01 0:0:0"
// dt Forward "2017.01.01 0:0:0" "2017.11.01 0:0:0"
// da Forward "2017.01.01 0:0:0" "2017.11.01 0:0:0"
