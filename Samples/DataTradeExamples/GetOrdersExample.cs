namespace DataTradeExamples
{
    using System;
    using TickTrader.FDK.Extended;
    using TickTrader.FDK.Objects;

    class GetOrdersExample : Example
    {
        public GetOrdersExample(string address, string username, string password)
            : base(address, username, password)
        {
        }

        protected override void RunExample()
        {
            var records = this.Trade.Server.GetTradeRecords();
            Console.WriteLine("Records number = {0}", records.Length);
            foreach (var record in records)
                Console.WriteLine(record);
        }
    }
}