using System;
using System.Collections.Generic;
using System.Diagnostics;
using NDesk.Options;
using TickTrader.FDK.Common;
using TickTrader.FDK.OrderEntry;

namespace OrderEntrySample
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
                    Console.Write("OrderEntrySample: ");
                    Console.WriteLine(e.Message);
                    Console.WriteLine("Try `OrderEntrySample --help' for more information.");
                    return;
                }

                if (help)
                {
                    Console.WriteLine("OrderEntrySample usage:");
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
            client_ = new Client("OrderEntrySample", port, false, "Logs", true);

            client_.LogoutEvent += new Client.LogoutDelegate(this.OnLogout);
            client_.DisconnectEvent += new Client.DisconnectDelegate(this.OnDisconnect);
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
                    catch (ExecutionException executionException)
                    {
                        ExecutionReport executionReport = executionException.Report;

                        if (executionReport.OrigClientOrderId == null)
                        {
                            Console.WriteLine("Error : {0}, {1}, {2}, {3}, {4}, {5}", executionReport.ExecutionType, executionReport.ClientOrderId, executionReport.OrderType, executionReport.OrderStatus, executionReport.RejectReason, executionReport.Text);
                        }
                        else
                            Console.WriteLine("Error : {0}, {1}, {2}, {3}, {4}, {5}, {6}", executionReport.ExecutionType, executionReport.ClientOrderId, executionReport.OrigClientOrderId, executionReport.OrderType, executionReport.OrderStatus, executionReport.RejectReason, executionReport.Text);
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

            client_.Login(login_, password_, "", "", "", Timeout);

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
            AccountInfo accountInfo = client_.GetAccountInfo(Timeout);

            Console.Error.WriteLine("Account : {0}, {1}, {2}, {3}, {4}, {5}, {6} ", accountInfo.AccountId, accountInfo.Type, accountInfo.Currency, accountInfo.Leverage, accountInfo.Balance, accountInfo.Margin, accountInfo.Equity);

            AssetInfo[] accountAssets = accountInfo.Assets;

            int count = accountAssets.Length;
            for (int index = 0; index < count; ++index)
            {
                AssetInfo accountAsset = accountAssets[index];

                Console.Error.WriteLine("    Asset : {0}, {1}, {2}", accountAsset.Currency, accountAsset.Balance, accountAsset.LockedAmount);
            }
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
            ExecutionReport[] executionReports = client_.GetOrders(Timeout);

            int count = executionReports.Length;

            Console.Error.WriteLine("Total orders : {0}", count);

            for (int index = 0; index < count; ++ index)
            {
                ExecutionReport executionReport = executionReports[index];

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

        void NewOrderMarket(string symbolId, OrderSide side, double qty, string comment)
        {
            ExecutionReport[] executionReports = client_.NewOrder(Guid.NewGuid().ToString(), symbolId, OrderType.Market, side, qty, null, null, null, null, null, null, null, comment, null, null, Timeout);

            foreach (ExecutionReport executionReport in executionReports)
                Console.WriteLine("Execution report : {0}, {1}, {2}, {3}, {4}", executionReport.ExecutionType, executionReport.ClientOrderId, executionReport.OrderId, executionReport.OrderType, executionReport.OrderStatus);
        }

        void NewOrderLimit(string symbolId, OrderSide side, double qty, double price, string comment)
        {
            ExecutionReport[] executionReports = client_.NewOrder(Guid.NewGuid().ToString(), symbolId,  OrderType.Limit, side, qty, null, price, null, OrderTimeInForce.GoodTillCancel, null, null, null, comment, null, null, Timeout);

            foreach (ExecutionReport executionReport in executionReports)
                Console.WriteLine("Execution report : {0}, {1}, {2}, {3}, {4}", executionReport.ExecutionType, executionReport.ClientOrderId, executionReport.OrderId, executionReport.OrderType, executionReport.OrderStatus);
        }

        void NewOrderStop(string symbolId, OrderSide side, double qty, double stopPrice, string comment)
        {
            ExecutionReport[] executionReports = client_.NewOrder(Guid.NewGuid().ToString(), symbolId,  OrderType.Stop, side, qty, null, null, stopPrice, null, null, null, null, comment, null, null, Timeout);

            foreach (ExecutionReport executionReport in executionReports)
                Console.WriteLine("Execution report : {0}, {1}, {2}, {3}, {4}", executionReport.ExecutionType, executionReport.ClientOrderId, executionReport.OrderId, executionReport.OrderType, executionReport.OrderStatus);
        }

        void NewOrderStopLimit(string symbolId, OrderSide side, double qty, double price, double stopPrice, string comment)
        {
            ExecutionReport[] executionReports = client_.NewOrder(Guid.NewGuid().ToString(), symbolId,  OrderType.StopLimit, side, qty, null, price, stopPrice, null, null, null, null, comment, null, null, Timeout);

            foreach (ExecutionReport executionReport in executionReports)
                Console.WriteLine("Execution report : {0}, {1}, {2}, {3}, {4}", executionReport.ExecutionType, executionReport.ClientOrderId, executionReport.OrderId, executionReport.OrderType, executionReport.OrderStatus);
        }

        void ReplaceOrderLimit(string orderId, string symbolId, OrderSide side, double qty, double price, string comment)
        {
            ExecutionReport[] executionReports = client_.ReplaceOrder(Guid.NewGuid().ToString(), orderId, null, symbolId, OrderType.Limit, side, qty, null, price, null, OrderTimeInForce.GoodTillCancel, null, null, null, comment, null, null, Timeout);

            foreach (ExecutionReport executionReport in executionReports)
                Console.WriteLine("Execution report : {0}, {1}, {2}, {3}, {4}, {5}", executionReport.ExecutionType, executionReport.ClientOrderId, executionReport.OrigClientOrderId, executionReport.OrderId, executionReport.OrderType, executionReport.OrderStatus);
        }

        void ReplaceOrderStop(string orderId, string symbolId, OrderSide side, double qty, double stopPrice, string comment)
        {
            ExecutionReport[] executionReports = client_.ReplaceOrder(Guid.NewGuid().ToString(), orderId, null, symbolId, OrderType.Stop, side, qty, null, null, stopPrice, OrderTimeInForce.GoodTillCancel, null, null, null, comment, null, null, Timeout);

            foreach (ExecutionReport executionReport in executionReports)
                Console.WriteLine("Execution report : {0}, {1}, {2}, {3}, {4}, {5}", executionReport.ExecutionType, executionReport.ClientOrderId, executionReport.OrigClientOrderId, executionReport.OrderId, executionReport.OrderType, executionReport.OrderStatus);
        }

        void ReplaceOrderStopLimit(string orderId, string symbolId, OrderSide side, double qty, double price, double stopPrice, string comment)
        {
            ExecutionReport[] executionReports = client_.ReplaceOrder(Guid.NewGuid().ToString(), orderId, null, symbolId, OrderType.Stop, side, qty, null, price, stopPrice, OrderTimeInForce.GoodTillCancel, null, null, null, comment, null, null, Timeout);

            foreach (ExecutionReport executionReport in executionReports)
                Console.WriteLine("Execution report : {0}, {1}, {2}, {3}, {4}, {5}", executionReport.ExecutionType, executionReport.ClientOrderId, executionReport.OrigClientOrderId, executionReport.OrderId, executionReport.OrderType, executionReport.OrderStatus);
        }

        void CancelOrder(string orderId)
        {
            ExecutionReport[] executionReports = client_.CancelOrder(Guid.NewGuid().ToString(), orderId, null, Timeout);

            foreach (ExecutionReport executionReport in executionReports)
                Console.WriteLine("Execution report : {0}, {1}, {2}, {2}, {3}, {4}", executionReport.ExecutionType, executionReport.ClientOrderId, executionReport.OrigClientOrderId, executionReport.OrderId, executionReport.OrderType, executionReport.OrderStatus);
        }

        void ClosePosition(string orderId, double qty)
        {
            ExecutionReport[] executionReports = client_.ClosePosition(Guid.NewGuid().ToString(), orderId, qty, Timeout);

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

        void OnDisconnect(Client client, object data, string text)
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

        void OnLogout(Client client, LogoutInfo info)
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

        void OnExecutionReport(Client client, ExecutionReport executionReport)
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

        void OnPositionUpdate(Client client, Position position)
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

        void OnAccountInfoUpdate(Client client, AccountInfo accountInfo)
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

        void OnSessionInfoUpdate(Client client, SessionInfo sessionInfo)
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

        void OnNotification(Client client, Notification notification)
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
