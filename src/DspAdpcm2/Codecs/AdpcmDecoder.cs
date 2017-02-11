using System;
using DspAdpcm.Formats;
using DspAdpcm.Utilities;

namespace DspAdpcm.Codecs
{
    public static class AdpcmDecoder
    {
        public static short[] Decode(byte[] adpcm, short[] coefficients, int length, short history1, short history2)
        {
            var pcm = new short[length];
            short hist1 = history1;
            short hist2 = history2;
            int frameCount = length.DivideByRoundUp(AdpcmHelpers.SamplesPerFrame);

            int inIndex = 0;
            int outIndex = 0;

            for (int i = 0; i < frameCount; i++)
            {
                byte ps = adpcm[inIndex++];
                int scale = 1 << Helpers.GetLowNibble(ps);
                int predictor = Helpers.GetHighNibble(ps);
                short coef1 = coefficients[predictor * 2];
                short coef2 = coefficients[predictor * 2 + 1];

                int samplesToRead = Math.Min(AdpcmHelpers.SamplesPerFrame, length - outIndex);

                for (int s = 0; s < samplesToRead; s++)
                {
                    int sample = s % 2 == 0 ? Helpers.GetHighNibble(adpcm[inIndex]) : Helpers.GetLowNibble(adpcm[inIndex++]);
                    sample = sample >= 8 ? sample - 16 : sample;
                    sample = (((scale * sample) << 11) + 1024 + (coef1 * hist1 + coef2 * hist2)) >> 11;
                    short finalSample = Helpers.Clamp16(sample);

                    hist2 = hist1;
                    hist1 = finalSample;

                    pcm[outIndex++] = finalSample;
                }
            }

            return pcm;
        }
    }
}
