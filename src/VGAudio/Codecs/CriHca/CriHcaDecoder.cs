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

        public static short[][] Decode(HcaInfo hca, byte[][] audio, CriHcaParameters config = null)
        {
            config?.Progress?.SetTotal(hca.FrameCount);
            var pcmOut = Helpers.CreateJaggedArray<short[][]>(hca.ChannelCount, hca.SampleCount);
            var pcmBuffer = Helpers.CreateJaggedArray<short[][]>(hca.ChannelCount, FrameLength);

            var frame = new CriHcaFrame(hca);

            for (int i = 0; i < hca.FrameCount; i++)
            {
                DecodeFrame(audio[i], frame, pcmBuffer);

                CopyPcmToOutput(pcmBuffer, pcmOut, hca, i);
                //CopyBuffer(pcmBuffer, pcmOut, hca.InsertedSamples, i);
                config?.Progress?.ReportAdd(1);
            }

            return pcmOut;
        }

        private static void CopyPcmToOutput(short[][] pcmIn, short[][] pcmOut, HcaInfo hca, int frame)
        {
            int currentSample = frame * FrameLength - hca.InsertedSamples;
            int remainingSamples = Math.Min(hca.SampleCount - currentSample, hca.SampleCount);
            int srcStart = Helpers.Clamp(0 - currentSample, 0, FrameLength);
            int destStart = Math.Max(currentSample, 0);

            int length = Math.Min(FrameLength - srcStart, remainingSamples);
            if (length <= 0) return;

            for (int c = 0; c < pcmOut.Length; c++)
            {
                Array.Copy(pcmIn[c], srcStart, pcmOut[c], destStart, length);
            }
        }

        public static void CopyBuffer(short[][] bufferIn, short[][] bufferOut, int startIndex, int bufferIndex)
        {
            if (bufferIn == null || bufferOut == null || bufferIn.Length == 0 || bufferOut.Length == 0)
            {
                throw new ArgumentException(
                    $"{nameof(bufferIn)} and {nameof(bufferOut)} must be non-null with a length greater than 0");
            }

            int bufferLength = bufferIn[0].Length;
            int outLength = bufferOut[0].Length;

            int currentIndex = bufferIndex * bufferLength - startIndex;
            int remainingElements = Math.Min(outLength - currentIndex, outLength);
            int srcStart = Helpers.Clamp(0 - currentIndex, 0, FrameLength);
            int destStart = Math.Max(currentIndex, 0);

            int length = Math.Min(FrameLength - srcStart, remainingElements);
            if (length <= 0) return;

            for (int c = 0; c < bufferOut.Length; c++)
            {
                Array.Copy(bufferIn[c], srcStart, bufferOut[c], destStart, length);
            }
        }

        private static void DecodeFrame(byte[] audio, CriHcaFrame frame, short[][] pcmOut)
        {
            var reader = new BitReader(audio);

            UnpackFrame(frame, reader);
            DequantizeFrame(frame);
            RestoreMissingBands(frame);
            RunImdct(frame);
            PcmFloatToShort(frame, pcmOut);
        }

        public static bool UnpackFrame(CriHcaFrame frame, BitReader reader)
        {
            if (!UnpackFrameHeader(frame, reader)) return false;
            ReadSpectralCoefficients(frame, reader);
            // Todo: I've found 2 HCA files from Jojo's Bizarre Adventure - All Star Battle
            // that don't use all the available bits in frames at the beginning of the file.
            // Come up with a method of verifying if a frame unpacks successfully or not
            // that accounts for frames like these.
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
                CalculateResolution(frame.Scale[i], frame.Resolution[i], r, frame.ScaleLength[i], frame.AthCurve);

                if (frame.ChannelType[i] == ChannelType.StereoSecondary)
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

        private static void CalculateResolution(int[] scales, int[] resolutions, int r, int scaleCount, byte[] athCurve)
        {
            for (int i = 0; i < scaleCount; i++)
            {
                if (scales[i] == 0)
                {
                    resolutions[i] = 0;
                }
                else
                {
                    int a = athCurve[i] + ((r + i) / 256) - (5 * scales[i] / 2) + 2;
                    a = Helpers.Clamp(a, 0, 58);
                    resolutions[i] = ScaleToResolutionCurve[a];
                }
            }
        }

        private static void CalculateGain(int[] scale, int[] resolution, float[] gain, int scaleCount)
        {
            for (int i = 0; i < scaleCount; i++)
            {
                gain[i] = DequantizerScalingTable[scale[i]] * DequantizerNormalizeTable[resolution[i]];
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
                        int bits = QuantizedSpectrumMaxBits[resolution];
                        int code = reader.PeekInt(bits);
                        if (resolution < 8)
                        {
                            bits = QuantizedSpectrumBits[resolution][code];
                            frame.QuantizedSpectra[sf][c][s] = QuantizedSpectrumValue[resolution][code];
                        }
                        else
                        {
                            // Read the sign-magnitude value. The low bit is the sign
                            int quantizedCoefficient = code / 2 * (1 - (code % 2 * 2));
                            if (quantizedCoefficient == 0)
                            {
                                bits--;
                            }
                            frame.QuantizedSpectra[sf][c][s] = quantizedCoefficient;
                        }
                        reader.Position += bits;
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
                    if (frame.ChannelType[c] == ChannelType.StereoSecondary) continue;

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
                    if (frame.ChannelType[c] != ChannelType.StereoPrimary) continue;

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

        private static void RunImdct(CriHcaFrame frame)
        {
            for (int sf = 0; sf < 8; sf++)
            {
                for (int c = 0; c < frame.ChannelCount; c++)
                {
                    frame.Mdct[c].RunImdct(frame.Spectra[sf][c], frame.PcmFloat[sf][c]);
                }
            }
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
