using System;
using VGAudio.Utilities;
using static VGAudio.Codecs.CriHca.CriHcaConstants;
using static VGAudio.Codecs.CriHca.CriHcaPacking;

namespace VGAudio.Codecs.CriHca
{
    public class CriHcaEncoder
    {
        private CriHcaEncoder() { }

        public HcaInfo Hca { get; private set; }
        public CriHcaQuality Quality { get; private set; }
        public int Bitrate { get; private set; }
        public int CutoffFrequency { get; private set; }
        public short[][] PcmBuffer { get; private set; }
        public byte[] HcaBuffer { get; private set; }
        private CriHcaChannel[] Channels { get; set; }
        private CriHcaFrame Frame { get; set; }
        private Crc16 Crc { get; } = new Crc16(0x8005);

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
                TrackCount = 1,
                SampleCount = config.SampleCount,
                SampleRate = config.SampleRate,
                MinResolution = 1,
                MaxResolution = 15,
                InsertedSamples = SamplesPerSubFrame
            };
            Hca.FrameCount = ((Hca.SampleCount + Hca.InsertedSamples + 1023) / 1024 << 10) / 1024;

            CutoffFrequency = config.SampleRate / 2;
            Quality = config.Quality;

            Bitrate = CalculateBitrate(Hca, Quality, config.LimitBitrate);
            CalculateBandCounts(Hca, Bitrate, CutoffFrequency);

            Frame = new CriHcaFrame(Hca);
            Channels = Frame.Channels;

            PcmBuffer = Helpers.CreateJaggedArray<short[][]>(Hca.ChannelCount, 1024);
            HcaBuffer = new byte[Hca.FrameSize];
        }

        public void EncodeFrame()
        {
            PcmToFloat(PcmBuffer, Channels);
            RunMdct(Channels);
            CalculateScaleFactors(Channels);
            ScaleSpectra(Channels);
            CalculateFrameHeaderLength(Frame);
            CalculateNoiseLevel(Frame);
            CalculateEvaluationBoundary(Frame);
            CalculateFrameResolutions(Frame);
            QuantizeSpectra(Channels);
            PackFrame(Frame, Crc, HcaBuffer);
        }

        private int CalculateBitrate(HcaInfo hca, CriHcaQuality quality, bool limitBitrate)
        {
            int pcmBitrate = Hca.SampleRate * Hca.ChannelCount * 16;
            int maxBitrate = pcmBitrate / 4;
            int minBitrate = 0;

            int compressionRatio = 6;
            switch (quality)
            {
                case CriHcaQuality.Highest:
                    compressionRatio = 4;
                    break;
                case CriHcaQuality.High:
                    compressionRatio = 6;
                    break;
                case CriHcaQuality.Middle:
                    compressionRatio = 8;
                    break;
                case CriHcaQuality.Low:
                    compressionRatio = hca.ChannelCount == 1 ? 10 : 12;
                    break;
                case CriHcaQuality.Lowest:
                    compressionRatio = hca.ChannelCount == 1 ? 12 : 16;
                    break;
            }

            int bitrate = pcmBitrate / compressionRatio;

            if (limitBitrate)
            {
                minBitrate = Math.Min(
                    hca.ChannelCount == 1 ? 42666 : 32000 * hca.ChannelCount,
                    pcmBitrate / 6);
            }

            return Helpers.Clamp(bitrate, minBitrate, maxBitrate);
        }

        private static void CalculateBandCounts(HcaInfo hca, int bitrate, int cutoffFreq)
        {
            hca.FrameSize = bitrate * 1024 / hca.SampleRate / 8;
            int numGroups = 0;
            int pcmBitrate = hca.SampleRate * hca.ChannelCount * 16;
            int hfrRatio; // HFR is used at bitrates below (pcmBitrate / hfrRatio)
            int cutoffRatio; // The cutoff frequency is lowered at bitrates below (pcmBitrate / cutoffRatio)

            if (hca.ChannelCount <= 1 || pcmBitrate / bitrate <= 6)
            {
                hfrRatio = 6;
                cutoffRatio = 12;
            }
            else
            {
                hfrRatio = 8;
                cutoffRatio = 16;
            }

            if (bitrate < pcmBitrate / cutoffRatio)
            {
                cutoffFreq = Math.Min(cutoffFreq, cutoffRatio * bitrate / (32 * hca.ChannelCount));
            }

            int totalBandCount = (int)Math.Round(cutoffFreq * 256d / hca.SampleRate);

            int hfrStartBand = (int)Math.Min(totalBandCount, Math.Round((hfrRatio * bitrate * 128d) / pcmBitrate));
            int stereoStartBand = hfrRatio == 6 ? hfrStartBand : (hfrStartBand + 1) / 2;

            int hfrBandCount = totalBandCount - hfrStartBand;
            int bandsPerGroup = hfrBandCount.DivideByRoundUp(8);

            if (bandsPerGroup > 0)
            {
                numGroups = hfrBandCount.DivideByRoundUp(bandsPerGroup);
            }

            hca.TotalBandCount = totalBandCount;
            hca.BaseBandCount = stereoStartBand;
            hca.StereoBandCount = hfrStartBand - stereoStartBand;
            hca.HfrGroupCount = numGroups;
            hca.BandsPerHfrGroup = bandsPerGroup;
        }

        private static void QuantizeSpectra(CriHcaChannel[] channels)
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
                                                          CriHcaTables.ResolutionMaxValues[resolution];
                    }
                }
            }
        }

        private static void CalculateFrameResolutions(CriHcaFrame frame)
        {
            foreach (CriHcaChannel channel in frame.Channels)
            {
                for (int i = 0; i < frame.EvaluationBoundary; i++)
                {
                    channel.Resolution[i] = CalculateResolution(channel.ScaleFactors[i], frame.AcceptableNoiseLevel - 1);
                }
                for (int i = frame.EvaluationBoundary; i < channel.CodedScaleFactorCount; i++)
                {
                    channel.Resolution[i] = CalculateResolution(channel.ScaleFactors[i], frame.AcceptableNoiseLevel);
                }
                Array.Clear(channel.Resolution, channel.CodedScaleFactorCount, channel.Resolution.Length - channel.CodedScaleFactorCount);
            }
        }

        private static void CalculateNoiseLevel(CriHcaFrame frame)
        {
            int availableBits = frame.Hca.FrameSize * 8;
            int maxLevel = 255;
            int minLevel = 0;
            int level = BinarySearchLevel(frame.Channels, availableBits, minLevel, maxLevel);
            frame.AcceptableNoiseLevel = level >= 0 ? level : throw new NotImplementedException();
        }

        private static void CalculateEvaluationBoundary(CriHcaFrame frame)
        {
            if (frame.AcceptableNoiseLevel == 0)
            {
                frame.EvaluationBoundary = 0;
                return;
            }

            int availableBits = frame.Hca.FrameSize * 8;
            int maxLevel = 127;
            int minLevel = 0;
            int level = BinarySearchBoundary(frame.Channels, availableBits, frame.AcceptableNoiseLevel, minLevel, maxLevel);
            frame.EvaluationBoundary = level >= 0 ? level : throw new NotImplementedException();
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
                                               CriHcaTables.ResolutionMaxValues[resolution];
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

        private static void CalculateFrameHeaderLength(CriHcaFrame frame)
        {
            foreach (CriHcaChannel channel in frame.Channels)
            {
                CalculateOptimalDeltaLength(channel);
                if (channel.Type == ChannelType.StereoSecondary) channel.ScaleFactorBits += 32;
                else if (frame.Hca.HfrGroupCount > 0) channel.ScaleFactorBits += 6 * frame.Hca.HfrGroupCount;
            }
        }

        private static void CalculateOptimalDeltaLength(CriHcaChannel channel)
        {
            bool emptyChannel = true;
            for (int i = 0; i < channel.CodedScaleFactorCount; i++)
            {
                if (channel.ScaleFactors[i] != 0)
                {
                    emptyChannel = false;
                    break;
                }
            }

            if (emptyChannel)
            {
                channel.ScaleFactorBits = 3;
                channel.ScaleFactorDeltaBits = 0;
                return;
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

        private static void ScaleSpectra(CriHcaChannel[] channels)
        {
            foreach (CriHcaChannel channel in channels)
            {
                for (int b = 0; b < channel.CodedScaleFactorCount; b++)
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

        private static void CalculateScaleFactors(CriHcaChannel[] channels)
        {
            foreach (CriHcaChannel channel in channels)
            {
                for (int b = 0; b < channel.CodedScaleFactorCount; b++)
                {
                    double max = 0;
                    for (int sf = 0; sf < SubframesPerFrame; sf++)
                    {
                        var coeff = Math.Abs(channel.Spectra[sf][b]);
                        max = Math.Max(coeff, max);
                    }
                    channel.ScaleFactors[b] = FindScaleFactor(max);
                }
                Array.Clear(channel.ScaleFactors, channel.CodedScaleFactorCount, channel.ScaleFactors.Length - channel.CodedScaleFactorCount);
            }
        }

        private static int FindScaleFactor(double value)
        {
            float[] sf = CriHcaTables.DequantizerScalingTable;
            for (int i = 0; i < sf.Length; i++)
            {
                if (sf[i] > value) return i;
            }
            return 63;
        }

        private static void RunMdct(CriHcaChannel[] channels)
        {
            foreach (CriHcaChannel channel in channels)
            {
                for (int sf = 0; sf < SubframesPerFrame; sf++)
                {
                    channel.Mdct.RunMdct(channel.PcmFloat[sf], channel.Spectra[sf]);
                }
            }
        }

        private static void PcmToFloat(short[][] pcm, CriHcaChannel[] channels)
        {
            for (int c = 0; c < channels.Length; c++)
            {
                int pcmIdx = 0;
                for (int sf = 0; sf < SubframesPerFrame; sf++)
                {
                    for (int i = 0; i < SamplesPerSubFrame; i++)
                    {
                        channels[c].PcmFloat[sf][i] = pcm[c][pcmIdx++] * (1f / 32768f);
                    }
                }
            }
        }
    }
}
