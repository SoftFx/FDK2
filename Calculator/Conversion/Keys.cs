using System;

namespace TickTrader.FDK.Calculator.Conversion
{
    struct CurrencyToCurrencyKey : IEquatable<CurrencyToCurrencyKey>
    {
        private readonly string from;
        private readonly string to;
        private readonly int hash;

        public CurrencyToCurrencyKey(string from, string to)
        {
            this.from = from;
            this.to = to;

            unchecked
            {
                hash = (int)2166136261;
                hash = hash * 16777619 ^ from.GetHashCode();
                hash = hash * 16777619 ^ to.GetHashCode();
            }
        }

        public CurrencyToCurrencyKey Invert() { return new CurrencyToCurrencyKey(to, from); }

        public override bool Equals(object obj)
        {
            if (!(obj is CurrencyToCurrencyKey))
                return false;

            return Equals((CurrencyToCurrencyKey)obj);
        }

        public override int GetHashCode()
        {
            return hash;
        }

        public bool Equals(CurrencyToCurrencyKey other)
        {
            return this.from == other.from && this.to == other.to;
        }
    }

    struct SymbolToCurrencyKey : IEquatable<SymbolToCurrencyKey>
    {
        private readonly string smb;
        private readonly string to;
        private readonly int hash;

        public SymbolToCurrencyKey(string smb, string to)
        {
            this.smb = smb;
            this.to = to;

            unchecked
            {
                hash = (int)2166136261;
                hash = hash * 16777619 ^ smb.GetHashCode();
                hash = hash * 16777619 ^ to.GetHashCode();
            }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is SymbolToCurrencyKey))
                return false;

            return Equals((SymbolToCurrencyKey)obj);
        }

        public override int GetHashCode()
        {
            return hash;
        }

        public bool Equals(SymbolToCurrencyKey other)
        {
            return this.smb == other.smb && this.to == other.to;
        }
    }
}
