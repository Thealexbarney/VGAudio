using System;
using System.Linq;
using VGAudio.Codecs.GcAdpcm;
using VGAudio.Formats.GcAdpcm;
using VGAudio.Formats.Pcm16;
using VGAudio.Utilities;

namespace VGAudio.Benchmark
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

        public static Pcm16Format GeneratePcmSineWave(int sampleCount, int channelCount, int sampleRate)
        {
            double[] frequencies = Frequencies.Take(channelCount).ToArray();
            var channels = new short[channelCount][];

            for (int i = 0; i < channelCount; i++)
            {
                channels[i] = GenerateSineWave(sampleCount, frequencies[i], sampleRate);
            }

            return new Pcm16Format(channels, sampleRate);
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
                var builder =  new GcAdpcmChannelBuilder(new byte[GcAdpcmHelpers.SampleCountToByteCount(sampleCount)], new short[16], sampleCount);
                builder.WithSeekTable(new short[sampleCount.DivideByRoundUp(samplesPerSeekTableEntry) * 2], samplesPerSeekTableEntry, true);
                channels[i] = builder.Build();
            }

            var adpcm = new GcAdpcmFormat(channels, sampleRate);

            return adpcm;
        }
    }
}
