using System;

namespace VGAudio.Codecs.CriHca
{
    public static partial class CriHcaEncryption
    {
        private const int FramesToTest = 10;

        public static void Decrypt(HcaInfo hca, byte[][] audio, CriHcaKey key)
        {
            for (int frame = 0; frame < hca.FrameCount; frame++)
            {
                DecryptFrame(hca, audio[frame], key);
            }
        }
        public static void DecryptFrame(HcaInfo hca, byte[] audio, CriHcaKey key)
        {
            for (int b = 0; b < hca.FrameSize; b++)
            {
                audio[b] = key.DecryptionTable[audio[b]];
            }
        }

        public static CriHcaKey FindKey(HcaInfo hca, byte[][] audio)
        {
            var frame = new CriHcaFrame(hca);
            var buffer = new byte[hca.FrameSize];
            foreach (CriHcaKey key in Keys)
            {
                if (TestKey(frame, audio, key, buffer))
                {
                    return key;
                }
            }
            return null;
        }

        private static bool TestKey(CriHcaFrame frame, byte[][] audio, CriHcaKey key, byte[] buffer)
        {
            int framesToTest = Math.Min(audio.Length, FramesToTest);
            for (int i = 0; i < framesToTest; i++)
            {
                Array.Copy(audio[i], buffer, audio[i].Length);
                DecryptFrame(frame.Hca, buffer, key);
                var reader = new BitReader(buffer);
                if (!CriHcaDecoder.UnpackFrame(frame, reader))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
