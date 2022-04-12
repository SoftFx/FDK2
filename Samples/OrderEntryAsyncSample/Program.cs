using System;
using System.Collections.Generic;
using System.Diagnostics;
using NDesk.Options;
using SoftFX.Net.Core;
using TickTrader.FDK.Common;
using TickTrader.FDK.Client;

namespace OrderEntryAsyncSample
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
            client_ = new OrderEntry(SampleName, port : port, logMessages : true, logEvents:false, logStates:false,
                validateClientCertificate: (sender, certificate, chain, errors) => true);

            client_.ConnectResultEvent += new OrderEntry.ConnectResultDelegate(this.OnConnectResult);
            client_.ConnectErrorEvent += new OrderEntry.ConnectErrorDelegate(this.OnConnectError);
            client_.DisconnectResultEvent += new OrderEntry.DisconnectResultDelegate(this.OnDisconnectResult);
            client_.DisconnectEvent += new OrderEntry.DisconnectDelegate(this.OnDisconnect);
            client_.ReconnectEvent += new OrderEntry.ReconnectDelegate(this.OnReconnect);
            client_.ReconnectErrorEvent += new OrderEntry.ReconnectErrorDelegate(this.OnReconnectError);
            client_.LoginResultEvent += new OrderEntry.LoginResultDelegate(this.OnLoginResult);
            client_.LoginErrorEvent += new OrderEntry.LoginErrorDelegate(this.OnLoginError);
            client_.TwoFactorLoginRequestEvent += new OrderEntry.TwoFactorLoginRequestDelegate(this.OnTwoFactorLoginRequest);
            client_.TwoFactorLoginResultEvent += new OrderEntry.TwoFactorLoginResultDelegate(this.OnTwoFactorLoginResult);
            client_.TwoFactorLoginErrorEvent += new OrderEntry.TwoFactorLoginErrorDelegate(this.OnTwoFactorLoginError);
            client_.TwoFactorLoginResumeEvent += new OrderEntry.TwoFactorLoginResumeDelegate(this.OnTwoFactorLoginResume);
            client_.LogoutResultEvent += new OrderEntry.LogoutResultDelegate(this.OnLogoutResult);
            client_.LogoutErrorEvent += new OrderEntry.LogoutErrorDelegate(this.OnLogoutError);
            client_.LogoutEvent += new OrderEntry.LogoutDelegate(this.OnLogout);
            client_.TradeServerInfoResultEvent += new OrderEntry.TradeServerInfoResultDelegate(this.OnTradeServerInfoResult);
            client_.TradeServerInfoErrorEvent += new OrderEntry.TradeServerInfoErrorDelegate(this.OnTradeServerErrorResult);
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
            client_.SplitListResultEvent += new OrderEntry.SplitListResultDelegate(this.OnSplitListResult);
            client_.SplitListErrorEvent += new OrderEntry.SplitListErrorDelegate(this.OnSplitListError);
            client_.DividendListResultEvent += new OrderEntry.DividendListResultDelegate(this.OnDividendListResult);
            client_.DividendListErrorEvent += new OrderEntry.DividendListErrorDelegate(this.OnDividendListError);
            client_.MergerAndAcquisitionListResultEvent += new OrderEntry.MergerAndAcquisitionListResultDelegate(this.OnMergerAndAcquisitionListResult);
            client_.MergerAndAcquisitionListErrorEvent += new OrderEntry.MergerAndAcquisitionListErrorDelegate(this.OnMergerAndAcquisitionListError);

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
                        else if (command == "tsi")
                        {
                            GetTradeServerInfo();
                        }
                        else if (command == "ai")
                        {
                            GetAccountInfo();
                        }
                        else if (command == "si")
                        {
                            GetSessionInfo();
                        }
                        else if (command == "o")
                        {
                            GetOrders();
                        }
                        else if (command == "p")
                        {
                            GetPositions();
                        }
                        else if (command == "nom")
                        {
                            string firstParam = GetNextWord(line, ref pos);
                            if (firstParam == "?")
                            {
                                Console.WriteLine("nom <symbol> <side> <qty> [<comment>]");
                                continue;
                            }

                            string symbolId = firstParam;

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
                        else if (command == "nol")
                        {
                            string firstParam = GetNextWord(line, ref pos);
                            if (firstParam == "?")
                            {
                                Console.WriteLine("nol <symbol> <side> <qty> <price> <ioc:true|false> <oco:true|false> [<ocoeqam:true|false>] [<relordid>] [<comment>]");
                                continue;
                            }

                            string symbolId = firstParam;

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

                            string ioc = GetNextWord(line, ref pos);

                            if (ioc == null)
                                throw new Exception("Invalid command : " + line);

                            string str = GetNextWord(line, ref pos);

                            bool oco = false;
                            bool ocoea = false;
                            long? relordid = null;

                            if (bool.TryParse(str, out oco))
                            {
                                if (oco)
                                {
                                    str = GetNextWord(line, ref pos);
                                    if (bool.TryParse(str, out ocoea))
                                    {
                                        str = GetNextWord(line, ref pos);
                                    }

                                    if (long.TryParse(str, out var id))
                                    {
                                        relordid = id;
                                        str = GetNextWord(line, ref pos);
                                    }
                                    else
                                        throw new Exception("Invalid params: " + line);
                                }
                            }

                            string comment = str;

                            NewOrderLimit
                            (
                                symbolId,
                                (OrderSide)Enum.Parse(typeof(OrderSide), side),
                                double.Parse(qty),
                                double.Parse(price),
                                bool.Parse(ioc),
                                oco, ocoea, relordid,
                                comment
                            );
                        }
                        else if (command == "nos")
                        {
                            string firstParam = GetNextWord(line, ref pos);
                            if (firstParam == "?")
                            {
                                Console.WriteLine("nos <symbol> <side> <qty> <stopPrice> <oco:true|false> [<ocoeqam:true|false>] [<relordid>] [<comment>]");
                                continue;
                            }

                            string symbolId = firstParam;

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

                            string str = GetNextWord(line, ref pos);

                            bool oco = false;
                            bool ocoea = false;
                            long? relordid = null;

                            if (bool.TryParse(str, out oco))
                            {
                                if (oco)
                                {
                                    str = GetNextWord(line, ref pos);
                                    if (bool.TryParse(str, out ocoea))
                                    {
                                        str = GetNextWord(line, ref pos);
                                    }

                                    if (long.TryParse(str, out var id))
                                    {
                                        relordid = id;
                                        str = GetNextWord(line, ref pos);
                                    }
                                    else
                                        throw new Exception("Invalid command : " + line);
                                }
                            }

                            string comment = str;

                            NewOrderStop
                            (
                                symbolId,
                                (OrderSide)Enum.Parse(typeof(OrderSide), side),
                                double.Parse(qty),
                                double.Parse(stopPrice),
                                oco, ocoea, relordid,
                                comment
                            );
                        }
                        else if (command == "nosl")
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
                        else if (command == "noco")
                        {
                            string firstParam = GetNextWord(line, ref pos);
                            if (firstParam == "?")
                                Console.WriteLine("ncoco <symbol> <side1> <type1> <qty1> <price1> <side2> <type2> <qty2> <price2> [<comment>]");
                            else
                            {
                                string symbolId = firstParam;

                                if (symbolId == null)
                                    throw new Exception("Invalid command : " + line);

                                string side1 = GetNextWord(line, ref pos);

                                if (side1 == null)
                                    throw new Exception("Invalid command : " + line);

                                string type1 = GetNextWord(line, ref pos);

                                if (type1 == null)
                                    throw new Exception("Invalid command : " + line);

                                string qty1 = GetNextWord(line, ref pos);

                                if (qty1 == null)
                                    throw new Exception("Invalid command : " + line);

                                string price1 = GetNextWord(line, ref pos);

                                if (price1 == null)
                                    throw new Exception("Invalid command : " + line);

                                string side2 = GetNextWord(line, ref pos);

                                if (side2 == null)
                                    throw new Exception("Invalid command : " + line);

                                string type2 = GetNextWord(line, ref pos);

                                if (type2 == null)
                                    throw new Exception("Invalid command : " + line);

                                string qty2 = GetNextWord(line, ref pos);

                                if (qty2 == null)
                                    throw new Exception("Invalid command : " + line);

                                string price2 = GetNextWord(line, ref pos);

                                if (price2 == null)
                                    throw new Exception("Invalid command : " + line);

                                string comment = GetNextWord(line, ref pos);

                                NewOcoOrders
                                (
                                    symbolId,
                                    (OrderSide)Enum.Parse(typeof(OrderSide), side1), (OrderType)Enum.Parse(typeof(OrderType), type1), double.Parse(qty1), double.Parse(price1),
                                    (OrderSide)Enum.Parse(typeof(OrderSide), side2), (OrderType)Enum.Parse(typeof(OrderType), type2), double.Parse(qty2), double.Parse(price2),
                                    null, null,
                                    comment
                                );
                            }
                        }
                        else if (command == "nco")
                        {
                            string firstParam = GetNextWord(line, ref pos);
                            if (firstParam == "?")
                            {
                                Console.WriteLine("nco <symbol> <side> <type> <qty> <price> <trigger> <triggerparam> [<comment>]");
                                continue;
                            }

                            string symbolId = firstParam;

                            if (symbolId == null)
                                throw new Exception("Invalid command : " + line);

                            string side = GetNextWord(line, ref pos);

                            if (side == null)
                                throw new Exception("Invalid command : " + line);

                            string type = GetNextWord(line, ref pos);

                            if (type == null)
                                throw new Exception("Invalid command : " + line);

                            string qty = GetNextWord(line, ref pos);

                            if (qty == null)
                                throw new Exception("Invalid command : " + line);

                            string price = GetNextWord(line, ref pos);

                            if (price == null)
                                throw new Exception("Invalid command : " + line);

                            string str = GetNextWord(line, ref pos);
                            if (!Enum.TryParse<ContingentOrderTriggerType>(str, out var trigger))
                                throw new Exception("Invalid command : " + line);
                            str = GetNextWord(line, ref pos);
                            if (!long.TryParse(str, out var trigparam))
                                throw new Exception("Invalid command : " + line);

                            //bool oco = false;
                            //bool ocoea = false;
                            //long? relordid = null;

                            //if (bool.TryParse(str, out oco))
                            //{
                            //    if (oco)
                            //    {
                            //        str = GetNextWord(line, ref pos);
                            //        if (bool.TryParse(str, out ocoea))
                            //        {
                            //            str = GetNextWord(line, ref pos);
                            //        }

                            //        if (long.TryParse(str, out var id))
                            //        {
                            //            relordid = id;
                            //            str = GetNextWord(line, ref pos);
                            //        }
                            //        else
                            //            throw new Exception("Invalid params: " + line);
                            //    }
                            //}

                            str = GetNextWord(line, ref pos);
                            string comment = str;

                            NewContingentOrder
                            (
                                symbolId,
                                (OrderSide)Enum.Parse(typeof(OrderSide), side), (OrderType)Enum.Parse(typeof(OrderType), type),
                                double.Parse(qty),
                                double.Parse(price),
                                trigger, trigparam,
                                comment
                            );
                        }
                        else if (command == "ncoco")
                        {
                            string firstParam = GetNextWord(line, ref pos);
                            if (firstParam == "?")
                                Console.WriteLine("ncoco <symbol> <side1> <type1> <qty1> <price1> <side2> <type2> <qty2> <price2> <trigger> <triggerparam> [<comment>]");
                            else
                            {
                                string symbolId = firstParam;

                                if (symbolId == null)
                                    throw new Exception("Invalid command : " + line);

                                string side1 = GetNextWord(line, ref pos);

                                if (side1 == null)
                                    throw new Exception("Invalid command : " + line);

                                string type1 = GetNextWord(line, ref pos);

                                if (type1 == null)
                                    throw new Exception("Invalid command : " + line);

                                string qty1 = GetNextWord(line, ref pos);

                                if (qty1 == null)
                                    throw new Exception("Invalid command : " + line);

                                string price1 = GetNextWord(line, ref pos);

                                if (price1 == null)
                                    throw new Exception("Invalid command : " + line);

                                string side2 = GetNextWord(line, ref pos);

                                if (side2 == null)
                                    throw new Exception("Invalid command : " + line);

                                string type2 = GetNextWord(line, ref pos);

                                if (type2 == null)
                                    throw new Exception("Invalid command : " + line);

                                string qty2 = GetNextWord(line, ref pos);

                                if (qty2 == null)
                                    throw new Exception("Invalid command : " + line);

                                string price2 = GetNextWord(line, ref pos);

                                if (price2 == null)
                                    throw new Exception("Invalid command : " + line);

                                string str = GetNextWord(line, ref pos);
                                if (!Enum.TryParse<ContingentOrderTriggerType>(str, out var trigger))
                                    throw new Exception("Invalid command : " + line);
                                str = GetNextWord(line, ref pos);
                                if (!long.TryParse(str, out var trigparam))
                                    throw new Exception("Invalid command : " + line);

                                //bool oco = false;
                                //bool ocoea = false;
                                //long? relordid = null;

                                //if (bool.TryParse(str, out oco))
                                //{
                                //    if (oco)
                                //    {
                                //        str = GetNextWord(line, ref pos);
                                //        if (bool.TryParse(str, out ocoea))
                                //        {
                                //            str = GetNextWord(line, ref pos);
                                //        }

                                //        if (long.TryParse(str, out var id))
                                //        {
                                //            relordid = id;
                                //            str = GetNextWord(line, ref pos);
                                //        }
                                //        else
                                //            throw new Exception("Invalid params: " + line);
                                //    }
                                //}

                                str = GetNextWord(line, ref pos);
                                string comment = str;

                                NewOcoOrders
                                (
                                    symbolId,
                                    (OrderSide)Enum.Parse(typeof(OrderSide), side1), (OrderType)Enum.Parse(typeof(OrderType), type1), double.Parse(qty1), double.Parse(price1),
                                    (OrderSide)Enum.Parse(typeof(OrderSide), side2), (OrderType)Enum.Parse(typeof(OrderType), type2), double.Parse(qty2), double.Parse(price2),
                                    trigger, trigparam,
                                    comment
                                );
                            }
                        }
                        else if (command == "rp")
                        {
                            string firstParam = GetNextWord(line, ref pos);
                            if (firstParam == "?")
                            {
                                Console.WriteLine("rp <orderId> <symbol> <side> <sl> <tp> [<comment>]");
                                continue;
                            }

                            string orderId = firstParam;

                            if (orderId == null)
                                throw new Exception("Invalid command : " + line);

                            string symbolId = GetNextWord(line, ref pos);

                            if (symbolId == null)
                                throw new Exception("Invalid command : " + line);

                            string side = GetNextWord(line, ref pos);

                            if (side == null)
                                throw new Exception("Invalid command : " + line);

                            string sls = GetNextWord(line, ref pos);
                            if (sls == null)
                                throw new Exception("Invalid command : " + line);
                            double? sl = double.TryParse(sls, out var sld) ? sld : default(double?);

                            string tps = GetNextWord(line, ref pos);
                            if (tps == null)
                                throw new Exception("Invalid command : " + line);
                            double? tp = double.TryParse(tps, out var tpd) ? tpd : default(double?);

                            string comment = GetNextWord(line, ref pos);

                            ReplacePosition
                            (
                                orderId,
                                symbolId,
                                (OrderSide)Enum.Parse(typeof(OrderSide), side),
                                sl,
                                tp,
                                comment
                            );
                        }
                        else if (command == "rol")
                        {
                            string firstParam = GetNextWord(line, ref pos);
                            if (firstParam == "?")
                            {
                                Console.WriteLine("rol <orderId> <symbol> <side> <qty> <price> <oco:true|false> [<ocoeqam:true|false>] [<relordid>] [<comment>]");
                                continue;
                            }

                            string orderId = firstParam;

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

                            string str = GetNextWord(line, ref pos);

                            bool? oco = null;
                            bool? ocoea = null;
                            long? relordid = null;

                            if (bool.TryParse(str, out var o))
                            {
                                oco = o;
                                if (oco.Value)
                                {
                                    str = GetNextWord(line, ref pos);
                                    if (bool.TryParse(str, out var oea))
                                    {
                                        ocoea = oea;
                                        str = GetNextWord(line, ref pos);
                                    }

                                    if (long.TryParse(str, out var id))
                                    {
                                        relordid = id;
                                        str = GetNextWord(line, ref pos);
                                    }
                                    else
                                        throw new Exception("Invalid command : " + line);
                                }
                            }

                            string comment = str;

                            ReplaceOrderLimit
                            (
                                orderId,
                                symbolId,
                                (OrderSide)Enum.Parse(typeof(OrderSide), side),
                                double.Parse(qty),
                                double.Parse(price),
                                oco, ocoea, relordid,
                                comment
                            );
                        }
                        else if (command == "ros")
                        {
                            string firstParam = GetNextWord(line, ref pos);
                            if (firstParam == "?")
                            {
                                Console.WriteLine("ros <orderId> <symbol> <side> <qty> <stopPrice> <oco:true|false> [<ocoeqam:true|false>] [<relordid>] [<comment>]");
                                continue;
                            }

                            string orderId = firstParam;

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

                            string str = GetNextWord(line, ref pos);

                            bool? oco = null;
                            bool? ocoea = null;
                            long? relordid = null;

                            if (bool.TryParse(str, out var o))
                            {
                                oco = o;
                                if (oco.Value)
                                {
                                    str = GetNextWord(line, ref pos);
                                    if (bool.TryParse(str, out var oea))
                                    {
                                        ocoea = oea;
                                        str = GetNextWord(line, ref pos);
                                    }

                                    if (long.TryParse(str, out var id))
                                    {
                                        relordid = id;
                                        str = GetNextWord(line, ref pos);
                                    }
                                    else
                                        throw new Exception("Invalid command : " + line);
                                }
                            }

                            string comment = str;

                            ReplaceOrderStop
                            (
                                orderId,
                                symbolId,
                                (OrderSide)Enum.Parse(typeof(OrderSide), side),
                                double.Parse(qty),
                                double.Parse(stopPrice),
                                oco, ocoea, relordid,
                                comment
                            );
                        }
                        else if (command == "rosl")
                        {
                            string firstParam = GetNextWord(line, ref pos);
                            if (firstParam == "?")
                            {
                                Console.WriteLine("rosl <orderId> <symbol> <side> <qty> <price> <stopPrice> [<comment>]");
                                continue;
                            }
                            string orderId = firstParam;

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
                        else if (command == "co")
                        {
                            string orderId = GetNextWord(line, ref pos);

                            if (orderId == null || !long.TryParse(orderId, out var ordId))
                                throw new Exception("Invalid command : " + line);

                            CancelOrder(ordId);
                        }
                        else if (command == "cp")
                        {
                            string orderId = GetNextWord(line, ref pos);

                            if (orderId == null)
                                throw new Exception("Invalid command : " + line);

                            string qty = GetNextWord(line, ref pos);

                            if (qty == null)
                                throw new Exception("Invalid command : " + line);

                            ClosePosition(orderId, double.Parse(qty));
                        }
                        else if (command == "sp")
                        {
                            GetSplits();
                        }
                        else if (command == "di")
                        {
                            GetDividends();
                        }
                        else if (command == "ma")
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

        void OnConnectResult(OrderEntry client, object data)
        {
            try
            {
                Console.WriteLine($"Connected to {address_}");

                client_.LoginAsync(null, login_, password_, "31DBAF09-94E1-4B2D-8ACF-5E6167E0D2D2", SampleName, "");
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);

                client_.DisconnectAsync(null, Reason.ClientError("Client disconnect"));
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

                client_.DisconnectAsync(null, Reason.ClientError("Client disconnect"));
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
                Console.WriteLine($"{login_}: Login succeeded");
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

                client_.DisconnectAsync(null, Reason.ClientError(error.Message));
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnTwoFactorLoginRequest(OrderEntry client, string message)
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

        void OnLogoutResult(OrderEntry client, object data, LogoutInfo logoutInfo)
        {
            try
            {
                Console.WriteLine("Logout : " + logoutInfo.Message);

                client_.DisconnectAsync(null, Reason.ClientRequest("Client logout"));
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
            Console.WriteLine("sotp <oneTimePassword> - send one time password");
            Console.WriteLine("rotp - resume one time password");
            Console.WriteLine("tsi - request trade server information");
            Console.WriteLine("ai - request account information");
            Console.WriteLine("si - request trading session information");
            Console.WriteLine("o - request list of orders");
            Console.WriteLine("p - request list of positions");
            Console.WriteLine("nom - send new market order. Type <nom ?> for help");
            Console.WriteLine("nol - send new order limit. Type <nol ?> for help");
            Console.WriteLine("nos - send new stop order. Type <nos ?> for help");
            Console.WriteLine("nosl - send new stop limit order. Type <nosl ?> for help");
            Console.WriteLine("noco - send new OCO orders. Type <noco ?> for help");
            Console.WriteLine("nco - send new contingent order. Type <nco ?> for help");
            Console.WriteLine("ncoco - send new contingent OCO orders. Type <ncoco ?> for help");
            Console.WriteLine("rp - send position replace. Type <rp ?> for help");
            Console.WriteLine("rol - send limit order replace. Type <rol ?> for help");
            Console.WriteLine("ros - send stop order replace. Type <ros ?> for help");
            Console.WriteLine("rosl - send stop limit order replace. Type <rosl ?> for help");
            Console.WriteLine("co <orderId> - send order cancel");
            Console.WriteLine("cp <orderId> <qty> - send position close");
            Console.WriteLine("sp - request list of splits");
            Console.WriteLine("di - request list of dividends");
            Console.WriteLine("ma - request list of mergers and acquisitions");
            Console.WriteLine("e - exit");
        }

        void SendOneTimePassword(string oneTimePassword)
        {
            client_.TwoFactorLoginResponseAsync(this, oneTimePassword);
        }

        void OnTwoFactorLoginResult(OrderEntry orderEntry, object data, DateTime expireTime)
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

        void OnTwoFactorLoginError(OrderEntry orderEntry, object data, Exception error)
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

        void OnTwoFactorLoginResume(OrderEntry orderEntry, object data, DateTime expireTime)
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

        void GetTradeServerInfo()
        {
            client_.GetTradeServerInfoAsync(this);
        }

        void OnTradeServerInfoResult(OrderEntry client, object data, TradeServerInfo tradeServerInfo)
        {
            try
            {
                Console.WriteLine("Trade server info : {0}, {1}", tradeServerInfo.ServerName, tradeServerInfo.ServerDescription);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        void OnTradeServerErrorResult(OrderEntry client, object data, Exception error)
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

        void OnOrdersResult(OrderEntry client, object data, ExecutionReport er)
        {
            try
            {
                if (er.OrderType == OrderType.Stop)
                {
                    Console.Error.WriteLine("Order : {0}, {1}, {2}, {3} {4} {5} @@{6}, {7}, {8}, \"{9}\"", er.OrderId, er.OrderStatus, er.OrigClientOrderId, er.OrderType, er.Symbol, er.OrderSide, er.InitialVolume, er.StopPrice, er.LeavesVolume, er.Comment);
                }
                else if (er.OrderType == OrderType.StopLimit)
                {
                    Console.Error.WriteLine("Order : {0}, {1}, {2}, {3} {4} {5}@{6} @@{7}, {8}, {9}, \"{10}\"", er.OrderId, er.OrderStatus, er.OrigClientOrderId, er.OrderType, er.Symbol, er.OrderSide, er.InitialVolume, er.Price, er.StopPrice, er.LeavesVolume, er.Comment);
                }
                else
                    Console.Error.WriteLine("Order : {0}, {1}, {2}, {3} {4} {5}@{6}, {7}, {8}, \"{9}\"", er.OrderId, er.OrderStatus, er.OrigClientOrderId, er.OrderType, er.Symbol, er.OrderSide, er.InitialVolume, er.Price, er.LeavesVolume, er.Comment);
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

                    string posDetails = position.BuyAmount != 0
                        ? $"Buy, {position.BuyAmount}, {position.BuyPrice}"
                        : $"Sell, {position.SellAmount}, {position.SellPrice}";
                    Console.Error.WriteLine($"{position.PosId}, {position.Symbol}, {posDetails}");
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
            var newOrder = OrderEntry.CreateNewOrderRequest(Guid.NewGuid().ToString(), symbolId, OrderType.Market, side, qty)
                .WithComment(comment);
            newOrder.Async(null).Send(client_);
        }

        void NewOrderLimit(string symbolId, OrderSide side, double qty, double price, bool ioc, bool oco, bool ocoeqam, long? relordid, string comment)
        {
            var newOrder = OrderEntry.CreateNewOrderRequest(Guid.NewGuid().ToString(), symbolId, OrderType.Limit, side, qty)
                .WithPrice(price).WithComment(comment);
            if (ioc)
                newOrder.WithIoc();
            if (oco && relordid.HasValue)
                newOrder.WithOneCancelsTheOther(relordid.Value, ocoeqam);
            newOrder.Async(null).Send(client_);
        }

        void NewOrderStop(string symbolId, OrderSide side, double qty, double stopPrice, bool oco, bool ocoeqam, long? relordid, string comment)
        {
            var newOrder = OrderEntry.CreateNewOrderRequest(Guid.NewGuid().ToString(), symbolId, OrderType.Stop, side, qty)
                .WithStopPrice(stopPrice).WithComment(comment);
            if (oco && relordid.HasValue)
                newOrder.WithOneCancelsTheOther(relordid.Value, ocoeqam);
            newOrder.Async(null).Send(client_);
        }

        void NewOrderStopLimit(string symbolId, OrderSide side, double qty, double price, double stopPrice, string comment)
        {
            var newOrder = OrderEntry.CreateNewOrderRequest(Guid.NewGuid().ToString(), symbolId, OrderType.StopLimit, side, qty)
                .WithPrice(price).WithStopPrice(stopPrice).WithComment(comment);
            newOrder.Async(null).Send(client_);
        }

        void NewContingentOrder(string symbolId,
            OrderSide side, OrderType type, double qty, double price,
            ContingentOrderTriggerType trType, long trParam, string comment)
        {
            var newOrder = OrderEntry.CreateNewOrderRequest(Guid.NewGuid().ToString(), symbolId, type, side, qty)
                .WithComment(comment);

            if (type == OrderType.Limit) newOrder.WithPrice(price);
            else if (type == OrderType.Stop) newOrder.WithStopPrice(price);

            if (trType == ContingentOrderTriggerType.OnTime)
                newOrder.WithContingent(trType, DateTime.UtcNow.AddSeconds(trParam), null);
            else
                newOrder.WithContingent(trType, null, trParam);
            newOrder.Async(null).Send(client_);
        }

        void NewOcoOrders(string symbolId,
            OrderSide side1, OrderType type1, double qty1, double price1,
            OrderSide side2, OrderType type2, double qty2, double price2,
            ContingentOrderTriggerType? trType, long? trParam, string comment)
        {
            var newOrder = OrderEntry.CreateNewOcoOrdersRequest(Guid.NewGuid().ToString(), symbolId)
                .WithFirst(Guid.NewGuid().ToString(), type1, side1, qty1)
                .WithSecond(Guid.NewGuid().ToString(), type2, side2, qty2)
                .FirstWithComment(comment).SecondWithComment(comment);

            if (type1 == OrderType.Limit) newOrder.FirstWithPrice(price1);
            else if (type1 == OrderType.Stop) newOrder.FirstWithStopPrice(price1);

            if (type2 == OrderType.Limit) newOrder.SecondWithPrice(price2);
            else if (type2 == OrderType.Stop) newOrder.SecondWithStopPrice(price2);

            if (trType.HasValue && trParam.HasValue)
            {
                if (trType == ContingentOrderTriggerType.OnTime)
                    newOrder.WithContingent(trType, DateTime.UtcNow.AddSeconds(trParam.Value), null);
                else
                    newOrder.WithContingent(trType, null, trParam);
            }

            newOrder.Async(null).Send(client_);
        }

        void OnNewOrderResult(OrderEntry client, object data, ExecutionReport executionReport)
        {
            try
            {
                Console.WriteLine("Execution report : {0}, {1}, {2}, {3}, {4}", executionReport.OrderId, executionReport.ExecutionType, executionReport.OrderStatus, executionReport.OrderType, executionReport.Last?"Last":"");
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

        void ReplacePosition(string orderId, string symbolId, OrderSide side, double? sl, double? tp, string comment)
        {
            var request = OrderEntry.CreateReplaceOrderRequest(Guid.NewGuid().ToString(), symbolId, OrderType.Position, side);
            if (sl.HasValue)
                request.WithStopLoss(sl.Value);
            if (tp.HasValue)
                request.WithTakeProfit(tp.Value);
            request.WithComment(comment).Async(null).Send(client_);
        }

        void ReplaceOrderLimit(string orderId, string symbolId, OrderSide side, double qtyChange, double price, bool? oco, bool? ocoeqam, long? relordid, string comment)
        {
            var request = OrderEntry.CreateReplaceOrderRequest(Guid.NewGuid().ToString(), symbolId, OrderType.Limit, side)
                .WithQtyChange(qtyChange).WithPrice(price);
            if (oco ?? false)
            {
                request.WithOneCancelsTheOther(relordid ?? 0, ocoeqam ?? false);
            }
            request.WithComment(comment).Async(null).Send(client_);
        }

        void ReplaceOrderStop(string orderId, string symbolId, OrderSide side, double qtyChange, double stopPrice, bool? oco, bool? ocoeqam, long? relordid, string comment)
        {
            var request = OrderEntry.CreateReplaceOrderRequest(Guid.NewGuid().ToString(), symbolId, OrderType.Stop, side)
                .WithQtyChange(qtyChange).WithStopPrice(stopPrice);
            if (oco ?? false)
            {
                request.WithOneCancelsTheOther(relordid ?? 0, ocoeqam ?? false);
            }
            request.WithComment(comment).Async(null).Send(client_);
        }

        void ReplaceOrderStopLimit(string orderId, string symbolId, OrderSide side, double qtyChange, double price, double stopPrice, string comment)
        {
            var request = OrderEntry.CreateReplaceOrderRequest(Guid.NewGuid().ToString(), symbolId, OrderType.StopLimit, side)
                .WithQtyChange(qtyChange).WithPrice(price).WithStopPrice(stopPrice).WithComment(comment).Async(null).Send(client_);
        }

        void OnReplaceOrderResult(OrderEntry client, object data, ExecutionReport executionReport)
        {
            try
            {
                Console.WriteLine("Execution report : {0}, {1}, {2}, {3}, {4}", executionReport.OrderId, executionReport.ExecutionType, executionReport.OrderStatus, executionReport.OrderType, executionReport.Last?"Last":"");
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

        void CancelOrder(long orderId)
        {
            OrderEntry.CreateCancelOrderRequest(Guid.NewGuid().ToString()).WithOrderId(orderId).Async(null)
                .Send(client_);
        }

        void OnCancelOrderResult(OrderEntry client, object data, ExecutionReport executionReport)
        {
            try
            {
                Console.WriteLine("Execution report : {0}, {1}, {2}, {3}, {4}", executionReport.OrderId, executionReport.ExecutionType, executionReport.OrderStatus, executionReport.OrderType, executionReport.Last?"Last":"");
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
            var request = OrderEntry.CreateClosePositionRequest(Guid.NewGuid().ToString())
                .WithPositionId(long.Parse(orderId)).WithQty(qty).Async(null).Send(client_);
        }

        void OnClosePositionResult(OrderEntry client, object data, ExecutionReport executionReport)
        {
            try
            {
                if (executionReport.ExecutionType == ExecutionType.Trade)
                {
                    Console.WriteLine("Execution report : {0}, {1}, {2}, {3}, {4}@{5}", executionReport.OrderId, executionReport.ExecutionType, executionReport.OrderStatus, executionReport.OrderType, executionReport.TradeAmount, executionReport.TradePrice);
                }
                else
                    Console.WriteLine("Execution report : {0}, {1}, {2}, {3}", executionReport.OrderId, executionReport.ExecutionType, executionReport.OrderStatus, executionReport.OrderType);
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
                    Console.WriteLine("Execution report : {0}, {1}, {2}, {3}, {4}, {5}@{6}", executionReport.OrderId, executionReport.ExecutionType, executionReport.OrderStatus, executionReport.OrderType, executionReport.OrderTimeInForce, executionReport.TradeAmount, executionReport.TradePrice);
                }
                else
                    Console.WriteLine("Execution report : {0}, {1}, {2}, {3}, {4}", executionReport.OrderId, executionReport.ExecutionType, executionReport.OrderStatus, executionReport.OrderType, executionReport.OrderTimeInForce);
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
                string posDetails = position.BuyAmount != 0
                    ? $"Buy, {position.BuyAmount}, {position.BuyPrice}"
                    : $"Sell, {position.SellAmount}, {position.SellPrice}";
                Console.Error.WriteLine($"Position report : {position.PosId}, {position.Symbol}, {posDetails}");
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

        void GetSplits()
        {
            client_.GetSplitListAsync(this);
        }

        void GetDividends()
        {
            client_.GetDividendListAsync(this);
        }

        void GetMergersAndAcquisitions()
        {
            client_.GetMergerAndAcquisitionListAsync(this);
        }

        private void OnSplitListResult(OrderEntry orderentry, object data, Split[] splits)
        {
            try
            {
                int count = splits.Length;

                Console.Error.WriteLine("Total Splits : {0}", count);

                for (int index = 0; index < count; ++index)
                {
                    Split split = splits[index];

                    Console.Error.WriteLine($"Split: {split}");
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        private void OnSplitListError(OrderEntry orderentry, object data, Exception error)
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

        private void OnDividendListResult(OrderEntry orderentry, object data, Dividend[] dividends)
        {
            try
            {
                int count = dividends.Length;

                Console.Error.WriteLine("Total Dividends : {0}", count);

                for (int index = 0; index < count; ++index)
                {
                    Dividend dividend = dividends[index];

                    Console.Error.WriteLine($"Dividend: {dividend}");
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        private void OnDividendListError(OrderEntry orderentry, object data, Exception error)
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

        private void OnMergerAndAcquisitionListResult(OrderEntry orderentry, object data, MergerAndAcquisition[] dividends)
        {
            try
            {
                int count = dividends.Length;

                Console.Error.WriteLine("Total MergerAndAcquisitions : {0}", count);

                for (int index = 0; index < count; ++index)
                {
                    MergerAndAcquisition dividend = dividends[index];

                    Console.Error.WriteLine($"MergerAndAcquisition: {dividend}");
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }

        private void OnMergerAndAcquisitionListError(OrderEntry orderentry, object data, Exception error)
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

        readonly OrderEntry client_;

        readonly string address_;
        readonly string login_;
        readonly string password_;
    }
}
