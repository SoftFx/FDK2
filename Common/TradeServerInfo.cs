using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TickTrader.FDK.Common
{
    public class TradeServerInfo
    {
        public TradeServerInfo()
        {
        }

        public string CompanyName { get; set; }
        public string CompanyFullName { get; set; }
        public string CompanyDescription { get; set; }
        public string CompanyAddress { get; set; }
        public string CompanyPhone { get; set; }
        public string CompanyEmail { get; set; }
        public string CompanyWebSite { get; set; }
        public string ServerName { get; set; }
        public string ServerFullName { get; set; }
        public string ServerDescription { get; set; }
        public string ServerAddress { get; set; }
        public int? ServerRestPort { get; set; }
        public int? ServerWebSocketFeedPort { get; set; }
        public int? ServerWebSocketTradePort { get; set; }
        public int? ServerSfxQuoteFeedPort { get; set; }
        public int? ServerSfxQuoteStorePort { get; set; }
        public int? ServerSfxOrderEntryPort { get; set; }
        public int? ServerSfxTradeCapturePort { get; set; }
        public int? ServerFixFeedSslPort { get; set; }
        public int? ServerFixTradeSslPort { get; set; }
        public string WebTerminalAddress { get; set; }
        public string WebCabinetAddress { get; set; }
        public string SupportCrmAddress { get; set; }        

        public override string ToString()
        {
            return String.Format("CompanyName = {0}; ServerName = {1}", CompanyName, ServerName);
        }
    }
}
