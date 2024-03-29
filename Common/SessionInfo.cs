﻿namespace TickTrader.FDK.Common
{
    using System;

    /// <summary>
    /// Represents information about feed/trade session.
    /// </summary>
    public class SessionInfo
    {
        public SessionInfo()
        {
        }

        /// <summary>
        /// Unique identifiedr for the feed/trade session; can not be null.
        /// </summary>
        public string TradingSessionId { get; set; }

        /// <summary>
        /// Gets state of the session. Possible values: open, closed.
        /// </summary>
        public SessionStatus Status { get; set; }

        /// <summary>
        /// Gets server time zone.
        /// </summary>
        public int ServerTimeZoneOffset { get; set; }

        /// <summary>
        /// Get platform name.
        /// </summary>
        public string PlatformName { get; set; }

        /// <summary>
        /// Get platform company name.
        /// </summary>
        public string PlatformCompany { get; set; }

        /// <summary>
        /// Returns true, if the current session is "Open".
        /// </summary>
        public bool IsOpened
        {
            get
            {
                return this.Status == SessionStatus.Open;
            }
        }

        /// <summary>
        /// Returns true, if the current session is "Closed".
        /// </summary>
        public bool IsClosed
        {
            get
            {
                return this.Status == SessionStatus.Closed;
            }
        }

        /// <summary>
        /// Gets start time of the current feed/trade session.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets the open time of the current feed/trade session in case of current session is opened.
        /// Gets the open time of the next open feed/trade session in case of current session is closed.
        /// </summary>
        public DateTime OpenTime { get; set; }

        /// <summary>
        /// Gets the close time of the current feed/trade session in case of current session is opened.
        /// Gets the close time of the next open feed/trade session in case of current session is closed.
        /// </summary>
        public DateTime CloseTime { get; set; }

        /// <summary>
        /// Gets the end time of the current feed/trade session.
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Status groups.
        /// </summary>
        public StatusGroupInfo[] StatusGroups { get; set; }

        /// <summary>
        /// Disabled features.
        /// </summary>
        public OffTimeDisabledFeatures DisabledFeatures { get; set; }



        /// <summary>
        /// Returns formatted string for the class instance.
        /// </summary>
        /// <returns>Can not be null.</returns>
        public override string ToString()
        {
            string s = string.Format("Status={0}; Start={1}; End={2}; Open={3}; Close={4};", this.Status, this.StartTime, this.EndTime, this.OpenTime, this.CloseTime);
            foreach (StatusGroupInfo info in StatusGroups)
                s += " " + info.ToString();
            return s;
        }
    }
}
