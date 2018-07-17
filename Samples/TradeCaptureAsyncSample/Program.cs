using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using NDesk.Options;
using TickTrader.FDK.Common;
using TickTrader.FDK.Client;

namespace TradeCaptureAsyncSample
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
                    Console.Write("TradeCaptureAsyncSample: ");
                    Console.WriteLine(e.Message);
                    Console.WriteLine("Try `TradeCaptureAsyncSample --help' for more information.");
                    return;
                }

                if (help)
                {
                    Console.WriteLine("TradeCaptureAsyncSample usage:");
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
            client_ = new TradeCapture("TradeCaptureAsyncSample", port : port, logMessages : true);

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

                        if (command == "help" || command == "h")
                        {
                            PrintCommands();
                        }
                        else if (command == "send_one_time_password" || command == "sotp")
                        {
                            string oneTimePassword = GetNextWord(line, ref pos);

                            SendOneTimePassword(oneTimePassword);
                        }
                        else if (command == "resume_one_time_password" || command == "rotp")
                        {
                            ResumeOneTimePassword();
                        }
                        else if (command == "subscribe_trades" || command == "st")
                        {
                            string from = GetNextWord(line, ref pos);

                            if (from == null)
                                throw new Exception("Invalid command : " + line);

                            SubscribeTrades(DateTime.Parse(from + "Z", null, DateTimeStyles.AdjustToUniversal));
                        }
                        else if (command == "unsubscribe_trades" || command == "ut")
                        {
                            UnsubscribeTrades();
                        }
                        else if (command == "download_trade_reports" || command == "dt")
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
                        else if (command == "download_account_reports" || command == "da")
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

        void OnConnectResult(TradeCapture client, object data)
        {
            try
            {
                Console.WriteLine("Connected");

                client_.LoginAsync(null, login_, password_, "", "", "");
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

                client_.DisconnectAsync(null, "Client disconnect");
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
                Console.WriteLine("Login succeeded");
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

                client_.DisconnectAsync(null, "Client disconnect");
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

                client_.DisconnectAsync(null, "Client disconnect");
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
            Console.WriteLine("send_one_time_password (sotp) <one_time_password> - send one time password");
            Console.WriteLine("resume_one_time_password (rotp) - resume one time password");
            Console.WriteLine("subscribe_trades (st) <from> - subscribe to trades updates");
            Console.WriteLine("unsubscribe_trades (ut) - unsubscribe from trades updates");
            Console.WriteLine("download_trade_reports (dt) <direction> <from> <to> - download trade reports");
            Console.WriteLine("download_account_reports (da) <direction> <from> <to> - download account reports");
            Console.WriteLine("exit (e) - exit");
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
                Console.Error.WriteLine("Subscribing");
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
                Console.Error.WriteLine("Subscribed");
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
                Console.Error.WriteLine("Unsubscribed");
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
                Console.Error.WriteLine("--------------------------------------------------------------------------------");
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
                    Console.Error.WriteLine
                    (
                        "Trade report : {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}@{9}, {10}",
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
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        public void OnDownloadTradesResultEnd(TradeCapture client, object data)
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
                Console.Error.WriteLine("--------------------------------------------------------------------------------");
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        public void OnDownloadAccountReportsResult(TradeCapture client, object data, AccountReport accountReport)
        {
            try
            {
                Console.Error.WriteLine
                (
                    "Account report : {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}",
                    accountReport.Timestamp,
                    accountReport.AccountId,
                    accountReport.Type,                    
                    accountReport.BalanceCurrency, 
                    accountReport.Leverage, 
                    accountReport.Balance, 
                    accountReport.Margin, 
                    accountReport.Equity
                );
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
                Console.Error.WriteLine("--------------------------------------------------------------------------------");
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
                    Console.Error.WriteLine
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
    }
}

// st "2017.01.01 0:0:0"
// dt Forward "2017.01.01 0:0:0" "2017.11.01 0:0:0"
// da Forward "2017.01.01 0:0:0" "2017.11.01 0:0:0"
