namespace TickTrader.FDK.Calculator
{
    /// <summary>
    /// Defines methods and properties for calculable item.
    /// </summary>
    public interface ICalculable
    {
        /// <summary>
        /// Returns whether item has been calculated.
        /// </summary>
        bool IsCalculated { get; }
    }
}
