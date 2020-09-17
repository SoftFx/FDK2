using TickTrader.FDK.Common;

namespace TickTrader.FDK.Calculator
{
    public class NetPositionUpdate
    {
        /// <summary>
        /// Gets previous position.
        /// </summary>
        public Position PreviousPosition { get; set; }

        /// <summary>
        /// Gets new position report; can not be null.
        /// </summary>
        public Position NewPosition { get; set; }
    }
}
