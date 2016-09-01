using System.Collections.Generic;

namespace DspAdpcm.Adpcm.Formats
{
    /// <summary>
    /// Defines the partial common structure and metadata
    /// of IDSP files.
    /// </summary>
    public class IdspStructure
    {
        /// <summary>
        /// This flag is set if the file loops.
        /// </summary>
        public bool Looping { get; set; }
        /// <summary>
        /// The number of channels in the file.
        /// </summary>
        public int NumChannels { get; set; }
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
        public int NumSamples { get; set; }
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
        public int ChannelSize { get; set; }
        /// <summary>
        /// The ADPCM information for each channel.
        /// </summary>
        public List<IDSPChannelInfo> Channels { get; set; } = new List<IDSPChannelInfo>();
        internal byte[][] AudioData { get; set; }
    }

    /// <summary>
    /// Defines the ADPCM information for a single
    /// ADPCM channel.
    /// Used in IDSP files.
    /// </summary>
    public class IDSPChannelInfo : AdpcmChannelInfo
    {
        /// <summary>
        /// The number of samples in the channel.
        /// </summary>
        public int NumSamples { get; set; }
        /// <summary>
        /// The number of ADPCM nibbles in the channel.
        /// </summary>
        public int NumNibbles { get; set; }
        /// <summary>
        /// The sample rate of the audio.
        /// </summary>
        public int SampleRate { get; set; }
        /// <summary>
        /// This flag is set if the channel loops.
        /// </summary>
        public bool Looping { get; set; }
        /// <summary>
        /// The end loop point in nibbles.
        /// </summary>
        public int StartLoop { get; set; }
        /// <summary>
        /// The end loop point in nibbles.
        /// </summary>
        public int EndLoop { get; set; }
        /// <summary>
        /// Not sure what this value is used for.
        /// </summary>
        public int Unknown { get; set; }
    }
}
