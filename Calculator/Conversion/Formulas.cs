using System;

namespace TickTrader.FDK.Calculator.Conversion
{
    internal class NoConvertion : IConversionFormula
    {
        public decimal Value => 1;
        public CalcError Error => null;

        public event Action ValChanged { add { } remove { } }

        public void AddUsage() { }
        public void RemoveUsage() { }
    }

    internal abstract class UsageAwareFormula : IConversionFormula
    {
        private decimal _val;
        private bool _isPermanentUsage;

        public decimal Value
        {
            get { return _val; }
            set
            {
                if (_val != value)
                {
                    _val = value;
                    ValChanged?.Invoke();
                }
            }
        }

        public int UsageCount { get; private set; }
        public CalcError Error { get; protected set; }
        public event Action ValChanged;

        protected abstract void Attach();
        protected abstract void Deattach();

        public void SetPermanentUsage()
        {
            _isPermanentUsage = true;
            Attach();
        }

        public void AddUsage()
        {
            if (!_isPermanentUsage && UsageCount <= 0)
                Attach();

            UsageCount++;
        }

        public void RemoveUsage()
        {
            UsageCount--;

            if (!_isPermanentUsage && UsageCount <= 0)
                Deattach();
        }
    }

    internal abstract class ComplexConversion : UsageAwareFormula
    {
        public IConversionFormula SrcFromula { get; set; }
        public SymbolMarketNode SrcSymbol { get; set; }

        protected abstract decimal GetValue();

        protected override void Attach()
        {
            if (SrcFromula != null)
            {
                SrcFromula.AddUsage();
                SrcFromula.ValChanged += SrcFromula_ValChanged;
            }

            SrcSymbol.RateChanging += SrcSymbol_Changed;

            Value = GetValue();
        }

        protected override void Deattach()
        {
            if (SrcFromula != null)
            {
                SrcFromula.RemoveUsage();
                SrcFromula.ValChanged -= SrcFromula_ValChanged;
            }

            SrcSymbol.RateChanging -= SrcSymbol_Changed;
        }

        private void SrcSymbol_Changed()
        {
            Value = GetValue();
        }

        private void SrcFromula_ValChanged()
        {
            Value = GetValue();
        }

        protected bool CheckBid()
        {
            if (!SrcSymbol.HasBid)
            {
                Error = SrcSymbol.NoBidCrossError;
                return false;
            }
            return true;
        }

        protected bool CheckAsk()
        {
            if (!SrcSymbol.HasAsk)
            {
                Error = SrcSymbol.NoAskCrossError;
                return false;
            }
            return true;
        }

        protected bool CheckSrcFormula()
        {
            var error = SrcFromula.Error;

            if (error != null)
            {
                Error = error;
                return false;
            }

            return true;
        }
    }

    internal class GetAsk : ComplexConversion
    {
        protected override decimal GetValue()
        {
            if (CheckAsk())
            {
                Error = null;
                return SrcSymbol.Ask;
            }
            return 0;
        }
    }

    internal class GetBid : ComplexConversion
    {
        protected override decimal GetValue()
        {
            if (CheckBid())
            {
                Error = null;
                return SrcSymbol.Bid;
            }
            return 0;
        }
    }

    internal class GetInvertedAsk : ComplexConversion
    {
        protected override decimal GetValue()
        {
            if (CheckAsk())
            {
                Error = null;
                return 1 / SrcSymbol.Ask;
            }
            return 0;
        }
    }

    internal class GetInvertedBid : ComplexConversion
    {
        protected override decimal GetValue()
        {
            if (CheckBid())
            {
                Error = null;
                return 1 / SrcSymbol.Bid;
            }
            return 0;
        }
    }

    internal class MultByBid : ComplexConversion
    {
        protected override decimal GetValue()
        {
            if (CheckBid() && CheckSrcFormula())
            {
                Error = null;
                return SrcFromula.Value * SrcSymbol.Bid;
            }
            return 0;
        }
    }

    internal class MultByAsk : ComplexConversion
    {
        protected override decimal GetValue()
        {
            if (CheckAsk() && CheckSrcFormula())
            {
                Error = null;
                return SrcFromula.Value * SrcSymbol.Ask;
            }
            return 0;
        }
    }

    internal class DivByBid : ComplexConversion
    {
        protected override decimal GetValue()
        {
            if (CheckBid() && CheckSrcFormula())
            {
                Error = null;
                return SrcFromula.Value / SrcSymbol.Bid;
            }
            return 0;
        }
    }

    internal class DivByAsk : ComplexConversion
    {
        protected override decimal GetValue()
        {
            if (CheckAsk() && CheckSrcFormula())
            {
                Error = null;
                return SrcFromula.Value / SrcSymbol.Ask;
            }
            return 0;
        }
    }

    internal class ConversionError : IConversionFormula
    {
        public ConversionError(CalcError error)
        {
            Error = error;
        }

        public decimal Value
        {
            get { throw new InvalidOperationException("Conversion error: " + Error.Code + "!"); }
        }

        public CalcError Error { get; }
        public event Action ValChanged { add { } remove { } }

        public void AddUsage() { }
        public void RemoveUsage() { }
    }
}

