using System;
using System.Collections.Generic;
using System.Linq;
using TickTrader.FDK.Calculator.Conversion;

namespace TickTrader.FDK.Calculator
{
    public abstract class MarketStateBase
    {
        private readonly Dictionary<Tuple<string, string>, OrderCalculator> _orderCalculators = new Dictionary<Tuple<string, string>, OrderCalculator>();
        private readonly Dictionary<string, ICurrencyInfo> _currenciesByName = new Dictionary<string, ICurrencyInfo>();

        public MarketStateBase(bool usageTracking = true)
        {
            ConversionMap = new ConversionManager(this, usageTracking);
            UsageTrackingEnabled = usageTracking;
        }

        public IEnumerable<ISymbolInfo> Symbols { get; private set; }
        public IEnumerable<ICurrencyInfo> Currencies { get; private set; }
        public ConversionManager ConversionMap { get; }

        internal bool IsInitialized { get; private set; }
        internal bool UsageTrackingEnabled { get; }

        public void Init(IEnumerable<ISymbolInfo> symbolList, IEnumerable<ICurrencyInfo> currencyList)
        {
            Currencies = currencyList.ToList();

            _currenciesByName.Clear();
            foreach (var currency in currencyList)
                _currenciesByName[currency.Name] = currency;


            Symbols = symbolList.ToList();
            UpsertSymbols(symbolList);

            ConversionMap.Init();

            InitCalculators();

            IsInitialized = true;
            Initialized?.Invoke();
        }

        //protected abstract void InitNodes();

        protected abstract void UpsertNode(ISymbolInfo smbInfo);
        protected abstract void DisableNode(ISymbolInfo smbInfo);
        protected abstract IEnumerable<ISymbolInfo> ListEnabledNodes();

        internal abstract SymbolMarketNode GetSymbolNode(string smb, bool addIfMissing);

        public ICurrencyInfo GetCurrencyOrThrow(string name)
        {
            var result = _currenciesByName.GetOrDefault(name);
            if (result == null)
                throw new MarketConfigurationException("Currency Not Found: " + name);
            return result;
        }

        internal event Action Initialized;

        public OrderCalculator GetCalculator(string symbol, string balanceCurrency)
        {
            var key = Tuple.Create(symbol, balanceCurrency);

            OrderCalculator calculator;
            if (!_orderCalculators.TryGetValue(key, out calculator))
            {
                var tracker = GetSymbolNode(symbol, true);
                calculator = new OrderCalculator(symbol, tracker, ConversionMap, balanceCurrency);
                _orderCalculators.Add(key, calculator);
                OnCalculatorAdded(calculator);
            }
            return calculator;
        }

        protected virtual void OnCalculatorAdded(OrderCalculator calculator) { }

        private void UpsertSymbols(IEnumerable<ISymbolInfo> symbolList)
        {
            var newSymbols = symbolList.ToDictionary(s => s.Symbol);

            // remove nodes

            foreach (var existingSmb in ListEnabledNodes())
            {
                if (!newSymbols.ContainsKey(existingSmb.Symbol))
                    DisableNode(existingSmb);
            }

            // upsert nodes

            foreach (var smb in symbolList)
                UpsertNode(smb);
        }

        private void InitCalculators()
        {
            foreach (var calc in _orderCalculators.Values)
                calc.Init();
        }
    }

}
