﻿namespace TickTrader.FDK.Common
{
    using System;

    /// <summary>
    /// Contains position information for a symbol.
    /// </summary>
    public class Position
    {
        public Position()
        {
        }

        /// <summary>
        /// Gets the position symbol.
        /// </summary>
        public string Symbol { get; set; }

        /// <summary>
        /// Gets total amount, which has been bought.
        /// </summary>
        public double BuyAmount { get; set; }

        /// <summary>
        /// Gets total amount, which has been sold.
        /// </summary>
        public double SellAmount { get; set; }

        /// <summary>
        /// Gets commission.
        /// </summary>
        public double Commission { get; set; }

        /// <summary>
        /// Gets agent commission.
        /// </summary>
        public double AgentCommission { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double Swap { get; set; }

        /// <summary>
        /// It's used by FinancialCalculator.
        /// </summary>
        public double? Profit { get; set; }

        /// <summary>
        /// It's used by FinancialCalculator.
        /// </summary>
        public double? Margin { get; set; }

        /// <summary>
        /// Gets average price of buy position.
        /// </summary>
        public double? BuyPrice { get; set; }

        /// <summary>
        /// Gets average price of sell position.
        /// </summary>
        public double? SellPrice { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime? Modified { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double? BidPrice { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double? AskPrice { get; set; }

        /// <summary>
        /// Returns formatted string for the class instance.
        /// </summary>
        /// <returns>Can not be null.</returns>
        public override string ToString()
        {
            return string.Format("Symbol = {0}; Buy Price = {1}; Buy Amount = {2}; Sell Price = {3}; Sell Amount = {4}", this.Symbol, this.BuyPrice, this.BuyAmount, this.SellPrice, this.SellAmount);
        }
    }
}