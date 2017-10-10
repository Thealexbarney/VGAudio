using System;
using VGAudio.Utilities;

namespace VGAudio.Codecs.CriHca
{
    public class CriHcaEncoder
    {
        private CriHcaEncoder() { }

        public HcaInfo Hca { get; private set; }
        public int Quality { get; private set; }
        public int Bitrate { get; private set; }
        public int CutoffFrequency { get; private set; }
        public short[][] PcmBuffer { get; private set; }
        public byte[] HcaBuffer { get; private set; }
        private CriHcaChannel[] Channels { get; set; }
        private int AcceptableNoiseLevel { get; set; }
        private int EvaluationBoundary { get; set; }
        private Crc16 Crc { get; } = new Crc16(0x8005);

        private const int MinResolution = 1;
        private const int MaxResolution = 15;

        public static CriHcaEncoder InitializeNew(CriHcaParameters config)
        {
            var encoder = new CriHcaEncoder();
            encoder.Initialize(config);
            return encoder;
        }

        public void Initialize(CriHcaParameters config)
        {
            Hca = new HcaInfo
            {
                ChannelCount = config.ChannelCount,
                TrackCount = config.ChannelCount,
                SampleCount = config.SampleCount,
                SampleRate = config.SampleRate,
                MinResolution = 1,
                MaxResolution = 15,
                InsertedSamples = 128
            };

            CutoffFrequency = config.SampleRate / 2;
            Quality = config.Quality;

            int pcmBitrate = Hca.SampleRate * Hca.ChannelCount * 16;
            Bitrate = pcmBitrate / 6;
            CalculateBandCounts(Hca, Bitrate);

            Hca.FrameCount = ((Hca.SampleCount + Hca.InsertedSamples + 1023) / 1024 << 10) / 1024;

            PcmBuffer = Helpers.CreateJaggedArray<short[][]>(Hca.ChannelCount, 1024);
            HcaBuffer = new byte[Hca.FrameSize];

            var channelTypes = CriHcaFrame.GetChannelTypes(Hca);
            Channels = new CriHcaChannel[Hca.ChannelCount];
            for (int i = 0; i < Channels.Length; i++)
            {
                Channels[i] = new CriHcaChannel
                {
                    Type = channelTypes[i],
                    CodedScaleFactorCount = channelTypes[i] == ChannelType.StereoSecondary
                        ? Hca.BaseBandCount + Hca.StereoBandCount
                        : Hca.BaseBandCount
                };
            }
        }

        private void CalculateBandCounts(HcaInfo hca, int bitrate)
        {
            hca.FrameSize = bitrate * 1024 / hca.SampleRate / 8;
            hca.TotalBandCount = 128;
            hca.BaseBandCount = 128;
        }

        public void EncodeFrame()
        {
            PcmToFloat(PcmBuffer, Channels);
            RunMdct(Channels);
            CalculateScaleFactors(Channels, Hca);
            ScaleSpectra(Channels, Hca);
            CalculateScaleFactorLength(Channels);
            AcceptableNoiseLevel = CalculateNoiseLevel(Channels, Hca);
            EvaluationBoundary = CalculateEvaluationBoundary(Channels, Hca, AcceptableNoiseLevel);
            CalculateFrameResolutions(Channels, AcceptableNoiseLevel, EvaluationBoundary);
            QuantizeSpectra(Channels);
            PackFrame(Channels);
        }

        private void PackFrame(CriHcaChannel[] channels)
        {
            var writer = new BitWriter(HcaBuffer);
            writer.Write(0xffff, 16);
            writer.Write(AcceptableNoiseLevel, 9);
            writer.Write(EvaluationBoundary, 7);

            foreach (CriHcaChannel channel in channels)
            {
                WriteScaleFactors(writer, channel);
            }

            for (int sf = 0; sf < 8; sf++)
            {
                foreach (CriHcaChannel channel in channels)
                {
                    WriteSpectra(writer, channel, sf);
                }
            }

            writer.AlignPosition(8);
            for (int i = writer.Position / 8; i < Hca.FrameSize - 2; i++)
            {
                writer.Buffer[i] = 0;
            }

            WriteChecksum(writer);
        }

        private void WriteChecksum(BitWriter writer)
        {
            writer.Position = writer.LengthBits - 16;
            var crc16 = Crc.Compute(HcaBuffer, HcaBuffer.Length - 2);
            writer.Write(crc16, 16);
        }

        private void WriteSpectra(BitWriter writer, CriHcaChannel channel, int subFrame)
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

        private void WriteScaleFactors(BitWriter writer, CriHcaChannel channel)
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

        private void QuantizeSpectra(CriHcaChannel[] channels)
        {
            foreach (CriHcaChannel channel in channels)
            {
                for (int i = 0; i < channel.CodedScaleFactorCount; i++)
                {
                    var scaled = channel.ScaledSpectra[i];
                    int resolution = channel.Resolution[i];
                    for (int sf = 0; sf < scaled.Length; sf++)
                    {
                        double value = scaled[sf] + 1;
                        channel.QuantizedSpectra[sf][i] = (int)(value * CriHcaTables.QuantizerRangeTable[resolution]) -
                                                          CriHcaTables.ResolutionLevelsTable[resolution] / 2;
                    }
                }
            }
        }

        private static void CalculateFrameResolutions(CriHcaChannel[] channels, int noiseLevel, int evalBoundary)
        {
            foreach (CriHcaChannel channel in channels)
            {
                for (int i = 0; i < evalBoundary; i++)
                {
                    channel.Resolution[i] = CalculateResolution(channel.ScaleFactors[i], noiseLevel - 1);
                }
                for (int i = evalBoundary; i < channel.CodedScaleFactorCount; i++)
                {
                    channel.Resolution[i] = CalculateResolution(channel.ScaleFactors[i], noiseLevel);
                }
                Array.Clear(channel.Resolution, channel.CodedScaleFactorCount, channel.Resolution.Length - channel.CodedScaleFactorCount);
            }
        }

        private static int CalculateNoiseLevel(CriHcaChannel[] channels, HcaInfo hca)
        {
            int availableBits = hca.FrameSize * 8;
            int maxLevel = 255;
            int minLevel = 0;
            int level = BinarySearchLevel(channels, availableBits, minLevel, maxLevel);
            return level >= 0 ? level : throw new NotImplementedException();
        }

        private static int CalculateEvaluationBoundary(CriHcaChannel[] channels, HcaInfo hca, int noiseLevel)
        {
            int availableBits = hca.FrameSize * 8;
            int maxLevel = 127;
            int minLevel = 0;
            int level = BinarySearchBoundary(channels, availableBits, noiseLevel, minLevel, maxLevel);
            return level >= 0 ? level : throw new NotImplementedException();
        }

        private static int BinarySearchLevel(CriHcaChannel[] channels, int availableBits, int low, int high)
        {
            int max = high;
            int midValue = 0;

            while (low != high)
            {
                int mid = (low + high) / 2;
                midValue = CalculateUsedBits(channels, mid, 0);

                if (midValue > availableBits)
                {
                    low = mid + 1;
                }
                else if (midValue <= availableBits)
                {
                    high = mid;
                }
            }

            return low == max && midValue > availableBits ? -1 : low;
        }

        private static int BinarySearchBoundary(CriHcaChannel[] channels, int availableBits, int noiseLevel, int low, int high)
        {
            int max = high;

            while (Math.Abs(high - low) > 1)
            {
                int mid = (low + high) / 2;
                int midValue = CalculateUsedBits(channels, noiseLevel, mid);

                if (availableBits < midValue)
                {
                    high = mid - 1;
                }
                else if (availableBits >= midValue)
                {
                    low = mid;
                }
            }

            if (low == high)
            {
                return low < max ? low : -1;
            }

            int hiValue = CalculateUsedBits(channels, noiseLevel, high);

            return hiValue > availableBits ? low : high;
        }

        private static int CalculateUsedBits(CriHcaChannel[] channels, int noiseLevel, int evalBoundary)
        {
            int length = 16 + 16 + 16; // Sync word, noise level and checksum

            foreach (CriHcaChannel channel in channels)
            {
                length += channel.ScaleFactorBits;
                for (int i = 0; i < channel.CodedScaleFactorCount; i++)
                {
                    int noise = i < evalBoundary ? noiseLevel - 1 : noiseLevel;
                    int resolution = CalculateResolution(channel.ScaleFactors[i], noise);

                    foreach (double scaledSpectra in channel.ScaledSpectra[i])
                    {
                        double value = scaledSpectra + 1;
                        int quantizedSpectra = (int)(value * CriHcaTables.QuantizerRangeTable[resolution]) -
                                               CriHcaTables.ResolutionLevelsTable[resolution] / 2;
                        length += CalculateBitsUsedBySpectra(quantizedSpectra, resolution);
                    }
                }
            }

            return length;
        }

        private static int CalculateBitsUsedBySpectra(int quantizedSpectra, int resolution)
        {
            if (resolution >= 8)
            {
                return CriHcaTables.QuantizedSpectrumMaxBits[resolution] - (quantizedSpectra == 0 ? 1 : 0);
            }
            return CriHcaTables.QuantizeSpectrumBits[resolution][quantizedSpectra + 8];
        }

        private static int CalculateResolution(int scaleFactor, int noiseLevel)
        {
            if (scaleFactor == 0)
            {
                return 0;
            }
            int curvePosition = noiseLevel - 5 * scaleFactor / 2 + 2;
            curvePosition = Helpers.Clamp(curvePosition, 0, 58);
            return CriHcaTables.ScaleToResolutionCurve[curvePosition];
        }

        private static void CalculateScaleFactorLength(CriHcaChannel[] channels)
        {
            foreach (CriHcaChannel channel in channels)
            {
                CalculateOptimalDeltaLength(channel);
                if (channel.Type == ChannelType.StereoSecondary) channel.ScaleFactorBits += 32;
            }
        }

        private static void CalculateOptimalDeltaLength(CriHcaChannel channel)
        {
            var emptyChannel = true;
            for (int i = 0; i < channel.CodedScaleFactorCount; i++)
            {
                if (i != 0)
                {
                    emptyChannel = false;
                    break;
                }
            }

            if (emptyChannel)
            {
                channel.ScaleFactorBits = 3;
                channel.ScaleFactorDeltaBits = 0;
            }

            int minDeltaBits = 6;
            int minLength = 3 + 6 * channel.CodedScaleFactorCount;

            for (int deltaBits = 1; deltaBits < 6; deltaBits++)
            {
                int maxDelta = (1 << (deltaBits - 1)) - 1;
                int length = 3 + 6;
                for (int band = 1; band < channel.CodedScaleFactorCount; band++)
                {
                    int delta = channel.ScaleFactors[band] - channel.ScaleFactors[band - 1];
                    length += Math.Abs(delta) > maxDelta ? deltaBits + 6 : deltaBits;
                }
                if (length < minLength)
                {
                    minLength = length;
                    minDeltaBits = deltaBits;
                }
            }

            channel.ScaleFactorBits = minLength;
            channel.ScaleFactorDeltaBits = minDeltaBits;
        }

        private static void ScaleSpectra(CriHcaChannel[] channels, HcaInfo hca)
        {
            foreach (CriHcaChannel channel in channels)
            {
                int bands = channel.Type == ChannelType.StereoSecondary ? hca.BaseBandCount : hca.TotalBandCount;

                for (int b = 0; b < bands; b++)
                {
                    double[] scaledSpectra = channel.ScaledSpectra[b];
                    int scaleFactor = channel.ScaleFactors[b];
                    for (int sf = 0; sf < scaledSpectra.Length; sf++)
                    {
                        double coeff = channel.Spectra[sf][b];
                        scaledSpectra[sf] = scaleFactor == 0 ? 0 :
                            coeff * CriHcaTables.QuantizerScalingTable[scaleFactor];
                    }
                }
            }
        }

        private static void CalculateScaleFactors(CriHcaChannel[] channels, HcaInfo hca)
        {
            foreach (CriHcaChannel channel in channels)
            {
                var bands = channel.Type == ChannelType.StereoSecondary ? hca.BaseBandCount : hca.TotalBandCount;

                for (int b = 0; b < bands; b++)
                {
                    double max = 0;
                    for (int sf = 0; sf < 8; sf++)
                    {
                        var coeff = Math.Abs(channel.Spectra[sf][b]);
                        max = Math.Max(coeff, max);
                    }
                    channel.ScaleFactors[b] = FindScaleFactor(max);
                }
                Array.Clear(channel.ScaleFactors, bands, channel.ScaleFactors.Length - bands);
            }
        }

        private static int FindScaleFactor(double value)
        {
            var sf = CriHcaTables.DequantizerScalingTable;
            for (int i = 0; i < sf.Length; i++)
            {
                if (sf[i] > value) return i;
            }
            return 63;
        }

        private void RunMdct(CriHcaChannel[] channels)
        {
            for (int c = 0; c < channels.Length; c++)
            {
                for (int sf = 0; sf < 8; sf++)
                {
                    channels[c].Mdct.RunMdct(channels[c].PcmFloat[sf], channels[c].Spectra[sf]);
                }
            }
        }

        private void PcmToFloat(short[][] pcm, CriHcaChannel[] channels)
        {
            for (int c = 0; c < channels.Length; c++)
            {
                int pcmIdx = 0;
                for (int sf = 0; sf < 8; sf++)
                {
                    for (int i = 0; i < 128; i++)
                    {
                        channels[c].PcmFloat[sf][i] = pcm[c][pcmIdx++] * (1f / 32768f);
                    }
                }
            }
        }
    }
}
