using System;
using System.Collections.Generic;
using static DspAdpcm.Encode.Helpers;

namespace DspAdpcm.Encode.Adpcm
{
    public class AdpcmStream : IAdpcmStream
    {
        public IList<IAdpcmChannel> Channels { get; private set; } = new List<IAdpcmChannel>();

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
            if (loopStart < 0 || loopStart > NumSamples)
            {
                throw new ArgumentOutOfRangeException(nameof(loopStart), loopStart, "Loop points must be less than the number of samples and non-negative.");
            }

            if (loopEnd < 0 || loopEnd > NumSamples)
            {
                throw new ArgumentOutOfRangeException(nameof(loopEnd), loopEnd, "Loop points must be less than the number of samples and non-negative.");
            }

            if (loopEnd < loopStart)
            {
                throw new ArgumentOutOfRangeException(nameof(loopEnd), loopEnd, "The loop end must be greater than the loop start");
            }

            Looping = true;
            LoopStart = loopStart;
            LoopEnd = loopEnd;
        }

        public AdpcmStream ShallowClone()
        {
            return this.MemberwiseClone() as AdpcmStream;
        }

        public IAdpcmStream ShallowCloneChannelSubset(int start, int end)
        {
            if (start > Channels.Count || end > Channels.Count)
            {
                throw new ArgumentOutOfRangeException(start > Channels.Count ? nameof(start) : nameof(end),
                    $"Argument must be less than channel count ({Channels.Count})");
            }

            if (start < 0 || end < 0)
            {
                throw new ArgumentOutOfRangeException(start > Channels.Count ? nameof(start) : nameof(end),
                    "Argument must be greater than zero");
            }

            if (start > end)
            {
                throw new ArgumentOutOfRangeException(nameof(start),
                    $"{nameof(start)} must be less than or equal to {nameof(end)}");
            }

            AdpcmStream copy = ShallowClone();
            copy.Channels = new List<IAdpcmChannel>();

            for (int i = start; i <= end; i++)
            {
                copy.Channels.Add(Channels[i]);
            }

            return copy;
        }
    }
}
