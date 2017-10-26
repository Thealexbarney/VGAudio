using VGAudio.Codecs.CriHca;

namespace VGAudio.Containers.Hca
{
    public class HcaConfiguration : Configuration
    {
        public CriHcaKey EncryptionKey { get; set; }
        public CriHcaQuality Quality { get; set; }
        public int Bitrate { get; set; }
        public bool LimitBitrate { get; set; }
    }
}
