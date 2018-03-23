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
            this.Trade.Server.SubscribeTradeTransactionReports(DateTime.UtcNow, true);

            Console.WriteLine("Press any key to stop");
            Console.ReadKey();

            this.Trade.Server.UnsubscribeTradeTransactionReports();
        }
    }
}
