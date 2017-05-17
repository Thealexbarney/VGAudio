using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace VGAudio.Utilities
{
    internal static class Extensions
    {
        public static IEnumerable<T[]> Batch<T>(this IEnumerable<T> source, int size, bool truncateLastBatch = false)
        {
            T[] bucket = new T[size];
            var count = 0;

            foreach (var item in source)
            {
                bucket[count++] = item;

                if (count != size)
                    continue;

                yield return bucket;

                bucket = new T[size];
                count = 0;
            }

            // Return the last bucket with all remaining elements
            if (count > 0)
                yield return bucket.Take(truncateLastBatch ? count : size).ToArray();
        }

        public static byte[] ToByteArray(this short[] array, Endianness endianness = Endianness.LittleEndian)
        {
            var output = new byte[array.Length * 2];
            if (endianness == Endianness.LittleEndian)
            {
                Buffer.BlockCopy(array, 0, output, 0, output.Length);
            }
            else
            {
                for (int i = 0; i < array.Length; i++)
                {
                    output[i * 2] = (byte)(array[i] >> 8);
                    output[i * 2 + 1] = (byte)array[i];
                }
            }
            return output;
        }

        public static short[] ToShortArray(this byte[] array, Endianness endianness = Endianness.LittleEndian)
        {
            int length = array.Length.DivideByRoundUp(2);
            var output = new short[length];

            if (endianness == Endianness.LittleEndian)
            {
                Buffer.BlockCopy(array, 0, output, 0, array.Length);
            }
            else
            {
                for (int i = 0; i < length; i++)
                {
                    output[i] = (short)((array[i * 2] << 8) | array[i * 2 + 1]);
                }
            }

            return output;
        }

        public static string ReadUTF8(this BinaryReader reader, int size)
        {
            return Encoding.UTF8.GetString(reader.ReadBytes(size), 0, size);
        }

        public static void WriteUTF8(this BinaryWriter writer, string value)
        {
            byte[] text = Encoding.UTF8.GetBytes(value);
            writer.Write(text);
        }

        public static bool Eof(this BinaryReader reader) => reader.BaseStream.Position >= reader.BaseStream.Length;

        public static void Expect(this BinaryReader reader, params int[] expected)
        {
            long offset = reader.BaseStream.Position;
            int actual = reader.ReadInt32();
            if (!expected.Contains(actual))
            {
                throw new InvalidDataException(
                    $"Expected {(expected.Length > 1 ? "one of: " : "")}" +
                    $"{expected.ToDelimitedString()}, but got {actual} at offset 0x{offset:X}");
            }
        }

        public static void Expect(this BinaryReader reader, params short[] expected)
        {
            long offset = reader.BaseStream.Position;
            short actual = reader.ReadInt16();
            if (!expected.Contains(actual))
            {
                throw new InvalidDataException(
                    $"Expected {(expected.Length > 1 ? "one of: " : "")}" +
                    $"{expected.ToDelimitedString()}, but got {actual} at offset 0x{offset:X}");
            }
        }

        public static void Expect(this BinaryReader reader, params ushort[] expected)
        {
            long offset = reader.BaseStream.Position;
            ushort actual = reader.ReadUInt16();
            if (!expected.Contains(actual))
            {
                throw new InvalidDataException(
                    $"Expected {(expected.Length > 1 ? "one of: " : "")}" +
                    $"{expected.ToDelimitedString()}, but got {actual} at offset 0x{offset:X}");
            }
        }

        public static void Expect(this BinaryReader reader, params byte[] expected)
        {
            long offset = reader.BaseStream.Position;
            byte actual = reader.ReadByte();
            if (!expected.Contains(actual))
            {
                throw new InvalidDataException(
                    $"Expected {(expected.Length > 1 ? "one of: " : "")}" +
                    $"{expected.ToDelimitedString()}, but got {actual} at offset 0x{offset:X}");
            }
        }

        public static string ToDelimitedString<T>(this IList<T> items)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < items.Count; i++)
            {
                if (i != 0)
                {
                    sb.Append(", ");
                }
                sb.Append(items[i]);
            }
            return sb.ToString();
        }

        public static int DivideByRoundUp(this int value, int divisor) => (int)Math.Ceiling((double)value / divisor);
        public static int DivideBy2RoundUp(this int value) => (value / 2) + (value & 1);
    }
}
