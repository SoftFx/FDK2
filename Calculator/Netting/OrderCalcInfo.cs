using System;
using TickTrader.FDK.Common;

namespace TickTrader.FDK.Calculator.Netting
{
    public sealed class OrderCalcInfo : IOrderCalcInfo
    {
        public OrderCalcInfo()
        {
        }

        public OrderCalcInfo(IOrderCalcInfo originalOrder)
        {
            Symbol = originalOrder.Symbol;
            Side = originalOrder.Side;
            Type = originalOrder.Type;
            Amount = originalOrder.Amount;
            RemainingAmount = originalOrder.RemainingAmount;
            MaxVisibleAmount = originalOrder.MaxVisibleAmount;
            Price = originalOrder.Price;
            StopPrice = originalOrder.StopPrice;
            Slippage = originalOrder.Slippage;
            InitialType = originalOrder.InitialType;
            ImmediateOrCancel = originalOrder.ImmediateOrCancel;
            IsContingent = originalOrder.IsContingent;
        }

        public string Symbol { get; set; }
        public OrderSide Side { get; set; }
        public OrderType Type { get; set; }
        public decimal Amount { get; set; }
        public decimal RemainingAmount { get; set; }
        public decimal? MaxVisibleAmount { get; }
        public bool IsHidden => Extensions.IsHiddenOrder(MaxVisibleAmount);
        public bool IsIceberg => Extensions.IsIcebergOrder(MaxVisibleAmount);
        public decimal? Price { get; set; }
        public decimal? StopPrice { get; set; }
        public decimal? Slippage { get; }
        public OrderType InitialType { get; }
        public bool ImmediateOrCancel { get; set; }
        public bool IsContingent { get; set; }

        public decimal? Margin { get; set; }
    }
}
