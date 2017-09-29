using System;

namespace VGAudio.Utilities
{
    public static class Parallel
    {
        private static bool RunInParallel = true;

        public static void For(int fromInclusive, int toExclusive, Action<int> body)
        {
            if (RunInParallel)
            {
                System.Threading.Tasks.Parallel.For(fromInclusive, toExclusive, body);
                return;
            }

            for (int i = fromInclusive; i < toExclusive; i++)
            {
                body(i);
            }
        }
    }
}