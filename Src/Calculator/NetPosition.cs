using System;
using System.Runtime.Serialization;

namespace TickTrader.FDK.Calculator
{
    [Serializable]
    [DataContract]
    public class NetPosition : IExtensibleDataObject
    {
        public const string IdLogFormat = "#{0}";

        public NetPosition()
        {
        }

        public NetPosition(NetPosition origin)
        {
            Id = origin.Id;
            Symbol = origin.Symbol;
            SymbolAlias = origin.SymbolAlias;
            Side = origin.Side;
            Amount = origin.Amount;
            AveragePrice = origin.AveragePrice;
            Swap = origin.Swap;
            Commission = origin.Commission;
            AccountId = origin.AccountId;
            Modified = origin.Modified;
            Version = origin.Version;
            ClientApp = origin.ClientApp;
        }

        [DataMember]
        public long Id { get; set; }

        [DataMember]
        public string Symbol { get; set; }

        [DataMember]
        public OrderSides Side { get; set; }

        [DataMember]
        public decimal Amount { get; set; }

        [DataMember]
        public decimal AveragePrice { get; set; }

        [DataMember]
        public decimal Swap { get; set; }

        [DataMember]
        public decimal Commission { get; set; }

        [DataMember]
        public long AccountId { get; set; }

        [DataMember]
        public int Version { get; set; }

        [DataMember]
        public string SymbolAlias { get; set; }

        public string SymbolAliasOrName => SymbolAlias ?? Symbol;

        [DataMember]
        public DateTime? Modified { get; set; }

        [DataMember]
        public string ClientApp { get; set; }

        public NetPosition Clone()
        {
            return new NetPosition(this);
        }

        public override string ToString()
        {
            var builder = new BriefToStringBuilder();
            builder.Append("Id", Id, IdLogFormat);
            builder.Append("AccountLogin", AccountId);
            builder.Append("Symbol", Symbol);
            builder.Append("SymbolAlias", SymbolAlias);
            builder.Append("Side", Side);
            builder.Append("Amount", Amount);
            builder.Append("AveragePrice", AveragePrice);
            builder.Append("Swap", Swap);
            builder.Append("Commission", Commission);
            builder.AppendNotNull("Modified", Modified);
            builder.AppendNotNull("Version", Version);
            builder.AppendNotNull("ClientApp", ClientApp);
            return builder.GetResult();
        }

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
