namespace TickTrader.FDK.Common
{
    /// <summary>
    /// The type of balance transaction
    /// </summary>
    public enum BalanceTransactionType
    {
        /// <summary>
        /// Deposit or Withdrawal (depends on the sign of TransactionAmount)
        /// </summary>
        DepositWithdrawal,
        /// <summary>
        /// Dividend
        /// </summary>
        Dividend
    }

    /// <summary>
    /// The class contains details of balance operation.
    /// </summary>
    public class BalanceOperation
    {
        public BalanceOperation()
        {
        }

        /// <summary>
        /// Currency of a balance transaction.
        /// </summary>
        public string TransactionCurrency { get; set; }

        /// <summary>
        /// Amount of a balance transaction.
        /// </summary>
        public double TransactionAmount { get; set; }

        /// <summary>
        /// Actual account balance after balance operation.
        /// </summary>
        public double Balance { get; set; }

        /// <summary>
        /// Type of transaction
        /// </summary>
        public BalanceTransactionType TransactionType { get; set; }

        /// <summary>
        /// Returns formatted string for the class instance.
        /// </summary>
        /// <returns>Can not be null.</returns>
        public override string ToString()
        {
            return $"{nameof(TransactionType)}={TransactionType}; {nameof(TransactionCurrency)}={TransactionCurrency}; {nameof(TransactionAmount)}={TransactionAmount}; {nameof(Balance)}={Balance}";
        }
    }
}
