using System;
using System.Collections.Generic;
using System.Linq;

namespace TickTrader.FDK.Calculator.Conversion
{
    public class ConversionManager
    {
        private readonly MarketStateBase _market;
        private readonly Dictionary<SymbolToCurrencyKey, IConversionFormula> _marginConversions = new Dictionary<SymbolToCurrencyKey, IConversionFormula>();
        private readonly Dictionary<SymbolToCurrencyKey, IConversionFormula> _posProfitConversions = new Dictionary<SymbolToCurrencyKey, IConversionFormula>();
        private readonly Dictionary<SymbolToCurrencyKey, IConversionFormula> _negProfitConversions = new Dictionary<SymbolToCurrencyKey, IConversionFormula>();
        private readonly IDictionary<CurrencyToCurrencyKey, IConversionFormula> _posAssetFormulas = new Dictionary<CurrencyToCurrencyKey, IConversionFormula>();
        private readonly IDictionary<CurrencyToCurrencyKey, IConversionFormula> _negAssetFormulas = new Dictionary<CurrencyToCurrencyKey, IConversionFormula>();
        private readonly Dictionary<CurrencyToCurrencyKey, ISymbolInfo> _convertionSet = new Dictionary<CurrencyToCurrencyKey, ISymbolInfo>();
        private readonly bool _isUsageTrackingEnabled;

        public ConversionManager(MarketStateBase market, bool enableUsageTracking)
        {
            _market = market;
            _isUsageTrackingEnabled = enableUsageTracking;
        }

        internal void Init()
        {
            _convertionSet.Clear();
            _marginConversions.Clear();
            _posProfitConversions.Clear();
            _negProfitConversions.Clear();
            _posAssetFormulas.Clear();
            _negAssetFormulas.Clear();
            FillConversionSet();
        }

        private void FillConversionSet()
        {
            foreach (var symbol in _market.Symbols)
            {
                var key = new CurrencyToCurrencyKey(symbol.MarginCurrency, symbol.ProfitCurrency);
                if (!_convertionSet.ContainsKey(key))
                    _convertionSet.Add(key, symbol);
            }
        }

        public IConversionFormula GetMarginFormula(SymbolMarketNode tracker, string toCurrency)
        {
            if (tracker.SymbolInfo == null)
                return tracker.NoSymbolConversion;

            return _marginConversions.GetOrAdd(new SymbolToCurrencyKey(tracker.SymbolInfo.Symbol, toCurrency),
                () => AdjustUsageTracking(BuildMarginFormula(tracker, toCurrency)));
        }

        public IConversionFormula GetPositiveProfitFormula(SymbolMarketNode tracker, string toCurrency)
        {
            if (tracker.SymbolInfo == null)
                return tracker.NoSymbolConversion;

            return _posProfitConversions.GetOrAdd(new SymbolToCurrencyKey(tracker.SymbolInfo.Symbol, toCurrency),
                () => AdjustUsageTracking(BuildPositiveProfitFormula(tracker, toCurrency)));
        }

        public IConversionFormula GetNegativeProfitFormula(SymbolMarketNode tracker, string toCurrency)
        {
            if (tracker.SymbolInfo == null)
                return tracker.NoSymbolConversion;

            return _negProfitConversions.GetOrAdd(new SymbolToCurrencyKey(tracker.SymbolInfo.Symbol, toCurrency),
                () => AdjustUsageTracking(BuildNegativeProfitFormula(tracker, toCurrency)));
        }

        public IConversionFormula GetPositiveProfitConversion(string symbol, string toCurrency)
        {
            var tracker = _market.GetSymbolNode(symbol, true);
            return GetPositiveProfitFormula(tracker, toCurrency);
        }

        public IConversionFormula GetNegativeProfitConversion(string symbol, string toCurrency)
        {
            var tracker = _market.GetSymbolNode(symbol, true);
            return GetNegativeProfitFormula(tracker, toCurrency);
        }

        public IConversionFormula GetPositiveAssetConversion(string currency, string toCurrency)
        {
            return _posAssetFormulas.GetOrAdd(new CurrencyToCurrencyKey(currency, toCurrency),
                () => AdjustUsageTracking(BuidPositiveAssetFormula(currency, toCurrency)));
        }

        public IConversionFormula GetNegativeAssetConversion(string currency, string toCurrency)
        {
            return _negAssetFormulas.GetOrAdd(new CurrencyToCurrencyKey(currency, toCurrency),
                () => AdjustUsageTracking(BuidNegativeAssetFormula(currency, toCurrency)));
        }

        //private IConversionFormula SetTracking(IConversionFormula formula)
        //{
        //    if (!_market.UsageTrackingEnabled)
        //        formula.AddUsage();
        //    return formula;
        //}

        private IConversionFormula BuildMarginFormula(SymbolMarketNode tracker, string toCurrency)
        {
            ISymbolInfo XY = tracker.SymbolInfo;

            string X = XY.MarginCurrency;
            string Y = XY.ProfitCurrency;
            string Z = toCurrency;

            // N 1

            if (X == Z)
                return FormulaBuilder.Direct();

            // N 2

            if (Y == Z)
                return FormulaBuilder.Conversion(tracker, FxPriceType.Ask);

            // N 3

            ISymbolInfo XZ = GetFromSet(X, Z);

            if (XZ != null)
                return FormulaBuilder.Conversion(GetRate(XZ), FxPriceType.Ask);

            // N 4

            ISymbolInfo ZX = GetFromSet(Z, X);

            if (ZX != null)
                return FormulaBuilder.InverseConversion(GetRate(ZX), FxPriceType.Bid);

            // N 5

            ISymbolInfo YZ = GetFromSet(Y, Z);

            if (YZ != null)
                return FormulaBuilder.Conversion(GetRate(XY), FxPriceType.Ask)
                                     .Then(GetRate(YZ), FxPriceType.Ask);

            // N 6

            ISymbolInfo ZY = GetFromSet(Z, Y);

            if (ZY != null)
                return FormulaBuilder.Conversion(GetRate(XY), FxPriceType.Ask)
                                     .ThenDivide(GetRate(ZY), FxPriceType.Bid);

            foreach (var curr in _market.Currencies)
            {
                string C = curr.Name;

                // N 7

                ISymbolInfo XC = GetFromSet(X, C);
                ISymbolInfo ZC = GetFromSet(Z, C);

                if (XC != null && ZC != null)
                    return FormulaBuilder.Conversion(GetRate(XC), FxPriceType.Ask)
                                         .ThenDivide(GetRate(ZC), FxPriceType.Bid);

                // N 8

                ISymbolInfo CX = GetFromSet(C, X);

                if (CX != null && ZC != null)
                    return FormulaBuilder.InverseConversion(GetRate(CX), FxPriceType.Bid)
                                         .ThenDivide(GetRate(ZC), FxPriceType.Bid);

                // N 9

                ISymbolInfo CZ = GetFromSet(C, Z);

                if (XC != null && CZ != null)
                    return FormulaBuilder.Conversion(GetRate(XC), FxPriceType.Ask)
                                         .Then(GetRate(CZ), FxPriceType.Ask);

                // N 10

                if (CX != null && CZ != null)
                    return FormulaBuilder.InverseConversion(GetRate(CX), FxPriceType.Bid)
                                         .Then(GetRate(CZ), FxPriceType.Ask);

                // N 11

                ISymbolInfo YC = GetFromSet(Y, C);

                if (YC != null && ZC != null)
                    return FormulaBuilder.Conversion(GetRate(YC), FxPriceType.Ask)
                                         .ThenDivide(GetRate(ZC), FxPriceType.Bid)
                                         .Then(GetRate(XY), FxPriceType.Ask);

                // N 12

                ISymbolInfo CY = GetFromSet(C, Y);

                if (CY != null && ZC != null)
                    return FormulaBuilder.InverseConversion(GetRate(CY), FxPriceType.Bid)
                                         .ThenDivide(GetRate(ZC), FxPriceType.Bid)
                                         .Then(GetRate(XY), FxPriceType.Ask);

                // N 13

                if (YC != null && CZ != null)
                    return FormulaBuilder.Conversion(GetRate(YC), FxPriceType.Ask)
                                         .Then(GetRate(CZ), FxPriceType.Ask)
                                         .Then(GetRate(XY), FxPriceType.Ask);

                // N 14

                if (CY != null && CZ != null)
                    return FormulaBuilder.InverseConversion(GetRate(CY), FxPriceType.Bid)
                                         .Then(GetRate(CZ), FxPriceType.Ask)
                                         .Then(GetRate(XY), FxPriceType.Ask);
            }

            return FormulaBuilder.Error(XY, X, Z);
        }

        private IConversionFormula BuildPositiveProfitFormula(SymbolMarketNode tracker, string toCurrency)
        {
            return BuildProfitFormula(tracker, toCurrency, FxPriceType.Bid, FxPriceType.Ask);
        }

        private IConversionFormula BuildNegativeProfitFormula(SymbolMarketNode tracker, string toCurrency)
        {
            return BuildProfitFormula(tracker, toCurrency, FxPriceType.Ask, FxPriceType.Bid);
        }

        private IConversionFormula BuildProfitFormula(SymbolMarketNode tracker, string toCurrency, FxPriceType price1, FxPriceType price2)
        {
            ISymbolInfo XY = tracker.SymbolInfo;

            string X = XY.MarginCurrency;
            string Y = XY.ProfitCurrency;
            string Z = toCurrency;

            // N 1

            if (Y == Z)
                return FormulaBuilder.Direct();

            // N 2

            if (X == Z)
                return FormulaBuilder.InverseConversion(this.GetRate(XY), price2);

            // N 3

            ISymbolInfo YZ = GetFromSet(Y, Z);

            if (YZ != null)
                return FormulaBuilder.Conversion(GetRate(YZ), price1);

            // N 4

            ISymbolInfo ZY = GetFromSet(Z, Y);

            if (ZY != null)
                return FormulaBuilder.InverseConversion(GetRate(ZY), price2);

            // N 5

            ISymbolInfo ZX = GetFromSet(Z, X);

            if (ZX != null)
                return FormulaBuilder.InverseConversion(GetRate(XY), price2)
                                     .ThenDivide(GetRate(ZX), price2);

            // N 6

            ISymbolInfo XZ = GetFromSet(X, Z);

            if (XZ != null)
                return FormulaBuilder.InverseConversion(GetRate(XY), price2)
                                     .Then(GetRate(XZ), price1);

            foreach (var curr in this._market.Currencies)
            {
                string C = curr.Name;

                // N 7

                ISymbolInfo YC = GetFromSet(Y, C);
                ISymbolInfo ZC = GetFromSet(Z, C);

                if (YC != null && ZC != null)
                    return FormulaBuilder.Conversion(GetRate(YC), price1)
                                         .ThenDivide(GetRate(ZC), price2);

                // N 8

                ISymbolInfo CY = GetFromSet(C, Y);

                if (CY != null && ZC != null)
                    return FormulaBuilder.InverseConversion(GetRate(CY), price2)
                                         .ThenDivide(GetRate(ZC), price2);

                // N 9

                ISymbolInfo CZ = GetFromSet(C, Z);

                if (YC != null && CZ != null)
                    return FormulaBuilder.Conversion(GetRate(YC), price1)
                                         .Then(GetRate(CZ), price1);

                // N 10

                if (CY != null && CZ != null)
                    return FormulaBuilder.InverseConversion(GetRate(CY), price2)
                                         .Then(GetRate(CZ), price1);
            }

            return FormulaBuilder.Error(XY, Y, Z);
        }

        private IConversionFormula BuidPositiveAssetFormula(string currency, string toCurrency)
        {
            return this.BuidAssetFormula(currency, toCurrency, FxPriceType.Bid, FxPriceType.Ask);
        }

        private IConversionFormula BuidNegativeAssetFormula(string currency, string toCurrency)
        {
            return this.BuidAssetFormula(currency, toCurrency, FxPriceType.Ask, FxPriceType.Bid);
        }

        private IConversionFormula BuidAssetFormula(string currency, string toCurrency, FxPriceType price1, FxPriceType price2)
        {
            // https://intranet.soft-fx.lv/wiki/pages/viewpage.action?title=Exposure&spaceKey=TIC

            if (currency == null)
                throw new ArgumentNullException("currency");

            if (toCurrency == null)
                throw new ArgumentNullException("toCurrency");

            // if CUR == ZZZ => Asset(ZZZ) = Asset(CUR) and Asset(ZZZ) = Asset(CUR)
            if (currency == toCurrency)
                return FormulaBuilder.Direct();

            // if CUR/ZZZ exists => Asset(ZZZ) = Asset(CUR) * Ask(CUR/ZZZ) and Asset(ZZZ) = Asset(CUR) * Bid(CUR/ZZZ)
            var entry = this.GetFromSet(currency, toCurrency);
            if (entry != null)
                return FormulaBuilder.Conversion(this.GetRate(entry), price1);

            // if ZZZ/CUR exists => Asset(ZZZ) = Asset(CUR) / Bid(ZZZ/CUR) and Asset(ZZZ) = Asset(CUR) / Ask(ZZZ/CUR)
            entry = this.GetFromSet(toCurrency, currency);
            if (entry != null)
                return FormulaBuilder.InverseConversion(this.GetRate(entry), price2);

            var currencies = _market.Currencies.Select(o => o.Name);

            foreach (var c in currencies)
            {
                // if CUR/CCC and ZZZ/CCC exist => Asset(ZZZ) = Asset(CUR) * Ask(CUR/CCC) / Bid(ZZZ/CCC) and Asset(ZZZ) = Asset(CUR) * Bid(CUR/CCC) / Ask(ZZZ/CCC)
                var curEntry = this.GetFromSet(currency, c);
                var zzzEntry = this.GetFromSet(toCurrency, c);

                if (curEntry != null && zzzEntry != null)
                {
                    return FormulaBuilder.Conversion(this.GetRate(curEntry), price1)
                                         .ThenDivide(this.GetRate(zzzEntry), price2);
                }

                // if CCC/CUR and ZZZ/CCC exist => Asset(ZZZ) = Asset(CUR) / Bid(CCC/CUR) / Bid(ZZZ/CCC) and Asset(ZZZ) = Asset(CUR) / Ask(CCC/CUR) / Ask(ZZZ/CCC)
                curEntry = this.GetFromSet(c, currency);
                zzzEntry = this.GetFromSet(toCurrency, c);
                if (curEntry != null && zzzEntry != null)
                {
                    return FormulaBuilder.InverseConversion(this.GetRate(curEntry), price2)
                                         .ThenDivide(this.GetRate(zzzEntry), price2);
                }

                // if CUR/CCC and CCC/ZZZ exist => Asset(ZZZ) = Asset(CUR) * Ask(CUR/CCC) * Ask(CCC/ZZZ) and Asset(ZZZ) = Asset(CUR) * Bid(CUR/CCC) * Bid(CCC/ZZZ)
                curEntry = this.GetFromSet(currency, c);
                zzzEntry = this.GetFromSet(c, toCurrency);

                if (curEntry != null && zzzEntry != null)
                {
                    return FormulaBuilder.Conversion(this.GetRate(curEntry), price1)
                                         .Then(this.GetRate(zzzEntry), price1);
                }

                // if CCC/CUR and CCC/ZZZ exist => Asset(ZZZ) = Asset(CUR) / Bid(CCC/CUR) * Ask(CCC/ZZZ) and Asset(ZZZ) = Asset(CUR) / Ask(CCC/CUR) * Bid(CCC/ZZZ)
                curEntry = this.GetFromSet(c, currency);
                zzzEntry = this.GetFromSet(c, toCurrency);

                if (curEntry != null && zzzEntry != null)
                {
                    return FormulaBuilder.Conversion(this.GetRate(zzzEntry), price1)
                                         .ThenDivide(this.GetRate(curEntry), price2);
                }
            }

            return FormulaBuilder.Error(currency, toCurrency);
        }

        private ISymbolInfo GetFromSet(string currency1, string currency2)
        {
            return _convertionSet.GetOrDefault(new CurrencyToCurrencyKey(currency1, currency2));
        }

        private SymbolMarketNode GetRate(ISymbolInfo symbol)
        {
            return _market.GetSymbolNode(symbol.Symbol, true);
        }

        private IConversionFormula AdjustUsageTracking(IConversionFormula formula)
        {
            if (!_isUsageTrackingEnabled)
            {
                var usageAwareFormula = formula as UsageAwareFormula;
                usageAwareFormula?.SetPermanentUsage();
            }
            return formula;
        }
    }
}
