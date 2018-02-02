namespace TickTrader.FDK.Extended
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using TickTrader.FDK.Common;
    using TickTrader.FDK.QuoteStore;

	public class QuotesSingleSequenceEnumerator : IEnumerator<Quote>
	{
		internal QuotesSingleSequenceEnumerator(QuotesSingleSequence quotesSingleSequence, QuoteEnumerator quoteEnumerator)
		{
			quotesSingleSequence_ = quotesSingleSequence;
            quoteEnumerator_ = quoteEnumerator;

            quote_ = null;
		}

		public Quote Current
		{
			get { return quote_; }
		}

		object IEnumerator.Current
		{
            get { return quote_; }
		}

		public bool MoveNext()
		{
            quote_ = quoteEnumerator_.Next(quotesSingleSequence_.Timeout);

            return quote_ != null;
		}

		public void Reset()
		{
            quoteEnumerator_.Dispose();

            quoteEnumerator_ = quotesSingleSequence_.DataFeed.quoteStoreClient_.DownloadQuotes
            (
                quotesSingleSequence_.Symbol, 
                quotesSingleSequence_.Depth == 1 ? QuoteDepth.Top : QuoteDepth.Level2,
                quotesSingleSequence_.StartTime, 
                quotesSingleSequence_.EndTime, 
                quotesSingleSequence_.Timeout
            );

            quote_ = null;
		}

		public void Dispose()
		{
            quoteEnumerator_.Dispose();

            GC.SuppressFinalize(this);
		}

		QuotesSingleSequence quotesSingleSequence_;
        QuoteEnumerator quoteEnumerator_;

        Quote quote_;
	}
}
