using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DspAdpcm.Encode
{
    public static class Extensions
    {
        public static IEnumerable<T[]> Batch<T>(this IEnumerable<T> source, int size, int lastSize = -1)
        {
            if (lastSize < 0 || lastSize > size)
                lastSize = size;

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
                yield return bucket.Take(lastSize).ToArray();
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
            if (lastInterleaveSize < 0 || lastInterleaveSize > interleaveSize)
                lastInterleaveSize = interleaveSize;

            int inputLength = input.Length;

            if (inputLength % numOutputs != 0)
                throw new ArgumentOutOfRangeException(nameof(numOutputs), numOutputs,
                    $"The input array length ({inputLength}) must be divisible by the number of outputs.");

            int outputLength = inputLength / numOutputs;
            if (finalOutputLength < 0)
                finalOutputLength = outputLength;

            int numFullBlocks = outputLength / interleaveSize;
            int numShortBlocks = outputLength % interleaveSize == 0 ? 0 : 1;

            if (numShortBlocks != 0 && outputLength % interleaveSize < lastInterleaveSize)
                throw new ArgumentOutOfRangeException(nameof(lastInterleaveSize), lastInterleaveSize,
                    $"Not enough elements for specified last interleave size");

            var outputs = new T[numOutputs][];
            for (int i = 0; i < numOutputs; i++)
            {
                outputs[i] = new T[outputLength];
            }

            for (int b = 0; b < numFullBlocks; b++)
            {
                for (int o = 0; o < numOutputs; o++)
                {
                    Array.Copy(input, interleaveSize * b * numOutputs + interleaveSize * o,
                        outputs[o], interleaveSize * b ,
                        interleaveSize);
                }
            }

            for (int b = 0; b < numShortBlocks; b++)
            {
                for (int o = 0; o < numOutputs; o++)
                {
                    Array.Copy(input, interleaveSize * numFullBlocks * numOutputs + lastInterleaveSize * o,
                        outputs[o], interleaveSize * numFullBlocks,
                        lastInterleaveSize);
                }
            }

            for (int i = 0; i < outputs.Length; i++)
            {
                Array.Resize(ref outputs[i], finalOutputLength);
            }

            return outputs;
        }

        public static int ReadInt32BE(this BinaryReader reader) =>
            BitConverter.ToInt32(reader.ReadBytes(sizeof(int)).Reverse().ToArray(), 0);

        public static short ReadInt16BE(this BinaryReader reader) =>
            BitConverter.ToInt16(reader.ReadBytes(sizeof(short)).Reverse().ToArray(), 0);
    }
}
