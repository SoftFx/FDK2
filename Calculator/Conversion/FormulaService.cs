namespace TickTrader.FDK.Calculator.Conversion
{
    using System;
    using System.Linq;

    sealed class FormulaService : IFormulaService
    {
        readonly ConversionManager conversionManager;
        readonly MarketState market;

        public FormulaService(ConversionManager conversionManager)
        {
            if (conversionManager == null)
                throw new ArgumentNullException("conversionManager");

            this.conversionManager = conversionManager;
            this.market = conversionManager.Market;
        }

        public IConversionFormula BuildMarginFormula(string symbol, string toCurrency)
        {
            ISymbolInfo XY = this.market.GetSymbolInfoOrThrow(symbol);

            string X = XY.MarginCurrency;
            string Y = XY.ProfitCurrency;
            string Z = toCurrency;

            // N 1

            if (X == Z)
                return Formulas.Instance.Direct;

            // N 2

            if (Y == Z)
                return Formulas.Instance.Conversion(GetRate(XY), FxPriceType.Ask).AsFormula();

            // N 3

            ISymbolInfo XZ = GetFromSet(X, Z);

            if (XZ != null)
                return Formulas.Instance.Conversion(GetRate(XZ), FxPriceType.Ask).AsFormula();

            // N 4

            ISymbolInfo ZX = GetFromSet(Z, X);

            if (ZX != null)
                return Formulas.Instance.InverseConversion(GetRate(ZX), FxPriceType.Bid).AsFormula();

            // N 5

            ISymbolInfo YZ = GetFromSet(Y, Z);

            if (YZ != null)
                return Formulas.Instance.Conversion(GetRate(XY), FxPriceType.Ask)
                                        .Then(GetRate(YZ), FxPriceType.Ask);

            // N 6

            ISymbolInfo ZY = GetFromSet(Z, Y);

            if (ZY != null)
                return Formulas.Instance.Conversion(GetRate(XY), FxPriceType.Ask)
                                        .ThenDivide(GetRate(ZY), FxPriceType.Bid);

            foreach (CurrencyInfo curr in market.Currencies)
            {
                string C = curr.Name;

                // N 7

                ISymbolInfo XC = GetFromSet(X, C);
                ISymbolInfo ZC = GetFromSet(Z, C);

                if (XC != null && ZC != null)
                    return Formulas.Instance.Conversion(GetRate(XC), FxPriceType.Ask)
                                            .ThenDivide(GetRate(ZC), FxPriceType.Bid);

                // N 8

                ISymbolInfo CX = GetFromSet(C, X);

                if (CX != null && ZC != null)
                    return Formulas.Instance.InverseConversion(GetRate(CX), FxPriceType.Bid)
                                            .ThenDivide(GetRate(ZC), FxPriceType.Bid);

                // N 9

                ISymbolInfo CZ = GetFromSet(C, Z);

                if (XC != null && CZ != null)
                    return Formulas.Instance.Conversion(GetRate(XC), FxPriceType.Ask)
                                            .Then(GetRate(CZ), FxPriceType.Ask);

                // N 10

                if (CX != null && CZ != null)
                    return Formulas.Instance.InverseConversion(GetRate(CX), FxPriceType.Bid)
                                            .Then(GetRate(CZ), FxPriceType.Ask);

                // N 11

                ISymbolInfo YC = GetFromSet(Y, C);

                if (YC != null && ZC != null)
                    return Formulas.Instance.Conversion(GetRate(YC), FxPriceType.Ask)
                                            .ThenDivideConversion(GetRate(ZC), FxPriceType.Bid)
                                            .Then(GetRate(XY), FxPriceType.Ask);

                // N 12

                ISymbolInfo CY = GetFromSet(C, Y);

                if (CY != null && ZC != null)
                    return Formulas.Instance.InverseConversion(GetRate(CY), FxPriceType.Bid)
                                            .ThenDivideConversion(GetRate(ZC), FxPriceType.Bid)
                                            .Then(GetRate(XY), FxPriceType.Ask);

                // N 13

                if (YC != null && CZ != null)
                    return Formulas.Instance.Conversion(GetRate(YC), FxPriceType.Ask)
                                            .ThenConversion(GetRate(CZ), FxPriceType.Ask)
                                            .Then(GetRate(XY), FxPriceType.Ask);

                // N 14

                if (CY != null && CZ != null)
                    return Formulas.Instance.InverseConversion(GetRate(CY), FxPriceType.Bid)
                                            .ThenConversion(GetRate(CZ), FxPriceType.Ask)
                                            .Then(GetRate(XY), FxPriceType.Ask);
            }

            return Formulas.Instance.CreateError(XY, X, Z);
        }

        public IConversionFormula BuildPositiveProfitFormula(string symbol, string toCurrency)
        {
            return this.BuildProfitFormula(symbol, toCurrency, FxPriceType.Bid, FxPriceType.Ask);
        }

        public IConversionFormula BuildNegativeProfitFormula(string symbol, string toCurrency)
        {
            return this.BuildProfitFormula(symbol, toCurrency, FxPriceType.Ask, FxPriceType.Bid);
        }

        IConversionFormula BuildProfitFormula(string symbol, string toCurrency, FxPriceType price1, FxPriceType price2)
        {
            ISymbolInfo XY = market.GetSymbolInfoOrThrow(symbol);

            string X = XY.MarginCurrency;
            string Y = XY.ProfitCurrency;
            string Z = toCurrency;

            // N 1

            if (Y == Z)
                return Formulas.Instance.Direct;

            // N 2

            if (X == Z)
                return Formulas.Instance.InverseConversion(this.GetRate(XY), price2).AsFormula();

            // N 3

            ISymbolInfo YZ = GetFromSet(Y, Z);

            if (YZ != null)
                return Formulas.Instance.Conversion(GetRate(YZ), price1).AsFormula();

            // N 4

            ISymbolInfo ZY = GetFromSet(Z, Y);

            if (ZY != null)
                return Formulas.Instance.InverseConversion(GetRate(ZY), price2).AsFormula();

            // N 5

            ISymbolInfo ZX = GetFromSet(Z, X);

            if (ZX != null)
                return Formulas.Instance.InverseConversion(GetRate(XY), price2)
                                        .ThenDivide(GetRate(ZX), price2);

            // N 6

            ISymbolInfo XZ = GetFromSet(X, Z);

            if (XZ != null)
                return Formulas.Instance.InverseConversion(GetRate(XY), price2)
                                        .Then(GetRate(XZ), price1);

            foreach (CurrencyInfo curr in this.market.Currencies)
            {
                string C = curr.Name;

                // N 7

                ISymbolInfo YC = GetFromSet(Y, C);
                ISymbolInfo ZC = GetFromSet(Z, C);

                if (YC != null && ZC != null)
                    return Formulas.Instance.Conversion(GetRate(YC), price1)
                                            .ThenDivide(GetRate(ZC), price2);

                // N 8

                ISymbolInfo CY = GetFromSet(C, Y);

                if (CY != null && ZC != null)
                    return Formulas.Instance.InverseConversion(GetRate(CY), price2)
                                            .ThenDivide(GetRate(ZC), price2);

                // N 9

                ISymbolInfo CZ = GetFromSet(C, Z);

                if (YC != null && CZ != null)
                    return Formulas.Instance.Conversion(GetRate(YC), price1)
                                            .Then(GetRate(CZ), price1);

                // N 10

                if (CY != null && CZ != null)
                    return Formulas.Instance.InverseConversion(GetRate(CY), price2)
                                            .Then(GetRate(CZ), price1);
            }

            return Formulas.Instance.CreateError(XY, Y, Z);
        }

        public IConversionFormula BuidPositiveAssetFormula(string currency, string toCurrency)
        {
            return this.BuidAssetFormula(currency, toCurrency, FxPriceType.Bid, FxPriceType.Ask);
        }

        public IConversionFormula BuidNegativeAssetFormula(string currency, string toCurrency)
        {
            return this.BuidAssetFormula(currency, toCurrency, FxPriceType.Ask, FxPriceType.Bid);
        }

        IConversionFormula BuidAssetFormula(string currency, string toCurrency, FxPriceType price1, FxPriceType price2)
        {
            // https://intranet.soft-fx.lv/wiki/pages/viewpage.action?title=Exposure&spaceKey=TIC

            if (currency == null)
                throw new ArgumentNullException("currency");

            if (toCurrency == null)
                throw new ArgumentNullException("toCurrency");

            // if CUR == ZZZ => Asset(ZZZ) = Asset(CUR) and Asset(ZZZ) = Asset(CUR)
            if (currency == toCurrency)
                return Formulas.Instance.Direct;

            // if CUR/ZZZ exists => Asset(ZZZ) = Asset(CUR) * Ask(CUR/ZZZ) and Asset(ZZZ) = Asset(CUR) * Bid(CUR/ZZZ)
            var entry = this.GetFromSet(currency, toCurrency);
            if (entry != null)
                return Formulas.Instance.Conversion(this.GetRate(entry), price1).AsFormula();

            // if ZZZ/CUR exists => Asset(ZZZ) = Asset(CUR) / Bid(ZZZ/CUR) and Asset(ZZZ) = Asset(CUR) / Ask(ZZZ/CUR)
            entry = this.GetFromSet(toCurrency, currency);
            if (entry != null)
                return Formulas.Instance.InverseConversion(this.GetRate(entry), price2).AsFormula();

            var currencies = this.market.Currencies.Select(o => o.Name);

            foreach (var c in currencies)
            {
                // if CUR/CCC and ZZZ/CCC exist => Asset(ZZZ) = Asset(CUR) * Ask(CUR/CCC) / Bid(ZZZ/CCC) and Asset(ZZZ) = Asset(CUR) * Bid(CUR/CCC) / Ask(ZZZ/CCC)
                var curEntry = this.GetFromSet(currency, c);
                var zzzEntry = this.GetFromSet(toCurrency, c);

                if (curEntry != null && zzzEntry != null)
                {
                    return Formulas.Instance.Conversion(this.GetRate(curEntry), price1)
                                            .ThenDivide(this.GetRate(zzzEntry), price2);
                }

                // if CCC/CUR and ZZZ/CCC exist => Asset(ZZZ) = Asset(CUR) / Bid(CCC/CUR) / Bid(ZZZ/CCC) and Asset(ZZZ) = Asset(CUR) / Ask(CCC/CUR) / Ask(ZZZ/CCC)
                curEntry = this.GetFromSet(c, currency);
                zzzEntry = this.GetFromSet(toCurrency, c);
                if (curEntry != null && zzzEntry != null)
                {
                    return Formulas.Instance.InverseConversion(this.GetRate(curEntry), price2)
                                            .ThenDivide(this.GetRate(zzzEntry), price2);
                }

                // if CUR/CCC and CCC/ZZZ exist => Asset(ZZZ) = Asset(CUR) * Ask(CUR/CCC) * Ask(CCC/ZZZ) and Asset(ZZZ) = Asset(CUR) * Bid(CUR/CCC) * Bid(CCC/ZZZ)
                curEntry = this.GetFromSet(currency, c);
                zzzEntry = this.GetFromSet(c, toCurrency);

                if (curEntry != null && zzzEntry != null)
                {
                    return Formulas.Instance.Conversion(this.GetRate(curEntry), price1)
                                            .Then(this.GetRate(zzzEntry), price1);
                }

                // if CCC/CUR and CCC/ZZZ exist => Asset(ZZZ) = Asset(CUR) / Bid(CCC/CUR) * Ask(CCC/ZZZ) and Asset(ZZZ) = Asset(CUR) / Ask(CCC/CUR) * Bid(CCC/ZZZ)
                curEntry = this.GetFromSet(c, currency);
                zzzEntry = this.GetFromSet(c, toCurrency);

                if (curEntry != null && zzzEntry != null)
                {
                    return Formulas.Instance.Conversion(this.GetRate(zzzEntry), price1)
                                            .ThenDivide(this.GetRate(curEntry), price2);
                }
            }

            return Formulas.Instance.CreateError(currency, toCurrency);
        }

        ISymbolInfo GetFromSet(string currency1, string currency2)
        {
            return this.conversionManager.GetFromSet(currency1, currency2);
        }

        SymbolRateTracker GetRate(ISymbolInfo symbol)
        {
            return this.conversionManager.GetRate(symbol);
        }
    }
}
