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
        private PcmStream pcm;

        [Setup]
        public void Setup()
        {
            pcm = GenerateAudio.GeneratePcmSineWave((int)(sampleRate * lengthSeconds), 1, sampleRate);
        }

        [Benchmark]
        public AdpcmStream EncodeAdpcmBenchmark()
        {
            return Encode.PcmToAdpcm(pcm);
        }
    }
}
