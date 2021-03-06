﻿namespace TickTrader.FDK.Extended
{
    using System;
    using System.Text;
    using System.Collections.Generic;
    using System.Diagnostics;

    public enum ProxyType
    {
        None,
        Socks4,
        Socks5
    }

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
        /// Gets or sets trading platform port of the trade capture interface.
        /// </summary>
        public string ServerCertificateName { get; set; }

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
        /// If true, the FDK logs events.
        /// </summary>
        public bool? LogEvents { get; set; }

        /// <summary>
        /// If true, the FDK logs states.
        /// </summary>
        public bool? LogStates { get; set; }

        /// <summary>
        /// If true, the FDK logs messages.
        /// </summary>
        public bool? LogMessages { get; set; }

        /// <summary>
        /// If true, the FDK logs quote feed messages.
        /// </summary>
        public bool? LogQuoteFeedMessages { get; set; }

        /// <summary>
        /// If true, the FDK logs quote store messages.
        /// </summary>
        public bool? LogQuoteStoreMessages { get; set; }

        /// <summary>
        /// Gets or sets the proxy connection type of the data feed/trade instance. 
        /// </summary>
        public ProxyType ProxyType { get; set; }

        /// <summary>
        /// Gets or sets trading proxy address of the data feed/trade instance. 
        /// </summary>
        public string ProxyAddress { get; set; }

        /// <summary>
        /// Gets or sets proxy port. 
        /// </summary>
        public int? ProxyPort { get; set; }

        /// <summary>
        /// Gets or sets the username of the proxy connection.
        /// </summary>
        public string ProxyUsername { get; set; }

        /// <summary>
        /// Gets or sets the password of the proxy connection.
        /// </summary>
        public string ProxyPassword { get; set; }

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

            if (ServerCertificateName != null)
            {
                if (stringBuilder.Length != 0)
                    stringBuilder.Append(";");

                stringBuilder.AppendFormat("[String]ServerCertificateName={0}", ServerCertificateName);
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

            if (LogEvents != null)
            {
                if (stringBuilder.Length != 0)
                    stringBuilder.Append(";");

                stringBuilder.AppendFormat("[Boolean]LogEvents={0}", LogEvents);
            }

            if (LogStates != null)
            {
                if (stringBuilder.Length != 0)
                    stringBuilder.Append(";");

                stringBuilder.AppendFormat("[Boolean]LogStates={0}", LogStates);
            }

            if (LogMessages != null)
            {
                if (stringBuilder.Length != 0)
                    stringBuilder.Append(";");

                stringBuilder.AppendFormat("[Boolean]LogMessages={0}", LogMessages);
            }

            if (LogQuoteFeedMessages != null)
            {
                if (stringBuilder.Length != 0)
                    stringBuilder.Append(";");

                stringBuilder.AppendFormat("[Boolean]LogQuoteFeedMessages={0}", LogQuoteFeedMessages);
            }

            if (LogQuoteStoreMessages != null)
            {
                if (stringBuilder.Length != 0)
                    stringBuilder.Append(";");

                stringBuilder.AppendFormat("[Boolean]LogQuoteStoreMessages={0}", LogQuoteStoreMessages);
            }

            if (stringBuilder.Length != 0)
                stringBuilder.Append(";");

            stringBuilder.AppendFormat("[Int32]ProxyType={0}", (int)ProxyType);

            if (ProxyAddress != null)
            {
                if (stringBuilder.Length != 0)
                    stringBuilder.Append(";");

                stringBuilder.AppendFormat("[String]ProxyAddress={0}", ProxyAddress);
            }

            if (ProxyPassword != null)
            {
                if (stringBuilder.Length != 0)
                    stringBuilder.Append(";");

                stringBuilder.AppendFormat("[String]ProxyPassword={0}", ProxyPassword);
            }

            if (ProxyUsername != null)
            {
                if (stringBuilder.Length != 0)
                    stringBuilder.Append(";");

                stringBuilder.AppendFormat("[String]ProxyUsername={0}", ProxyUsername);
            }

            if (ProxyPort != null)
            {
                if (stringBuilder.Length != 0)
                    stringBuilder.Append(";");

                stringBuilder.AppendFormat("[Int32]ProxyPort={0}", ProxyPort);
            }

            return stringBuilder.ToString();
        }

        #endregion
    }
}
