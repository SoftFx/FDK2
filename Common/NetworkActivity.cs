namespace TickTrader.FDK.Common
{
    /// <summary>
    /// The class contains statistics of a client connection.
    /// </summary>
    public class NetworkActivity
    {
        public NetworkActivity(long dataBytesSent, long dataBytesReceived)
        {
            this.DataBytesSent = dataBytesSent;
            this.DataBytesReceived = dataBytesReceived;
        }

        /// <summary>
        /// Returns number of bytes, which have been sent;
        /// this value represents quantity of logical data.
        /// </summary>
        public long DataBytesSent { get; private set; }

        /// <summary>
        /// Returns number of bytes, which have been received;
        /// this value represents quantity of logical data.
        /// </summary>
        public long DataBytesReceived { get; private set; }
    }
}
