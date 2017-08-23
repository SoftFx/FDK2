namespace TickTrader.FDK.Objects
{
    /// <summary>
    /// Currency information.
    /// </summary>
    public class CurrencyInfo
    {
        public CurrencyInfo()
        {
        }

        /// <summary>
        /// Gets currency name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets currency description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets currency priority.
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// Gets currency precision.
        /// </summary>
        public int Precision { get; set; }
    }
}
