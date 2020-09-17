using System;
using System.Linq;
using TickTrader.FDK.Calculator.Adapter;
using System.Collections.Generic;
using TickTrader.FDK.Common;
using TickTrader.FDK.Extended;

namespace TickTrader.FDK.Calculator
{
    /// <summary>
    /// Contains methods for offline calculation of profit and margin.
    /// </summary>
    public class FinancialCalculator
    {
        #region Construction

        /// <summary>
        /// Creates a new financial calculator instance.
        /// </summary>
        public FinancialCalculator()
        {
            IsInitialized = false;
            Prices = new Dictionary<string, ISymbolRate>();
            MarketState = new MarketState(false);
            Account = new AccountAdapter(GetSymbolOrNull);
        }

        #endregion

        #region Methods
        internal void Initialize(IEnumerable<SymbolInfo> symbols, IEnumerable<CurrencyInfo> currencies,
            IEnumerable<Quote> quotes, AccountInfo accountInfo, IEnumerable<TradeRecord> orders,
            IEnumerable<Position> positions)
        {
            if (IsInitialized)
                Clear();

            var symbolAdapters = symbols.OrderBy(o => o.GroupSortOrder).ThenBy(o => o.SortOrder).ThenBy(o => o.Name)
                .Select(o => new SymbolModel(o)).ToList();
            var currencyAdapters = currencies.OrderBy(c => c.SortOrder).Select(c => new CurrencyModel(c)).ToList();

            Symbols = symbolAdapters.ToDictionary(s => s.Symbol);
            MarketState.Init(symbolAdapters, currencyAdapters);

            UpdateRates(quotes);

            Account.AccountingType = accountInfo.Type;
            Account.Balance = (decimal)accountInfo.Balance.GetValueOrDefault();
            Account.BalanceCurrency = accountInfo.Currency;
            Account.Leverage = accountInfo.Leverage ?? 1;

            var assets = accountInfo.Assets;
            Account.InitAssets(assets);
            Account.InitOrders(orders);
            Account.InitPositions(positions);

            Account.InitCalculator(MarketState);

            IsInitialized = true;
        }

        internal void UpdateRates(IEnumerable<Quote> quotes)
        {
            foreach (var quote in quotes)
            {
                var newRate = quote.ToSymbolRate();
                Prices[newRate.Symbol] = newRate;
            }
            MarketState.Update(Prices.Values);
        }

        internal void UpdateRate(ISymbolRate newRate)
        {
            Prices[newRate.Symbol] = newRate;
            MarketState.Update(newRate);
        }

        internal void ProcessAccountInfoUpdate(AccountInfo accountInfoUpdate)
        {
            Account.AccountingType = accountInfoUpdate.Type;
            Account.Balance = (decimal)accountInfoUpdate.Balance.GetValueOrDefault();
            Account.BalanceCurrency = accountInfoUpdate.Currency;
            Account.Leverage = accountInfoUpdate.Leverage ?? 1;
            var assets = accountInfoUpdate.Assets;
            Account.UpdateAssets(assets);
        }

        internal void ProcessTradeUpdate(TradeUpdate update)
        {
            Account.ProcessUpdate(update);
        }

        internal void ProcessPositionUpdate(NetPositionUpdate update)
        {
            Account.ProcessUpdate(update?.NewPosition);
        }

        internal SymbolModel GetSymbolOrNull(string symbolName)
        {
            SymbolModel symbol;
            try
            {
                if (Symbols != null && Symbols.TryGetValue(symbolName, out symbol))
                    return symbol;
            }
            catch { }

            return null;
        }

        internal void Clear()
        {
            Account.Clear();
            Symbols.Clear();
            Prices.Clear();
            IsInitialized = false;
        }

        /// <summary>
        ///  Calculates asset cross rate.
        /// </summary>
        /// <param name="asset">Asset volume.</param>
        /// <param name="assetCurrency">Asset currency.</param>
        /// <param name="currency">Deposit currency.</param>
        /// <returns>Rate or null if rate cannot be calculated.</returns>
        [Obsolete]
        public double? CalculateAssetRate(double asset, string assetCurrency, string currency)
        {
            if (assetCurrency == null)
                throw new ArgumentNullException(nameof(assetCurrency));
            if (currency == null)
                throw new ArgumentNullException(nameof(currency));

            try
            {
                if (!IsInitialized)
                    return null;

                if (asset >= 0)
                    return (double)MarketState.ConversionMap.GetPositiveAssetConversion(assetCurrency, currency).Value;
                else
                    return (double)MarketState.ConversionMap.GetNegativeAssetConversion(assetCurrency, currency).Value;
            }
            catch (Exception)
            {
                return null;
            }
        }
        /// <summary>
        ///  Converts volume in currency Y to currency Z (profit currency to deposit currency)
        /// </summary>
        /// <param name="amount">Volume.</param>
        /// <param name="symbol">Symbol X/Y.</param>
        /// <param name="depositCurrency">Deposit currency.</param>
        /// <returns>Rate or null if rate cannot be calculated.</returns>
        public double? ConvertYToZ(double amount, string symbol, string depositCurrency)
        {
            if (symbol == null)
                throw new ArgumentNullException(nameof(symbol));
            if (depositCurrency == null)
                throw new ArgumentNullException(nameof(depositCurrency));

            try
            {
                if (!IsInitialized)
                    return null;

                double rate = 1.0;
                if (amount >= 0)
                {
                    rate = (double)MarketState.ConversionMap.GetPositiveProfitConversion(symbol, depositCurrency).Value;
                }
                else
                {
                    rate = (double)MarketState.ConversionMap.GetNegativeProfitConversion(symbol, depositCurrency).Value;
                }
                return rate * amount;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        ///  Converts volume in currency X to currency Z (margin currency to deposit currency)
        /// </summary>
        /// <param name="amount">Volume.</param>
        /// <param name="symbol">Symbol X/Y.</param>
        /// <param name="depositCurrency">Deposit currency.</param>
        /// <returns>Rate or null if rate cannot be calculated.</returns>
        public double? ConvertXToZ(double amount, string symbol, string depositCurrency)
        {
            if (symbol == null)
                throw new ArgumentNullException(nameof(symbol));
            if (depositCurrency == null)
                throw new ArgumentNullException(nameof(depositCurrency));

            try
            {
                if (!IsInitialized)
                    return null;

                var node = MarketState.GetSymbolNode(symbol, false);
                if (node == null)
                    return null;

                double rate = (double)MarketState.ConversionMap.GetMarginFormula(node, depositCurrency).Value;
                return rate * amount;
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion

        #region Internal Properties

        internal bool IsInitialized { get; private set; }
        internal IDictionary<string, ISymbolRate> Prices { get; }
        internal MarketState MarketState { get; }
        internal AccountAdapter Account { get; }
        internal IDictionary<string, SymbolModel> Symbols { get; private set; }

        #endregion

        #region Fields

        #endregion
    }
}
