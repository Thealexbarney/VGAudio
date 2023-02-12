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
            return UnpackingWasSuccessful(frame, reader);
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

        private static int CalculateResolution(int scaleFactor, int noiseLevel, int version)
        {
            if (scaleFactor == 0)
            {
                return 0;
            }
            int curvePosition = noiseLevel - 5 * scaleFactor / 2 + 2;
            //https://github.com/vgmstream/vgmstream/blob/4eecdada9a03a73af0c7c17f5cd6e08518fd7e3f/src/coding/hca_decoder_clhca.c#L1450
            if (version >= 0x0300 && curvePosition >= 67) return 0; //FIXME
            curvePosition = Helpers.Clamp(curvePosition, 0, 58);
            return CriHcaTables.ScaleToResolutionCurve[curvePosition];
        }

        public static int CalculateResolution(int scaleFactor, int noiseLevel)
        {
            const int HcaVersion = 0x0200;
            return CalculateResolution(scaleFactor, noiseLevel, HcaVersion);
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
                if (!ReadScaleFactors(channel, reader, frame.Hca.HfrGroupCount, frame.Hca.Version)) return false;

                for (int i = 0, newResolution = 0; i < frame.EvaluationBoundary; i++)
                {
                    newResolution = CalculateResolution(channel.ScaleFactors[i], athCurve[i] + frame.AcceptableNoiseLevel - 1, frame.Hca.Version);
                    if (frame.Hca.Version >= 0x0300) newResolution = Helpers.Clamp(newResolution, frame.Hca.MinResolution, frame.Hca.MaxResolution);
                    channel.Resolution[i] = newResolution;
                }

                for (int i = frame.EvaluationBoundary, newResolution = 0; i < channel.CodedScaleFactorCount; i++)
                {
                    newResolution = CalculateResolution(channel.ScaleFactors[i], athCurve[i] + frame.AcceptableNoiseLevel, frame.Hca.Version);
                    if (frame.Hca.Version >= 0x0300) newResolution = Helpers.Clamp(newResolution, frame.Hca.MinResolution, frame.Hca.MaxResolution);
                    channel.Resolution[i] = newResolution;
                }

                if (channel.Type == ChannelType.StereoSecondary)
                {
                    ReadIntensity(reader, channel.Intensity, frame.Hca.Version);
                }
                else if (frame.Hca.HfrGroupCount > 0)
                {
                    if (frame.Hca.Version < 0x0300) ReadHfrScaleFactors(reader, frame.Hca.HfrGroupCount, channel.HfrScales);
                    // v3.0 uses values derived in ReadScaleFactors
                }
            }
            return true;
        }

        private static bool ReadScaleFactors(CriHcaChannel channel, BitReader reader, int hfrGroupCount, int version)
        {
            channel.ScaleFactorDeltaBits = reader.ReadInt(3);
            if (channel.ScaleFactorDeltaBits == 0)
            {
                Array.Clear(channel.ScaleFactors, 0, channel.ScaleFactors.Length);
                return true;
            }

            // added in v3.0
            // https://github.com/vgmstream/vgmstream/blob/4eecdada9a03a73af0c7c17f5cd6e08518fd7e3f/src/coding/hca_decoder_clhca.c#L1287
            int extraCodedScaleFactorCount;
            int codedScaleFactorCount;
            if (channel.Type == ChannelType.StereoSecondary || hfrGroupCount <= 0 || version < 0x0300)
            {
                extraCodedScaleFactorCount = 0;
                codedScaleFactorCount = channel.CodedScaleFactorCount;
            }
            else
            {
                extraCodedScaleFactorCount = hfrGroupCount;
                codedScaleFactorCount = channel.CodedScaleFactorCount + extraCodedScaleFactorCount;

                // just in case
                if (codedScaleFactorCount > SamplesPerSubFrame)
                    throw new InvalidDataException("codedScaleFactorCount > SamplesPerSubFrame");
            }

            if (channel.ScaleFactorDeltaBits >= 6)
            {
                for (int i = 0; i < codedScaleFactorCount; i++)
                {
                    channel.ScaleFactors[i] = reader.ReadInt(6);
                }
                return true;
            }

            bool result = DeltaDecode(reader, channel.ScaleFactorDeltaBits, 6, codedScaleFactorCount, channel.ScaleFactors);
            if (!result) return result;

            // set derived HFR scales for v3.0
            //FIXME UNTESTED
            for (int i = 0; i < extraCodedScaleFactorCount; i++)
            {
                channel.HfrScales[codedScaleFactorCount - 1 - i] = channel.ScaleFactors[codedScaleFactorCount - i];
            }

            return result;
        }

        private static void ReadIntensity(BitReader reader, int[] intensity, int version)
        {
            if (version < 0x0300)
            {
                for (int i = 0; i < SubframesPerFrame; i++)
                {
                    intensity[i] = reader.ReadInt(4);
                }
            } else
            {
                //https://github.com/vgmstream/vgmstream/blob/4eecdada9a03a73af0c7c17f5cd6e08518fd7e3f/src/coding/hca_decoder_clhca.c#L1374
                int value = reader.ReadInt(4);
                int delta_bits;

                if (value < 15)
                {
                    delta_bits = reader.ReadInt(2); /* +1 */

                    intensity[0] = value;
                    if (delta_bits == 3)
                    { /* 3+1 = 4b */
                        /* fixed intensities */
                        for (int i = 1; i < SubframesPerFrame; i++)
                        {
                            intensity[i] = reader.ReadInt(4);
                        }
                    }
                    else
                    {
                        /* delta intensities */
                        int bmax = (2 << delta_bits) - 1;
                        int bits = delta_bits + 1;

                        for (int i = 1; i < SubframesPerFrame; i++)
                        {
                            int delta = reader.ReadInt(bits);
                            if (delta == bmax)
                            {
                                value = reader.ReadInt(4); /* encoded */
                            }
                            else
                            {
                                value = value - (bmax >> 1) + delta; /* differential */
                                if (value > 15) //todo check
                                    throw new InvalidDataException("value > 15"); /* not done in lib */
                            }

                            intensity[i] = value;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < SubframesPerFrame; i++)
                    {
                        intensity[i] = 7;
                    }
                }
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
                int delta = reader.ReadOffsetBinary(deltaBits, BitReader.OffsetBias.Positive);

                if (delta < maxDelta)
                {
                    int value = output[i - 1] + delta;
                    if (value < 0 || value > maxValue)
                    {
                        return false;
                    }

                    // https://github.com/vgmstream/vgmstream/blob/4eecdada9a03a73af0c7c17f5cd6e08518fd7e3f/src/coding/hca_decoder_clhca.c#L1327
                    //value &= 0x3F; // v3.0 lib

                    output[i] = value;
                }
                else
                {
                    output[i] = reader.ReadInt(dataBits);
                }
            }
            return true;
        }

        private static bool UnpackingWasSuccessful(CriHcaFrame frame, BitReader reader)
        {
            // 128 leftover bits after unpacking should be high enough to get rid of false negatives,
            // and low enough that false positives will be uncommon.
            return reader.Remaining >= 16 && reader.Remaining <= 128
                   || FrameEmpty(frame)
                   || frame.AcceptableNoiseLevel == 0 && reader.Remaining >= 16;
        }

        private static bool FrameEmpty(CriHcaFrame frame)
        {
            if (frame.AcceptableNoiseLevel > 0) return false;

            // If all the scale factors are 0, the frame is empty
            foreach (CriHcaChannel channel in frame.Channels)
            {
                if (channel.ScaleFactorDeltaBits > 0)
                {
                    return false;
                }
            }
            return true;
        }

        private static void WriteChecksum(BitWriter writer, Crc16 crc, byte[] hcaBuffer)
        {
            writer.Position = writer.LengthBits - 16;
            ushort crc16 = crc.Compute(hcaBuffer, hcaBuffer.Length - 2);
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
            int[] scales = channel.ScaleFactors;
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
