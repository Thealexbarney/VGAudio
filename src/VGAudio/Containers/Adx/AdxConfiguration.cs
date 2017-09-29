using VGAudio.Codecs.CriAdx;

namespace VGAudio.Containers.Adx
{
    public class AdxConfiguration : Configuration
    {
        public int Version { get; set; } = 4;
        public int EncryptionType { get; set; }
        public CriAdxKey EncryptionKey { get; set; }
        public int FrameSize { get; set; } = 18;
        public int Filter { get; set; } = 2;
        public CriAdxType Type { get; set; } = CriAdxType.Linear;
    }
}