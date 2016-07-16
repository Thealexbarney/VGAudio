using System.Collections.Generic;
using static DspAdpcm.Encode.Adpcm.Helpers;

namespace DspAdpcm.Encode.Adpcm
{
    public class AdpcmStream
    {
        private IPcmStream InputPcmStream { get; set; }
        public IList<AdpcmChannel> Channels { get; set; } = new List<AdpcmChannel>();

        public int NumSamples { get; }
        public int NumNibbles => GetNibbleFromSample(NumSamples);
        public int SampleRate { get; }

        public int LoopStart { get; set; }
        public int LoopEnd { get; set; }
        public bool Looping { get; set; }

        public AdpcmStream(int samples, int sampleRate)
        {
            NumSamples = samples;
            SampleRate = sampleRate;
        }

        public void SetLoop(int loopStart, int loopEnd)
        {
            Looping = true;
            LoopStart = loopStart;
            LoopEnd = loopEnd;
        }
    }
}
