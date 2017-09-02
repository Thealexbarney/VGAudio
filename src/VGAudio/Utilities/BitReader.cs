using System;
using System.Diagnostics;

namespace VGAudio.Utilities
{
    public class BitReader
    {
        public byte[] Buffer { get; }
        public int LengthBits { get; }
        public int Position { get; set; }
        public int Remaining => LengthBits - Position;

        public BitReader(byte[] buffer)
        {
            Buffer = buffer;
            LengthBits = Buffer.Length * 8;
        }

        public int ReadInt(int bitCount)
        {
            int value = PeekInt(bitCount);
            Position += bitCount;
            return value;
        }

        public int ReadSignedInt(int bitCount)
        {
            int value = PeekInt(bitCount);
            Position += bitCount;
            return SignExtend32(value, bitCount);
        }

        public bool ReadBool() => ReadInt(1) == 1;

        public int ReadOffsetBinary(int bitCount)
        {
            int offset = (1 << (bitCount - 1)) - 1;
            int value = PeekInt(bitCount) - offset;
            Position += bitCount;
            return value;
        }

        public static int SignExtend32(int value, int bits)
        {
            int shift = 8 * sizeof(int) - bits;
            return (value << shift) >> shift;
        }

        public void AlignPosition(int multiple)
        {
            Position = Helpers.GetNextMultiple(Position, multiple);
        }

        public int PeekInt(int bitCount)
        {
            Debug.Assert(bitCount >= 0 && bitCount <= 32);

            if (bitCount > Remaining)
            {
                if (Position >= LengthBits) return 0;

                int extraBits = bitCount - Remaining;
                return PeekIntFallback(Remaining) << extraBits;
            }

            int byteIndex = Position / 8;
            int bitIndex = Position % 8;

            if (bitCount <= 9 && Remaining >= 16)
            {
                int value = Buffer[byteIndex] << 8 | Buffer[byteIndex + 1];
                value &= 0xFFFF >> bitIndex;
                value >>= 16 - bitCount - bitIndex;
                return value;
            }

            if (bitCount <= 17 && Remaining >= 24)
            {
                int value = Buffer[byteIndex] << 16 | Buffer[byteIndex + 1] << 8 | Buffer[byteIndex + 2];
                value &= 0xFFFFFF >> bitIndex;
                value >>= 24 - bitCount - bitIndex;
                return value;
            }

            if (bitCount <= 25 && Remaining >= 32)
            {
                int value = Buffer[byteIndex] << 24 | Buffer[byteIndex + 1] << 16 | Buffer[byteIndex + 2] << 8 | Buffer[byteIndex + 3];
                value &= (int)(0xFFFFFFFF >> bitIndex);
                value >>= 32 - bitCount - bitIndex;
                return value;
            }
            return PeekIntFallback(bitCount);
        }

        private int PeekIntFallback(int bitCount)
        {
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
