#if NOPARALLEL
using System.Collections.Generic;

namespace System.Threading.Tasks
{
    internal static class Parallel
    {
        public static void ForEach<TSource>(IEnumerable<TSource> source, Action<TSource> body)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (body == null)
            {
                throw new ArgumentNullException(nameof(body));
            }

            foreach (TSource item in source)
            {
                body(item);
            }
        }

        public static void For(int fromInclusive, int toExclusive, Action<int> body)
        {
            if (body == null)
            {
                throw new ArgumentNullException(nameof(body));
            }

            for (int i = fromInclusive; i < toExclusive; i++)
            {
                body(i);
            }
        }
    }
}
#endif