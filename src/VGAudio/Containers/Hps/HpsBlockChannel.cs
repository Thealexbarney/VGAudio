using VGAudio.Formats.GcAdpcm;

namespace VGAudio.Containers.Hps
{
    public class HpsBlockChannel
    {
        public GcAdpcmContext Context { get; set; }
        public byte[] AudioData { get; set; }
    }
}
