using Snappy;
using SoftFX.Net.Core;
using SoftFX.Net.QuoteFeed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TickTrader.FDK.Client
{
    class CompressedStreamHandler
    {
        private MessageData messageData_;
        private Thread readThread_;
        private bool stopPending_;
        private SnappyStream readStream_;
        private SoftFX.Net.QuoteFeed.ClientSessionListener listener_;
        private SoftFX.Net.QuoteFeed.ClientSession session_;
        private WaitStream writeStream_;
        private bool started_;
        private void ReadLoop()
        {
            while (!stopPending_)
            {
                try
                {
                    readStream_.Read(messageData_.Data, 0, 4);
                    int messageSize = messageData_.Size;
                    if (messageData_.Data.Length < messageSize)
                        messageData_.Resize(messageSize);
                    readStream_.Read(messageData_.Data, 4, messageSize - 4);
                    Message message = new Message(Info.QuoteFeed.FindMessageInfo(messageData_.GetInt(4)), messageData_);
                    if (Is.MarketDataUpdate(message))
                        listener_.OnMarketDataUpdate(session_, new MarketDataUpdate(Info.MarketDataUpdate, messageData_));
                }
                catch(Exception)
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

        public void Start(SoftFX.Net.QuoteFeed.ClientSessionListener listener)
        {
            if (!started_)
            {
                started_ = true;
                listener_ = listener;
                writeStream_ = new WaitStream();
                readStream_ = new SnappyStream(writeStream_, System.IO.Compression.CompressionMode.Decompress);
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
