namespace DataTradeExamples
{
    using System;
    using TickTrader.FDK.Extended;
    using TickTrader.FDK.Objects;

    class CloseByExample : Example
    {
        public CloseByExample(string address, string username, string password)
            : base(address, username, password)
        {
        }

        protected override void RunExample()
        {
            var buyRecord = this.Trade.Server.SendOrder("EURUSD", TradeCommand.Market, TradeRecordSide.Buy, 100000, null, null, null, null, null, null, null, null, null);
            Console.WriteLine(buyRecord);
            var sellRecord = this.Trade.Server.SendOrder("EURUSD", TradeCommand.Market, TradeRecordSide.Sell, 120000, null, null, null, null, null, null, null, null, null);
            Console.WriteLine(sellRecord);
            buyRecord.CloseBy(sellRecord);
            Console.WriteLine("Positions have been closed");
        }
    }
}
