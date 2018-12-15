using System.Collections.Generic;
using VGAudio.Codecs.Opus;

namespace VGAudio.Containers.Opus
{
    public class NxOpusStructure
    {
        public NxOpusHeaderType HeaderType { get; set; }

        public uint Type { get; set; }
        public int HeaderSize { get; set; }
        public int Version { get; set; }
        public int ChannelCount { get; set; }
        public int FrameSize { get; set; }
        public int SampleRate { get; set; }
        public int DataOffset { get; set; }
        public int PreSkip { get; set; }

        public uint DataType { get; set; }
        public int DataSize { get; set; }

        public int SampleCount { get; set; }
        public int LoopStart { get; set; }
        public int LoopEnd { get; set; }
        public bool Looping { get; set; }

        public int NamcoFieldC { get; set; }
        public int NamcoField1C { get; set; }
        public int NamcoDataOffset { get; set; }
        public int NamcoCoreDataLength { get; set; }
        public int SadfDataOffset { get; set; }

        public List<OpusFrame> Frames { get; set; } = new List<OpusFrame>();
    }
}
