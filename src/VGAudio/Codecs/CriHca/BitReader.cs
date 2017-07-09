using System;
using System.Diagnostics;

namespace VGAudio.Codecs.CriHca
{
    public class BitReader
    {
        public byte[] Buffer { get; }
        public int Position { get; set; }

        public BitReader(byte[] buffer) => Buffer = buffer;

        public int ReadInt(int bitCount)
        {
            int value = PeekInt(bitCount);
            Position += bitCount;
            return value;
        }

        public int ReadOffsetBinary(int bitCount)
        {
            int offset = (1 << (bitCount - 1)) - 1;
            int value = PeekInt(bitCount) - offset;
            Position += bitCount;
            return value;
        }

        public int PeekInt(int bitCount)
        {
            Debug.Assert(bitCount >= 0 && bitCount <= 32);
            int value = 0;
            int byteIndex = Position / 8;
            int bitIndex = Position % 8;

            while (bitCount > 0)
            {
                if (bitIndex >= 8)
                {
                    bitIndex = 0;
                    byteIndex++;
                }

                int bitsToRead = Math.Min(bitCount, 8 - bitIndex);
                int mask = 0xFF >> bitIndex;
                int currentByte = (mask & Buffer[byteIndex]) >> (8 - bitIndex - bitsToRead);

                value = (value << bitsToRead) | currentByte;
                bitIndex += bitsToRead;
                bitCount -= bitsToRead;
            }
            return value;
        }
    }
}
