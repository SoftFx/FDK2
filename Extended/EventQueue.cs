namespace TickTrader.FDK.Extended
{
    using System;
    using System.Threading;

    class EventQueue : IDisposable
    {
        public EventQueue(int capacity)
        {
            mutex_ = new object();
            events_ = new Event[capacity];
            nonEmptyEvent_ = new AutoResetEvent(false);
            notFullEvent_ = new AutoResetEvent(false);
            opened_ = false;
        }

        public void Open()
        {
            lock (mutex_)
            {
                size_ = 0;
                beginIndex_ = 0;
                endIndex_ = 0;

                nonEmptyEvent_.Reset();
                notFullEvent_.Reset();

                opened_ = true;
            }
        }

        public void Close()
        {
            lock (mutex_)
            {
                opened_ = false;

                nonEmptyEvent_.Set();
                notFullEvent_.Set();
            }
        }

        public int Size
        {
            get
            {
                lock (mutex_)
                {
                    return size_;
                }
            }
        }

        public void PushEvent(EventArgs eventArgs)
        {
            while (true)
            {
                lock (mutex_)
                {
                    if (size_ < events_.Length)
                    {
                        // TODO: clone ?
                        events_[endIndex_].Args = eventArgs;
                        endIndex_ = (endIndex_ + 1) % events_.Length;
                        ++ size_;

                        nonEmptyEvent_.Set();

                        break;
                    }

                    if (! opened_)
                        return;
                }

                notFullEvent_.WaitOne();
            }
        }

        public bool PopEvent(out EventArgs eventArgs)
        {
            while (true)
            {
                lock (mutex_)
                {
                    if (size_ > 0)
                    {
                        eventArgs = events_[beginIndex_].Args;
                        beginIndex_ = (beginIndex_ + 1) % events_.Length;
                        -- size_;

                        notFullEvent_.Set();

                        return true;
                    }

                    if (! opened_)
                    {
                        eventArgs = null;

                        return false;
                    }
                }

                nonEmptyEvent_.WaitOne();
            }
        }

        public void Dispose()
        {
            Close();

            nonEmptyEvent_.Dispose();
            notFullEvent_.Dispose();
        }

        struct Event
        {
            public EventArgs Args;
        }

        object mutex_;
        Event[] events_;
        int size_;
        int beginIndex_;
        int endIndex_;
        AutoResetEvent nonEmptyEvent_;
        AutoResetEvent notFullEvent_;
        bool opened_;
    }
}