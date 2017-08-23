namespace DataTradeExamples
{
    using System;
    using TickTrader.FDK.Extended;
    using TickTrader.FDK.Objects;

    class TradeServerInfoExample : Example
    {
        public TradeServerInfoExample(string address, string username, string password)
            : base(address, username, password)
        {
        }

        protected override void RunExample()
        {
            TradeServerInfo tradeServerInfo = Trade.Server.GetTradeServerInfo();
            Console.WriteLine(tradeServerInfo);
        }
    }
}
