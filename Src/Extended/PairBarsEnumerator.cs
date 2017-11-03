namespace TickTrader.FDK.Extended
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using TickTrader.FDK.Common;

    class PairBarsEnumerator : IEnumerator<PairBar>
    {
        public PairBarsEnumerator(PairBars pairBars, IEnumerator<Bar> bidEnumerator, IEnumerator<Bar> askEnumerator)
        {
            this.pairBars = pairBars;
            this.bidEnumerator = bidEnumerator;
            this.askEnumerator = askEnumerator;

            // TODO: check this
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
            this.bidEnumerator = pairBars.bids.GetEnumerator();
            this.askEnumerator = pairBars.asks.GetEnumerator();
            this.bid = null;
            this.ask = null;

            // TODO: check this
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
            {
                if (this.bidEnumerator.MoveNext())
                {
                    this.bid = this.bidEnumerator.Current;
                }
            }
            if (this.ask == null)
            {
                if (this.askEnumerator.MoveNext())
                {
                    this.ask = this.askEnumerator.Current;
                }
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

        IEnumerator<Bar> bidEnumerator;
        IEnumerator<Bar> askEnumerator;

        Bar bid;
        Bar ask;

        PairBar current;
    }
}
