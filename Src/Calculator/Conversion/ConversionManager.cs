namespace TickTrader.FDK.Calculator.Conversion
{
    using System;
    using System.Collections.Generic;

    public class ConversionManager
    {
        readonly IFormulaService formulaSevice;

        readonly IDictionary<SymbolToCurrencyKey, IConversionFormula> marginFormulas = new Dictionary<SymbolToCurrencyKey, IConversionFormula>();
        readonly IDictionary<SymbolToCurrencyKey, IConversionFormula> posProfitFormulas = new Dictionary<SymbolToCurrencyKey, IConversionFormula>();
        readonly IDictionary<SymbolToCurrencyKey, IConversionFormula> negProfitFormulas = new Dictionary<SymbolToCurrencyKey, IConversionFormula>();
        readonly IDictionary<CurrencyToCurrencyKey, IConversionFormula> posAssetFormulas = new Dictionary<CurrencyToCurrencyKey, IConversionFormula>();
        readonly IDictionary<CurrencyToCurrencyKey, IConversionFormula> negAssetFormulas = new Dictionary<CurrencyToCurrencyKey, IConversionFormula>();
        readonly IDictionary<CurrencyToCurrencyKey, ISymbolInfo> conversionSet = new Dictionary<CurrencyToCurrencyKey, ISymbolInfo>();

        internal MarketState Market { get; private set; }

        public ConversionManager(MarketState market)
        {
            if (market == null)
                throw new ArgumentNullException("market");

            this.Market = market;

            this.formulaSevice = new FormulaService(this);

            this.Update();

            this.Market.SymbolsChanged += this.Update;
            this.Market.CurrenciesChanged += this.Update;
        }

        void Update()
        {
            this.marginFormulas.Clear();
            this.posProfitFormulas.Clear();
            this.negProfitFormulas.Clear();
            this.posAssetFormulas.Clear();
            this.negAssetFormulas.Clear();
            FillConversionSet(this.conversionSet, this.Market.Symbols);
        }

        static IDictionary<CurrencyToCurrencyKey, ISymbolInfo> FillConversionSet(IDictionary<CurrencyToCurrencyKey, ISymbolInfo> set, IEnumerable<ISymbolInfo> symbols)
        {
            set.Clear();
 
            foreach (ISymbolInfo symbol in symbols)
            {
                CurrencyToCurrencyKey key = new CurrencyToCurrencyKey(symbol.MarginCurrency, symbol.ProfitCurrency);
                if (!set.ContainsKey(key))
                    set.Add(key, symbol);
            }

            return set;
        }

        public IConversionFormula GetMarginConversion(string symbol, string toCurrency)
        {
            return this.marginFormulas.GetOrAdd(new SymbolToCurrencyKey(symbol, toCurrency), () => this.formulaSevice.BuildMarginFormula(symbol, toCurrency));
        }

        public IConversionFormula GetPositiveProfitConversion(string symbol, string toCurrency)
        {
            return this.posProfitFormulas.GetOrAdd(new SymbolToCurrencyKey(symbol, toCurrency), () => this.formulaSevice.BuildPositiveProfitFormula(symbol, toCurrency));
        }

        public IConversionFormula GetNegativeProfitConversion(string symbol, string toCurrency)
        {
            return this.negProfitFormulas.GetOrAdd(new SymbolToCurrencyKey(symbol, toCurrency), () => this.formulaSevice.BuildNegativeProfitFormula(symbol, toCurrency));
        }

        public IConversionFormula GetPositiveAssetConversion(string currency, string toCurrency)
        {
            return this.posAssetFormulas.GetOrAdd(new CurrencyToCurrencyKey(currency, toCurrency), () => this.formulaSevice.BuidPositiveAssetFormula(currency, toCurrency));
        }

        public IConversionFormula GetNegativeAssetConversion(string currency, string toCurrency)
        {
            return this.negAssetFormulas.GetOrAdd(new CurrencyToCurrencyKey(currency, toCurrency), () => this.formulaSevice.BuidNegativeAssetFormula(currency, toCurrency));
        }

        internal ISymbolInfo GetFromSet(string currency1, string currency2)
        {
            return this.conversionSet.GetOrDefault(new CurrencyToCurrencyKey(currency1, currency2));
        }

        internal SymbolRateTracker GetRate(ISymbolInfo symbol)
        {
            return this.Market.GetSymbolTracker(symbol.Symbol);
        }
    }
}
