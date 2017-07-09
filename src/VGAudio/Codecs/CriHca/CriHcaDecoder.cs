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

            var channels = new Channel[hca.ChannelCount];
            for (int i = 0; i < channels.Length; i++)
            {
                channels[i] = new Channel();
            }

            DecodePrep(hca, channels);

            for (int i = 0; i < hca.FrameCount; i++)
            {
                DecodeFrame(hca, audio[i], channels, pcmBuffer);

                for (int c = 0; c < pcmOut.Length; c++)
                {
                    Array.Copy(pcmBuffer[c], 0, pcmOut[c], i * FrameLength, pcmBuffer[c].Length);
                }
            }

            return pcmOut;
        }

        private static void DecodePrep(HcaInfo hca, Channel[] channels)
        {
            int[] types = GetChannelTypes(hca);

            for (int i = 0; i < channels.Length; i++)
            {
                channels[i].ElementType = (ChannelType)types[i];
                channels[i].ScaleLength = hca.BaseBandCount;
                if (types[i] != 2) channels[i].ScaleLength += hca.StereoBandCount;
            }
        }

        private static void DecodeFrame(HcaInfo hca, byte[] audio, Channel[] channels, short[][] pcmOut)
        {
            var reader = new BitReader(audio);

            UnpackFrameHeader(hca, reader, channels);

            for (int subframe = 0; subframe < SubframesPerFrame; subframe++)
            {
                ReadSpectralCoefficients(reader, channels);
                ReconstructHighFrequency(hca, channels);
                ApplyIntensityStereo(hca, channels, subframe);

                foreach (Channel channel in channels)
                {
                    Mdct.RunImdct(channel, subframe);
                }
            }

            PcmFloatToShort(channels, pcmOut);
        }

        private static void UnpackFrameHeader(HcaInfo hca, BitReader reader, Channel[] channels)
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
            foreach (Channel channel in channels)
            {
                ReadScaleFactors(reader, channel.Scale, channel.ScaleLength);
                CalculateResolution(channel.Scale, channel.Resolution, r, channel.ScaleLength);
                CalculateGain(channel.Scale, channel.Resolution, channel.Gain, channel.ScaleLength);

                if (channel.ElementType == ChannelType.IntensityStereoSecondary)
                {
                    ReadIntensity(reader, channel.Intensity);
                }
                else if (hca.HfrGroupCount > 0)
                {
                    ReadHfrScaleFactors(reader, hca.HfrGroupCount, channel.HfrScale);
                }
            }
        }

        private static void ReadScaleFactors(BitReader reader, int[] scale, int scaleCount)
        {
            int deltaBits = reader.ReadInt(3);
            if (deltaBits == 0)
            {
                Array.Clear(scale, 0, scale.Length);
                return;
            }

            if (deltaBits >= 6)
            {
                for (int i = 0; i < scaleCount; i++)
                {
                    scale[i] = reader.ReadInt(6);
                }
                return;
            }

            DeltaDecode(reader, deltaBits, 6, scaleCount, scale);
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

        private static void ReadSpectralCoefficients(BitReader reader, Channel[] channels)
        {
            foreach (Channel channel in channels)
            {
                for (int i = 0; i < channel.ScaleLength; i++)
                {
                    int resolution = channel.Resolution[i];
                    int bitSize = MaxSampleBitSize[resolution];
                    int value = reader.PeekInt(bitSize);
                    int quantizedCoefficient;
                    if (resolution < 8)
                    {
                        channel.QuantizedSpectra[i] = value;
                        bitSize = ActualSampleBitSize[resolution, value];
                        quantizedCoefficient = QuantizedSampleValue[resolution, value];
                    }
                    else
                    {
                        // Read the sign-magnitude value. The low bit is the sign
                        quantizedCoefficient = value / 2 * (1 - (value % 2 * 2));
                        if (value < 2)
                        {
                            bitSize--;
                        }
                        channel.QuantizedSpectra[i] = quantizedCoefficient;
                    }
                    channel.Spectra[i] = quantizedCoefficient * channel.Gain[i];
                    reader.Position += bitSize;
                }

                Array.Clear(channel.Spectra, channel.ScaleLength, 0x80 - channel.ScaleLength);
            }
        }

        private static void ReconstructHighFrequency(HcaInfo hca, Channel[] channels)
        {
            if (hca.HfrGroupCount <= 0) return;
            int storedBands = hca.BaseBandCount + hca.StereoBandCount;
            for (int c = 0; c < hca.ChannelCount; c++)
            {
                if (channels[c].ElementType == ChannelType.IntensityStereoSecondary) continue;

                int destBand = storedBands;
                int sourceBand = storedBands - 1;
                for (int group = 0; group < hca.HfrGroupCount; group++)
                {
                    for (int band = 0; band < hca.BandsPerHfrGroup && destBand < hca.TotalBandCount; band++, sourceBand--)
                    {
                        channels[c].Spectra[destBand++] =
                            ScaleConversionTable[channels[c].HfrScale[group] - channels[c].Scale[sourceBand] + 64] *
                            channels[c].Spectra[sourceBand];
                    }
                }
                channels[c].Spectra[0x7f] = 0;
            }
        }

        private static void ApplyIntensityStereo(HcaInfo hca, Channel[] channels, int index)
        {
            if (hca.StereoBandCount <= 0) return;
            for (int i = 0; i < channels.Length - 1; i++)
            {
                if (channels[i].ElementType != ChannelType.IntensityStereoPrimary) continue;

                var l = channels[i].Spectra;
                var r = channels[i + 1].Spectra;
                float ratioL = IntensityRatioTable[channels[i + 1].Intensity[index]];
                float ratioR = ratioL - 2.0f;
                for (int b = hca.BaseBandCount; b < hca.TotalBandCount; b++)
                {
                    r[b] = l[b] * ratioR;
                    l[b] *= ratioL;
                }
            }
        }

        private static int[] GetChannelTypes(HcaInfo hca)
        {
            int channelsPerTrack = hca.ChannelCount / hca.TrackCount;
            if (hca.StereoBandCount == 0 || channelsPerTrack == 1) { return new int[8]; }

            switch (channelsPerTrack)
            {
                case 2: return new[] { 1, 2 };
                case 3: return new[] { 1, 2, 0 };
                case 4: return hca.ChannelConfig > 0 ? new[] { 1, 2, 0, 0 } : new[] { 1, 2, 1, 2 };
                case 5: return hca.ChannelConfig > 2 ? new[] { 2, 0, 0, 0 } : new[] { 2, 0, 1, 2 };
                case 6: return new[] { 1, 2, 0, 0, 1, 2 };
                case 7: return new[] { 1, 2, 0, 0, 1, 2, 0 };
                case 8: return new[] { 1, 2, 0, 0, 1, 2, 1, 2 };
                default: return new int[channelsPerTrack];
            }
        }

        private static void DeltaDecode(BitReader reader, int deltaBits, int dataBits, int count, int[] output)
        {
            output[0] = reader.ReadInt(dataBits);
            int maxDelta = 1 << (deltaBits - 1);

            for (int i = 1; i < count; i++)
            {
                int delta = reader.ReadOffsetBinary(deltaBits);

                if (delta < maxDelta)
                {
                    output[i] = output[i - 1] + delta;
                }
                else
                {
                    output[i] = reader.ReadInt(dataBits);
                }
            }
        }

        private static void PcmFloatToShort(Channel[] channels, short[][] pcm)
        {
            for (int c = 0; c < channels.Length; c++)
            {
                for (int sf = 0; sf < SubframesPerFrame; sf++)
                {
                    for (int s = 0; s < SubframeLength; s++)
                    {
                        int sample = (int)(channels[c].PcmFloat[sf][s] * (short.MaxValue + 1));
                        pcm[c][sf * SubframeLength + s] = Helpers.Clamp16(sample);
                    }
                }
            }
        }
    }

    public enum ChannelType
    {
        FullStereo = 0,
        IntensityStereoPrimary = 1,
        IntensityStereoSecondary = 2
    }

    public class Channel
    {
        public ChannelType ElementType;
        public int ScaleLength;
        public readonly int[] Scale = new int[0x80];
        public readonly int[] HfrScale = new int[0x80];
        public readonly int[] Intensity = new int[8];
        public readonly int[] Resolution = new int[0x80];
        public readonly int[] QuantizedSpectra = new int[0x80];
        public readonly float[] Gain = new float[0x80];
        public readonly float[] Spectra = new float[0x80];
        public readonly float[] DctTempBuffer = new float[0x80];
        public readonly float[] ImdctPrevious = new float[0x80];
        public readonly float[] DctOutput = new float[0x80];
        public readonly float[][] PcmFloat = Helpers.CreateJaggedArray<float[][]>(8, 0x80);
    }
}
