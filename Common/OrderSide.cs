﻿namespace TickTrader.FDK.Common
{
    /// <summary>
    /// Enumerates possible orders side.
    /// </summary>
    public enum OrderSide
    {
        None = -1,

        /// <summary>
        /// Specifies 'Position buy', 'Limit buy' or 'Stop buy'.
        /// </summary>
        Buy = 1,

        /// <summary>
        /// Specifies 'Position sell', 'Limit sell' or 'Stop sell'.
        /// </summary>
        Sell = 2
    }
}
