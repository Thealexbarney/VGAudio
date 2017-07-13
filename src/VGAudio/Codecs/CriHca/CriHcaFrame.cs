using System.Linq;
using VGAudio.Utilities;

namespace VGAudio.Codecs.CriHca
{
    public class CriHcaFrame
    {
        private const int SubframesPerFrame = 8;

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
        public float[][][] Spectra { get; }
        public float[][][] PcmFloat { get; }
        public float[] DctTempBuffer { get; } = new float[0x80];
        public float[][] ImdctPrevious { get; }
        public float[][] DctOutput { get; }


        public CriHcaFrame(HcaInfo hca)
        {
            Hca = hca;
            ChannelCount = hca.ChannelCount;
            ChannelType = GetChannelTypes(hca).Select(x => (ChannelType)x).ToArray();
            ScaleLength = new int[hca.ChannelCount];
            Scale = Helpers.CreateJaggedArray<int[][]>(ChannelCount, 0x80);
            Intensity = Helpers.CreateJaggedArray<int[][]>(ChannelCount, 8);
            Resolution = Helpers.CreateJaggedArray<int[][]>(ChannelCount, 0x80);
            HfrScale = Helpers.CreateJaggedArray<int[][]>(ChannelCount, 0x80);
            Gain = Helpers.CreateJaggedArray<float[][]>(ChannelCount, 0x80);
            QuantizedSpectra = Helpers.CreateJaggedArray<int[][][]>(SubframesPerFrame, ChannelCount, 0x80);
            Spectra = Helpers.CreateJaggedArray<float[][][]>(SubframesPerFrame, ChannelCount, 0x80);
            PcmFloat = Helpers.CreateJaggedArray<float[][][]>(SubframesPerFrame, ChannelCount, 0x80);
            ImdctPrevious = Helpers.CreateJaggedArray<float[][]>(ChannelCount, 0x80);
            DctOutput = Helpers.CreateJaggedArray<float[][]>(ChannelCount, 0x80);

            for (int i = 0; i < hca.ChannelCount; i++)
            {
                ScaleLength[i] = hca.BaseBandCount;
                if (ChannelType[i] != CriHca.ChannelType.IntensityStereoSecondary) ScaleLength[i] += hca.StereoBandCount;
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
    }
}
