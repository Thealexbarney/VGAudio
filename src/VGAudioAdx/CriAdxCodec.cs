using System;
using VGAudio.Utilities;
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
                short scale = (short)((adpcm[inIndex] << 8 | adpcm[inIndex + 1]) + 1);
                inIndex += 2;

                for (int s = 0; s < samplesPerFrame; s++)
                {
                    int sample = s % 2 == 0 ? GetHighNibble(adpcm[inIndex]) : GetLowNibble(adpcm[inIndex++]);
                    sample = sample >= 8 ? sample - 16 : sample;
                    sample = scale * sample + (hist1 * coefs[0] >> 12) + (hist2 * coefs[1] >> 12);
                    short finalSample = Clamp16(sample);

                    hist2 = hist1;
                    hist1 = finalSample;
                    pcm[outIndex++] = finalSample;
                }
            }
            return pcm;
        }

        public static byte[] Encode(short[] pcm, int sampleRate, int frameSize)
        {
            int sampleCount = pcm.Length;
            int samplesPerFrame = (frameSize - 2) * 2;
            int frameCount = sampleCount.DivideByRoundUp(samplesPerFrame);
            short[] coefs = CalculateCoefficients(500, sampleRate);

            var pcmBuffer = new short[samplesPerFrame + 2];
            var adpcmBuffer = new byte[frameSize];
            var adpcmOut = new byte[frameCount * frameSize];

            for (int i = 0; i < frameCount; i++)
            {
                int samplesToCopy = Math.Min(sampleCount - i * samplesPerFrame, samplesPerFrame);
                Array.Copy(pcm, i * samplesPerFrame, pcmBuffer, 2, samplesToCopy);
                Array.Clear(pcmBuffer, 2 + samplesToCopy, samplesPerFrame - samplesToCopy);

                EncodeFrame(pcmBuffer, adpcmBuffer, coefs, samplesPerFrame);

                Array.Copy(adpcmBuffer, 0, adpcmOut, i * frameSize, frameSize);
                pcmBuffer[0] = pcmBuffer[samplesPerFrame];
                pcmBuffer[1] = pcmBuffer[samplesPerFrame + 1];
            }

            return adpcmOut;
        }

        public static void EncodeFrame(short[] pcm, byte[] adpcmOut, short[] coefs, int samplesPerFrame)
        {
            int maxDistance = 0;
            int[] adpcm = new int[samplesPerFrame];

            for (int i = 0; i < samplesPerFrame; i++)
            {
                int predictedSample = (pcm[i + 1] * coefs[0] >> 12) + (pcm[i] * coefs[1] >> 12);
                int distance = pcm[i + 2] - predictedSample;
                distance = Math.Abs((int)Clamp16(distance));
                if (distance > maxDistance) maxDistance = distance;
            }

            int scale = (maxDistance - 1) / 7 + 1;
            if (scale > 0x1000) scale = 0x1000;
            double gain = maxDistance == 0 ? 0 : (double)short.MaxValue / maxDistance;

            for (int i = 0; i < samplesPerFrame; i++)
            {
                int predictedSample = (pcm[i + 1] * coefs[0] >> 12) + (pcm[i] * coefs[1] >> 12);
                int rawDistance = pcm[i + 2] - predictedSample;
                int scaledDistance = Clamp16((int)(rawDistance * gain));

                int adpcmSample = ScaleShortToNibble(scaledDistance);
                adpcm[i] = adpcmSample;

                short decodedDistance = Clamp16(scale * adpcmSample);
                int decodedSample = decodedDistance + predictedSample;
                pcm[i + 2] = Clamp16(decodedSample);
            }

            scale = scale - 1;
            adpcmOut[0] = (byte)(scale >> 8);
            adpcmOut[1] = (byte)scale;

            for (int i = 0; i < samplesPerFrame / 2; i++)
            {
                adpcmOut[i + 2] = (byte)((adpcm[i * 2] << 4) | (adpcm[i * 2 + 1] & 0xf));
            }
        }

        private static int ScaleShortToNibble(int sample)
        {
            sample = (sample + (short.MaxValue / 14) * Math.Sign(sample)) / (short.MaxValue / 7);
            return Clamp4(sample);
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

        public static sbyte Clamp4(int value)
        {
            if (value > 7)
                return 7;
            if (value < -8)
                return -8;
            return (sbyte)value;
        }
    }
}
