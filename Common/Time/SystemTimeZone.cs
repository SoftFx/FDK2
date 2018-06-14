using System;
using System.Collections.Generic;
using System.Linq;

namespace TickTrader.Common.Time
{
    internal sealed class SystemTimeZone : ITimeZone<TimeZoneInfo>
    {
        private TimeZoneInfo _timeZone;
        private int _offset;
        private string _timeZoneId;

        public int Offset
        {
            get { return _offset; }
            set { _offset = value; }
        }

        public string TimeZoneId
        {
            get { return _timeZoneId; }
            set
            {
                _timeZoneId = value;
                _timeZone = TimeZoneInfo.FindSystemTimeZoneById(_timeZoneId);
            }
        }

        public TimeZoneInfo Info => _timeZone;

        public SystemTimeZone(string timeZoneId, int offset)
        {
            _timeZoneId = timeZoneId;
            _offset = offset;
            _timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }

        public SystemTimeZone(TimeZoneInfo timeZone, int offset)
        {
            _timeZone = timeZone;
            _timeZoneId = timeZone.Id;
            _offset = offset;
        }

        public DateTime ConvertToUtc(DateTime tzDateTime)
        {
            DateTime dateTime = DateTime.SpecifyKind(tzDateTime, DateTimeKind.Unspecified).AddHours(-_offset);
            return TimeZoneInfo.ConvertTimeToUtc(dateTime, _timeZone);
        }

        public DateTime ConvertFromUtc(DateTime utcDateTime)
        {
            DateTime tzDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, _timeZone);
            return tzDateTime.AddHours(_offset);
        }

        public TimeSpan GetUtcOffset()
        {
            DateTime tzNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZone);
            return _timeZone.GetUtcOffset(tzNow).Add(TimeSpan.FromHours(_offset));
        }
    }
}
