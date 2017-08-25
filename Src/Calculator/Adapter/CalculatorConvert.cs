﻿namespace TickTrader.FDK.Calculator.Adapter
{
    using System;
    using System.Collections.Generic;
    using TickTrader.BusinessLogic;
    using TickTrader.BusinessObjects;
    using TickTrader.Common.Business;
    using TickTrader.FDK.Common;

    static class CalculatorConvert
    {
        public static BusinessObjects.CurrencyInfo ToCurrencyInfo(string currency, int priority)
        {
            return new BusinessObjects.CurrencyInfo
            {
                Name = currency,
                SortOrder = priority,
                Id = (short)currency.GetHashCode()
            };
        }

        public static BusinessObjects.SymbolInfo ToSymbolInfo(SymbolEntry symbol)
        {
            return new BusinessObjects.SymbolInfo
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
                MarginMode = ToMarginCalculationModes(symbol.MarginCalcMode)
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

        public static OrderSides ToOrderSides(TradeRecordSide side)
        {
            switch (side)
            {
                case TradeRecordSide.Buy:
                    return OrderSides.Buy;
                case TradeRecordSide.Sell:
                    return OrderSides.Sell;
            }

            throw new ArgumentException("side");
        }

        public static OrderTypes ToOrderTypes(TradeRecordType type)
        {
            switch (type)
            {
                case TradeRecordType.Market:
                    return OrderTypes.Market;
                case TradeRecordType.Position:
                    return OrderTypes.Position;
                case TradeRecordType.Limit:
                    return OrderTypes.Limit;
                case TradeRecordType.Stop:
                    return OrderTypes.Stop;
                case TradeRecordType.IoC:
                    return OrderTypes.Limit;
                case TradeRecordType.MarketWithSlippage:
                    return OrderTypes.Limit;
                case TradeRecordType.StopLimit:
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