namespace VGAudio.Containers.Bxstm.Structures
{
    public class StreamInfo
    {
        /// <summary>The audio codec.</summary>
        public BxstmCodec Codec { get; set; }
        /// <summary>This flag is set if the file loops.</summary>
        public bool Looping { get; set; }
        /// <summary>The number of channels in the file.</summary>
        public int ChannelCount { get; set; }
        /// <summary>The number of audio regions in the file.</summary>
        public int RegionCount { get; set; }
        /// <summary>The sample rate of the audio.</summary>
        public int SampleRate { get; set; }
        /// <summary>The start loop point in samples.</summary>
        public int LoopStart { get; set; }
        /// <summary>The number of samples in the file.</summary>
        public int SampleCount { get; set; }

        /// <summary>The total count of interleaved audio data blocks.</summary>
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
        /// <summary>The number of bytes per seek table entry.</summary>
        public int BytesPerSeekTableEntry { get; set; }
        /// <summary>The number of samples per seek table entry.</summary>
        public int SamplesPerSeekTableEntry { get; set; }

        /// <summary>The offset that the actual audio data starts at.</summary>
        public int AudioDataOffset { get; set; }
    }
}