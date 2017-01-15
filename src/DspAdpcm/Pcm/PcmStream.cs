using System;
using System.Collections.Generic;
using static DspAdpcm.Helpers;
using DspAdpcm.Adpcm;

#if NET20
using DspAdpcm.Compatibility.LinqBridge;
#else
using System.Linq;
#endif

namespace DspAdpcm.Pcm
{
    /// <summary>
    /// A 16-bit PCM audio stream.
    /// The stream can contain any number of individual channels.
    /// </summary>
    public class PcmStream : LoopingTrackStream
    {
        /// <summary>
        /// The number of samples in the <see cref="PcmStream"/>.
        /// </summary>
        public int NumSamples { get; set; }
        /// <summary>
        /// The sample rate of the <see cref="PcmStream"/> in Hz
        /// </summary>
        public int SampleRate { get; set; }

        internal List<PcmChannel> Channels { get; set; } = new List<PcmChannel>();

        /// <summary>
        /// The number of channels currently in the <see cref="PcmStream"/>.
        /// </summary>
        public int NumChannels => Channels.Count;

        /// <summary>
        /// The loop start point in samples.
        /// </summary>
        public int LoopStart { get; internal set; }
        /// <summary>
        /// The loop end point in samples.
        /// </summary>
        public int LoopEnd { get; internal set; }
        /// <summary>
        /// Indicates whether the <see cref="PcmStream"/>
        /// loops or not.
        /// </summary>
        public bool Looping { get; private set; }

        private List<AdpcmTrack> _tracks;

        /// <summary>
        /// A list of tracks in the stream (used for B_STM formats.) Each track is composed of one or two channels.
        /// </summary>
        public List<AdpcmTrack> Tracks
        {
            get { return _tracks == null || _tracks.Count == 0 ? GetDefaultTrackList().ToList() : _tracks; }
            internal set { _tracks = value; }
        }

        /// <summary>
        /// Creates an empty <see cref="PcmStream"/> and sets the
        /// number of samples and sample rate.
        /// </summary>
        /// <param name="numSamples">The sample count.</param>
        /// <param name="sampleRate">The sample rate in Hz.</param>
        public PcmStream(int numSamples, int sampleRate)
        {
            NumSamples = numSamples;
            SampleRate = sampleRate;
        }

        /// <summary>
        /// Adds all the channels in the input <see cref="PcmStream"/>
        /// to the current one.
        /// </summary>
        /// <param name="pcm">The <see cref="PcmStream"/> containing
        /// the channels to add.</param>
        public void Add(PcmStream pcm)
        {
            if (pcm.NumSamples != NumSamples)
            {
                throw new ArgumentException("Only audio streams of the same length can be added to each other.");
            }

            Channels.AddRange(pcm.Channels);
        }

        private PcmStream ShallowClone() => (PcmStream)MemberwiseClone();

        /// <summary>
        /// Returns a new <see cref="PcmStream"/> shallow clone containing 
        /// the specified subset of channels. The channels are the only
        /// shallow cloned item. Everything else is deep cloned. 
        /// </summary>
        /// <param name="startIndex">The zero-based index at which the range starts.</param>
        /// <param name="count">The number of channels in the range.</param>
        /// <returns>The new <see cref="PcmStream"/> containing the specified channels.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="startIndex"/>
        /// or <paramref name="count"/> are out of range.</exception>
        public PcmStream GetChannels(int startIndex, int count)
        {
            PcmStream copy = ShallowClone();

            copy.Channels = Channels.GetRange(startIndex, count);

            return copy;
        }

        /// <summary>
        /// Returns a new <see cref="PcmStream"/> shallow clone containing 
        /// the specified subset of channels. The channels are the only
        /// shallow cloned item. Everything else is deep cloned.
        /// Channels will be returned in the order specified.
        /// </summary>
        /// <param name="channelRange">The channels that will be returned.</param>
        /// <returns>The new <see cref="PcmStream"/> containing the specified channels.</returns>
        /// <exception cref="ArgumentException">Thrown if a channel in <paramref name="channelRange"/>
        /// does not exist.</exception>
        /// <exception cref="ArgumentNullException">Thrown if
        /// <paramref name="channelRange"/> is null</exception>
        public PcmStream GetChannels(IEnumerable<int> channelRange)
        {
            if (channelRange == null)
                throw new ArgumentNullException(nameof(channelRange));

            PcmStream copy = ShallowClone();
            copy.Channels = new List<PcmChannel>();

            foreach (int i in channelRange)
            {
                if (i < 0 || i >= Channels.Count)
                    throw new ArgumentException($"Channel {i} does not exist.", nameof(channelRange));
                copy.Channels.Add(Channels[i]);
            }

            return copy;
        }

        /// <summary>
        /// Sets the loop points for the <see cref="PcmStream"/>.
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
        /// Sets the loop points for the <see cref="PcmStream"/>.
        /// </summary>
        /// <param name="loop">If <c>false</c>, don't loop the <see cref="PcmStream"/>.
        /// If <c>true</c>, loop the <see cref="PcmStream"/> from 0 to <see cref="PcmStream.NumSamples"/></param>
        public void SetLoop(bool loop)
        {
            Looping = loop;
            LoopStart = 0;
            LoopEnd = loop ? NumSamples : 0;
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

        /// <summary>
        /// Get an array of byte arrays containing the encoded audio data (one array per channel.)
        /// </summary>
        /// <param name="endianness">The endianness of the format (not used for ADPCM)</param>
        /// <returns>An array that contains one byte array per channel</returns>
        public byte[][] GetAudioData(Endianness endianness)
        {
            return Channels.Select(x => x.GetAudioData(endianness)).ToArray();
        }

        /// <summary>
        /// Determines whether the specified <see cref="Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="Object"/> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="Object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            var item = obj as PcmStream;

            if (item == null)
            {
                return false;
            }

            return
                item.NumSamples == NumSamples &&
                item.SampleRate == SampleRate &&
                ArraysEqual(item.Channels.ToArray(), Channels.ToArray());
        }

        /// <summary>
        /// Returns a hash code for the <see cref="PcmStream"/> instance.
        /// </summary>
        /// <returns>A hash code for the <see cref="PcmStream"/> instance.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = NumSamples.GetHashCode();
                hashCode = (hashCode * 397) ^ SampleRate.GetHashCode();
                hashCode = (hashCode * 397) ^ Channels.GetHashCode();
                return hashCode;
            }
        }
    }
}
