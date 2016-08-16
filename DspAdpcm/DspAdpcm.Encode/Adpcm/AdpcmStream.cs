using System;
using System.Collections.Generic;
using System.Linq;
using static DspAdpcm.Encode.Helpers;

namespace DspAdpcm.Encode.Adpcm
{
    /// <summary>
    /// A 4-bit Nintendo ADPCM audio stream.
    /// The stream can contain any number of individual channels.
    /// </summary>
    public class AdpcmStream
    {
        internal List<AdpcmChannel> Channels { get; set; } = new List<AdpcmChannel>();
        private List<AdpcmTrack> _tracks;
        internal List<AdpcmTrack> Tracks
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
        /// The number of channels currently in the <see cref="AdpcmStream"/>.
        /// </summary>
        public int NumChannels => Channels.Count;

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

        private AdpcmStream ShallowClone() => (AdpcmStream)MemberwiseClone();

        /// <summary>
        /// Returns a new <see cref="AdpcmStream"/> shallow clone containing 
        /// the specified subset of channels. The channels are the only
        /// shallow cloned item. Everything else is deep cloned. 
        /// </summary>
        /// <param name="startIndex">The zero-based index at which the range starts.</param>
        /// <param name="count">The number of channels in the range.</param>
        /// <returns>The new <see cref="AdpcmStream"/> containing the specified channels.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="startIndex"/>
        /// or <paramref name="count"/> are out of range.</exception>
        public AdpcmStream GetChannels(int startIndex, int count)
        {
            AdpcmStream copy = ShallowClone();
            
            copy.Channels = Channels.GetRange(startIndex, count);
            copy._tracks = _tracks.Select(x => x.Clone()).ToList();

            return copy;
        }

        private IEnumerable<AdpcmTrack> GetDefaultTrackList()
        {
            int numTracks = Channels.Count.DivideByRoundUp(2);
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

        public AdpcmTrack Clone() => (AdpcmTrack)MemberwiseClone();
    }
}
