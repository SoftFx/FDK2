using TickTrader.FDK.Calculator.Adapter;

namespace TickTrader.FDK.Calculator
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using TickTrader.FDK.Common;
    using TickTrader.FDK.Extended;

    /// <summary>
    /// Represents financial state of account.
    /// </summary>
    [DebuggerDisplay("#{Generation}: Balance={Balance}; Profit={Profit}; Margin={Margin}")]
    public sealed class StateInfo
    {
        #region Construction

        internal StateInfo(AccountAdapter account, IDictionary<string, Quote> quotes, long generation, bool isCalculatorInitialized)
        {
            this.Generation = generation;

            this.Balance = (double)account.BalanceRounded;
            this.Profit = (double)account.ProfitRounded;
            this.Margin = (double)account.MarginRounded;
            this.Equity = (double)account.EquityRounded;
            this.MarginLevel = (double)account.MarginLevelRounded;
            this.Commission = (double)account.CommissionRounded;
            this.Swap = (double)account.SwapRounded;
            this.Rebate = (double)account.RebateRounded;
            this.AgentCommission = (double)account.AgentCommissionRounded;

            this.Quotes = quotes;
            
            CalcError assetsError;
            this.Assets = account.GetAssetsCalculated(out assetsError);
            this.TradeRecords = account.GetOrdersCalculated();
            this.Positions = account.GetPositionsCalculated();
            this.UnknownSymbols = account.GetUnknownSymbols();

            this.Status = isCalculatorInitialized
                ? (CalcError.GetWorst(account.CalcWorstError, assetsError)?.Code ?? CalcErrorCode.None)
                    .ToAccountEntryStatus()
                : AccountEntryStatus.NotCalculated;
        }

        #endregion

        /// <summary>
        /// Gets account calculation status.
        /// </summary>
        public AccountEntryStatus Status { get; private set; }

        /// <summary>
        /// Gets the number, which indicates how many times financial information has been updated.
        /// </summary>
        public long Generation { get; private set; }

        /// <summary>
        /// Gets balance of account, which has been specified for data trade object.
        /// </summary>
        public double Balance { get; private set; }

        /// <summary>
        /// Gets equity of account.
        /// </summary>
        public double Equity { get; private set; }

        /// <summary>
        /// Gets profit of all opened positions for data trade account by data feed quotes.
        /// </summary>
        public double Profit { get; private set; }

        /// <summary>
        /// Gets margin of data trade account by data feed quotes.
        /// </summary>
        public double Margin { get; private set; }

        /// <summary>
        /// Gets total commission.
        /// </summary>
        public double Commission { get; private set; }

        /// <summary>
        /// Gets total agent commission.
        /// </summary>
        public double AgentCommission { get; private set; }

        /// <summary>
        /// Gets total swap.
        /// </summary>
        public double Swap { get; private set; }

        /// <summary>
        /// Gets total rebate.
        /// </summary>
        public double Rebate { get; private set; }

        /// <summary>
        /// Gets free margin.
        /// </summary>
        public double FreeMargin
        {
            get
            {
                return this.Equity - this.Margin;
            }
        }

        /// <summary>
        /// Gets margin level.
        /// </summary>
        public double MarginLevel { get; private set; }

        /// <summary>
        /// Quotes snapshot, which has been used for calculation the financial information.
        /// </summary>
        public IDictionary<string, Quote> Quotes { get; private set; }

        /// <summary>
        /// Gets list of available assets.
        /// </summary>
        public IDictionary<string, Asset> Assets { get; private set; }

        /// <summary>
        /// Gets list of available trade records.
        /// </summary>
        public TradeRecord[] TradeRecords { get; private set; }

        /// <summary>
        /// Gets list of opened positions. Available for .NET account only.
        /// </summary>
        public Position[] Positions { get; private set; }

        /// <summary>
        /// Gets list of symbols, which are not supported by server.
        /// Example: user has opened position by BTC/USD, but the corresponding symbol information is not available.
        /// </summary>
        public string[] UnknownSymbols { get; private set; }

        #region Overrides

        /// <summary>
        /// Returns formatted string for the class instance.
        /// </summary>
        /// <returns>Can not be null.</returns>
        public override string ToString()
        {
            return string.Format("#{0}: Balance={1}; Profit={2}; Margin={3}", this.Generation, this.Balance, this.Profit, this.Margin);
        }

        #endregion
    }
}
