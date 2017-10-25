namespace VGAudio.Codecs.CriHca
{
    public class CriHcaParameters : CodecParameters
    {
        public CriHcaParameters() { }
        public CriHcaParameters(CodecParameters source) : base(source) { }
        public CriHcaQuality Quality { get; set; } = CriHcaQuality.High;
        public int Bitrate { get; set; }
        public bool LimitBitrate { get; set; }
        public int ChannelCount { get; set; }
        public int SampleRate { get; set; }
        public bool Looping { get; set; }
        public int LoopStart { get; set; }
        public int LoopEnd { get; set; }
    }
}
