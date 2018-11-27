using System.Collections.Generic;

namespace VGAudio.Containers.Opus
{
    public class NxOpusStructure
    {
        public uint Type { get; set; }
        public int HeaderSize { get; set; }
        public int Version { get; set; }
        public int ChannelCount { get; set; }
        public int FrameSize { get; set; }
        public int SampleRate { get; set; }
        public int DataOffset { get; set; }

        public uint DataType { get; set; }
        public int DataSize { get; set; }

        public List<NxOpusFrame> Frames { get; set; } = new List<NxOpusFrame>();
    }

    public class NxOpusFrame
    {
        public int Length { get; set; }
        public uint FinalRange { get; set; }
        public byte[] Data { get; set; }
        public int SampleCount { get; set; }

        
    }
}
