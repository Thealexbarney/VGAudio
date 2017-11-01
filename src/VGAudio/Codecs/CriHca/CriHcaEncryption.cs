using System;
using VGAudio.Utilities;

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
            int startFrame = FindFirstNonEmptyFrame(audio);
            int endFrame = Math.Min(audio.Length, startFrame + FramesToTest);
            for (int i = startFrame; i < endFrame; i++)
            {
                Array.Copy(audio[i], buffer, audio[i].Length);
                DecryptFrame(frame.Hca, buffer, key);
                var reader = new BitReader(buffer);
                if (!CriHcaPacking.UnpackFrame(frame, reader))
                {
                    return false;
                }
            }
            return true;
        }

        private static int FindFirstNonEmptyFrame(byte[][] frames)
        {
            for (int i = 0; i < frames.Length; i++)
            {
                if (!FrameEmpty(frames[i]))
                {
                    return i;
                }
            }
            return 0;
        }

        private static bool FrameEmpty(byte[] frame)
        {
            for (int i = 2; i < frame.Length - 2; i++)
            {
                if (frame[i] != 0)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
