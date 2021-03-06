﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using NDesk.Options;
using TickTrader.FDK.Common;
using TickTrader.FDK.Client;

namespace TradeCaptureSyncSample
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
                int port = 5044;

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
                    Console.Write("TradeCaptureSyncSample: ");
                    Console.WriteLine(e.Message);
                    Console.WriteLine("Try `TradeCaptureSyncSample --help' for more information.");
                    return;
                }

                if (help)
                {
                    Console.WriteLine("TradeCaptureSyncSample usage:");
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
            client_ = new TradeCapture("TradeCaptureSyncSample", port : port, reconnectAttempts : 0, logMessages : true,
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

                        if (command == "help" || command == "h")
                        {
                            PrintCommands();
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
            Console.WriteLine("subscribe_trades (st) <from> - subscribe to trades updates");
            Console.WriteLine("unsubscribe_trades (ut) - unsubscribe from trades updates");
            Console.WriteLine("download_trade_report (dt) <direction> <from> <to> - download trade reports");
            Console.WriteLine("download_account_reports (da) <direction> <from> <to> - download account reports");
            Console.WriteLine("exit (e) - exit");
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
                    AccountReport accountReport = downloadAccountReportsEnumerator.Next(-1);
                    accountReport != null;
                    accountReport = downloadAccountReportsEnumerator.Next(-1)
                )
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
    }
}

// st "2017.01.01 0:0:0"
// dt Forward "2017.01.01 0:0:0" "2017.11.01 0:0:0"
// da Forward "2017.01.01 0:0:0" "2017.11.01 0:0:0"
