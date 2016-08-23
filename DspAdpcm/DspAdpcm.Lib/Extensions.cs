using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static DspAdpcm.Lib.Helpers;

namespace DspAdpcm.Lib
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

        public static T[] Interleave<T>(this T[][] inputs, int interleaveSize, int paddingAlignment = 0)
        {
            int length = inputs[0].Length;
            if (inputs.Any(x => x.Length != length))
                throw new ArgumentOutOfRangeException(nameof(inputs), "Inputs must be of equal length");

            int numInputs = inputs.Length;
            int numBlocks = length.DivideByRoundUp(interleaveSize);
            int lastInterleaveSize = length - (numBlocks - 1) * interleaveSize;
            int padding = GetNextMultiple(lastInterleaveSize, paddingAlignment) - lastInterleaveSize;

            var output = new T[(interleaveSize * (numBlocks - 1) + lastInterleaveSize + padding) * numInputs];

            for (int b = 0; b < numBlocks; b++)
            {
                for (int i = 0; i < numInputs; i++)
                {
                    int currentInterleaveSize = b == numBlocks - 1 ? lastInterleaveSize : interleaveSize;
                    Array.Copy(inputs[i], interleaveSize * b,
                        output, interleaveSize * b * numInputs + currentInterleaveSize * i,
                        currentInterleaveSize);
                }
            }

            return output;
        }

        public static void Interleave(this byte[][] inputs, Stream output, int length, int interleaveSize, int paddingAlignment = 0)
        {
            if (inputs.Any(x => x.Length < length))
                throw new ArgumentOutOfRangeException(nameof(inputs), "Inputs must be as long as the specified length");

            int numInputs = inputs.Length;
            int numBlocks = length.DivideByRoundUp(interleaveSize);
            int lastInterleaveSize = length - (numBlocks - 1) * interleaveSize;
            int padding = GetNextMultiple(lastInterleaveSize, paddingAlignment) - lastInterleaveSize;

            for (int b = 0; b < numBlocks; b++)
            {
                for (int o = 0; o < numInputs; o++)
                {
                    output.Write(inputs[o], interleaveSize * b, b != numBlocks - 1 ? interleaveSize : lastInterleaveSize);
                    if (b == numBlocks - 1)
                    {
                        output.Position += padding;
                    }
                }
            }
        }

        public static T[][] DeInterleave<T>(this T[] input, int interleaveSize, int numOutputs)
        {
            if (input.Length % numOutputs != 0)
                throw new ArgumentOutOfRangeException(nameof(numOutputs), numOutputs,
                    $"The input array length ({input.Length}) must be divisible by the number of outputs.");

            int singleLength = input.Length / numOutputs;
            int numBlocks = singleLength.DivideByRoundUp(interleaveSize);
            int lastInterleaveSize = singleLength - (numBlocks - 1) * interleaveSize;

            var outputs = new T[numOutputs][];
            for (int i = 0; i < numOutputs; i++)
            {
                outputs[i] = new T[singleLength];
            }

            for (int b = 0; b < numBlocks; b++)
            {
                for (int o = 0; o < numOutputs; o++)
                {
                    int currentInterleaveSize = b == numBlocks - 1 ? lastInterleaveSize : interleaveSize;
                    Array.Copy(input, interleaveSize * b * numOutputs + currentInterleaveSize * o,
                        outputs[o], interleaveSize * b,
                        currentInterleaveSize);
                }
            }

            return outputs;
        }

        public static byte[][] DeInterleave(this Stream input, int length, int interleaveSize, int numOutputs)
        {
            if (input.CanSeek)
            {
                long remainingLength = input.Length - input.Position;
                if (remainingLength < length)
                {
                    throw new ArgumentOutOfRangeException(nameof(length), length,
                        "Specified length is less than the number of bytes remaining in the Stream");
                }
            }

            if (length % numOutputs != 0)
                throw new ArgumentOutOfRangeException(nameof(numOutputs), numOutputs,
                    $"The input length ({length}) must be divisible by the number of outputs.");

            int singleLength = length / numOutputs;
            int numBlocks = singleLength.DivideByRoundUp(interleaveSize);
            int lastInterleaveSize = singleLength - (numBlocks - 1) * interleaveSize;

            var outputs = new byte[numOutputs][];
            for (int i = 0; i < numOutputs; i++)
            {
                outputs[i] = new byte[singleLength];
            }

            for (int b = 0; b < numBlocks; b++)
            {
                for (int o = 0; o < numOutputs; o++)
                {
                    input.Read(outputs[o], interleaveSize * b, b != (numBlocks - 1) ? interleaveSize : lastInterleaveSize);
                }
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

        public static short[] ToShortArray(this byte[] array)
        {
            var output = new short[array.Length.DivideByRoundUp(2)];
            Buffer.BlockCopy(array, 0, output, 0, array.Length);
            return output;
        }

        public static short[] ToShortArrayFlippedBytes(this byte[] array)
        {
            int length = array.Length.DivideByRoundUp(2);

            var output = new short[length];
            for (int i = 0; i < length; i++)
            {
                output[i] = (short)((array[i * 2] << 8) | array[i * 2 + 1]);
            }

            return output;
        }

        public static void WriteASCII(this BinaryWriter writer, string value)
        {
            byte[] text = Encoding.ASCII.GetBytes(value);
            writer.Write(text);
        }

        public static int DivideByRoundUp(this int value, int divisor) => (int)Math.Ceiling((double)value / divisor);
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
