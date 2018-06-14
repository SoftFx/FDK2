namespace TickTrader.FDK.Extended
{
    using System;
    using Common;

    /// <summary>
    /// The class contains information about network usage by client connection.
    /// </summary>
    public class DataFeedNetwork
    {
        public DataFeedNetwork(DataFeed dataFeed)
        {
            dataFeed_ = dataFeed;
        }

        /// <summary>
        /// Returns network activity of last session. Can not be null.
        /// </summary>
        public NetworkActivity GetLastSessionActivity()
        {
            NetworkActivity quoteFeedNetworkActivity = dataFeed_.quoteFeedClient_.NetworkActivity;
            NetworkActivity quoteStoreNetworkActivity = dataFeed_.quoteStoreClient_.NetworkActivity;

            return new NetworkActivity
            (
                quoteFeedNetworkActivity.DataBytesSent + quoteStoreNetworkActivity.DataBytesSent, 
                quoteFeedNetworkActivity.DataBytesReceived + quoteStoreNetworkActivity.DataBytesReceived
            );
        }

        DataFeed dataFeed_;
    }
}
