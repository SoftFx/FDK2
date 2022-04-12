using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TickTrader.FDK.Common;

namespace DataFeedExamples
{
    class BarsExample : Example
    {
        public BarsExample(string address, string username, string password)
            : base(address, username, password)
        {
        }

        protected override void RunExample()
        {
            var symbols = new List<BarSubscriptionSymbolEntry>
            {
                new BarSubscriptionSymbolEntry
                {
                    Symbol = "EURUSD",
                    Params = new[]
                    {
                        new BarParameters(Periodicity.Parse("M1"), PriceType.Ask)
                    }
                }
            };

            this.Feed.Server.SubscribeToBars(symbols);

            Console.WriteLine("Press any key to stop");
            Console.ReadKey();

            this.Feed.Server.UnsubscribeBars(symbols.Select(it => it.Symbol));
        }
    }
}
