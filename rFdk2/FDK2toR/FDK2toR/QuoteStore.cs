using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TickTrader.FDK.Common;
using TickTrader.FDK.QuoteStore;

namespace FDK2toR
{
    class QuoteStore
    {
        private const int Timeout = 30000;
        private static Client _client;
        private static List<Quote> _ticks;
        private static List<Bar> _bars; 

        #region Connection
        public static int Connect(string address, string login, string password)
        {
            try
            {
                _client = new Client("name", 5050, false, "Logs", true);
                _client.Connect(address, Timeout);
                _client.Login(login, password, "", "", "", Timeout);
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return -1;
            }
        }

        public static int Disconnect()
        {
            try
            {
                _client.Logout("Client logout", Timeout);
                _client.Disconnect("Clent disconnect");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return -1;
            }
        }

        #endregion

        #region Ticks

        public static void GetTickList(string symbol, DateTime from, double count)
        {
            try
            {
                _ticks?.Clear();
                _ticks = _client.GetQuoteList(symbol, QuoteDepth.Top, from, (int)count, Timeout).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        public static double[] GetTickBidPrice()
        {
            return _ticks.Select(it => it.Bids.First().Price).ToArray();
        }
        public static double[] GetTickBidVolume()
        {
            return _ticks.Select(it => it.Bids.First().Volume).ToArray();
        }
        public static double[] GetTickAskPrice()
        {
            return _ticks.Select(it => it.Asks.First().Price).ToArray();
        }
        public static double[] GetTickAskVolume()
        {
            return _ticks.Select(it => it.Asks.First().Volume).ToArray();
        }

        public static DateTime[] GetTickTimestamp()
        {
            return _ticks.Select(it => it.CreatingTime).ToArray();
        }
        #endregion

        #region Ticks L2

        public static void GetTickL2List(string symbol, DateTime from, double count)
        {
            try
            {
                _ticks?.Clear();
                _ticks = _client.GetQuoteList(symbol, QuoteDepth.Level2, from, (int)count, Timeout).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        public static double[] GetTickL2BidVolume()
        {
            var result = new List<double>();
            foreach (var tick in _ticks)
            {
                var buf = tick.HasBid
                    ? tick.Bids.Select(bid => bid.Volume).ToList()
                    : new List<double>(tick.Asks.Count);
                var count = buf.Count;
                for (var i = 1; i <= tick.Asks.Count - count; i++)
                {
                    buf.Add(0);
                }
                result.AddRange(buf);
            }
            return result.ToArray();
        }

        public static double[] GetTickL2AskVolume()
        {
            var result = new List<double>();
            foreach (var tick in _ticks)
            {
                var buf = tick.HasAsk ? tick.Asks.Select(ask => ask.Volume).ToList() : new List<double>(tick.Bids.Count);
                var count = buf.Count;
                for (var i = 1; i <= tick.Bids.Count - count; i++)
                {
                    buf.Add(0);
                }
                result.AddRange(buf);
            }
            return result.ToArray();
        }

        public static double[] GetTickL2BidPrice()
        {
            var result = new List<double>();
            foreach (var tick in _ticks)
            {
                var buf = tick.HasBid ? tick.Bids.Select(bid => bid.Price).ToList() : new List<double>(tick.Asks.Count);
                var count = buf.Count;
                for (var i = 1; i <= tick.Asks.Count - count; i++)
                {
                    buf.Add(0);
                }
                result.AddRange(buf);
            }
            return result.ToArray();
        }

        public static double[] GetTickL2AskPrice()
        {
            var result = new List<double>();
            foreach (var tick in _ticks)
            {
                var buf = tick.HasAsk ? tick.Asks.Select(ask => ask.Price).ToList() : new List<double>(tick.Bids.Count);
                var count = buf.Count;
                for (var i = 1; i <= tick.Bids.Count - count; i++)
                {
                    buf.Add(0);
                }
                result.AddRange(buf);
            }
            return result.ToArray();
        }

        public static int[] GetTickL2Level()
        {
            var result = new List<int>();
            foreach (var tick in _ticks)
            {
                result.AddRange(Enumerable.Range(1, Math.Max(tick.Bids.Count, tick.Asks.Count)));
            }
            return result.ToArray();
        }

        public static DateTime[] GetTickL2Timestamp()
        {
            var result = new List<DateTime>();
            foreach (var tick in _ticks)
            {
                for (int level = 0; level < Math.Max(tick.Bids.Count, tick.Asks.Count); level++)
                {
                    result.Add(tick.CreatingTime);
                }
            }
            return result.ToArray();
        }
        #endregion

        #region Bars

        public static void GetBarList(string symbol, string priceType, string periodicity, DateTime from, double count)
        {
            try
            {
                _bars?.Clear();
                _bars =
                    _client.GetBarList(symbol, priceType.Equals("Ask") ? PriceType.Ask : PriceType.Bid,
                        new BarPeriod(periodicity), from, (int) count, Timeout).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public static DateTime[] GetBarFrom()
        {
            return _bars.Select(it => it.From).ToArray();
        }
        public static DateTime[] GetBarTo()
        {
            return _bars.Select(it => it.To).ToArray();
        }
        public static double[] GetBarOpen()
        {
            return _bars.Select(it => it.Open).ToArray();
        }
        public static double[] GetBarClose()
        {
            return _bars.Select(it => it.Close).ToArray();
        }
        public static double[] GetBarHigh()
        {
            return _bars.Select(it => it.High).ToArray();
        }
        public static double[] GetBarLow()
        {
            return _bars.Select(it => it.Low).ToArray();
        }
        public static double[] GetBarVolume()
        {
            return _bars.Select(it => it.Volume).ToArray();
        }

        #endregion
    }
}
