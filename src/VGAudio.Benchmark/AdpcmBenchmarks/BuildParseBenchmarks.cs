using BenchmarkDotNet.Attributes;
using VGAudio.Containers.Bxstm;
using VGAudio.Containers.Dsp;
using VGAudio.Containers.Idsp;
using VGAudio.Formats;
using VGAudio.Formats.GcAdpcm;

namespace VGAudio.Benchmark.AdpcmBenchmarks
{
    public class BuildParseBenchmarks
    {
        [Params(300)]
        public double LengthSeconds;
        [Params(2)]
        public int ChannelCount;
        private int _sampleRate = 48000;

        private GcAdpcmFormat _adpcm;
        private byte[] _brstm;
        private byte[] _dsp;
        private byte[] _idsp;

        [GlobalSetup]
        public void Setup()
        {
            _adpcm = GenerateAudio.GenerateAdpcmEmpty((int)(_sampleRate * LengthSeconds), ChannelCount, _sampleRate);
            _brstm = new BrstmWriter().GetFile(_adpcm);
            _dsp = new DspWriter().GetFile(_adpcm);
            _idsp = new IdspWriter().GetFile(_adpcm);
        }

        [Benchmark] public byte[] BuildDsp() => new DspWriter().GetFile(_adpcm);
        [Benchmark] public byte[] BuildBxstm() => new BrstmWriter().GetFile(_adpcm);
        [Benchmark] public byte[] BuildIdsp() => new IdspWriter().GetFile(_adpcm);

        [Benchmark] public IAudioFormat ParseDsp() => new DspReader().ReadFormat(_dsp);
        [Benchmark] public IAudioFormat ParseBxstm() => new BrstmReader().ReadFormat(_brstm);
        [Benchmark] public IAudioFormat ParseIdsp() => new IdspReader().ReadFormat(_idsp);

        [Benchmark] public byte[] RebuildDsp() => new DspWriter().GetFile(new DspReader().Read(_dsp));
        [Benchmark] public byte[] RebuildBxstm() => new BrstmWriter().GetFile(new BrstmReader().Read(_brstm));
        [Benchmark] public byte[] RebuildIdsp() => new IdspWriter().GetFile(new IdspReader().Read(_idsp));
    }
}