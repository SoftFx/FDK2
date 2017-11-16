using System;
using System.Collections.Generic;
using System.Linq;
using TickTrader.FDK.Calculator.Netting;
using TickTrader.FDK.Calculator.Conversion;

namespace TickTrader.FDK.Calculator
{

    /// <summary>
    /// Manages state (configuration and rates) within accounts with the same configuration (group).
    /// Can be used as a child object of MarketManager or as stand-alone market state.
    /// </summary>
    public sealed class MarketState
    {
        readonly IDictionary<string, SymbolRateTracker> rates = new Dictionary<string, SymbolRateTracker>();
        IEnumerable<ISymbolInfo> sortedSymbols = Enumerable.Empty<ISymbolInfo>();
        IEnumerable<ICurrencyInfo> sortedCurrencies = Enumerable.Empty<ICurrencyInfo>();
        IDictionary<string, ISymbolInfo> symbolsByName = new Dictionary<string, ISymbolInfo>();
        IDictionary<string, ICurrencyInfo> currenciesByName = new Dictionary<string, ICurrencyInfo>();

        internal IEnumerable<ISymbolInfo> Symbols { get { return this.sortedSymbols; } }
        internal IEnumerable<ICurrencyInfo> Currencies { get { return this.sortedCurrencies; } }

        internal MarketUpdateManager RateUpdater { get; private set; }

        public ConversionManager ConversionMap { get; private set; }

        public MarketState(NettingCalculationTypes calcType)
        {
            this.ConversionMap = new ConversionManager(this);
            this.RateUpdater = new MarketUpdateManager(calcType);
        }

        internal ISymbolRate GetRate(string symbol)
        {
            SymbolRateTracker tracker;
            if (rates.TryGetValue(symbol, out tracker))
                return tracker.Rate;
            return null;
        }

        internal ICurrencyInfo GetCurrency(string name)
        {
            return currenciesByName.GetOrDefault(name);
        }

        internal ICurrencyInfo GetCurrencyOrThrow(string name)
        {
            ICurrencyInfo result = currenciesByName.GetOrDefault(name);
            if (result == null)
                throw new MarketConfigurationException("Currency Not Found: " + name);
            return result;
        }

        internal SymbolRateTracker GetSymbolTracker(string symbol)
        {
            SymbolRateTracker tracker;
            if (rates.TryGetValue(symbol, out tracker))
                return tracker;
            else
            {
                tracker = new SymbolRateTracker(symbol);
                rates.Add(symbol, tracker);
                return tracker;
            }
        }

        internal ISymbolInfo GetISymbolInfo(string symbol)
        {
            return symbolsByName.GetOrDefault(symbol);
        }

        internal ISymbolInfo GetSymbolInfoOrThrow(string symbol)
        {
            ISymbolInfo info = symbolsByName.GetOrDefault(symbol);
            if (info == null)
                throw new SymbolNotFoundException(symbol);
            return info;
        }

        /// <summary>
        /// Updates rate for the symbol.
        /// </summary>
        /// <param name="rate">Current symbol rate.</param>
        /// <returns></returns>
        public IEnumerable<IMarginAccountInfo> Update(ISymbolRate rate)
        {
            SymbolRateTracker tracker = GetSymbolTracker(rate.Symbol);
            tracker.Rate = rate;
            RateChanged(rate);
            return RateUpdater.Update(rate.Symbol);
        }

        /// <summary>
        /// Initialize or reinitialize symbols configuration.
        /// Note: Supplied symbol list should be already sorted according priority rules.
        /// </summary>
        /// <param name="symbolList">New symbols configuration to use.</param>
        public void Set(IEnumerable<ISymbolInfo> symbolList)
        {
            this.sortedSymbols = symbolList.ToList();
            this.symbolsByName = symbolList.ToDictionary(smb => smb.Symbol);
            this.SymbolsChanged();
        }

        /// <summary>
        /// Initialize or reinitialize currencies configuration.
        /// Note: Supplied currency list should be already sorted according priority rules.
        /// </summary>
        /// <param name="currencyList">New currecnies configuration to use.</param>
        public void Set(IEnumerable<ICurrencyInfo> currencyList)
        {
            this.sortedCurrencies = currencyList.ToList();
            this.currenciesByName = currencyList.ToDictionary(c => c.Name);
            this.CurrenciesChanged();
        }

        public OrderCalculator GetCalculator(string symbol, string accountCurrency)
        {
            // TO DO: implement cache for calculators
            return new OrderCalculator(symbol, this, accountCurrency);
        }

        public List<ISymbolRate> GetRatesSnapshot()
        {
            return symbolsByName.Select(s => GetRate(s.Key)).Where(r => r != null).ToList();
        }

        public event Action SymbolsChanged = delegate { };
        public event Action CurrenciesChanged = delegate { };
        public event Action<ISymbolRate> RateChanged = delegate { };
    }

    sealed class SymbolRateTracker
    {
        public SymbolRateTracker(string smbName)
        {
            this.Symbol = smbName;
        }

        public string Symbol { get; private set; }

        public ISymbolRate Rate { get; set; }
    }
}
