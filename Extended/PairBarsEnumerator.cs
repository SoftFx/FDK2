﻿namespace TickTrader.FDK.Extended
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;
    using System.Runtime.ExceptionServices;
    using TickTrader.FDK.Common;
    using TickTrader.FDK.Client;

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

        public DateTime AvailFrom
        {
            get { return bidEnumerator.AvailFrom < askEnumerator.AvailTo ? bidEnumerator.AvailFrom : askEnumerator.AvailFrom; }
        }

        public DateTime AvailTo
        {
            get { return bidEnumerator.AvailTo > askEnumerator.AvailTo ? bidEnumerator.AvailTo : askEnumerator.AvailTo; }
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
            bidEnumerator.Close();
            askEnumerator.Close();

            bidEnumerator = new DownloadBarsEnumerator(pairBars.datafeed_.quoteStoreClient_);
            askEnumerator = new DownloadBarsEnumerator(pairBars.datafeed_.quoteStoreClient_);

            DataFeed.BarDownloadContext bidBarDownloadContext = new DataFeed.BarDownloadContext();
            bidBarDownloadContext.barEnumerator_ = bidEnumerator;

            pairBars.datafeed_.quoteStoreClient_.DownloadBarsAsync
            (
                bidBarDownloadContext,
                pairBars.symbol_,
                PriceType.Bid,
                pairBars.period_,
                pairBars.startTime_,
                pairBars.endTime_
            );

            try
            {
                DataFeed.BarDownloadContext askBarDownloadContext = new DataFeed.BarDownloadContext();
                askBarDownloadContext.barEnumerator_ = askEnumerator;

                pairBars.datafeed_.quoteStoreClient_.DownloadBarsAsync
                (
                    askBarDownloadContext,
                    pairBars.symbol_,
                    PriceType.Ask,
                    pairBars.period_,
                    pairBars.startTime_,
                    pairBars.endTime_
                );

                try
                {
                    bidEnumerator.Begin(pairBars.timeout_);
                    askEnumerator.Begin(pairBars.timeout_);

                    bid = null;
                    ask = null;

                    current = new PairBar();
                }
                catch
                {
                    askEnumerator.Close();

                    throw;
                }
            }
            catch
            {
                bidEnumerator.Close();

                throw;
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
                if ((this.pairBars.isPositiveTimeRange && (status <= 0)) || (!this.pairBars.isPositiveTimeRange && (status >= 0)))
                {
                    this.bid = null;
                }
                if ((this.pairBars.isPositiveTimeRange && (status >= 0)) || (!this.pairBars.isPositiveTimeRange && (status <= 0)))
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
                if ((this.pairBars.isPositiveTimeRange && (status < 0)) || (!this.pairBars.isPositiveTimeRange && (status > 0)))
                {
                    ask = null;
                }
                if ((this.pairBars.isPositiveTimeRange && (status > 0)) || (!this.pairBars.isPositiveTimeRange && (status < 0)))
                {
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
