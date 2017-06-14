using VGAudio.Utilities;

namespace VGAudio.Formats.CriAdx
{
    public static class CriAdxHelpers
    {
        public static int NibbleCountToSampleCount(int nibbleCount, int frameSize)
        {
            int nibblesPerFrame = frameSize * 2;
            int samplesPerFrame = nibblesPerFrame - 4;

            int frames = nibbleCount / nibblesPerFrame;
            int extraNibbles = nibbleCount % nibblesPerFrame;
            int extraSamples = extraNibbles < 4 ? 0 : extraNibbles - 4;

            return samplesPerFrame * frames + extraSamples;
        }

        public static int SampleCountToNibbleCount(int sampleCount, int frameSize)
        {
            int nibblesPerFrame = frameSize * 2;
            int samplesPerFrame = nibblesPerFrame - 4;

            int frames = sampleCount / samplesPerFrame;
            int extraSamples = sampleCount % samplesPerFrame;
            int extraNibbles = extraSamples == 0 ? 0 : extraSamples + 4;

            return nibblesPerFrame * frames + extraNibbles;
        }

        public static int SampleCountToByteCount(int sampleCount, int frameSize) =>
            SampleCountToNibbleCount(sampleCount, frameSize).DivideBy2RoundUp();
    }
}