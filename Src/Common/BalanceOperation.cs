namespace TickTrader.FDK.Common
{
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
        /// Returns formatted string for the class instance.
        /// </summary>
        /// <returns>Can not be null.</returns>
        public override string ToString()
        {
            return string.Format("Transaction currency = {0}; Balance = {1}; Transaction amount = {2}", this.TransactionCurrency, this.Balance, this.TransactionAmount);
        }
    }
}
