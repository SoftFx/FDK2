namespace DataTradeExamples
{
    using System;
    using System.Threading;
    using TickTrader.FDK.Extended;
    using TickTrader.FDK.Common;

    class TradeHistoryExample : Example
    {
        public TradeHistoryExample(string address, string username, string password)
            : base(address, username, password)
        {
        }

        protected override void RunExample()
        {
            DateTime from = DateTime.Parse("01.01.2017 00:00:00");
            DateTime to = DateTime.Parse("01.01.2018 00:00:00");

            double minQuantity = double.MaxValue;
            double maxQuantity = double.MinValue;

            foreach (TradeTransactionReport tradeReport in this.Trade.Server.GetTradeTransactionReportsHistory(TimeDirection.Forward, from, to, true))
            {
                if (tradeReport.Quantity < minQuantity)
                    minQuantity = tradeReport.Quantity;

                if (tradeReport.Quantity > maxQuantity)
                    maxQuantity = tradeReport.Quantity;
            }

            Console.WriteLine("MinQuantity={0}, MaxQuantity={1}", minQuantity, maxQuantity);
        }
    }
}
