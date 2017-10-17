using System;
using System.IO;
using VGAudio.Utilities;
using static VGAudio.Codecs.CriHca.CriHcaConstants;

namespace VGAudio.Codecs.CriHca
{
    internal static class CriHcaPacking
    {
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

        public static void PackFrame(CriHcaFrame frame, Crc16 crc, byte[] outBuffer)
        {
            var writer = new BitWriter(outBuffer);
            writer.Write(0xffff, 16);
            writer.Write(frame.AcceptableNoiseLevel, 9);
            writer.Write(frame.EvaluationBoundary, 7);

            foreach (CriHcaChannel channel in frame.Channels)
            {
                WriteScaleFactors(writer, channel);
                if (channel.Type == ChannelType.StereoSecondary)
                {
                    for (int i = 0; i < SubframesPerFrame; i++)
                    {
                        writer.Write(channel.Intensity[i], 4);
                    }
                }
                else if (frame.Hca.HfrGroupCount > 0)
                {
                    for (int i = 0; i < frame.Hca.HfrGroupCount; i++)
                    {
                        writer.Write(channel.HfrScales[i], 6);
                    }
                }
            }

            for (int sf = 0; sf < SubframesPerFrame; sf++)
            {
                foreach (CriHcaChannel channel in frame.Channels)
                {
                    WriteSpectra(writer, channel, sf);
                }
            }

            writer.AlignPosition(8);
            for (int i = writer.Position / 8; i < frame.Hca.FrameSize - 2; i++)
            {
                writer.Buffer[i] = 0;
            }

            WriteChecksum(writer, crc, outBuffer);
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
            frame.AcceptableNoiseLevel = reader.ReadInt(9);
            frame.EvaluationBoundary = reader.ReadInt(7);

            foreach (CriHcaChannel channel in frame.Channels)
            {
                if (!ReadScaleFactors(reader, channel.ScaleFactors, channel.CodedScaleFactorCount)) return false;

                for (int i = 0; i < frame.EvaluationBoundary; i++)
                {
                    channel.Resolution[i] = CalculateResolution(channel.ScaleFactors[i], athCurve[i] + frame.AcceptableNoiseLevel - 1);
                }

                for (int i = frame.EvaluationBoundary; i < channel.CodedScaleFactorCount; i++)
                {
                    channel.Resolution[i] = CalculateResolution(channel.ScaleFactors[i], athCurve[i] + frame.AcceptableNoiseLevel);
                }

                if (channel.Type == ChannelType.StereoSecondary)
                {
                    ReadIntensity(reader, channel.Intensity);
                }
                else if (frame.Hca.HfrGroupCount > 0)
                {
                    ReadHfrScaleFactors(reader, frame.Hca.HfrGroupCount, channel.HfrScales);
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
                foreach (CriHcaChannel channel in frame.Channels)
                {
                    for (int s = 0; s < channel.CodedScaleFactorCount; s++)
                    {
                        int resolution = channel.Resolution[s];
                        int bits = CriHcaTables.QuantizedSpectrumMaxBits[resolution];
                        int code = reader.PeekInt(bits);
                        if (resolution < 8)
                        {
                            bits = CriHcaTables.QuantizedSpectrumBits[resolution][code];
                            channel.QuantizedSpectra[sf][s] = CriHcaTables.QuantizedSpectrumValue[resolution][code];
                        }
                        else
                        {
                            // Read the sign-magnitude value. The low bit is the sign
                            int quantizedCoefficient = code / 2 * (1 - (code % 2 * 2));
                            if (quantizedCoefficient == 0)
                            {
                                bits--;
                            }
                            channel.QuantizedSpectra[sf][s] = quantizedCoefficient;
                        }
                        reader.Position += bits;
                    }

                    Array.Clear(channel.Spectra[sf], channel.CodedScaleFactorCount, 0x80 - channel.CodedScaleFactorCount);
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
