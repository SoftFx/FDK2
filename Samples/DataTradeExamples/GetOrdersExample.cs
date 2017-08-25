namespace DataTradeExamples
{
    using System;
    using TickTrader.FDK.Extended;
    using TickTrader.FDK.Common;

    class GetOrdersExample : Example
    {
        public GetOrdersExample(string address, string username, string password)
            : base(address, username, password)
        {
        }

        protected override void RunExample()
        {
            var records = this.Trade.Cache.TradeRecords;

            Console.WriteLine("Records number = {0}", records.Length);
            foreach (var record in records)
                Console.WriteLine(record);
        }
    }
}