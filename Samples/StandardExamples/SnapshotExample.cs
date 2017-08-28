namespace StandardExamples
{
    using System;
    using TickTrader.FDK.Common;
    using TickTrader.FDK.Standard;

    class SnapshotExample : Example
    {
        public SnapshotExample(string address, string username, string password)
            : base(address, username, password)
        {
        }

        protected override void RunExample()
        {
            Console.WriteLine("Press any key to stop");
            Console.ReadKey();
        }

        protected override void OnUpdated(object sender, EventArgs args)
        {
            try
            {
                Snapshot snapshot = this.Manager.TakeSnapshot("EURUSD", PriceType.Ask, BarPeriod.M1);
                Console.WriteLine("Snapshot : {0}; {1}; {2}; {3}; {4}; {5}", snapshot.ServerDateTime, snapshot.Quotes?.Count, snapshot.TradeRecords?.Count, snapshot.Positions?.Count, snapshot.AccountInfo?.Margin, snapshot.AccountInfo?.Balance);
            }
            catch
            {
            }
        }
    }
}