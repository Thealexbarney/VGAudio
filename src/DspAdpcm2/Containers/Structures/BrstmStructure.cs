using System.Collections.Generic;
using DspAdpcm.Containers.Structures.Base;
using DspAdpcm.Formats.Adpcm;

namespace DspAdpcm.Containers.Structures
{
    public class BrstmStructure
    {
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
        /// A list of all tracks defined in the file.
        /// </summary>
        public List<AdpcmTrack> Tracks { get; set; } = new List<AdpcmTrack>();
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

        /// <summary>
        /// The size of the entire BRSTM file.
        /// </summary>
        public int FileSize { get; set; }
        /// <summary>
        /// The version listed in the RSTM header.
        /// </summary>
        public int Version { get; set; }
        /// <summary>
        /// The size of the RSTM header.
        /// </summary>
        public int RstmHeaderSize { get; set; }
        /// <summary>
        /// The number of sections listed in the RSTM header.
        /// </summary>
        public int RstmHeaderSections { get; set; }
        /// <summary>
        /// The offset of the HEAD chunk.
        /// </summary>
        public int HeadChunkOffset { get; set; }
        /// <summary>
        /// The size of the HEAD chunk as stated in the
        /// RSTM header.
        /// </summary>
        public int HeadChunkSizeRstm { get; set; }
        /// <summary>
        /// The offset of the ADPC chunk.
        /// </summary>
        public int AdpcChunkOffset { get; set; }
        /// <summary>
        /// The size of the ADPC chunk as stated in the
        /// RSTM header.
        /// </summary>
        public int AdpcChunkSizeRstm { get; set; }
        /// <summary>
        /// The offset of the DATA chunk.
        /// </summary>
        public int DataChunkOffset { get; set; }
        /// <summary>
        /// The size of the DATA chunk as stated in the
        /// RSTM header.
        /// </summary>
        public int DataChunkSizeRstm { get; set; }

        /// <summary>
        /// The size of the HEAD chunk as stated in the
        /// HEAD chunk header.
        /// </summary>
        public int HeadChunkSize { get; set; }
        /// <summary>
        /// The offset of part 1 of the HEAD chunk.
        /// </summary>
        public int HeadChunk1Offset { get; set; }
        /// <summary>
        /// The offset of part 2 of the HEAD chunk.
        /// </summary>
        public int HeadChunk2Offset { get; set; }
        /// <summary>
        /// The offset of part 3 of the HEAD chunk.
        /// </summary>
        public int HeadChunk3Offset { get; set; }

        /// <summary>
        /// The size of the ADPC chunk as stated in the
        /// ADPC chunk header.
        /// </summary>
        public int AdpcChunkSize { get; set; }
        /// <summary>
        /// Specifies whether the seek table is full
        /// length, or a truncated table used in some
        /// games including Pokémon Battle Revolution
        /// and Mario Party 8. 
        /// </summary>
        public BrstmSeekTableType SeekTableType { get; set; }

        /// <summary>
        /// The size of the DATA chunk as stated in the
        /// DATA chunk header.
        /// </summary>
        public int DataChunkSize { get; set; }

        /// <summary>
        /// The audio codec.
        /// </summary>
        public BxstmCodec Codec { get; set; }

        /// <summary>
        /// The type of description used for the tracks
        /// in part 2 of the HEAD chunk.
        /// </summary>
        public BrstmTrackType HeaderType { get; set; }
    }

    /// <summary>
    /// The different types of seek tables used in BRSTM files.
    /// </summary>
    public enum BrstmSeekTableType
    {
        /// <summary>
        /// A normal length, complete seek table.
        /// </summary>
        Standard,
        /// <summary>
        /// A shortened, truncated seek table used in games 
        /// including Pokémon Battle Revolution and Mario Party 8.
        /// </summary>
        Short
    }

    /// <summary>
    /// The different track description types used in BRSTM files.
    /// </summary>
    public enum BrstmTrackType
    {
        /// <summary>
        /// The track description used in most games other than 
        /// Super Smash Bros. Brawl.
        /// </summary>
        Standard,
        /// <summary>
        /// The track description used in Super Smash Bros. Brawl.
        /// It does not contain values for volume or panning.
        /// </summary>
        Short
    }
}
