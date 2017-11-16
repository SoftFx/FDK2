namespace TickTrader.FDK.Calculator.Rounding
{
    using System;
    using System.Collections.Generic;

    public interface ISimpleObjectCache<TKey, T>
    {
        T Get(TKey key);
    }

    public class SimpleObjectCache<TKey, T> : SimpleObjectFactoryCache<TKey, T>
        where T : new()
    {
        public SimpleObjectCache()
            : base(o => new T())
        {
        }

        public SimpleObjectCache(IEnumerable<TKey> keys)
            : base(o => new T(), keys)
        {
        }
    }

    public class SimpleObjectFactoryCache<TKey, T>: ISimpleObjectCache<TKey, T>
    {
        readonly IDictionary<TKey, T> cache;
        readonly Func<TKey, T> factory;

        public SimpleObjectFactoryCache(Func<TKey, T> factory)
        {
            if (factory == null)
                throw new ArgumentNullException("factory");

            this.cache = new Dictionary<TKey, T>();
            this.factory = factory;
        }

        public SimpleObjectFactoryCache(Func<TKey, T> factory, IEnumerable<TKey> keys)
            : this(factory)
        {
            if (keys == null)
                throw new ArgumentNullException("keys");

            foreach (var key in keys)
                this.Get(key);
        }

        public T Get(TKey key)
        {
            return this.GetOrSet(key);
        }

        T GetOrSet(TKey key)
        {
            if (this.cache.ContainsKey(key))
                return this.cache[key];

            return this.cache[key] = this.factory(key);
        }
    }
}
