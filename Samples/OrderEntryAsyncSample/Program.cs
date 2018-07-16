using System;
using System.Collections.Generic;
using System.Diagnostics;
using NDesk.Options;
using TickTrader.FDK.Common;
using TickTrader.FDK.Client;

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
            client_ = new OrderEntry("OrderEntryAsyncSample", port : port, logMessages : true);
            
            client_.ConnectResultEvent += new OrderEntry.ConnectResultDelegate(this.OnConnectResult);
            client_.ConnectErrorEvent += new OrderEntry.ConnectErrorDelegate(this.OnConnectError);
            client_.DisconnectResultEvent += new OrderEntry.DisconnectResultDelegate(this.OnDisconnectResult);
            client_.DisconnectEvent += new OrderEntry.DisconnectDelegate(this.OnDisconnect);
            client_.ReconnectEvent += new OrderEntry.ReconnectDelegate(this.OnReconnect);
            client_.ReconnectErrorEvent += new OrderEntry.ReconnectErrorDelegate(this.OnReconnectError);
            client_.LoginResultEvent += new OrderEntry.LoginResultDelegate(this.OnLoginResult);
            client_.LoginErrorEvent += new OrderEntry.LoginErrorDelegate(this.OnLoginError);
            client_.LogoutResultEvent += new OrderEntry.LogoutResultDelegate(this.OnLogoutResult);
            client_.LogoutErrorEvent += new OrderEntry.LogoutErrorDelegate(this.OnLogoutError);
            client_.LogoutEvent += new OrderEntry.LogoutDelegate(this.OnLogout);            
            client_.AccountInfoResultEvent += new OrderEntry.AccountInfoResultDelegate(this.OnAccountInfoResult);
            client_.AccountInfoErrorEvent += new OrderEntry.AccountInfoErrorDelegate(this.OnAccountInfoError);
            client_.SessionInfoResultEvent += new OrderEntry.SessionInfoResultDelegate(this.OnSessionInfoResult);
            client_.SessionInfoErrorEvent += new OrderEntry.SessionInfoErrorDelegate(this.OnSessionInfoError);
            client_.OrdersBeginResultEvent += new OrderEntry.OrdersBeginResultDelegate(this.OnOrdersBeginResult);
            client_.OrdersResultEvent += new OrderEntry.OrdersResultDelegate(this.OnOrdersResult);
            client_.OrdersErrorEvent += new OrderEntry.OrdersErrorDelegate(this.OnOrdersError);
            client_.PositionsResultEvent += new OrderEntry.PositionsResultDelegate(this.OnPositionsResult);
            client_.PositionsErrorEvent += new OrderEntry.PositionsErrorDelegate(this.OnPositionsError);
            client_.NewOrderResultEvent += new OrderEntry.NewOrderResultDelegate(this.OnNewOrderResult);
            client_.NewOrderErrorEvent += new OrderEntry.NewOrderErrorDelegate(this.OnNewOrderError);
            client_.ReplaceOrderResultEvent += new OrderEntry.ReplaceOrderResultDelegate(this.OnReplaceOrderResult);
            client_.ReplaceOrderErrorEvent += new OrderEntry.ReplaceOrderErrorDelegate(this.OnReplaceOrderError);
            client_.CancelOrderResultEvent += new OrderEntry.CancelOrderResultDelegate(this.OnCancelOrderResult);
            client_.CancelOrderErrorEvent += new OrderEntry.CancelOrderErrorDelegate(this.OnCancelOrderError);
            client_.ClosePositionResultEvent += new OrderEntry.ClosePositionResultDelegate(this.OnClosePositionResult);
            client_.ClosePositionErrorEvent += new OrderEntry.ClosePositionErrorDelegate(this.OnClosePositionError);            
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

        void OnConnectResult(OrderEntry client, object data)
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

        void OnConnectError(OrderEntry client, object data, Exception error)
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

        void OnDisconnectResult(OrderEntry client, object data, string text)
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

        void OnDisconnect(OrderEntry client, string text)
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

        void OnReconnect(OrderEntry client)
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

        void OnReconnectError(OrderEntry client, Exception error)
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

        void OnLoginResult(OrderEntry client, object data)
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

        void OnLoginError(OrderEntry client, object data, Exception error)
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

        void OnLogoutResult(OrderEntry client, object data, LogoutInfo logoutInfo)
        {
            try
            {
                Console.WriteLine("Logout : " + logoutInfo.Message);

                client_.DisconnectAsync(null, "Client disconnect");
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnLogoutError(OrderEntry client, object data, Exception error)
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

        void OnLogout(OrderEntry client, LogoutInfo logoutInfo)
        {
            try
            {
                Console.WriteLine("Logout : " + logoutInfo.Message);

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
            Console.WriteLine("exit (e) - exit");
        }

        void GetAccountInfo()
        {
            client_.GetAccountInfoAsync(this);
        }

        void OnAccountInfoResult(OrderEntry client, object data, AccountInfo accountInfo)
        {
            try
            {
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
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnAccountInfoError(OrderEntry client, object data, Exception error)
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

        void GetSessionInfo()
        {
            client_.GetSessionInfoAsync(this);
        }

        void OnSessionInfoResult(OrderEntry client, object data, SessionInfo sessionInfo)
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

        void OnSessionInfoError(OrderEntry client, object data, Exception error)
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
        
        void GetOrders()
        {
            client_.GetOrdersAsync(this);
        }

        void OnOrdersBeginResult(OrderEntry client, object data, string id, int orderCount)
        {
            try
            {
                Console.Error.WriteLine("Total orders : {0}", orderCount);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnOrdersResult(OrderEntry client, object data, ExecutionReport executionReport)
        {
            try
            {
                if (executionReport.OrderType == OrderType.Stop)
                {
                    Console.Error.WriteLine("    Order : {0}, {1}, {2}, {3} {4} {5} @@{6}, {7}, {8}@{9}, \"{10}\"", executionReport.OrigClientOrderId, executionReport.OrderId, executionReport.OrderType, executionReport.Symbol, executionReport.OrderSide, executionReport.InitialVolume, executionReport.StopPrice, executionReport.OrderStatus, executionReport.InitialVolume - executionReport.LeavesVolume, executionReport.AveragePrice, executionReport.Comment);
                }
                else if (executionReport.OrderType == OrderType.StopLimit)
                {
                    Console.Error.WriteLine("    Order : {0}, {1}, {2}, {3} {4} {5}@{6} @@{7}, {8}, {9}@{10}, \"{11}\"", executionReport.OrigClientOrderId, executionReport.OrderId, executionReport.OrderType, executionReport.Symbol, executionReport.OrderSide, executionReport.InitialVolume, executionReport.Price, executionReport.StopPrice, executionReport.OrderStatus, executionReport.InitialVolume - executionReport.LeavesVolume, executionReport.AveragePrice, executionReport.Comment);
                }
                else
                    Console.Error.WriteLine("    Order : {0}, {1}, {2}, {3} {4} {5}@{6}, {7}, {8}@{9}, \"{10}\"", executionReport.OrigClientOrderId, executionReport.OrderId, executionReport.OrderType, executionReport.Symbol, executionReport.OrderSide, executionReport.InitialVolume, executionReport.Price, executionReport.OrderStatus, executionReport.InitialVolume - executionReport.LeavesVolume, executionReport.AveragePrice, executionReport.Comment);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnOrdersError(OrderEntry client, object data, Exception error)
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

        void GetPositions()
        {
            client_.GetPositionsAsync(this);
        }

        void OnPositionsResult(OrderEntry client, object data, Position[] positions)
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

        void OnPositionsError(OrderEntry client, object data, Exception error)
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

        void NewOrderMarket(string symbolId, OrderSide side, double qty, string comment)
        {
            client_.NewOrderAsync(null, Guid.NewGuid().ToString(), symbolId,  OrderType.Market, side, qty, null, null, null, null, null, null, null, comment, null, null);
        }

        void NewOrderLimit(string symbolId, OrderSide side, double qty, double price, string comment)
        {
            client_.NewOrderAsync(null, Guid.NewGuid().ToString(), symbolId, OrderType.Limit, side, qty, null, price, null, OrderTimeInForce.GoodTillCancel, null, null, null, comment, null, null);
        }

        void NewOrderStop(string symbolId, OrderSide side, double qty, double stopPrice, string comment)
        {
            client_.NewOrderAsync(null, Guid.NewGuid().ToString(), symbolId,  OrderType.Stop, side, qty, null, null, stopPrice, null, null, null, null, comment, null, null);
        }

        void NewOrderStopLimit(string symbolId, OrderSide side, double qty, double price, double stopPrice, string comment)
        {
            client_.NewOrderAsync(null, Guid.NewGuid().ToString(), symbolId,  OrderType.StopLimit, side, qty, null, price, stopPrice, OrderTimeInForce.GoodTillCancel, null, null, null, comment, null, null);
        }

        void OnNewOrderResult(OrderEntry client, object data, ExecutionReport executionReport)
        {
            try
            {
                Console.WriteLine("Execution report : {0}, {1}, {2}, {3}, {4}", executionReport.ExecutionType, executionReport.ClientOrderId, executionReport.OrderId, executionReport.OrderType, executionReport.OrderStatus);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnNewOrderError(OrderEntry client, object data, Exception error)
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

        void ReplaceOrderLimit(string orderId, string symbolId, OrderSide side, double qty, double price, string comment)
        {
            client_.ReplaceOrderAsync(null, Guid.NewGuid().ToString(), orderId, null, symbolId, OrderType.Limit, side, qty, null, price, null, OrderTimeInForce.GoodTillCancel, null, null, null, true, null, comment, null, null);
        }

        void ReplaceOrderStop(string orderId, string symbolId, OrderSide side, double qty, double stopPrice, string comment)
        {
            client_.ReplaceOrderAsync(null, Guid.NewGuid().ToString(), orderId, null, symbolId, OrderType.Stop, side, qty, null, null, stopPrice, OrderTimeInForce.GoodTillCancel, null, null, null, true, null, comment, null, null);
        }

        void ReplaceOrderStopLimit(string orderId, string symbolId, OrderSide side, double qty, double price, double stopPrice, string comment)
        {
            client_.ReplaceOrderAsync(null, Guid.NewGuid().ToString(), orderId, null, symbolId, OrderType.StopLimit, side, qty, null, price, stopPrice, OrderTimeInForce.GoodTillCancel, null, null, null, true, null, comment, null, null);
        }

        void OnReplaceOrderResult(OrderEntry client, object data, ExecutionReport executionReport)
        {
            try
            {
                Console.WriteLine("Execution report : {0}, {1}, {2}, {3}, {4}, {5}", executionReport.ExecutionType, executionReport.ClientOrderId, executionReport.OrigClientOrderId, executionReport.OrderId, executionReport.OrderType, executionReport.OrderStatus);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnReplaceOrderError(OrderEntry client, object data, Exception error)
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

        void CancelOrder(string orderId)
        {
            client_.CancelOrderAsync(null, Guid.NewGuid().ToString(), orderId, null);
        }

        void OnCancelOrderResult(OrderEntry client, object data, ExecutionReport executionReport)
        {
            try
            {
                Console.WriteLine("Execution report : {0}, {1}, {2}, {3}, {4}, {5}", executionReport.ExecutionType, executionReport.ClientOrderId, executionReport.OrigClientOrderId, executionReport.OrderId, executionReport.OrderType, executionReport.OrderStatus);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnCancelOrderError(OrderEntry client, object data, Exception error)
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

        void ClosePosition(string orderId, double qty)
        {
            client_.ClosePositionAsync(null, Guid.NewGuid().ToString(), orderId, qty);
        }

        void OnClosePositionResult(OrderEntry client, object data, ExecutionReport executionReport)
        {
            try
            {
                if (executionReport.ExecutionType == ExecutionType.Trade)
                {
                    Console.WriteLine("Execution report : {0}, {1}, {2}, {3}, {4}, {5}@{6}", executionReport.ExecutionType, executionReport.ClientOrderId, executionReport.OrderId, executionReport.OrderType, executionReport.OrderStatus, executionReport.TradeAmount, executionReport.TradePrice);
                }
                else
                    Console.WriteLine("Execution report : {0}, {1}, {2}, {3}, {4}", executionReport.ExecutionType, executionReport.ClientOrderId, executionReport.OrderId, executionReport.OrderType, executionReport.OrderStatus);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnClosePositionError(OrderEntry client, object data, Exception error)
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
    }
}
