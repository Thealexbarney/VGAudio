using BenchmarkDotNet.Attributes;
using DspAdpcm.Adpcm;
using DspAdpcm.Pcm;

namespace DspAdpcm.Benchmark
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
            coefs = Encode.DspCorrelateCoefs(pcm, pcm.Length);
        }

        [Benchmark]
        public byte[] EncodeAdpcmBenchmark()
        {
            return Encode.EncodeAdpcm(pcm, coefs);
        }
    }
}
