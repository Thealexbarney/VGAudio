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
                    Type = channelTypes[i]
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
