using System.Collections.Generic;

namespace DspAdpcm.Encode.Pcm
{
    public class PcmStream
    {
        public int NumSamples { get; set; }
        public int SampleRate { get; set; }
        public int BitDepth { get; set; } = 16;

        public IList<PcmChannel> Channels { get; set; } = new List<PcmChannel>();

        public PcmStream() { }

        public PcmStream(int numSamples, int sampleRate)
        {
            NumSamples = numSamples;
            SampleRate = sampleRate;
        }
    }
}
