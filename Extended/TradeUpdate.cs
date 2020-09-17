using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TickTrader.FDK.Common;

namespace TickTrader.FDK.Extended
{
    public enum UpdateActions
    {
        /// <summary>
        /// None
        /// </summary>
        None = 0,

        /// <summary>
        /// Record was added
        /// </summary>
        Added,

        /// <summary>
        /// Record was replaced
        /// </summary>
        Replaced,

        /// <summary>
        /// Record was removed
        /// </summary>
        Removed
    }

    public class TradeUpdate
    {
        /// <summary>
        /// Trade record update action
        /// </summary>
        public UpdateActions TradeRecordUpdateAction { get; set; }

        /// <summary>
        /// Old trade record
        /// </summary>
        public TradeRecord OldRecord { get; set; }

        /// <summary>
        /// New trade record
        /// </summary>
        public TradeRecord NewRecord { get; set; }

        /// <summary>
        /// New balance
        /// </summary>
        public double? NewBalance { get; set; }

        /// <summary>
        /// Updated assets
        /// </summary>
        public AssetInfo[] UpdatedAssets { get; set; }

        /// <summary>
        /// Returns formatted string for the class instance.
        /// </summary>
        /// <returns>can not be null</returns>
        public override string ToString()
        {
            var result = string.Format("TradeRecordUpdateAction = {0}, OldRecord = {1}, NewRecord = {2}, NewBalance = {3}}", TradeRecordUpdateAction, OldRecord, NewRecord, NewBalance);
            return result;
        }
    }
}
