using System;
using VGAudio.Containers.Adx;
using VGAudio.Utilities;
using static VGAudio.Utilities.Helpers;

// ReSharper disable once CheckNamespace
namespace VGAudio.Codecs
{
    public class CriAdxCodec
    {
        public static short[] Decode(byte[] adpcm, int sampleCount, int sampleRate, int frameSize, short highpassFrequency, AdxType type = AdxType.Standard)
        {
            int samplesPerFrame = (frameSize - 2) * 2;
            short[][] coefs = type == AdxType.Fixed ? Coefs : new[] { CalculateCoefficients(highpassFrequency, sampleRate) };
            var pcm = new short[sampleCount];

            int hist1 = 0;
            int hist2 = 0;
            int frameCount = sampleCount.DivideByRoundUp(samplesPerFrame);

            int currentSample = 0;
            int inIndex = 0;

            for (int i = 0; i < frameCount; i++)
            {
                int filterNum = GetHighNibble(adpcm[inIndex]) >> 1;
                short scale = (short)((adpcm[inIndex] << 8 | adpcm[inIndex + 1]) & 0x1FFF);
                scale = (short)(type == AdxType.Exponential ? 1 << (12 - scale) : scale + 1);
                inIndex += 2;

                int samplesToRead = Math.Min(samplesPerFrame, sampleCount - currentSample);

                for (int s = 0; s < samplesToRead; s++)
                {
                    int sample = s % 2 == 0 ? GetHighNibble(adpcm[inIndex]) : GetLowNibble(adpcm[inIndex++]);
                    sample = sample >= 8 ? sample - 16 : sample;
                    sample = scale * sample + (hist1 * coefs[filterNum][0] >> 12) + (hist2 * coefs[filterNum][1] >> 12);
                    short finalSample = Clamp16(sample);

                    hist2 = hist1;
                    hist1 = finalSample;
                    pcm[currentSample++] = finalSample;
                }
            }
            return pcm;
        }

        public static byte[] Encode(short[] pcm, int sampleRate, int frameSize, AdxType type = AdxType.Standard, int filter = 2)
        {
            int sampleCount = pcm.Length;
            int samplesPerFrame = (frameSize - 2) * 2;
            int frameCount = sampleCount.DivideByRoundUp(samplesPerFrame);
            short[] coefs = type == AdxType.Fixed ? Coefs[filter] : CalculateCoefficients(500, sampleRate);

            var pcmBuffer = new short[samplesPerFrame + 2];
            var adpcmBuffer = new byte[frameSize];
            var adpcmOut = new byte[frameCount * frameSize];

            for (int i = 0; i < frameCount; i++)
            {
                int samplesToCopy = Math.Min(sampleCount - i * samplesPerFrame, samplesPerFrame);
                Array.Copy(pcm, i * samplesPerFrame, pcmBuffer, 2, samplesToCopy);
                Array.Clear(pcmBuffer, 2 + samplesToCopy, samplesPerFrame - samplesToCopy);

                EncodeFrame(pcmBuffer, adpcmBuffer, coefs, samplesPerFrame, type);

                if (type == AdxType.Fixed) { adpcmBuffer[0] |= (byte)(filter << 5); }

                Array.Copy(adpcmBuffer, 0, adpcmOut, i * frameSize, frameSize);
                pcmBuffer[0] = pcmBuffer[samplesPerFrame];
                pcmBuffer[1] = pcmBuffer[samplesPerFrame + 1];
            }

            return adpcmOut;
        }

        public static void EncodeFrame(short[] pcm, byte[] adpcmOut, short[] coefs, int samplesPerFrame, AdxType type)
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

            int scale = CalculateScale(maxDistance, out double gain, out int scaleOut, type == AdxType.Exponential);

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

            adpcmOut[0] = (byte)((scaleOut >> 8) & 0x1f);
            adpcmOut[1] = (byte)scaleOut;

            for (int i = 0; i < samplesPerFrame / 2; i++)
            {
                adpcmOut[i + 2] = (byte)((adpcm[i * 2] << 4) | (adpcm[i * 2 + 1] & 0xf));
            }
        }

        private static int CalculateScale(int maxDistance, out double gain, out int scaleToWrite, bool exponential = false)
        {
            int scale = (maxDistance - 1) / 7 + 1;
            if (scale > 0x1000) scale = 0x1000;
            scaleToWrite = scale - 1;

            if (exponential)
            {
                int power = 0;
                while (scaleToWrite > 0)
                {
                    power++;
                    scaleToWrite >>= 1;
                }

                scale = 1 << power;
                scaleToWrite = 12 - power;
                maxDistance = 8 * scale - 1;
            }

            gain = maxDistance == 0 ? 0 : (double)short.MaxValue / maxDistance;
            return scale;
        }

        public static void Decrypt(byte[][] adpcm, AdxKey key, int frameSize)
        {
            for (int i = 0; i < adpcm.Length; i++)
            {
                DecryptChannel(adpcm[i], key, frameSize, i, adpcm.Length);
            }
        }

        public static void DecryptChannel(byte[] adpcm, AdxKey key, int frameSize, int channelNum, int channelCount)
        {
            int xor = key.Seed;
            int frameCount = adpcm.Length.DivideByRoundUp(frameSize);

            for (int i = 0; i < channelNum; i++)
            {
                xor = (xor * key.Mult + key.Inc) & 0x7fff;
            }

            for (int i = 0; i < frameCount; i++)
            {
                int pos = i * frameSize;
                if (adpcm[pos] != 0 || adpcm[pos + 1] != 0)
                {
                    adpcm[pos] ^= (byte)(xor >> 8);
                    adpcm[pos] &= 0x1f;
                    adpcm[pos + 1] ^= (byte)xor;
                }

                for (int c = 0; c < channelCount; c++)
                {
                    xor = (xor * key.Mult + key.Inc) & 0x7fff;
                }
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

        private static readonly short[][] Coefs = {
            new short[] {0, 0},
            new short[] {0x0F00, 0},
            new short[] {0x1CC0, unchecked((short)0xF300)},
            new short[] {0x1880, unchecked((short)0xF240)}
        };

        public static sbyte Clamp4(int value)
        {
            if (value > 7)
                return 7;
            if (value < -8)
                return -8;
            return (sbyte)value;
        }
    }

    public class AdxKey
    {
        public AdxKey(int seed, int mult, int inc)
        {
            Seed = seed;
            Mult = mult;
            Inc = inc;
        }

        public AdxKey(ulong keyCode)
        {
            keyCode--;
            Seed = (int)((keyCode >> 27) & 0x7fff);
            Mult = (int)(4 * ((keyCode >> 14) & 0x1FFF) | 1);
            Inc = (int)(2 * (keyCode & 0x3FFF) | 1);
        }

        public int Seed { get; }
        public int Mult { get; }
        public int Inc { get; }

        public ulong KeyCode
        {
            get
            {
                long seed = Seed << 27;
                long mult = Mult / 4 << 14;
                long inc = Inc / 2;
                return (ulong)(seed | mult | inc) + 1;
            }
        }

        public override string ToString()
        {
            return $"Seed - {Seed:x4} Multiplier - {Mult:x4} Increment - {Inc:x4}";
        }
    }
}
