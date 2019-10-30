namespace TickTrader.FDK.Common
{
    using System;

    /// <summary>
    /// Contains account information.
    /// </summary>
    public class AccountInfo
    {
        public AccountInfo()
        {
        }

        /// <summary>
        /// Gets the the account id.
        /// </summary>
        public string AccountId { get; set; }

        /// <summary>
        /// Gets the accounting type.
        /// </summary>
        public AccountType Type { get; set; }

        /// <summary>
        /// Gets account name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string FirtName { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Phone { get; set; }
	
        /// <summary>
        /// 
        /// </summary>
        public string Country { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string ZipCode { get; set; }

        /// <summary>
        /// Gets account comment.
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// Gets account email.
        /// </summary>
        public string Email { get; set; }
	
        /// <summary>
        /// Gets the account balance currency.
        /// </summary>
        public string Currency { get; set; }
	
        /// <summary>
        /// Gets the account registered date.
        /// </summary>
        public DateTime? RegistredDate { get; set; }

        /// <summary>
        /// Gets the account leverage.
        /// </summary>
        public int? Leverage { get; set; }

        /// <summary>
        /// Gets the account balance.
        /// </summary>
        public double? Balance { get; set; }

        /// <summary>
        /// Gets the account margin.
        /// </summary>
        public double? Margin { get; set; }

        /// <summary>
        /// Gets the account equity.
        /// </summary>
        public double? Equity { get; set; }

        /// <summary>
        /// Gets margin call level.
        /// </summary>
        public double? MarginCallLevel { get; set; }

        /// <summary>
        /// Get stop out level.7
        /// </summary>
        public double? StopOutLevel { get; set; }

        /// <summary>
        /// Gets account state:
        /// true, if account is valid
        /// false, if account has broken/invalid trades
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets true, if account can trade, otherwise false (investor password).
        /// </summary>
        public bool IsReadOnly { get; set; }

        /// <summary>
        /// Gets whether account is blocked or not.
        /// </summary>
        public bool IsBlocked { get; set; }

        /// <summary>
        /// Gets whether account is blocked or not.
        /// </summary>
        public bool IsWebApiEnabled { get; set; }

        /// <summary>
        /// Gets assets; this feature is available for cash accounts only.
        /// </summary>
        public AssetInfo[] Assets { get; set; }

        /// <summary>
        /// Gets throttling account's information.
        /// </summary>
        public ThrottlingInfo Throttling { get; set; }

        /// <summary>
        /// Gets account report currency.
        /// </summary>
        public string ReportCurrency { get; set; }

        /// <summary>
        /// Token Commission Currency.
        /// </summary>
        public string TokenCommissionCurrency { get; set; }

        /// <summary>
        /// Token Commission Currency Discount
        /// </summary>
        public double? TokenCommissionCurrencyDiscount { get; set; }

        /// <summary>
        /// Gets whether token commission is enabled or not.
        /// </summary>
        public bool IsTokenCommissionEnabled { get; set; }

        /// <summary>
        /// Returns formatted string for the class instance.
        /// </summary>
        /// <returns>can not be null</returns>
        public override string ToString()
        {
            return string.Format("AccountId = {0}; Type = {1}; Readonly = {2}; Currency = {3}; Leverage = {4}; Balance = {5}; Equity = {6}; Margin = {7}", this.AccountId, this.Type, this.IsReadOnly, this.Currency, this.Leverage, this.Balance, this.Equity, this.Margin);
        }
    }
}
