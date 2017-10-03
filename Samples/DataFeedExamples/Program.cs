namespace DataFeedExamples
{
    using System;

    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    Console.WriteLine("Usage: DataFeedExamples <address> <login> <password>");

                    return;
                }

                if (args.Length < 3)
                    throw new Exception("Invalid command line");

                string address = args[0];
                string username = args[1];
                string password = args[2];

                while (true)
                {
                    try
                    {
                        Console.WriteLine("1 - SymbolInfoExample");
                        Console.WriteLine("2 - TicksExample");
                        Console.WriteLine("3 - BarsHistoryExample");
                        Console.WriteLine("4 - TicksHistoryExample");
                        Console.WriteLine("0 - Exit");
                        Console.Write("Please select : ");

                        string command = Console.ReadLine();

                        Example example;

                        if (command == "1")
                        {
                            example = new SymbolInfoExample(address, username, password);
                        }
                        else if (command == "2")
                        {
                            example = new TicksExample(address, username, password);
                        }
                        else if (command == "3")
                        {
                            example = new BarsHistoryExample(address, username, password);
                        }
                        else if (command == "4")
                        {
                            example = new TicksHistoryExample(address, username, password);
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
