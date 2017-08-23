namespace DataTradeExamples
{
    using System;
    using TickTrader.FDK.Extended;
    using TickTrader.FDK.Objects;

    class SendMarketOrderExample : Example
    {
        public SendMarketOrderExample(string address, string username, string password)
            : base(address, username, password)
        {
        }

        protected override void RunExample()
        {
            var record = this.Trade.Server.SendOrder("EURUSD", TradeCommand.Market, TradeRecordSide.Buy, 10000, null, null, null, null, null, null, null, null, null);
            Console.WriteLine(record);
        }        
    }
}
