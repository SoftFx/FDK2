namespace TickTrader.FDK.Common
{
    using System;

    /// <summary>
    /// Represents status group information.
    /// </summary>
    public class StatusGroupInfo
    {
        public StatusGroupInfo()
        {
        }

        /// <summary>
        /// Status group id.
        /// </summary>
        public string StatusGroupId { get; set; }

        /// <summary>
        /// Status group state.
        /// </summary>
        public SessionStatus Status { get; set; }

        /// <summary>
        /// Gets start time of the current feed/trade session.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets the end time of the current feed/trade session.
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Gets open time of the current feed/trade session.
        /// </summary>
        public DateTime OpenTime { get; set; }

        /// <summary>
        /// Gets the close time of the current feed/trade session.
        /// </summary>
        public DateTime CloseTime { get; set; }

        /// <summary>
        /// Returns string representation.
        /// </summary>
        public override string ToString()
        {
            return string.Format("StatusGroupId = {0}; Status = {1}; Start = {2}; End = {3}; Open = {4}; Close = {5};", StatusGroupId, Status, StartTime, EndTime, OpenTime, CloseTime);
        }
    }
}