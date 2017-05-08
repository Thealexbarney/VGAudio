using System.Collections.Generic;
using VGAudio.Utilities;

namespace VGAudio.Containers.Hps
{
    public class HpsBlock
    {
        public int Offset { get; set; }
        public int NextOffset { get; set; }
        public int Size { get; set; }
        public int FinalNibble { get; set; }
        public int AudioSizeBytes => (FinalNibble + 1).DivideBy2RoundUp();
        public List<HpsBlockChannel> Channels { get; } = new List<HpsBlockChannel>();
    }
}
