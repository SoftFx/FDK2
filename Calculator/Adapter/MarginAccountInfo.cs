﻿namespace TickTrader.FDK.Calculator.Adapter
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    sealed class MarginAccountInfo : IMarginAccountInfo
    {
        readonly AccountEntry entry;

        public MarginAccountInfo(AccountEntry entry)
        {
            this.entry = entry;
        }

        public void LogInfo(string message)
        {
        }

        public void LogWarn(string message)
        {
        }

        public void LogError(string message)
        {
        }

        public decimal Balance
        {
            get { return (decimal)this.entry.Balance; }
        }

        public string BalanceCurrency
        {
            get { return this.entry.Currency; }
        }

        public int Leverage
        {
            get { return (int)this.entry.Leverage; }
        }

        public AccountingTypes AccountingType
        {
            get { return CalculatorConvert.ToAccountingTypes(this.entry.Type); }
        }

        public long Id
        {
            get { return this.entry.GetHashCode(); }
        }

        public IEnumerable<IOrderModel> Orders
        {
            get { return this.entry.Trades.Select(CalculatorConvert.ToCalculatorOrder); }
        }

        #region Events

        public event Action<IOrderModel> OrderAdded
        {
            add { }
            remove { }
        }

        public event Action<IOrderModel> OrderRemoved
        {
            add { }
            remove { }
        }

        public event Action<IOrderModel> OrderReplaced
        {
            add { }
            remove { }
        }

        public event Action<IEnumerable<IOrderModel>> OrdersAdded
        {
            add { }
            remove { }
        }

        #endregion

        public event Action<IPositionModel, PositionChageTypes> PositionChanged
        {
            add { }
            remove { }
        }

        public IEnumerable<IPositionModel> Positions
        {
            get { return Enumerable.Empty<IPositionModel>(); }
        }
    }
}
