namespace DataFeedExamples
{
    using System;
    using System.Globalization;
    using TickTrader.FDK.Common;
    using TickTrader.FDK.Extended;

    class TicksHistoryExample : Example
    {
        public TicksHistoryExample(string address, string username, string password)
            : base(address, username, password)
        {
        }

        protected override void RunExample()
        {
            var startTime = DateTime.Parse("06/01/2017 15:00:00Z", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
            var endTime = DateTime.Parse("06/01/2017 15:59:59Z", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);

            var quotes = this.Feed.Server.GetQuotesHistory("EURUSD", startTime, endTime, 1);

            var sumSpread = 0D;
            var count = 0;

            foreach (var quote in quotes)
            {
                if (quote.HasAsk && quote.HasBid)
                {
                    sumSpread += quote.Spread;
                    count++;
                }
            }

            if (count != 0)
            {
                var averageSpread = sumSpread / count;
                Console.WriteLine("Average spread = {0}", averageSpread);
            }
            else
                Console.WriteLine("Average spread = NA");
        }
    }
}
