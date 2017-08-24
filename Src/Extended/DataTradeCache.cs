namespace TickTrader.FDK.Extended
{
    using System;
    using System.Collections.Generic;
    using Common;

    /// <summary>
    /// 
    /// </summary>
    public class DataTradeCache
    {
        internal DataTradeCache(DataTrade dataTrade)
        {
            dataTrade_ = dataTrade;
            mutex_ = new object();
//          tradeServerInfo_ = null;
//          sessionInfo_ = null;
//          accountInfo_ = null;
//          tradeRecords_ = null;
//          positions_ = null;
        }

        #region Properties

        /// <summary>
        /// Returns cache of session information.
        /// </summary>
        public TradeServerInfo TradeServerInfo
        {
            get
            {
                lock (mutex_)
                {
                    if (tradeServerInfo_ != null)
                        return tradeServerInfo_;                       
                }

                return dataTrade_.server_.GetTradeServerInfo();
            }
        }

        /// <summary>
        /// Returns cache of session information.
        /// </summary>
        public SessionInfo SessionInfo
        {
            get
            {
                lock (mutex_)
                {
                    if (sessionInfo_ != null)
                        return sessionInfo_;
                }

                return dataTrade_.server_.GetSessionInfo();
            }
        }

        /// <summary>
        /// Gets account information.
        /// </summary>
        public AccountInfo AccountInfo
        {
            get
            {
                lock (mutex_)
                {
                    if (accountInfo_ != null)
                        return accountInfo_;
                }

                return dataTrade_.server_.GetAccountInfo();
            }
        }
        
        /// <summary>
        /// Gets trade records.
        /// </summary>
        public TradeRecord[] TradeRecords
        {
            get
            {
                lock (mutex_)
                {
                    if (tradeRecords_ != null)
                    {
                        TradeRecord[] tradeRecords = new TradeRecord[tradeRecords_.Count];

                        int index2 = 0;
                        foreach (KeyValuePair<string, TradeRecord> item in tradeRecords_)
                            tradeRecords[index2 ++] = item.Value;

                        return tradeRecords;
                    }
                }

                return dataTrade_.server_.GetTradeRecords();
            }
        }

        /// <summary>
        /// Gets postions; available for Net account only.
        /// </summary>
        public Position[] Positions
        {
            get
            {
                lock (mutex_)
                {
                    if (positions_ != null)
                    {
                        Position[] positions = new Position[positions_.Count];

                        int index = 0;

                        foreach (KeyValuePair<string, Position> item in positions_)
                            positions[index ++] = item.Value;

                        return positions;
                    }
                }

                return dataTrade_.server_.GetPositions();
            }
        }

        #endregion

        DataTrade dataTrade_;

        internal object mutex_;
        internal TradeServerInfo tradeServerInfo_;
        internal SessionInfo sessionInfo_;
        internal AccountInfo accountInfo_;
        internal Dictionary<string, TradeRecord> tradeRecords_;
        internal Dictionary<string, Position> positions_;
    }
}
