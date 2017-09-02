using System;
using System.Collections.Generic;

namespace VGAudio.Utilities
{
    public class Mdct
    {
        public int MdctBits { get; }
        public int MdctSize { get; }
        public double Scale { get; }

        private static readonly object TableLock = new object();
        private static int _tableBits = -1;
        private static readonly List<double[]> SinTables = new List<double[]>();
        private static readonly List<double[]> CosTables = new List<double[]>();
        private static readonly List<int[]> ShuffleTables = new List<int[]>();

        private readonly double[] _previous;
        private readonly double[] _dctOut;
        private readonly double[] _imdctWindow;
        private readonly double[] _work;

        public Mdct(int mdctBits, double[] window, double scale = 1)
        {
            if (mdctBits > _tableBits)
            {
                SetTables(mdctBits);
            }

            MdctBits = mdctBits;
            MdctSize = 1 << mdctBits;
            Scale = scale;

            if (window.Length < MdctSize)
            {
                throw new ArgumentException("Window must be as long as the MDCT size.", nameof(window));
            }

            _previous = new double[MdctSize];
            _dctOut = new double[MdctSize];
            _work = new double[MdctSize];
            _imdctWindow = window;
        }

        private static void SetTables(int maxBits)
        {
            lock (TableLock)
            {
                for (int i = _tableBits + 1; i <= maxBits; i++)
                {
                    GenerateTrigTables(i, out double[] sin, out double[] cos);
                    SinTables.Add(sin);
                    CosTables.Add(cos);
                    ShuffleTables.Add(GenerateShuffleTable(i));
                }
                _tableBits = maxBits;
            }
        }

        public void RunImdct(double[] input, double[] output)
        {
            if (input.Length < MdctSize)
            {
                throw new ArgumentException("Input must be as long as the MDCT size.", nameof(input));
            }

            if (output.Length < MdctSize)
            {
                throw new ArgumentException("Output must be as long as the MDCT size.", nameof(output));
            }

            int size = MdctSize;
            int half = size / 2;

            Dct4(input, _dctOut);

            for (int i = 0; i < half; i++)
            {
                output[i] = _imdctWindow[i] * _dctOut[i + half] + _previous[i];
                output[i + half] = _imdctWindow[i + half] * -_dctOut[size - 1 - i] - _previous[i + half];
                _previous[i] = _imdctWindow[size - 1 - i] * -_dctOut[half - i - 1];
                _previous[i + half] = _imdctWindow[half - i - 1] * _dctOut[i];
            }
        }

        /// <summary>
        /// Does a Type-4 DCT.
        /// </summary>
        /// <param name="input">The input array containing the time or frequency-domain samples</param>
        /// <param name="output">The output array that will contain the transformed time or frequency-domain samples</param>
        private void Dct4(double[] input, double[] output)
        {
            var shuffleTable = ShuffleTables[MdctBits];
            var sinTable = SinTables[MdctBits];
            var cosTable = CosTables[MdctBits];
            double[] dctTemp = _work;

            int size = MdctSize;
            int lastIndex = size - 1;
            int halfSize = size / 2;

            for (int i = 0; i < halfSize; i++)
            {
                int i2 = i * 2;
                var a = input[i2];
                var b = input[lastIndex - i2];
                var sin = sinTable[i];
                var cos = cosTable[i];
                dctTemp[i2] = a * cos + b * sin;
                dctTemp[i2 + 1] = a * sin - b * cos;
            }
            int stageCount = MdctBits - 1;

            for (int stage = 0; stage < stageCount; stage++)
            {
                int blockCount = 1 << stage;
                int blockSizeBits = stageCount - stage;
                int blockHalfSizeBits = blockSizeBits - 1;
                int blockSize = 1 << blockSizeBits;
                int blockHalfSize = 1 << blockHalfSizeBits;
                sinTable = SinTables[blockHalfSizeBits];
                cosTable = CosTables[blockHalfSizeBits];

                for (int block = 0; block < blockCount; block++)
                {
                    for (int i = 0; i < blockHalfSize; i++)
                    {
                        int frontPos = (block * blockSize + i) * 2;
                        int backPos = frontPos + blockSize;
                        var a = dctTemp[frontPos] - dctTemp[backPos];
                        var b = dctTemp[frontPos + 1] - dctTemp[backPos + 1];
                        var sin = sinTable[i];
                        var cos = cosTable[i];
                        dctTemp[frontPos] += dctTemp[backPos];
                        dctTemp[frontPos + 1] += dctTemp[backPos + 1];
                        dctTemp[backPos] = a * cos + b * sin;
                        dctTemp[backPos + 1] = a * sin - b * cos;
                    }
                }
            }

            for (int i = 0; i < MdctSize; i++)
            {
                output[i] = dctTemp[shuffleTable[i]] * Scale;
            }
        }

        internal static void GenerateTrigTables(int sizeBits, out double[] sin, out double[] cos)
        {
            int size = 1 << sizeBits;
            sin = new double[size];
            cos = new double[size];

            for (int i = 0; i < size; i++)
            {
                double value = Math.PI * (4 * i + 1) / (4 * size);
                sin[i] = Math.Sin(value);
                cos[i] = Math.Cos(value);
            }
        }

        internal static int[] GenerateShuffleTable(int sizeBits)
        {
            int size = 1 << sizeBits;
            var table = new int[size];

            for (int i = 0; i < size; i++)
            {
                table[i] = Bit.BitReverse32(i ^ (i / 2), sizeBits);
            }

            return table;
        }

        // ReSharper disable once UnusedMember.Local
        /// <summary>
        /// Does a Type-4 DCT. Intended for reference.
        /// </summary>
        /// <param name="input">The input array containing the time or frequency-domain samples</param>
        /// <param name="output">The output array that will contain the transformed time or frequency-domain samples</param>
        private void Dct4Slow(double[] input, double[] output)
        {
            for (int k = 0; k < MdctSize; k++)
            {
                double sample = 0;
                for (int n = 0; n < MdctSize; n++)
                {
                    double angle = Math.PI / MdctSize * (k + 0.5) * (n + 0.5);
                    sample += Math.Cos(angle) * input[n];
                }
                output[k] = sample * Scale;
            }
        }
    }
}
