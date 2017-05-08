using System;
using VGAudio.Utilities;
using static VGAudio.Formats.GcAdpcm.GcAdpcmHelpers;
using static VGAudio.Utilities.Helpers;

namespace VGAudio.Codecs
{
    /// <summary>
    /// This class contains functions used for encoding
    /// Nintendo's 4-bit ADPCM audio format.
    /// </summary>
    public static class GcAdpcmEncoder
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

        private static bool AnalyzeRanges(double[][] mtx, int[] vecIdxsOut, double[] recips)
        {
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

            return min / max < 1.0e-10;
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

        private static void MatrixFilter(double[,] src, int row, double[] dst, double[][] mtx)
        {
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

            double[][] mtx = new double[3][];
            for (int i = 0; i < mtx.Length; i++)
            {
                mtx[i] = new double[3];
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
                    MatrixFilter(records, z, buffer2, mtx);
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

        public static short[] DspCorrelateCoefs(short[] source)
        {
            int frameCount = source.Length.DivideByRoundUp(SamplesPerFrame);

            short[] pcmHistBuffer = new short[28];

            short[] coefs = new short[16];

            double[] vec1 = new double[3];
            double[] vec2 = new double[3];
            double[] buffer = new double[3];

            double[][] mtx = new double[3][];
            for (int i = 0; i < mtx.Length; i++)
            {
                mtx[i] = new double[3];
            }

            int[] vecIdxs = new int[3];

            double[,] records = new double[frameCount * 2, 3];

            int recordCount = 0;

            double[][] vecBest = new double[8][];
            for (int i = 0; i < vecBest.Length; i++)
            {
                vecBest[i] = new double[3];
            }

            /* Iterate though one frame at a time */
            for (int sample = 0, remaining = source.Length; sample < source.Length; sample += 14, remaining -= 14)
            {
                Array.Clear(pcmHistBuffer, 14, 14);
                Array.Copy(source, sample, pcmHistBuffer, 14, Math.Min(14, remaining));

                InnerProductMerge(vec1, pcmHistBuffer);
                if (Math.Abs(vec1[0]) > 10.0)
                {
                    OuterProductMerge(mtx, pcmHistBuffer);
                    if (!AnalyzeRanges(mtx, vecIdxs, buffer))
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
                MatrixFilter(records, z, vecBest[0], mtx);
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
                DspEncodeCoef(pcmInOut, sampleCount, b.Coefs[i], b.InSamples[i], b.OutSamples[i], out b.Scale[i],
                    out b.DistAccum[i]);
            }

            int bestIndex = 0;

            double min = double.MaxValue;
            for (int i = 0; i < 8; i++)
            {
                if (b.DistAccum[i] < min)
                {
                    min = b.DistAccum[i];
                    bestIndex = i;
                }
            }

            /* Write converted samples */
            for (int s = 0; s < sampleCount; s++)
                pcmInOut[s + 2] = (short)b.InSamples[bestIndex][s + 2];

            /* Write ps */
            adpcmOut[0] = (byte)((bestIndex << 4) | (b.Scale[bestIndex] & 0xF));

            /* Zero remaining samples */
            for (int s = sampleCount; s < 14; s++)
                b.OutSamples[bestIndex][s] = 0;

            /* Write output samples */
            for (int y = 0; y < 7; y++)
            {
                adpcmOut[y + 1] = (byte)((b.OutSamples[bestIndex][y * 2] << 4) | (b.OutSamples[bestIndex][y * 2 + 1] & 0xF));
            }
        }

        private static void DspEncodeCoef(short[] pcmInOut, int sampleCount, short[] coefs, int[] inSamples,
            int[] outSamples, out int scale, out double distAccum)
        {
            int v1, v2, v3;
            int index;
            int distance = 0;

            /* Set yn values */
            inSamples[0] = pcmInOut[0];
            inSamples[1] = pcmInOut[1];

            /* Round and clamp samples for this coef set */
            for (int s = 0; s < sampleCount; s++)
            {
                /* Multiply previous samples by coefs */
                inSamples[s + 2] = v1 = ((pcmInOut[s] * coefs[1]) + (pcmInOut[s + 1] * coefs[0])) / 2048;
                /* Subtract from current sample */
                v2 = pcmInOut[s + 2] - v1;
                /* Clamp */
                v3 = Clamp16(v2);
                /* Compare distance */
                if (Math.Abs(v3) > Math.Abs(distance))
                    distance = v3;
            }

            /* Set initial scale */
            for (scale = 0; (scale <= 12) && ((distance > 7) || (distance < -8)); scale++, distance /= 2)
            {
            }
            scale = (scale <= 1) ? -1 : scale - 2;

            do
            {
                scale++;
                distAccum = 0;
                index = 0;

                for (int s = 0; s < sampleCount; s++)
                {
                    /* Multiply previous */
                    v1 = ((inSamples[s] * coefs[1]) + (inSamples[s + 1] * coefs[0]));
                    /* Evaluate from real sample */
                    v2 = (pcmInOut[s + 2] << 11) - v1;
                    /* Round to nearest sample */
                    // The official encoder does the casting this way, so match that behavior
                    v3 = (v2 > 0)
                        ? (int)((double)((float)v2 / (1 << scale) / 2048) + 0.4999999f)
                        : (int)((double)((float)v2 / (1 << scale) / 2048) - 0.4999999f);

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
                    outSamples[s] = v3;

                    /* Round and expand */
                    v1 = (v1 + ((v3 * (1 << scale)) << 11) + 1024) >> 11;
                    /* Clamp and store */
                    inSamples[s + 2] = v2 = Clamp16(v1);
                    /* Accumulate distance */
                    v3 = pcmInOut[s + 2] - v2;
                    distAccum += v3 * (double)v3;
                }

                for (int x = index + 8; x > 256; x >>= 1)
                    if (++scale >= 12)
                        scale = 11;
            } while ((scale < 12) && (index > 1));
        }

        public static byte[] EncodeAdpcm(short[] pcm, short[] coefs, int samples = -1, short hist1 = 0,
            short hist2 = 0)
        {
            int sampleCount = samples == -1 ? pcm.Length : samples;
            var adpcm = new byte[SampleCountToByteCount(sampleCount)];

            /* Execute encoding-predictor for each frame */
            var pcmBuffer = new short[2 + SamplesPerFrame];
            var adpcmBuffer = new byte[BytesPerFrame];

            pcmBuffer[0] = hist2;
            pcmBuffer[1] = hist1;

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
            }

            return adpcm;
        }

        public class AdpcmEncodeBuffers
        {
            public short[][] Coefs { get; } = new short[8][];
            public int[][] InSamples { get; } = new int[8][];
            public int[][] OutSamples { get; } = new int[8][];
            public int[] Scale { get; } = new int[8];
            public double[] DistAccum { get; } = new double[8];

            public AdpcmEncodeBuffers()
            {
                for (int i = 0; i < 8; i++)
                {
                    InSamples[i] = new int[16];
                    OutSamples[i] = new int[14];
                    Coefs[i] = new short[2];
                }
            }
        }
    }
}
