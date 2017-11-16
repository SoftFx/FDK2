using System;
using System.Runtime.Serialization;

namespace TickTrader.FDK.Calculator
{
    /// <summary>
    /// Order types
    /// </summary>
    public enum OrderTypes
    {
        Market      = 0,
        Limit       = 1,
        Stop        = 2,
        Position    = 3,
        StopLimit   = 4
    }

    /// <summary>
    /// Order sides
    /// </summary>
    public enum OrderSides
    {
        Buy     = 0,
        Sell    = 1
    }

    /// <summary>
    /// Order statuses
    /// </summary>
    public enum OrderStatuses
    {
        New                 = 0,
        Calculated          = 1,
        Filled              = 3,
        Canceled            = 4,
        Rejected            = 5,
        Expired             = 6,
        PartiallyFilled     = 7,
        Activated           = 8,
        Invalid             = 99
    }

    /// <summary>
    /// Order execution options
    /// </summary>
    [Flags]
    public enum OrderExecutionOptions
    {
        MarketWithSlippage = 1,
        ImmediateOrCancel = 2,
        HiddenIceberg = 4,
        FillOrKill = 8
    }

    /// <summary>
    /// TODO Order
    /// </summary>
    [Serializable]
    [DataContract]
    public class Order : IOrder, ICloneable, IExtensibleDataObject
    {
        /// <summary>
        /// Order Id format for logging
        /// </summary>
        public const string IdLogFormat = "#{0}";

        /// <summary>
        /// Create new instance of the class
        /// </summary>
        public Order()
        {
            Version = 0;
            Properties = new CustomProperties();
        }

        /// <summary>
        /// Create a copy of existing order
        /// </summary>
        /// <param name="order"></param>
        public Order(IOrder order)
        {
            RangeId = order.RangeId;
            AccountId = order.AccountId;
            OrderId = order.OrderId;
            ParentOrderId = order.ParentOrderId;
            Price = order.Price;
            StopPrice = order.StopPrice;
            Side = order.Side;
            Type = order.Type;
            InitialType = order.InitialType;
            Symbol = order.Symbol;
            SymbolAlias = order.SymbolAlias;
            ClientOrderId = order.ClientOrderId;
            Status = order.Status;
            Created = order.Created;
            RemainingAmount = order.RemainingAmount;
            HiddenAmount = order.HiddenAmount;
            MaxVisibleAmount = order.MaxVisibleAmount;
            AggrFillPrice = order.AggrFillPrice;
            AverageFillPrice = order.AverageFillPrice;
            Amount = order.Amount;
            Modified = order.Modified;
            PositionCreated = order.PositionCreated;
            Created = order.Created;
            StopLoss = order.StopLoss;
            TakeProfit = order.TakeProfit;
            Profit = order.Profit;
            Margin = order.Margin;
            TransferringCoefficient = order.TransferringCoefficient;
            UserComment = order.UserComment;
            ManagerComment = order.ManagerComment;
            UserTag = order.UserTag;
            ManagerTag = order.ManagerTag;
            Magic = order.Magic;
            Commission = order.Commission;
            AgentCommision = order.AgentCommision;
            Swap = order.Swap;
            Filled = order.Filled;
            Expired = order.Expired;
            ClosePrice = order.ClosePrice;
            CurrentPrice = order.CurrentPrice;
            MarginRateInitial = order.MarginRateInitial;
            MarginRateCurrent = order.MarginRateCurrent;
            OpenConversionRate = order.OpenConversionRate;
            CloseConversionRate = order.CloseConversionRate;
            IsReducedOpenCommission = order.IsReducedOpenCommission;
            IsReducedCloseCommission = order.IsReducedCloseCommission;
            ReqOpenPrice = order.ReqOpenPrice;
            ReqOpenAmount = order.ReqOpenAmount;
            Activation = order.Activation;
            Version = order.Version;
            Options = order.Options;
            Properties = (order.Properties != null) ? order.Properties.Clone() : new CustomProperties();
            Taxes = order.Taxes;
            ClientApp = order.ClientApp;
        }

        /// <summary>
        ///
        /// </summary>
        [DataMember]
        public int RangeId { get; set; }

        /// <summary>
        ///
        /// </summary>
        [DataMember]
        public long AccountId { get; set; }

        /// <summary>
        ///
        /// </summary>
        [DataMember]
        public string Symbol { get; set; }

        /// <summary>
        ///
        /// </summary>
        [DataMember]
        public string SymbolAlias { get; set; }

        /// <summary>
        ///
        /// </summary>
        public string SymbolAliasOrName { get { return SymbolAlias ?? Symbol; } }

        /// <summary>
        ///
        /// </summary>
        [DataMember]
        public long OrderId { get; set; }

        /// <summary>
        ///
        /// </summary>
        [DataMember]
        public string ClientOrderId { get; set; }

        /// <summary>
        ///
        /// </summary>
        [DataMember]
        public long? ParentOrderId { get; set; }

        /// <summary>
        ///
        /// </summary>
        [DataMember]
        public decimal? Price { get; set; }

        /// <summary>
        ///
        /// </summary>
        [DataMember]
        public decimal? StopPrice { get; set; }

        /// <summary>
        ///
        /// </summary>
        [DataMember]
        public WEnum<OrderSides> Side { get; set; }

        /// <summary>
        ///
        /// </summary>
        [DataMember]
        public WEnum<OrderTypes> Type { get; set; }

        /// <summary>
        ///
        /// </summary>
        [DataMember]
        public WEnum<OrderTypes> InitialType { get; set; }

        /// <summary>
        ///
        /// </summary>
        [DataMember]
        public WEnum<OrderStatuses> Status { get; set; }

        /// <summary>
        ///
        /// </summary>
        [DataMember]
        public decimal Amount { get; set; }

        /// <summary>
        ///
        /// </summary>
        [DataMember]
        public decimal RemainingAmount { get; set; }

        /// <summary>
        ///
        /// </summary>
        [DataMember]
        [Obsolete]
        public decimal HiddenAmount { get; set; }

        /// <summary>
        ///
        /// </summary>
        [DataMember]
        public decimal AggrFillPrice { get; set; }

        /// <summary>
        ///
        /// </summary>
        [DataMember]
        public decimal AverageFillPrice { get; set; }

        /// <summary>
        ///
        /// </summary>
        [DataMember]
        public DateTime Created { get; set; }

        /// <summary>
        ///
        /// </summary>
        [DataMember]
        public DateTime? Modified { get; set; }

        /// <summary>
        ///
        /// </summary>
        [DataMember]
        public DateTime? Filled { get; set; }

        /// <summary>
        ///
        /// </summary>
        [DataMember]
        public DateTime? PositionCreated { get; set; }

        /// <summary>
        ///
        /// </summary>
        [DataMember]
        public decimal? StopLoss { get; set; }

        /// <summary>
        ///
        /// </summary>
        [DataMember]
        public decimal? TakeProfit { get; set; }

        /// <summary>
        ///
        /// </summary>
        [DataMember]
        public decimal? Profit { get; set; }

        /// <summary>
        ///
        /// </summary>
        [DataMember]
        public decimal? Margin { get; set; }

        /// <summary>
        ///
        /// </summary>
        [DataMember]
        public string UserComment { get; set; }

        /// <summary>
        ///
        /// </summary>
        [DataMember]
        public string ManagerComment { get; set; }

        /// <summary>
        ///
        /// </summary>
        [DataMember]
        public string UserTag { get; set; }

        /// <summary>
        ///
        /// </summary>
        [DataMember]
        public string ManagerTag { get; set; }

        /// <summary>
        ///
        /// </summary>
        [DataMember]
        public int Magic { get; set; }

        /// <summary>
        ///
        /// </summary>
        [DataMember]
        public decimal? Commission { get; set; }

        /// <summary>
        ///
        /// </summary>
        [DataMember]
        public decimal? AgentCommision { get; set; }

        [DataMember]
        public decimal? Swap { get; set; }

        /// <summary>
        ///
        /// </summary>
        [DataMember]
        public DateTime? Expired { get; set; }

        /// <summary>
        ///
        /// </summary>
        [DataMember]
        public decimal? ClosePrice { get; set; }

        /// <summary>
        ///
        /// </summary>
        [DataMember]
        public decimal? MarginRateInitial { get; set; }

        /// <summary>
        ///
        /// </summary>
        [DataMember]
        public decimal? MarginRateCurrent { get; set; }

        /// <summary>
        ///
        /// </summary>
        [DataMember]
        public decimal? OpenConversionRate { get; set; }

        /// <summary>
        ///
        /// </summary>
        [DataMember]
        public decimal? CloseConversionRate { get; set; }

        /// <summary>
        ///
        /// </summary>
        [DataMember]
        public int Version { get; set; }

        /// <summary>
        ///
        /// </summary>
        [DataMember]
        public WEnum<OrderExecutionOptions> Options { get; set; }

        /// <summary>
        ///
        /// </summary>
        [DataMember]
        public CustomProperties Properties { get; set; }

        /// <summary>
        ///
        /// </summary>
        [DataMember]
        public decimal? Taxes { get; set; }

        /// <summary>
        ///
        /// </summary>
        [DataMember]
        public decimal? ReqOpenPrice { get; set; }

        /// <summary>
        ///
        /// </summary>
        [DataMember]
        public decimal? ReqOpenAmount { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public string ClientApp { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public bool IsReducedOpenCommission { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public bool IsReducedCloseCommission { get; set; }

        /// <summary>
        ///
        /// </summary>
        [DataMember]
        public decimal? MaxVisibleAmount { get; set; }

        /// <summary>
        ///
        /// </summary>
        [DataMember]
        public decimal? CurrentPrice { get; set; }

        /// <summary>
        ///
        /// </summary>
        [DataMember]
        public decimal? TransferringCoefficient { get; set; }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public Order Clone()
        {
            return new Order(this);
        }

        /// <summary>
        ///
        /// </summary>
        public decimal FilledAmount => Amount - RemainingAmount;

        /// <summary>
        ///
        /// </summary>
        public decimal VisibleAmount => RemainingAmount - ((MaxVisibleAmount.HasValue && (MaxVisibleAmount.Value < RemainingAmount)) ? (RemainingAmount - MaxVisibleAmount.Value) : 0);

        /// <summary>
        ///
        /// </summary>
        public decimal TotalCommission => Commission.GetValueOrDefault(0) + AgentCommision.GetValueOrDefault(0);

        /// <summary>
        /// Pumping activation flag. Used only in client manager model.
        /// </summary>
        public ActivationTypes Activation { get; set; }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var builder = new BriefToStringBuilder();
            builder.Append("OrderId", OrderId, IdLogFormat);
            builder.Append("AccountLogin", AccountId);
            builder.Append("Symbol", Symbol);
            builder.Append("Side", Side);
            builder.Append("Type", Type);
            builder.AppendNotNull("Price", Price);
            builder.AppendNotNull("StopPrice", StopPrice);
            builder.Append("Status", Status);
            builder.Append("Amount", Amount);
            builder.Append("RemainingAmount", RemainingAmount);
            builder.Append("MaxVisibleAmount", MaxVisibleAmount);
            builder.Append("AggrFillPrice", AggrFillPrice);
            builder.Append("AverageFillPrice", AverageFillPrice);
            builder.AppendNotNull("SL", StopLoss);
            builder.AppendNotNull("TP", TakeProfit);
            builder.AppendNotNull("Commission", Commission);
            builder.AppendNotNull("AgentCommision", AgentCommision);
            builder.AppendNotNull("Swap", Swap);
            builder.AppendNotNull("CurrentPrice", CurrentPrice);
            builder.AppendNotNull("ClientOrderId", ClientOrderId);
            builder.AppendNotNull("ParentOrderId", ParentOrderId);
            builder.Append("Created", Created, PlatformStd.LogDateTimeFormatAction);
            builder.AppendNotNull("Modified", Modified, PlatformStd.LogDateTimeFormatAction);
            builder.AppendNotNull("Filled", Filled, PlatformStd.LogDateTimeFormatAction);
            builder.AppendNotNull("Expired", Expired, PlatformStd.LogDateTimeFormatAction);
            builder.AppendNotNull("PositionCreated", PositionCreated, PlatformStd.LogDateTimeFormatAction);
            builder.AppendNotNull("UserComment", UserComment);
            builder.AppendNotNull("ManagerComment", ManagerComment);
            builder.AppendNotNull("UserTag", UserTag);
            builder.AppendNotNull("ManagerTag", ManagerTag);
            builder.AppendNotNull("TransferringCoefficient", TransferringCoefficient);
            builder.Append("Magic", Magic);
            builder.Append("Options", Options);
            builder.Append("MarginRateInitial", MarginRateInitial);
            builder.Append("MarginRateCurrent", MarginRateCurrent);
            builder.Append("OpenConversionRate", OpenConversionRate);
            builder.Append("CloseConversionRate", CloseConversionRate);
            builder.Append("IsReducedOpenCommission", IsReducedOpenCommission);
            builder.Append("IsReducedCloseCommission", IsReducedCloseCommission);
            builder.Append("Activation", Activation);
            builder.Append("Taxes", Taxes);
            builder.Append("ReqOpenPrice", ReqOpenPrice);
            builder.Append("ReqOpenAmount", ReqOpenAmount);
            builder.Append("Version", Version);
            builder.AppendNotNull("ClientApp", ClientApp);
            return builder.GetResult();
        }

        /// <summary>
        ///
        /// </summary>
        public bool IsPending
        {
            get { return this.GetIsPending(); }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="option"></param>
        /// <returns></returns>
        public bool IsOptionSet(OrderExecutionOptions option)
        {
            return (Options & option) != 0;
        }

        /// <summary>
        /// Check if there are major changes of the order
        /// </summary>
        /// <param name="order">New order with the same OrderId</param>
        /// <returns>Returns true if it has any major changes</returns>
        public bool IsSameOrder(IOrder order)
        {
            return (order != null && this.OrderId == order.OrderId && this.Type == order.Type);
        }

        #region ICloneable Members

        object ICloneable.Clone()
        {
            return Clone();
        }

        #endregion

        #region IExtensibleDataObject

        [NonSerialized]
        private ExtensionDataObject theData;

        public virtual ExtensionDataObject ExtensionData
        {
            get { return theData; }
            set { theData = value; }
        }

        #endregion

        #region WEnum

        OrderSides IOrder.Side { get { return Side; } }
        OrderTypes IOrder.Type { get { return Type; } }
        OrderTypes IOrder.InitialType { get { return InitialType; } }
        OrderStatuses IOrder.Status { get { return Status; } }
        OrderExecutionOptions IOrder.Options { get { return Options; } }

        #endregion
    }

    /// <summary>
    ///
    /// </summary>
    public static class OrderExtension
    {
        public static bool GetIsPending(this IOrder order)
        {
            return order.Type == OrderTypes.Limit || order.Type == OrderTypes.Stop || order.Type == OrderTypes.StopLimit;
        }
    }

    public interface IOrder
    {
        int RangeId { get; }
        long AccountId { get; }
        string Symbol { get; }
        string SymbolAlias { get; }
        long OrderId { get; }
        string ClientOrderId { get; }
        long? ParentOrderId { get; }
        decimal? Price { get; }
        decimal? StopPrice { get; }
        OrderSides Side { get; }
        OrderTypes Type { get; }
        OrderTypes InitialType { get; }
        OrderStatuses Status { get; }
        decimal Amount { get; }
        decimal RemainingAmount { get; }
        [Obsolete]
        decimal HiddenAmount { get; }
        decimal? MaxVisibleAmount { get; }
        DateTime Created { get; }
        DateTime? Modified { get; }
        DateTime? Filled { get; }
        DateTime? PositionCreated { get; }
        decimal? StopLoss { get; }
        decimal? TakeProfit { get; }
        decimal? Profit { get; }
        decimal? Margin { get; }
        decimal AggrFillPrice { get; }
        decimal AverageFillPrice { get; }
        decimal? TransferringCoefficient { get; }
        string UserComment { get; }
        string ManagerComment { get; }
        string UserTag { get; }
        string ManagerTag { get; }
        int Magic { get; }
        decimal? Commission { get; }
        decimal? AgentCommision { get; }
        decimal? Swap { get; }
        DateTime? Expired { get; }
        decimal? ClosePrice { get; }
        decimal? CurrentPrice { get; }
        decimal? MarginRateInitial { get; }
        decimal? MarginRateCurrent { get; }
        ActivationTypes Activation { get; }
        decimal? OpenConversionRate { get; }
        decimal? CloseConversionRate { get; }
        bool IsReducedOpenCommission { get; }
        bool IsReducedCloseCommission { get; }
        int Version { get; }
        OrderExecutionOptions Options { get; }
        CustomProperties Properties { get; }
        decimal? Taxes { get; }
        decimal? ReqOpenPrice { get; }
        decimal? ReqOpenAmount { get; }
        string ClientApp { get; }

    }
}
