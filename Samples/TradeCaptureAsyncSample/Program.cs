using System;
using System.Collections.Generic;
using System.Diagnostics;
using NDesk.Options;
using TickTrader.FDK.Common;
using TickTrader.FDK.TradeCapture;

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
            client_ = new Client("TradeCaptureAsyncSample", port, true, "Logs", false);

            client_.ConnectEvent += new Client.ConnectDelegate(this.OnConnect);
            client_.ConnectErrorEvent += new Client.ConnectErrorDelegate(this.OnConnectError);
            client_.LoginResultEvent += new Client.LoginResultDelegate(this.OnLoginResult);
            client_.LoginErrorEvent += new Client.LoginErrorDelegate(this.OnLoginError);
            client_.LogoutResultEvent += new Client.LogoutResultDelegate(this.OnLogoutResult);
            client_.LogoutEvent += new Client.LogoutDelegate(this.OnLogout);
            client_.DisconnectEvent += new Client.DisconnectDelegate(this.OnDisconnect);
            client_.SubscribeTradesResultEvent += new Client.SubscribeTradesResultDelegate(this.OnSubscribeTradesResult);
            client_.SubscribeTradesErrorEvent += new Client.SubscribeTradesErrorDelegate(this.OnSubscribeTradesError);
            client_.UnsubscribeTradesResultEvent += new Client.UnsubscribeTradesResultDelegate(this.OnSubscribeTradesResult);
            client_.UnsubscribeTradesErrorEvent += new Client.UnsubscribeTradesErrorDelegate(this.OnSubscribeTradesError);
            client_.TradeUpdateEvent += new Client.TradeUpdateDelegate(this.OnTradeUpdate);
            client_.DownloadTradesResultBeginEvent += new Client.DownloadTradesResultBeginDelegate(this.OnDownloadTradesResultBegin);
            client_.DownloadTradesResultEvent += new Client.DownloadTradesResultDelegate(this.OnDownloadTradesResult);
            client_.DownloadTradesResultEndEvent += new Client.DownloadTradesResultEndDelegate(this.OnDownloadTradesResultEnd);
            client_.DownloadTradesErrorEvent += new Client.DownloadTradesErrorDelegate(this.OnDownloadTradesError);

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
            client_.ConnectAsync(address_);
        }

        void OnConnect(Client client)
        {
            try
            {
                Console.WriteLine("Connected");

                client_.LoginAsync(this, login_, password_, "", "");
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
                client_.LogoutAsync(this, "Client logout");
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
            client_.SubscribeTradesAsync(this, false);
        }

        void UnsubscribeTrades()
        {
            client_.UnsubscribeTradesAsync(this);
        }

        void DownloadTrades(TimeDirection timeDirection, DateTime from, DateTime to)
        {
            client_.DownloadTradesAsync(this, timeDirection, from, to, false);
        }

        public void OnSubscribeTradesResult(Client client, object data)
        {
            try
            {
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        public void OnSubscribeTradesError(Client client, object data, string message)
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

        public void OnUnsubscribeTradesResult(Client client, object data)
        {
            try
            {
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        public void OnUnsubscribeTradesError(Client client, object data, string message)
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

        public void OnDownloadTradesResultBegin(Client client, object data)
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

        public void OnDownloadTradesResult(Client client, object data, TradeTransactionReport tradeTransactionReport)
        {
            try
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
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        public void OnDownloadTradesResultEnd(Client client, object data)
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

        public void OnDownloadTradesError(Client client, object data, string message)
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

        Client client_;

        string address_;
        string login_;
        string password_;
    }
}

// d Forward "2017.01.01 0:0:0" "2017.11.01 0:0:0"
