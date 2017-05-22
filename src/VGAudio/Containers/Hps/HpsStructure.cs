using System.Collections.Generic;

namespace VGAudio.Containers.Hps
{
    public class HpsStructure
    {
        public int ChannelCount { get; set; }
        public int SampleRate { get; set; }
        public bool Looping { get; set; }
        public int LoopStart { get; set; }
        public int SampleCount { get; set; }
        public List<HpsChannelInfo> Channels { get; } = new List<HpsChannelInfo>();
        public List<HpsBlock> Blocks { get; } = new List<HpsBlock>();
    }
}
