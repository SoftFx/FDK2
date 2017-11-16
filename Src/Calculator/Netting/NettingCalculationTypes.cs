namespace TickTrader.FDK.Calculator.Netting
{
    /// <summary>
    /// Netting calculation types.
    /// </summary>
    public enum NettingCalculationTypes
    {
        /// <summary>
        // Calculate and update each order separately, then aggragate results.
        /// </summary>
        OneByOne,

        /// <summary>
        /// Calculate netted margin/profit. Orders are not updated.
        /// </summary>
        Optimized,
    }
}
