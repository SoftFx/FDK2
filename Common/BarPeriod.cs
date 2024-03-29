﻿namespace TickTrader.FDK.Common
{
    using System;
    using System.Text.RegularExpressions;

    public enum BarPeriodPrefix
    {
        S, M, H, D, W, MN
    }

    /// <summary>
    /// Contains different bar period.
    /// </summary>
    public class BarPeriod
    {
        #region Global Constants

        /// <summary>
        /// Bar period is 1 second.
        /// </summary>
        public static readonly BarPeriod S1 = new BarPeriod("S", 1);

        /// <summary>
        /// Bar period is 10 seconds.
        /// </summary>
        public static readonly BarPeriod S10 = new BarPeriod("S", 10);

        /// <summary>
        /// Bar period is 1 minute.
        /// </summary>
        public static readonly BarPeriod M1 = new BarPeriod("M", 1);

        /// <summary>
        /// Bar period is 5 minutes.
        /// </summary>
        public static readonly BarPeriod M5 = new BarPeriod("M", 5);

        /// <summary>
        /// Bar period is 15 minutes.
        /// </summary>
        public static readonly BarPeriod M15 = new BarPeriod("M", 15);

        /// <summary>
        /// Bar period is 30 minutes.
        /// </summary>
        public static readonly BarPeriod M30 = new BarPeriod("M", 30);

        /// <summary>
        /// Bar period is 1 hour.
        /// </summary>
        public static readonly BarPeriod H1 = new BarPeriod("H", 1);

        /// <summary>
        /// Bar period is 4 hours.
        /// </summary>
        public static readonly BarPeriod H4 = new BarPeriod("H", 4);

        /// <summary>
        /// Bar period is 1 day.
        /// </summary>
        public static readonly BarPeriod D1 = new BarPeriod("D", 1);

        /// <summary>
        /// Bar period is 1 week.
        /// </summary>
        public static readonly BarPeriod W1 = new BarPeriod("W", 1);

        /// <summary>
        /// Bar period is 1 month.
        /// </summary>
        public static readonly BarPeriod MN1 = new BarPeriod("MN", 1);

        #endregion

        #region Constants

        const int DaysOfWeek = 7;

        #endregion

        internal BarPeriod(string prefix, int interval)
        {
            this.prefix = ParsePrefix(prefix);
            this.factor = interval;
        }

        static BarPeriodPrefix ParsePrefix(string prefix)
        {
            return (BarPeriodPrefix)Enum.Parse(typeof(BarPeriodPrefix), prefix);
        }

        /// <summary>
        /// Creates a new instance of BarPeriod class from string.
        /// </summary>
        /// <param name="text">string representation of bar period</param>
        public BarPeriod(string text)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            var match = Regex.Match(text, @"([a-zA-Z]+)(\d+)");
            if (!match.Success)
            {
                var message = string.Format("Incorrect bar periodicity={0}", text);
                throw new ArgumentException(message, nameof(text));
            }

            this.prefix = ParsePrefix(match.Groups[1].Value);
            this.factor = Convert.ToInt32(match.Groups[2].Value);
        }

        public BarPeriodPrefix Prefix
        {
            get { return prefix; }
        }

        public int Factor
        {
            get { return factor; }
        }

        /// <summary>
        /// Calculates a next date time.
        /// </summary>
        /// <param name="period">A valid bar period.</param>
        /// <param name="time">A valid date time.</param>
        /// <returns>A next date time.</returns>
        public static DateTime operator +(BarPeriod period, DateTime time)
        {
            return time + period;
        }

        /// <summary>
        /// Calculates a next date time.
        /// </summary>
        /// <param name="time">A valid date time.</param>
        /// <param name="period">A valid bar period.</param>
        /// <returns>A next date time.</returns>
        public static DateTime operator +(DateTime time, BarPeriod period)
        {
            switch (period.prefix)
            {
                case BarPeriodPrefix.S:
                    return time.AddSeconds(period.factor);
                case BarPeriodPrefix.M:
                    return time.AddMinutes(period.factor);
                case BarPeriodPrefix.H:
                    return time.AddHours(period.factor);
                case BarPeriodPrefix.D:
                    return time.AddDays(period.factor);
                case BarPeriodPrefix.W:
                    return time.AddDays(DaysOfWeek * period.factor);
            }
            return time.AddMonths(period.factor);
        }

        /// <summary>
        /// Calculates a previous date time.
        /// </summary>
        /// <param name="time">A valid date time.</param>
        /// <param name="period">A valid bar period.</param>
        /// <returns>A previous date time.</returns>
        public static DateTime operator -(DateTime time, BarPeriod period)
        {
            switch (period.prefix)
            {
                case BarPeriodPrefix.S:
                    return time.AddSeconds(-period.factor);
                case BarPeriodPrefix.M:
                    return time.AddMinutes(-period.factor);
                case BarPeriodPrefix.H:
                    return time.AddHours(-period.factor);
                case BarPeriodPrefix.D:
                    return time.AddDays(-period.factor);
                case BarPeriodPrefix.W:
                    return time.AddDays(-DaysOfWeek * period.factor);
            }

            return time.AddMonths(-1 * period.factor);
        }

        public long ToMilliseconds()
        {
            switch (prefix)
            {
                case BarPeriodPrefix.S:
                    return factor * 1000;
                case BarPeriodPrefix.M:
                    return factor * 60000;
                case BarPeriodPrefix.H:
                    return factor * 3600000;
                case BarPeriodPrefix.D:
                    return factor * 86400000;
                case BarPeriodPrefix.W:
                    return factor * 604800000;
                case BarPeriodPrefix.MN:
                    return factor * 2592000000;
                default:
                    throw new Exception("Invalid bar period pefix");
            }
        }

        /// <summary>
        /// Converts the value of the current bar period object to its equivalent string representation.
        /// </summary>
        /// <returns>Can not be null.</returns>
        public override string ToString()
        {
            return string.Format("{0}{1}", prefix, factor);
        }

        #region Members

        readonly BarPeriodPrefix prefix;
        readonly int factor;

        #endregion
    }
}
