using System.Collections.Generic;

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
    }
}
