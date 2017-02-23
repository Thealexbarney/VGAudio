using BenchmarkDotNet.Attributes;
using DspAdpcm.Containers;
using DspAdpcm.Formats;

namespace DspAdpcm.Benchmark.AdpcmBenchmarks
{
    public class BuildParseBenchmarks
    {
        [Params(300)]
        public double lengthSeconds;
        [Params(2)]
        public int numChannels;
        private int sampleRate = 48000;

        private GcAdpcmFormat adpcm;
        private byte[] brstm;
        private byte[] dsp;
        //private byte[] idsp;

        [Setup]
        public void Setup()
        {
            adpcm = GenerateAudio.GenerateAdpcmEmpty((int)(sampleRate * lengthSeconds), numChannels, sampleRate);
            brstm = new BrstmWriter().GetFile(adpcm);
            dsp = new DspWriter().GetFile(adpcm);
            //idsp = new IdspWriter().GetFile(adpcm);
        }

        [Benchmark] public byte[] BuildDsp() => new DspWriter().GetFile(adpcm);
        [Benchmark] public byte[] BuildBxstm() => new BrstmWriter().GetFile(adpcm);
        //[Benchmark] public byte[] BuildIdsp() => new IdspWriter().GetFile(adpcm);

        [Benchmark] public IAudioFormat ParseDsp() => DspReader.ReadFormat(dsp);
        [Benchmark] public IAudioFormat ParseBxstm() => BrstmReader.ReadFormat(brstm);
        //[Benchmark] public IAudioFormat ParseIdsp() => IdspReader.ReadFormat(idsp);

        [Benchmark] public byte[] RebuildDsp() => new DspWriter().GetFile(DspReader.Read(dsp));
        [Benchmark] public byte[] RebuildBxstm() => new BrstmWriter().GetFile(BrstmReader.Read(brstm));
        //[Benchmark] public byte[] RebuildIdsp() => new IdspWriter().GetFile(IdspReader.Read(idsp));
    }
}