using System;
using System.Linq;
using VGAudio.Codecs.GcAdpcm;
using VGAudio.Codecs.Pcm8;
using VGAudio.Formats.GcAdpcm;
using VGAudio.Formats.Pcm16;
using VGAudio.Formats.Pcm8;
using VGAudio.Utilities;

namespace VGAudio.Tests
{
    public static class GenerateAudio
    {
        private static double[] Frequencies { get; } = { 261.63, 329.63, 392, 523.25, 659.25, 783.99, 1046.50, 130.81 };

        /// <summary>
        /// Generates a sine wave.
        /// </summary>
        /// <param name="sampleCount">The number of samples to generate.</param>
        /// <param name="frequency">The frequency, in Hz, of the wave.</param>
        /// <param name="sampleRate">The sample rate of the sine wave.</param>
        /// <returns>The generated sine wave.</returns>
        public static short[] GenerateSineWave(int sampleCount, double frequency, int sampleRate)
        {
            var wave = new short[sampleCount];
            var c = 2 * Math.PI * frequency / sampleRate;
            for (int i = 0; i < sampleCount; i++)
            {
                wave[i] = (short)(short.MaxValue * Math.Sin(c * i));
            }

            return wave;
        }

        public static short[][] GeneratePcmSineWaveChannels(int sampleCount, int channelCount, int sampleRate)
        {
            var channels = new short[channelCount][];

            for (int i = 0; i < channelCount; i++)
            {
                channels[i] = GenerateSineWave(sampleCount, Frequencies[i], sampleRate);
            }

            return channels;
        }

        public static Pcm16Format GeneratePcmSineWave(int sampleCount, int channelCount, int sampleRate)
        {
            short[][] channels = GeneratePcmSineWaveChannels(sampleCount, channelCount, sampleRate);
            return new Pcm16Format(channels, sampleRate);
        }

        public static Pcm8Format GeneratePcm8SineWave(int sampleCount, int channelCount, int sampleRate)
        {
            byte[][] channels = GeneratePcmSineWaveChannels(sampleCount, channelCount, sampleRate)
                .Select(Pcm8Codec.EncodeSigned).ToArray();
            return new Pcm8Format(channels, sampleRate);
        }

        public static Pcm8SignedFormat GeneratePcm8SignedSineWave(int sampleCount, int channelCount, int sampleRate)
        {
            byte[][] channels = GeneratePcmSineWaveChannels(sampleCount, channelCount, sampleRate)
                .Select(Pcm8Codec.EncodeSigned).ToArray();
            return new Pcm8SignedFormat(channels, sampleRate);
        }

        public static GcAdpcmFormat GenerateAdpcmSineWave(int sampleCount, int channelCount, int sampleRate)
        {
            Pcm16Format pcm = GeneratePcmSineWave(sampleCount, channelCount, sampleRate);
            return new GcAdpcmFormat().EncodeFromPcm16(pcm);
        }

        public static GcAdpcmFormat GenerateAdpcmEmpty(int sampleCount, int channelCount, int sampleRate, int samplesPerSeekTableEntry = 0x3800)
        {
            var channels = new GcAdpcmChannel[channelCount];

            for (int i = 0; i < channelCount; i++)
            {
                channels[i] =
                    new GcAdpcmChannelBuilder(new byte[GcAdpcmHelpers.SampleCountToByteCount(sampleCount)], new short[16], sampleCount)
                        .WithSeekTable(new short[sampleCount.DivideByRoundUp(samplesPerSeekTableEntry) * 2], samplesPerSeekTableEntry, true)
                        .Build();
            }

            var adpcm = new GcAdpcmFormat(channels, sampleRate);

            return adpcm;
        }

        public static (byte[] adpcm, short[] coefs) GenerateAdpcmEmpty(int sampleCount)
        {
            var adpcm = new byte[GcAdpcmHelpers.SampleCountToByteCount(sampleCount)];
            var coefs = new short[16];
            return (adpcm, coefs);
        }

        public static GcAdpcmChannel[] GenerateAdpcmChannelsEmpty(int sampleCount, int channelCount)
        {
            var channels = new GcAdpcmChannel[channelCount];
            for (int i = 0; i < channelCount; i++)
            {
                var adpcm = new byte[GcAdpcmHelpers.SampleCountToByteCount(sampleCount)];
                var coefs = new short[16];
                channels[i] = new GcAdpcmChannelBuilder(adpcm, coefs, sampleCount).Build();
            }
            return channels;
        }

        public static short[] GenerateAccendingShorts(int start, int count)
        {
            var pcm = new short[count];
            for (int i = 0; i < count; i++)
            {
                pcm[i] = (short)(i + 1 + start);
            }
            return pcm;
        }
    }
}
