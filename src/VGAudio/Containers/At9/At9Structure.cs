using VGAudio.Codecs.Atrac9;

namespace VGAudio.Containers.At9
{
    public class At9Structure
    {
        public Atrac9Config Config { get; set; }
        public byte[][] AudioData { get; set; }
        public int SampleCount { get; set; }
        public int Version { get; set; }
        public int EncoderDelay { get; set; }
        public int SuperframeCount { get; set; }

        public bool Looping { get; set; }
        public int LoopStart { get; set; }
        public int LoopEnd { get; set; }
    }
}
