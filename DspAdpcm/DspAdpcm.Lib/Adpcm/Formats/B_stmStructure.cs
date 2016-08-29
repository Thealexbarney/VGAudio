using System.Collections.Generic;

namespace DspAdpcm.Lib.Adpcm.Formats
{
    /// <summary>
    /// Defines the partial common structure and metadata
    /// of B_STM files.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public abstract class B_stmStructure
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
        /// The number of samples in the file.
        /// </summary>
        public int NumSamples { get; set; }
        /// <summary>
        /// The offset that the actual audio data starts at.
        /// </summary>
        public int AudioDataOffset { get; set; }
        /// <summary>
        /// The total count of interleaved audio data blocks.
        /// </summary>
        public int InterleaveCount { get; set; }
        /// <summary>
        /// The number of bytes per channel in each
        /// interleaved audio data block.
        /// </summary>
        public int InterleaveSize { get; set; }
        /// <summary>
        /// The number of samples per channel in each
        /// interleaved audio data block.
        /// </summary>
        public int SamplesPerInterleave { get; set; }
        /// <summary>
        /// The number of bytes per channel in the final
        /// interleaved audio data block, not including
        /// the padding at the end of each channel.
        /// </summary>
        public int LastBlockSizeWithoutPadding { get; set; }
        /// <summary>
        /// The number of samples per channel in the final
        /// interleaved audio data block.
        /// </summary>
        public int LastBlockSamples { get; set; }
        /// <summary>
        /// The number of bytes per channel in the final
        /// interleaved audio data block, including
        /// the padding at the end of each channel.
        /// </summary>
        public int LastBlockSize { get; set; }
        /// <summary>
        /// The number of samples per seek table entry.
        /// </summary>
        public int SamplesPerSeekTableEntry { get; set; }
        /// <summary>
        /// The number of bytes per seek table entry.
        /// </summary>
        public int BytesPerSeekTableEntry { get; set; }
        /// <summary>
        /// A list of all tracks defined in the file.
        /// </summary>
        public List<AdpcmTrack> Tracks { get; set; } = new List<AdpcmTrack>();
        /// <summary>
        /// The ADPCM information for each channel.
        /// </summary>
        public List<B_stmChannelInfo> Channels { get; set; } = new List<B_stmChannelInfo>();
        /// <summary>
        /// The length of the seek table in the file.
        /// </summary>
        public int SeekTableLength { get; set; }
        /// <summary>
        /// The seek table containing PCM samples
        /// from throughout the audio stream.
        /// </summary>
        public short[][] SeekTable { get; set; }
        internal byte[][] AudioData { get; set; }
    }

    /// <summary>
    /// Defines the ADPCM information for a single
    /// ADPCM channel.
    /// Used in B_STM files.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public class B_stmChannelInfo : AdpcmChannelInfo
    {
        /// <summary>
        /// The offset of the channel information. 
        /// Used in a B_STM header.
        /// </summary>
        public int Offset { get; set; }
    }
}
