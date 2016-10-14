using BenchmarkDotNet.Attributes;
using DspAdpcm.Adpcm;

namespace DspAdpcm.Benchmark.AdpcmBenchmarks
{
    public class EncodeBenchmarks
    {
        [Params(1)]
        public double lengthSeconds;
        private int sampleRate = 48000;
        private short[] pcm;
        private short[] coefs;

        [Setup]
        public void Setup()
        {
            pcm = GenerateAudio.GenerateSineWave((int)(sampleRate * lengthSeconds), 440, sampleRate);
            coefs = Encode.DspCorrelateCoefs(pcm);
        }

        [Benchmark]
        public byte[] EncodeAdpcmBenchmark()
        {
            return Encode.EncodeAdpcm(pcm, coefs);
        }
    }
}
