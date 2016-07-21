using System.Collections.Generic;
using static DspAdpcm.Encode.Adpcm.Helpers;

namespace DspAdpcm.Encode.Adpcm
{
    public class AdpcmStream
    {
        public IList<AdpcmChannel> Channels { get; } = new List<AdpcmChannel>();

        public int NumSamples { get; }
        public int NumNibbles => GetNibbleFromSample(NumSamples);
        public int SampleRate { get; }

        public int LoopStart { get; private set; }
        public int LoopEnd { get; private set; }
        public bool Looping { get; private set; }

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
