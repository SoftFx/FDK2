namespace DataTradeExamples
{
    using System;
    using TickTrader.FDK.Extended;
    using TickTrader.FDK.Common;

    class SendLimitOrderExample : Example
    {
        public SendLimitOrderExample(string address, string username, string password)
            : base(address, username, password)
        {
        }

        protected override void RunExample()
        {
            var record = this.Trade.Server.SendOrderEx(Guid.NewGuid().ToString(), "EURUSD", TradeCommand.Limit, TradeRecordSide.Buy, 100000, null, 1.15, null, null, null, null, null, null, null);
            Console.WriteLine(record);
        }
    }
}