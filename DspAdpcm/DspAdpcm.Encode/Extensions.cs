using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DspAdpcm.Encode
{
    internal static class Extensions
    {
        public static IEnumerable<T[]> Batch<T>(this IEnumerable<T> source, int size, bool truncateLastBatch = false)
        {
            T[] bucket = new T[size];
            var count = 0;

            foreach (var item in source)
            {
                bucket[count++] = item;

                if (count != size)
                    continue;

                yield return bucket;

                bucket = new T[size];
                count = 0;
            }

            // Return the last bucket with all remaining elements
            if (count > 0)
                yield return bucket.Take(truncateLastBatch ? count : size).ToArray();
        }

        public static T[] Interleave<T>(this T[][] inputs, int interleaveSize, int lastInterleaveSize = -1)
        {
            if (lastInterleaveSize < 0 || lastInterleaveSize > interleaveSize)
                lastInterleaveSize = interleaveSize;

            int length = inputs[0].Length;
            if (inputs.Any(x => x.Length != length))
                throw new ArgumentOutOfRangeException(nameof(inputs), "Inputs must be of equal length");

            int numInputs = inputs.Length;
            int numFullBlocks = length / interleaveSize;
            int numShortBlocks = length % interleaveSize == 0 ? 0 : 1;
            int interleavedLength = numFullBlocks * interleaveSize + numShortBlocks * lastInterleaveSize;

            if (interleavedLength < length)
                throw new ArgumentOutOfRangeException(nameof(lastInterleaveSize), lastInterleaveSize,
                    $"Last interleave size is too small by {length - interleavedLength} bytes");

            int lastBlockSizeWithoutPadding = length - numFullBlocks * interleaveSize;

            var output = new T[interleavedLength * numInputs];

            for (int b = 0; b < numFullBlocks; b++)
            {
                for (int i = 0; i < numInputs; i++)
                {
                    Array.Copy(inputs[i], interleaveSize * b,
                        output, interleaveSize * b * numInputs + interleaveSize * i,
                        interleaveSize);
                }
            }

            for (int b = 0; b < numShortBlocks; b++)
            {
                for (int i = 0; i < numInputs; i++)
                {
                    Array.Copy(inputs[i], interleaveSize * numFullBlocks,
                        output, interleaveSize * numFullBlocks * numInputs + lastInterleaveSize * i,
                        lastBlockSizeWithoutPadding);
                }
            }

            return output;
        }

        public static T[][] DeInterleave<T>(this T[] input, int interleaveSize, int numOutputs, int lastInterleaveSize = -1, int finalOutputLength = -1)
        {
            if (input.Length % numOutputs != 0)
                throw new ArgumentOutOfRangeException(nameof(numOutputs), numOutputs,
                    $"The input array length ({input.Length}) must be divisible by the number of outputs.");

            if (lastInterleaveSize < 0 || lastInterleaveSize > interleaveSize)
                lastInterleaveSize = interleaveSize;

            int outputLength = input.Length / numOutputs;
            if (finalOutputLength < 0)
                finalOutputLength = outputLength;

            int numShortBlocks = outputLength % interleaveSize == 0 ? 0 : 1;
            int numBlocks = outputLength / interleaveSize + numShortBlocks;

            if (numShortBlocks != 0 && outputLength % interleaveSize < lastInterleaveSize)
                throw new ArgumentOutOfRangeException(nameof(lastInterleaveSize), lastInterleaveSize,
                    $"Not enough elements for specified last interleave size({lastInterleaveSize})");

            var outputs = new T[numOutputs][];

            for (int o = 0; o < numOutputs; o++)
            {
                outputs[o] = new T[finalOutputLength];

                for (int b = 0; b < numBlocks - 1; b++)
                {
                    Array.Copy(input, interleaveSize * b * numOutputs + interleaveSize * o,
                        outputs[o], interleaveSize * b,
                        interleaveSize);
                }

                Array.Copy(input, interleaveSize * (numBlocks - 1) * numOutputs + lastInterleaveSize * o,
                    outputs[o], interleaveSize * (numBlocks - 1),
                    finalOutputLength - interleaveSize * (numBlocks - 1));
            }

            return outputs;
        }

        public static byte[] ToFlippedBytes(this short[] array)
        {
            var output = new byte[array.Length * 2];

            for (int i = 0; i < array.Length; i++)
            {
                output[i * 2] = (byte)(array[i] >> 8);
                output[i * 2 + 1] = (byte)array[i];
            }

            return output;
        }

        public static short FlipBytes(this short value) => (short)(value << 8 | (ushort)value >> 8);

        public static IEnumerable<byte> ToBytesBE(this int value) => BitConverter.GetBytes(value).Reverse();
        public static IEnumerable<byte> ToBytesBE(this short value) => BitConverter.GetBytes(value).Reverse();
        public static void Add16BE(this List<byte> list, int value) => list.Add16BE((short)value);
        public static void Add16BE(this List<byte> list, short value) => list.AddRange(value.ToBytesBE());
        public static void Add32BE(this List<byte> list, int value) => list.AddRange(value.ToBytesBE());
        public static void Add16(this List<byte> list, short value) => list.AddRange(BitConverter.GetBytes(value));
        public static void Add16(this List<byte> list, ushort value) => list.AddRange(BitConverter.GetBytes(value));
        public static void Add32(this List<byte> list, int value) => list.AddRange(BitConverter.GetBytes(value));
        public static void Add32(this List<byte> list, string value) => list.AddRange(Encoding.ASCII.GetBytes(value.PadRight(4, '\0').Substring(0, 4)));
    }
}
