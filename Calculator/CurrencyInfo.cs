using System;
using System.Runtime.Serialization;

namespace TickTrader.FDK.Calculator
{
    [DataContract]
    public class CurrencyInfo : ICurrencyInfo, IExtensibleDataObject
    {
        [DataMember]
        public short Id { get; set; }

        [DataMember]
        public String Name { get; set; }

        [DataMember]
        public int Precision { get; set; }

        [DataMember]
        public String Description { get; set; }

        [DataMember]
        public int SortOrder { get; set; }

        public CurrencyInfo Clone()
        {
            var result = new CurrencyInfo();
            result.Id = Id;
            result.Name = Name;
            result.Precision = Precision;
            result.Description = Description;
            result.SortOrder = SortOrder;
            return result;
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

        public override string ToString()
        {
            BriefToStringBuilder builder = new BriefToStringBuilder();
            builder.Append("Id", Id);
            builder.Append("Name", Name);
            builder.Append("Precision", Precision);
            builder.Append("Description", Description);
            builder.Append("SortOrder", SortOrder);
            return builder.ToString();
        }

    };
}
