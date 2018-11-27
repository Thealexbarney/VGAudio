using System;
using VGAudio.Utilities;
using static VGAudio.Codecs.GcAdpcm.GcAdpcmMath;

namespace VGAudio.Codecs.GcAdpcm
{
    public static class GcAdpcmCoefficients
    {
        public static short[] CalculateCoefficients(short[] source)
        {
            int frameCount = source.Length.DivideByRoundUp(SamplesPerFrame);

            var pcmHistBuffer = new short[28];

            var coefs = new short[16];

            var vec1 = new double[3];
            var vec2 = new double[3];
            var buffer = new double[3];

            var mtx = new double[3][];
            for (int i = 0; i < mtx.Length; i++)
            {
                mtx[i] = new double[3];
            }

            var vecIdxs = new int[3];

            var records = new double[frameCount * 2, 3];

            int recordCount = 0;

            var vecBest = new double[8][];
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
            var tmp = new double[3];
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
            var bufferList = new double[8][];
            for (int i = 0; i < bufferList.Length; i++)
            {
                bufferList[i] = new double[3];
            }

            var mtx = new double[3][];
            for (int i = 0; i < mtx.Length; i++)
            {
                mtx[i] = new double[3];
            }

            var buffer1 = new int[8];
            var buffer2 = new double[3];

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
                    int index = 0;
                    double value = 1.0e30;
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
    }
}
