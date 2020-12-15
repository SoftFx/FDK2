using System;
using System.Collections.Generic;
using TickTrader.FDK.Common;

namespace TickTrader.FDK.Calculator
{
    /// <summary>
    /// Defines methods and properties for account.
    /// </summary>
    public interface IAccountInfo
    {
        /// <summary>
        /// Account Id.
        /// </summary>
        //long Id { get; }

        /// <summary>
        /// Account type.
        /// </summary>
        AccountType AccountingType { get; }

        /// <summary>
        /// Account orders.
        /// </summary>
        IEnumerable<IOrderModel> Orders { get; }

        /// <summary>
        /// Fired when single order was added.
        /// </summary>
        event Action<IOrderModel> OrderAdded;

        /// <summary>
        /// Fired when multiple orders were added.
        /// </summary>
        event Action<IEnumerable<IOrderModel>> OrdersAdded;

        /// <summary>
        /// Fired when order was removed.
        /// </summary>
        event Action<IOrderModel> OrderRemoved;

        /// <summary>
        /// Fired when order was replaced.
        /// </summary>
        //event Action<IOrderModel2> OrderReplaced;
    }

    /// <summary>
    /// Defines methods and properties for marginal account.
    /// </summary>
    public interface IMarginAccountInfo : IAccountInfo
    {
        /// <summary>
        /// Account balance.
        /// </summary>
        decimal Balance { get; }

        /// <summary>
        /// Account leverage.
        /// </summary>
        int Leverage { get; }

        /// <summary>
        /// Account currency.
        /// </summary>
        string BalanceCurrency { get; }

        /// <summary>
        /// Account positions.
        /// </summary>
        IEnumerable<IPositionModel> Positions { get; }

        /// <summary>
        /// Fired when position changed.
        /// </summary>
        event Action<PositionEssentialsChangeArgs> PositionChanged;
    }

    /// <summary>
    /// Defines methods and properties for cash account.
    /// </summary>
    public interface ICashAccountInfo : IAccountInfo
    {
        /// <summary>
        /// Cash account assets.
        /// </summary>
        IEnumerable<IAssetModel> Assets { get; }

        /// <summary>
        /// Fired when underlying assests list was changed.
        /// </summary>
        event Action<IAssetModel, AssetChangeTypes> AssetsChanged;

        decimal MaxOverdraftAmount { get; }
        string OverdraftCurrency { get; }
    }

    public struct PositionEssentialsChangeArgs
    {
        public PositionEssentialsChangeArgs(IPositionModel position, decimal? oldLongAmount, decimal? oldLongPrice, decimal? oldShortAmount, decimal? oldShortPrice)
        {
            Position = position;
            OldLongAmount = oldLongAmount;
            OldLongPrice = oldLongPrice;
            OldShortAmount = oldShortAmount;
            OldShortPrice = oldShortPrice;
        }

        public IPositionModel Position { get; }
        public decimal? OldLongAmount { get; }
        public decimal? OldLongPrice { get; }
        public decimal? OldShortAmount { get; }
        public decimal? OldShortPrice { get; }
    }
}
