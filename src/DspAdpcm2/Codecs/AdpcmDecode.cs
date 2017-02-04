using DspAdpcm.Utilities;
using static DspAdpcm.Utilities.Helpers;

namespace DspAdpcm.Codecs
{
    public static class AdpcmDecode
    {
        public static short[] Decode(byte[] adpcm, short[] coefficients, int length, short history1, short history2)
        {
            var pcm = new short[length];
            short hist1 = history1;
            short hist2 = history2;
            int frameCount = length.DivideByRoundUp(Adpcm.BytesPerFrame);

            int inIndex = 0;
            int outIndex = 0;

            for (int i = 0; i < frameCount; i++)
            {
                byte ps = adpcm[inIndex++];
                int scale = 1 << (ps & 0xF);
                int predictor = (ps >> 4) & 0xF;
                short coef1 = coefficients[predictor * 2];
                short coef2 = coefficients[predictor * 2 + 1];

                for (int s = 0; s < 14; s++)
                {
                    int sample;
                    if (s % 2 == 0)
                    {
                        sample = (adpcm[inIndex] >> 4) & 0xF;
                    }
                    else
                    {
                        sample = adpcm[inIndex++] & 0xF;
                    }
                    sample = sample >= 8 ? sample - 16 : sample;

                    sample = (((scale * sample) << 11) + 1024 + (coef1 * hist1 + coef2 * hist2)) >> 11;
                    short finalSample = Clamp16(sample);

                    hist2 = hist1;
                    hist1 = finalSample;

                    pcm[outIndex++] = finalSample;

                    if (outIndex >= length)
                    {
                        return pcm;
                    }
                }
            }

            return pcm;
        }
    }
}
