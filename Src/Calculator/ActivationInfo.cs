using System;
using System.Runtime.Serialization;

namespace TickTrader.FDK.Calculator
{
    public enum ActivationTypes
    {
        None = 0,
        StopLoss,
        TakeProfit,
        Pending,
        Stopout,
        StopLossRollback = -StopLoss,
        TakeProfitRollback = -TakeProfit,
        PendingRollback = -Pending,
        StopoutRollback = -Stopout
    };

    [DataContract]
    public class ActivationInfo : IExtensibleDataObject
    {
        [DataMember]
        public long AccountId { get; set; }

        [DataMember]
        public long OrderID { get; set; }

        [DataMember]
        public WEnum<ActivationTypes> Type { get; set; }

        [DataMember]
        public decimal ActivationPrice { get; set; }

        [DataMember]
        public decimal OrderAmount { get; set; }

        [DataMember]
        public decimal RemainingAmount { get; set; }

        [DataMember]
        public decimal HiddenAmount { get; set; }

        [DataMember]
        public decimal AmtStep { get; set; }

        [DataMember]
        public WEnum<OrderTypes> OrderType { get; set; }

        #region IExtensibleDataObject

        [NonSerialized]
        private ExtensionDataObject theData;

        public virtual ExtensionDataObject ExtensionData
        {
            get { return theData; }
            set { theData = value; }
        }

        #endregion
    }
}
