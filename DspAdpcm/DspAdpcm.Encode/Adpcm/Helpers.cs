using System;
using System.Collections.Generic;
using System.Linq;

namespace DspAdpcm.Encode.Adpcm
{
    public static class Helpers
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

        public static IEnumerable<byte> ToBytesBE(this int input) => BitConverter.GetBytes(input).Reverse();
        public static IEnumerable<byte> ToBytesBE(this short input) => BitConverter.GetBytes(input).Reverse();
    }
}
