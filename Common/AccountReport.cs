namespace TickTrader.FDK.Common
{
    using System;

    /// <summary>
    /// 
    /// </summary>
    public class AccountReport
    {
        public AccountReport()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string AccountId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public AccountType Type { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int Leverage { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double Balance { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string BalanceCurrency { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double Profit { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double Commission { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double AgentCommission { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double TotalCommission => Commission + AgentCommission;

        /// <summary>
        /// 
        /// </summary>
        public double Swap { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double TotalProfitLoss => Profit + TotalCommission + Swap;

        /// <summary>
        /// 
        /// </summary>
        public double Equity { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double Margin { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double MarginLevel { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool IsBlocked { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool IsReadOnly { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Position[] Positions { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public AssetInfo[] Assets { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double? BalanceCurrencyToUsdConversionRate { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double? UsdToBalanceCurrencyConversionRate { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double? ProfitCurrencyToUsdConversionRate { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double? UsdToProfitCurrencyConversionRate { get; set; }

        public AccountReport Clone()
        {
            AccountReport accountReport = new AccountReport();
            accountReport.Timestamp = Timestamp;
            accountReport.AccountId = AccountId;
            accountReport.Type = Type;
            accountReport.Leverage = Leverage;
            accountReport.Balance = Balance;
            accountReport.BalanceCurrency = BalanceCurrency;
            accountReport.Profit = Profit;
            accountReport.Commission = Commission;
            accountReport.AgentCommission = AgentCommission;
            accountReport.Swap = Swap;
            accountReport.Equity = Equity;
            accountReport.Margin = Margin;
            accountReport.MarginLevel = MarginLevel;
            accountReport.IsBlocked = IsBlocked;
            accountReport.IsValid = IsValid;
            accountReport.IsReadOnly = IsReadOnly;
            accountReport.Positions = (Position[])Positions.Clone();
            accountReport.Assets = (AssetInfo[])Assets.Clone();
            accountReport.BalanceCurrencyToUsdConversionRate = BalanceCurrencyToUsdConversionRate;
            accountReport.UsdToBalanceCurrencyConversionRate = UsdToBalanceCurrencyConversionRate;
            accountReport.ProfitCurrencyToUsdConversionRate = ProfitCurrencyToUsdConversionRate;
            accountReport.UsdToProfitCurrencyConversionRate = UsdToProfitCurrencyConversionRate;

            return accountReport;
        }

        /// <summary>
        /// Returns formatted string for the class instance.
        /// </summary>
        /// <returns>can not be null</returns>
        public override string ToString()
        {
            return string.Format("AccountId = {0}; Type = {1}; Readonly = {2}; BalanceCurrency = {3}; Leverage = {4}; Balance = {5}; Equity = {6}; Margin = {7}", this.AccountId, this.Type, this.IsReadOnly, this.BalanceCurrency, this.Leverage, this.Balance, this.Equity, this.Margin);
        }
    }
}
