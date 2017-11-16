namespace TickTrader.FDK.Calculator.Adapter
{
    using System;
    using System.Collections.Generic;
    using TickTrader.FDK.Common;
    using TickTrader.FDK.Extended;

    static class CalculatorConvert
    {
        public static Calculator.CurrencyInfo ToCurrencyInfo(CurrencyEntry currency, int priority)
        {
            return new Calculator.CurrencyInfo
            {
                Name = currency.Name,
                Precision = currency.Precision,
                SortOrder = currency.SortOrder,
                Id = (short)currency.GetHashCode()
            };
        }

        public static Calculator.SymbolInfo ToSymbolInfo(SymbolEntry symbol)
        {
            return new Calculator.SymbolInfo
            {
                Symbol = symbol.Symbol,
                MarginCurrency = symbol.MarginCurrency,
                MarginCurrencyId = (short)symbol.MarginCurrency.GetHashCode(),
                ProfitCurrency = symbol.ProfitCurrency,
                ProfitCurrencyId = (short)symbol.ProfitCurrency.GetHashCode(),
                ContractSizeFractional = symbol.ContractSize,
                MarginFactorFractional = symbol.MarginFactor,
                MarginHedged = symbol.Hedging,
                SortOrder = symbol.SortOrder,
                MarginMode = ToMarginCalculationModes(symbol.MarginCalcMode),
                StopOrderMarginReduction = symbol.StopOrderMarginReduction ?? 1,
                HiddenLimitOrderMarginReduction = symbol.HiddenLimitOrderMarginReduction ?? 1
            };
        }

        static MarginCalculationModes ToMarginCalculationModes(MarginCalcMode mode)
        {
            return (MarginCalculationModes)mode;
        }

        public static ISymbolRate ToSymbolRate(KeyValuePair<string, PriceEntry> price)
        {
            return new SymbolRate(price.Key, price.Value);
        }

        public static AccountingTypes ToAccountingTypes(AccountType type)
        {
            switch (type)
            {
                case AccountType.Gross:
                    return AccountingTypes.Gross;
                case AccountType.Net:
                    return AccountingTypes.Net;
                case AccountType.Cash:
                    return AccountingTypes.Cash;
            }

            throw new ArgumentException("type");
        }

        public static IMarginAccountInfo ToMarginAccountInfo(AccountEntry account)
        {
            return new MarginAccountInfo(account);
        }

        public static IOrderModel ToCalculatorOrder(TradeEntry trade)
        {
            return new CalculatorOrder(trade);
        }

        public static OrderSides ToOrderSides(OrderSide side)
        {
            switch (side)
            {
                case OrderSide.Buy:
                    return OrderSides.Buy;
                case OrderSide.Sell:
                    return OrderSides.Sell;
            }

            throw new ArgumentException("side");
        }

        public static OrderTypes ToOrderTypes(OrderType type)
        {
            switch (type)
            {
                case OrderType.Market:
                    return OrderTypes.Market;
                case OrderType.Position:
                    return OrderTypes.Position;
                case OrderType.Limit:
                    return OrderTypes.Limit;
                case OrderType.Stop:
                    return OrderTypes.Stop;
                case OrderType.StopLimit:
                    return OrderTypes.StopLimit;
            }

            throw new ArgumentException("type");
        }

        public static IAssetModel ToAssetModel(Asset asset)
        {
            return new CalculatorAsset(asset);
        }
    }
}
