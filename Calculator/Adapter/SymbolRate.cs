using TickTrader.FDK.Common;

namespace TickTrader.FDK.Calculator.Adapter
{
    sealed class SymbolRate : ISymbolRate
    {
        readonly PriceEntry price;
        readonly string symbol;

        public SymbolRate(string symbol, PriceEntry price)
        {
            this.price = price;
            this.symbol = symbol;
        }

        public decimal Ask
        {
            get { return (decimal)this.price.Ask; }
        }

        public decimal Bid
        {
            get { return (decimal)this.price.Bid; }
        }

        public decimal? NullableAsk
        {
            get { return this.price.Ask; }
        }

        public decimal? NullableBid
        {
            get { return this.price.Bid; }
        }

        public string Symbol
        {
            get { return this.symbol; }
        }

        public TickTypes TickType
        {
            get { return this.price.TickType; }
        }
    }
}
