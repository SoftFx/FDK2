using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TickTrader.FDK.Common;

namespace TickTrader.FDK.Client.Splits
{

    struct CacheBarId
    {
        public Periodicity Periodicity { get; private set; }
        public DateTime Time { get; private set; }

        public CacheBarId(Periodicity periodicity, DateTime time)
        {
            Periodicity = periodicity;
            Time = Periodicity.GetPeriodStartTime(time);
        }
    }

    class SEModifiedSymbolBarsCache
    {
        private ConcurrentDictionary<CacheBarId, Bar> _modifiedBars;
        private int version_;
        public void ValidateVersion(int version)
        {
            if (version != version_)
            {
                version_ = version;
                _modifiedBars.Clear();
            }
        }

        public SEModifiedSymbolBarsCache()
        {
            _modifiedBars = new ConcurrentDictionary<CacheBarId, Bar>();
        }

        public bool TryGetBar(CacheBarId barId, ref Bar bar)
        {
            Bar cacheBar;
            bool exist = _modifiedBars.TryGetValue(barId, out cacheBar);
            if (exist)
                bar = cacheBar;
            return exist;
        }

        public void InsertBar(CacheBarId barId, Bar bar)
        {
            _modifiedBars[barId] = bar;
        }
    }

    class SEModifiedBarsCache
    {
        private ConcurrentDictionary<string, SEModifiedSymbolBarsCache> _symbolToCache;



        public SEModifiedBarsCache()
        {
            _symbolToCache = new ConcurrentDictionary<string, SEModifiedSymbolBarsCache>();
        }

        public bool TryGetBar(string symbol, Periodicity periodicity, DateTime time, ref Bar bar)
        {
            if (!_symbolToCache.ContainsKey(symbol))
            {
                return false;
            }
            SEModifiedSymbolBarsCache symbolCache = _symbolToCache[symbol];
            Bar cacheBar;
            return symbolCache.TryGetBar(new CacheBarId(periodicity, time), ref bar);
        }

        public void InsertBar(string symbol, Periodicity periodicity, Bar bar)
        {
            var symbolCache = _symbolToCache.GetOrAdd(symbol, new SEModifiedSymbolBarsCache());
            symbolCache.InsertBar(new CacheBarId(periodicity, bar.From), bar);
        }

        public void ClearSymbol(string symbol)
        {
            SEModifiedSymbolBarsCache cache;
            _symbolToCache.TryRemove(symbol, out cache);
        }
    }
}
