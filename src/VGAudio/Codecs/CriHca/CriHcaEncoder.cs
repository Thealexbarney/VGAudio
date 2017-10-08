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
            CalculateScaleToResolution(Channels);
            AcceptableNoiseLevel = CalculateNoiseLevel(Channels, Hca);
            EvaluationBoundary = CalculateEvaluationBoundary(Channels, Hca, AcceptableNoiseLevel);
        }

        private static int CalculateNoiseLevel(CriHcaChannel[] channels, HcaInfo hca)
        {
            int availableBits = hca.FrameSize * 8;
            int maxLevel = 511;
            int minLevel = 0;
            return BinarySearchLevel(channels, availableBits, minLevel, maxLevel);
        }

        private static int CalculateEvaluationBoundary(CriHcaChannel[] channels, HcaInfo hca, int noiseLevel)
        {
            int availableBits = hca.FrameSize * 8;
            int maxLevel = 127;
            int minLevel = 0;
            return BinarySearchBoundary(channels, availableBits, noiseLevel, minLevel, maxLevel);
        }

        private static int BinarySearchLevel(CriHcaChannel[] channels, int availableBits, int lo, int hi)
        {
            int hiValue = CalculateUsedBits(channels, hi, 0);
            int loValue = CalculateUsedBits(channels, lo, 0);

            do
            {
                int mid = (lo + hi) / 2;
                int midValue = CalculateUsedBits(channels, mid, 0);
                if (midValue < availableBits)
                {
                    hi = mid;
                    hiValue = midValue;
                }
                else
                {
                    lo = mid;
                    loValue = midValue;
                }
            } while (hi - lo > 1);

            return hiValue > availableBits ? lo : hi;
        }

        private static int BinarySearchBoundary(CriHcaChannel[] channels, int availableBits, int noiseLevel, int lo, int hi)
        {
            int hiValue = CalculateUsedBits(channels, noiseLevel, hi);
            int loValue = CalculateUsedBits(channels, noiseLevel, lo);

            do
            {
                int mid = (lo + hi) / 2;
                int midValue = CalculateUsedBits(channels, noiseLevel, mid);
                if (midValue > availableBits)
                {
                    hi = mid;
                    hiValue = midValue;
                }
                else
                {
                    lo = mid;
                    loValue = midValue;
                }
            } while (hi - lo > 1);

            return hiValue > availableBits ? lo : hi;
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
                    int resolution = CalculateResolution(channel.ScaleToResolution[i], noise);

                    for (int sf = 0; sf < 8; sf++)
                    {
                        double value = channel.ScaledSpectra[sf][i] + 1;
                        channel.QuantizedSpectra[sf][i] = (int)(value / CriHcaTables.DequantizerNormalizeTable[resolution]) -
                                                                 CriHcaTables.ResolutionLevelsTable[resolution] / 2;
                        length += CalculateBitsUsedBySpectra(channel.QuantizedSpectra[sf][i], resolution);
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
            return CriHcaTables.QuantizeSpectrumBits[resolution, quantizedSpectra + 8];
        }

        private static int CalculateResolution(int resolution, int noiseLevel)
        {
            if (resolution < 4)
            {
                return 0;
            }
            var res = Helpers.Clamp(noiseLevel - resolution + 9, 0, 71);
            return CriHcaTables.ScaleToResolutionCurveEncode[res];
        }


        private static void CalculateScaleToResolution(CriHcaChannel[] channels)
        {
            foreach (CriHcaChannel channel in channels)
            {
                for (int i = 0; i < channel.CodedScaleFactorCount; i++)
                {
                    channel.ScaleToResolution[i] = (5 * channel.ScaleFactors[i] >> 1) + 2;
                }

                for (int i = channel.CodedScaleFactorCount; i < 128; i++)
                {
                    channel.ScaleToResolution[i] = 0;
                }
            }
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
                var bands = channel.Type == ChannelType.StereoSecondary ? hca.BaseBandCount : hca.TotalBandCount;

                for (int b = 0; b < bands; b++)
                {
                    var scaleFactor = channel.ScaleFactors[b];
                    for (int sf = 0; sf < 8; sf++)
                    {
                        var coeff = channel.Spectra[sf][b];
                        channel.ScaledSpectra[sf][b] = scaleFactor == 0 ? 0 :
                            coeff / CriHcaTables.DequantizerScalingTable[scaleFactor];
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
