﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using NDesk.Options;
using TickTrader.FDK.Common;
using TickTrader.FDK.Client;
using System.Linq;
using SoftFX.Net.Core;

namespace OrderEntrySyncSample
{
    public class Program : IDisposable
    {
        static string SampleName = typeof(Program).Namespace;

        static void Main(string[] args)
        {
            try
            {
                string address = null;
                int port = 5043;
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
            client_ = new OrderEntry(SampleName, port : port, reconnectAttempts : 0, logMessages : true,
                validateClientCertificate: (sender, certificate, chain, errors) => true);

            client_.LogoutEvent += new OrderEntry.LogoutDelegate(this.OnLogout);
            client_.DisconnectEvent += new OrderEntry.DisconnectDelegate(this.OnDisconnect);
            client_.OrderUpdateEvent += new OrderEntry.OrderUpdateDelegate(this.OnOrderUpdate);
            client_.PositionUpdateEvent += new OrderEntry.PositionUpdateDelegate(this.OnPositionUpdate);
            client_.AccountInfoUpdateEvent += new OrderEntry.AccountInfoUpdateDelegate(this.OnAccountInfoUpdate);
            client_.SessionInfoUpdateEvent += new OrderEntry.SessionInfoUpdateDelegate(this.OnSessionInfoUpdate);
            client_.NotificationEvent += new OrderEntry.NotificationDelegate(this.OnNotification);

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

            int startIndex = index;

            while (index < line.Length && line[index] != ' ')
                ++ index;

            return line.Substring(startIndex, index - startIndex);
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
                        else if (command == "account_info" || command == "a")
                        {
                            GetAccountInfo();
                        }
                        else if (command == "session_info" || command == "i")
                        {
                            GetSessionInfo();
                        }
                        else if (command == "orders" || command == "o")
                        {
                            GetOrders();
                        }
                        else if (command == "positions" || command == "p")
                        {
                            GetPositions();
                        }
                        else if (command == "new_order_market" || command == "nom")
                        {
                            string symbolId = GetNextWord(line, ref pos);

                            if (symbolId == null)
                                throw new Exception("Invalid command : " + line);

                            string side = GetNextWord(line, ref pos);

                            if (side == null)
                                throw new Exception("Invalid command : " + line);

                            string qty = GetNextWord(line, ref pos);

                            if (qty == null)
                                throw new Exception("Invalid command : " + line);

                            string comment = GetNextWord(line, ref pos);

                            NewOrderMarket
                            (
                                symbolId,
                                (OrderSide)Enum.Parse(typeof(OrderSide), side),
                                double.Parse(qty),
                                comment
                            );
                        }
                        else if (command == "new_oco_orders" || command == "oco")
                        {
                            string symbolId = GetNextWord(line, ref pos);

                            if (symbolId == null)
                                throw new Exception("Invalid command : " + line);
                            string side1 = GetNextWord(line, ref pos);

                            if (side1 == null)
                                throw new Exception("Invalid command : " + line);
                            string qty1 = GetNextWord(line, ref pos);

                            if (qty1 == null)
                                throw new Exception("Invalid command : " + line);
                            string price1 = GetNextWord(line, ref pos);

                            if (price1 == null)
                                throw new Exception("Invalid command : " + line);
                            string stopprice1 = GetNextWord(line, ref pos);

                            if (stopprice1 == null)
                                throw new Exception("Invalid command : " + line);
                            string type1 = GetNextWord(line, ref pos);

                            if (type1 == null)
                                throw new Exception("Invalid command : " + line);
                            string side2 = GetNextWord(line, ref pos);

                            if (side2 == null)
                                throw new Exception("Invalid command : " + line);
                            string qty2 = GetNextWord(line, ref pos);

                            if (qty2 == null)
                                throw new Exception("Invalid command : " + line);
                            string price2 = GetNextWord(line, ref pos);

                            if (price2 == null)
                                throw new Exception("Invalid command : " + line);
                            string stopprice2 = GetNextWord(line, ref pos);

                            if (stopprice2 == null)
                                throw new Exception("Invalid command : " + line);
                            string type2 = GetNextWord(line, ref pos);

                            if (type2 == null)
                                throw new Exception("Invalid command : " + line);
                            NewOcoOrders(symbolId, 
                                (OrderSide)Enum.Parse(typeof(OrderSide), side1), 
                                double.Parse(qty1), 
                                double.Parse(price1),
                                double.Parse(stopprice1),
                                (OrderType)Enum.Parse(typeof(OrderType), type1), 
                                (OrderSide)Enum.Parse(typeof(OrderSide), side2), 
                                double.Parse(qty2), 
                                double.Parse(price2), 
                                double.Parse(stopprice2), 
                                (OrderType)Enum.Parse(typeof(OrderType), type2));
                        }
                        else if (command == "new_order_limit" || command == "nol")
                        {
                            string symbolId = GetNextWord(line, ref pos);

                            if (symbolId == null)
                                throw new Exception("Invalid command : " + line);

                            string side = GetNextWord(line, ref pos);

                            if (side == null)
                                throw new Exception("Invalid command : " + line);

                            string qty = GetNextWord(line, ref pos);

                            if (qty == null)
                                throw new Exception("Invalid command : " + line);

                            string price = GetNextWord(line, ref pos);

                            if (price == null)
                                throw new Exception("Invalid command : " + line);

                            string comment = GetNextWord(line, ref pos);

                            NewOrderLimit
                            (
                                symbolId,
                                (OrderSide)Enum.Parse(typeof(OrderSide), side),
                                double.Parse(qty),
                                double.Parse(price),
                                comment
                            );
                        }
                        else if (command == "new_order_stop" || command == "nos")
                        {
                            string symbolId = GetNextWord(line, ref pos);

                            if (symbolId == null)
                                throw new Exception("Invalid command : " + line);

                            string side = GetNextWord(line, ref pos);

                            if (side == null)
                                throw new Exception("Invalid command : " + line);

                            string qty = GetNextWord(line, ref pos);

                            if (qty == null)
                                throw new Exception("Invalid command : " + line);

                            string stopPrice = GetNextWord(line, ref pos);

                            if (stopPrice == null)
                                throw new Exception("Invalid command : " + line);

                            string comment = GetNextWord(line, ref pos);

                            NewOrderStop
                            (
                                symbolId,
                                (OrderSide)Enum.Parse(typeof(OrderSide), side),
                                double.Parse(qty),
                                double.Parse(stopPrice),
                                comment
                            );
                        }
                        else if (command == "new_order_stop_limit" || command == "nosl")
                        {
                            string symbolId = GetNextWord(line, ref pos);

                            if (symbolId == null)
                                throw new Exception("Invalid command : " + line);

                            string side = GetNextWord(line, ref pos);

                            if (side == null)
                                throw new Exception("Invalid command : " + line);

                            string qty = GetNextWord(line, ref pos);

                            if (qty == null)
                                throw new Exception("Invalid command : " + line);

                            string price = GetNextWord(line, ref pos);

                            if (price == null)
                                throw new Exception("Invalid command : " + line);

                            string stopPrice = GetNextWord(line, ref pos);

                            if (stopPrice == null)
                                throw new Exception("Invalid command : " + line);

                            string comment = GetNextWord(line, ref pos);

                            NewOrderStopLimit
                            (
                                symbolId,
                                (OrderSide)Enum.Parse(typeof(OrderSide), side),
                                double.Parse(qty),
                                double.Parse(price),
                                double.Parse(stopPrice),
                                comment
                            );
                        }
                        else if (command == "replace_order_limit" || command == "rol")
                        {
                            string orderId = GetNextWord(line, ref pos);

                            if (orderId == null)
                                throw new Exception("Invalid command : " + line);

                            string symbolId = GetNextWord(line, ref pos);

                            if (symbolId == null)
                                throw new Exception("Invalid command : " + line);

                            string side = GetNextWord(line, ref pos);

                            if (side == null)
                                throw new Exception("Invalid command : " + line);

                            string qty = GetNextWord(line, ref pos);

                            if (qty == null)
                                throw new Exception("Invalid command : " + line);

                            string price = GetNextWord(line, ref pos);

                            if (price == null)
                                throw new Exception("Invalid command : " + line);

                            string comment = GetNextWord(line, ref pos);

                            ReplaceOrderLimit
                            (
                                orderId,
                                symbolId,
                                (OrderSide)Enum.Parse(typeof(OrderSide), side),
                                double.Parse(qty),
                                double.Parse(price),
                                comment
                            );
                        }
                        else if (command == "replace_order_limit" || command == "rol")
                        {
                            string orderId = GetNextWord(line, ref pos);

                            if (orderId == null)
                                throw new Exception("Invalid command : " + line);

                            string symbolId = GetNextWord(line, ref pos);

                            if (symbolId == null)
                                throw new Exception("Invalid command : " + line);

                            string side = GetNextWord(line, ref pos);

                            if (side == null)
                                throw new Exception("Invalid command : " + line);

                            string qty = GetNextWord(line, ref pos);

                            if (qty == null)
                                throw new Exception("Invalid command : " + line);

                            string price = GetNextWord(line, ref pos);

                            if (price == null)
                                throw new Exception("Invalid command : " + line);

                            string comment = GetNextWord(line, ref pos);

                            ReplaceOrderLimit
                            (
                                orderId,
                                symbolId,
                                (OrderSide)Enum.Parse(typeof(OrderSide), side),
                                double.Parse(qty),
                                double.Parse(price),
                                comment
                            );
                        }
                        else if (command == "replace_order_stop" || command == "ros")
                        {
                            string orderId = GetNextWord(line, ref pos);

                            if (orderId == null)
                                throw new Exception("Invalid command : " + line);

                            string symbolId = GetNextWord(line, ref pos);

                            if (symbolId == null)
                                throw new Exception("Invalid command : " + line);

                            string side = GetNextWord(line, ref pos);

                            if (side == null)
                                throw new Exception("Invalid command : " + line);

                            string qty = GetNextWord(line, ref pos);

                            if (qty == null)
                                throw new Exception("Invalid command : " + line);

                            string stopPrice = GetNextWord(line, ref pos);

                            if (stopPrice == null)
                                throw new Exception("Invalid command : " + line);

                            string comment = GetNextWord(line, ref pos);

                            ReplaceOrderStop
                            (
                                orderId,
                                symbolId,
                                (OrderSide)Enum.Parse(typeof(OrderSide), side),
                                double.Parse(qty),
                                double.Parse(stopPrice),
                                comment
                            );
                        }
                        else if (command == "replace_order_stop_limit" || command == "rosl")
                        {
                            string orderId = GetNextWord(line, ref pos);

                            if (orderId == null)
                                throw new Exception("Invalid command : " + line);

                            string symbolId = GetNextWord(line, ref pos);

                            if (symbolId == null)
                                throw new Exception("Invalid command : " + line);

                            string side = GetNextWord(line, ref pos);

                            if (side == null)
                                throw new Exception("Invalid command : " + line);

                            string qty = GetNextWord(line, ref pos);

                            if (qty == null)
                                throw new Exception("Invalid command : " + line);

                            string price = GetNextWord(line, ref pos);

                            if (price == null)
                                throw new Exception("Invalid command : " + line);

                            string stopPrice = GetNextWord(line, ref pos);

                            if (stopPrice == null)
                                throw new Exception("Invalid command : " + line);

                            string comment = GetNextWord(line, ref pos);

                            ReplaceOrderStopLimit
                            (
                                orderId,
                                symbolId,
                                (OrderSide)Enum.Parse(typeof(OrderSide), side),
                                double.Parse(qty),
                                double.Parse(price),
                                double.Parse(stopPrice),
                                comment
                            );
                        }
                        else if (command == "cancel_order" || command == "co")
                        {
                            string orderId = GetNextWord(line, ref pos);

                            if (orderId == null)
                                throw new Exception("Invalid command : " + line);

                            CancelOrder(orderId);
                        }
                        else if (command == "close_position" || command == "cp")
                        {
                            string orderId = GetNextWord(line, ref pos);

                            if (orderId == null)
                                throw new Exception("Invalid command : " + line);

                            string qty = GetNextWord(line, ref pos);

                            if (qty == null)
                                throw new Exception("Invalid command : " + line);

                            ClosePosition(orderId, double.Parse(qty));
                        }
                        else if (command == "split_list" || command == "sl")
                        {
                            GetSplits();
                        }
                        else if (command == "dividend_list" || command == "dl")
                        {
                            GetDividends();
                        }
                        else if (command == "mergersandacquisitions" || command == "ma")
                        {
                            GetMergersAndAcquisitions();
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

                SessionInfo info =  client_.GetSessionInfo(Timeout);
            }
            catch
            {
                string text = client_.Disconnect(Reason.ClientError("Client disconnect"));

                if (text != null)
                    Console.WriteLine("Disconnected : " + text);

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
            Console.WriteLine("account_info (a) - request account information");
            Console.WriteLine("session_info (i) - request trading session information");
            Console.WriteLine("orders (o) - request list of orders");
            Console.WriteLine("positions (p) - request list of positions");
            Console.WriteLine("new_order_market (nom) <symbol_id> <side> <qty> [<comment>] - send new market order");
            Console.WriteLine("new_order_limit (nol) <symbol_id> <side> <qty> <price> [<comment>] - send new limit order");
            Console.WriteLine("new_order_stop (nos) <symbol_id> <side> <qty> <stop_price> [<comment>] - send new stop order");
            Console.WriteLine("new_order_stop_limit (nosl) <symbol_id> <side> <qty> <price> <stop_price> [<comment>] - send new stop limit order");
            Console.WriteLine("replace_order_limit (rol) <client_order_id> <symbol_id> <side> <qty> <price> [<comment>] - send limit order replace");
            Console.WriteLine("replace_order_stop (ros) <client_order_id> <symbol_id> <side> <qty> <stop_price> [<comment>] - send stop order replace");
            Console.WriteLine("replace_order_stop_limit (rosl) <client_order_id> <symbol_id> <side> <qty> <price> <stop_price> [<comment>] - send stop limit order replace");
            Console.WriteLine("cancel_order (co) <client_order_id> - send order cancel");
            Console.WriteLine("close_position (cp) <order_id> <qty> - send position close");
            Console.WriteLine("split_list (sl) - get account split list");
            Console.WriteLine("dividend_list (dl) - get account dividend list");
            Console.WriteLine("mergersandacquisitions (ma) - request list of mergers and acquisitions");
            Console.WriteLine("exit (e) - exit");
        }

        void GetAccountInfo()
        {
            AccountInfo accountInfo = client_.GetAccountInfo(Timeout);

            if (accountInfo.Type == AccountType.Cash)
            {
                Console.Error.WriteLine("Account : {0}, {1}", accountInfo.AccountId, accountInfo.Type);

                AssetInfo[] accountAssets = accountInfo.Assets;

                int count = accountAssets.Length;
                for (int index = 0; index < count; ++index)
                {
                    AssetInfo accountAsset = accountAssets[index];

                    Console.Error.WriteLine("    Asset : {0}, {1}, {2}", accountAsset.Currency, accountAsset.Balance, accountAsset.LockedAmount);
                }
            }
            else
                Console.Error.WriteLine("Account : {0}, {1}, {2}, {3}, {4}, {5}, {6}", accountInfo.AccountId, accountInfo.Type, accountInfo.Currency, accountInfo.Leverage, accountInfo.Balance, accountInfo.Margin, accountInfo.Equity);
        }

        void GetSessionInfo()
        {
            SessionInfo sessionInfo = client_.GetSessionInfo(Timeout);

            Console.Error.WriteLine("Session info : {0}, {1}-{2}, {3}", sessionInfo.Status, sessionInfo.StartTime, sessionInfo.EndTime, sessionInfo.ServerTimeZoneOffset);

            StatusGroupInfo[] groups = sessionInfo.StatusGroups;

            int count = groups.Length;
            for (int index = 0; index < count; ++index)
            {
                StatusGroupInfo group = groups[index];

                Console.Error.WriteLine("Session status group : {0}, {1}, {2}-{3}", group.StatusGroupId, group.Status, group.StartTime, group.EndTime);
            }
        }

        void GetOrders()
        {
            GetOrdersEnumerator enumerator = client_.GetOrders(Timeout);

            try
            {
                Console.Error.WriteLine("Total orders : {0}", enumerator.TotalCount);

                for (ExecutionReport executionReport = enumerator.Next(Timeout); executionReport != null; executionReport = enumerator.Next(Timeout))
                {
                    if (executionReport.OrderType == OrderType.Stop)
                    {
                        Console.Error.WriteLine("    Order : {0}, {1}, {2}, {3} {4} {5} @@{6}, {7}, {8}, \"{9}\"", executionReport.OrigClientOrderId, executionReport.OrderId, executionReport.OrderType, executionReport.Symbol, executionReport.OrderSide, executionReport.InitialVolume, executionReport.StopPrice, executionReport.OrderStatus, executionReport.LeavesVolume, executionReport.Comment);
                    }
                    else if (executionReport.OrderType == OrderType.StopLimit)
                    {
                        Console.Error.WriteLine("    Order : {0}, {1}, {2}, {3} {4} {5}@{6} @@{7}, {8}, {9}, \"{10}\"", executionReport.OrigClientOrderId, executionReport.OrderId, executionReport.OrderType, executionReport.Symbol, executionReport.OrderSide, executionReport.InitialVolume, executionReport.Price, executionReport.StopPrice, executionReport.OrderStatus, executionReport.LeavesVolume, executionReport.Comment);
                    }
                    else
                        Console.Error.WriteLine("    Order : {0}, {1}, {2}, {3} {4} {5}@{6}, {7}, {8}, \"{9}\"", executionReport.OrigClientOrderId, executionReport.OrderId, executionReport.OrderType, executionReport.Symbol, executionReport.OrderSide, executionReport.InitialVolume, executionReport.Price, executionReport.OrderStatus, executionReport.LeavesVolume, executionReport.Comment);
                }
            }
            finally
            {
                enumerator.Close();
            }
        }

        void GetPositions()
        {
            Position[] positions = client_.GetPositions(Timeout);

            int count = positions.Length;

            Console.Error.WriteLine("Total positions : {0}", count);

            for (int index = 0; index < count; ++ index)
            {
                Position position = positions[index];

                double qty = position.BuyAmount != 0 ? position.BuyAmount : - position.SellAmount;
                Console.Error.WriteLine("    Position : {0}, {1}", position.Symbol, qty);
            }
        }

        void GetSplits()
        {
            Split[] splits = client_.GetSplitList(Timeout);

            int count = splits.Length;

            Console.Error.WriteLine("Total splits : {0}", count);

            for (int index = 0; index < count; ++index)
            {
                Split split = splits[index];

                Console.WriteLine($"{split.Id}\t{split.StartTime}\t Ratio={split.Ratio}\tSymbols=[{string.Join(",", split.Symbols.ToArray())}]\tCurrencies=[{string.Join(",", split.Currencies.ToArray())}]");
            }
        }

        void GetDividends()
        {
            Dividend[] dividends = client_.GetDividendList(Timeout);

            int count = dividends.Length;

            Console.Error.WriteLine("Total dividends : {0}", count);

            for (int index = 0; index < count; ++index)
            {
                Dividend dividend = dividends[index];

                Console.WriteLine($"{dividend.Id}\t{dividend.Time}\t GrossRate={dividend.GrossRate}\tSymbol={dividend.Symbol}");
            }
        }

        void GetMergersAndAcquisitions()
        {
            MergerAndAcquisition[] mergersAndAcquisitions = client_.GetMergerAndAcquisitionList(Timeout);

            int count = mergersAndAcquisitions.Length;

            Console.Error.WriteLine("Total mergersAndAcquisitions : {0}", count);

            for (int index = 0; index < count; ++index)
            {
                MergerAndAcquisition mergerAndAcquisition = mergersAndAcquisitions[index];

                Console.WriteLine($"Id={mergerAndAcquisition.Id}\t{string.Join("\t", mergerAndAcquisition.Values.Select(it => $"{it.Key}={it.Value}"))}");
            }
        }

        void NewOrderMarket(string symbolId, OrderSide side, double qty, string comment)
        {
            var newOrder = OrderEntry.CreateNewOrderRequest(Guid.NewGuid().ToString(), symbolId, OrderType.Market, side, qty)
                .WithComment(comment);
            var executionReports = newOrder.Sync(Timeout).Send(client_);

            foreach (ExecutionReport executionReport in executionReports)
                Console.WriteLine("Execution report : {0}, {1}, {2}, {3}, {4}", executionReport.ExecutionType, executionReport.ClientOrderId, executionReport.OrderId, executionReport.OrderType, executionReport.OrderStatus);
        }

        void NewOcoOrders(string symbolId, OrderSide side1, double qty1, double? price1, double? stopprice1, OrderType type1, OrderSide side2, double qty2, double? price2, double? stopprice2, OrderType type2)
        {
            var ocoOrdersRequest = OrderEntry.CreateNewOcoOrdersRequest(Guid.NewGuid().ToString(), symbolId)
                .WithFirst(Guid.NewGuid().ToString(), type1, side1, qty1)
                .WithSecond(Guid.NewGuid().ToString(), type2, side2, qty2);
            if (price1.HasValue) ocoOrdersRequest.FirstWithPrice(price1.Value);
            if (stopprice1.HasValue) ocoOrdersRequest.FirstWithStopPrice(stopprice1.Value);
            if (price2.HasValue) ocoOrdersRequest.SecondWithPrice(price2.Value);
            if (stopprice2.HasValue) ocoOrdersRequest.SecondWithPrice(stopprice2.Value);

            var executionReports = ocoOrdersRequest.Sync(Timeout).Send(client_);

            foreach (ExecutionReport executionReport in executionReports)
                Console.WriteLine("Execution report : {0}, {1}, {2}, {3}, {4}", executionReport.ExecutionType, executionReport.ClientOrderId, executionReport.OrderId, executionReport.OrderType, executionReport.OrderStatus);
        }

        void NewOrderLimit(string symbolId, OrderSide side, double qty, double price, string comment)
        {
            var newOrder = OrderEntry.CreateNewOrderRequest(Guid.NewGuid().ToString(), symbolId, OrderType.Limit, side, qty)
                .WithPrice(price).WithComment(comment);
            var executionReports = newOrder.Sync(Timeout).Send(client_);

            foreach (ExecutionReport executionReport in executionReports)
                Console.WriteLine("Execution report : {0}, {1}, {2}, {3}, {4}", executionReport.ExecutionType, executionReport.ClientOrderId, executionReport.OrderId, executionReport.OrderType, executionReport.OrderStatus);
        }

        void NewOrderStop(string symbolId, OrderSide side, double qty, double stopPrice, string comment)
        {
            var newOrder = OrderEntry.CreateNewOrderRequest(Guid.NewGuid().ToString(), symbolId, OrderType.Stop, side, qty)
                .WithStopPrice(stopPrice).WithComment(comment);
            var executionReports = newOrder.Sync(Timeout).Send(client_);

            foreach (ExecutionReport executionReport in executionReports)
                Console.WriteLine("Execution report : {0}, {1}, {2}, {3}, {4}", executionReport.ExecutionType, executionReport.ClientOrderId, executionReport.OrderId, executionReport.OrderType, executionReport.OrderStatus);
        }

        void NewOrderStopLimit(string symbolId, OrderSide side, double qty, double price, double stopPrice, string comment)
        {
            var newOrder = OrderEntry.CreateNewOrderRequest(Guid.NewGuid().ToString(), symbolId, OrderType.StopLimit, side, qty)
                .WithPrice(price).WithStopPrice(stopPrice).WithComment(comment);
            var executionReports = newOrder.Sync(Timeout).Send(client_);

            foreach (ExecutionReport executionReport in executionReports)
                Console.WriteLine("Execution report : {0}, {1}, {2}, {3}, {4}", executionReport.ExecutionType, executionReport.ClientOrderId, executionReport.OrderId, executionReport.OrderType, executionReport.OrderStatus);
        }

        void ReplaceOrderLimit(string orderId, string symbolId, OrderSide side, double qty, double price, string comment)
        {
            var replaceOrder = OrderEntry
                .CreateReplaceOrderRequest(Guid.NewGuid().ToString(), symbolId, OrderType.Limit, side)
                .WithQtyChange(qty).WithPrice(price).WithComment(comment);
            var executionReports = replaceOrder.Sync(Timeout).Send(client_);

            foreach (ExecutionReport executionReport in executionReports)
                Console.WriteLine("Execution report : {0}, {1}, {2}, {3}, {4}, {5}", executionReport.ExecutionType, executionReport.ClientOrderId, executionReport.OrigClientOrderId, executionReport.OrderId, executionReport.OrderType, executionReport.OrderStatus);
        }

        void ReplaceOrderStop(string orderId, string symbolId, OrderSide side, double qty, double stopPrice, string comment)
        {
            var replaceOrder = OrderEntry
                .CreateReplaceOrderRequest(Guid.NewGuid().ToString(), symbolId, OrderType.Stop, side)
                .WithQtyChange(qty).WithStopPrice(stopPrice).WithComment(comment);
            var executionReports = replaceOrder.Sync(Timeout).Send(client_);

            foreach (ExecutionReport executionReport in executionReports)
                Console.WriteLine("Execution report : {0}, {1}, {2}, {3}, {4}, {5}", executionReport.ExecutionType, executionReport.ClientOrderId, executionReport.OrigClientOrderId, executionReport.OrderId, executionReport.OrderType, executionReport.OrderStatus);
        }

        void ReplaceOrderStopLimit(string orderId, string symbolId, OrderSide side, double qty, double price, double stopPrice, string comment)
        {
            var replaceOrder = OrderEntry
                .CreateReplaceOrderRequest(Guid.NewGuid().ToString(), symbolId, OrderType.StopLimit, side)
                .WithQtyChange(qty).WithPrice(price).WithStopPrice(stopPrice).WithComment(comment);
            var executionReports = replaceOrder.Sync(Timeout).Send(client_);

            foreach (ExecutionReport executionReport in executionReports)
                Console.WriteLine("Execution report : {0}, {1}, {2}, {3}, {4}, {5}", executionReport.ExecutionType, executionReport.ClientOrderId, executionReport.OrigClientOrderId, executionReport.OrderId, executionReport.OrderType, executionReport.OrderStatus);
        }

        void CancelOrder(string orderId)
        {
            var cancelOrderRequest = OrderEntry.CreateCancelOrderRequest(Guid.NewGuid().ToString()).WithOrderId(long.Parse(orderId));
            var executionReports = cancelOrderRequest.Sync(Timeout).Send(client_);

            foreach (ExecutionReport executionReport in executionReports)
                Console.WriteLine("Execution report : {0}, {1}, {2}, {2}, {3}, {4}", executionReport.ExecutionType, executionReport.ClientOrderId, executionReport.OrigClientOrderId, executionReport.OrderId, executionReport.OrderType, executionReport.OrderStatus);
        }

        void ClosePosition(string orderId, double qty)
        {
            var closePositionRequest = OrderEntry.CreateClosePositionRequest(Guid.NewGuid().ToString()).WithPositionId(long.Parse(orderId))
                .WithQty(qty);
            var executionReports = closePositionRequest.Sync(Timeout).Send(client_);

            foreach (ExecutionReport executionReport in executionReports)
            {
                if (executionReport.ExecutionType == ExecutionType.Trade)
                {
                    Console.WriteLine("Execution report : {0}, {1}, {2}, {3}, {4}, {5}@{6}", executionReport.ExecutionType, executionReport.ClientOrderId, executionReport.OrderId, executionReport.OrderType, executionReport.OrderStatus, executionReport.TradeAmount, executionReport.TradePrice);
                }
                else
                    Console.WriteLine("Execution report : {0}, {1}, {2}, {3}, {4}", executionReport.ExecutionType, executionReport.ClientOrderId, executionReport.OrderId, executionReport.OrderType, executionReport.OrderStatus);
            }
        }

        void OnLogout(OrderEntry client, LogoutInfo info)
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

        void OnDisconnect(OrderEntry client, string text)
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

        void OnOrderUpdate(OrderEntry client, ExecutionReport executionReport)
        {
            try
            {
                if (executionReport.ExecutionType == ExecutionType.Trade)
                {
                    Console.WriteLine("Execution report : {0}, {1}, {2}, {3}, {4}, {5}@{6}", executionReport.ExecutionType, executionReport.OrigClientOrderId, executionReport.OrderId, executionReport.OrderType, executionReport.OrderStatus, executionReport.TradeAmount, executionReport.TradePrice);
                }
                else
                    Console.WriteLine("Execution report : {0}, {1}, {2}, {3}, {4}", executionReport.ExecutionType, executionReport.OrigClientOrderId, executionReport.OrderId, executionReport.OrderType, executionReport.OrderStatus);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnPositionUpdate(OrderEntry client, Position position)
        {
            try
            {
                double qty = position.BuyAmount != 0 ? position.BuyAmount : - position.SellAmount;
                Console.Error.WriteLine("Position report : {0}, {1}", position.Symbol, qty);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnAccountInfoUpdate(OrderEntry client, AccountInfo accountInfo)
        {
            try
            {
                Console.Error.WriteLine("Account update : {0}, {1}, {2}, {3}, {4}, {5}, {6} ", accountInfo.AccountId, accountInfo.Type, accountInfo.Currency, accountInfo.Leverage, accountInfo.Balance, accountInfo.Margin, accountInfo.Equity);

                AssetInfo[] accountAssets = accountInfo.Assets;

                int count = accountAssets.Length;
                for (int index = 0; index < count; ++index)
                {
                    AssetInfo accountAsset = accountAssets[index];

                    Console.Error.WriteLine("    Asset : {0}, {1}, {2}", accountAsset.Currency, accountAsset.Balance, accountAsset.LockedAmount);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnSessionInfoUpdate(OrderEntry client, SessionInfo sessionInfo)
        {
            try
            {
                Console.Error.WriteLine("Session info update : {0}, {1}-{2}, {3}", sessionInfo.Status, sessionInfo.StartTime, sessionInfo.EndTime, sessionInfo.ServerTimeZoneOffset);

                StatusGroupInfo[] groups = sessionInfo.StatusGroups;

                int count = groups.Length;
                for (int index = 0; index < count; ++index)
                {
                    StatusGroupInfo group = groups[index];

                    Console.Error.WriteLine("Session status group : {0}, {1}, {2}-{3}", group.StatusGroupId, group.Status, group.StartTime, group.EndTime);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnNotification(OrderEntry client, Notification notification)
        {
            try
            {
                Console.Error.WriteLine("Notification : {0}, {1}, {2}", notification.Type, notification.Severity, notification.Message);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        OrderEntry client_;

        string address_;
        string login_;
        string password_;
        const int Timeout = 30000;
    }
}
