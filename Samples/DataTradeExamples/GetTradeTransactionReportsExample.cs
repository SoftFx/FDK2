namespace DataTradeExamples
{
    using System;
    using System.Threading;
    using TickTrader.FDK.Extended;
    using TickTrader.FDK.Common;

    class GetTradeTransactionReportsExample : Example
    {
        public GetTradeTransactionReportsExample(string address, string username, string password)
            : base(address, username, password)
        {
        }

        protected override void RunExample()
        {
            DateTime to = DateTime.UtcNow;
            DateTime from = to.AddDays(-1);

            using (TradeTransactionReportsEnumerator enumerator = this.Trade.Server.GetTradeTransactionReports(TimeDirection.Forward, true, from, to, false))
            {
                for (; ! enumerator.EndOfStream; enumerator.Next())
                    Console.WriteLine(enumerator.Item);
            }

            Console.WriteLine("Press any key to stop");
            Console.ReadKey();

            this.Trade.Server.UnsubscribeTradeTransactionReports();
        }
    }
}
