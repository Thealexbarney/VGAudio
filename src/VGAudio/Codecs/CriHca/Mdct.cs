using System;
using static VGAudio.Codecs.CriHca.CriHcaTables;

namespace VGAudio.Codecs.CriHca
{
    public static class Mdct
    {
        public static void RunImdct(Channel channel, int index)
        {
            Dct4(channel.Spectra, channel.DctTempBuffer);
            Array.Copy(channel.Spectra, channel.DctOutput, 0x80);

            for (int i = 0; i < 0x40; i++)
            {
                channel.PcmFloat[index][i] = MdctWindow[i] * channel.DctOutput[i + 0x40] + channel.ImdctPrevious[i];
                channel.PcmFloat[index][i + 0x40] = MdctWindow[i + 0x40] * -channel.DctOutput[0x7f - i] - channel.ImdctPrevious[i + 0x40];
                channel.ImdctPrevious[i] = MdctWindow[0x7f - i] * -channel.DctOutput[0x3f - i];
                channel.ImdctPrevious[i + 0x40] = MdctWindow[0x3f - i] * channel.DctOutput[i];
            }
        }

        private static void Dct4(float[] spectra, float[] work)
        {
            RunDif(spectra, work);
            DctMain(work, spectra);
        }

        private static void RunDif(float[] input, float[] workBuffer)
        {
            int bflyCount = 1; // Number of butterflies in the current transformation stage
            int bflyHalfSize = 0x40; // Half the size of a butterfly in the current stage
            float[] src = input; // Data source for the current stage
            float[] dst = workBuffer; // Destination for the transformed data
            for (int stage = 0; stage < 7; stage++)
            {
                for (int bflyNum = 0; bflyNum < bflyCount; bflyNum++) // Index of the current butterfly
                {
                    for (int bflyPos = 0; bflyPos < bflyHalfSize; bflyPos++) // Position in the current butterfly
                    {
                        int srcPos = (bflyNum * bflyHalfSize + bflyPos) * 2;
                        int dstPos = bflyNum * bflyHalfSize * 2 + bflyPos;
                        float a = src[srcPos];
                        float b = src[srcPos + 1];
                        dst[dstPos] = a + b;
                        dst[dstPos + bflyHalfSize] = a - b;
                    }
                }
                bflyCount *= 2;
                bflyHalfSize /= 2;
                float[] swapTemp = src;
                src = dst;
                dst = swapTemp;
            }
        }

        private static void DctMain(float[] workBuffer, float[] output)
        {
            int bflyCount = 0x40;
            int bflyHalfSize = 1;
            int bflySize = 2;
            float[] src = workBuffer;
            float[] dst = output;

            for (int stage = 0; stage < 7; stage++)
            {
                for (int bflyNum = 0; bflyNum < bflyCount; bflyNum++)
                {
                    for (int bflyPos = 0; bflyPos < bflyHalfSize; bflyPos++)
                    {
                        int currentBflyOffset = bflyNum * bflySize;
                        int srcPos = currentBflyOffset + bflyPos;
                        int dstPos = currentBflyOffset + bflySize - bflyPos - 1;
                        int tablePos = bflyNum * bflyHalfSize + bflyPos;

                        float a = src[srcPos];
                        float b = src[srcPos + bflyHalfSize];
                        float cos = CosTable[stage][tablePos];
                        float sin = SinTable[stage][tablePos];
                        dst[srcPos] = a * cos - b * sin;
                        dst[dstPos] = a * sin + b * cos;
                    }
                }

                bflyCount /= 2;
                bflyHalfSize = bflySize;
                bflySize *= 2;
                float[] swapTemp = src;
                src = dst;
                dst = swapTemp;
            }
        }

        // ReSharper disable once UnusedMember.Local
        private static void Dct4Slow(float[] input, float[] output)
        {
            const int points = 128;

            for (int k = 0; k < points; k++)
            {
                double sample = 0;
                for (int n = 0; n < points; n++)
                {
                    double angle = Math.PI / points * (k + 0.5) * (n + 0.5);
                    sample += Math.Cos(angle) * input[n];
                }
                sample *= Math.Sqrt(2.0 / points);
                output[k] = (float)sample;
            }
        }
    }
}
