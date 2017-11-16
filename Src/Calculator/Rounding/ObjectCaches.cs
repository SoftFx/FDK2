namespace TickTrader.FDK.Calculator.Rounding
{
    using System;
    using System.Linq;

    public static class ObjectCaches
    {
        public static readonly ISimpleObjectCache<int, RoundingTools> RoundingTools =
            new SimpleObjectFactoryCache<int, RoundingTools>(o => new RoundingTools(o), Enumerable.Range(1, 10));

        public static RoundingTools WithPrecision(this ISimpleObjectCache<int, RoundingTools> cache, int precision)
        {
            if (cache == null)
                throw new ArgumentNullException("cache");

            return cache.Get(precision);
        }
    }
}
