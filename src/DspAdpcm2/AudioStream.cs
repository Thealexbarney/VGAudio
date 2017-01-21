using DspAdpcm.Codecs;

namespace DspAdpcm
{
    /// <summary>
    /// Represents an audio stream.
    /// The stream can contain any number of individual channels.
    /// </summary>
    public class AudioStream
    {
        private Pcm16Stream _pcm16;

        /// <summary>
        /// The number of samples in the <see cref="AudioStream"/>.
        /// </summary>
        public int NumSamples { get; }
        /// <summary>
        /// The sample rate of the <see cref="AudioStream"/> in Hz
        /// </summary>
        public int SampleRate { get; set; }

        /// <summary>
        /// The loop start point in samples.
        /// </summary>
        public int LoopStart { get; internal set; }
        /// <summary>
        /// The loop end point in samples.
        /// </summary>
        public int LoopEnd { get; internal set; }
        /// <summary>
        /// Indicates whether the <see cref="AudioStream"/>
        /// loops or not.
        /// </summary>
        public bool Looping { get; private set; }

        public AudioStream(int numSamples)
        {
            NumSamples = numSamples;
        }

        public AudioStream(int numSamples, int sampleRate)
        {
            NumSamples = numSamples;
            SampleRate = sampleRate;
        }

        /// <summary>
        /// The 16-bit PCM encoding of the <see cref="AudioStream"/>
        /// </summary>
        public Pcm16Stream Pcm16
        {
            get { return _pcm16 ?? (_pcm16 = new Pcm16Stream(NumSamples)); }
            set { _pcm16 = value; }
        }
    }
}
