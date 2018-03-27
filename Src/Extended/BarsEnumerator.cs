namespace TickTrader.FDK.Extended
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using TickTrader.FDK.Common;
    using TickTrader.FDK.QuoteStore;

    public class BarsEnumerator : IEnumerator<Bar>
    {
        internal BarsEnumerator(Bars bars, DownloadBarsEnumerator barEnumerator)
        {
            bars_ = bars;
            barEnumerator_ = barEnumerator;

            bar_ = null;
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
            bar_ = barEnumerator_.Next(bars_.timeout_);

            return bar_ != null;
        }

        public void Reset()
        {
            barEnumerator_.Dispose();

            barEnumerator_ = bars_.datafeed_.quoteStoreClient_.DownloadBars
            (
                bars_.symbol_, 
                bars_.priceType_, 
                bars_.period_, 
                bars_.startTime_, 
                bars_.endTime_, 
                bars_.timeout_
            );

            bar_ = null;
        }

        public void Dispose()
        {
            barEnumerator_.Dispose();

            GC.SuppressFinalize(this);
        }

        Bars bars_;
        DownloadBarsEnumerator barEnumerator_;
        Bar bar_;
    }
}
