using DspAdpcm.Utilities;

namespace DspAdpcm.Formats.Adpcm
{
    public static class AdpcmHelpers
    {
        public static readonly int BytesPerFrame = 8;
        public static readonly int SamplesPerFrame = 14;
        public static readonly int NibblesPerFrame = 16;

        public static int SampleCountToNibbleCount(int sampleCount)
        {
            int frames = sampleCount / SamplesPerFrame;
            int extraSamples = sampleCount % SamplesPerFrame;
            int extraNibbles = extraSamples == 0 ? 0 : extraSamples + 2;

            return NibblesPerFrame * frames + extraNibbles;
        }

        public static int NibbleToSample(int nibble)
        {
            int frames = nibble / NibblesPerFrame;
            int extraNibbles = nibble % NibblesPerFrame;
            int samples = SamplesPerFrame * frames;

            return samples + extraNibbles - 2;
        }

        public static int SampleToNibble(int sample)
        {
            int frames = sample / SamplesPerFrame;
            int extraSamples = sample % SamplesPerFrame;

            return NibblesPerFrame * frames + extraSamples + 2;
        }

        public static int SampleCountToByteCount(int sampleCount) => SampleCountToNibbleCount(sampleCount).DivideBy2RoundUp();
    }
}
