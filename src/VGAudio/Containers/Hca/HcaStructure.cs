using VGAudio.Codecs.CriHca;

namespace VGAudio.Containers.Hca
{
    public class HcaStructure
    {
        public int Version { get; set; }
        public int HeaderSize { get; set; }

        public HcaInfo Hca { get; set; } = new HcaInfo();

        public CriHcaKey EncryptionKey { get; set; }

        public int Reserved1 { get; set; }
        public int Reserved2 { get; set; }

        public byte[][] AudioData { get; set; }
    }
}
