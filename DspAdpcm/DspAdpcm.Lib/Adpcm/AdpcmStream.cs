using System;
using System.Collections.Generic;
using System.Linq;
using static DspAdpcm.Lib.Helpers;

namespace DspAdpcm.Lib.Adpcm
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
        public int LoopStart { get; internal set; }
        /// <summary>
        /// The loop end point in samples.
        /// </summary>
        public int LoopEnd { get; internal set; }
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

        /// <summary>
        /// Adds all the channels in the input <see cref="AdpcmStream"/>
        /// to the current one.
        /// </summary>
        /// <param name="adpcm">The <see cref="AdpcmStream"/> containing
        /// the channels to add.</param>
        public void Add(AdpcmStream adpcm)
        {
            if (adpcm.NumSamples != NumSamples)
            {
                throw new ArgumentException("Only audio streams of the same length can be added to each other.");
            }

            Channels.AddRange(adpcm.Channels);
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
            copy._tracks = _tracks?.Select(x => x.Clone()).ToList();

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

    /// <summary>
    /// Defines an audio track in an ADPCM audio
    /// stream. Each track is composed of one
    /// or two channels.
    /// </summary>
    public class AdpcmTrack
    {
        /// <summary>
        /// The volume of the track. Ranges from
        /// 0 to 127 (0x7f).
        /// </summary>
        public int Volume { get; set; } = 0x7f;
        /// <summary>
        /// The panning of the track. Ranges from
        /// 0 (Completely to the left) to 127 (0x7f)
        /// (Completely to the right) with the center
        /// at 64 (0x40).
        /// </summary>
        public int Panning { get; set; } = 0x40;
        /// <summary>
        /// The number of channels in the track.
        /// If <c>1</c>, only <see cref="ChannelLeft"/>
        /// will be used for the mono track.
        /// If <c>2</c>, both <see cref="ChannelLeft"/>
        /// and <see cref="ChannelRight"/> will be used.
        /// </summary>
        public int NumChannels { get; set; }
        /// <summary>
        /// The zero-based ID of the left channel in a stereo
        /// track, or the only channel in a mono track.
        /// </summary>
        public int ChannelLeft { get; set; }
        /// <summary>
        /// The zero-based ID of the right channel in
        /// a stereo track.
        /// </summary>
        public int ChannelRight { get; set; }

        internal AdpcmTrack Clone() => (AdpcmTrack)MemberwiseClone();
    }
}
