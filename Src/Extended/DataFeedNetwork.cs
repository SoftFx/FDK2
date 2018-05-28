namespace TickTrader.FDK.Extended
{
    using System;
    using Common;

    /// <summary>
    /// The class contains information about network usage by client connection.
    /// </summary>
    public class DataTradeNetwork
    {
        public DataTradeNetwork(DataTrade dataTrade)
        {
            dataTrade_ = dataTrade;
        }

        /// <summary>
        /// Returns network activity of last session. Can not be null.
        /// </summary>
        public NetworkActivity GetLastSessionActivity()
        {
            NetworkActivity orderEntryNetworkActivity = dataTrade_.orderEntryClient_.NetworkActivity;
            NetworkActivity tradeCaptureNetworkActivity = dataTrade_.tradeCaptureClient_.NetworkActivity;

            return new NetworkActivity
            (
                orderEntryNetworkActivity.DataBytesSent + tradeCaptureNetworkActivity.DataBytesSent, 
                orderEntryNetworkActivity.DataBytesReceived + tradeCaptureNetworkActivity.DataBytesReceived
            );
        }

        DataTrade dataTrade_;
    }
}
