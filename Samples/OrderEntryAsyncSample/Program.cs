﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using NDesk.Options;
using TickTrader.FDK.Objects;
using TickTrader.FDK.OrderEntry;

namespace OrderEntryAsyncSample
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
                int port = 5040;

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
                    Console.Write("OrderEntryAsyncSample: ");
                    Console.WriteLine(e.Message);
                    Console.WriteLine("Try `OrderEntryAsyncSample --help' for more information.");
                    return;
                }

                if (help)
                {
                    Console.WriteLine("OrderEntryAsyncSample usage:");
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
            client_ = new Client("OrderEntryAsyncSample", port, true, "Logs", false);
            
            client_.ConnectEvent += new Client.ConnectDelegate(this.OnConnect);
            client_.ConnectErrorEvent += new Client.ConnectErrorDelegate(this.OnConnectError);
            client_.LoginResultEvent += new Client.LoginResultDelegate(this.OnLoginResult);
            client_.LoginErrorEvent += new Client.LoginErrorDelegate(this.OnLoginError);
            client_.LogoutResultEvent += new Client.LogoutResultDelegate(this.OnLogoutResult);
            client_.LogoutEvent += new Client.LogoutDelegate(this.OnLogout);
            client_.DisconnectEvent += new Client.DisconnectDelegate(this.OnDisconnect);
            client_.AccountInfoResultEvent += new Client.AccountInfoResultDelegate(this.OnAccountInfoResult);
            client_.AccountInfoErrorEvent += new Client.AccountInfoErrorDelegate(this.OnAccountInfoError);
            client_.SessionInfoResultEvent += new Client.SessionInfoResultDelegate(this.OnSessionInfoResult);
            client_.SessionInfoErrorEvent += new Client.SessionInfoErrorDelegate(this.OnSessionInfoError);
            client_.OrdersResultEvent += new Client.OrdersResultDelegate(this.OnOrdersResult);
            client_.OrdersErrorEvent += new Client.OrdersErrorDelegate(this.OnOrdersError);
            client_.PositionsResultEvent += new Client.PositionsResultDelegate(this.OnPositionsResult);
            client_.PositionsErrorEvent += new Client.PositionsErrorDelegate(this.OnPositionsError);
            client_.NewOrderResultEvent += new Client.NewOrderResultDelegate(this.OnNewOrderResult);
            client_.NewOrderErrorEvent += new Client.NewOrderErrorDelegate(this.OnNewOrderError);
            client_.ReplaceOrderResultEvent += new Client.ReplaceOrderResultDelegate(this.OnReplaceOrderResult);
            client_.ReplaceOrderErrorEvent += new Client.ReplaceOrderErrorDelegate(this.OnReplaceOrderError);
            client_.CancelOrderResultEvent += new Client.CancelOrderResultDelegate(this.OnCancelOrderResult);
            client_.CancelOrderErrorEvent += new Client.CancelOrderErrorDelegate(this.OnCancelOrderError);
            client_.ClosePositionResultEvent += new Client.ClosePositionResultDelegate(this.OnClosePositionResult);
            client_.ClosePositionErrorEvent += new Client.ClosePositionErrorDelegate(this.OnClosePositionError);
            client_.ExecutionReportEvent += new Client.ExecutionReportDelegate(this.OnExecutionReport);
            client_.PositionUpdateEvent += new Client.PositionUpdateDelegate(this.OnPositionUpdate);
            client_.AccountInfoUpdateEvent += new Client.AccountInfoUpdateDelegate(this.OnAccountInfoUpdate);
            client_.SessionInfoUpdateEvent += new Client.SessionInfoUpdateDelegate(this.OnSessionInfoUpdate);
            client_.NotificationEvent += new Client.NotificationDelegate(this.OnNotification);

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

                            NewOrderMarket
                            (
                                symbolId,
                                (OrderSide)Enum.Parse(typeof(OrderSide), side),
                                double.Parse(qty)
                            );
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

                            NewOrderLimit
                            (
                                symbolId,
                                (OrderSide)Enum.Parse(typeof(OrderSide), side),
                                double.Parse(qty),
                                double.Parse(price)
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

                            NewOrderStop
                            (
                                symbolId,
                                (OrderSide)Enum.Parse(typeof(OrderSide), side),
                                double.Parse(qty),
                                double.Parse(stopPrice)
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

                            NewOrderStopLimit
                            (
                                symbolId,
                                (OrderSide)Enum.Parse(typeof(OrderSide), side),
                                double.Parse(qty),
                                double.Parse(price),
                                double.Parse(stopPrice)
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

                            ReplaceOrderLimit
                            (
                                orderId,
                                symbolId,
                                (OrderSide)Enum.Parse(typeof(OrderSide), side),
                                double.Parse(qty),
                                double.Parse(price)
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

                            ReplaceOrderLimit
                            (
                                orderId,
                                symbolId,
                                (OrderSide)Enum.Parse(typeof(OrderSide), side),
                                double.Parse(qty),
                                double.Parse(price)
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

                            ReplaceOrderStop
                            (
                                orderId,
                                symbolId,
                                (OrderSide)Enum.Parse(typeof(OrderSide), side),
                                double.Parse(qty),
                                double.Parse(stopPrice)
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

                            ReplaceOrderStopLimit
                            (
                                orderId,
                                symbolId,
                                (OrderSide)Enum.Parse(typeof(OrderSide), side),
                                double.Parse(qty),
                                double.Parse(price),
                                double.Parse(stopPrice)
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

                client_.LoginAsync(null, login_, password_, "", "");
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
                client_.LogoutAsync(null, "Client logout");
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

        void PrintCommands()
        {
            Console.WriteLine("help (h) - print commands");
            Console.WriteLine("account_info (a) - request account information");
            Console.WriteLine("session_info (i) - request trading session information");
            Console.WriteLine("orders (o) - request list of orders");
            Console.WriteLine("positions (p) - request list of positions");
            Console.WriteLine("new_order_market (nom) <symbol_id> <side> <qty> - send new market order");
            Console.WriteLine("new_order_limit (nol) <symbol_id> <side> <qty> <price> - send new limit order");
            Console.WriteLine("new_order_stop (nos) <symbol_id> <side> <qty> <stop_price> - send new stop order");
            Console.WriteLine("new_order_stop_limit (nosl) <symbol_id> <side> <qty> <price> <stop_price> - send new stop limit order");
            Console.WriteLine("replace_order_limit (rol) <client_order_id> <symbol_id> <side> <qty> <price> - send limit order replace");
            Console.WriteLine("replace_order_stop (ros) <client_order_id> <symbol_id> <side> <qty> <stop_price> - send stop order replace");
            Console.WriteLine("replace_order_stop_limit (rosl) <client_order_id> <symbol_id> <side> <qty> <price> <stop_price> - send stop limit order replace");
            Console.WriteLine("cancel_order (co) <client_order_id> - send order cancel");
            Console.WriteLine("close_position (cp) <order_id> <qty> - send position close");
            Console.WriteLine("exit (e) - exit");
        }

        void GetAccountInfo()
        {
            client_.GetAccountInfoAsync(null);
        }

        void OnAccountInfoResult(Client client, object data, AccountInfo accountInfo)
        {
            try
            {
                Console.Error.WriteLine("Account : {0}, {1}, {2}, {3}, {4}, {5}, {6} ", accountInfo.AccountId, accountInfo.Type, accountInfo.Currency, accountInfo.Leverage, accountInfo.Balance, accountInfo.Margin, accountInfo.Equity);

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

        void OnAccountInfoError(Client client, object data, string message)
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

        void GetSessionInfo()
        {
            client_.GetSessionInfoAsync(null);
        }

        void OnSessionInfoResult(Client client, object data, SessionInfo sessionInfo)
        {
            try
            {
                Console.Error.WriteLine("Session info : {0}, {1}-{2}, {3}", sessionInfo.Status, sessionInfo.StartTime, sessionInfo.EndTime, sessionInfo.ServerTimeZoneOffset);

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

        void OnSessionInfoError(Client client, object data, string message)
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
        
        void GetOrders()
        {
            client_.GetOrdersAsync(null);
        }

        void OnOrdersResult(Client client, object data, ExecutionReport[] executionReports)
        {
            try
            {
                int count = executionReports.Length;

                Console.Error.WriteLine("Total orders : {0}", count);

                for (int index = 0; index < count; ++ index)
                {
                    ExecutionReport executionReport = executionReports[index];

                    if (executionReport.OrderType == OrderType.Stop)
                    {
                        Console.Error.WriteLine("    Order : {0}, {1}, {2}, {3} {4} {5} @@{6}, {7}, {8}@{9}", executionReport.OrigClientOrderId, executionReport.OrderId, executionReport.OrderType, executionReport.Symbol, executionReport.OrderSide, executionReport.InitialVolume, executionReport.StopPrice, executionReport.OrderStatus, executionReport.InitialVolume - executionReport.LeavesVolume, executionReport.AveragePrice);
                    }
                    else if (executionReport.OrderType == OrderType.StopLimit)
                    {
                        Console.Error.WriteLine("    Order : {0}, {1}, {2}, {3} {4} {5}@{6} @@{7}, {8}, {9}@{10}", executionReport.OrigClientOrderId, executionReport.OrderId, executionReport.OrderType, executionReport.Symbol, executionReport.OrderSide, executionReport.InitialVolume, executionReport.Price, executionReport.StopPrice, executionReport.OrderStatus, executionReport.InitialVolume - executionReport.LeavesVolume, executionReport.AveragePrice);
                    }
                    else
                        Console.Error.WriteLine("    Order : {0}, {1}, {2}, {3} {4} {5}@{6}, {7}, {8}@{9}", executionReport.OrigClientOrderId, executionReport.OrderId, executionReport.OrderType, executionReport.Symbol, executionReport.OrderSide, executionReport.InitialVolume, executionReport.Price, executionReport.OrderStatus, executionReport.InitialVolume - executionReport.LeavesVolume, executionReport.AveragePrice);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnOrdersError(Client client, object data, string message)
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

        void GetPositions()
        {
            client_.GetPositionsAsync(null);
        }

        void OnPositionsResult(Client client, object data, Position[] positions)
        {
            try
            {
                int count = positions.Length;

                Console.Error.WriteLine("Total positions : {0}", count);

                for (int index = 0; index < count; ++ index)
                {
                    Position position = positions[index];

                    double qty = position.BuyAmount != 0 ? position.BuyAmount : - position.SellAmount;
                    Console.Error.WriteLine("    Position : {0}, {1}", position.Symbol, qty);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnPositionsError(Client client, object data, string message)
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

        void NewOrderMarket(string symbolId, OrderSide side, double qty)
        {
            client_.NewOrderAsync(null, Guid.NewGuid().ToString(), symbolId,  OrderType.Market, side, qty, null, null, null, null, null, null, null, null, null, null);
        }

        void NewOrderLimit(string symbolId, OrderSide side, double qty, double price)
        {
            client_.NewOrderAsync(null, Guid.NewGuid().ToString(), symbolId,  OrderType.Limit, side, qty, null, price, null, OrderTimeInForce.GoodTillCancel, null, null, null, null, null, null);
        }

        void NewOrderStop(string symbolId, OrderSide side, double qty, double stopPrice)
        {
            client_.NewOrderAsync(null, Guid.NewGuid().ToString(), symbolId,  OrderType.Stop, side, qty, null, null, stopPrice, null, null, null, null, null, null, null);
        }

        void NewOrderStopLimit(string symbolId, OrderSide side, double qty, double price, double stopPrice)
        {
            client_.NewOrderAsync(null, Guid.NewGuid().ToString(), symbolId,  OrderType.StopLimit, side, qty, null, price, stopPrice, null, null, null, null, null, null, null);
        }

        void OnNewOrderResult(Client client, object data, ExecutionReport executionReport)
        {
            try
            {
                Console.WriteLine("Execution report : {0}, {1}, {2}, {3}, {4}", executionReport.ExecutionType, executionReport.OrigClientOrderId, executionReport.OrderId, executionReport.OrderType, executionReport.OrderStatus);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnNewOrderError(Client client, object data, string message)
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

        void ReplaceOrderLimit(string orderId, string symbolId, OrderSide side, double qty, double price)
        {
            client_.ReplaceOrderAsync(null, Guid.NewGuid().ToString(), orderId, null, symbolId, OrderType.Limit, side, qty, null, price, null, OrderTimeInForce.GoodTillCancel, null, null, null, null, null, null);
        }

        void ReplaceOrderStop(string orderId, string symbolId, OrderSide side, double qty, double stopPrice)
        {
            client_.ReplaceOrderAsync(null, Guid.NewGuid().ToString(), orderId, null, symbolId, OrderType.Stop, side, qty, null, null, stopPrice, OrderTimeInForce.GoodTillCancel, null, null, null, null, null, null);
        }

        void ReplaceOrderStopLimit(string orderId, string symbolId, OrderSide side, double qty, double price, double stopPrice)
        {
            client_.ReplaceOrderAsync(null, Guid.NewGuid().ToString(), orderId, null, symbolId, OrderType.Stop, side, qty, null, price, stopPrice, OrderTimeInForce.GoodTillCancel, null, null, null, null, null, null);
        }

        void OnReplaceOrderResult(Client client, object data, ExecutionReport executionReport)
        {
            try
            {
                Console.WriteLine("Execution report : {0}, {1}, {2}, {3}, {4}", executionReport.ExecutionType, executionReport.OrigClientOrderId, executionReport.OrderId, executionReport.OrderType, executionReport.OrderStatus);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnReplaceOrderError(Client client, object data, string message)
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

        void CancelOrder(string orderId)
        {
            client_.CancelOrderAsync(null, Guid.NewGuid().ToString(), orderId, null);
        }

        void OnCancelOrderResult(Client client, object data, ExecutionReport executionReport)
        {
            try
            {
                Console.WriteLine("Execution report : {0}, {1}, {2}, {3}, {4}", executionReport.ExecutionType, executionReport.OrigClientOrderId, executionReport.OrderId, executionReport.OrderType, executionReport.OrderStatus);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnCancelOrderError(Client client, object data, string message)
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

        void ClosePosition(string orderId, double qty)
        {
            client_.ClosePositionAsync(null, Guid.NewGuid().ToString(), orderId, qty);
        }

        void OnClosePositionResult(Client client, object data, ExecutionReport executionReport)
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

        void OnClosePositionError(Client client, object data, string message)
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

        void OnDisconnect(Client orderEntryClient, string text)
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

        void OnLogout(Client orderEntryClient, LogoutInfo info)
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

        void OnExecutionReport(Client orderEntryClient, ExecutionReport executionReport)
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

        void OnPositionUpdate(Client orderEntryClient, Position[] positions)
        {
            try
            {
                int count = positions.Length;
                for (int index = 0; index < count; ++ index)
                {
                    Position position = positions[index];

                    double qty = position.BuyAmount != 0 ? position.BuyAmount : - position.SellAmount;
                    Console.Error.WriteLine("Position report : {0}, {1}", position.Symbol, qty);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnAccountInfoUpdate(Client orderEntryClient, AccountInfo accountInfo)
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

        void OnSessionInfoUpdate(Client orderEntryClient, SessionInfo sessionInfo)
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

        void OnNotification(Client orderEntryClient, Notification notification)
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

        Client client_;

        string address_;
        string login_;
        string password_;
    }
}