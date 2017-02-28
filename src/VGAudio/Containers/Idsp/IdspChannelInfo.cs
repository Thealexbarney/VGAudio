using VGAudio.Formats.GcAdpcm;

namespace VGAudio.Containers.Idsp
{
    public class IdspChannelInfo : GcAdpcmChannelInfo
    {
        internal IdspChannelInfo() { }

        /// <summary>
        /// The number of samples in the channel.
        /// </summary>
        public int SampleCount { get; set; }
        /// <summary>
        /// The number of ADPCM nibbles in the channel.
        /// </summary>
        public int NibbleCount { get; set; }
        /// <summary>
        /// The sample rate of the audio.
        /// </summary>
        public int SampleRate { get; set; }
        /// <summary>
        /// This flag is set if the channel loops.
        /// </summary>
        public bool Looping { get; set; }
        /// <summary>
        /// The address, in nibbles, of the start
        /// loop point.
        /// </summary>
        public int StartAddress { get; set; }
        /// <summary>
        /// The address, in nibbles, of the end
        /// loop point.
        /// </summary>
        public int EndAddress { get; set; }
        /// <summary>
        /// The address, in nibbles, of the initial
        /// playback position.
        /// </summary>
        public int CurrentAddress { get; set; }
    }
}