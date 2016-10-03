using System;
using DspAdpcm.Adpcm;
using DspAdpcm.Pcm;

namespace DspAdpcm.Tests
{
    public class GenerateAudio
    {
        /// <summary>
        /// Generates a sine wave.
        /// </summary>
        /// <param name="samples">The number of samples to generate.</param>
        /// <param name="frequency">The frequency, in Hz, of the wave.</param>
        /// <param name="sampleRate">The sample rate of the sine wave.</param>
        /// <returns>The generated sine wave.</returns>
        public static short[] GenerateSineWave(int samples, int frequency, int sampleRate)
        {
            var wave = new short[samples];
            var c = 2 * Math.PI * frequency / sampleRate;
            for (int i = 0; i < samples; i++)
            {
                wave[i] = (short)(short.MaxValue * Math.Sin(c * i));
            }

            return wave;
        }

        public static PcmStream GeneratePcmSineWave(int samples, int frequency)
        {
            int sampleRate = 48000;

            var pcm = new PcmStream(samples, sampleRate);
            pcm.Channels.Add(new PcmChannel(samples, GenerateSineWave(samples, frequency, sampleRate)));

            return pcm;
        }

        public static AdpcmStream GenerateAdpcmSineWave(int samples, int frequency)
        {
            PcmStream pcm = GeneratePcmSineWave(samples, frequency);
            var adpcm = Encode.PcmToAdpcm(pcm);

            return adpcm;
        }

        public static AdpcmStream GenerateAdpcmEmpty(int samples)
        {
            var adpcm = new AdpcmStream(samples, 48000);
            adpcm.Channels.Add(new AdpcmChannel(samples) { Coefs = new short[16] });

            return adpcm;
        }
    }
}
