using System;

namespace VGAudio.Utilities
{
    public static class Parallel
    {
#if !NOPARALLEL
        private static bool RunInParallel = true;
#endif

        public static void For(int fromInclusive, int toExclusive, Action<int> body)
        {
#if !NOPARALLEL
            if (RunInParallel)
            {
                System.Threading.Tasks.Parallel.For(fromInclusive, toExclusive, body);
                return;
            }
#endif

            for (int i = fromInclusive; i < toExclusive; i++)
            {
                body(i);
            }
        }
    }
}