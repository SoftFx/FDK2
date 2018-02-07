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
            var startTime = DateTime.Parse("06/01/2017 08:00:00Z", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
            var endTime = DateTime.Parse("06/30/2017 07:59:59Z", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);

            var bars = this.Feed.Server.GetBarsHistory("EURUSD", BarPeriod.M1, startTime, endTime);

            var bidSumDeviation = 0D;
            var bidCount = 0;

            var askSumDeviation = 0D;
            var askCount = 0;            

            foreach (var bar in bars)
            {
                if (bar.Bid != null)
                {
                    bidSumDeviation += (bar.Bid.High - bar.Bid.Low);
                    bidCount++;
                }               

                if (bar.Ask != null)
                {
                    askSumDeviation += (bar.Ask.High - bar.Ask.Low);
                    askCount++;
                }                
            }

            if (bidCount != 0)
            {
                var averageDeviation = bidSumDeviation / bidCount;
                Console.WriteLine("Average bid deviation = {0}", averageDeviation);
            }
            else
                Console.WriteLine("Average bid deviation = NA");

            if (askCount != 0)
            {
                var averageDeviation = askSumDeviation / askCount;
                Console.WriteLine("Average ask deviation = {0}", averageDeviation);
            }
            else
                Console.WriteLine("Average ask deviation = NA");
        }
    }
}
