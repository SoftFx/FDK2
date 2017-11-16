namespace DataTradeExamples
{
    using System;
    using TickTrader.FDK.Extended;
    using TickTrader.FDK.Common;

    class CloseByExample : Example
    {
        public CloseByExample(string address, string username, string password)
            : base(address, username, password)
        {
        }

        protected override void RunExample()
        {
            var buyRecord = this.Trade.Server.SendOrder("EURUSD", TradeCommand.Market, OrderSide.Buy, 100000, null, null, null, null, null, null, null, null, null);
            Console.WriteLine(buyRecord);
            var sellRecord = this.Trade.Server.SendOrder("EURUSD", TradeCommand.Market, OrderSide.Sell, 120000, null, null, null, null, null, null, null, null, null);
            Console.WriteLine(sellRecord);
            buyRecord.CloseBy(sellRecord);
            Console.WriteLine("Positions have been closed");
        }
    }
}
