using System;
using System.Collections.Generic;
using TickTrader.FDK.Common;

namespace TickTrader.FDK.Calculator.Netting
{
    public class SymbolNetting : IDisposable
    {
        private MarketStateBase _market;
        private readonly AccountCalculator _parent;
        private readonly bool _isAutoUpdateEnabled;
        private OrderCalculator _calc;
        private decimal _hedgeFormulPart;
        private decimal _netPosSwap;
        private decimal _netPosComm;

        public SymbolNetting(string symbol, AccountCalculator parent, MarketStateBase market, bool autoUpdate)
        {
            Symbol = symbol;
            _parent = parent;
            _market = market;
            _isAutoUpdateEnabled = autoUpdate;
            AccInfo = parent.Info;
            CreateCalculator();
        }

        internal SymbolMarketNode Tracker { get; private set; }
        internal NettingCalculationTypes NettingType => _parent.NettingType;
        public IMarginAccountInfo AccInfo { get; }
        //public int Count { get; private set; }
        public bool IsEmpty => Sell == null && Buy == null;
        public string Symbol { get; }

        public SideNetting Buy { get; private set; }
        public SideNetting Sell { get; private set; }
        public decimal Margin { get; private set; }
        public OrderCalculator Calc => _calc;

        internal void Recalculate()
        {
            StatsChange change;

            var bCopy = Buy;
            var sCopy = Sell;

            if (bCopy != null)
            {
                if (sCopy != null)
                    change = bCopy.Recalculate() + sCopy.Recalculate();
                else
                    change = bCopy.Recalculate();
            }
            else if (sCopy != null)
                change = sCopy.Recalculate();
            else
                return;

            OnStatsChange(change);
        }

        internal void AddOrder(IOrderModel order)
        {
            order.Calculator = _calc;
            GetSideNetting(order).AddOrder(order);
        }

        internal void AddOrderWithoutCalculation(IOrderModel order)
        {
            order.Calculator = _calc;
            GetSideNetting(order).AddOrderWithoutCalculation(order);
        }

        internal void RemoveOrder(IOrderModel order)
        {
            if (order.Side == OrderSide.Buy)
            {
                var buy = GetOrAddBuy();
                buy.RemoveOrder(order);
                RemoveBuyIfEmtpy();
            }
            else
            {
                var sell = GetOrAddSell();
                sell.RemoveOrder(order);
                RemoveSellIfEmtpy();
            }
        }

        internal void UpdatePosition(IPositionModel pos, out decimal swapDelta, out decimal commDelta)
        {
            pos.Calculator = Calc;

            swapDelta = pos.Swap - _netPosSwap;
            commDelta = pos.Commission - _netPosComm;

            _netPosSwap = pos.Swap;
            _netPosComm = pos.Commission;

            GetOrAddBuy().UpdatePosition(pos.Long);
            GetOrAddSell().UpdatePosition(pos.Short);

            RemoveBuyIfEmtpy();
            RemoveSellIfEmtpy();
        }

        internal CalcError GetWorstError()
        {
            if (Buy != null)
            {
                if (Sell != null)
                    return CalcError.GetWorst(Buy.GetWorstError(), Sell.GetWorstError());
                else
                    return Buy.GetWorstError();
            }
            else if (Sell != null)
                return Sell.GetWorstError();

            return null;
        }

        public void Dispose()
        {
            _calc?.RemoveUsage();
            _calc = null;
            if (Tracker != null)
                Tracker.RateChanged -= Recalculate;
        }

        private SideNetting GetSideNetting(IOrderModel order)
        {
            if (order.Side == OrderSide.Buy)
                return GetOrAddBuy();
            else
                return GetOrAddSell();
        }

        private void UpdateMargin()
        {
            var buyMargin = Buy?.Margin ?? 0;
            var sellMargin = Sell?.Margin ?? 0;
            Margin = Math.Max(sellMargin, buyMargin) + _hedgeFormulPart * Math.Min(sellMargin, buyMargin);
        }

        internal void OnStatsChange(StatsChange args)
        {
            var oldMargin = Margin;
            UpdateMargin();
            var delta = Margin - oldMargin;
            _parent.Calc_StatsChanged(new StatsChange(delta, args.ProfitDelta, args.ErrorDelta, args.IsErrorChanged));
        }

        internal void CreateCalculator()
        {
            if (Tracker != null)
            {
                Tracker.RateChanged -= Recalculate;
                Tracker = null;
            }

            _calc?.RemoveUsage();
            _calc = _market.GetCalculator(Symbol, AccInfo.BalanceCurrency);
            _calc.AddUsage();

            var hedge = _calc.SymbolInfo?.MarginHedged ?? 0.5;
            _hedgeFormulPart = (decimal)(2 * hedge - 1);

            Buy?.SetCalculator(_calc);
            Sell?.SetCalculator(_calc);

            if (_isAutoUpdateEnabled)
            {
                Tracker = _market.GetSymbolNode(Symbol, true); // ?? throw new Exception("Market state lacks symbol:" + Symbol);
                Tracker.RateChanged += Recalculate;
            }
        }

        private SideNetting GetOrAddBuy()
        {
            if (Buy == null)
            {
                Buy = new SideNetting(this, OrderSide.Buy);
                Buy.SetCalculator(Calc);
            }
            return Buy;
        }

        private void RemoveBuyIfEmtpy()
        {
            if (Buy.IsEmpty)
                Buy = null;
        }

        private SideNetting GetOrAddSell()
        {
            if (Sell == null)
            {
                Sell = new SideNetting(this, OrderSide.Sell);
                Sell.SetCalculator(Calc);
            }
            return Sell;
        }

        private void RemoveSellIfEmtpy()
        {
            if (Sell.IsEmpty)
                Sell = null;
        }
    }
}
