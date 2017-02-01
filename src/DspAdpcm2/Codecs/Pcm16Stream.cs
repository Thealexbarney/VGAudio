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

        internal List<short[]> Channels { get; set; } = new List<short[]>();

        public short[][] GetAudio => Channels.ToArray();

        public Pcm16Stream(int numSamples)
        {
            NumSamples = numSamples;
        }

        internal bool AddChannel(short[] audio)
        {
            if (audio.Length != NumSamples)
            {
                return false;
            }
            Channels.Add(audio);

            return true;
        }
    }
}
