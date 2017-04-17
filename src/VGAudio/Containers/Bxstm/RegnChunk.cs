using System.Collections.Generic;

namespace VGAudio.Containers.Bxstm
{
    public class RegnChunk
    {
        public int Size { get; set; }
        public int EntryCount { get; set; }
        public List<RegnEntry> Entries { get; set; } = new List<RegnEntry>();
    }

    public class RegnEntry
    {
        public int StartSample { get; set; }
        public int EndSample { get; set; }
        public List<RegnChannel> Channels { get; set; } = new List<RegnChannel>();
    }

    public class RegnChannel
    {
        public short PredScale { get; set; }
        public short Value1 { get; set; }
        public short Value2 { get; set; }
    }
}
