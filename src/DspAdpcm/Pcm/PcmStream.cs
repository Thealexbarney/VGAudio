using System;
using System.Collections.Generic;
using static DspAdpcm.Helpers;

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
    public class PcmStream
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
