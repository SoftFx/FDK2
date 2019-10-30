using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.Serialization;
using TickTrader.Common.Time;

namespace TickTrader.FDK.Common
{
    [Serializable]
    public struct Periodicity : IEquatable<Periodicity>, IComparable<Periodicity>, ISerializable 
    {
        private static readonly SortedList<TimeInterval, string> intervalAcronims = new SortedList<TimeInterval, string>
            {
                {TimeInterval.Second,   "S"},
                {TimeInterval.Minute,   "M"},
                {TimeInterval.Hour,     "H"},
                {TimeInterval.Day,      "D"},
                {TimeInterval.Week,     "W"},
                {TimeInterval.Month,    "MN"},
                {TimeInterval.Year,     "Y"}
            };

        private const string NoneString = "None";
        private const int CountMaskLength = 5;
        private const uint IntervalMask = uint.MaxValue << CountMaskLength;
        private const uint CountMask = ~IntervalMask;


        private readonly byte bitValue;

        public static readonly Periodicity None = new Periodicity(TimeInterval.None, 0);
        public static readonly DateTime MinTimeLimit = DateTime.MinValue.AddYears(2);
        public static readonly DateTime MaxTimeLimit = DateTime.MaxValue.AddYears(-2);

        public Periodicity(TimeInterval interval, int intervalsCount = 1)
        {
            if ((intervalsCount <= 0 && interval != TimeInterval.None) || (intervalsCount > CountMask))
            {
                throw new ArgumentOutOfRangeException("intervalsCount");
            }

            switch (interval)
            {
                case TimeInterval.None:
                    if (intervalsCount != 0)
                    {
                        throw new ArgumentOutOfRangeException("intervalsCount");
                    }
                    break;
                case TimeInterval.Second:
                    if (intervalsCount > 30 || 60 % intervalsCount != 0)
                    {
                        throw new ArgumentOutOfRangeException("intervalsCount", "Second intervals count should be divisor of 60");
                    }
                    break;
                case TimeInterval.Minute:
                    if (intervalsCount > 30 || 60 % intervalsCount != 0)
                    {
                        throw new ArgumentOutOfRangeException("intervalsCount", "Minute intervals count should be divisor of 60");
                    }
                    break;
                case TimeInterval.Hour:
                    if (intervalsCount > 12 || 24 % intervalsCount != 0)
                    {
                        throw new ArgumentOutOfRangeException("intervalsCount", "Hour intervals count should be divisor of 24");
                    }
                    break;
                case TimeInterval.Day:
                    if (intervalsCount != 1)
                    {
                        throw new ArgumentOutOfRangeException("intervalsCount", "Day intervals count should be 1");
                    }
                    break;
                case TimeInterval.Week:
                    if (intervalsCount != 1)
                    {
                        throw new ArgumentOutOfRangeException("intervalsCount", "Week intervals count should be 1");
                    }
                    break;
                case TimeInterval.Month:
                    if (intervalsCount > 6 || 12 % intervalsCount != 0)
                    {
                        throw new ArgumentOutOfRangeException("intervalsCount", "Month intervals count should be divisor of 12");
                    }
                    break;
                case TimeInterval.Year:
                    if (intervalsCount != 1)
                    {
                        throw new ArgumentOutOfRangeException("intervalsCount", "Year intervals count should be 1");
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException("interval");
            }

            this.bitValue = (byte) (((int)interval << CountMaskLength) | intervalsCount);
        }

        public Periodicity(SerializationInfo info, StreamingContext context)
        {
            var period = Parse(info.GetString("Period"));
            bitValue = (byte)(((int)period.Interval << CountMaskLength) | period.IntervalsCount);
        }

        public static bool TryParse(string source, out Periodicity periodicity)
        {
            foreach(var acronim in intervalAcronims)
            {
                if (source.StartsWith(acronim.Value, StringComparison.InvariantCultureIgnoreCase))
                {
                    var interval = acronim.Key;
                    var acronimLength = acronim.Value.Length;
                    byte intervalsCount;
                    if (acronimLength < source.Length && byte.TryParse(source.Substring(acronimLength), out intervalsCount))
                    {
                        try
                        {
                            periodicity = new Periodicity(interval, intervalsCount);
                            return true;
                        }
                        catch(ArgumentOutOfRangeException)
                        {
                        }
                    }
                }
            }

            periodicity = None;
            return string.Compare(source, NoneString, StringComparison.InvariantCultureIgnoreCase) == 0;
        }

        public static Periodicity Parse(string source)
        {
            Periodicity result;
            if (TryParse(source, out result))
            {
                return result;
            }

            throw new FormatException("Can't parse periodicity: " + source + ".");
        }

        public static DateTime operator +(DateTime barTime, Periodicity periodicity)
        {
            return barTime.Add(periodicity.Interval, periodicity.IntervalsCount);
        }

        public static DateTime operator -(DateTime barTime, Periodicity periodicity)
        {
            return barTime.Add(periodicity.Interval, -periodicity.IntervalsCount);
        }

        public static implicit operator Periodicity(TimeInterval timeInterval)
        {
            return new Periodicity(timeInterval, 1);
        }

        public TimeInterval Interval
        {
            get { return (TimeInterval)(this.bitValue >> CountMaskLength); }
        }

        public int IntervalsCount
        {
            get { return (int) (this.bitValue & CountMask); }
        }

        [Pure]
        public DateTime GetPeriodStartTime(DateTime anyTimeFromPeriod)
        {
            var intervalsCount = this.IntervalsCount;
            switch (this.Interval)
            {
                case TimeInterval.None:
                    return anyTimeFromPeriod;
                case TimeInterval.Second:
                    return anyTimeFromPeriod.AddTicks(-(anyTimeFromPeriod.Ticks % (TimeSpan.TicksPerSecond * intervalsCount)));
                case TimeInterval.Minute:
                    return anyTimeFromPeriod.AddTicks(-(anyTimeFromPeriod.Ticks % (TimeSpan.TicksPerMinute * intervalsCount)));
                case TimeInterval.Hour:
                    return anyTimeFromPeriod.AddTicks(-(anyTimeFromPeriod.Ticks % (TimeSpan.TicksPerHour * intervalsCount)));
                case TimeInterval.Day:
                    return anyTimeFromPeriod.Date;
                case TimeInterval.Week:
                    return anyTimeFromPeriod.Date.AddDays(DayOfWeek.Sunday - anyTimeFromPeriod.DayOfWeek);
                case TimeInterval.Month:
                    var month = anyTimeFromPeriod.Month;
                    return new DateTime(anyTimeFromPeriod.Year, month - (month - 1) % intervalsCount, 1);
                case TimeInterval.Year:
                    return new DateTime(anyTimeFromPeriod.Year, 1, 1);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public DateTime GetPeriodEndTime(DateTime anyTimeFromPeriod)
        {
            return GetPeriodStartTime(anyTimeFromPeriod).Add(Interval, IntervalsCount);
        }

        public DateTime Shift(DateTime timestamp, int count)
        {
            return timestamp.Add(Interval, IntervalsCount * count);
        }

        public bool ValidatePeriodStartTime(DateTime time)
        {
            return time == this.GetPeriodStartTime(time);
        }

        public bool CanBeDerivedFrom(Periodicity source)
        {
            if (source == None)
            {
                return true;
            }

            var sourceInterval = source.Interval;
            var thisInterval = this.Interval;

            if (sourceInterval != TimeInterval.Week && sourceInterval < thisInterval)
            {
                return true;
            }

            return (thisInterval == sourceInterval) && (this.IntervalsCount % source.IntervalsCount == 0);
        }

        public bool IsDivisorOf(Periodicity dividend)
        {
            if (dividend == None || this == None)
            {
                return false;
            }

            var thisInterval = this.Interval;
            var dividendInterval = dividend.Interval;

            if (thisInterval != TimeInterval.Week && thisInterval < dividendInterval)
            {
                return true;
            }

            return dividendInterval == thisInterval && dividend.IntervalsCount % this.IntervalsCount == 0;
        }
        
        public int CompareTo(Periodicity other)
        {
            return this.bitValue.CompareTo(other.bitValue);
        }

        public override string ToString()
        {
            if (this == None)
            {
                return NoneString;
            }

            return intervalAcronims[this.Interval] + this.IntervalsCount;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (obj.GetType() != typeof (Periodicity)) return false;
            return Equals((Periodicity) obj);
        }

        public bool Equals(Periodicity other)
        {
            return other.bitValue == this.bitValue;
        }

        public override int GetHashCode()
        {
            return this.bitValue.GetHashCode();
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Period", ToString());
        }

        public static bool operator ==(Periodicity left, Periodicity right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Periodicity left, Periodicity right)
        {
            return !left.Equals(right);
        }
        public static bool operator <(Periodicity left, Periodicity right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator >(Periodicity left, Periodicity right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator <=(Periodicity left, Periodicity right)
        {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >=(Periodicity left, Periodicity right)
        {
            return left.CompareTo(right) >= 0;
        }
    }
}