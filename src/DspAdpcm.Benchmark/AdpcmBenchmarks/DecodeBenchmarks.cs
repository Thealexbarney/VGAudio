using BenchmarkDotNet.Attributes;
using DspAdpcm.Formats;

namespace DspAdpcm.Benchmark.AdpcmBenchmarks
{
    public class DecodeBenchmarks
    {
        [Params(1)]
        public double lengthSeconds;
        private int sampleRate = 48000;
        private GcAdpcmFormat adpcmStream;

        [Setup]
        public void Setup()
        {
            adpcmStream = GenerateAudio.GenerateAdpcmEmpty((int)(sampleRate * lengthSeconds), 1, sampleRate);
        }

        [Benchmark]
        public Pcm16Format DecodeAdpcm() => adpcmStream.ToPcm16();
    }
}
