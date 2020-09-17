using System;
using TickTrader.FDK.Common;

namespace TickTrader.FDK.Calculator.Adapter
{
    public class PositionAccessor : IPositionModel
    {
        private Position _position;
        private int _leverage;
        private ISymbolInfo _symbolInfo;
        private readonly PositionSideModel _buy = new PositionSideModel();
        private readonly PositionSideModel _sell = new PositionSideModel();

        public PositionAccessor(Position position, int leverage, ISymbolInfo symbolInfo)
        {
            _position = position;
            _leverage = leverage;
            _symbolInfo = symbolInfo;
            _buy.Update((decimal)position.BuyAmount, (decimal)(position.BuyPrice ?? 0));
            _sell.Update((decimal)position.SellAmount, (decimal)(position.SellPrice ?? 0));
        }

        public string Symbol => _position.Symbol;
        public decimal Commission => (decimal)_position.Commission;
        public decimal Swap => (decimal)_position.Swap;
        public IPositionSide Long => _buy;
        public IPositionSide Short => _sell;
        public OrderCalculator Calculator { get; set; }
        public ISymbolInfo SymbolInfo => _symbolInfo;

        public bool IsEmpty => _buy.Amount <= 0 && _sell.Amount <= 0;
        public decimal Margin => _buy.Margin + _sell.Margin;
        public decimal Profit => _buy.Profit + _sell.Profit;
        public Position Position => _position;


        public void Update(Position position)
        {
            _position = position;
            _buy.Update((decimal)position.BuyAmount, (decimal)(position.BuyPrice ?? 0));
            _sell.Update((decimal)position.SellAmount, (decimal)(position.SellPrice ?? 0));
        }

        public class PositionSideModel : IPositionSide
        {
            public PositionSideModel()
            {
            }

            internal void Update(decimal amount, decimal price)
            {
                Amount = amount;
                Price = price;
            }

            public decimal Amount { get; private set; }
            public decimal Price { get; private set; }
            public decimal Margin { get; set; }
            public decimal Profit { get; set; }
        }

        private decimal? CalculateMargin()
        {
            var calc = Calculator;
            if (calc != null)
            {
                CalcError error;
                var margin = calc.CalculateMargin(this, _leverage, out error);
                if (error != null && error.Code != CalcErrorCode.None)
                    return null;
                return margin;
            }
            return null;
        }

        private decimal? CalculateProfit()
        {
            var calc = Calculator;
            if (calc != null)
            {
                CalcError error;
                var prof = calc.CalculateProfit(this, out error);
                if (error != null && error.Code != CalcErrorCode.None)
                    return null;
                return prof;
            }
            return null;
        }
    }
}
