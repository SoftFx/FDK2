using System;
using System.Collections.Generic;
using System.Linq;

namespace TickTrader.FDK.Common.Time
{
    public interface ITimeZone
    {
        int Offset { get; set; }
        string TimeZoneId { get; set; }

        DateTime ConvertFromUtc(DateTime utcDateTime);
        DateTime ConvertToUtc(DateTime tzDateTime);
        TimeSpan GetUtcOffset();
    }

    public interface ITimeZone<T> : ITimeZone
    {
        T Info { get; }
    }

    public static class TimeZoneFactory
    {
        public static ITimeZone CreateSystemTimeZone(string timeZoneId, int offset = 0)
        {
            return new SystemTimeZone(timeZoneId, offset);
        }

        public static Dictionary<string, string> GetSystemTimeZoneNames()
        {
            var tzs = TimeZoneInfo.GetSystemTimeZones();
            Dictionary<string, string> result = tzs.ToDictionary(tz => tz.Id, tz => tz.DisplayName);
            return result;
        }

        public static bool ValidateSystemTimeZone(string timeZoneId)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId) != null;
            }
            catch
            {
                // ignored
            }
            return false;
        }
    }
}
