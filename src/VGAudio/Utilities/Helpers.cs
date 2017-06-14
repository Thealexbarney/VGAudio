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
        public static short Clamp16(int value)
        {
            if (value > short.MaxValue)
                return short.MaxValue;
            if (value < short.MinValue)
                return short.MinValue;
            return (short)value;
        }

        public static byte GetHighNibble(byte value) => (byte)((value >> 4) & 0xF);
        public static byte GetLowNibble(byte value) => (byte)(value & 0xF);

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
            var sieve = new int[maxPrime];

            for (int i = 2; i < maxPrime; i++)
            {
                for (int j = i * i; j < maxPrime; j += i)
                {
                    sieve[j] = 1;
                }
            }

            var primes = new List<int>();
            for (int i = 0; i < maxPrime; i++)
            {
                if (sieve[i] == 0)
                {
                    primes.Add(i);
                }
            }

            return primes.ToArray();
        }
    }
}
