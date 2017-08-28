namespace DataTradeExamples
{
    using System;
    using TickTrader.FDK.Extended;
    using TickTrader.FDK.Common;

    class TradeServerInfoExample : Example
    {
        public TradeServerInfoExample(string address, string username, string password)
            : base(address, username, password)
        {
        }

        protected override void RunExample()
        {
            TradeServerInfo tradeServerInfo = Trade.Cache.TradeServerInfo;
            Console.WriteLine(tradeServerInfo);
        }
    }
}
