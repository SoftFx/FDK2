namespace TickTrader.FDK.Extended
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;
    using System.Runtime.ExceptionServices;
    using TickTrader.FDK.Common;
    using TickTrader.FDK.QuoteStore;

    public class PairBarsEnumerator : IEnumerator<PairBar>
    {
        public PairBarsEnumerator(PairBars pairBars, DownloadBarsEnumerator bidEnumerator, DownloadBarsEnumerator askEnumerator)
        {
            this.pairBars = pairBars;
            this.bidEnumerator = bidEnumerator;
            this.askEnumerator = askEnumerator;

            this.bid = null;
            this.ask = null;

            this.current = new PairBar();
        }

        public PairBar Current
        {
            get
            {
                return this.current;
            }
        }

        object IEnumerator.Current
        {
            get
            {
                return this.current;
            }
        }

        public bool MoveNext()
        {
            this.ResetCurrent();
            this.Move();

            return this.UpdateCurrent();
        }

        public void Reset()
        {
            this.bidEnumerator.Dispose();
            this.askEnumerator.Dispose();

            DataFeed.PairBarDownloadContext pairBarDownloadContext = new DataFeed.PairBarDownloadContext();
            pairBarDownloadContext.pairBars_ = pairBars;
            pairBarDownloadContext.bidBarEnumerator_ = null;
            pairBarDownloadContext.askBarEnumerator_ = null;
            pairBarDownloadContext.exception_ = null;
            pairBarDownloadContext.pairBarsEnumerator_ = null;
            pairBarDownloadContext.event_ = new AutoResetEvent(false);

            DataFeed.BarDownloadContext bidBarDownloadContext = new DataFeed.BarDownloadContext();
            bidBarDownloadContext.priceType_ = PriceType.Bid;
            bidBarDownloadContext.pairContext_ = pairBarDownloadContext;

            DataFeed.BarDownloadContext askBarDownloadContext = new DataFeed.BarDownloadContext();
            askBarDownloadContext.priceType_ = PriceType.Ask;
            askBarDownloadContext.pairContext_ = pairBarDownloadContext;

            pairBars.datafeed_.quoteStoreClient_.DownloadBarsAsync
            (
                bidBarDownloadContext,
                pairBars.symbol_,
                PriceType.Bid,
                pairBars.period_,
                pairBars.startTime_,
                pairBars.endTime_
            );

            pairBars.datafeed_.quoteStoreClient_.DownloadBarsAsync
            (
                askBarDownloadContext,
                pairBars.symbol_,
                PriceType.Ask,
                pairBars.period_,
                pairBars.startTime_,
                pairBars.endTime_
            );

            if (!pairBarDownloadContext.event_.WaitOne(pairBars.timeout_))
                throw new Common.TimeoutException("Method call timed out");

            if (pairBarDownloadContext.exception_ != null)
                throw pairBarDownloadContext.exception_;

            bidEnumerator = pairBarDownloadContext.bidBarEnumerator_;
            askEnumerator = pairBarDownloadContext.askBarEnumerator_;

            this.bid = null;
            this.ask = null;

            this.current = new PairBar();
        }

        public void Dispose()
        {
            bidEnumerator.Dispose();
            askEnumerator.Dispose();

            GC.SuppressFinalize(this);
        }

        void ResetCurrent()
        {
            if (this.bid == null)
            {
                this.ask = null;
            }
            else if (this.ask == null)
            {
                this.bid = null;
            }
            else
            {
                var status = DateTime.Compare(this.bid.From, this.ask.From);
                if ((this.pairBars.positive && (status <= 0)) || (!this.pairBars.positive && (status >= 0)))
                {
                    this.bid = null;
                }
                if ((this.pairBars.positive && (status >= 0)) || (!this.pairBars.positive && (status <= 0)))
                {
                    this.ask = null;
                }
            }
        }

        void Move()
        {
            if (this.bid == null)
                this.bid = this.bidEnumerator.Next(pairBars.timeout_);

            if (this.ask == null)
                this.ask = this.askEnumerator.Next(pairBars.timeout_);
        }

        bool UpdateCurrent()
        {
            var bid = this.bid;
            var ask = this.ask;
            if (bid != null && ask != null)
            {
                var status = DateTime.Compare(bid.From, ask.From);
                if ((this.pairBars.positive && (status < 0)) || (!this.pairBars.positive && (status > 0)))
                {
                    // TODO: check this
                    ask = null;
                }
                if ((this.pairBars.positive && (status > 0)) || (!this.pairBars.positive && (status < 0)))
                {
                    // TODO: check this
                    bid = null;
                }
            }
            this.current = new PairBar(bid, ask);
            if (bid == null && ask == null)
            {
                return false;
            }
            return true;
        }

        PairBars pairBars;

        DownloadBarsEnumerator bidEnumerator;
        DownloadBarsEnumerator askEnumerator;

        Bar bid;
        Bar ask;

        PairBar current;
    }
}
