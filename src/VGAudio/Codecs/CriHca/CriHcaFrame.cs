using System;
using VGAudio.Utilities;
using static VGAudio.Codecs.CriHca.ChannelType;

namespace VGAudio.Codecs.CriHca
{
    public class CriHcaFrame
    {
        private const int SubframesPerFrame = 8;
        private const int FrameSizeBits = 7;
        private const int FrameSize = 1 << FrameSizeBits;

        public HcaInfo Hca { get; }
        public int ChannelCount { get; }
        public int[] ScaleLength { get; }
        public ChannelType[] ChannelType { get; }
        public int[][] Scale { get; }
        public int[][] Intensity { get; }
        public int[][] Resolution { get; }
        public int[][] HfrScale { get; }
        public float[][] Gain { get; }
        public int[][][] QuantizedSpectra { get; }
        public double[][][] Spectra { get; }
        public double[][][] PcmFloat { get; }
        public Mdct[] Mdct { get; }
        public byte[] AthCurve { get; } = new byte[128];

        public CriHcaFrame(HcaInfo hca)
        {
            Hca = hca;
            ChannelCount = hca.ChannelCount;
            ChannelType = GetChannelTypes(hca);
            ScaleLength = new int[hca.ChannelCount];
            Mdct = new Mdct[hca.ChannelCount];
            Scale = Helpers.CreateJaggedArray<int[][]>(ChannelCount, FrameSize);
            Intensity = Helpers.CreateJaggedArray<int[][]>(ChannelCount, 8);
            Resolution = Helpers.CreateJaggedArray<int[][]>(ChannelCount, FrameSize);
            HfrScale = Helpers.CreateJaggedArray<int[][]>(ChannelCount, FrameSize);
            Gain = Helpers.CreateJaggedArray<float[][]>(ChannelCount, FrameSize);
            QuantizedSpectra = Helpers.CreateJaggedArray<int[][][]>(SubframesPerFrame, ChannelCount, FrameSize);
            Spectra = Helpers.CreateJaggedArray<double[][][]>(SubframesPerFrame, ChannelCount, FrameSize);
            PcmFloat = Helpers.CreateJaggedArray<double[][][]>(SubframesPerFrame, ChannelCount, FrameSize);

            for (int i = 0; i < hca.ChannelCount; i++)
            {
                ScaleLength[i] = hca.BaseBandCount;
                if (ChannelType[i] != StereoSecondary) ScaleLength[i] += hca.StereoBandCount;
                Mdct[i] = new Mdct(FrameSizeBits, CriHcaTables.MdctWindow, Math.Sqrt(2.0 / FrameSize));
            }

            if (hca.UseAthCurve)
            {
                AthCurve = ScaleAthCurve(hca.SampleRate);
            }
        }

        private static ChannelType[] GetChannelTypes(HcaInfo hca)
        {
            int channelsPerTrack = hca.ChannelCount / hca.TrackCount;
            if (hca.StereoBandCount == 0 || channelsPerTrack == 1) { return new ChannelType[8]; }

            switch (channelsPerTrack)
            {
                case 2: return new[] { StereoPrimary, StereoSecondary };
                case 3: return new[] { StereoPrimary, StereoSecondary, Discrete };
                case 4 when hca.ChannelConfig != 0: return new[] { StereoPrimary, StereoSecondary, Discrete, Discrete };
                case 4 when hca.ChannelConfig == 0: return new[] { StereoPrimary, StereoSecondary, StereoPrimary, StereoSecondary };
                case 5 when hca.ChannelConfig > 2: return new[] { StereoPrimary, StereoSecondary, Discrete, Discrete, Discrete };
                case 5 when hca.ChannelConfig <= 2: return new[] { StereoPrimary, StereoSecondary, Discrete, StereoPrimary, StereoSecondary };
                case 6: return new[] { StereoPrimary, StereoSecondary, Discrete, Discrete, StereoPrimary, StereoSecondary };
                case 7: return new[] { StereoPrimary, StereoSecondary, Discrete, Discrete, StereoPrimary, StereoSecondary, Discrete };
                case 8: return new[] { StereoPrimary, StereoSecondary, Discrete, Discrete, StereoPrimary, StereoSecondary, StereoPrimary, StereoSecondary };
                default: return new ChannelType[channelsPerTrack];
            }
        }

        /// <summary>
        /// Scales an ATH curve to the specified frequency.
        /// </summary>
        /// <param name="frequency">The frequency to scale the curve to.</param>
        /// <returns>The scaled ATH curve</returns>
        /// <remarks>The original ATH curve is for a frequency of 41856 Hz.</remarks>
        private static byte[] ScaleAthCurve(int frequency)
        {
            var ath = new byte[128];

            int acc = 0;
            int i;
            for (i = 0; i < ath.Length; i++)
            {
                acc += frequency;
                int index = acc >> 13;
                
                if (index >= CriHcaTables.AthCurve.Length)
                {
                    break;
                }
                ath[i] = CriHcaTables.AthCurve[index];
            }

            for (; i < ath.Length; i++)
            {
                ath[i] = 0xff;
            }

            return ath;
        }
    }
}
