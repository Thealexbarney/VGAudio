using System;

namespace VGAudio.Codecs.CriHca
{
    public static class CriHcaTables
    {
        public static float[] DequantizerScalingTable { get; } = GenerateTable(64, DequantizerScalingFunction);
        public static float[] DequantizerRangeTable { get; } = GenerateTable(16, DequantizerRangeFunction);
        public static float[] IntensityRatioTable { get; } = GenerateTable(16, IntensityRatioFunction);
        public static float[] ScaleConversionTable { get; } = GenerateTable(128, ScaleConversionTableFunction);

        public static byte[] ResolutionTable { get; } =
        {
            0x0F, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0E, 0x0D,
            0x0D, 0x0D, 0x0D, 0x0D, 0x0D, 0x0C, 0x0C, 0x0C,
            0x0C, 0x0C, 0x0C, 0x0B, 0x0B, 0x0B, 0x0B, 0x0B,
            0x0B, 0x0A, 0x0A, 0x0A, 0x0A, 0x0A, 0x0A, 0x0A,
            0x09, 0x09, 0x09, 0x09, 0x09, 0x09, 0x08, 0x08,
            0x08, 0x08, 0x08, 0x08, 0x07, 0x06, 0x06, 0x05,
            0x04, 0x04, 0x04, 0x03, 0x03, 0x03, 0x02, 0x02,
            0x02, 0x02, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        public static byte[] MaxSampleBitSize { get; } =
        {
            0, 2, 3, 3, 4, 4, 4, 4, 5, 6, 7, 8, 9, 10, 11, 12
        };

        public static byte[,] ActualSampleBitSize { get; } =
        {
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
            {1, 1, 2, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
            {2, 2, 2, 2, 2, 2, 3, 3, 0, 0, 0, 0, 0, 0, 0, 0},
            {2, 2, 3, 3, 3, 3, 3, 3, 0, 0, 0, 0, 0, 0, 0, 0},
            {3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 4, 4},
            {3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4},
            {3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4},
            {3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4}
        };

        public static sbyte[,] QuantizedSampleValue { get; } =
        {
            {+0, +0, +0, +0, +0, +0, +0, +0, +0, +0, +0, +0, +0, +0, +0, +0},
            {+0, +0, +1, -1, +0, +0, +0, +0, +0, +0, +0, +0, +0, +0, +0, +0},
            {+0, +0, +1, +1, -1, -1, +2, -2, +0, +0, +0, +0, +0, +0, +0, +0},
            {+0, +0, +1, -1, +2, -2, +3, -3, +0, +0, +0, +0, +0, +0, +0, +0},
            {+0, +0, +1, +1, -1, -1, +2, +2, -2, -2, +3, +3, -3, -3, +4, -4},
            {+0, +0, +1, +1, -1, -1, +2, +2, -2, -2, +3, -3, +4, -4, +5, -5},
            {+0, +0, +1, +1, -1, -1, +2, -2, +3, -3, +4, -4, +5, -5, +6, -6},
            {+0, +0, +1, -1, +2, -2, +3, -3, +4, -4, +5, -5, +6, -6, +7, -7}
        };

        // Don't know what the window function is.
        // It's close to a KBD window with an alpha of around 3.82.
        // AAC and Vorbis windows are similar to it.
        //Todo: Make float
        public static double[] MdctWindow { get; } =
        {
            6.90533780e-4f, 1.97623484e-3f, 3.67386453e-3f, 5.72424009e-3f, 8.09670333e-3f, 1.07731819e-2f, 1.37425177e-2f, 1.69978570e-2f,
            2.05352642e-2f, 2.43529025e-2f, 2.84505188e-2f, 3.28290947e-2f, 3.74906212e-2f, 4.24378961e-2f, 4.76744287e-2f, 5.32043017e-2f,
            5.90321124e-2f, 6.51628822e-2f, 7.16020092e-2f, 7.83552229e-2f, 8.54284912e-2f, 9.28280205e-2f, 1.00560151e-1f, 1.08631350e-1f,
            1.17048122e-1f, 1.25816986e-1f, 1.34944350e-1f, 1.44436508e-1f, 1.54299513e-1f, 1.64539129e-1f, 1.75160721e-1f, 1.86169162e-1f,
            1.97568730e-1f, 2.09362969e-1f, 2.21554622e-1f, 2.34145418e-1f, 2.47135997e-1f, 2.60525763e-1f, 2.74312705e-1f, 2.88493186e-1f,
            3.03061932e-1f, 3.18011731e-1f, 3.33333343e-1f, 3.49015296e-1f, 3.65043819e-1f, 3.81402701e-1f, 3.98073107e-1f, 4.15033519e-1f,
            4.32259798e-1f, 4.49725032e-1f, 4.67399567e-1f, 4.85251158e-1f, 5.03244936e-1f, 5.21343827e-1f, 5.39508522e-1f, 5.57697773e-1f,
            5.75868905e-1f, 5.93978047e-1f, 6.11980557e-1f, 6.29831433e-1f, 6.47486031e-1f, 6.64900243e-1f, 6.82031155e-1f, 6.98837578e-1f,
            7.15280414e-1f, 7.31323123e-1f, 7.46932149e-1f, 7.62077332e-1f, 7.76731849e-1f, 7.90872812e-1f, 8.04481268e-1f, 8.17542017e-1f,
            8.30044091e-1f, 8.41980159e-1f, 8.53346705e-1f, 8.64143789e-1f, 8.74374807e-1f, 8.84046197e-1f, 8.93167078e-1f, 9.01749134e-1f,
            9.09806132e-1f, 9.17353690e-1f, 9.24408972e-1f, 9.30990338e-1f, 9.37117040e-1f, 9.42809045e-1f, 9.48086798e-1f, 9.52970862e-1f,
            9.57481921e-1f, 9.61640537e-1f, 9.65466917e-1f, 9.68980789e-1f, 9.72201586e-1f, 9.75147963e-1f, 9.77837980e-1f, 9.80289042e-1f,
            9.82517719e-1f, 9.84539866e-1f, 9.86370564e-1f, 9.88024116e-1f, 9.89514053e-1f, 9.90853190e-1f, 9.92053449e-1f, 9.93126273e-1f,
            9.94082093e-1f, 9.94930983e-1f, 9.95682180e-1f, 9.96344328e-1f, 9.96925533e-1f, 9.97433305e-1f, 9.97874618e-1f, 9.98256087e-1f,
            9.98583674e-1f, 9.98862922e-1f, 9.99099135e-1f, 9.99296963e-1f, 9.99460995e-1f, 9.99595225e-1f, 9.99703407e-1f, 9.99789119e-1f,
            9.99855518e-1f, 9.99905586e-1f, 9.99941945e-1f, 9.99967217e-1f, 9.99983609e-1f, 9.99993265e-1f, 9.99998033e-1f, 9.99999762e-1f
        };

        private static float DequantizerScalingFunction(int x) => (float)(Math.Sqrt(128) * Math.Pow(Math.Pow(2, 53f / 128), x - 63));
        private static float ScaleConversionTableFunction(int x) => x > 1 && x < 127 ? (float)Math.Pow(Math.Pow(2, 53f / 128), x - 64) : 0;
        private static float IntensityRatioFunction(int x) => x <= 14 ? (14 - x) / 7f : 0;

        private static float DequantizerRangeFunction(int x)
        {
            if (x == 0) return 0;
            if (x < 8) return 2f / (2 * x + 1);
            return 2f / ((1 << (x - 3)) - 1);
        }

        private static T[] GenerateTable<T>(int count, Func<int, T> elementGenerator)
        {
            var table = new T[count];
            for (int i = 0; i < count; i++)
            {
                table[i] = elementGenerator(i);
            }
            return table;
        }
    }
}
