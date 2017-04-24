using System;
using VGAudio.Utilities;
using static VGAudio.Formats.GcAdpcm.GcAdpcmHelpers;
using static VGAudio.Utilities.Helpers;

namespace VGAudio.Codecs
{
    public static class GcAdpcmDecoder
    {
        public static short[] Decode(byte[] adpcm, short[] coefficients, short hist1 = 0, short hist2 = 0)
        {
            int nibbles = adpcm.Length * 2;
            int sampleCount = NibbleCountToSampleCount(nibbles);
            return Decode(adpcm, coefficients, sampleCount, hist1, hist2);
        }

        public static short[] Decode(byte[] adpcm, short[] coefficients, int length, short hist1 = 0, short hist2 = 0)
        {
            var pcm = new short[length];

            if (length == 0)
            {
                return pcm;
            }

            int currentSample = 0;
            const int startSample = 0;

            int outIndex = 0;
            int inIndex = 0;

            int endSample = startSample + length;
            int frameCount = (endSample - currentSample).DivideByRoundUp(SamplesPerFrame);

            for (int i = 0; i < frameCount; i++)
            {
                byte ps = adpcm[inIndex++];
                int scale = 1 << GetLowNibble(ps);
                int predictor = GetHighNibble(ps);
                short coef1 = coefficients[predictor * 2];
                short coef2 = coefficients[predictor * 2 + 1];

                int samplesToRead = Math.Min(SamplesPerFrame, endSample - currentSample);

                for (int s = 0; s < samplesToRead; s++)
                {
                    int sample = s % 2 == 0 ? GetHighNibble(adpcm[inIndex]) : GetLowNibble(adpcm[inIndex++]);
                    sample = sample >= 8 ? sample - 16 : sample;
                    sample = (((scale * sample) << 11) + 1024 + (coef1 * hist1 + coef2 * hist2)) >> 11;
                    short finalSample = Clamp16(sample);

                    hist2 = hist1;
                    hist1 = finalSample;

                    if (currentSample++ >= startSample)
                    {
                        pcm[outIndex++] = finalSample;
                    }
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
