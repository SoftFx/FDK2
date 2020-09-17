using Snappy;
using SoftFX.Net.Core;
using SoftFX.Net.QuoteFeed;
using System;
using System.IO;
using System.Threading;

namespace TickTrader.FDK.Client
{
    class CompressedStreamHandler
    {
        private MessageData messageData_;
        private Thread readThread_;
        private bool stopPending_;
        private Stream readStream_;
        private SoftFX.Net.QuoteFeed.ClientSessionListener listener_;
        private SoftFX.Net.QuoteFeed.ClientSession session_;
        private WaitStream writeStream_;
        private bool started_;
        private bool snappyStream_;

        private void ReadLoop()
        {
            while (!stopPending_)
            {
                try
                {
                    if (snappyStream_)
                    {
                        readStream_.Read(messageData_.Data, 0, 4);
                        int messageSize = messageData_.Size;
                        if (messageData_.Data.Length < messageSize)
                        {
                            messageData_.Resize(messageSize);
                            readStream_.Read(messageData_.Data, 4, messageSize - 4);
                        }
                        Message message = new Message(Info.QuoteFeed.FindMessageInfo(messageData_.GetInt(4)), messageData_);
                        if (Is.MarketDataUpdate(message))
                            listener_.OnMarketDataUpdate(session_, new MarketDataUpdate(Info.MarketDataUpdate, messageData_));
                    }
                    else
                    {
                        //if (readStream_.CanRead)
                        {
                            int size = (int) readStream_.Length;
                            byte[] block = new byte[size];
                            readStream_.Read(block, 0, size);
                            var decompessed = Snappy.SnappyCodec.Uncompress(block);
                            MessageData md = new MessageData(decompessed);
                            Message msg = new Message(Info.QuoteFeed.FindMessageInfo(md.GetInt(4)), md);
                            if (Is.MarketDataUpdate(msg))
                                listener_.OnMarketDataUpdate(session_, new MarketDataUpdate(Info.MarketDataUpdate, md));
                        }
                    }
                }
                catch(Exception ex)
                {
                    if (!stopPending_)
                        throw;
                }
            }
        }

        public CompressedStreamHandler(SoftFX.Net.QuoteFeed.ClientSession session)
        {
            messageData_ = new MessageData(1024);

            session_ = session;
            stopPending_ = false;
            started_ = false;
        }

        public void Write(byte[] block)
        {
            writeStream_.Write(block, 0, block.Length);
        }

        public void Start(SoftFX.Net.QuoteFeed.ClientSessionListener listener, bool snappyStream = true)
        {
            if (!started_)
            {
                started_ = true;
                listener_ = listener;
                snappyStream_ = snappyStream;
                writeStream_ = new WaitStream();
                if (snappyStream)
                    readStream_ = new SnappyStream(writeStream_, System.IO.Compression.CompressionMode.Decompress);
                else
                    readStream_ = writeStream_;
                readThread_ = new Thread(ReadLoop);
                readThread_.Start();

            }
        }

        public void Stop()
        {
            if (started_)
            {
                started_ = false;
                stopPending_ = true;
                writeStream_.CloseWrite();
                readThread_.Join();
            }
        }
    }
}
