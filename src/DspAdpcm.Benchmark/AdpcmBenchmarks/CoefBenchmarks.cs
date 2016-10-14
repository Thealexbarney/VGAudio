using BenchmarkDotNet.Attributes;
using DspAdpcm.Adpcm;

namespace DspAdpcm.Benchmark.AdpcmBenchmarks
{
    public class CoefBenchmarks
    {
        [Params(1)]
        public double lengthSeconds;
        private int sampleRate = 48000;
        private short[] pcm;

        [Setup]
        public void Setup()
        {
            pcm = GenerateAudio.GenerateSineWave((int)(sampleRate * lengthSeconds), 440, sampleRate);
        }

        [Benchmark]
        public short[] GenerateCoefBenchmark()
        {
            return Encode.DspCorrelateCoefs(pcm, pcm.Length);
        }
    }
}
