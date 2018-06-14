using System;
using System.IO;

namespace TickTrader.FDK.Client.Serialization
{
    public class StreamParser
    {
        public StreamParser()
        {
        }

        public StreamParser(Stream streamToParse)
        {
            Initialize(streamToParse);
        }

        public void Initialize(Stream text)
        {
            streamReader_ = new StreamReader(text, System.Text.Encoding.ASCII, false, 8 * 1024);
            pos_ = -1;

            NextChar();
        }

        public void ValidateVerbatimText( String value )
        {
            for (int index = 0; index < value.Length; ++index)
            {
                char expected = value[index];
                ValidateVerbatimChar(expected);
            }
        }

        public bool TryValidateVerbatimText( String value )
        {
            for (int index = 0; index < value.Length; ++index)
            {
                char expected = value[index];
                if (! TryValidateVerbatimChar(expected))
                    return false;
            }

            return true;
        }

        public void ValidateVerbatimChar( char expected )
        {
            if (eof_)
                throw new FormatException("Unexpected end of stream");

            if (expected != ch_)
            {
                String message = String.Format("Validate verbatim text is failed; expected {0}, but read {1}", expected, ch_);
                throw new FormatException(message);
            }

            NextChar();
        }

        public bool TryValidateVerbatimChar( char expected )
        {
            if (eof_)
                return false;

            if (expected != ch_)
                return false;

            NextChar();

            return true;
        }

        public void ReadInt32(out int value)
        {
            int v;

            if (eof_)
                throw new FormatException("Unexpected end of stream");

            {
                if (ch_ == '-')
                {
                    NextChar();

                    if (eof_)
                        throw new FormatException("Unexpected end of stream");

                    v = ('0' - ch_);
                }
                else
                {
                    if ((ch_ > '9') || (ch_ < '0'))
                    {
                        String message = String.Format("Integer value can not strart from {0}", ch_);
                        throw new FormatException(message);
                    }
                    v = (ch_ - '0');
                }

                NextChar();
            }

            while (true)
            {
                if (eof_)
                    break;

                if ((ch_ > '9') || (ch_ < '0'))
                    break;

                checked
                {
                    v *= 10;
                    v += (ch_ - '0');
                }

                NextChar();
            }

            value = v;
        }

        public void ReadInt64(out Int64 value)
        {
            Int64 v;

            if (eof_)
                throw new FormatException("Unexpected end of stream");

            {
                if (ch_ == '-')
                {
                    NextChar();

                    if (eof_)
                        throw new FormatException("Unexpected end of stream");

                    v = ('0' - ch_);
                }
                else
                {
                    if ((ch_ > '9') || (ch_ < '0'))
                    {
                        String message = String.Format("Integer value can not strart from {0}", ch_);
                        throw new FormatException(message);
                    }
                    v = (ch_ - '0');
                }

                NextChar();
            }

            while (true)
            {
                if (eof_)
                    break;

                if ((ch_ > '9') || (ch_ < '0'))
                    break;

                checked
                {
                    v *= 10;
                    v += (ch_ - '0');
                }

                NextChar();
            }

            value = v;
        }

        public bool IsEnd()
        {
            return eof_;
        }

        public void ReadDouble(out double value)
        {
            Int64 cel;
            ReadInt64(out cel);
            value = cel;

            if (! eof_)
            {
                if (ch_ == '.')
                {
                    NextChar();

                    int oldPos = pos_;
                    Int64 drob;
                    ReadInt64(out drob);
                    value += (double)drob / Pow10(pos_ - oldPos);
                }
            }
        }

        void NextChar()
        {
            int i = streamReader_.Read();

            if (i == -1)
            {
                eof_ = true;
                return;
            }

            eof_ = false;
            ch_ = (char) i;
            ++ pos_;
        }

        long Pow10(int val)
        {
            switch (val)
            {
                case 0:
                    return 1L;
                case 1:
                    return 10L;
                case 2:
                    return 100L;
                case 3:
                    return 1000L;
                case 4:
                    return 10000L;
                case 5:
                    return 100000L;
                case 6:
                    return 1000000L;
                case 7:
                    return 10000000L;
                case 8:
                    return 100000000L;
                case 9:
                    return 1000000000L;
                case 10:
                    return 10000000000L;
                case 11:
                    return 100000000000L;
                case 12:
                    return 1000000000000L;
                case 13:
                    return 10000000000000L;
                default:
                    return (long)Math.Pow(10, val);
            }
        }

        StreamReader streamReader_;

        bool eof_;
        char ch_;
        int pos_;
    }
}
