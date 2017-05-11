using System;
using static VGAudio.Utilities.Helpers;

// ReSharper disable once CheckNamespace
namespace VGAudio.Codecs
{
    public class CriAdxCodec
    {
        public static short[] Decode(byte[] adpcm, int sampleCount, int sampleRate, int frameSize, short highpassFrequency)
        {
            int samplesPerFrame = (frameSize - 2) * 2;
            var coefs = CalculateCoefficients(highpassFrequency, sampleRate);
            var pcm = new short[sampleCount];

            int hist1 = 0;
            int hist2 = 0;
            int frameCount = sampleCount / samplesPerFrame;

            int outIndex = 0;
            int inIndex = 0;

            for (int i = 0; i < frameCount; i++)
            {
                short scale = (short) (adpcm[inIndex] << 8 | adpcm[inIndex + 1]);
                inIndex += 2;

                for (int s = 0; s < samplesPerFrame; s++)
                {
                    int sample = s % 2 == 0 ? GetHighNibble(adpcm[inIndex]) : GetLowNibble(adpcm[inIndex++]);
                    sample = sample >= 8 ? sample - 16 : sample;
                    sample = scale * sample + ((coefs[0] * hist1 + coefs[1] * hist2) >> 12);
                    short finalSample = Clamp16(sample);

                    hist2 = hist1;
                    hist1 = finalSample;
                    pcm[outIndex++] = finalSample;
                }
            }
            return pcm;
        }

        public static short[] CalculateCoefficients(int highpassFreq, int sampleRate)
        {
            double sqrt2 = Math.Sqrt(2);
            double a = sqrt2 - Math.Cos(2.0 * Math.PI * highpassFreq / sampleRate);
            double b = sqrt2 - 1;
            double c = (a - Math.Sqrt((a + b) * (a - b))) / b;

            short coef1 = (short)(c * 8192);
            short coef2 = (short)(c * c * -4096);

            return new[] { coef1, coef2 };
        }
    }
}
