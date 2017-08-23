namespace TickTrader.FDK.Objects
{
    /// <summary>
    /// Contains price and volume of bid or ask.
    /// </summary>
    public struct QuoteEntry
    {
        /// <summary>
        /// Price of the quote.
        /// </summary>
        public double Price { get; set; }

        /// <summary>
        /// Volume of the quote.
        /// </summary>
        public double Volume { get; set; }

        /// <summary>
        /// Returns formatted string for the class instance.
        /// </summary>
        /// <returns>can not be null</returns>
        public override string ToString()
        {
            return string.Format("Price = {0}; Volume = {1};", this.Price, this.Volume);
        }
    }
}
