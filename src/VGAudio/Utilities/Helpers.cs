using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace VGAudio.Utilities
{
    public static class Helpers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Clamp(int value, int min, int max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Clamp(double value, double min, double max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short Clamp16(int value)
        {
            if (value > short.MaxValue)
                return short.MaxValue;
            if (value < short.MinValue)
                return short.MinValue;
            return (short)value;
        }

        public static sbyte Clamp4(int value)
        {
            if (value > 7)
                return 7;
            if (value < -8)
                return -8;
            return (sbyte)value;
        }

        private static readonly sbyte[] SignedNibbles = { 0, 1, 2, 3, 4, 5, 6, 7, -8, -7, -6, -5, -4, -3, -2, -1 };

        public static byte GetHighNibble(byte value) => (byte)((value >> 4) & 0xF);
        public static byte GetLowNibble(byte value) => (byte)(value & 0xF);

        public static sbyte GetHighNibbleSigned(byte value) => SignedNibbles[(value >> 4) & 0xF];
        public static sbyte GetLowNibbleSigned(byte value) => SignedNibbles[value & 0xF];

        public static byte CombineNibbles(int high, int low) => (byte)((high << 4) | (low & 0xF));

        public static int BitCount(int v) => BitCount(unchecked((uint)v));
        public static int BitCount(uint v)
        {
            unchecked
            {
                v = v - ((v >> 1) & 0x55555555);
                v = (v & 0x33333333) + ((v >> 2) & 0x33333333);
                return (int)((v + (v >> 4) & 0xF0F0F0F) * 0x1010101) >> 24;
            }
        }

        public static int GetNextMultiple(int value, int multiple)
        {
            if (multiple <= 0)
                return value;

            if (value % multiple == 0)
                return value;

            return value + multiple - value % multiple;
        }

        public static bool LoopPointsAreAligned(int loopStart, int alignmentMultiple)
            => !(alignmentMultiple != 0 && loopStart % alignmentMultiple != 0);

        public static BinaryReader GetBinaryReader(Stream stream, Endianness endianness) =>
            endianness == Endianness.LittleEndian
                ? new BinaryReader(stream, Encoding.UTF8, true)
                : new BinaryReaderBE(stream, Encoding.UTF8, true);

        public static BinaryWriter GetBinaryWriter(Stream stream, Endianness endianness) =>
            endianness == Endianness.LittleEndian
                ? new BinaryWriter(stream, Encoding.UTF8, true)
                : new BinaryWriterBE(stream, Encoding.UTF8, true);

        public static T CreateJaggedArray<T>(params int[] lengths)
        {
            return (T)InitializeJaggedArray(typeof(T).GetElementType(), 0, lengths);
        }

        private static object InitializeJaggedArray(Type type, int index, int[] lengths)
        {
            Array array = Array.CreateInstance(type, lengths[index]);

            Type elementType = type.GetElementType();
            if (elementType == null) return array;

            for (int i = 0; i < lengths[index]; i++)
            {
                array.SetValue(InitializeJaggedArray(elementType, index + 1, lengths), i);
            }

            return array;
        }

        public static int[] GetPrimes(int maxPrime)
        {
            int max = maxPrime / 2;
            var sieve = new byte[max];

            for (int i = 3; i * i < maxPrime; i += 2)
            {
                if (sieve[i >> 1] != 0) continue;
                for (int j = i * i; j < maxPrime; j += i * 2)
                {
                    sieve[j >> 1] = 1;
                }
            }

            var primes = new List<int> { 2 };
            for (int i = 1; i < max; i++)
            {
                if (sieve[i] == 0)
                {
                    primes.Add(i * 2 + 1);
                }
            }

            return primes.ToArray();
        }
    }
}
