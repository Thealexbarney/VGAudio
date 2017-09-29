using BenchmarkDotNet.Attributes;
using VGAudio.Codecs.GcAdpcm;

namespace VGAudio.Benchmark.AdpcmBenchmarks
{
    public class EncodeBenchmarks
    {
        [Params(1)]
        public double LengthSeconds;
        private int _sampleRate = 48000;
        private short[] _pcm;
        private short[] _coefs;

        [GlobalSetup]
        public void Setup()
        {
            _pcm = GenerateAudio.GenerateSineWave((int)(_sampleRate * LengthSeconds), 440, _sampleRate);
            _coefs = GcAdpcmCoefficients.CalculateCoefficients(_pcm);
        }

        [Benchmark] public short[] GenerateCoefs() => GcAdpcmCoefficients.CalculateCoefficients(_pcm);
        [Benchmark] public byte[] EncodeAdpcm() => GcAdpcmEncoder.Encode(_pcm, _coefs);
        [Benchmark] public byte[] GenerateCoefsAndEncodeAdpcm() => GcAdpcmEncoder.Encode(_pcm, GcAdpcmCoefficients.CalculateCoefficients(_pcm));
    }
}
