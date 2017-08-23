namespace DataFeedExamples
{
    using System;

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
                        Console.WriteLine("1 - SymbolInfoExample");
                        Console.WriteLine("2 - TicksExample");
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
