namespace DataTradeExamples
{
    using System;
    using System.Threading;
    using TickTrader.FDK.Extended;
    using TickTrader.FDK.Common;

    class TradeExample : Example
    {
        public TradeExample(string address, string username, string password)
            : base(address, username, password)
        {
        }

        protected override void RunExample()
        {
            DateTime from = DateTime.Parse("01.01.2017 00:00:00");

            SubscribeTradeTransactionReportsEnumerator enumerator = this.Trade.Server.SubscribeTradeTransactionReports(from, true);

            while (enumerator.MoveNext())
            {
                Console.WriteLine("Trade update : {0}", enumerator.Current);
            }

            Console.WriteLine("Press any key to stop");
            Console.ReadKey();

            this.Trade.Server.UnsubscribeTradeTransactionReports();
        }
    }
}
