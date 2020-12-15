using System;
using System.Collections.Generic;
using System.Globalization;

namespace TickTrader.FDK.Common.Time
{
    public static class TimeIntervalExtensions
    {
        private static readonly Dictionary<TimeInterval, string> dateTimeFormats = new Dictionary<TimeInterval, string>
            {
                {TimeInterval.Year,     "yyyy"},
                {TimeInterval.Month,    "yyyy':'MM"},
                {TimeInterval.Week,     "yyyy':'MM':'dd"},
                {TimeInterval.Day,      "yyyy':'MM':'dd"},
                {TimeInterval.Hour,     "yyyy':'MM':'dd' 'HH'h'"},
                {TimeInterval.Minute,   "yyyy':'MM':'dd' 'HH':'mm"},
                {TimeInterval.Second,   "yyyy':'MM':'dd' 'HH':'mm':'ss"}
            };

        private static readonly CultureInfo invariantCulture = CultureInfo.InvariantCulture;

        public static DateTime Add(this DateTime source, TimeInterval interval, int count = 1)
        {
            switch (interval)
            {
                case TimeInterval.None:
                    return source;
                case TimeInterval.Second:
                    return source.AddSeconds(count);
                case TimeInterval.Minute:
                    return source.AddMinutes(count);
                case TimeInterval.Hour:
                    return source.AddHours(count);
                case TimeInterval.Day:
                    return source.AddDays(count);
                case TimeInterval.Week:
                    return source.AddDays(7 * count);
                case TimeInterval.Month:
                    return source.AddMonths(count);
                case TimeInterval.Year:
                    return source.AddYears(count);
                default:
                    throw new ArgumentOutOfRangeException("interval");
            }
        }

        public static string ToString(this DateTime source, TimeInterval precision)
        {
            if (dateTimeFormats.ContainsKey(precision))
            {
                return source.ToString(dateTimeFormats[precision], invariantCulture);
            }

            return source.ToString();
        }


        public static DateTime Parse(this string source, TimeInterval precision)
        {
            if (dateTimeFormats.ContainsKey(precision))
            {
                return DateTime.ParseExact(source, dateTimeFormats[precision], invariantCulture);
            }
            return DateTime.Parse(source);
        }
    }
}