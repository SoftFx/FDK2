namespace TickTrader.FDK.Extended
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using TickTrader.FDK.Common;
    using TickTrader.FDK.QuoteStore;

    class BarsEnumerator : IEnumerator<Bar>
    {
        internal BarsEnumerator(Bars bars, BarEnumerator barEnumerator)
        {
            bars_ = bars;
            barEnumerator_ = barEnumerator;

            bar_ = new Bar();
        }

        public Bar Current
        {
            get { return bar_; }
        }

        object IEnumerator.Current
        {
            get { return bar_; }
        }

        public bool MoveNext()
        {
            if (bar_ != null)
            {
                bar_ = barEnumerator_.Next(bars_.timeout_);

                return bar_ != null;
            }

            return false;
        }

        public void Reset()
        {
            barEnumerator_.Dispose();

            barEnumerator_ = bars_.datafeed_.quoteStoreClient_.DownloadBars
            (
                Guid.NewGuid().ToString(), 
                bars_.symbol_, 
                bars_.priceType_, 
                bars_.period_, 
                bars_.startTime_, 
                bars_.endTime_, 
                bars_.timeout_
            );

            bar_ = new Bar();
        }

        public void Dispose()
        {
            barEnumerator_.Dispose();

            GC.SuppressFinalize(this);
        }

        Bars bars_;
        BarEnumerator barEnumerator_;
        Bar bar_;
    }
}
