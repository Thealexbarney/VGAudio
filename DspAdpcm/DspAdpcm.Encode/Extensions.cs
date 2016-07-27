using System;
using System.Collections.Generic;
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

        public static IEnumerable<T> Interleave<T>(this IEnumerable<IEnumerable<T>> channels, int interleaveSize, int lastInterleaveSize)
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
    }
}
