using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace DspAdpcm.Lib
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    }
}
