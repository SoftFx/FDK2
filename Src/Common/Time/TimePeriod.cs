using System;

namespace TickTrader.Common.Time
{
    public struct TimePeriod : ITimePeriod, IEquatable<TimePeriod>
    {
        private readonly DateTime startsAt;
        private readonly DateTime endsAt;
        private readonly bool startsAtIncluded;
        private readonly bool endsAtIncluded;

        public TimePeriod(TimePeriod period)
        {
            this.startsAt = period.startsAt;
            this.endsAt = period.endsAt;
            this.startsAtIncluded = period.startsAtIncluded;
            this.endsAtIncluded = period.endsAtIncluded;
        }

        public TimePeriod(DateTime startsAt, DateTime endsAt, bool startsAtIncluded = true, bool endsAtIncluded = true)
        {
            if (startsAt > endsAt)
            {
                throw new ArgumentOutOfRangeException("endsAt", "startsAt should be less or equal to endsAt");
            }
            if (startsAt == endsAt && (!startsAtIncluded || !endsAtIncluded))
            {
                throw new ArgumentException("startsAtIncluded and endsAtIncluded should be set to true when startsAt equals endsAt");
            }

            this.startsAt = startsAt;
            this.endsAt = endsAt;
            this.startsAtIncluded = startsAtIncluded;
            this.endsAtIncluded = endsAtIncluded;
        }

        public TimePeriod(DateTime startsAt, TimeSpan span, bool startsAtIncluded = true, bool endsAtIncluded = true) : this(startsAt, startsAt + span, startsAtIncluded, endsAtIncluded)
        {
        }

        public bool StartsAtIncluded
        {
            get { return this.startsAtIncluded; }
        }

        public bool EndsAtIncluded
        {
            get { return this.endsAtIncluded; }
        }

        public DateTime StartsAt
        {
            get { return this.startsAt; }
        }

        public DateTime EndsAt
        {
            get { return this.endsAt; }
        }

        public TimeSpan Span
        {
            get { return this.endsAt - this.startsAt; }
        }
        
        public bool Contains(DateTime time)
        {
            if (this.startsAt < time && time < this.endsAt)
            {
                return true;
            }
            if (this.startsAt == time && this.startsAtIncluded)
            {
                return true;
            }
            if (this.endsAt == time && this.endsAtIncluded)
            {
                return true;
            }
            return false;
        }

        public bool StartsAfter(DateTime time)
        {
            if (this.startsAtIncluded)
                return time < this.startsAt;
            return time <= this.startsAt;
        }

        public bool EndsBefore(DateTime time)
        {
            if (this.endsAtIncluded)
                return time > this.endsAt;
            return time >= this.endsAt;
        }

        public bool IntersectsWith(TimePeriod queryPeriod)
        {
            if (this.startsAt < queryPeriod.endsAt && this.endsAt > queryPeriod.startsAt)
            {
                return true;
            }
            if (this.startsAt == queryPeriod.endsAt)
            {
                return this.startsAtIncluded && queryPeriod.endsAtIncluded;
            }
            if (this.endsAt == queryPeriod.startsAt)
            {
                return this.endsAtIncluded && queryPeriod.startsAtIncluded;
            }
            return false;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (obj.GetType() != typeof (TimePeriod)) return false;
            return Equals((TimePeriod) obj);
        }

        public bool Equals(TimePeriod other)
        {
            return other.startsAt.Equals(this.startsAt) 
                && other.endsAt.Equals(this.endsAt) 
                && other.startsAtIncluded.Equals(this.startsAtIncluded) 
                && other.endsAtIncluded.Equals(this.endsAtIncluded);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = this.startsAt.GetHashCode();
                result = (result*397) ^ this.endsAt.GetHashCode();
                result = (result*397) ^ this.startsAtIncluded.GetHashCode();
                result = (result*397) ^ this.endsAtIncluded.GetHashCode();
                return result;
            }
        }

        public static bool operator ==(TimePeriod left, TimePeriod right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TimePeriod left, TimePeriod right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"[{startsAt.ToString("G")} - {endsAt.ToString("G")}]";
        }

        public static TimePeriod? Join(TimePeriod period1, TimePeriod period2)
        {
            if (!period1.IntersectsWith(period2))
                return null;

            DateTime start = DateTime.MinValue;
            DateTime end = DateTime.MaxValue;
            bool startInc = false;
            bool endIncl = false;

            if (period1.startsAt == period2.startsAt)
            {
                start = period1.StartsAt;
                startInc = period1.StartsAtIncluded || period2.startsAtIncluded;
            }
            else if (period1.startsAt < period2.startsAt)
            {
                start = period1.StartsAt;
                startInc = period1.StartsAtIncluded;
            }
            else
            {
                start = period2.StartsAt;
                startInc = period2.StartsAtIncluded;
            }
            if (period1.EndsAt == period2.EndsAt)
            {
                end = period1.EndsAt;
                endIncl = period1.EndsAtIncluded || period2.EndsAtIncluded;
            }
            else if (period1.EndsAt > period2.EndsAt)
            {
                end = period1.EndsAt;
                endIncl = period1.EndsAtIncluded;
            }
            else
            {
                end = period2.EndsAt;
                endIncl = period2.EndsAtIncluded;
            }

            return new TimePeriod(start, end, startInc, endIncl);
        }
    }
}