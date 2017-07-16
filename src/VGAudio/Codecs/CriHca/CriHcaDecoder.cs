using System;
using System.IO;
using VGAudio.Utilities;
using static VGAudio.Codecs.CriHca.CriHcaTables;

namespace VGAudio.Codecs.CriHca
{
    public static class CriHcaDecoder
    {
        private const int SubframesPerFrame = 8;
        private const int SubframeLength = 128;
        private const int FrameLength = SubframesPerFrame * SubframeLength;

        public static short[][] Decode(HcaInfo hca, byte[][] audio)
        {
            var pcmOut = Helpers.CreateJaggedArray<short[][]>(hca.ChannelCount, hca.SampleCount);
            var pcmBuffer = Helpers.CreateJaggedArray<short[][]>(hca.ChannelCount, FrameLength);

            var frame = new CriHcaFrame(hca);

            for (int i = 0; i < hca.FrameCount; i++)
            {
                DecodeFrame(audio[i], frame, pcmBuffer);

                for (int c = 0; c < pcmOut.Length; c++)
                {
                    Array.Copy(pcmBuffer[c], 0, pcmOut[c], i * FrameLength, pcmBuffer[c].Length);
                }
            }

            return pcmOut;
        }

        private static void DecodeFrame(byte[] audio, CriHcaFrame frame, short[][] pcmOut)
        {
            var reader = new BitReader(audio);

            UnpackFrame(frame, reader);
            DequantizeFrame(frame);
            RestoreMissingBands(frame);
            Mdct.RunImdct(frame);
            PcmFloatToShort(frame, pcmOut);
        }

        public static bool UnpackFrame(CriHcaFrame frame, BitReader reader)
        {
            if (!UnpackFrameHeader(frame, reader)) return false;
            ReadSpectralCoefficients(frame, reader);
            return reader.Remaining >= 16 && reader.Remaining <= 40 ||
                   FrameEmpty(reader.Buffer, 2, reader.Buffer.Length - 4);
        }

        private static void DequantizeFrame(CriHcaFrame frame)
        {
            for (int i = 0; i < frame.ChannelCount; i++)
            {
                CalculateGain(frame.Scale[i], frame.Resolution[i], frame.Gain[i], frame.ScaleLength[i]);
            }

            for (int sf = 0; sf < SubframesPerFrame; sf++)
            {
                for (int c = 0; c < frame.ChannelCount; c++)
                {
                    for (int s = 0; s < frame.ScaleLength[c]; s++)
                    {
                        frame.Spectra[sf][c][s] = frame.QuantizedSpectra[sf][c][s] * frame.Gain[c][s];
                    }
                }
            }
        }

        private static void RestoreMissingBands(CriHcaFrame frame)
        {
            ReconstructHighFrequency(frame);
            ApplyIntensityStereo(frame);
        }

        private static bool UnpackFrameHeader(CriHcaFrame frame, BitReader reader)
        {
            int syncWord = reader.ReadInt(16);
            if (syncWord != 0xffff)
            {
                throw new InvalidDataException("Invalid frame header");
            }

            int acceptableNoiseLevel = reader.ReadInt(9);
            // The sample at which the resolution moves up to the next value in the table
            int evaluationBoundary = reader.ReadInt(7);
            int r = acceptableNoiseLevel * 256 - evaluationBoundary;
            for (int i = 0; i < frame.ChannelCount; i++)
            {
                if (!ReadScaleFactors(reader, frame.Scale[i], frame.ScaleLength[i])) return false;
                CalculateResolution(frame.Scale[i], frame.Resolution[i], r, frame.ScaleLength[i]);

                if (frame.ChannelType[i] == ChannelType.IntensityStereoSecondary)
                {
                    ReadIntensity(reader, frame.Intensity[i]);
                }
                else if (frame.Hca.HfrGroupCount > 0)
                {
                    ReadHfrScaleFactors(reader, frame.Hca.HfrGroupCount, frame.HfrScale[i]);
                }
            }
            return true;
        }

        private static bool ReadScaleFactors(BitReader reader, int[] scale, int scaleCount)
        {
            int deltaBits = reader.ReadInt(3);
            if (deltaBits == 0)
            {
                Array.Clear(scale, 0, scale.Length);
                return true;
            }

            if (deltaBits >= 6)
            {
                for (int i = 0; i < scaleCount; i++)
                {
                    scale[i] = reader.ReadInt(6);
                }
                return true;
            }

            return DeltaDecode(reader, deltaBits, 6, scaleCount, scale);
        }

        private static void CalculateResolution(int[] scales, int[] resolutions, int r, int scaleCount)
        {
            for (int i = 0; i < scaleCount; i++)
            {
                if (scales[i] == 0)
                {
                    resolutions[i] = 0;
                }
                else
                {
                    int a = ((r + i) / 256) - (5 * scales[i] / 2) + 2;
                    a = Helpers.Clamp(a, 0, 58);
                    resolutions[i] = ResolutionTable[a];
                }
            }
        }

        private static void CalculateGain(int[] scale, int[] resolution, float[] gain, int scaleCount)
        {
            for (int i = 0; i < scaleCount; i++)
            {
                gain[i] = DequantizerScalingTable[scale[i]] * DequantizerRangeTable[resolution[i]];
            }
        }

        private static void ReadIntensity(BitReader reader, int[] intensity)
        {
            for (int i = 0; i < SubframesPerFrame; i++)
            {
                intensity[i] = reader.ReadInt(4);
            }
        }

        private static void ReadHfrScaleFactors(BitReader reader, int groupCount, int[] hfrScale)
        {
            for (int i = 0; i < groupCount; i++)
            {
                hfrScale[i] = reader.ReadInt(6);
            }
        }

        private static void ReadSpectralCoefficients(CriHcaFrame frame, BitReader reader)
        {
            for (int sf = 0; sf < SubframesPerFrame; sf++)
            {
                for (int c = 0; c < frame.ChannelCount; c++)
                {
                    for (int s = 0; s < frame.ScaleLength[c]; s++)
                    {
                        int resolution = frame.Resolution[c][s];
                        int bitSize = MaxSampleBitSize[resolution];
                        int value = reader.PeekInt(bitSize);
                        if (resolution < 8)
                        {
                            frame.QuantizedSpectra[sf][c][s] = value;
                            bitSize = ActualSampleBitSize[resolution, value];
                            frame.QuantizedSpectra[sf][c][s] = QuantizedSampleValue[resolution, value];
                        }
                        else
                        {
                            // Read the sign-magnitude value. The low bit is the sign
                            int quantizedCoefficient = value / 2 * (1 - (value % 2 * 2));
                            if (value < 2)
                            {
                                bitSize--;
                            }
                            frame.QuantizedSpectra[sf][c][s] = quantizedCoefficient;
                        }
                        reader.Position += bitSize;
                    }

                    Array.Clear(frame.Spectra[sf][c], frame.ScaleLength[c], 0x80 - frame.ScaleLength[c]);
                }
            }
        }

        private static void ReconstructHighFrequency(CriHcaFrame frame)
        {
            if (frame.Hca.HfrGroupCount <= 0) return;
            int storedBands = frame.Hca.BaseBandCount + frame.Hca.StereoBandCount;
            for (int sf = 0; sf < SubframesPerFrame; sf++)
            {
                for (int c = 0; c < frame.ChannelCount; c++)
                {
                    if (frame.ChannelType[c] == ChannelType.IntensityStereoSecondary) continue;

                    int destBand = storedBands;
                    int sourceBand = storedBands - 1;
                    for (int group = 0; group < frame.Hca.HfrGroupCount; group++)
                    {
                        for (int band = 0;
                            band < frame.Hca.BandsPerHfrGroup && destBand < frame.Hca.TotalBandCount;
                            band++, sourceBand--)
                        {
                            frame.Spectra[sf][c][destBand++] =
                                ScaleConversionTable[frame.HfrScale[c][group] - frame.Scale[c][sourceBand] + 64] *
                                frame.Spectra[sf][c][sourceBand];
                        }
                    }
                    frame.Spectra[sf][c][0x7f] = 0;
                }
            }
        }

        private static void ApplyIntensityStereo(CriHcaFrame frame)
        {
            if (frame.Hca.StereoBandCount <= 0) return;
            for (int sf = 0; sf < SubframesPerFrame; sf++)
            {
                for (int c = 0; c < frame.ChannelCount - 1; c++)
                {
                    if (frame.ChannelType[c] != ChannelType.IntensityStereoPrimary) continue;

                    var l = frame.Spectra[sf][c];
                    var r = frame.Spectra[sf][c + 1];
                    float ratioL = IntensityRatioTable[frame.Intensity[c + 1][sf]];
                    float ratioR = ratioL - 2.0f;
                    for (int b = frame.Hca.BaseBandCount; b < frame.Hca.TotalBandCount; b++)
                    {
                        r[b] = l[b] * ratioR;
                        l[b] *= ratioL;
                    }
                }
            }
        }

        private static bool DeltaDecode(BitReader reader, int deltaBits, int dataBits, int count, int[] output)
        {
            output[0] = reader.ReadInt(dataBits);
            int maxDelta = 1 << (deltaBits - 1);
            int maxValue = (1 << dataBits) - 1;

            for (int i = 1; i < count; i++)
            {
                int delta = reader.ReadOffsetBinary(deltaBits);

                if (delta < maxDelta)
                {
                    int value = output[i - 1] + delta;
                    if (value < 0 || value > maxValue)
                    {
                        return false;
                    }
                    output[i] = value;
                }
                else
                {
                    output[i] = reader.ReadInt(dataBits);
                }
            }
            return true;
        }

        private static void PcmFloatToShort(CriHcaFrame frame, short[][] pcm)
        {
            for (int c = 0; c < frame.ChannelCount; c++)
            {
                for (int sf = 0; sf < SubframesPerFrame; sf++)
                {
                    for (int s = 0; s < SubframeLength; s++)
                    {
                        int sample = (int)(frame.PcmFloat[sf][c][s] * (short.MaxValue + 1));
                        pcm[c][sf * SubframeLength + s] = Helpers.Clamp16(sample);
                    }
                }
            }
        }

        private static bool FrameEmpty(byte[] bytes, int start, int length)
        {
            int end = start + length;
            for (int i = start; i < end; i++)
            {
                if (bytes[i] != 0)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
