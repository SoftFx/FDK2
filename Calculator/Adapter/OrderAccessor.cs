using System;
using TickTrader.FDK.Common;
using TickTrader.FDK.Extended;

namespace TickTrader.FDK.Calculator.Adapter
{
    public class OrderAccessor : IOrderModel
    {
        private TradeRecord _tradeRecord;
        private ISymbolInfo _symbolInfo;

        public OrderAccessor(TradeRecord record, ISymbolInfo symbolInfo)
        {
            _tradeRecord = record;
            _symbolInfo = symbolInfo;
        }

        public string Symbol => _tradeRecord.Symbol;
        public decimal? Price => (decimal?) _tradeRecord.Price;
        public decimal? StopPrice => (decimal?) _tradeRecord.StopPrice;
        public OrderSide Side => _tradeRecord.Side;
        public OrderType Type => _tradeRecord.Type;
        public decimal RemainingAmount => (decimal) _tradeRecord.Volume;
        public decimal Commission => (decimal) _tradeRecord.Commission;
        public decimal Swap => (decimal) _tradeRecord.Swap;
        public bool IsHidden => Extensions.IsHiddenOrder(_tradeRecord.MaxVisibleVolume);
        public decimal? Slippage => (decimal?) _tradeRecord.Slippage;
        public OrderType InitialType => _tradeRecord.InitialType;
        public bool ImmediateOrCancel => _tradeRecord.ImmediateOrCancel;
        public string OrderId => _tradeRecord.OrderId;
        public ISymbolInfo SymbolInfo => _symbolInfo;

        public TradeRecord TradeRecord => _tradeRecord;

        public OrderCalculator Calculator { get; set; }
        public decimal CashMargin { get; set; }
        public decimal Profit { get; set; }
        public decimal Margin { get; set; }
        public CalcError CalculationError { get; set; }

        public event Action<OrderEssentialsChangeArgs> EssentialsChanged;
        public event Action<OrderPropArgs<decimal>> SwapChanged;
        public event Action<OrderPropArgs<decimal>> CommissionChanged;

        public void Update(TradeRecord newRecord)
        {
            var oldType = Type;
            var oldRemAmount = RemainingAmount;
            var oldPrice = Price;
            var oldStopPrice = StopPrice;
            var oldIsHidden = IsHidden;
            var oldCommission = Commission;
            var oldSwap = Swap;

            _tradeRecord = newRecord;

            EssentialsChanged?.Invoke(new OrderEssentialsChangeArgs(this, oldRemAmount, oldPrice, oldStopPrice, oldType, oldIsHidden));
            if (oldCommission != Commission)
                CommissionChanged?.Invoke(new OrderPropArgs<decimal>(this, oldCommission, Commission));
            if (oldSwap != Swap)
                SwapChanged?.Invoke(new OrderPropArgs<decimal>(this, oldSwap, Swap));
        }
    }
}
