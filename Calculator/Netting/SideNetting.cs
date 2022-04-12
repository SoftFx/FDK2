using System;
using TickTrader.FDK.Common;

namespace TickTrader.FDK.Calculator.Netting
{
    public class SideNetting
    {
        private readonly SymbolNetting _parent;
        private IOrderNetting _positions;
        private IOrderNetting _limitOrders;
        private IOrderNetting _stopOrders;
        private IOrderNetting _hiddendOrders;
        private decimal _netPosAmount;
        private decimal _netPosPrice;
        private OrderCalculator _calc;

        public SideNetting(SymbolNetting parent, OrderSide side)
        {
            _parent = parent;
            Side = side;
            NettingType = parent.NettingType;
        }

        public OrderSide Side { get; }
        public bool IsEmpty { get; private set; } = true;
        public decimal Margin { get; private set; }
        public decimal TotalAmount { get; private set; }

        public decimal NetPosMargin { get; private set; }
        public decimal NetPosProfit { get; private set; }
        public int NetErrorCount { get; private set; }
        public decimal NetPosAmount => _netPosAmount;
        internal NettingCalculationTypes NettingType { get; }
        public decimal MarketAmount => _positions.Amount + _netPosAmount;

        internal StatsChange Recalculate()
        {
            var result = new StatsChange(0, 0, 0, false);

            var pos = _positions;
            var limits = _limitOrders;
            var stops = _stopOrders;
            var hiddens = _hiddendOrders;

            if (pos != null)
            {
                result += pos.Recalculate();
                NetPosMargin += result.MarginDelta;
            }
            else
                NetPosMargin = 0;

            if (limits != null)
                result += limits.Recalculate();

            if (stops != null)
                result += stops.Recalculate();

            if (hiddens != null)
                result += hiddens.Recalculate();

            Margin += result.MarginDelta;
            return result;
        }

        internal void AddOrder(IOrderModel order)
        {
            //Skip Contingent orders
            if (order.IsContingent)
                return;

            //Count++;
            order.EssentialsChanged += Order_EssentialsChanged;
            //order.PriceChanged += Order_PriceChanged;
            var netting = GetNetting(order.Type, order.IsHidden);
            var change = netting.AddOrder(order, order.RemainingAmount, order.Price);
            UpdateStats(change);
        }

        internal void AddOrderWithoutCalculation(IOrderModel order)
        {
            //Skip Contingent orders
            if (order.IsContingent)
                return;

            //Count++;
            order.EssentialsChanged += Order_EssentialsChanged;
            //order.PriceChanged += Order_PriceChanged;
            var netting = GetNetting(order.Type, order.IsHidden);
            netting.AddOrderWithoutCalculation(order, order.RemainingAmount, order.Price);
        }

        internal void RemoveOrder(IOrderModel order)
        {
            //Skip Contingent orders
            if (order.IsContingent)
                return;

            //Count--;
            order.EssentialsChanged -= Order_EssentialsChanged;
            //order.PriceChanged -= Order_PriceChanged;
            var netting = GetNetting(order.Type, order.IsHidden);
            var change = netting.RemoveOrder(order, order.RemainingAmount, order.Price);
            if (netting.IsEmpty)
                RemoveNetting(netting);
            UpdateStats(change);
        }

        internal void UpdatePosition(IPositionSide pos)
        {
            var positions = GetOrCreatePositions();

            positions.RemovePositionWithoutCalculation(_netPosAmount, _netPosPrice);

            _netPosAmount = pos.Amount;
            _netPosPrice = pos.Price;

            positions.AddPositionWithoutCalculation(pos, _netPosAmount, _netPosPrice);

            var change = _positions.Recalculate();
            NetPosMargin += change.MarginDelta;
            UpdateStats(change);

            if (_positions.IsEmpty)
                RemovePositions();
        }

        internal void SetCalculator(OrderCalculator calc)
        {
            _calc = calc;
            if (_positions != null)
                _positions.Calculator = calc;
            if (_limitOrders != null)
                _limitOrders.Calculator = calc;
            if (_stopOrders != null)
                _stopOrders.Calculator = calc;
            if (_hiddendOrders != null)
                _hiddendOrders.Calculator = calc;
        }

        internal CalcError GetWorstError()
        {
            CalcError result = null;

            if (_positions != null)
                result = CalcError.GetWorst(result, _positions.GetWorstError());
            if (_limitOrders != null)
                result = CalcError.GetWorst(result, _limitOrders.GetWorstError());
            if (_stopOrders != null)
                result = CalcError.GetWorst(result, _stopOrders.GetWorstError());
            if (_hiddendOrders != null)
                result = CalcError.GetWorst(result, _hiddendOrders.GetWorstError());

            return result;
        }

        private void Order_EssentialsChanged(OrderEssentialsChangeArgs args)
        {
            var c1 = GetNetting(args.OldType, args.OldIsHidden).RemoveOrder(args.Order, args.OldRemAmount, args.OldPrice);
            var c2 = GetNetting(args.Order.Type, args.Order.IsHidden).AddOrder(args.Order, args.Order.RemainingAmount, args.Order.Price);
            var cSum = c1 + c2;
            UpdateStats(cSum);
        }

        private IOrderNetting GetNetting(OrderType orderType, bool isHidden)
        {
            switch (orderType)
            {
                case OrderType.Limit:
                    {
                        if (isHidden)
                            return GetOrCreateHiddenOrders();
                        else
                            return GetOrCreateLimitOrders();
                    }
                case OrderType.Stop: return GetOrCreateStopOrders();
                case OrderType.StopLimit: return GetOrCreateStopOrders(); // StopLimit orders have same calculation logic as Stop orders
                case OrderType.Market: return GetOrCreatePositions(); // Market orders have same calculation logic as positions
                case OrderType.Position: return GetOrCreatePositions();
            }

            throw new Exception("Unsupported Order Type: " + orderType);
        }

        private void RemoveNetting(IOrderNetting netting)
        {
            if (_positions == netting)
                RemovePositions();
            else if (_stopOrders == netting)
            {
                _stopOrders.AmountChanged -= OnAmountChanged;
                _stopOrders = null;
            }
            else if (_limitOrders == netting)
            {
                _limitOrders.AmountChanged -= OnAmountChanged;
                _limitOrders = null;
            }
            else if (_hiddendOrders == netting)
            {
                _hiddendOrders.AmountChanged -= OnAmountChanged;
                _hiddendOrders = null;
            }
        }

        private void RemovePositions()
        {
            _positions.AmountChanged -= OnAmountChanged;
            _positions = null;
        }

        private void UpdateStats(StatsChange change)
        {
            Margin += change.MarginDelta;
            _parent.OnStatsChange(change);
        }

        private void OnAmountChanged(decimal delta)
        {
            TotalAmount += delta;
            IsEmpty = TotalAmount == 0;
        }

        private IOrderNetting GetOrCreatePositions()
        {
            if (_positions == null)
            {
                _positions = CreateNetting(OrderType.Position, Side, false);
                _positions.AmountChanged += OnAmountChanged;
                _positions.Calculator = _calc;
            }
            return _positions;
        }

        private IOrderNetting GetOrCreateLimitOrders()
        {
            if (_limitOrders == null)
            {
                _limitOrders = CreateNetting(OrderType.Limit, Side, false);
                _limitOrders.AmountChanged += OnAmountChanged;
                _limitOrders.Calculator = _calc;
            }
            return _limitOrders;
        }

        private IOrderNetting GetOrCreateStopOrders()
        {
            if (_stopOrders == null)
            {
                _stopOrders = CreateNetting(OrderType.Stop, Side, false);
                _stopOrders.AmountChanged += OnAmountChanged;
                _stopOrders.Calculator = _calc;
            }
            return _stopOrders;
        }

        private IOrderNetting GetOrCreateHiddenOrders()
        {
            if (_hiddendOrders == null)
            {
                _hiddendOrders = CreateNetting(OrderType.Limit, Side, true);
                _hiddendOrders.AmountChanged += OnAmountChanged;
                _hiddendOrders.Calculator = _calc;
            }
            return _hiddendOrders;
        }

        private IOrderNetting CreateNetting(OrderType type, OrderSide side, bool isHidden)
        {
            if (_parent.NettingType == NettingCalculationTypes.OneByOne)
                return new EachOrderNetting(_parent.AccInfo, side);
            else if (_parent.NettingType == NettingCalculationTypes.Optimized)
                return new BacthOrderNetting(_parent.AccInfo, type, side, isHidden);
            else
                throw new Exception("Unsupported netting type: " + _parent.NettingType);
        }
    }
}
