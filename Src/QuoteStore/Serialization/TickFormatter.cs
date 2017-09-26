using System;
using System.Collections.Generic;
using System.IO;
using TickTrader.FDK.Common;

namespace TickTrader.FDK.QuoteStore.Serialization
{
    public class TickFormatter
    {
        public TickFormatter(QuoteDepth quoteDepth, Stream stream)
        {
            quoteDepth_ = quoteDepth;
            streamParser_ = new StreamParser(stream);
            bidList_ = new List<QuoteEntry>(100);
            askList_ = new List<QuoteEntry>(100);
        }

        public bool IsEnd
        {
            get { return streamParser_.IsEnd(); }
        }

        public void Deserialize(Quote quote)
        {
            int year, mon, day, hour, min, sec, msec;           
            
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
            streamParser_.ValidateVerbatimChar('.');
            streamParser_.ReadInt32(out msec);

            if (streamParser_.TryValidateVerbatimChar('-'))
            {
                int num;
                streamParser_.ReadInt32(out num);
                quote.Id = string.Format("{0}.{1}.{2} {3}:{4}:{5}.{6}-{7}", year, mon, day, hour, min, sec, msec, num);
            }
            else
            {
                quote.Id = string.Format("{0}.{1}.{2} {3}:{4}:{5}.{6}", year, mon, day, hour, min, sec, msec);
            }

            quote.CreatingTime = new DateTime(year, mon, day, hour, min, sec, msec);

            if (quoteDepth_ == QuoteDepth.Top)
            {
                double ask_price, ask_vol, bid_price, bid_vol;

                streamParser_.ValidateVerbatimChar('\t');
                streamParser_.ReadDouble(out bid_price);
                streamParser_.ValidateVerbatimChar('\t');
                streamParser_.ReadDouble(out bid_vol);
                streamParser_.ValidateVerbatimChar('\t');
                streamParser_.ReadDouble(out ask_price);
                streamParser_.ValidateVerbatimChar('\t');
                streamParser_.ReadDouble(out ask_vol);

                quote.Bids = new QuoteEntry[] { new QuoteEntry { Price = bid_price, Volume = bid_vol } };
                quote.Asks = new QuoteEntry[] { new QuoteEntry { Price = ask_price, Volume = ask_vol } };
            }
            else
            {
                bidList_.Clear();
                askList_.Clear();

                PriceType recType = PriceType.Bid;
                QuoteEntry l2R = new QuoteEntry();

                while (true)
                {
                    if (streamParser_.TryValidateVerbatimChar('\t'))
                    {
                        if (streamParser_.TryValidateVerbatimText("bid"))
                        {
                            recType = PriceType.Bid;
                        }
                        else if (streamParser_.TryValidateVerbatimText("ask"))
                        {
                            recType = PriceType.Ask;
                        }
                        else
                        {
                            double pr;
                            double vl;
                            
                            streamParser_.ReadDouble(out pr);
                            streamParser_.ValidateVerbatimChar('\t');
                            streamParser_.ReadDouble(out vl);

                            l2R.Price = pr;
                            l2R.Volume = vl;

                            if (recType == PriceType.Bid)
                            {
                                bidList_.Add(l2R);                                
                            }
                            else
                                askList_.Add(l2R);                                
                        }
                    }
                    else
                        break;
                }

                bidList_.Reverse();

                // TODO: ?
                quote.Bids = bidList_.ToArray();
                quote.Asks = askList_.ToArray();
            }

            streamParser_.ValidateVerbatimChar('\r');
            streamParser_.ValidateVerbatimChar('\n');
        }

        QuoteDepth quoteDepth_;
        StreamParser streamParser_;
        List<QuoteEntry> bidList_;
        List<QuoteEntry> askList_;
    }
}