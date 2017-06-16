namespace VGAudio.Codecs.Pcm8
{
    public static class Pcm8Codec
    {
        public static byte[] Encode(short[] array)
        {
            var output = new byte[array.Length];

            for (int i = 0; i < array.Length; i++)
            {
                output[i] = (byte)((array[i] + short.MaxValue + 1) >> 8);
            }

            return output;
        }

        public static short[] Decode(byte[] array)
        {
            var output = new short[array.Length];

            for (int i = 0; i < array.Length; i++)
            {
                output[i] = (short)((array[i] - 0x80) << 8);
            }

            return output;
        }

        public static byte[] EncodeSigned(short[] array)
        {
            var output = new byte[array.Length];

            for (int i = 0; i < array.Length; i++)
            {
                output[i] = (byte)(array[i] >> 8);
            }

            return output;
        }

        public static short[] DecodeSigned(byte[] array)
        {
            var output = new short[array.Length];

            for (int i = 0; i < array.Length; i++)
            {
                output[i] = (short)(array[i] << 8);
            }

            return output;
        }
    }
}
