namespace DataTradeExamples
{
    using System;
    using System.Threading;
    using TickTrader.FDK.Extended;
    using TickTrader.FDK.Common;

    class AccountHistoryExample : Example
    {
        public AccountHistoryExample(string address, string username, string password)
            : base(address, username, password)
        {
        }

        protected override void RunExample()
        {
            DateTime from = DateTime.Parse("01.01.2017 00:00:00");
            DateTime to = DateTime.Parse("01.01.2018 00:00:00");

            double minMargin = double.MaxValue;
            double maxMargin = double.MinValue;

            foreach (AccountReport accountReport in this.Trade.Server.GetAccountHistory(TimeDirection.Forward, from, to))
            {
                if (accountReport.Margin < minMargin)
                    minMargin = accountReport.Margin;

                if (accountReport.Margin > maxMargin)
                    maxMargin = accountReport.Margin;
            }

            Console.WriteLine("MinMargin = {0}, MaxMargin = {1}", minMargin, maxMargin);
        }
    }
}
