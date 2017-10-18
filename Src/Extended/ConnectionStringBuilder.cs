namespace TickTrader.FDK.Extended
{
    using System;
    using System.Text;
    using System.Collections.Generic;
    using System.Diagnostics;

    public class ConnectionStringBuilder
    {
        /// <summary>
        /// Sets all string properties to empty value.
        /// </summary>
        public ConnectionStringBuilder()
        {
            DeviceId = "";
            AppSessionId = "";
            AppId = "";
        }

        #region Properties

        /// <summary>
        /// <summary>
        /// Gets or sets trading platform address of the data feed/trade instance. Can be IP address or host name.
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Gets or sets trading platform port of the quote feed interface.
        /// </summary>
        public int? QuoteFeedPort { get; set; }

        /// <summary>
        /// Gets or sets trading platform port of the order entry interface.
        /// </summary>
        public int? OrderEntryPort { get; set; }

        /// <summary>
        /// Gets or sets trading platform port of the quote store interface.
        /// </summary>
        public int? QuoteStorePort { get; set; }

        /// <summary>
        /// Gets or sets trading platform port of the trade capture interface.
        /// </summary>
        public int? TradeCapturePort { get; set; }

        /// <summary>
        /// Gets or sets the username of the data feed instance.
        /// Can not be modified, when the data feed is running.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the password of the data feed instance.
        /// Can not be modified, when the data feed is running.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the device ID of the data feed instance.
        /// Can not be modified, when the data feed is running.
        /// </summary>
        public string DeviceId { get; set; }

        /// <summary>
        /// Gets or sets the application ID of the data feed instance.
        /// Can not be modified, when the data feed is running.
        /// </summary>
        public string AppId { get; set; }

        /// <summary>
        /// Gets or sets the application session ID of the data feed instance.
        /// Can not be modified, when the data feed is running.
        /// </summary>
        public string AppSessionId { get; set; }

        /// <summary>
        /// Gets or sets the vent queue maximal size.
        /// Can not be modified, when the data feed is running.
        /// </summary>
        public int? EventQueueSize { get; set; }

        /// <summary>
        /// Gets or sets the vent queue maximal size.
        /// Can not be modified, when the data feed is running.
        /// </summary>
        public int? OperationTimeout { get; set; }

        /// <summary>
        /// Gets or sets log dictionary for messages.
        /// </summary>
        public string LogDirectory { get; set; }

        /// <summary>
        /// If true, the FDK converts messages to good readable format.
        /// </summary>
        public bool? DecodeLogMessages { get; set; }

        /// <summary>
        /// Makes and returns connection string.
        /// </summary>
        /// <returns>Can not be null.</returns>
        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            
            if (Address != null)
                stringBuilder.AppendFormat("[String]Address={0}", Address);

            if (QuoteFeedPort != null)
            {
                if (stringBuilder.Length != 0)
                    stringBuilder.Append(";");

                stringBuilder.AppendFormat("[Int32]QuoteFeedPort={0}", QuoteFeedPort);
            }

            if (OrderEntryPort != null)
            {
                if (stringBuilder.Length != 0)
                    stringBuilder.Append(";");

                stringBuilder.AppendFormat("[Int32]OrderEntryPort={0}", OrderEntryPort);
            }

            if (QuoteStorePort != null)
            {
                if (stringBuilder.Length != 0)
                    stringBuilder.Append(";");

                stringBuilder.AppendFormat("[Int32]QuoteStorePort={0}", QuoteStorePort);
            }

            if (TradeCapturePort != null)
            {
                if (stringBuilder.Length != 0)
                    stringBuilder.Append(";");

                stringBuilder.AppendFormat("[Int32]TradeCapturePort={0}", TradeCapturePort);
            }

            if (Username != null)
            {
                if (stringBuilder.Length != 0)
                    stringBuilder.Append(";");

                stringBuilder.AppendFormat("[String]Username={0}", Username);
            }

            if (Password != null)
            {
                if (stringBuilder.Length != 0)
                    stringBuilder.Append(";");

                stringBuilder.AppendFormat("[String]Password={0}", Password);
            }

            if (DeviceId != null)
            {
                if (stringBuilder.Length != 0)
                    stringBuilder.Append(";");

                stringBuilder.AppendFormat("[String]DeviceId={0}", DeviceId);
            }

            if (AppId != null)
            {
                if (stringBuilder.Length != 0)
                    stringBuilder.Append(";");

                stringBuilder.AppendFormat("[String]AppId={0}", AppId);
            }

            if (AppSessionId != null)
            {
                if (stringBuilder.Length != 0)
                    stringBuilder.Append(";");

                stringBuilder.AppendFormat("[String]AppSessionId={0}", AppSessionId);
            }

            if (EventQueueSize != null)
            {
                if (stringBuilder.Length != 0)
                    stringBuilder.Append(";");

                stringBuilder.AppendFormat("[Int32]EventQueueSize={0}", EventQueueSize);
            }

            if (OperationTimeout != null)
            {
                if (stringBuilder.Length != 0)
                    stringBuilder.Append(";");

                stringBuilder.AppendFormat("[Int32]OperationTimeout={0}", OperationTimeout);
            }

            if (LogDirectory != null)
            {
                if (stringBuilder.Length != 0)
                    stringBuilder.Append(";");

                stringBuilder.AppendFormat("[String]LogDirectory={0}", LogDirectory);
            }

            if (DecodeLogMessages != null)
            {
                if (stringBuilder.Length != 0)
                    stringBuilder.Append(";");

                stringBuilder.AppendFormat("[Boolean]DecodeLogMessages={0}", DecodeLogMessages);
            }

            return stringBuilder.ToString();
        }

        #endregion
    }
}
