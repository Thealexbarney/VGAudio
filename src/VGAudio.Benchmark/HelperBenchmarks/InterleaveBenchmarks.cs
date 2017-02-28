using System.IO;
using BenchmarkDotNet.Attributes;
using DspAdpcm.Utilities;

namespace DspAdpcm.Benchmark.HelperBenchmarks
{
    public class InterleaveBenchmarks
    {
        [Params(4)]
        public int NumStreams;
        [Params(0x10000)]
        public int TotalStreamLength;
        private int SingleStreamLength;
        [Params(4, 0x2000)]
        public int InterleaveSize;

        private byte[][] deinterleaved;
        private short[][] deinterleavedShort;

        [Setup]
        public void Setup()
        {
            SingleStreamLength = TotalStreamLength / NumStreams;
            TotalStreamLength = SingleStreamLength * NumStreams;

            deinterleaved = new byte[NumStreams][];
            for (int i = 0; i < NumStreams; i++)
            {
                deinterleaved[i] = new byte[SingleStreamLength];
            }

            deinterleavedShort = new short[NumStreams][];
            for (int i = 0; i < NumStreams; i++)
            {
                deinterleavedShort[i] = new short[SingleStreamLength / 2];
            }
        }

        [Benchmark]
        public byte[] InterleaveArray()
        {
            return deinterleaved.Interleave(InterleaveSize);
        }

       [Benchmark]
        public byte[] InterleaveStream()
        {
            var output = new byte[NumStreams * SingleStreamLength];

            using (var stream = new MemoryStream(output))
            {
                deinterleaved.Interleave(stream, InterleaveSize);
            }

            return output;
        }
    }
}
