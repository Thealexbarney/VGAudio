using System;
using System.Collections.Generic;
using System.Linq;
using DspAdpcm.Adpcm;
using DspAdpcm.Pcm;

namespace DspAdpcm.Benchmark
{
    public static class GenerateAudio
    {
        private static double[] Frequencies { get; } = { 261.63, 329.63, 392, 523.25, 659.25, 783.99, 1046.50, 130.81 };

        /// <summary>
        /// Generates a sine wave.
        /// </summary>
        /// <param name="samples">The number of samples to generate.</param>
        /// <param name="frequency">The frequency, in Hz, of the wave.</param>
        /// <param name="sampleRate">The sample rate of the sine wave.</param>
        /// <returns>The generated sine wave.</returns>
        public static short[] GenerateSineWave(int samples, double frequency, int sampleRate)
        {
            var wave = new short[samples];
            var c = 2 * Math.PI * frequency / sampleRate;
            for (int i = 0; i < samples; i++)
            {
                wave[i] = (short)(short.MaxValue * Math.Sin(c * i));
            }

            return wave;
        }

        public static PcmStream GeneratePcmSineWave(int samples, int channels, int sampleRate)
        {
            IEnumerable<double> frequencies = Frequencies.Take(channels);

            var pcm = new PcmStream(samples, sampleRate);
            foreach (double frequency in frequencies)
            {
                pcm.Channels.Add(new PcmChannel(samples, GenerateSineWave(samples, frequency, sampleRate)));
            }

            return pcm;
        }

        public static AdpcmStream GenerateAdpcmSineWave(int samples, int channels, int sampleRate)
        {
            PcmStream pcm = GeneratePcmSineWave(samples, channels, sampleRate);
            return Encode.PcmToAdpcm(pcm);
        }

        public static AdpcmStream GenerateAdpcmEmpty(int samples, int channels, int sampleRate, int samplesPerSeekTableEntry = 0x3800)
        {
            var adpcm = new AdpcmStream(samples, sampleRate);

            for (int i = 0; i < channels; i++)
            {
                adpcm.Channels.Add(new AdpcmChannel(samples)
                {
                    Coefs = new short[16],
                    SeekTable = new short[samples.DivideByRoundUp(samplesPerSeekTableEntry) * 2],
                    SelfCalculatedSeekTable = true,
                    SamplesPerSeekTableEntry = samplesPerSeekTableEntry
                });
            }

            return adpcm;
        }
    }
}
