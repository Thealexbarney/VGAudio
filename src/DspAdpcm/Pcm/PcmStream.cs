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

        internal IList<PcmChannel> Channels { get; set; } = new List<PcmChannel>();

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
