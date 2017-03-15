using BenchmarkDotNet.Attributes;
using VGAudio.Codecs;

namespace VGAudio.Benchmark.AdpcmBenchmarks
{
    public class EncodeBenchmarks
    {
        [Params(1)]
        public double LengthSeconds;
        private int _sampleRate = 48000;
        private short[] _pcm;
        private short[] _coefs;

        [Setup]
        public void Setup()
        {
            _pcm = GenerateAudio.GenerateSineWave((int)(_sampleRate * LengthSeconds), 440, _sampleRate);
            _coefs = GcAdpcmEncoder.DspCorrelateCoefs(_pcm);
        }

        [Benchmark] public short[] GenerateCoefs() => GcAdpcmEncoder.DspCorrelateCoefs(_pcm);
        [Benchmark] public byte[] EncodeAdpcm() => GcAdpcmEncoder.EncodeAdpcm(_pcm, _coefs);
        [Benchmark] public byte[] GenerateCoefsAndEncodeAdpcm() => GcAdpcmEncoder.EncodeAdpcm(_pcm, GcAdpcmEncoder.DspCorrelateCoefs(_pcm));
    }
}
