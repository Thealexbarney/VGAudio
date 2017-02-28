using BenchmarkDotNet.Attributes;
using VGAudio.Codecs;

namespace VGAudio.Benchmark.AdpcmBenchmarks
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
            coefs = GcAdpcmEncoder.DspCorrelateCoefs(pcm);
        }

        [Benchmark] public short[] GenerateCoefs() => GcAdpcmEncoder.DspCorrelateCoefs(pcm);
        [Benchmark] public byte[] EncodeAdpcm() => GcAdpcmEncoder.EncodeAdpcm(pcm, coefs);
        [Benchmark] public byte[] GenerateCoefsAndEncodeAdpcm() => GcAdpcmEncoder.EncodeAdpcm(pcm, GcAdpcmEncoder.DspCorrelateCoefs(pcm));
    }
}
