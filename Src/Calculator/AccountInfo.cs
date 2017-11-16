using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace TickTrader.FDK.Calculator
{
    /// <summary>
    /// Accounting types
    /// </summary>
    public enum AccountingTypes
    {
        Gross,
        Net,
        Cash
    }

    public enum AccountingSystemTypes
    {
        None,
    }

    public enum SecurityLevels
    {
        High = 0,
        Low = 1,
    }

    [Serializable]
    [DataContract]
    class AccountInfo : IExtensibleDataObject
    {
        [DataMember]
        public long AccountId { get; set; }                          // Trade Platform account ID cache. If -1 we should take it from DB.

        [DataMember]
        public int RangeId { get; set; }                            // Account range.

        [DataMember]
        public long AccountLogin { get; set; }                       // Account login.

        [DataMember]
        public string AccountPassword { get; set; }                 // Account password.

        [DataMember]
        public string AccountInvestorPassword { get; set; }         // Account investor password.

        [DataMember]
        public string Group { get; set; }

        [DataMember]
        public WEnum<AccountingTypes> AccountingType { get; set; }

        [DataMember]
        public string Comment { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Email { get; set; }

        [DataMember]
        public DateTime RegistrationDate { get; set; }

        [DataMember]
        public bool Blocked { get; set; }

        [DataMember]
        public bool Readonly { get; set; }

        [DataMember]
        public int Leverage { get; set; }

        [DataMember]
        public decimal Balance { get; set; }

        [DataMember]
        public string BalanceCurrency { get; set; }

        [DataMember]
        public decimal Equity { get; set; }

        [DataMember]
        public decimal Margin { get; set; }

        [DataMember]
        public decimal MarginLevel { get; set; }

        public decimal MarginFree { get { return Equity - Margin;  } }

        [DataMember]
        public bool IsValid { get; set; }

        [DataMember]
        public string InternalComment { get; set; }

        [DataMember]
        public string AccountTag { get; set; }

        [DataMember]
        public int MarginCallLevel { get; set; }

        [DataMember]
        public int StopOutLevel { get; set; }

        [DataMember]
        public int Version { get; set; }

        [DataMember]
        public List<AssetInfo> Assets { get; set; }

        [DataMember]
        public CustomProperties Properties { get; set; }

        [DataMember]
        public bool IsWebApiEnabled { get; set; }

        [DataMember]
        public List<NetPosition> Positions { get; set; }

        [DataMember]
        public bool IsArchived { get; set; }

        [DataMember]
        public string Domain { get; set; }

        [DataMember]
        public decimal Profit { get; set; }

        [DataMember]
        public decimal Commission { get; set; }

        [DataMember]
        public decimal AgentCommission { get; set; }

        public decimal TotalCommission => Commission + AgentCommission;

        [DataMember]
        public decimal Swap { get; set; }

        [DataMember]
        public bool IsTwoFactorAuthSet { get; set; }

        [DataMember]
        public WEnum<FeedPriority> FeedPriority { get; set; }

        [DataMember]
        public string FirstName { get; set; }

        [DataMember]
        public string LastName { get; set; }

        [DataMember]
        public string Phone { get; set; }

        [DataMember]
        public string Country { get; set; }

        [DataMember]
        public string State { get; set; }

        [DataMember]
        public string City { get; set; }

        [DataMember]
        public string Address { get; set; }

        [DataMember]
        public string ZipCode { get; set; }

        [DataMember]
        public string SocialSecurityNumber { get; set; }

        [DataMember]
        public DateTime LastModifyTime { get; set; }

//        [DataMember]
//        public List<ThrottlingInfo> Throttling { get; set; }

        [DataMember]
        public bool SkipJournal { get; set; }
        [DataMember]
        public bool SkipTradeHistory { get; set; }

        public AccountInfo()
        {
            AccountId = -1;
            Version = 0;
            Properties = new CustomProperties();
            FeedPriority = Calculator.FeedPriority.High;
//            Throttling = new List<ThrottlingInfo>();
        }

        public AccountInfo Clone()
        {
            var clone = new AccountInfo();
            clone.AccountId = AccountId;
            clone.AccountLogin = AccountLogin;
            clone.AccountPassword = AccountPassword;
            clone.AccountInvestorPassword = AccountInvestorPassword;
            clone.Domain = Domain;
            clone.Group = Group;
            clone.AccountingType = AccountingType;
            clone.Comment = Comment;
            clone.Name = Name;
            clone.FirstName = FirstName;
            clone.LastName = LastName;
            clone.Phone = Phone;
            clone.Country = Country;
            clone.State = State;
            clone.City = City;
            clone.Address = Address;
            clone.ZipCode = ZipCode;
            clone.SocialSecurityNumber = SocialSecurityNumber;
            clone.Email = Email;
            clone.RegistrationDate = RegistrationDate;
            clone.LastModifyTime = LastModifyTime;
            clone.Blocked = Blocked;
            clone.Readonly = Readonly;
            clone.Leverage = Leverage;
            clone.Balance = Balance;
            clone.BalanceCurrency = BalanceCurrency;
            clone.Profit = Profit;
            clone.Commission = Commission;
            clone.AgentCommission = AgentCommission;
            clone.Swap = Swap;
            clone.Equity = Equity;
            clone.Margin = Margin;
            clone.MarginLevel = MarginLevel;
            clone.IsValid = IsValid;
            clone.InternalComment = InternalComment;
            clone.AccountTag = AccountTag;
            clone.MarginCallLevel = MarginCallLevel;
            clone.StopOutLevel = StopOutLevel;
            clone.Version = Version;
            if (Properties != null)
                clone.Properties = Properties.Clone();
            if (Assets != null)
                clone.Assets = Assets.Select(a => a.Clone()).ToList();
            if (Positions != null)
                clone.Positions = Positions.Select(p => p.Clone()).ToList();
//            if (Throttling != null)
//                clone.Throttling = Throttling.Select(t => t.Clone()).ToList();
            clone.IsWebApiEnabled = IsWebApiEnabled;
            clone.IsArchived = IsArchived;
            clone.IsTwoFactorAuthSet = IsTwoFactorAuthSet;
            clone.FeedPriority = FeedPriority;
            clone.SkipJournal = SkipJournal;
            clone.SkipTradeHistory = SkipTradeHistory;
            return clone;
        }

        public override string ToString()
        {
            var builder = new BriefToStringBuilder();
            builder.Append("Login", AccountLogin);
            builder.AppendNotNull("Name", Name);
            builder.Append("Domain", Domain);
            builder.Append("Group", Group);
            builder.Append("AccountingType", AccountingType);
            builder.Append("Leveage", Leverage);
            builder.Append("Balance", Balance);
            builder.Append("BalanceCurrency", BalanceCurrency);
            builder.Append("Equity", Equity);
            builder.Append("Margin", Margin);
            builder.Append("MarginLevel", MarginLevel);
            builder.Append("Commission", Commission);
            builder.Append("AgentCommission", AgentCommission);
            builder.Append("Swap", Swap);
            builder.Append("Profit", Profit);
            builder.Append("MarginCallLevel", MarginCallLevel);
            builder.Append("StopOutLevel", StopOutLevel);
            builder.Append("Blocked", Blocked);
            builder.Append("Readonly", Readonly);
            builder.Append("IsWebApiEnabled", IsWebApiEnabled);
            builder.Append("IsTwoFactorAuthSet", IsTwoFactorAuthSet);
            builder.Append("IsValid", IsValid);
            builder.Append("IsArchived", IsArchived);
            builder.Append("FeedPriority", FeedPriority);
            builder.Append("SkipJournal", SkipJournal);
            builder.Append("SkipTradeHistory", SkipTradeHistory);
            builder.AppendNotNull("Comment", Comment);
            builder.AppendNotNull("InternalComment", InternalComment);
            builder.Append("Version", Version);
            return builder.GetResult();
        }


        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (Assets == null)
                Assets = new List<AssetInfo>();
            if (Positions == null)
                Positions = new List<NetPosition>();
//            if (Throttling == null)
//                Throttling = new List<ThrottlingInfo>();
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

    [Serializable]
    [DataContract]
    class AssetInfo
    {
        public AssetInfo()
        {
        }

        public AssetInfo(AssetInfo original)
        {
            Currency = original.Currency;
            CurrencyId = original.CurrencyId;
            Amount = original.Amount;
            LockedAmount = original.LockedAmount;
        }

        [DataMember]
        public string Currency { get; set; }

        [DataMember]
        public short CurrencyId { get; set; }

        [DataMember]
        public decimal Amount { get; set; }

        public decimal FreeAmount => Amount - LockedAmount;

        [DataMember]
        public decimal LockedAmount { get; set; }

        public AssetInfo Clone()
        {
            return new AssetInfo(this);
        }
    }
}
