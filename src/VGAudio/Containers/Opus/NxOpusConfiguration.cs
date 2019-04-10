namespace VGAudio.Containers.Opus
{
    public class NxOpusConfiguration : Configuration
    {
        public NxOpusHeaderType HeaderType { get; set; }
        public int Bitrate { get; set; }
        public bool EncodeCbr { get; set; }
    }
}
