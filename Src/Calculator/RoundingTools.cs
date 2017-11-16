using System;

namespace TickTrader.FDK.Calculator
{
    public sealed class RoundingTools
    {
        readonly decimal roundingDummy;

        public int Precision { get; private set; }

        public RoundingTools(int precision)
        {
            Math.Round(0M, precision);

            this.Precision = precision;
            this.roundingDummy = new decimal(1, 0, 0, false, (byte)precision);
        }

        public decimal Ceil(decimal number)
        {
            if (this.Precision == 0)
                return Math.Ceiling(number);

            var rounded = Math.Round(number, this.Precision);
            if (rounded < number)
                return rounded + this.roundingDummy;
            else
                return rounded;
        }

        public decimal Floor(decimal number)
        {
            if (this.Precision == 0)
                return Math.Floor(number);

            var rounded = Math.Round(number, this.Precision);
            if (rounded > number)
                return rounded - this.roundingDummy;
            else
                return rounded;
        }

        public decimal Trunc(decimal number)
        {
            if (this.Precision == 0)
                return Math.Truncate(number);

            var rounded = Math.Round(number, this.Precision);
            return number >= 0 ? (rounded > number ? rounded - this.roundingDummy : rounded)
                               : (rounded < number ? rounded + this.roundingDummy : rounded);
        }
    }
}
