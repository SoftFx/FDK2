namespace TickTrader.FDK.Common
{
    using System;

    public class ExecutionReport
    {
        public ExecutionReport()
        {
        }

        /// <summary>
        /// Gets ExecType = 150 field.
        /// </summary>
        public ExecutionType ExecutionType { get; set; }

        /// <summary>
        /// Gets ClOrdID = 11 field.
        /// </summary>
        public string ClientOrderId { get; set; }

        /// <summary>
        /// Gets OrigClOrdID = 41 field.
        /// </summary>
        public string OrigClientOrderId { get; set; }

        /// <summary>
        /// Gets OrderID = 37 field.
        /// </summary>
        public string OrderId { get; set; }

        /// <summary>
        /// Gets Symbol = 55 field.
        /// </summary>
        public string Symbol { get; set; }

        /// <summary>
        /// Gets Side = 54 field.
        /// </summary>
        /// <remarks>
        /// For backward compatibility see TradeRecordSide property.
        /// </remarks>
        public OrderSide OrderSide { get; set; }

        /// <summary>
        /// Gets OrdType = 40 field.
        /// </summary>
        /// <remarks>
        /// For backward compatibility see TradeRecordType property.
        /// </remarks>
        public OrderType OrderType { get; set; }

        /// <summary>
        /// Gets TimeInForce field.
        /// </summary>
        public OrderTimeInForce? OrderTimeInForce { get; set; }
        
        /// <summary>
        /// Gets OrdType = 40 field.
        /// </summary>
        /// <remarks>
        /// For backward compatibility see TradeRecordType property.
        /// </remarks>
        public OrderType InitialOrderType { get; set; }

        /// <summary>
        /// Gets OrderQty = 38 field.
        /// </summary>
        public double? InitialVolume { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double? InitialPrice { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double? MaxVisibleVolume { get; set; }

        /// <summary>
        /// Gets Price = 44 field.
        /// </summary>
        public double? Price { get; set; }

        /// <summary>
        /// Gets StopPx = 99 field.
        /// </summary>
        public double? StopPrice { get; set; }

        /// <summary>
        /// Gets ExpireTime = 126 field.
        /// </summary>
        public DateTime? Expiration { get; set; }

        /// <summary>
        /// Gets TakeProfit = 10037 field.
        /// </summary>
        public double? TakeProfit { get; set; }

        /// <summary>
        /// Gets StopLoss = 10038 field.
        /// </summary>
        public double? StopLoss { get; set; }

        /// <summary>
        /// Gets MarketWithSlippage flag.
        /// </summary>
        public bool MarketWithSlippage { get; set; }

        /// <summary>
        /// Gets OrdStatus = 39 field.
        /// </summary>
        public OrderStatus OrderStatus { get; set; }

        /// <summary>
        /// Gets CumQty = 14 field.
        /// </summary>
        public double ExecutedVolume { get; set; }

        /// <summary>
        /// Gets LeavesQty = 151 field.
        /// </summary>
        public double LeavesVolume { get; set; }

        /// <summary>
        /// Gets HiddenVolume.
        /// </summary>
        public double? HiddenVolume { get; set; }

        /// <summary>
        /// Get LastQty = 32 field.
        /// </summary>
        public double? TradeAmount { get; set; }

        /// <summary>
        /// Get LastPx field.
        /// </summary>
        public double? TradePrice { get; set; }

        /// <summary>
        /// Gets Commission = 12 field.
        /// </summary>
        public double Commission { get; set; }

        /// <summary>
        /// Gets AgentCommission = 10113 field.
        /// </summary>
        public double AgentCommission { get;  set;}

        /// <summary>
        /// 
        /// </summary>
        public bool ReducedOpenCommission { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool ReducedCloseCommission { get; set; }

        /// <summary>
        /// Gets Swap = 10096 field.
        /// </summary>
        public double Swap { get; set; }

        /// <summary>
        /// Gets AvgPx = 6 field.
        /// </summary>
        public double? AveragePrice { get; set; }

        /// <summary>
        /// Gets OrdCreated = 10083
        /// </summary>
        public DateTime? Created { get; set; }

        /// <summary>
        /// Gets OrdModified = 10084
        /// </summary>
        public DateTime? Modified { get; set; }

        /// <summary>
        /// Gets OrdRejReason = 103 field.
        /// </summary>
        public RejectReason RejectReason { get; set; }

        /// <summary>
        /// Gets Text = 58 field.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets user comment, if it is available
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// Gets user tag, if it is available
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        /// Gets magic number
        /// </summary>
        public int? Magic { get; set; }

        /// <summary>
        /// Gets assets; it is available for cash accounts only.
        /// </summary>
        public AssetInfo[] Assets { get; set; }

        /// <summary>
        /// Account balance.
        /// </summary>
        public double? Balance { get; set; }

        /// <summary>
        /// Account balance movement.
        /// </summary>
        public double? BalanceTradeAmount { get; set; }

        /// <summary>
        /// Immediate Or Cancel Flag.
        /// </summary>
        public bool ImmediateOrCancelFlag { get; set; }

        /// <summary>
        /// Optional Slippage.
        /// </summary>
        public double? Slippage { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool Last { get; set; }

        public ExecutionReport Clone()
        {
            ExecutionReport executionReport = new ExecutionReport();
            executionReport.ExecutionType = ExecutionType;
            executionReport.ClientOrderId = ClientOrderId;
            executionReport.OrigClientOrderId = OrigClientOrderId;
            executionReport.OrderId = OrderId;
            executionReport.Symbol = Symbol;
            executionReport.OrderSide = OrderSide;
            executionReport.OrderType = OrderType;
            executionReport.OrderTimeInForce = OrderTimeInForce;
            executionReport.InitialVolume = InitialVolume;
            executionReport.MaxVisibleVolume = MaxVisibleVolume;
            executionReport.Price = Price;
            executionReport.StopPrice = StopPrice;
            executionReport.Expiration = Expiration;
            executionReport.TakeProfit = TakeProfit;
            executionReport.StopLoss = StopLoss;
            executionReport.MarketWithSlippage = MarketWithSlippage;
            executionReport.OrderStatus = OrderStatus;
            executionReport.ExecutedVolume = ExecutedVolume;
            executionReport.LeavesVolume = LeavesVolume;
            executionReport.HiddenVolume = HiddenVolume;
            executionReport.TradeAmount = TradeAmount;
            executionReport.TradePrice = TradePrice;
            executionReport.Commission = Commission;
            executionReport.AgentCommission = AgentCommission;
            executionReport.ReducedOpenCommission = ReducedOpenCommission;
            executionReport.ReducedCloseCommission = ReducedCloseCommission;
            executionReport.Swap = Swap;
            executionReport.AveragePrice = AveragePrice;
            executionReport.Created = Created;
            executionReport.Modified = Modified;
            executionReport.RejectReason = RejectReason;
            executionReport.Text = Text;
            executionReport.Comment = Comment;
            executionReport.Tag = Tag;
            executionReport.Magic = Magic;
            if (Assets != null)
            {
                executionReport.Assets = (AssetInfo[])Assets.Clone();
            }
            else
                executionReport.Assets = null;
            executionReport.Balance = Balance;
            executionReport.BalanceTradeAmount = BalanceTradeAmount;
            executionReport.Last = Last;
            executionReport.ImmediateOrCancelFlag = ImmediateOrCancelFlag;
            executionReport.Slippage = Slippage;

            return executionReport;
        }

        public override string ToString()
        {
            return string.Format("ExecutionType = {0}; ClientOrderId = {1}; OrderId = {2}; OrderType = {3}; Symbol = {4}; OrderSide = {5}; InitialVolume = {6}; Price = {7}; OrderStatus = {8}; LeavesVolume = {9}; TradeAmount = {10}; TradePrice = {11}", ExecutionType, ClientOrderId, OrderId, OrderType, Symbol, OrderSide, InitialVolume, Price, OrderStatus, LeavesVolume, TradeAmount, TradePrice);
        }
    }
}
