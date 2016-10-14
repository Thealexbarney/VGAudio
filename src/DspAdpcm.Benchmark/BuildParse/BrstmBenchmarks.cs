using BenchmarkDotNet.Attributes;
using DspAdpcm.Adpcm;
using DspAdpcm.Adpcm.Formats;

namespace DspAdpcm.Benchmark.BuildParse
{
    public class BrstmBenchmarks
    {
        [Params(360)]
        public double lengthSeconds;
        [Params(1)]
        public int numChannels;
        private int sampleRate = 48000;
        private AdpcmStream adpcm;
        private byte[] brstm;

        [Setup]
        public void Setup()
        {
            adpcm = GenerateAudio.GenerateAdpcmEmpty((int)(sampleRate * lengthSeconds), numChannels, sampleRate);
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