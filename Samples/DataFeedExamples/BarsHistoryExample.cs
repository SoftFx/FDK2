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
            var historyInfo = this.Feed.Server.GetBarsHistoryInfo("EURUSD", PriceType.Bid, BarPeriod.M1);
            Console.WriteLine("BarsHistoryInfo: EURUSD, Bid, M1");
            Console.WriteLine(historyInfo);

            var startTime = historyInfo.AvailFrom ?? DateTime.Parse("06/01/2017 08:00:00Z", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
            var endTime = startTime.AddHours(1);

            Console.WriteLine("GetBarsHistory: EURUSD, M1 from {0} to {1}", startTime, endTime);
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
