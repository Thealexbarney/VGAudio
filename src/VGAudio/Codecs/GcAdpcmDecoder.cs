using System;
using VGAudio.Formats.GcAdpcm;
using VGAudio.Utilities;
using static VGAudio.Formats.GcAdpcm.GcAdpcmHelpers;
using static VGAudio.Utilities.Helpers;

namespace VGAudio.Codecs
{
    public static class GcAdpcmDecoder
    {
        public static short[] Decode(byte[] adpcm, short[] coefficients, int length)
        {
            int nibbles = adpcm.Length * 2;
            int samples = NibbleCountToSampleCount(nibbles);
            var audio = new GcAdpcmChannel(samples, adpcm) { Coefs = coefficients };
            return Decode(audio, 0, length);
        }

        public static short[] Decode(GcAdpcmChannel audio, int length) => Decode(audio, 0, length);

        public static short[] Decode(GcAdpcmChannel audio, int startSample, int length, bool includeHistorySamples = false)
        {
            if (audio == null)
            {
                throw new ArgumentNullException(nameof(audio));
            }

            if (startSample < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startSample), startSample, "Argument must be non-negative");
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), length, "Argument must be non-negative");
            }

            if (audio.SampleCount - startSample < length)
            {
                throw new ArgumentException("startSample and length were out of bounds for the array, or length is" +
                                            " greater than the number of samples from the startSample to the end of the ADPCM data.");
            }

            if (includeHistorySamples)
            {
                startSample -= 2;
                length += 2;
            }

            var pcm = new short[length];

            if (length == 0)
            {
                return pcm;
            }

            byte[] adpcm = audio.GetAudioData();

            var history = audio.GetStartingHistory(startSample);

            short hist1 = history.Item2;
            short hist2 = history.Item3;
            int currentSample = history.Item1;

            int outIndex = 0;
            int inIndex = currentSample / SamplesPerFrame * BytesPerFrame;

            if (startSample == currentSample - 2)
            {
                pcm[outIndex++] = hist2;
                pcm[outIndex++] = hist1;
            }
            if (startSample == currentSample - 1)
            {
                pcm[outIndex++] = hist1;
            }

            int endSample = startSample + length;
            int frameCount = (endSample - currentSample).DivideByRoundUp(SamplesPerFrame);

            for (int i = 0; i < frameCount; i++)
            {
                byte ps = adpcm[inIndex++];
                int scale = 1 << GetLowNibble(ps);
                int predictor = GetHighNibble(ps);
                short coef1 = audio.Coefs[predictor * 2];
                short coef2 = audio.Coefs[predictor * 2 + 1];

                int samplesToRead = Math.Min(SamplesPerFrame, endSample - currentSample);

                for (int s = 0; s < samplesToRead; s++)
                {
                    int sample = s % 2 == 0 ? GetHighNibble(adpcm[inIndex]) : GetLowNibble(adpcm[inIndex++]);
                    sample = sample >= 8 ? sample - 16 : sample;
                    sample = (((scale * sample) << 11) + 1024 + (coef1 * hist1 + coef2 * hist2)) >> 11;
                    short finalSample = Clamp16(sample);

                    hist2 = hist1;
                    hist1 = finalSample;

                    if (currentSample++ >= startSample)
                    {
                        pcm[outIndex++] = finalSample;
                    }
                }
            }
            return pcm;
        }

        public static byte GetPredictorScale(byte[] adpcm, int sample)
        {
            return adpcm[sample / SamplesPerFrame * BytesPerFrame];
        }
    }
}
