using BenchmarkDotNet.Attributes;
using DspAdpcm.Adpcm;
using DspAdpcm.Adpcm.Formats;

namespace DspAdpcm.Benchmark.BuildParse
{
    public class BuildParseBenchmarks
    {
        [Params(300)]
        public double lengthSeconds;
        [Params(2)]
        public int numChannels;
        private int sampleRate = 48000;

        private AdpcmStream adpcm;
        private byte[] brstm;
        private byte[] dsp;
        private byte[] idsp;

        [Setup]
        public void Setup()
        {
            adpcm = GenerateAudio.GenerateAdpcmEmpty((int)(sampleRate * lengthSeconds), numChannels, sampleRate);
            brstm = new Brstm(adpcm).GetFile();
            idsp = new Idsp(adpcm).GetFile();
            dsp = new Dsp(adpcm).GetFile();
        }

        [Benchmark] public byte[] BuildDsp() => new Dsp(adpcm).GetFile();
        [Benchmark] public byte[] BuildBxstm() => new Brstm(adpcm).GetFile();
        [Benchmark] public byte[] BuildIdsp() => new Idsp(adpcm).GetFile();

        [Benchmark] public Dsp ParseDsp() => new Dsp(dsp);
        [Benchmark] public Brstm ParseBxstm() => new Brstm(brstm);
        [Benchmark] public Idsp ParseIdsp() => new Idsp(idsp);

        [Benchmark] public byte[] RebuildDsp() => new Dsp(dsp).GetFile();
        [Benchmark] public byte[] RebuildBxstm() => new Brstm(brstm).GetFile();
        [Benchmark] public byte[] RebuildIdsp() => new Idsp(idsp).GetFile();
    }
}