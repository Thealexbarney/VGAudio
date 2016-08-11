using System;
using System.Collections.Generic;
using System.Linq;
using static DspAdpcm.Encode.Helpers;

namespace DspAdpcm.Encode.Adpcm
{
    public class AdpcmStream
    {
        public IList<AdpcmChannel> Channels { get; private set; } = new List<AdpcmChannel>();
        private IList<AdpcmTrack> _tracks;
        internal IList<AdpcmTrack> Tracks
        {
            get { return _tracks ?? GetDefaultTrackList().ToList(); }
            set { _tracks = value; }
        }

        /// <summary>
        /// The number of samples in the <see cref="AdpcmStream"/>.
        /// </summary>
        public int NumSamples { get; }
        private int NumNibbles => GetNibbleFromSample(NumSamples);
        /// <summary>
        /// The audio sample rate of the <see cref="AdpcmStream"/>.
        /// </summary>
        public int SampleRate { get; }

        /// <summary>
        /// The loop start point in samples.
        /// </summary>
        public int LoopStart { get; private set; }
        /// <summary>
        /// The loop end point in samples.
        /// </summary>
        public int LoopEnd { get; private set; }
        /// <summary>
        /// Indicates whether the <see cref="AdpcmStream"/>
        /// loops or not.
        /// </summary>
        public bool Looping { get; private set; }

        /// <summary>
        /// Creates an empty<see cref="AdpcmStream"/> and sets the
        /// number of samples and sample rate.
        /// </summary>
        /// <param name="numSamples">The sample count.</param>
        /// <param name="sampleRate">The sample rate in Hz.</param>
        public AdpcmStream(int numSamples, int sampleRate)
        {
            NumSamples = numSamples;
            SampleRate = sampleRate;
        }

        /// <summary>
        /// Sets the loop points for the <see cref="AdpcmStream"/>.
        /// </summary>
        /// <param name="loopStart">The start loop point in samples.</param>
        /// <param name="loopEnd">The end loop point in samples.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the
        /// specified <paramref name="loopStart"/> or <paramref name="loopEnd"/>
        /// are invalid./></exception>
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
            return MemberwiseClone() as AdpcmStream;
        }

        public AdpcmStream ShallowCloneChannelSubset(int start, int end)
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
            copy.Channels = new List<AdpcmChannel>();

            for (int i = start; i <= end; i++)
            {
                copy.Channels.Add(Channels[i]);
            }

            return copy;
        }

        private IEnumerable<AdpcmTrack> GetDefaultTrackList()
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

    internal class AdpcmTrack
    {
        public int Volume { get; set; } = 0x7f;
        public int Panning { get; set; } = 0x40;
        public int NumChannels { get; set; }
        public int ChannelLeft { get; set; }
        public int ChannelRight { get; set; }
    }
}
