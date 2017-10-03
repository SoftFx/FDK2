namespace DataFeedExamples
{
    using System;
    using System.Globalization;
    using TickTrader.FDK.Common;
    using TickTrader.FDK.Extended;

    class BarsHistoryExample : Example
    {
        public BarsHistoryExample(string address, string username, string password)
            : base(address, username, password)
        {
        }

        protected override void RunExample()
        {
            var startTime = DateTime.Parse("06/01/2017 08:00:00", CultureInfo.InvariantCulture);
            var endTime = DateTime.Parse("06/30/2017 07:59:59", CultureInfo.InvariantCulture);

            var bars = this.Feed.Server.GetBarsHistory("EURUSD", PriceType.Bid, BarPeriod.M1, startTime, endTime);

            var sumDeviation = 0D;
            var count = 0;

            foreach (var bar in bars)
            {
                sumDeviation += (bar.High - bar.Low);
                count++;
            }

            if (count != 0)
            {
                var averageDeviation = sumDeviation / count;
                Console.WriteLine("Average deviation = {0}", averageDeviation);
            }
            else
                Console.WriteLine("Average deviation = NA");
        }
    }
}
