using System;
using VGAudio.Utilities;
using static VGAudio.Utilities.Helpers;

namespace VGAudio.Codecs.CriAdx
{
    public static class CriAdxCodec
    {
        public static short[] Decode(byte[] adpcm, int sampleCount, CriAdxParameters config = null)
        {
            CriAdxParameters c = config ?? new CriAdxParameters();
            int samplesPerFrame = (c.FrameSize - 2) * 2;
            short[][] coefs = c.Type == CriAdxType.Fixed ? Coefs : new[] { CalculateCoefficients(c.HighpassFrequency, c.SampleRate) };
            var pcm = new short[sampleCount];

            int hist1 = c.History;
            int hist2 = c.History;
            int frameCount = sampleCount.DivideByRoundUp(samplesPerFrame);

            int currentSample = 0;
            int startSample = c.Padding > 0 ? c.Padding % samplesPerFrame : 0;
            int inIndex = c.Padding / samplesPerFrame * c.FrameSize;

            for (int i = 0; i < frameCount; i++)
            {
                int filterNum = GetHighNibble(adpcm[inIndex]) >> 1;
                short scale = (short)((adpcm[inIndex] << 8 | adpcm[inIndex + 1]) & 0x1FFF);
                scale = (short)(c.Type == CriAdxType.Exponential ? 1 << (12 - scale) : scale + 1);
                inIndex += 2 + startSample / 2;

                int samplesToRead = Math.Min(samplesPerFrame, sampleCount - currentSample);

                for (int s = startSample; s < samplesToRead; s++)
                {
                    int sample = s % 2 == 0 ? GetHighNibbleSigned(adpcm[inIndex]) : GetLowNibbleSigned(adpcm[inIndex++]);
                    if (c.Version == 4)
                    {
                        sample = scale * sample + ((hist1 * coefs[filterNum][0] + hist2 * coefs[filterNum][1]) >> 12);
                    }
                    else
                    {
                        sample = scale * sample + (hist1 * coefs[filterNum][0] >> 12) + (hist2 * coefs[filterNum][1] >> 12);
                    }

                    short finalSample = Clamp16(sample);

                    hist2 = hist1;
                    hist1 = finalSample;
                    pcm[currentSample++] = finalSample;
                }
                startSample = 0;
            }
            return pcm;
        }

        public static byte[] Encode(short[] pcm, CriAdxParameters config)
        {
            CriAdxParameters c = config;
            int sampleCount = pcm.Length + c.Padding;
            int samplesPerFrame = (c.FrameSize - 2) * 2;
            int frameCount = sampleCount.DivideByRoundUp(samplesPerFrame);
            int paddingRemaining = c.Padding;
            short[] coefs = c.Type == CriAdxType.Fixed ? Coefs[c.Filter] : CalculateCoefficients(500, c.SampleRate);

            var pcmBuffer = new short[samplesPerFrame + 2];
            var adpcmBuffer = new byte[c.FrameSize];
            var adpcmOut = new byte[frameCount * c.FrameSize];

            if (c.Version == 4 && c.Padding == 0)
            {
                pcmBuffer[0] = pcm[0];
                pcmBuffer[1] = pcm[0];
                c.History = pcm[0];
            }

            for (int i = 0; i < frameCount; i++)
            {
                int samplesToCopy = Math.Min(sampleCount - i * samplesPerFrame, samplesPerFrame);
                int pcmBufferStart = 2;
                if (paddingRemaining != 0)
                {
                    while (paddingRemaining > 0 && samplesToCopy > 0)
                    {
                        paddingRemaining--;
                        samplesToCopy--;
                        pcmBufferStart++;
                    }
                    if (samplesToCopy == 0) continue;
                }
                Array.Copy(pcm, Math.Max(i * samplesPerFrame - c.Padding, 0), pcmBuffer, pcmBufferStart, samplesToCopy);
                Array.Clear(pcmBuffer, pcmBufferStart + samplesToCopy, samplesPerFrame - samplesToCopy - pcmBufferStart + 2);

                EncodeFrame(pcmBuffer, adpcmBuffer, coefs, samplesPerFrame, c.Type, c.Version);

                if (c.Type == CriAdxType.Fixed) { adpcmBuffer[0] |= (byte)(c.Filter << 5); }

                Array.Copy(adpcmBuffer, 0, adpcmOut, i * c.FrameSize, c.FrameSize);
                pcmBuffer[0] = pcmBuffer[samplesPerFrame];
                pcmBuffer[1] = pcmBuffer[samplesPerFrame + 1];

                config.Progress?.ReportAdd(1);
            }

            return adpcmOut;
        }

        public static void EncodeFrame(short[] pcm, byte[] adpcmOut, short[] coefs, int samplesPerFrame, CriAdxType type, int version)
        {
            int maxDistance = 0;
            var adpcm = new int[samplesPerFrame];

            for (int i = 0; i < samplesPerFrame; i++)
            {
                int predictedSample = (pcm[i + 1] * coefs[0] >> 12) + (pcm[i] * coefs[1] >> 12);
                int distance = pcm[i + 2] - predictedSample;
                distance = Math.Abs((int)Clamp16(distance));
                if (distance > maxDistance) maxDistance = distance;
            }

            int scale = CalculateScale(maxDistance, out double gain, out int scaleOut, type == CriAdxType.Exponential);

            for (int i = 0; i < samplesPerFrame; i++)
            {
                int predictedSample = (pcm[i + 1] * coefs[0] >> 12) + (pcm[i] * coefs[1] >> 12);
                int rawDistance = pcm[i + 2] - predictedSample;
                int scaledDistance = Clamp16((int)(rawDistance * gain));

                int adpcmSample = ScaleShortToNibble(scaledDistance);
                adpcm[i] = adpcmSample;

                short decodedDistance = Clamp16(scale * adpcmSample);
                if (version == 4)
                {
                    predictedSample = (pcm[i + 1] * coefs[0] + pcm[i] * coefs[1]) >> 12;
                }
                int decodedSample = decodedDistance + predictedSample;
                pcm[i + 2] = Clamp16(decodedSample);
            }

            adpcmOut[0] = (byte)((scaleOut >> 8) & 0x1f);
            adpcmOut[1] = (byte)scaleOut;

            for (int i = 0; i < samplesPerFrame / 2; i++)
            {
                adpcmOut[i + 2] = CombineNibbles(adpcm[i * 2], adpcm[i * 2 + 1]);
            }
        }

        private static int CalculateScale(int maxDistance, out double gain, out int scaleToWrite, bool exponential = false)
        {
            int scale = (maxDistance - 1) / 7 + 1;
            if (scale > 0x1000) scale = 0x1000;
            scaleToWrite = scale - 1;

            if (exponential)
            {
                int power = scaleToWrite == 0 ? 0 : Log2(scaleToWrite) + 1;
                scale = 1 << power;
                scaleToWrite = 12 - power;
                maxDistance = 8 * scale - 1;
            }

            gain = maxDistance == 0 ? 0 : (double)short.MaxValue / maxDistance;
            return scale;
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

        private static readonly short[][] Coefs = {
            new short[] {0, 0},
            new short[] {0x0F00, 0},
            new short[] {0x1CC0, unchecked((short)0xF300)},
            new short[] {0x1880, unchecked((short)0xF240)}
        };
    }
}
