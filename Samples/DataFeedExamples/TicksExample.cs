namespace DataFeedExamples
{
    using System;
    using TickTrader.FDK.Extended;

    class TicksExample : Example
    {
        public TicksExample(string address, string username, string password)
            : base(address, username, password)
        {
            this.Feed.Subscribed += this.OnSubscribed;
            this.Feed.Unsubscribed += this.OnUnsubscribed;
            this.Feed.Tick += this.OnTick;
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

        void OnSubscribed(object sender, SubscribedEventArgs e)
        {
            Console.WriteLine("OnSubscribed(): {0}", e);
        }

        void OnUnsubscribed(object sender, UnsubscribedEventArgs e)
        {
            Console.WriteLine("OnUnsubscribed(): {0}", e.Symbol);
        }

        void OnTick(object sender, TickEventArgs e)
        {
            Console.WriteLine("OnTick(): {0}", e);
        }        
    }
}
