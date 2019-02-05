namespace VGAudio.Codecs.Opus
{
    public class OpusFrame
    {
        public int Length { get; set; }
        public uint FinalRange { get; set; }
        public byte[] Data { get; set; }
        public int SampleCount { get; set; }
    }
}
