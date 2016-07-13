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
        public bool LoopFlag { get; }

        public AdpcmStream(int samples, int sampleRate)
        {
            NumSamples = samples;
            SampleRate = sampleRate;
        }

        public AdpcmStream(int samples, int sampleRate, int loopStart, int loopEnd)
            : this(samples, sampleRate)
        {
            LoopFlag = true;
            LoopStart = loopStart;
            LoopEnd = loopEnd;
        }

        public AdpcmStream(IPcmStream stream)
            : this(stream.GetNumSamples(), stream.GetSampleRate())
        {
            InputPcmStream = stream;
            foreach (IPcmChannel channel in InputPcmStream.GetChannels())
            {
                Channels.Add(new AdpcmChannel(channel));
            }
        }

        public AdpcmStream(IPcmStream stream, int loopStart, int loopEnd)
            : this(stream.GetNumSamples(), stream.GetSampleRate(), loopStart, loopEnd)
        {
            InputPcmStream = stream;
            foreach (IPcmChannel channel in InputPcmStream.GetChannels())
            {
                Channels.Add(new AdpcmChannel(channel));
            }
        }

        public void Encode()
        {
            foreach (var channel in Channels)
            {
                channel.Encode();
            }
        }
    }
}
