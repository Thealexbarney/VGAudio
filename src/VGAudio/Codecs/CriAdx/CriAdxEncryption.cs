using System.Collections.Generic;
using VGAudio.Utilities;

namespace VGAudio.Codecs.CriAdx
{
    public static partial class CriAdxEncryption
    {
        public static void EncryptDecrypt(byte[][] adpcm, CriAdxKey key, int encryptionType, int frameSize)
        {
            for (int i = 0; i < adpcm.Length; i++)
            {
                EncryptDecryptChannel(adpcm[i], key, encryptionType, frameSize, i, adpcm.Length);
            }
        }

        public static void EncryptDecryptChannel(byte[] adpcm, CriAdxKey key, int encryptionType, int frameSize, int channelNum, int channelCount)
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
                if (FrameNotEmpty(adpcm, pos, frameSize))
                {
                    adpcm[pos] ^= (byte)(xor >> 8);
                    if (encryptionType == 9) adpcm[pos] &= 0x1f;
                    adpcm[pos + 1] ^= (byte)xor;
                }

                for (int c = 0; c < channelCount; c++)
                {
                    xor = (xor * key.Mult + key.Inc) & 0x7fff;
                }
            }
        }

        public static CriAdxKey FindKey(byte[][] adpcm, int encryptionType, int frameSize)
        {
            ushort[] scales = GetScales(adpcm, frameSize);
            List<CriAdxKey> keys = encryptionType == 8 ? Keys8 : Keys9;

            foreach (CriAdxKey key in keys)
            {
                if (TestKey(key, encryptionType, scales))
                {
                    return key;
                }
            }

            return null;
        }

        private static ushort[] GetScales(byte[][] adpcm, int frameSize)
        {
            if (adpcm.Length < 1) return null;

            int channelCount = adpcm.Length;
            int frameCount = adpcm[0].Length.DivideByRoundUp(frameSize);
            var scales = new ushort[frameCount * channelCount];

            for (int frame = 0; frame < frameCount; frame++)
            {
                for (int channel = 0; channel < channelCount; channel++)
                {
                    int pos = frame * frameSize;
                    scales[frame * channelCount + channel] = (ushort)(adpcm[channel][pos] << 8 | adpcm[channel][pos + 1]);
                }
            }

            return scales;
        }

        private static bool TestKey(CriAdxKey key, int encryptionType, ushort[] scales)
        {
            int validationMask = encryptionType == 8 ? 0xE000 : 0x1000;
            int xorMask = 0x7fff;

            int xor = key.Seed;
            foreach (ushort scale in scales)
            {
                if (((scale ^ xor) & validationMask) != 0 && scale != 0)
                {
                    return false;
                }
                xor = (xor * key.Mult + key.Inc) & xorMask;
            }
            return true;
        }

        private static bool FrameNotEmpty(byte[] bytes, int start, int length)
        {
            int end = start + length;
            for (int i = start; i < end; i++)
            {
                if (bytes[i] != 0)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
