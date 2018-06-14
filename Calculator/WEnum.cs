using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace System
{
    [DataContract]
    public struct WEnum<T> : IComparable where T : struct, IComparable
    {
        [DataMember]
        private int interopValue;

        [IgnoreDataMember]
        [XmlIgnore]
        public T Value { get; private set; }

        public override string ToString()
        {
            return Value.ToString();
        }

        [OnSerializing]
        void Box(StreamingContext context)
        {
            interopValue = (int)(object)Value;
        }

        [OnDeserialized]
        void Unbox(StreamingContext context)
        {
            Value = (T)(object)interopValue;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public int CompareTo(object other)
        {
            return Value.CompareTo(((WEnum<T>)other).Value);
        }

        public override bool Equals(object obj)
        {
            if (obj is WEnum<T>)
            {
                WEnum<T> wrp = (WEnum<T>)obj;
                return wrp.Value.Equals(Value);
            }
            else if (obj is T)
            {
                return ((T)obj).Equals(Value);
            }

            return false;
        }

        public static implicit operator T(WEnum<T> value)
        {
            return value.Value;
        }

        public static implicit operator int(WEnum<T> value)
        {
            return (int)(object)value.Value;
        }

        public static implicit operator WEnum<T>(T value)
        {
            return new WEnum<T>() { Value = value };
        }

        public static bool operator ==(WEnum<T> a, T b)
        {
            return a.Value.Equals(b);
        }

        public static bool operator !=(WEnum<T> a, T b)
        {
            return !(a == b);
        }

        public static bool operator ==(T a, WEnum<T> b)
        {
            return b.Value.Equals(a);
        }

        public static bool operator !=(T a, WEnum<T> b)
        {
            return !(a == b);
        }

        public static bool operator ==(WEnum<T> a, WEnum<T> b)
        {
            return a.Value.Equals(b.Value);
        }

        public static bool operator !=(WEnum<T> a, WEnum<T> b)
        {
            return !(a == b);
        }
    }
}
