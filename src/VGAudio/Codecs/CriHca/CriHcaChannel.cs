using System;
using VGAudio.Utilities;

namespace VGAudio.Codecs.CriHca
{
    public class CriHcaChannel
    {
        public double[][] PcmFloat { get; } = Helpers.CreateJaggedArray<double[][]>(8, 128);
        public double[][] Spectra { get; } = Helpers.CreateJaggedArray<double[][]>(8, 128);
        public Mdct Mdct { get; } = new Mdct(7, CriHcaTables.MdctWindow, Math.Sqrt(2.0 / 128));
    }
}
