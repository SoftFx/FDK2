using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TickTrader.FDK.Client
{
    public class WaitStream : Stream
    {

        private long _availableDataSize;
        private long _writtenDataSize;
        private long _readPosition;
        private int _currentBucketPosition;
        private Queue<byte[]> _dataQueue;
        private object _readWriteLock;
        private long _maxDataSize;
        private int _maxDataSizeMultiplier;

        private int _waitedReadCount;
        private bool _writeClosed;


        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length
        {
            get { throw new NotImplementedException(); }
        }

        public override long Position
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public String CloseWriteReason { get; set; }

        public WaitStream(int maxDataSizeMultiplier = 8)
        {
            _maxDataSizeMultiplier = maxDataSizeMultiplier;
            _dataQueue = new Queue<byte[]>();
            _readWriteLock = new object();
            CloseWriteReason = null;
        }

        public override void Close()
        {
            CloseWrite();
            base.Close();
        }

        public void CloseWrite()
        {
            lock (_readWriteLock)
            {
                _writeClosed = true;
                Monitor.Pulse(_readWriteLock);
            }
        }

        public override void Flush()
        {
            Console.WriteLine("Flushed!");
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            try
            {
                if (offset + count > buffer.Length)
                    throw new ArgumentException("Buffer size must be at least " + offset + count);

                lock (_readWriteLock)
                {
                    if (_availableDataSize < count && !_writeClosed)
                    {
                        _waitedReadCount = count;
                        Monitor.Wait(_readWriteLock, int.MaxValue);
                    }
                    else
                        _waitedReadCount = -1;

                    var readed = ReadCount(buffer, offset, count);

                    _availableDataSize -= readed;

                    if (_availableDataSize < _maxDataSize)
                        Monitor.Pulse(_readWriteLock);

                    if (readed != count && !_writeClosed)
                        throw new TimeoutException("Read timeout has been expired");
                    return readed;
                }
            }
            catch (Exception ex)
            {
                int a = 1;
                return 0;
            }

        }

        private int ReadCount(byte[] buffer, int offset, int count)
        {

            int readed = 0;
            int actualOffset = offset;
            int actualCount = count;
            while (_dataQueue.Count != 0)
            {
                if (_dataQueue.Peek().LongLength - _currentBucketPosition > actualCount)
                {
                    Array.Copy(_dataQueue.Peek(), _currentBucketPosition, buffer, actualOffset, actualCount);
                    _currentBucketPosition += actualCount;
                    readed += actualCount;
                    return readed;
                }
                else
                {
                    Array.Copy(_dataQueue.Peek(), _currentBucketPosition, buffer, actualOffset, (_dataQueue.Peek().LongLength - _currentBucketPosition));
                    readed += (_dataQueue.Peek().Length - _currentBucketPosition);
                    actualCount -= (_dataQueue.Peek().Length - _currentBucketPosition);
                    actualOffset += (_dataQueue.Peek().Length - _currentBucketPosition);
                    _dataQueue.Dequeue();
                    _currentBucketPosition = 0;
                }
            }
            return readed;

        }


        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (offset + count > buffer.Length)
                throw new ArgumentException("Buffer size must be at least " + offset + count);

            byte[] writeBuf = buffer.Skip(offset).Take(count).ToArray();

            lock (_readWriteLock)
            {
                if (_writeClosed)
                    throw new InvalidOperationException("Buffer is closed for write." + (CloseWriteReason != null ? (" Reason: " + CloseWriteReason) : ""));

                if (count > _maxDataSize)
                    _maxDataSize = _maxDataSizeMultiplier * count;

                if (_availableDataSize > _maxDataSize)
                    Monitor.Wait(_readWriteLock, int.MaxValue);

                if (_availableDataSize > _maxDataSize)
                    throw new TimeoutException("Write timeout has been expired");

                _dataQueue.Enqueue(writeBuf);
                _availableDataSize += count;
                if (_waitedReadCount > 0 && _availableDataSize > _waitedReadCount)
                    Monitor.Pulse(_readWriteLock);
            }
        }
    }
}
