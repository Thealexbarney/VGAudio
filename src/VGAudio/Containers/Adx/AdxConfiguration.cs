using VGAudio.Formats.CriAdx;

namespace VGAudio.Containers.Adx
{
    public class AdxConfiguration : Configuration
    {
        public int Version { get; set; } = 4;
        public int FrameSize { get; set; } = 18;
        public int Filter { get; set; } = 2;
        public CriAdxType Type { get; set; } = CriAdxType.Linear;
    }
}