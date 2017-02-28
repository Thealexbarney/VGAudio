using System.Collections.Generic;

namespace VGAudio.Containers.Idsp
{
    public class IdspStructure
    {
        internal IdspStructure() { }

        /// <summary>
        /// This flag is set if the file loops.
        /// </summary>
        public bool Looping { get; set; }
        /// <summary>
        /// The number of channels in the file.
        /// </summary>
        public int ChannelCount { get; set; }
        /// <summary>
        /// The sample rate of the audio.
        /// </summary>
        public int SampleRate { get; set; }
        /// <summary>
        /// The start loop point in samples.
        /// </summary>
        public int LoopStart { get; set; }
        /// <summary>
        /// The end loop point in samples.
        /// </summary>
        public int LoopEnd { get; set; }
        /// <summary>
        /// The number of samples in the file.
        /// </summary>
        public int SampleCount { get; set; }
        /// <summary>
        /// The offset that the actual audio data starts at.
        /// </summary>
        public int AudioDataOffset { get; set; }
        /// <summary>
        /// The number of bytes per channel in each
        /// interleaved audio data block.
        /// </summary>
        public int InterleaveSize { get; set; }
        /// <summary>
        /// The size, in bytes, of the main IDSP header.
        /// </summary>
        public int HeaderSize { get; set; }
        /// <summary>
        /// The size, in bytes, of the information for each channel
        /// in the file header.
        /// </summary>
        public int ChannelInfoSize { get; set; }
        /// <summary>
        /// The length, in bytes, of the audio data of each channel.
        /// </summary>
        public int AudioDataLength { get; set; }
        /// <summary>
        /// The ADPCM information for each channel.
        /// </summary>
        public List<IdspChannelInfo> Channels { get; set; } = new List<IdspChannelInfo>();
        internal byte[][] AudioData { get; set; }
    }
}
