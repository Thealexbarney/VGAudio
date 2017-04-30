namespace VGAudio.Codecs
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
            int length = array.Length;
            var output = new short[length];

            for (int i = 0; i < length; i++)
            {
                output[i] = (short)((array[i] - 0x80) << 8);
            }

            return output;
        }
    }
}
