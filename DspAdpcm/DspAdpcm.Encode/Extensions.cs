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
            if (lastSize == -1 || lastSize > size)
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

        public static T[] Interleave<T>(this T[][] inputs, int interleaveSize, int lastInterleaveSize)
        {
            int length = inputs[0].Length;
            if (inputs.Any(x => x.Length != length))
                throw new ArgumentOutOfRangeException(nameof(inputs), "Inputs must be of equal length");

            int numInputs = inputs.Length;
            int numBlocks = length / interleaveSize;
            var output = new T[interleaveSize * numInputs * numBlocks + lastInterleaveSize * numInputs];

            for (int b = 0; b < numBlocks; b++)
            {
                for (int i = 0; i < numInputs; i++)
                {
                    Array.Copy(inputs[i], interleaveSize * b,
                        output, interleaveSize * b * numInputs + interleaveSize * i,
                        interleaveSize);
                }
            }

            if (length % interleaveSize == 0) return output;

            for (int i = 0; i < numInputs; i++)
            {
                Array.Copy(inputs[i], interleaveSize * numBlocks,
                    output, interleaveSize * numBlocks * numInputs + lastInterleaveSize * i,
                    lastInterleaveSize);
            }

            return output;
        }

        public static IEnumerable<T> Interleave2<T>(this IEnumerable<IEnumerable<T>> channels, int interleaveSize, int lastInterleaveSize)
        {
            IEnumerable<IEnumerable<T[]>> batchedAudioData = channels
                .Select(channel => channel.Batch(interleaveSize, lastInterleaveSize));

            IEnumerable<IEnumerable<T>> interleavedBlocks = batchedAudioData
                .Zip(channel => channel.SelectMany(batch => batch));

            return interleavedBlocks.SelectMany(x => x);
        }

        public static IEnumerable<TResult> Zip<T, TResult>(this IEnumerable<IEnumerable<T>> sequences, Func<T[], TResult> resultSelector)
        {
            IEnumerator<T>[] enumerators = sequences.Select(s => s.GetEnumerator()).ToArray();
            while (enumerators.All(e => e.MoveNext()))
                yield return resultSelector(enumerators.Select(e => e.Current).ToArray());
        }

        public static int ReadInt32BE(this BinaryReader reader) =>
            BitConverter.ToInt32(reader.ReadBytes(sizeof(int)).Reverse().ToArray(), 0);

        public static short ReadInt16BE(this BinaryReader reader) =>
            BitConverter.ToInt16(reader.ReadBytes(sizeof(short)).Reverse().ToArray(), 0);
    }
}
