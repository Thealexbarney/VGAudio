namespace VGAudio.Codecs.CriAdx
{
    public class CriAdxParameters : CodecParameters
    {
        public int SampleRate { get; set; } = 48000;
        public int HighpassFrequency { get; set; } = 500;
        public int FrameSize { get; set; } = 18;
        public int Version { get; set; } = 4;
        public short History { get; set; }
        public int Padding { get; set; }
        public CriAdxType Type { get; set; } = CriAdxType.Linear;
        public int Filter { get; set; }
    }
}