namespace DataTradeExamples
{
    using System;
    using System.IO;

    class Program
    {
        static void Main()
        {
            try
            {
                string address = "tp.dev.soft-fx.eu";
                string username = "5";
                string password = "123qwe!";

                while (true)
                {
                    try
                    {
                        Console.WriteLine("1 - TradeServerInfoExample");
                        Console.WriteLine("2 - AccountInfoExample");
                        Console.WriteLine("3 - GetOrdersExample");
                        Console.WriteLine("4 - SendMarketOrderExample");
                        Console.WriteLine("5 - SendLimitOrderExample");
                        Console.WriteLine("6 - SendStopOrderExample");
                        Console.WriteLine("7 - SendStopLimitOrderExample");
                        Console.WriteLine("8 - ModifyTradeRecordExample");
                        Console.WriteLine("9 - DeletePendingOrderExample");
                        Console.WriteLine("10 - ClosePositionExample");
                        Console.WriteLine("11 - ClosePartiallyPositionExample");
                        Console.WriteLine("12 - CloseByExample");
                        Console.WriteLine("0 - Exit");
                        Console.Write("Please select : ");

                        string command = Console.ReadLine();

                        Example example;

                        if (command == "1")
                        {
                            example = new TradeServerInfoExample(address, username, password);
                        }
                        else if (command == "2")
                        {
                            example = new AccountInfoExample(address, username, password);
                        }
                        else if (command == "3")
                        {
                            example = new GetOrdersExample(address, username, password);
                        }
                        else if (command == "4")
                        {
                            example = new SendMarketOrderExample(address, username, password);
                        }
                        else if (command == "5")
                        {
                            example = new SendLimitOrderExample(address, username, password);
                        }
                        else if (command == "6")
                        {
                            example = new SendStopOrderExample(address, username, password);
                        }
                        else if (command == "7")
                        {
                            example = new SendStopLimitOrderExample(address, username, password);
                        }
                        else if (command == "8")
                        {
                            example = new ModifyTradeRecordExample(address, username, password);
                        }
                        else if (command == "9")
                        {
                            example = new DeletePendingOrderExample(address, username, password);
                        }
                        else if (command == "10")
                        {
                            example = new ClosePositionExample(address, username, password);
                        }
                        else if (command == "11")
                        {
                            example = new ClosePartiallyPositionExample(address, username, password);
                        }
                        else if (command == "12")
                        {
                            example = new CloseByExample(address, username, password);
                        }
                        else if (command == "0")
                        {
                            break;
                        }
                        else
                            throw new Exception("Invalid command : " + command);

                        using (example)
                        {
                            example.Run();
                        }
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine("Error : " + exception.Message);
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error : " + exception.Message);
            }
        }
    }
}
