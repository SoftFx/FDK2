namespace DataTradeExamples
{
    using System;
    using TickTrader.FDK.Extended;
    using TickTrader.FDK.Common;

    class SendStopOrderExample : Example
    {
        public SendStopOrderExample(string address, string username, string password)
            : base(address, username, password)
        {
        }

        protected override void RunExample()
        {
            var record = this.Trade.Server.SendOrder("EURUSD", OrderType.Stop, OrderSide.Sell, 10000, null, null, 1.0, null, null, null, null, null, null, null);
            Console.WriteLine(record);
        }
    }
}