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
            var record = this.Trade.Server.SendOrderEx(Guid.NewGuid().ToString(), "EURUSD", OrderType.Limit, OrderSide.Buy, 100000, null, 1.15, null, null, null, OrderTimeInForce.GoodTillCancel, null, null, null, null, false, null, false, false, null);
            Console.WriteLine(record);
        }
    }
}