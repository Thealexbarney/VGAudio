using System.Collections.Generic;

namespace DspAdpcm.Encode.Pcm
{
    public class PcmStream
    {
        public int NumSamples { get; set; }
        public int SampleRate { get; set; }
        public int BitDepth { get; set; }

        public IList<PcmChannel> Channels { get; set; } = new List<PcmChannel>();
    }
}
