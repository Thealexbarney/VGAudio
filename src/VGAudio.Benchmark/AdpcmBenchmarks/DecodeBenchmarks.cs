using BenchmarkDotNet.Attributes;
using VGAudio.Formats.GcAdpcm;
using VGAudio.Formats.Pcm16;

namespace VGAudio.Benchmark.AdpcmBenchmarks
{
    public class DecodeBenchmarks
    {
        [Params(1)]
        public double LengthSeconds;
        private int _sampleRate = 48000;
        private GcAdpcmFormat _adpcmStream;

        [GlobalSetup]
        public void Setup()
        {
            _adpcmStream = GenerateAudio.GenerateAdpcmEmpty((int)(_sampleRate * LengthSeconds), 1, _sampleRate);
        }

        [Benchmark]
        public Pcm16Format DecodeAdpcm() => _adpcmStream.ToPcm16();
    }
}
