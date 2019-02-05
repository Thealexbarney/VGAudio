using System.Collections.Generic;
using VGAudio.Codecs.Opus;

namespace VGAudio.Containers.Opus
{
    public class OggOpusStructure
    {
        public int Version { get; set; }
        public int ChannelCount { get; set; }
        public int PreSkip { get; set; }
        public int SampleRate { get; set; }
        public int OutputGain { get; set; }
        public int SampleCount { get; set; }
        public byte ChannelMapping { get; set; }

        public List<OpusFrame> Frames { get; set; } = new List<OpusFrame>();
    }
}
