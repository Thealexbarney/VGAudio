using VGAudio.Utilities;

namespace VGAudio.Codecs.CriAdx
{
    public static class CriAdxEncryption
    {
        public static void Decrypt(byte[][] adpcm, CriAdxKey key, int frameSize)
        {
            for (int i = 0; i < adpcm.Length; i++)
            {
                DecryptChannel(adpcm[i], key, frameSize, i, adpcm.Length);
            }
        }

        public static void DecryptChannel(byte[] adpcm, CriAdxKey key, int frameSize, int channelNum, int channelCount)
        {
            int xor = key.Seed;
            int frameCount = adpcm.Length.DivideByRoundUp(frameSize);

            for (int i = 0; i < channelNum; i++)
            {
                xor = (xor * key.Mult + key.Inc) & 0x7fff;
            }

            for (int i = 0; i < frameCount; i++)
            {
                int pos = i * frameSize;
                if (adpcm[pos] != 0 || adpcm[pos + 1] != 0)
                {
                    adpcm[pos] ^= (byte)(xor >> 8);
                    adpcm[pos] &= 0x1f;
                    adpcm[pos + 1] ^= (byte)xor;
                }

                for (int c = 0; c < channelCount; c++)
                {
                    xor = (xor * key.Mult + key.Inc) & 0x7fff;
                }
            }
        }
    }
}
