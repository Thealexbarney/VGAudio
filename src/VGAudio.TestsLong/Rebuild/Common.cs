namespace VGAudio.TestsLong.Rebuild
{
    internal static class Common
    {
        internal static int ArraysEqual(byte[] a1, byte[] a2)
        {
            if (a1 == null || a2 == null) return -1;
            if (a1 == a2) return -1;
            if (a1.Length != a2.Length) return -2;
            int byteCount = 0;

            for (int i = 0; i < a1.Length; i++)
            {
                if (a1[i] != a2[i])
                {
                    byteCount++;
                }
            }

            return byteCount;
        }
    }
}
