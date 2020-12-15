using System;
using TickTrader.FDK.Common;

namespace TickTrader.FDK.Calculator.Netting
{
    public sealed class OrderLightClone : IOrderCalcInfo
    {
        public OrderLightClone()
        {
        }

        public OrderLightClone(IOrderCalcInfo originalOrder)
        {
            Symbol = originalOrder.Symbol;
            Side = originalOrder.Side;
            Type = originalOrder.Type;
            RemainingAmount = originalOrder.RemainingAmount;
            IsHidden = originalOrder.IsHidden;
            Price = originalOrder.Price;
            StopPrice = originalOrder.StopPrice;
            Slippage = originalOrder.Slippage;
            InitialType = originalOrder.InitialType;
            ImmediateOrCancel = originalOrder.ImmediateOrCancel;
        }

        public string Symbol { get; set; }
        public OrderSide Side { get; set; }
        public OrderType Type { get; set; }
        public decimal RemainingAmount { get; set; }
        public bool IsHidden { get; set; }
        public decimal? Price { get; set; }
        public decimal? StopPrice { get; set; }
        public decimal? Slippage { get; }
        public OrderType InitialType { get; }
        public bool ImmediateOrCancel { get; set; }

        public decimal? Margin { get; set; }
    }
}
