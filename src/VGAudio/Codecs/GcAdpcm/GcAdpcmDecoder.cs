using System;
using VGAudio.Utilities;
using static VGAudio.Codecs.GcAdpcm.GcAdpcmMath;
using static VGAudio.Utilities.Helpers;

namespace VGAudio.Codecs.GcAdpcm
{
    public static class GcAdpcmDecoder
    {
        public static short[] Decode(byte[] adpcm, short[] coefficients, GcAdpcmParameters config = null)
        {
            config = config ?? new GcAdpcmParameters { SampleCount = ByteCountToSampleCount(adpcm.Length) };
            var pcm = new short[config.SampleCount];

            if (config.SampleCount == 0)
            {
                return pcm;
            }

            int frameCount = config.SampleCount.DivideByRoundUp(SamplesPerFrame);
            int currentSample = 0;
            int outIndex = 0;
            int inIndex = 0;
            short hist1 = config.History1;
            short hist2 = config.History2;

            for (int i = 0; i < frameCount; i++)
            {
                byte predictorScale = adpcm[inIndex++];
                int scale = (1 << GetLowNibble(predictorScale)) * 2048;
                int predictor = GetHighNibble(predictorScale);
                short coef1 = coefficients[predictor * 2];
                short coef2 = coefficients[predictor * 2 + 1];

                int samplesToRead = Math.Min(SamplesPerFrame, config.SampleCount - currentSample);

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
