namespace TickTrader.FDK.Extended
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Runtime.ExceptionServices;
    using TickTrader.FDK.Common;
    using TickTrader.FDK.QuoteStore;

    public class PairBarsEnumerator : IEnumerator<PairBar>
    {
        public PairBarsEnumerator(PairBars pairBars, BarEnumerator bidEnumerator, BarEnumerator askEnumerator)
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
            try
            {
                this.bidEnumerator.Dispose();
                this.askEnumerator.Dispose();

                Task<BarEnumerator> bidTask = pairBars.datafeed_.quoteStoreClient_.DownloadBarsAsync
                (
                    Guid.NewGuid().ToString(),
                    pairBars.symbol_,
                    PriceType.Bid,
                    pairBars.period_,
                    pairBars.startTime_,
                    pairBars.endTime_
                );

                Task<BarEnumerator> askTask = pairBars.datafeed_.quoteStoreClient_.DownloadBarsAsync
                (
                    Guid.NewGuid().ToString(),
                    pairBars.symbol_,
                    PriceType.Ask,
                    pairBars.period_,
                    pairBars.startTime_,
                    pairBars.endTime_
                );

                if (!bidTask.Wait(pairBars.timeout_))
                    throw new TimeoutException("Method call timed out");

                if (!askTask.Wait(pairBars.timeout_))
                    throw new TimeoutException("Method call timed out");

                this.bidEnumerator = bidTask.Result;
                this.askEnumerator = askTask.Result;

                this.bid = null;
                this.ask = null;

                this.current = new PairBar();
            }
            catch (AggregateException ex)
            {
                ExceptionDispatchInfo.Capture(ex.Flatten().InnerExceptions[0]).Throw();
            }
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
            try
            {
                Task<Bar> bidTask = null;
                Task<Bar> askTask = null;

                if (this.bid == null)
                    bidTask = this.bidEnumerator.NextAsync();

                if (this.ask == null)
                    askTask = this.askEnumerator.NextAsync();

                if (bidTask != null)
                {
                    if (!bidTask.Wait(pairBars.timeout_))
                        throw new TimeoutException("Method call timed out");
                }

                if (askTask != null)
                {
                    if (!askTask.Wait(pairBars.timeout_))
                        throw new TimeoutException("Method call timed out");
                }

                if (bidTask != null)
                    this.bid = bidTask.Result;

                if (askTask != null)
                    this.ask = askTask.Result;
            }
            catch (AggregateException ex)
            {
                ExceptionDispatchInfo.Capture(ex.Flatten().InnerExceptions[0]).Throw();
            }
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

        BarEnumerator bidEnumerator;
        BarEnumerator askEnumerator;

        Bar bid;
        Bar ask;

        PairBar current;
    }
}
