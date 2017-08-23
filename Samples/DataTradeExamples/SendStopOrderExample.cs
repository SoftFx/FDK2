namespace DataTradeExamples
{
    using System;
    using TickTrader.FDK.Extended;
    using TickTrader.FDK.Objects;

    class SendStopOrderExample : Example
    {
        public SendStopOrderExample(string address, string username, string password)
            : base(address, username, password)
        {
        }

        protected override void RunExample()
        {
            var record = this.Trade.Server.SendOrder("EURUSD", TradeCommand.Stop, TradeRecordSide.Sell, 10000, null, null, 1.0, null, null, null, null, null, null);
            Console.WriteLine(record);
        }
    }
}