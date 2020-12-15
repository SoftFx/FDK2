namespace DataTradeExamples
{
    using System;
    using TickTrader.FDK.Extended;
    using TickTrader.FDK.Common;

    class SendStopLimitOrderExample : Example
    {
        public SendStopLimitOrderExample(string address, string username, string password)
            : base(address, username, password)
        {
        }

        protected override void RunExample()
        {
            var record = this.Trade.Server.SendOrder("EURUSD", OrderType.StopLimit, OrderSide.Sell, 10000, null, 1.15, 1.0, null, null, OrderTimeInForce.GoodTillCancel, null, null, null, null, false, null, false, false, null);
            Console.WriteLine(record);
        }
    }
}
