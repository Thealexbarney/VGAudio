﻿using System;
using System.IO;
using System.Runtime.CompilerServices;
using DspAdpcm.Compatibility;

namespace DspAdpcm.Utilities
{
    internal static class Helpers
    {
#if !(NET20 || NET35 || NET40)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static short Clamp16(int value)
        {
            if (value > short.MaxValue)
                return short.MaxValue;
            if (value < short.MinValue)
                return short.MinValue;
            return (short)value;
        }

        public static int GetNextMultiple(int value, int multiple)
        {
            if (multiple <= 0)
                return value;

            if (value % multiple == 0)
                return value;

            return value + multiple - value % multiple;
        }

        public static void CheckStream(Stream stream, int minLength)
        {
            if (!stream.CanSeek)
            {
                throw new NotSupportedException("A seekable stream is required");
            }

            if (stream.Length < minLength)
            {
                throw new InvalidDataException($"File is only {stream.Length} bytes long");
            }
        }

        public static bool ArraysEqual<T>(T[] a1, T[] a2)
        {
            if (a1 == null || a2 == null) return false;
            if (a1 == a2) return true;
            if (a1.Length != a2.Length) return false;

            for (int i = 0; i < a1.Length; i++)
            {
                if (!a1[i].Equals(a2[i]))
                {
                    return false;
                }
            }
            return true;
        }

        public enum Endianness
        {
            BigEndian,
            LittleEndian
        }

        public static BinaryReader GetBinaryReader(Stream stream, Endianness endianness) =>
            endianness == Endianness.LittleEndian
                ? GetStream.GetBinaryReader(stream)
                : GetStream.GetBinaryReaderBE(stream);

        public static BinaryWriter GetBinaryWriter(Stream stream, Endianness endianness) =>
            endianness == Endianness.LittleEndian
                ? GetStream.GetBinaryWriter(stream)
                : GetStream.GetBinaryWriterBE(stream);
    }
}
