using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DspAdpcm.Encode
{
    internal static class Helpers
    {
        public const int BytesPerBlock = 8;
        public const int SamplesPerBlock = 14;
        public const int NibblesPerBlock = 16;

        public static int GetNibbleFromSample(int samples)
        {
            int blocks = samples / SamplesPerBlock;
            int extraSamples = samples % SamplesPerBlock;
            int extraNibbles = extraSamples == 0 ? 0 : extraSamples + 2;

            return NibblesPerBlock * blocks + extraNibbles;
        }

        public static int GetSampleFromNibble(int nibble)
        {
            int blocks = nibble / NibblesPerBlock;
            int extraNibbles = nibble % NibblesPerBlock;
            int samples = SamplesPerBlock * blocks;

            return samples + extraNibbles - 2;
        }

        public static int GetNibbleAddress(int sample)
        {
            int blocks = sample / SamplesPerBlock;
            int extraSamples = sample % SamplesPerBlock;

            return NibblesPerBlock * blocks + extraSamples + 2;
        }

        public static int GetBytesForAdpcmSamples(int samples)
        {
            int extraBytes = 0;
            int blocks = samples / SamplesPerBlock;
            int extraSamples = samples % SamplesPerBlock;

            if (extraSamples != 0)
            {
                extraBytes = (extraSamples / 2) + (extraSamples % 2) + 1;
            }

            return BytesPerBlock * blocks + extraBytes;
        }

        public static short Clamp16(int value)
        {
            if (value > short.MaxValue)
                value = short.MaxValue;
            if (value < short.MinValue)
                value = short.MinValue;
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

        public static byte[] Combine(params byte[][] arrays)
        {
            var ret = new byte[arrays.Sum(x => x.Length)];
            int offset = 0;
            foreach (byte[] data in arrays)
            {
                Buffer.BlockCopy(data, 0, ret, offset, data.Length);
                offset += data.Length;
            }
            return ret;
        }

        public static IEnumerable<byte> ToBytesBE(this int value) => BitConverter.GetBytes(value).Reverse();
        public static IEnumerable<byte> ToBytesBE(this short value) => BitConverter.GetBytes(value).Reverse();
        public static IEnumerable<byte> ToBytesBE(this ushort value) => BitConverter.GetBytes(value).Reverse();
        public static void Add16BE(this List<byte> list, int value) => list.Add16BE((short) value);
        public static void Add16BE(this List<byte> list, short value) => list.AddRange(value.ToBytesBE());
        public static void Add16BE(this List<byte> list, ushort value) => list.AddRange(value.ToBytesBE());
        public static void Add32BE(this List<byte> list, int value) => list.AddRange(value.ToBytesBE());
        public static void Add16(this List<byte> list, short value) => list.AddRange(BitConverter.GetBytes(value));
        public static void Add32(this List<byte> list, int value) => list.AddRange(BitConverter.GetBytes(value));
        public static void Add32(this List<byte> list, string value) => list.AddRange(Encoding.ASCII.GetBytes(value.PadRight(4, '\0').Substring(0, 4)));
    }
}
