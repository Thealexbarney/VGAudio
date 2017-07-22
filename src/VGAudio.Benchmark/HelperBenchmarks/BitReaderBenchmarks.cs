using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using VGAudio.Utilities;


namespace VGAudio.Benchmark.HelperBenchmarks
{
    public class BitReaderBenchmarks
    {
        private byte[] _data;
        private byte[] _rand;
        private const int Size = 0x2000;

        [Setup]
        public void Setup()
        {
            _data = new byte[Size];
            var rand = new Random(12345);
            rand.NextBytes(_data);

            int total = 0;
            var rands = new List<byte>();
            do
            {
                var next = rand.Next(1, 16);
                rands.Add((byte)next);
                total += next;
            } while (total < Size);

            _rand = rands.ToArray();
        }

        [Benchmark]
        public void ReadBitArray()
        {
            const int bits = 7;
            int toRead = Size / bits;
            var reader = new BitReader(_data);

            for (int i = 0; i < toRead; i++)
            {
                reader.ReadInt(bits);
            }
        }

        [Benchmark]
        public void ReadBitArrayRandom()
        {
            var reader = new BitReader(_data);

            foreach (byte t in _rand)
            {
                reader.ReadInt(t);
            }
        }
    }
}
