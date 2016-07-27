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
            int numBlocks = length / interleaveSize;
            int lastBlockSize = length % interleaveSize == 0 ? 0 : lastInterleaveSize * numInputs;
            var output = new T[interleaveSize * numInputs * numBlocks + lastBlockSize];

            for (int b = 0; b < numBlocks; b++)
            {
                for (int i = 0; i < numInputs; i++)
                {
                    Array.Copy(inputs[i], interleaveSize * b,
                        output, interleaveSize * b * numInputs + interleaveSize * i,
                        interleaveSize);
                }
            }

            if (lastBlockSize == 0) return output;

            for (int i = 0; i < numInputs; i++)
            {
                Array.Copy(inputs[i], interleaveSize * numBlocks,
                    output, interleaveSize * numBlocks * numInputs + lastInterleaveSize * i,
                    lastInterleaveSize);
            }

            return output;
        }

        public static int ReadInt32BE(this BinaryReader reader) =>
            BitConverter.ToInt32(reader.ReadBytes(sizeof(int)).Reverse().ToArray(), 0);

        public static short ReadInt16BE(this BinaryReader reader) =>
            BitConverter.ToInt16(reader.ReadBytes(sizeof(short)).Reverse().ToArray(), 0);
    }
}
