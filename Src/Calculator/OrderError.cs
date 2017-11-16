using System;
using System.Linq;

namespace TickTrader.FDK.Calculator
{
    /// <summary>
    /// Represents order calculation error.
    /// </summary>
    public class OrderError
    {
        public OrderError(BusinessLogicException ex)
        {
            Description = ex.Message;
            Code = ex.CalcError;
            Exception = ex;
        }

        /// <summary>
        /// Creates new instance of order calculation error.
        /// </summary>
        /// <param name="code">Error code.</param>
        /// <param name="description">Error descriptio.</param>
        public OrderError(OrderErrorCode code, string description = null)
        {
            if (code == OrderErrorCode.None)
                throw new ArgumentException("Error code cannot be CalculationErrorCodes.None");

            if (string.IsNullOrEmpty(description))
                description = code.ToString();
        }

        /// <summary>
        /// Gets order calculatiion error description.
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Gets order calculation error code.
        /// </summary>
        public OrderErrorCode Code { get; private set; }

        /// <summary>
        /// Gets order calculation exception
        /// </summary>
        public BusinessLogicException Exception { get; private set; }

        internal static OrderErrorCode GetWorst(OrderErrorCode c1, OrderErrorCode c2)
        {
            if (c1 > c2)
                return c1;

            return c2;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="codes"></param>
        /// <returns></returns>
        internal static OrderErrorCode GetWorst(params OrderErrorCode[] codes)
        {
            return codes.Max();
        }

        public override string ToString()
        {
            return $"{Code}. {Description}";
        }
    }

    /// <summary>
    /// Defines codes for possible calculation errors.
    /// Codes go in order of severity.
    /// </summary>
    public enum OrderErrorCode
    {
        /// <summary>
        /// No error.
        /// </summary>
        None,                   // green

        /// <summary>
        /// Quote is missing.
        /// </summary>
        OffQuotes,              // yellow

        /// <summary>
        /// Configuration is incorrect.
        /// </summary>
        Misconfiguration        // red
    }
}
