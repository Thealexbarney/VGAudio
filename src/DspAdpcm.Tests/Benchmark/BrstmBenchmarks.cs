using BenchmarkDotNet.Attributes;
using DspAdpcm.Adpcm;
using DspAdpcm.Adpcm.Formats;

namespace DspAdpcm.Tests.Benchmark
{
    public class BrstmBenchmarks
    {
        [Params(120)]
        public double brstmLength;
        private int sampleRate = 48000;
        private AdpcmStream adpcm;
        private byte[] brstm;

        [Setup]
        public void Setup()
        {
            adpcm = GenerateAudio.GenerateAdpcmEmpty((int)(sampleRate * brstmLength));
            brstm = new Brstm(adpcm).GetFile();
        }

        [Benchmark]
        public byte[] BuildBrstmBenchmark()
        {
            return new Brstm(adpcm).GetFile();
        }

        [Benchmark]
        public Brstm ParseBrstmBenchmark()
        {
            return new Brstm(brstm);
        }
    }
}
