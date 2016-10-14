using BenchmarkDotNet.Attributes;
using DspAdpcm.Adpcm;
using DspAdpcm.Adpcm.Formats;

namespace DspAdpcm.Benchmark.BuildParse
{
    public class IdspBenchmarks
    {
        [Params(360)]
        public double lengthSeconds;
        [Params(8)]
        public int numChannels;
        private int sampleRate = 48000;
        private AdpcmStream adpcm;
        private byte[] idsp;

        [Setup]
        public void Setup()
        {
            adpcm = GenerateAudio.GenerateAdpcmEmpty((int)(sampleRate * lengthSeconds), numChannels, sampleRate);
            idsp = new Idsp(adpcm).GetFile();
        }

        [Benchmark]
        public byte[] BuildIdspBenchmark()
        {
            return new Idsp(adpcm).GetFile();
        }

        [Benchmark]
        public Idsp ParseIdspBenchmark()
        {
            return new Idsp(idsp);
        }
    }
}