namespace VGAudio.Codecs.Opus
{
    public class OpusParameters : CodecParameters
    {
        public OpusParameters() { }
        public OpusParameters(CodecParameters source) : base(source) { }
        public int Bitrate { get; set; }
        public bool EncodeCbr { get; set; }
    }
}
