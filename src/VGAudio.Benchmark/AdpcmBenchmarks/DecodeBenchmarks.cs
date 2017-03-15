using BenchmarkDotNet.Attributes;
using VGAudio.Formats;

namespace VGAudio.Benchmark.AdpcmBenchmarks
{
    public class DecodeBenchmarks
    {
        [Params(1)]
        public double LengthSeconds;
        private int _sampleRate = 48000;
        private GcAdpcmFormat _adpcmStream;

        [Setup]
        public void Setup()
        {
            _adpcmStream = GenerateAudio.GenerateAdpcmEmpty((int)(_sampleRate * LengthSeconds), 1, _sampleRate);
        }

        [Benchmark]
        public Pcm16Format DecodeAdpcm() => _adpcmStream.ToPcm16();
    }
}
