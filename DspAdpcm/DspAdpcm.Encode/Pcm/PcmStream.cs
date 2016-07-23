using System.Collections.Generic;

namespace DspAdpcm.Encode.Pcm
{
    public class PcmStream : IPcmStream
    {
        public int NumSamples { get; set; }
        public int SampleRate { get; set; }
        public int BitDepth { get; set; }

        public IList<IPcmChannel> Channels { get; set; } = new List<IPcmChannel>();
    }
}
