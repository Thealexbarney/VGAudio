namespace VGAudio.Codecs.CriHca
{
    public class CriHcaParameters : CodecParameters
    {
        public CriHcaParameters() { }
        public CriHcaParameters(CodecParameters source) : base(source) { }
        public int Quality { get; set; } = 0;
        public int ChannelCount { get; set; }
        public int SampleRate { get; set; }
    }
}
