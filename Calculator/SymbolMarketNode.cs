using System;
using System.Collections.Generic;
using System.Linq;
using TickTrader.FDK.Calculator.Adapter;
using TickTrader.FDK.Calculator.Conversion;

namespace TickTrader.FDK.Calculator
{
    public class SymbolMarketNode
    {
        public SymbolMarketNode(string smbName, ISymbolInfo smb)
        {
            SymbolInfo = smb;
            Rate = new SymbolRate(smbName, new PriceEntry());

            NoBidError = new OffQuoteError(false, smbName, FxPriceType.Bid);
            NoAskError = new OffQuoteError(false, smbName, FxPriceType.Ask);
            NoBidCrossError = new OffQuoteError(true, smbName, FxPriceType.Bid);
            NoAskCrossError = new OffQuoteError(true, smbName, FxPriceType.Ask);
            NoSymbolError = new SymbolNotFoundMisconfigError(smbName);
            NoSymbolConversion = new ConversionError(NoSymbolError);
        }

        internal bool IsEnabled => SymbolInfo != null;

        public ISymbolInfo SymbolInfo { get; private set; }
        public ISymbolRate Rate { get; private set; }

        public decimal Ask { get; private set; }
        public decimal Bid { get; private set; } 

        public bool HasBid { get; private set; }
        public bool HasAsk { get; private set; }

        public CalcError NoBidError { get; }
        public CalcError NoAskError { get; }
        public CalcError NoBidCrossError { get; }
        public CalcError NoAskCrossError { get; }
        public CalcError NoSymbolError { get; }
        public IConversionFormula NoSymbolConversion { get; }

        internal void Update(ISymbolInfo smbInfo)
        {
            SymbolInfo = smbInfo;
        }

        internal void UpdateRate(ISymbolRate rate)
        {
            Rate = rate;

            HasBid = rate.HasBid();
            if (HasBid)
                Bid = rate.Bid;
            else
                Bid = 0;

            HasAsk = rate.HasAsk();
            if (HasAsk)
                Ask = rate.Ask;
            else
                Ask = 0;

            RateChanging?.Invoke();
        }

        internal void FireChanged()
        {
            if (IsEnabled)
                RateChanged?.Invoke();
        }

        public decimal GetBidOrError(out CalcError error)
        {
            if (HasBid)
            {
                error = null;
                return Bid;
            }

            error = NoBidError;
            return 0;
        }

        public decimal GetAskOrError(out CalcError error)
        {
            if (HasAsk)
            {
                error = null;
                return Ask;
            }

            error = NoAskError;
            return 0;
        }

        internal event Action RateChanging;

        public event Action RateChanged;
    }
}
