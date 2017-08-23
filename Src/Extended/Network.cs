namespace TickTrader.FDK.Extended
{
    using System;

    /// <summary>
    /// The class contains information about network usage by client connection.
    /// </summary>
    public class Network
    {
        /// <summary>
        /// Returns network activity of last session. Can not be null.
        /// </summary>
        public NetworkActivity GetLastSessionActivity()
        {
            throw new System.Exception("Not impled");
        }
    }
}
