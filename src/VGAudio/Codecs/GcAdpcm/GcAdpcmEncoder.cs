using System;
using VGAudio.Utilities;
using static VGAudio.Codecs.GcAdpcm.GcAdpcmMath;
using static VGAudio.Utilities.Helpers;

namespace VGAudio.Codecs.GcAdpcm
{
    /// <summary>
    /// This class contains functions used for encoding
    /// Nintendo's 4-bit ADPCM audio format.
    /// </summary>
    public static class GcAdpcmEncoder
    {
        public static byte[] Encode(short[] pcm, short[] coefs, GcAdpcmParameters config = null)
        {
            config = config ?? new GcAdpcmParameters();
            int sampleCount = config.SampleCount == -1 ? pcm.Length : config.SampleCount;
            var adpcm = new byte[SampleCountToByteCount(sampleCount)];

            /* Execute encoding-predictor for each frame */
            var pcmBuffer = new short[2 + SamplesPerFrame];
            var adpcmBuffer = new byte[BytesPerFrame];

            pcmBuffer[0] = config.History2;
            pcmBuffer[1] = config.History1;

            int frameCount = sampleCount.DivideByRoundUp(SamplesPerFrame);
            var buffers = new AdpcmEncodeBuffers();

            for (int frame = 0; frame < frameCount; frame++)
            {
                int samplesToCopy = Math.Min(sampleCount - frame * SamplesPerFrame, SamplesPerFrame);
                Array.Copy(pcm, frame * SamplesPerFrame, pcmBuffer, 2, samplesToCopy);
                Array.Clear(pcmBuffer, 2 + samplesToCopy, SamplesPerFrame - samplesToCopy);

                DspEncodeFrame(pcmBuffer, SamplesPerFrame, adpcmBuffer, coefs, buffers);

                Array.Copy(adpcmBuffer, 0, adpcm, frame * BytesPerFrame, SampleCountToByteCount(samplesToCopy));

                pcmBuffer[0] = pcmBuffer[14];
                pcmBuffer[1] = pcmBuffer[15];
                config.Progress?.ReportAdd(1);
            }

            return adpcm;
        }

        public static void DspEncodeFrame(short[] pcmInOut, int sampleCount, byte[] adpcmOut, short[] coefsIn,
            AdpcmEncodeBuffers b = null)
        {
            b = b ?? new AdpcmEncodeBuffers();

            for (int i = 0; i < 8; i++)
            {
                b.Coefs[i][0] = coefsIn[i * 2];
                b.Coefs[i][1] = coefsIn[i * 2 + 1];
            }

            /* Iterate through each coef set, finding the set with the smallest error */
            for (int i = 0; i < 8; i++)
            {
                DspEncodeCoef(pcmInOut, sampleCount, b.Coefs[i], b.PcmOut[i], b.AdpcmOut[i], out b.Scale[i],
                    out b.TotalDistance[i]);
            }

            int bestCoef = 0;

            double min = double.MaxValue;
            for (int i = 0; i < 8; i++)
            {
                if (b.TotalDistance[i] < min)
                {
                    min = b.TotalDistance[i];
                    bestCoef = i;
                }
            }

            /* Write converted samples */
            for (int s = 0; s < sampleCount; s++)
                pcmInOut[s + 2] = (short)b.PcmOut[bestCoef][s + 2];

            /* Write predictor and scale */
            adpcmOut[0] = CombineNibbles(bestCoef, b.Scale[bestCoef]);

            /* Zero remaining samples */
            for (int s = sampleCount; s < 14; s++)
                b.AdpcmOut[bestCoef][s] = 0;

            /* Write output samples */
            for (int i = 0; i < 7; i++)
            {
                adpcmOut[i + 1] = CombineNibbles(b.AdpcmOut[bestCoef][i * 2], b.AdpcmOut[bestCoef][i * 2 + 1]);
            }
        }

        private static void DspEncodeCoef(short[] pcmIn, int sampleCount, short[] coefs, int[] pcmOut,
            int[] adpcmOut, out int scalePower, out double totalDistance)
        {
            int maxOverflow;
            int maxDistance = 0;

            // Set history values
            pcmOut[0] = pcmIn[0];
            pcmOut[1] = pcmIn[1];

            // Encode the frame with a scale of 1
            for (int s = 0; s < sampleCount; s++)
            {
                int inputSample = pcmIn[s + 2];
                int predictedSample = (pcmIn[s] * coefs[1] + pcmIn[s + 1] * coefs[0]) / 2048;
                int distance = inputSample - predictedSample;
                distance = Clamp16(distance);
                if (Math.Abs(distance) > Math.Abs(maxDistance))
                    maxDistance = distance;
            }

            // Use the maximum distance of the encoded frame to find a scale that will fit the current frame
            scalePower = 0;
            while (scalePower <= 12 && (maxDistance > 7 || maxDistance < -8))
            {
                maxDistance /= 2;
                scalePower++;
            }
            scalePower = scalePower <= 1 ? -1 : scalePower - 2;

            // Try increasing scales until the encoded frame is in the range of a 4-bit value
            do
            {
                scalePower++;
                int scale = (1 << scalePower) * 2048;
                totalDistance = 0;
                maxOverflow = 0;

                for (int s = 0; s < sampleCount; s++)
                {
                    // Calculate the difference between the actual and predicted samples
                    int inputSample = pcmIn[s + 2] * 2048;
                    int predictedSample = pcmOut[s] * coefs[1] + pcmOut[s + 1] * coefs[0];
                    int distance = inputSample - predictedSample;
                    // Scale to 4-bits, and round to the nearest sample
                    // The official encoder does the casting this way, so match that behavior
                    int unclampedAdpcmSample = (distance > 0)
                        ? (int)((double)((float)distance / scale) + 0.4999999f)
                        : (int)((double)((float)distance / scale) - 0.4999999f);

                    int adpcmSample = Clamp4(unclampedAdpcmSample);
                    if (adpcmSample != unclampedAdpcmSample)
                    {
                        int overflow = Math.Abs(unclampedAdpcmSample - adpcmSample);
                        if (overflow > maxOverflow) maxOverflow = overflow;
                    }

                    adpcmOut[s] = adpcmSample;

                    // Decode sample to use as history
                    int decodedDistance = adpcmSample * scale;
                    int correctedSample = predictedSample + decodedDistance;
                    int scaledSample = (correctedSample + 1024) >> 11;
                    // Clamp and store
                    pcmOut[s + 2] = Clamp16(scaledSample);
                    // Accumulate distance
                    double actualDistance = pcmIn[s + 2] - pcmOut[s + 2];
                    totalDistance += actualDistance * actualDistance;
                }

                for (int x = maxOverflow + 8; x > 256; x >>= 1)
                    if (++scalePower >= 12)
                        scalePower = 11;

            } while (scalePower < 12 && maxOverflow > 1);
        }

        public class AdpcmEncodeBuffers
        {
            public short[][] Coefs { get; } = new short[8][];
            public int[][] PcmOut { get; } = new int[8][];
            public int[][] AdpcmOut { get; } = new int[8][];
            public int[] Scale { get; } = new int[8];
            public double[] TotalDistance { get; } = new double[8];

            public AdpcmEncodeBuffers()
            {
                for (int i = 0; i < 8; i++)
                {
                    PcmOut[i] = new int[16];
                    AdpcmOut[i] = new int[14];
                    Coefs[i] = new short[2];
                }
            }
        }
    }
}
