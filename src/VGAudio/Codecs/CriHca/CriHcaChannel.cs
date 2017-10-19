using System;
using VGAudio.Utilities;
using static VGAudio.Codecs.CriHca.CriHcaConstants;

namespace VGAudio.Codecs.CriHca
{
    public class CriHcaChannel
    {
        public ChannelType Type { get; set; }
        public int CodedScaleFactorCount { get; set; }
        public double[][] PcmFloat { get; } = Helpers.CreateJaggedArray<double[][]>(SubframesPerFrame, SamplesPerSubFrame);
        public double[][] Spectra { get; } = Helpers.CreateJaggedArray<double[][]>(SubframesPerFrame, SamplesPerSubFrame);
        public double[][] ScaledSpectra { get; } = Helpers.CreateJaggedArray<double[][]>(SamplesPerSubFrame, SubframesPerFrame);
        public int[][] QuantizedSpectra { get; } = Helpers.CreateJaggedArray<int[][]>(SubframesPerFrame, SamplesPerSubFrame);
        public double[] Gain { get; } = new double[SamplesPerSubFrame];
        public int[] Intensity { get; } = new int[SubframesPerFrame];
        public int[] HfrScales { get; } = new int[8];
        public double[] HfrGroupAverageSpectra { get; } = new double[8];
        public Mdct Mdct { get; } = new Mdct(SubFrameSamplesBits, CriHcaTables.MdctWindow, Math.Sqrt(2.0 / SamplesPerSubFrame));
        public int[] ScaleFactors { get; } = new int[SamplesPerSubFrame];
        public int[] Resolution { get; } = new int[SamplesPerSubFrame];
        public int HeaderLengthBits { get; set; }
        public int ScaleFactorDeltaBits { get; set; }
    }
}
