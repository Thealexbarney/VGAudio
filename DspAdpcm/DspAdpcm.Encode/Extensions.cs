using System;
using System.Collections.Generic;

namespace DspAdpcm.Encode
{
    public static class Extensions
    {
        public static IEnumerable<T[]> Batch<T>(
        this IEnumerable<T> source, int size)
        {
            T[] bucket = new T[size];
            var count = 0;

            foreach (var item in source)
            {
                bucket[count++] = item;

                if (count != size)
                    continue;

                yield return bucket;

                Array.Clear(bucket, 0, size);
                count = 0;
            }

            // Return the last bucket with all remaining elements
            if (count > 0)
                yield return bucket;
        }
    }
}
