namespace VGAudio.Codecs.CriHca
{
    public static class CriHcaEncryption
    {
        public static void Decrypt(HcaInfo hca, byte[][] audio, CriHcaKey key)
        {
            for (int frame = 0; frame < hca.FrameCount; frame++)
            {
                for (int b = 0; b < hca.FrameSize; b++)
                {
                    audio[frame][b] = key.DecryptionTable[audio[frame][b]];
                }
            }
        }
    }
}
