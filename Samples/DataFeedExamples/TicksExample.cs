namespace DataFeedExamples
{
    using System;
    using TickTrader.FDK.Extended;

    class TicksExample : Example
    {
        public TicksExample(string address, string username, string password)
            : base(address, username, password)
        {
        }

        protected override void RunExample()
        {
            var symbols = new[]
            {
                "EURUSD",
                "EURJPY",
            };
                    
            this.Feed.Server.SubscribeToQuotes(symbols, 3);

            Console.WriteLine("Press any key to stop");
            Console.ReadKey();

            this.Feed.Server.UnsubscribeQuotes(symbols);
        }
    }
}
