using System;
using System.Collections.Generic;
using System.Linq;
using static DspAdpcm.Encode.Helpers;

namespace DspAdpcm.Encode.Adpcm
{
    public class AdpcmStream : IAdpcmStream
    {
        public IList<IAdpcmChannel> Channels { get; private set; } = new List<IAdpcmChannel>();
        private IList<AdpcmTrack> _tracks;
        public IList<AdpcmTrack> Tracks
        {
            get { return _tracks ?? GetDefaultTrackList().ToList(); }
            set { _tracks = value; }
        }

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

        public IEnumerable<AdpcmTrack> GetDefaultTrackList()
        {
            int numTracks = (int)Math.Ceiling((double)Channels.Count / 2);
            for (int i = 0; i < numTracks; i++)
            {
                int numChannels = Math.Min(Channels.Count - i * 2, 2);
                yield return new AdpcmTrack
                {
                    NumChannels = numChannels,
                    ChannelLeft = i * 2,
                    ChannelRight = numChannels >= 2 ? i * 2 + 1 : 0
                };
            }
        }
    }

    public class AdpcmTrack
    {
        public int Volume { get; set; } = 0x7f;
        public int Panning { get; set; } = 0x40;
        public int NumChannels { get; set; }
        public int ChannelLeft { get; set; }
        public int ChannelRight { get; set; }
    }
}
