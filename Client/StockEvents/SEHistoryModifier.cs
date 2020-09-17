using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TickTrader.FDK.Common;

namespace TickTrader.FDK.Client.Splits
{

    interface ISplitStorge
    {
        ReadOnlyCollection<Split> GetSymbolSplits(string symbol);
    }

    interface IHistoryStorageProvider
    {
        List<Bar> GetBars(DateTime to, int maxBars, string symbol, string periodicity, PriceType priceType);
        List<Quote> GetTicks(DateTime to, int maxTicks, string symbol, bool includeLevel2);
    }

    interface IModifiedBarSlowGetter
    {
        bool TryGetBar(DateTime time, string symbol, Periodicity periodicity, PriceType priceType, ref Bar bar);
    }

    class SEHistoryRangeModifier
    {
        private int _eventInd = 0;
        private List<KeyValuePair<DateTime, double>> _splitsAccumCoefList;
        private SEModifiedBarsCache _cache;
        private IModifiedBarSlowGetter _slowBarGetter;
        private string _symbol;
        Periodicity _periodicity;
        PriceType _priceType;
        public SEHistoryRangeModifier(string symbol, Periodicity periodicity, PriceType priceType, List<KeyValuePair<DateTime, double>> splitsAccumCoefList, SEModifiedBarsCache cache, IModifiedBarSlowGetter slowBarGetter)
        {
            _symbol = symbol;
            _splitsAccumCoefList = splitsAccumCoefList;
            _cache = cache;
            _slowBarGetter = slowBarGetter;
            _periodicity = periodicity;
            _priceType = priceType;


            if (periodicity != Periodicity.None)
            {
                Bar bar = new Bar();
                foreach (var split in _splitsAccumCoefList)
                    if (!_cache.TryGetBar(symbol, periodicity, periodicity.GetPeriodStartTime(split.Key), ref bar))
                    {
                        if (_slowBarGetter.TryGetBar(periodicity.GetPeriodStartTime(split.Key), _symbol, _periodicity, _priceType, ref bar))
                            _cache.InsertBar(_symbol, _periodicity, bar);
                    }
            }
        }

        private KeyValuePair<DateTime, double> GetTimeAndCoef(DateTime time)
        {
            if (_splitsAccumCoefList.Count > 1)
            {
                while (_splitsAccumCoefList[_eventInd].Key <= time)
                    _eventInd++;
                while (_eventInd > 0 && _splitsAccumCoefList[_eventInd - 1].Key > time)
                    _eventInd--;
                return _splitsAccumCoefList[_eventInd];
            }
            else return _splitsAccumCoefList.Last();
        }

        public void ModifyTick(ref Quote tick)
        {
            if (_splitsAccumCoefList.Count > 0)
            {
                var splitTimeAndCoef = GetTimeAndCoef(tick.CreatingTime);
                if (splitTimeAndCoef.Value != 1.0 && splitTimeAndCoef.Value > 0.0)
                {
                    for (int i = 0; i < tick.Bids.Count; i++)
                        tick.Bids[i] = new QuoteEntry() { Price = tick.Bids[i].Price / splitTimeAndCoef.Value, Volume = tick.Bids[i].Volume * splitTimeAndCoef.Value };
                    for (int i = 0; i < tick.Asks.Count; i++)
                        tick.Asks[i] = new QuoteEntry() { Price = tick.Asks[i].Price / splitTimeAndCoef.Value, Volume = tick.Asks[i].Volume * splitTimeAndCoef.Value };
                }
            }
        }

        public void ModifyBar(ref Bar bar)
        {
            if (_splitsAccumCoefList.Count > 0)
            {
                var splitTimeAndCoef = GetTimeAndCoef(bar.From);
                if (_periodicity.GetPeriodEndTime(bar.From) < splitTimeAndCoef.Key)
                {
                    if (splitTimeAndCoef.Value != 1.0 && splitTimeAndCoef.Value > 0.0)
                    {
                        bar.Open = bar.Open / splitTimeAndCoef.Value;
                        bar.High = bar.High / splitTimeAndCoef.Value;
                        bar.Low = bar.Low / splitTimeAndCoef.Value;
                        bar.Close = bar.Close / splitTimeAndCoef.Value;
                        bar.Volume = bar.Volume * splitTimeAndCoef.Value;
                    }
                }
                else
                {
                    _cache.TryGetBar(_symbol, _periodicity, bar.From, ref bar);
                }
            }
        }
    }

    class SEHistoryModifier
    {
        private SEModifiedBarsCache _cache;
        private IModifiedBarSlowGetter _slowBarGetter;


        private List<KeyValuePair<DateTime, double>> BuildCoefList(ReadOnlyCollection<SEQHModifier> splits)
        {
            DateTime now = DateTime.UtcNow;
            int actualSplitCount = 0;

            for (int i = splits.Count - 1; i >= 0; i--)
                if (splits[i].StartTime <= now)
                {
                    actualSplitCount = i + 1;
                    break;
                }

            List<KeyValuePair<DateTime, double>> result = new List<KeyValuePair<DateTime, double>>();
            result.Add(new KeyValuePair<DateTime, double>(DateTime.MaxValue, 1.0));

            double accumRatio = 1.0;
            for (int i = 0; i < actualSplitCount; i++)
            {
                var split = splits[actualSplitCount - 1 - i];
                if(split.FromFactor == 0 || split.ToFactor == 0)
                    accumRatio *= split.Ratio;
                else
                {
                    var ratio = 1.0 * split.ToFactor / split.FromFactor;
                    accumRatio *= ratio;
                }
                result.Add(new KeyValuePair<DateTime, double>(split.StartTime, accumRatio));
            }

            result.Reverse();

            return result;
        }

        public SEHistoryModifier(IModifiedBarSlowGetter getter, SEModifiedBarsCache cache)
        {
            _cache = cache;
            _slowBarGetter = getter;
        }

        public SEHistoryRangeModifier GetRangeModifier(string symbol, Periodicity periodicity, PriceType priceType, ReadOnlyCollection<SEQHModifier> splits)
        {
            return new SEHistoryRangeModifier(symbol, periodicity, priceType, BuildCoefList(splits), _cache, _slowBarGetter);
        }
    }
}
