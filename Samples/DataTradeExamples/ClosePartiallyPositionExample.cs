namespace DataTradeExamples
{
    using System;
    using TickTrader.FDK.Extended;
    using TickTrader.FDK.Objects;

    class ClosePartiallyPositionExample : Example
    {
        public ClosePartiallyPositionExample(string address, string username, string password)
            : base(address, username, password)
        {
        }

        protected override void RunExample()
        {
            var record = this.Trade.Server.SendOrder("EURUSD", TradeCommand.Market, TradeRecordSide.Buy, 20000, null, null, null, null, null, null, null, null, null);
            Console.WriteLine(record);
            var result1 = record.ClosePartially(10000);
            Console.WriteLine(result1);
            var result2 = record.ClosePartially(10000);
            Console.WriteLine(result2);
        }
    }
}
