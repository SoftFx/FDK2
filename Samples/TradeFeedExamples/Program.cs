namespace TradeFeedExamples
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
                    Console.WriteLine("Usage: TradeFeedExamples <address> <login> <password>");

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
                        Console.WriteLine("1 - StateCalculatorExample");
                        Console.WriteLine("0 - Exit");
                        Console.Write("Please select : ");

                        string command = Console.ReadLine();

                        Example example;

                        if (command == "1")
                        {
                            example = new StateCalculatorExample(address, username, password);
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
