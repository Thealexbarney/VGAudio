using System.Collections.Generic;

namespace DspAdpcm.Codecs
{
    /// <summary>
    /// A 16-bit PCM audio stream.
    /// The stream can contain any number of individual channels.
    /// </summary>
    public class Pcm16Stream
    {
        /// <summary>
        /// The number of samples in the <see cref="Pcm16Stream"/>.
        /// </summary>
        public int NumSamples { get; set; }

        internal List<Pcm16Channel> Channels { get; set; } = new List<Pcm16Channel>();

        public Pcm16Stream(int numSamples)
        {
            NumSamples = numSamples;
        }

        public bool AddChannel(short[] audio)
        {
            if (audio.Length != NumSamples)
            {
                return false;
            }

            var channel = new Pcm16Channel(audio);
            Channels.Add(channel);

            return true;
        }
    }
}
