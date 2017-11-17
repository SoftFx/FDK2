using System;
using System.IO;
using TickTrader.FDK.Common;

namespace TickTrader.FDK.QuoteStore.Serialization
{
    public class BarFormatter
    {
        public BarFormatter(Stream stream)
        {
            streamParser_ = new StreamParser(stream);
        }

        public bool IsEnd
        {
            get { return streamParser_.IsEnd(); }
        }

        public void Deserialize(BarPeriod barPeriod, Bar bar)
        {
            int year, mon, day, hour, min, sec;            
            
            streamParser_.ReadInt32(out year);
            streamParser_.ValidateVerbatimChar('.');
            streamParser_.ReadInt32(out mon);
            streamParser_.ValidateVerbatimChar('.');
            streamParser_.ReadInt32(out day);
            streamParser_.ValidateVerbatimChar(' ');
            streamParser_.ReadInt32(out hour);
            streamParser_.ValidateVerbatimChar(':');
            streamParser_.ReadInt32(out min);
            streamParser_.ValidateVerbatimChar(':');
            streamParser_.ReadInt32(out sec);

            streamParser_.ValidateVerbatimChar('\t');

            var dt = new DateTime(year, mon, day, hour, min, sec);
            double lo, hi, op, cl;
            double vol;

            streamParser_.ReadDouble(out op);
            streamParser_.ValidateVerbatimChar('\t');
            streamParser_.ReadDouble(out hi);
            streamParser_.ValidateVerbatimChar('\t');
            streamParser_.ReadDouble(out lo);
            streamParser_.ValidateVerbatimChar('\t');
            streamParser_.ReadDouble(out cl);
            streamParser_.ValidateVerbatimChar('\t');
            streamParser_.ReadDouble(out vol);

            bar.From = dt;
            bar.To = dt + barPeriod;
            bar.Open = op;
            bar.High = hi;
            bar.Low = lo;
            bar.Close = cl;
            bar.Volume = vol;

            streamParser_.ValidateVerbatimChar('\r');
            streamParser_.ValidateVerbatimChar('\n');
        }

        StreamParser streamParser_;
    }
}