using System;
using System.Diagnostics;

namespace VGAudio.Utilities
{
    public class BitWriter
    {
        public byte[] Buffer { get; }
        public int LengthBits { get; }
        public int Position { get; set; }
        public int Remaining => LengthBits - Position;

        public BitWriter(byte[] buffer)
        {
            Buffer = buffer;
            LengthBits = Buffer.Length * 8;
        }

        public void AlignPosition(int multiple)
        {
            int newPosition = Helpers.GetNextMultiple(Position, multiple);
            int bits = newPosition - Position;
            Write(0, bits);
        }

        public void Write(int value, int bitCount)
        {
            Debug.Assert(bitCount >= 0 && bitCount <= 32);

            if (bitCount > Remaining)
            {
                throw new InvalidOperationException("Not enough bits left in output buffer");
            }

            int byteIndex = Position / 8;
            int bitIndex = Position % 8;

            if (bitCount <= 9 && Remaining >= 16)
            {
                int outValue = ((value << (16 - bitCount)) & 0xFFFF) >> bitIndex;

                Buffer[byteIndex] |= (byte)(outValue >> 8);
                Buffer[byteIndex + 1] = (byte)outValue;
            }

            else if (bitCount <= 17 && Remaining >= 24)
            {
                int outValue = ((value << (24 - bitCount)) & 0xFFFFFF) >> bitIndex;

                Buffer[byteIndex] |= (byte)(outValue >> 16);
                Buffer[byteIndex + 1] = (byte)(outValue >> 8);
                Buffer[byteIndex + 2] = (byte)outValue;
            }

            else if (bitCount <= 25 && Remaining >= 32)
            {
                int outValue = (int)(((value << (32 - bitCount)) & 0xFFFFFFFF) >> bitIndex);

                Buffer[byteIndex] |= (byte)(outValue >> 24);
                Buffer[byteIndex + 1] = (byte)(outValue >> 16);
                Buffer[byteIndex + 2] = (byte)(outValue >> 8);
                Buffer[byteIndex + 3] = (byte)outValue;
            }
            else
            {
                WriteFallback(value, bitCount);
            }

            Position += bitCount;
        }

        private void WriteFallback(int value, int bitCount)
        {
            int byteIndex = Position / 8;
            int bitIndex = Position % 8;

            while (bitCount > 0)
            {
                if (bitIndex >= 8)
                {
                    bitIndex = 0;
                    byteIndex++;
                }

                int toShift = 8 - bitIndex - bitCount;
                int shifted = toShift < 0 ? value >> -toShift : value << toShift;
                int bitsToWrite = Math.Min(bitCount, 8 - bitIndex);

                int mask = ((1 << bitsToWrite) - 1) << 8 - bitIndex - bitsToWrite;
                int outByte = Buffer[byteIndex] & ~mask;
                outByte |= shifted & mask;
                Buffer[byteIndex] = (byte)outByte;

                bitIndex += bitsToWrite;
                bitCount -= bitsToWrite;
            }
        }
    }
}
