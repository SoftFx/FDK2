using System;

namespace TickTrader.FDK.Common.Time
{
    public interface ITimePeriod
    {
        bool StartsAtIncluded { get; }
        bool EndsAtIncluded { get; }
        DateTime StartsAt { get; }
        DateTime EndsAt { get; }
        bool Contains(DateTime time);
    }
}