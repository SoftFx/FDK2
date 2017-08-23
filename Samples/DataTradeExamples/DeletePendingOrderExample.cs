namespace DataTradeExamples
{
    using System;
    using TickTrader.FDK.Extended;
    using TickTrader.FDK.Objects;

    class DeletePendingOrderExample : Example
    {
        public DeletePendingOrderExample(string address, string username, string password)
            : base(address, username, password)
        {
        }

        protected override void RunExample()
        {
            var record = this.Trade.Server.SendOrder("EURUSD", TradeCommand.Limit, TradeRecordSide.Buy, 10000, null, 1.0, null, null, null, null, null, null, null);
            Console.WriteLine(record);
            record.Delete();
            Console.WriteLine("Order has been deleted");
        }
    }
}
