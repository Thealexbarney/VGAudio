namespace VGAudio.Codecs.CriHca
{
    public static class CriHcaConstants
    {
        public const int SubframesPerFrame = 8;
        public const int SubFrameSamplesBits = 7;
        public const int SamplesPerSubFrame = 1 << SubFrameSamplesBits;
        public const int SamplesPerFrame = SubframesPerFrame * SamplesPerSubFrame;
    }
}
