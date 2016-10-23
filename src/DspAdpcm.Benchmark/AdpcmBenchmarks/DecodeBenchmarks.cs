using BenchmarkDotNet.Attributes;
using DspAdpcm.Adpcm;
using DspAdpcm.Pcm;

namespace DspAdpcm.Benchmark.AdpcmBenchmarks
{
    public class DecodeBenchmarks
    {
        [Params(1)]
        public double lengthSeconds;
        private int sampleRate = 48000;
        private AdpcmStream adpcmStream;

        [Setup]
        public void Setup()
        {
            adpcmStream = GenerateAudio.GenerateAdpcmEmpty((int)(sampleRate * lengthSeconds), 1, sampleRate);
        }

        [Benchmark]
        public PcmStream DecodeAdpcm() => Decode.AdpcmtoPcm(adpcmStream);
    }
}
