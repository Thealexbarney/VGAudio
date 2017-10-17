using System;
using System.IO;
using VGAudio.Utilities;

namespace VGAudio.Codecs.CriHca
{
    internal static class CriHcaPacking
    {
        private const int SubframesPerFrame = 8;

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

        public static void PackFrame(CriHcaChannel[] channels, HcaInfo hca, Crc16 crc, int noiseLevel, int evalBoundary, byte[] hcaBuffer)
        {
            var writer = new BitWriter(hcaBuffer);
            writer.Write(0xffff, 16);
            writer.Write(noiseLevel, 9);
            writer.Write(evalBoundary, 7);

            foreach (CriHcaChannel channel in channels)
            {
                WriteScaleFactors(writer, channel);
                if (channel.Type == ChannelType.StereoSecondary)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        writer.Write(channel.Intensity[i], 4);
                    }
                }
                else if (hca.HfrGroupCount > 0)
                {
                    for (int i = 0; i < hca.HfrGroupCount; i++)
                    {
                        writer.Write(channel.HfrScales[i], 6);
                    }
                }
            }

            for (int sf = 0; sf < 8; sf++)
            {
                foreach (CriHcaChannel channel in channels)
                {
                    WriteSpectra(writer, channel, sf);
                }
            }

            writer.AlignPosition(8);
            for (int i = writer.Position / 8; i < hca.FrameSize - 2; i++)
            {
                writer.Buffer[i] = 0;
            }

            WriteChecksum(writer, crc, hcaBuffer);
        }

        public static int CalculateResolution(int scaleFactor, int noiseLevel)
        {
            if (scaleFactor == 0)
            {
                return 0;
            }
            int curvePosition = noiseLevel - 5 * scaleFactor / 2 + 2;
            curvePosition = Helpers.Clamp(curvePosition, 0, 58);
            return CriHcaTables.ScaleToResolutionCurve[curvePosition];
        }

        private static bool UnpackFrameHeader(CriHcaFrame frame, BitReader reader)
        {
            int syncWord = reader.ReadInt(16);
            if (syncWord != 0xffff)
            {
                throw new InvalidDataException("Invalid frame header");
            }

            byte[] athCurve = frame.AthCurve;
            int acceptableNoiseLevel = reader.ReadInt(9);
            int evaluationBoundary = reader.ReadInt(7);

            for (int i = 0; i < frame.ChannelCount; i++)
            {
                if (!ReadScaleFactors(reader, frame.Scale[i], frame.ScaleLength[i])) return false;

                for (int j = 0; j < evaluationBoundary; j++)
                {
                    frame.Resolution[i][j] = CalculateResolution(frame.Scale[i][j], athCurve[j] + acceptableNoiseLevel - 1);
                }

                for (int j = evaluationBoundary; j < frame.ScaleLength[i]; j++)
                {
                    frame.Resolution[i][j] = CalculateResolution(frame.Scale[i][j], athCurve[j] + acceptableNoiseLevel);
                }

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
                        int bits = CriHcaTables.QuantizedSpectrumMaxBits[resolution];
                        int code = reader.PeekInt(bits);
                        if (resolution < 8)
                        {
                            bits = CriHcaTables.QuantizedSpectrumBits[resolution][code];
                            frame.QuantizedSpectra[sf][c][s] = CriHcaTables.QuantizedSpectrumValue[resolution][code];
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

        private static void WriteChecksum(BitWriter writer, Crc16 crc, byte[] hcaBuffer)
        {
            writer.Position = writer.LengthBits - 16;
            var crc16 = crc.Compute(hcaBuffer, hcaBuffer.Length - 2);
            writer.Write(crc16, 16);
        }

        private static void WriteSpectra(BitWriter writer, CriHcaChannel channel, int subFrame)
        {
            for (int i = 0; i < channel.CodedScaleFactorCount; i++)
            {
                int resolution = channel.Resolution[i];
                int quantizedSpectra = channel.QuantizedSpectra[subFrame][i];
                if (resolution == 0) continue;
                if (resolution < 8)
                {
                    int bits = CriHcaTables.QuantizeSpectrumBits[resolution][quantizedSpectra + 8];
                    writer.Write(CriHcaTables.QuantizeSpectrumValue[resolution][quantizedSpectra + 8], bits);
                }
                else if (resolution < 16)
                {
                    int bits = CriHcaTables.QuantizedSpectrumMaxBits[resolution] - 1;
                    writer.Write(Math.Abs(quantizedSpectra), bits);
                    if (quantizedSpectra != 0)
                    {
                        writer.Write(quantizedSpectra > 0 ? 0 : 1, 1);
                    }
                }
            }
        }

        private static void WriteScaleFactors(BitWriter writer, CriHcaChannel channel)
        {
            int deltaBits = channel.ScaleFactorDeltaBits;
            var scales = channel.ScaleFactors;
            writer.Write(deltaBits, 3);
            if (deltaBits == 0) return;

            if (deltaBits == 6)
            {
                for (int i = 0; i < channel.CodedScaleFactorCount; i++)
                {
                    writer.Write(scales[i], 6);
                }
                return;
            }

            writer.Write(scales[0], 6);
            int maxDelta = (1 << (deltaBits - 1)) - 1;
            int escapeValue = (1 << deltaBits) - 1;

            for (int i = 1; i < channel.CodedScaleFactorCount; i++)
            {
                int delta = scales[i] - scales[i - 1];
                if (Math.Abs(delta) > maxDelta)
                {
                    writer.Write(escapeValue, deltaBits);
                    writer.Write(scales[i], 6);
                }
                else
                {
                    writer.Write(maxDelta + delta, deltaBits);
                }
            }
        }
    }
}
