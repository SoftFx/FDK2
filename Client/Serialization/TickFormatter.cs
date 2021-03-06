using System;
using System.Collections.Generic;
using System.IO;
using TickTrader.FDK.Common;

namespace TickTrader.FDK.Client.Serialization
{
    public class TickFormatter
    {
        public TickFormatter(QuoteDepth quoteDepth, Stream stream)
        {
            quoteDepth_ = quoteDepth;
            streamParser_ = new StreamParser(stream);
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

            quote.CreatingTime = new DateTime(year, mon, day, hour, min, sec, msec, DateTimeKind.Utc);

            bool haveIndicativeBid = false;
            bool haveIndicativeAsk = false;

            quote.Bids.Clear();
            quote.Asks.Clear();

            if (quoteDepth_ == QuoteDepth.Top)
            {

                double ask_price, ask_vol, bid_price, bid_vol;

                streamParser_.ValidateVerbatimChar('\t');

                if (!streamParser_.TryValidateVerbatimChar('\t'))
                {
                    streamParser_.ReadDouble(out bid_price);
                    streamParser_.ValidateVerbatimChar('\t');
                    streamParser_.ReadDouble(out bid_vol);
                    if (bid_vol <= 0)
                        haveIndicativeBid = true;
                    quote.Bids.Add(new QuoteEntry { Price = bid_price, Volume = Math.Abs(bid_vol) });
                }

                streamParser_.ValidateVerbatimChar('\t');

                if (!streamParser_.TryValidateVerbatimChar('\t'))
                {
                    streamParser_.ReadDouble(out ask_price);
                    streamParser_.ValidateVerbatimChar('\t');
                    streamParser_.ReadDouble(out ask_vol);
                    if (ask_vol <= 0)
                        haveIndicativeAsk = true;
                    quote.Asks.Add(new QuoteEntry { Price = ask_price, Volume = Math.Abs(ask_vol) });
                }
            }
            else
            {

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

                            if (vl <= 0)
                            {
                                if (recType == PriceType.Bid)
                                    haveIndicativeBid = true;
                                else haveIndicativeAsk = true;
                            }

                            l2R.Price = pr;
                            l2R.Volume = Math.Abs(vl);

                            if (recType == PriceType.Bid)
                            {
                                quote.Bids.Add(l2R);
                            }
                            else
                                quote.Asks.Add(l2R);
                        }
                    }
                    else
                        break;
                }

                quote.Bids.Reverse();
            }

            streamParser_.ValidateVerbatimChar('\r');
            streamParser_.ValidateVerbatimChar('\n');

            if (!haveIndicativeBid && !haveIndicativeAsk)
            {
                quote.IndicativeTick = false;
                quote.TickType = TickTypes.Normal;
            }
            else
            {
                quote.IndicativeTick = true;

                if (haveIndicativeBid && haveIndicativeAsk)
                    quote.TickType = TickTypes.IndicativeBidAsk;
                if (haveIndicativeBid && !haveIndicativeAsk)
                    quote.TickType = TickTypes.IndicativeBid;
                if (!haveIndicativeBid && haveIndicativeAsk)
                    quote.TickType = TickTypes.IndicativeAsk;
            }
        }

        QuoteDepth quoteDepth_;
        StreamParser streamParser_;
    }
}