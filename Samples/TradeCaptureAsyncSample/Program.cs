using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using NDesk.Options;
using SoftFX.Net.Core;
using TickTrader.FDK.Common;
using TickTrader.FDK.Client;

namespace TradeCaptureAsyncSample
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
            client_ = new TradeCapture(SampleName, port : port, logMessages : true,
                validateClientCertificate: (sender, certificate, chain, errors) => true);

            client_.ConnectResultEvent += new TradeCapture.ConnectResultDelegate(this.OnConnectResult);
            client_.ConnectErrorEvent += new TradeCapture.ConnectErrorDelegate(this.OnConnectError);
            client_.DisconnectResultEvent += new TradeCapture.DisconnectResultDelegate(this.OnDisconnectResult);
            client_.DisconnectEvent += new TradeCapture.DisconnectDelegate(this.OnDisconnect);
            client_.ReconnectEvent += new TradeCapture.ReconnectDelegate(this.OnReconnect);
            client_.ReconnectErrorEvent += new TradeCapture.ReconnectErrorDelegate(this.OnReconnectError);
            client_.LoginResultEvent += new TradeCapture.LoginResultDelegate(this.OnLoginResult);
            client_.LoginErrorEvent += new TradeCapture.LoginErrorDelegate(this.OnLoginError);
            client_.TwoFactorLoginRequestEvent += new TradeCapture.TwoFactorLoginRequestDelegate(this.OnTwoFactorLoginRequest);
            client_.TwoFactorLoginResultEvent += new TradeCapture.TwoFactorLoginResultDelegate(this.OnTwoFactorLoginResult);
            client_.TwoFactorLoginErrorEvent += new TradeCapture.TwoFactorLoginErrorDelegate(this.OnTwoFactorLoginError);
            client_.TwoFactorLoginResumeEvent += new TradeCapture.TwoFactorLoginResumeDelegate(this.OnTwoFactorLoginResume);
            client_.LogoutResultEvent += new TradeCapture.LogoutResultDelegate(this.OnLogoutResult);
            client_.LogoutErrorEvent += new TradeCapture.LogoutErrorDelegate(this.OnLogoutError);
            client_.LogoutEvent += new TradeCapture.LogoutDelegate(this.OnLogout);
            client_.SubscribeTradesResultBeginEvent += new TradeCapture.SubscribeTradesResultBeginDelegate(this.OnSubscribeTradesResultBegin);
            client_.SubscribeTradesResultEvent += new TradeCapture.SubscribeTradesResultDelegate(this.OnSubscribeTradesResult);
            client_.SubscribeTradesResultEndEvent += new TradeCapture.SubscribeTradesResultEndDelegate(this.OnSubscribeTradesResultEnd);
            client_.SubscribeTradesErrorEvent += new TradeCapture.SubscribeTradesErrorDelegate(this.OnSubscribeTradesError);
            client_.UnsubscribeTradesResultEvent += new TradeCapture.UnsubscribeTradesResultDelegate(this.OnUnsubscribeTradesResult);
            client_.UnsubscribeTradesErrorEvent += new TradeCapture.UnsubscribeTradesErrorDelegate(this.OnUnsubscribeTradesError);
            client_.DownloadTradesResultBeginEvent += new TradeCapture.DownloadTradesResultBeginDelegate(this.OnDownloadTradesResultBegin);
            client_.DownloadTradesResultEvent += new TradeCapture.DownloadTradesResultDelegate(this.OnDownloadTradesResult);
            client_.DownloadTradesResultEndEvent += new TradeCapture.DownloadTradesResultEndDelegate(this.OnDownloadTradesResultEnd);
            client_.DownloadTradesErrorEvent += new TradeCapture.DownloadTradesErrorDelegate(this.OnDownloadTradesError);
            client_.DownloadAccountReportsResultBeginEvent += new TradeCapture.DownloadAccountReportsResultBeginDelegate(this.OnDownloadAccountReportsResultBegin);
            client_.DownloadAccountReportsResultEvent += new TradeCapture.DownloadAccountReportsResultDelegate(this.OnDownloadAccountReportsResult);
            client_.DownloadAccountReportsResultEndEvent += new TradeCapture.DownloadAccountReportsResultEndDelegate(this.OnDownloadAccountReportsResultEnd);
            client_.DownloadAccountReportsErrorEvent += new TradeCapture.DownloadAccountReportsErrorDelegate(this.OnDownloadAccountReportsError);
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
                        else if (command == "sotp")
                        {
                            string oneTimePassword = GetNextWord(line, ref pos);

                            SendOneTimePassword(oneTimePassword);
                        }
                        else if (command == "rotp")
                        {
                            ResumeOneTimePassword();
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

        void OnConnectResult(TradeCapture client, object data)
        {
            try
            {
                Console.WriteLine($"Connected to {address_}");

                client_.LoginAsync(null, login_, password_, "31DBAF09-94E1-4B2D-8ACF-5E6167E0D2D2", SampleName, "");
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnConnectError(TradeCapture client, object data, Exception error)
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

        void OnDisconnectResult(TradeCapture client, object data, string text)
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

        void OnReconnect(TradeCapture client)
        {
            try
            {
                Console.WriteLine("Connected");

                client_.LoginAsync(null, login_, password_, "", "", "");
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);

                client_.DisconnectAsync(null, Reason.ClientError("Client disconnect"));
            }
        }

        void OnReconnectError(TradeCapture client, Exception error)
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

        void OnLoginResult(TradeCapture client, object data)
        {
            try
            {
                Console.WriteLine($"{login_}: Login succeeded");

                Console.WriteLine("Download trades for today");
                DownloadTrades(TimeDirection.Forward, DateTime.UtcNow.Date, DateTime.UtcNow);
                Console.WriteLine("Subscribe for trades");
                SubscribeTrades(DateTime.UtcNow);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnLoginError(TradeCapture client, object data, Exception error)
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

        void OnTwoFactorLoginRequest(TradeCapture client, string message)
        {
            try
            {
                Console.WriteLine("Please send one time password");
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnLogoutResult(TradeCapture client, object data, LogoutInfo info)
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

        void OnLogoutError(TradeCapture client, object data, Exception error)
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

        public void OnLogout(TradeCapture client, LogoutInfo info)
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
            Console.WriteLine("h|? - print commands");
            Console.WriteLine("sotp <one_time_password> - send one time password");
            Console.WriteLine("rotp - resume one time password");
            Console.WriteLine("st <from> - subscribe to trades updates");
            Console.WriteLine("ut - unsubscribe from trades updates");
            Console.WriteLine("dt <direction> <from> <to> - download trade reports");
            Console.WriteLine("da <direction> <from> <to> - download account reports");
            Console.WriteLine("e - exit");
        }

        void SendOneTimePassword(string oneTimePassword)
        {
            client_.TwoFactorLoginResponseAsync(this, oneTimePassword);
        }

        void OnTwoFactorLoginResult(TradeCapture orderEntry, object data, DateTime expireTime)
        {
            try
            {
                Console.WriteLine("One time password expiration time : " + expireTime);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnTwoFactorLoginError(TradeCapture orderEntry, object data, Exception error)
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

        void ResumeOneTimePassword()
        {
            client_.TwoFactorLoginResumeAsync(this);
        }

        void OnTwoFactorLoginResume(TradeCapture orderEntry, object data, DateTime expireTime)
        {
            try
            {
                Console.WriteLine("One time password expiration time : " + expireTime);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void SubscribeTrades(DateTime from)
        {
            client_.SubscribeTradesAsync(this, from,  false);
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
                    Console.WriteLine
                    (
                        "Trade update : {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}@{9}, {10}",
                        tradeTransactionReport.TradeTransactionId,
                        tradeTransactionReport.TransactionTime,
                        tradeTransactionReport.TradeTransactionReportType,
                        tradeTransactionReport.TradeTransactionReason,
                        tradeTransactionReport.Id,
                        tradeTransactionReport.OrderType,
                        tradeTransactionReport.Symbol,
                        tradeTransactionReport.OrderSide,
                        tradeTransactionReport.OrderLastFillAmount,
                        tradeTransactionReport.OrderFillPrice,
                        tradeTransactionReport.Comment
                    );
                }
                else
                {
                    Console.WriteLine
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

        public void OnSubscribeTradesError(TradeCapture client, object data, Exception error)
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

        void UnsubscribeTrades()
        {
            client_.UnsubscribeTradesAsync(this);
        }

        public void OnUnsubscribeTradesResult(TradeCapture client, object data)
        {
            try
            {
                Console.WriteLine("Unsubscribed");
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        public void OnUnsubscribeTradesError(TradeCapture client, object data, Exception error)
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

        void DownloadTrades(TimeDirection timeDirection, DateTime from, DateTime to)
        {
            client_.DownloadTradesAsync(this, timeDirection, from, to, false);
        }

        public void OnDownloadTradesResultBegin(TradeCapture client, object data, string id, int tradeCount)
        {
            try
            {
                Console.WriteLine("Trade Reports:------------------------------------------------------------------");
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        public void OnDownloadTradesResult(TradeCapture client, object data, TradeTransactionReport tradeTransactionReport)
        {
            try
            {
                if (tradeTransactionReport.TradeTransactionReportType == TradeTransactionReportType.OrderFilled ||
                    tradeTransactionReport.TradeTransactionReportType == TradeTransactionReportType.PositionClosed)
                {
                    Console.WriteLine
                    (
                        "{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}@{9}, {10}",
                        tradeTransactionReport.TradeTransactionId,
                        tradeTransactionReport.TransactionTime,
                        tradeTransactionReport.TradeTransactionReportType,
                        tradeTransactionReport.TradeTransactionReason,
                        tradeTransactionReport.ClientId,
                        tradeTransactionReport.OrderType,
                        tradeTransactionReport.Symbol,
                        tradeTransactionReport.OrderSide,
                        tradeTransactionReport.OrderLastFillAmount,
                        tradeTransactionReport.OrderFillPrice,
                        tradeTransactionReport.Comment
                    );
                }
                else if (tradeTransactionReport.TradeTransactionReportType == TradeTransactionReportType.BalanceTransaction)
                {
                    Console.WriteLine
                    (
                        "{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}",
                        tradeTransactionReport.TradeTransactionId,
                        tradeTransactionReport.TransactionTime,
                        tradeTransactionReport.TradeTransactionReportType,
                        tradeTransactionReport.TradeTransactionReason,
                        tradeTransactionReport.Id,
                        tradeTransactionReport.AccountBalance,
                        tradeTransactionReport.TransactionAmount,
                        tradeTransactionReport.Commission,
                        tradeTransactionReport.Tax,
                        tradeTransactionReport.TaxValue,
                        tradeTransactionReport.Comment
                    );
                }
                else
                {
                    Console.WriteLine
                    (
                        "{0}, {1}, {2}, {3}",
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

        public void OnDownloadTradesResultEnd(TradeCapture client, object data)
        {
            try
            {
                Console.WriteLine("--------------------------------------------------------------------------------");
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        public void OnDownloadTradesError(TradeCapture client, object data, Exception error)
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

        void DownloadAccountReports(TimeDirection timeDirection, DateTime from, DateTime to)
        {
            client_.DownloadAccountReportsAsync(this, timeDirection, from, to);
        }

        public void OnDownloadAccountReportsResultBegin(TradeCapture client, object data, string id, int totalCount)
        {
            try
            {
                Console.WriteLine($"{totalCount} Account Reports ---------------------------------------------------");
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        public void OnDownloadAccountReportsResult(TradeCapture client, object data, AccountReport report)
        {
            try
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
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        public void OnDownloadAccountReportsResultEnd(TradeCapture client, object data)
        {
            try
            {
                Console.WriteLine("--------------------------------------------------------------------------------");
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        public void OnDownloadAccountReportsError(TradeCapture client, object data, Exception error)
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

        public void OnTradeUpdate(TradeCapture client, TradeTransactionReport tradeTransactionReport)
        {
            try
            {
                if (tradeTransactionReport.TradeTransactionReportType == TradeTransactionReportType.OrderFilled ||
                    tradeTransactionReport.TradeTransactionReportType == TradeTransactionReportType.PositionClosed)
                {
                    Console.WriteLine
                    (
                        "Trade update : {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}@{9}, {10}, {11}",
                        tradeTransactionReport.TradeTransactionId,
                        tradeTransactionReport.TransactionTime,
                        tradeTransactionReport.TradeTransactionReportType,
                        tradeTransactionReport.TradeTransactionReason,
                        tradeTransactionReport.Id,
                        tradeTransactionReport.OrderType,
                        tradeTransactionReport.Symbol,
                        tradeTransactionReport.OrderSide,
                        tradeTransactionReport.OrderLastFillAmount,
                        tradeTransactionReport.OrderFillPrice,
                        tradeTransactionReport.Commission,
                        tradeTransactionReport.Comment
                    );
                }
                else if (tradeTransactionReport.TradeTransactionReportType == TradeTransactionReportType.BalanceTransaction)
                {
                    Console.WriteLine
                    (
                        "Trade update : {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}",
                        tradeTransactionReport.TradeTransactionId,
                        tradeTransactionReport.TransactionTime,
                        tradeTransactionReport.TradeTransactionReportType,
                        tradeTransactionReport.TradeTransactionReason,
                        tradeTransactionReport.Id,
                        tradeTransactionReport.AccountBalance,
                        tradeTransactionReport.TransactionAmount,
                        tradeTransactionReport.Commission,
                        tradeTransactionReport.Tax,
                        tradeTransactionReport.TaxValue,
                        tradeTransactionReport.Comment
                    );
                }
                else
                {
                    Console.WriteLine
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
    }
}

// st "2017.01.01 0:0:0"
// dt Forward "2017.01.01 0:0:0" "2017.11.01 0:0:0"
// da Forward "2022.01.27 0:0:0" "2022.01.29 0:0:0"
