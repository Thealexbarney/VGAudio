using System;

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

        public void Write(int value, int bitCount)
        {
            WriteFallback(value, bitCount);
            Position += bitCount;
        }

        public void AlignPosition(int multiple)
        {
            int newPosition = Helpers.GetNextMultiple(Position, multiple);
            int bits = newPosition - Position;
            Write(0, bits);
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
