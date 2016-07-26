using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

        public static int ReadInt32BE(this BinaryReader reader) => 
            BitConverter.ToInt32(reader.ReadBytes(sizeof(int)).Reverse().ToArray(), 0);

        public static short ReadInt16BE(this BinaryReader reader) =>
            BitConverter.ToInt16(reader.ReadBytes(sizeof(short)).Reverse().ToArray(), 0);
    }
}
