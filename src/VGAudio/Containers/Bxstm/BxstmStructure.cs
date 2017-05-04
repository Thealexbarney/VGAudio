using System.Collections.Generic;
using VGAudio.Formats;
using VGAudio.Utilities;

namespace VGAudio.Containers.Bxstm
{
    public abstract class BxstmStructure
    {
        /// <summary>
        /// The size of the entire file.
        /// </summary>
        public int FileSize { get; set; }
        public Endianness Endianness { get; set; }
        /// <summary>
        /// The version listed in the header.
        /// </summary>
        public int Version { get; set; }
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
        /// The number of samples in the file.
        /// </summary>
        public int SampleCount { get; set; }
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
        /// The audio codec.
        /// </summary>
        public BxstmCodec Codec { get; set; }
        /// <summary>
        /// A list of all tracks defined in the file.
        /// </summary>
        public List<AudioTrack> Tracks { get; set; } = new List<AudioTrack>();
        /// <summary>
        /// The ADPCM information for each channel.
        /// </summary>
        public List<BxstmChannelInfo> Channels { get; set; } = new List<BxstmChannelInfo>();
        /// <summary>
        /// The size of the seek table in the file.
        /// </summary>
        public int SeekTableSize { get; set; }
        /// <summary>
        /// The seek table containing PCM samples
        /// from throughout the audio stream.
        /// </summary>
        public short[][] SeekTable { get; set; }
        internal byte[][] AudioData { get; set; }
    }
}