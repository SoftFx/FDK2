using System;
using System.Collections.Generic;
using TickTrader.FDK.Calculator.Conversion;
using TickTrader.FDK.Common;

namespace TickTrader.FDK.Calculator
{
    public static class Extensions
    {
        public static bool HasBid(this ISymbolRate rate)
        {
            return rate.NullableBid.HasValue;
        }

        public static bool HasAsk(this ISymbolRate rate)
        {
            return rate.NullableAsk.HasValue;
        }

        public static bool IsHiddenOrder(decimal? maxVisibleAmount)
        {
            return maxVisibleAmount.HasValue && maxVisibleAmount.Value == 0;
        }

        public static bool IsHiddenOrder(double? maxVisibleAmount)
        {
            return maxVisibleAmount.HasValue && maxVisibleAmount.Value == 0;
        }

        public static bool IsIcebergOrder(decimal? maxVisibleAmount)
        {
            return maxVisibleAmount.HasValue && maxVisibleAmount.Value > 0;
        }

        public static bool IsIcebergOrder(double? maxVisibleAmount)
        {
            return maxVisibleAmount.HasValue && maxVisibleAmount.Value > 0;
        }

        public static OrderSide GetSide(this IPositionModel position)
        {
            return (position.Long.Amount > position.Short.Amount) ? OrderSide.Buy : OrderSide.Sell;
        }

        public static decimal GetAmount(this IPositionModel position)
        {
            return Math.Abs(position.Long.Amount - position.Short.Amount);
        }

        public static decimal GetPrice(this IPositionModel position)
        {
            return (position.Long.Amount > position.Short.Amount) ? position.Long.Price : position.Short.Price;
        }

        public static bool IsEmpty(this IAssetModel assetModel)
        {
            return assetModel.Amount == 0 && assetModel.Margin == 0;
        }

        public static bool IsEmpty(this AssetInfo asset)
        {
            return asset.Balance == 0 && asset.LockedAmount == 0;
        }

        public static AccountEntryStatus ToAccountEntryStatus(this CalcErrorCode code)
        {
            switch (code)
            {
                case CalcErrorCode.None:
                    return AccountEntryStatus.Calculated;
                case CalcErrorCode.OffQuotes:
                    return AccountEntryStatus.CalculatedWithErrors;
                case CalcErrorCode.Misconfiguration:
                    return AccountEntryStatus.Misconfiguration;
                default:
                    return AccountEntryStatus.NotCalculated;
            }
        }

        /// <summary>
        /// Wraps this object instance into an IEnumerable&lt;T&gt;
        /// consisting of a single item.
        /// </summary>
        /// <typeparam name="T"> Type of the object. </typeparam>
        /// <param name="item"> The instance that will be wrapped. </param>
        /// <returns> An IEnumerable&lt;T&gt; consisting of a single item. </returns>
        public static IEnumerable<T> Yield<T>(this T item)
        {
            yield return item;
        }
    }
}
