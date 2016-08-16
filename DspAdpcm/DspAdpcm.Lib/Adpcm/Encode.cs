/*Reference:
 * https://github.com/libertyernie/brawltools/blob/master/BrawlLib/Wii/Audio/AudioConverter.cs
 * https://github.com/jackoalan/gc-dspadpcm-encode
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DspAdpcm.Lib.Pcm;
using static DspAdpcm.Lib.Helpers;

namespace DspAdpcm.Lib.Adpcm
{
    /// <summary>
    /// This class contains functions for encoding and
    /// decoding Nintendo's 4-bit ADPCM audio format.
    /// </summary>
    public static class Encode
    {
        private static void InnerProductMerge(double[] vecOut, short[] pcmBuf)
        {
            for (int i = 0; i <= 2; i++)
            {
                vecOut[i] = 0.0f;
                for (int x = 0; x < 14; x++)
                    vecOut[i] -= pcmBuf[14 + x - i] * pcmBuf[14 + x];
            }
        }

        private static void OuterProductMerge(double[][] mtxOut, short[] pcmBuf)
        {
            for (int x = 1; x <= 2; x++)
                for (int y = 1; y <= 2; y++)
                {
                    mtxOut[x][y] = 0.0;
                    for (int z = 0; z < 14; z++)
                        mtxOut[x][y] += pcmBuf[14 + z - x] * pcmBuf[14 + z - y];
                }
        }

        private static bool AnalyzeRanges(double[][] mtx, int[] vecIdxsOut)
        {
            double[] recips = new double[3];
            double val, tmp, min, max;

            /* Get greatest distance from zero */
            for (int x = 1; x <= 2; x++)
            {
                val = Math.Max(Math.Abs(mtx[x][1]), Math.Abs(mtx[x][2]));
                if (val < double.Epsilon)
                    return true;

                recips[x] = 1.0 / val;
            }

            int maxIndex = 0;
            for (int i = 1; i <= 2; i++)
            {
                for (int x = 1; x < i; x++)
                {
                    tmp = mtx[x][i];
                    for (int y = 1; y < x; y++)
                        tmp -= mtx[x][y] * mtx[y][i];
                    mtx[x][i] = tmp;
                }

                val = 0.0;
                for (int x = i; x <= 2; x++)
                {
                    tmp = mtx[x][i];
                    for (int y = 1; y < i; y++)
                        tmp -= mtx[x][y] * mtx[y][i];

                    mtx[x][i] = tmp;
                    tmp = Math.Abs(tmp) * recips[x];
                    if (tmp >= val)
                    {
                        val = tmp;
                        maxIndex = x;
                    }
                }

                if (maxIndex != i)
                {
                    for (int y = 1; y <= 2; y++)
                    {
                        tmp = mtx[maxIndex][y];
                        mtx[maxIndex][y] = mtx[i][y];
                        mtx[i][y] = tmp;
                    }
                    recips[maxIndex] = recips[i];
                }

                vecIdxsOut[i] = maxIndex;

                if (i != 2)
                {
                    tmp = 1.0 / mtx[i][i];
                    for (int x = i + 1; x <= 2; x++)
                        mtx[x][i] *= tmp;
                }
            }

            /* Get range */
            min = 1.0e10;
            max = 0.0;
            for (int i = 1; i <= 2; i++)
            {
                tmp = Math.Abs(mtx[i][i]);
                if (tmp < min)
                    min = tmp;
                if (tmp > max)
                    max = tmp;
            }

            if (min / max < 1.0e-10)
                return true;

            return false;
        }

        private static void BidirectionalFilter(double[][] mtx, int[] vecIdxs, double[] vecOut)
        {
            double tmp;

            for (int i = 1, x = 0; i <= 2; i++)
            {
                int index = vecIdxs[i];
                tmp = vecOut[index];
                vecOut[index] = vecOut[i];
                if (x != 0)
                    for (int y = x; y <= i - 1; y++)
                        tmp -= vecOut[y] * mtx[i][y];
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                else if (tmp != 0.0)
                    x = i;
                vecOut[i] = tmp;
            }

            for (int i = 2; i > 0; i--)
            {
                tmp = vecOut[i];
                for (int y = i + 1; y <= 2; y++)
                    tmp -= vecOut[y] * mtx[i][y];
                vecOut[i] = tmp / mtx[i][i];
            }

            vecOut[0] = 1.0;
        }

        private static bool QuadraticMerge(double[] inOutVec)
        {
            double v2 = inOutVec[2];
            double tmp = 1.0 - (v2 * v2);

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (tmp == 0.0)
                return true;

            double v0 = (inOutVec[0] - (v2 * v2)) / tmp;
            double v1 = (inOutVec[1] - (inOutVec[1] * v2)) / tmp;

            inOutVec[0] = v0;
            inOutVec[1] = v1;

            return Math.Abs(v1) > 1.0;
        }

        private static void FinishRecord(double[] inR, double[,] outR, int row)
        {
            for (int z = 1; z <= 2; z++)
            {
                if (inR[z] >= 1.0)
                    inR[z] = 0.9999999999;
                else if (inR[z] <= -1.0)
                    inR[z] = -0.9999999999;
            }
            outR[row, 0] = 1.0;
            outR[row, 1] = (inR[2] * inR[1]) + inR[1];
            outR[row, 2] = inR[2];
        }

        private static void FinishRecord(double[] inR, double[] outR)
        {
            for (int z = 1; z <= 2; z++)
            {
                if (inR[z] >= 1.0)
                    inR[z] = 0.9999999999;
                else if (inR[z] <= -1.0)
                    inR[z] = -0.9999999999;
            }
            outR[0] = 1.0;
            outR[1] = (inR[2] * inR[1]) + inR[1];
            outR[2] = inR[2];
        }

        private static void MatrixFilter(double[,] src, int row, double[] dst)
        {
            double[][] mtx = new double[3][];
            for (int i = 0; i < mtx.Length; i++)
            {
                mtx[i] = new double[3];
            }

            mtx[2][0] = 1.0;
            for (int i = 1; i <= 2; i++)
                mtx[2][i] = -src[row, i];

            for (int i = 2; i > 0; i--)
            {
                double val = 1.0 - (mtx[i][i] * mtx[i][i]);
                for (int y = 1; y <= i; y++)
                    mtx[i - 1][y] = ((mtx[i][i] * mtx[i][y]) + mtx[i][y]) / val;
            }

            dst[0] = 1.0;
            for (int i = 1; i <= 2; i++)
            {
                dst[i] = 0.0;
                for (int y = 1; y <= i; y++)
                    dst[i] += mtx[i][y] * dst[i - y];
            }
        }

        private static void MergeFinishRecord(double[] src, double[] dst)
        {
            double[] tmp = new double[3];
            double val = src[0];

            dst[0] = 1.0;
            for (int i = 1; i <= 2; i++)
            {
                double v2 = 0.0;
                for (int y = 1; y < i; y++)
                    v2 += dst[y] * src[i - y];

                if (val > 0.0)
                    dst[i] = -(v2 + src[i]) / val;
                else
                    dst[i] = 0.0;

                tmp[i] = dst[i];

                for (int y = 1; y < i; y++)
                    dst[y] += dst[i] * dst[i - y];

                val *= 1.0 - (dst[i] * dst[i]);
            }

            FinishRecord(tmp, dst);
        }

        private static double ContrastVectors(double[] source1, double[,] source2, int row)
        {
            double val = (source2[row, 2] * source2[row, 1] + -source2[row, 1]) / (1.0 - source2[row, 2] * source2[row, 2]);
            double val1 = (source1[0] * source1[0]) + (source1[1] * source1[1]) + (source1[2] * source1[2]);
            double val2 = (source1[0] * source1[1]) + (source1[1] * source1[2]);
            double val3 = source1[0] * source1[2];
            return val1 + (2.0 * val * val2) + (2.0 * (-source2[row, 1] * val + -source2[row, 2]) * val3);
        }

        private static void FilterRecords(double[][] vecBest, int exp, double[,] records, int recordCount)
        {
            double[][] bufferList = new double[8][];
            for (int i = 0; i < bufferList.Length; i++)
            {
                bufferList[i] = new double[3];
            }

            int[] buffer1 = new int[8];
            double[] buffer2 = new double[3];

            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < exp; y++)
                {
                    buffer1[y] = 0;
                    for (int i = 0; i <= 2; i++)
                        bufferList[y][i] = 0.0;
                }
                for (int z = 0; z < recordCount; z++)
                {
                    var index = 0;
                    var value = 1.0e30;
                    for (int i = 0; i < exp; i++)
                    {
                        double tempVal = ContrastVectors(vecBest[i], records, z);
                        if (tempVal < value)
                        {
                            value = tempVal;
                            index = i;
                        }
                    }
                    buffer1[index]++;
                    MatrixFilter(records, z, buffer2);
                    for (int i = 0; i <= 2; i++)
                        bufferList[index][i] += buffer2[i];
                }

                for (int i = 0; i < exp; i++)
                    if (buffer1[i] > 0)
                        for (int y = 0; y <= 2; y++)
                            bufferList[i][y] /= buffer1[i];

                for (int i = 0; i < exp; i++)
                    MergeFinishRecord(bufferList[i], vecBest[i]);
            }
        }

        private static short[] DspCorrelateCoefs(IEnumerable<short> source, int samples)
        {
            int numFrames = (samples + 13) / 14;

            short[] pcmHistBuffer = new short[28];

            short[] coefs = new short[16];

            double[] vec1 = new double[3];
            double[] vec2 = new double[3];

            double[][] mtx = new double[3][];
            for (int i = 0; i < mtx.Length; i++)
            {
                mtx[i] = new double[3];
            }

            int[] vecIdxs = new int[3];

            double[,] records = new double[numFrames * 2, 3];

            int recordCount = 0;

            double[][] vecBest = new double[8][];
            for (int i = 0; i < vecBest.Length; i++)
            {
                vecBest[i] = new double[3];
            }

            /* Iterate though one block at a time */
            foreach (var block in source.Batch(14))
            {
                Array.Copy(block, 0, pcmHistBuffer, 14, 14);

                InnerProductMerge(vec1, pcmHistBuffer);
                if (Math.Abs(vec1[0]) > 10.0)
                {
                    OuterProductMerge(mtx, pcmHistBuffer);
                    if (!AnalyzeRanges(mtx, vecIdxs))
                    {
                        BidirectionalFilter(mtx, vecIdxs, vec1);
                        if (!QuadraticMerge(vec1))
                        {
                            FinishRecord(vec1, records, recordCount);
                            recordCount++;
                        }
                    }
                }

                Array.Copy(pcmHistBuffer, 14, pcmHistBuffer, 0, 14);
            }

            vec1[0] = 1.0;
            vec1[1] = 0.0;
            vec1[2] = 0.0;

            for (int z = 0; z < recordCount; z++)
            {
                MatrixFilter(records, z, vecBest[0]);
                for (int y = 1; y <= 2; y++)
                    vec1[y] += vecBest[0][y];
            }
            for (int y = 1; y <= 2; y++)
                vec1[y] /= recordCount;

            MergeFinishRecord(vec1, vecBest[0]);


            int exp = 1;
            for (int w = 0; w < 3;)
            {
                vec2[0] = 0.0;
                vec2[1] = -1.0;
                vec2[2] = 0.0;
                for (int i = 0; i < exp; i++)
                    for (int y = 0; y <= 2; y++)
                        vecBest[exp + i][y] = (0.01 * vec2[y]) + vecBest[i][y];
                ++w;
                exp = 1 << w;
                FilterRecords(vecBest, exp, records, recordCount);
            }

            /* Write output */
            for (int z = 0; z < 8; z++)
            {
                double d;
                d = -vecBest[z][1] * 2048.0;
                if (d > 0.0)
                    coefs[z * 2] = (d > short.MaxValue) ? short.MaxValue : (short)Math.Round(d);
                else
                    coefs[z * 2] = (d < short.MinValue) ? short.MinValue : (short)Math.Round(d);

                d = -vecBest[z][2] * 2048.0;
                if (d > 0.0)
                    coefs[z * 2 + 1] = (d > short.MaxValue) ? short.MaxValue : (short)Math.Round(d);
                else
                    coefs[z * 2 + 1] = (d < short.MinValue) ? short.MinValue : (short)Math.Round(d);
            }
            return coefs;
        }

        internal static void DspEncodeFrame(short[] pcmInOut, int sampleCount, byte[] adpcmOut, short[] coefsIn)
        {
            short[][] coefs = new short[8][];
            for (int i = 0; i < 8; i++)
            {
                coefs[i] = new short[2];
                coefs[i][0] = coefsIn[i * 2];
                coefs[i][1] = coefsIn[i * 2 + 1];
            }

            int[][] inSamples = new int[8][];
            int[][] outSamples = new int[8][];
            for (int i = 0; i < 8; i++)
            {
                inSamples[i] = new int[16];
                outSamples[i] = new int[14];
            }

            int[] scale = new int[8];
            double[] distAccum = new double[8];

            /* Iterate through each coef set, finding the set with the smallest error */
            for (int i = 0; i < 8; i++)
            {
                int v1, v2, v3;
                int index;
                int distance = 0;

                /* Set yn values */
                inSamples[i][0] = pcmInOut[0];
                inSamples[i][1] = pcmInOut[1];

                /* Round and clamp samples for this coef set */
                for (int s = 0; s < sampleCount; s++)
                {
                    /* Multiply previous samples by coefs */
                    inSamples[i][s + 2] = v1 = ((pcmInOut[s] * coefs[i][1]) + (pcmInOut[s + 1] * coefs[i][0])) / 2048;
                    /* Subtract from current sample */
                    v2 = pcmInOut[s + 2] - v1;
                    /* Clamp */
                    v3 = Clamp16(v2);
                    /* Compare distance */
                    if (Math.Abs(v3) > Math.Abs(distance))
                        distance = v3;
                }

                /* Set initial scale */
                for (scale[i] = 0; (scale[i] <= 12) && ((distance > 7) || (distance < -8)); scale[i]++, distance /= 2) { }
                scale[i] = (scale[i] <= 1) ? -1 : scale[i] - 2;

                do
                {
                    scale[i]++;
                    distAccum[i] = 0;
                    index = 0;

                    for (int s = 0; s < sampleCount; s++)
                    {
                        /* Multiply previous */
                        v1 = ((inSamples[i][s] * coefs[i][1]) + (inSamples[i][s + 1] * coefs[i][0]));
                        /* Evaluate from real sample */
                        v2 = ((pcmInOut[s + 2] << 11) - v1) / 2048;
                        /* Round to nearest sample */
                        v3 = (v2 > 0) ? (int)((double)v2 / (1 << scale[i]) + 0.4999999f) : (int)((double)v2 / (1 << scale[i]) - 0.4999999f);

                        /* Clamp sample and set index */
                        if (v3 < -8)
                        {
                            if (index < (v3 = -8 - v3))
                                index = v3;
                            v3 = -8;
                        }
                        else if (v3 > 7)
                        {
                            if (index < (v3 -= 7))
                                index = v3;
                            v3 = 7;
                        }

                        /* Store result */
                        outSamples[i][s] = v3;

                        /* Round and expand */
                        v1 = (v1 + ((v3 * (1 << scale[i])) << 11) + 1024) >> 11;
                        /* Clamp and store */
                        inSamples[i][s + 2] = v2 = Clamp16(v1);
                        /* Accumulate distance */
                        v3 = pcmInOut[s + 2] - v2;
                        distAccum[i] += v3 * (double)v3;
                    }

                    for (int x = index + 8; x > 256; x >>= 1)
                        if (++scale[i] >= 12)
                            scale[i] = 11;

                } while ((scale[i] < 12) && (index > 1));
            }

            int bestIndex = 0;

            double min = double.MaxValue;
            for (int i = 0; i < 8; i++)
            {
                if (distAccum[i] < min)
                {
                    min = distAccum[i];
                    bestIndex = i;
                }
            }

            /* Write converted samples */
            for (int s = 0; s < sampleCount; s++)
                pcmInOut[s + 2] = (short)inSamples[bestIndex][s + 2];

            /* Write ps */
            adpcmOut[0] = (byte)((bestIndex << 4) | (scale[bestIndex] & 0xF));

            /* Zero remaining samples */
            for (int s = sampleCount; s < 14; s++)
                outSamples[bestIndex][s] = 0;

            /* Write output samples */
            for (int y = 0; y < 7; y++)
            {
                adpcmOut[y + 1] = (byte)((outSamples[bestIndex][y * 2] << 4) | (outSamples[bestIndex][y * 2 + 1] & 0xF));
            }
        }
        internal static void CalculateLoopContext(IEnumerable<AdpcmChannel> channels, int loopStart)
        {
            Parallel.ForEach(channels, channel => CalculateLoopContext(channel, loopStart));
        }

        internal static void CalculateLoopContext(this AdpcmChannel audio, int loopStart)
        {
            byte ps = audio.GetPredictorScale(loopStart);
            short[] hist = audio.GetPcmAudio(loopStart, 0, true);
            audio.SetLoopContext(ps, hist[1], hist[0]);
            audio.SelfCalculatedLoopContext = true;
        }

        private static byte GetPredictorScale(this AdpcmChannel audio, int sample)
        {
            return audio.AudioByteArray[sample / SamplesPerBlock * BytesPerBlock];
        }

        private static Tuple<int, short, short> GetStartingHistory(this AdpcmChannel audio, int firstSample)
        {
            if (audio.SeekTable == null || !audio.SelfCalculatedSeekTable)
            {
                return new Tuple<int, short, short>(0, audio.Hist1, audio.Hist2);
            }

            int entry = firstSample / audio.SamplesPerSeekTableEntry;
            int sample = entry * audio.SamplesPerSeekTableEntry;
            short hist1 = audio.SeekTable[entry * 2];
            short hist2 = audio.SeekTable[entry * 2 + 1];

            return new Tuple<int, short, short>(sample, hist1, hist2);
        }

        internal static IEnumerable<short> GetPcmAudioLazy(this AdpcmChannel audio, bool includeHistorySamples = false)
        {
            int numSamples = audio.NumSamples;
            short hist1 = audio.Hist1;
            short hist2 = audio.Hist2;
            var adpcm = audio.AudioByteArray;
            int numBlocks = adpcm.Length.DivideByRoundUp(BytesPerBlock);

            int outSample = 0;
            int inByte = 0;

            if (includeHistorySamples)
            {
                yield return hist2;
                yield return hist1;
            }

            for (int i = 0; i < numBlocks; i++)
            {
                byte ps = adpcm[inByte++];
                int scale = 1 << (ps & 0xf);
                int predictor = (ps >> 4) & 0xf;
                short coef1 = audio.Coefs[predictor * 2];
                short coef2 = audio.Coefs[predictor * 2 + 1];

                for (int s = 0; s < 14; s++)
                {
                    int sample;
                    if (s % 2 == 0)
                    {
                        sample = (adpcm[inByte] >> 4) & 0xF;
                    }
                    else
                    {
                        sample = adpcm[inByte++] & 0xF;
                    }
                    sample = sample >= 8 ? sample - 16 : sample;

                    sample = (((scale * sample) << 11) + 1024 + (coef1 * hist1 + coef2 * hist2)) >> 11;
                    sample = Clamp16(sample);

                    hist2 = hist1;
                    hist1 = (short)sample;

                    yield return (short)sample;
                    if (++outSample >= numSamples)
                    {
                        yield break;
                    }
                }
            }
        }

        internal static short[] GetPcmAudio(this AdpcmChannel audio, bool includeHistorySamples = false) =>
            audio.GetPcmAudio(0, audio.NumSamples, includeHistorySamples);

        internal static short[] GetPcmAudio(this AdpcmChannel audio, int index, int count, bool includeHistorySamples = false)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Argument must be non-negative");
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, "Argument must be non-negative");
            }

            if (audio.NumSamples - index < count)
            {
                throw new ArgumentException("Offset and length were out of bounds for the array or count is" +
                                            " greater than the number of elements from index to the end of the source collection.");
            }

            var history = audio.GetStartingHistory(index);
            
            short hist1 = history.Item2;
            short hist2 = history.Item3;
            var adpcm = audio.AudioByteArray;
            int numBlocks = adpcm.Length.DivideByRoundUp(BytesPerBlock);

            short[] pcm;
            int numHistSamples = 0;
            int currentSample = history.Item1;
            int outSample = 0;
            int inByte = 0;

            if (includeHistorySamples)
            {
                numHistSamples = 2;
                pcm = new short[count + numHistSamples];
                if (index <= currentSample)
                {
                    pcm[outSample++] = hist2;
                }
                if (index <= currentSample + 1)
                {
                    pcm[outSample++] = hist1;
                }

            }
            else
            {
                pcm = new short[count];
            }

            int firstSample = Math.Max(index - numHistSamples, currentSample);
            int lastSample = index + count;

            if (firstSample == lastSample)
            {
                return pcm;
            }

            for (int i = 0; i < numBlocks; i++)
            {
                byte ps = adpcm[inByte++];
                int scale = 1 << (ps & 0xf);
                int predictor = (ps >> 4) & 0xf;
                short coef1 = audio.Coefs[predictor * 2];
                short coef2 = audio.Coefs[predictor * 2 + 1];

                for (int s = 0; s < 14; s++)
                {
                    int sample;
                    if (s % 2 == 0)
                    {
                        sample = (adpcm[inByte] >> 4) & 0xF;
                    }
                    else
                    {
                        sample = adpcm[inByte++] & 0xF;
                    }
                    sample = sample >= 8 ? sample - 16 : sample;

                    sample = (((scale * sample) << 11) + 1024 + (coef1 * hist1 + coef2 * hist2)) >> 11;

                    if (sample > short.MaxValue)
                        sample = short.MaxValue;
                    if (sample < short.MinValue)
                        sample = short.MinValue;

                    hist2 = hist1;
                    hist1 = (short)sample;

                    if (currentSample >= firstSample)
                    {
                        pcm[outSample++] = (short)sample;
                    }

                    if (++currentSample >= lastSample)
                    {
                        return pcm;
                    }
                }
            }
            return pcm;
        }

        private static AdpcmChannel PcmToAdpcm(PcmChannel pcmChannel)
        {
            var channel = new AdpcmChannel(pcmChannel.NumSamples);
            channel.Coefs = DspCorrelateCoefs(pcmChannel.GetAudioData(), pcmChannel.NumSamples);

            /* Execute encoding-predictor for each block */
            var convSamps = new short[2 + SamplesPerBlock];
            var block = new byte[BytesPerBlock];

            int blockCount = 0;
            foreach (short[] inBlock in pcmChannel.GetAudioData().Batch(SamplesPerBlock))
            {
                Array.Copy(inBlock, 0, convSamps, 2, SamplesPerBlock);

                DspEncodeFrame(convSamps, SamplesPerBlock, block, channel.Coefs);

                convSamps[0] = convSamps[14];
                convSamps[1] = convSamps[15];

                int numSamples = Math.Min(pcmChannel.NumSamples - blockCount * SamplesPerBlock, SamplesPerBlock);
                Array.Copy(block, 0, channel.AudioByteArray, blockCount++ * BytesPerBlock, GetBytesForAdpcmSamples(numSamples));
            }
            return channel;
        }

        /// <summary>
        /// Encodes a <see cref="PcmStream"/> to a <see cref="AdpcmStream"/>.
        /// </summary>
        /// <param name="pcmStream">The <see cref="PcmStream"/> to encode.</param>
        /// <returns>The encoded <see cref="AdpcmStream"/>.</returns>
        public static AdpcmStream PcmToAdpcm(PcmStream pcmStream)
        {
            AdpcmStream adpcm = new AdpcmStream(pcmStream.NumSamples, pcmStream.SampleRate);
            var channels = new AdpcmChannel[pcmStream.Channels.Count];

            for (int i = 0; i < channels.Length; i++)
            {
                channels[i] = PcmToAdpcm(pcmStream.Channels[i]);
            }

            foreach (AdpcmChannel channel in channels)
            {
                adpcm.Channels.Add(channel);
            }

            return adpcm;
        }

        /// <summary>
        /// Encodes a <see cref="PcmStream"/> to a <see cref="AdpcmStream"/>.
        /// Each channel will be encoded in parallel.
        /// </summary>
        /// <param name="pcmStream">The <see cref="PcmStream"/> to encode.</param>
        /// <returns>The encoded <see cref="AdpcmStream"/>.</returns>
        public static AdpcmStream PcmToAdpcmParallel(PcmStream pcmStream)
        {
            AdpcmStream adpcm = new AdpcmStream(pcmStream.NumSamples, pcmStream.SampleRate);
            var channels = new AdpcmChannel[pcmStream.Channels.Count];

            Parallel.For(0, channels.Length, i =>
            {
                channels[i] = PcmToAdpcm(pcmStream.Channels[i]);
            });

            foreach (AdpcmChannel channel in channels)
            {
                adpcm.Channels.Add(channel);
            }

            return adpcm;
        }

        private static PcmChannel AdpcmtoPcm(AdpcmChannel adpcmChannel)
        {
            return new PcmChannel(adpcmChannel.NumSamples)
            {
                AudioData = adpcmChannel.GetPcmAudio()
            };
        }

        /// <summary>
        /// Decodes an <see cref="AdpcmStream"/> to a <see cref="PcmStream"/>.
        /// </summary>
        /// <param name="adpcmStream">The <see cref="AdpcmStream"/> to decode.</param>
        /// <returns>The decoded <see cref="PcmStream"/>.</returns>
        public static PcmStream AdpcmtoPcm(AdpcmStream adpcmStream)
        {
            PcmStream pcm = new PcmStream(adpcmStream.NumSamples, adpcmStream.SampleRate);
            var channels = new PcmChannel[adpcmStream.Channels.Count];

            for (int i = 0; i < channels.Length; i++)
            {
                channels[i] = AdpcmtoPcm(adpcmStream.Channels[i]);
            }

            foreach (PcmChannel channel in channels)
            {
                pcm.Channels.Add(channel);
            }

            return pcm;
        }

        /// <summary>
        /// Decodes an <see cref="AdpcmStream"/> to a <see cref="PcmStream"/>.
        /// Each channel will be decoded in parallel.
        /// </summary>
        /// <param name="adpcmStream">The <see cref="AdpcmStream"/> to decode.</param>
        /// <returns>The decoded <see cref="PcmStream"/>.</returns>
        public static PcmStream AdpcmtoPcmParallel(AdpcmStream adpcmStream)
        {
            PcmStream pcm = new PcmStream(adpcmStream.NumSamples, adpcmStream.SampleRate);
            var channels = new PcmChannel[adpcmStream.Channels.Count];

            Parallel.For(0, channels.Length, i =>
            {
                channels[i] = AdpcmtoPcm(adpcmStream.Channels[i]);
            });

            foreach (PcmChannel channel in channels)
            {
                pcm.Channels.Add(channel);
            }

            return pcm;
        }

        internal static void CalculateAdpcTable(AdpcmChannel channel, int samplesPerEntry)
        {
            var audio = channel.GetPcmAudio(true);
            int numEntries = channel.NumSamples.DivideByRoundUp(samplesPerEntry);
            short[] table = new short[numEntries * 2];

            for (int i = 0; i < numEntries; i++)
            {
                table[i * 2] = audio[i * samplesPerEntry + 1];
                table[i * 2 + 1] = audio[i * samplesPerEntry];
            }

            channel.SeekTable = table;
            channel.SelfCalculatedSeekTable = true;
            channel.SamplesPerSeekTableEntry = samplesPerEntry;
        }

        internal static void CalculateAdpcTable(IEnumerable<AdpcmChannel> channels, int samplesPerEntry)
        {
            Parallel.ForEach(channels, channel => CalculateAdpcTable(channel, samplesPerEntry));
        }

        internal static byte[] BuildAdpcTable(IEnumerable<AdpcmChannel> channels, int samplesPerEntry, int numEntries)
        {
            channels = channels.ToList();
            CalculateAdpcTable(channels.Where(x =>
            x.SeekTable == null || x.SamplesPerSeekTableEntry != samplesPerEntry), samplesPerEntry);

            var table = channels
                .Select(x => x.SeekTable)
                .ToArray()
                .Interleave(2)
                .ToFlippedBytes();

            Array.Resize(ref table, numEntries * 4 * channels.Count());
            return table;
        }
    }
}
