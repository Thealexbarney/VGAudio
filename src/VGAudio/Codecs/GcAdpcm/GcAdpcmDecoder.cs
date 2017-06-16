using System;
using VGAudio.Utilities;
using static VGAudio.Formats.GcAdpcm.GcAdpcmHelpers;
using static VGAudio.Utilities.Helpers;

namespace VGAudio.Codecs.GcAdpcm
{
    public static class GcAdpcmDecoder
    {
        public static short[] Decode(byte[] adpcm, short[] coefficients, short hist1 = 0, short hist2 = 0)
        {
            int nibbleCount = adpcm.Length * 2;
            int sampleCount = NibbleCountToSampleCount(nibbleCount);
            return Decode(adpcm, coefficients, sampleCount, hist1, hist2);
        }

        public static short[] Decode(byte[] adpcm, short[] coefficients, int length, short hist1 = 0, short hist2 = 0)
        {
            var pcm = new short[length];

            if (length == 0)
            {
                return pcm;
            }

            int frameCount = length.DivideByRoundUp(SamplesPerFrame);
            int currentSample = 0;
            int outIndex = 0;
            int inIndex = 0;

            for (int i = 0; i < frameCount; i++)
            {
                byte predictorScale = adpcm[inIndex++];
                int scale = (1 << GetLowNibble(predictorScale)) * 2048;
                int predictor = GetHighNibble(predictorScale);
                short coef1 = coefficients[predictor * 2];
                short coef2 = coefficients[predictor * 2 + 1];

                int samplesToRead = Math.Min(SamplesPerFrame, length - currentSample);

                for (int s = 0; s < samplesToRead; s++)
                {
                    int adpcmSample = s % 2 == 0 ? GetHighNibbleSigned(adpcm[inIndex]) : GetLowNibbleSigned(adpcm[inIndex++]);
                    int distance = scale * adpcmSample;
                    int predictedSample = coef1 * hist1 + coef2 * hist2;
                    int correctedSample = predictedSample + distance;
                    int scaledSample = (correctedSample + 1024) >> 11;
                    short clampedSample = Clamp16(scaledSample);

                    hist2 = hist1;
                    hist1 = clampedSample;

                    pcm[outIndex++] = clampedSample;
                    currentSample++;
                }
            }
            return pcm;
        }

        public static byte GetPredictorScale(byte[] adpcm, int sample)
        {
            return adpcm[sample / SamplesPerFrame * BytesPerFrame];
        }
    }
}
