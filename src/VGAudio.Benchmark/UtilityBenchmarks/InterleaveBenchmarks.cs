using System.IO;
using BenchmarkDotNet.Attributes;
using VGAudio.Utilities;

namespace VGAudio.Benchmark.UtilityBenchmarks
{
    public class InterleaveBenchmarks
    {
        [Params(4)]
        public int StreamCount;
        [Params(0x10000)]
        public int TotalStreamLength;
        private int _singleStreamLength;
        [Params(4, 0x2000)]
        public int InterleaveSize;

        private byte[][] _deinterleaved;
        private short[][] _deinterleavedShort;

        [GlobalSetup]
        public void Setup()
        {
            _singleStreamLength = TotalStreamLength / StreamCount;
            TotalStreamLength = _singleStreamLength * StreamCount;

            _deinterleaved = new byte[StreamCount][];
            for (int i = 0; i < StreamCount; i++)
            {
                _deinterleaved[i] = new byte[_singleStreamLength];
            }

            _deinterleavedShort = new short[StreamCount][];
            for (int i = 0; i < StreamCount; i++)
            {
                _deinterleavedShort[i] = new short[_singleStreamLength / 2];
            }
        }

        [Benchmark]
        public byte[] InterleaveArray()
        {
            return _deinterleaved.Interleave(InterleaveSize);
        }

       [Benchmark]
        public byte[] InterleaveStream()
        {
            var output = new byte[StreamCount * _singleStreamLength];

            using (var stream = new MemoryStream(output))
            {
                _deinterleaved.Interleave(stream, InterleaveSize);
            }

            return output;
        }
    }
}
