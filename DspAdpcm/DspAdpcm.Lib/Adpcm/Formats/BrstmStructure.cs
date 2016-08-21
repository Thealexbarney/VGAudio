using System.Collections.Generic;

namespace DspAdpcm.Lib.Adpcm.Formats
{
    /// <summary>
    /// Defines the structure and metadata
    /// of a BRSTM file.
    /// </summary>
    public class BrstmStructure
    {
        /// <summary>
        /// The length of the entire BRSTM file.
        /// </summary>
        public int FileLength { get; set; }
        /// <summary>
        /// The length of the RSTM header.
        /// </summary>
        public int RstmHeaderLength { get; set; }
        /// <summary>
        /// The offset of the HEAD chunk.
        /// </summary>
        public int HeadChunkOffset { get; set; }
        /// <summary>
        /// The length of the HEAD chunk as stated in the
        /// RSTM header.
        /// </summary>
        public int HeadChunkLengthRstm { get; set; }
        /// <summary>
        /// The offset of the ADPC chunk.
        /// </summary>
        public int AdpcChunkOffset { get; set; }
        /// <summary>
        /// The length of the ADPC chunk as stated in the
        /// RSTM header.
        /// </summary>
        public int AdpcChunkLengthRstm { get; set; }
        /// <summary>
        /// The offset of the DATA chunk.
        /// </summary>
        public int DataChunkOffset { get; set; }
        /// <summary>
        /// The length of the DATA chunk as stated in the
        /// RSTM header.
        /// </summary>
        public int DataChunkLengthRstm { get; set; }

        /// <summary>
        /// The length of the HEAD chunk as stated in the
        /// HEAD chunk header.
        /// </summary>
        public int HeadChunkLength { get; set; }
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
        /// The length of part 1 of the HEAD chunk.
        /// </summary>
        public int HeadChunk1Length => HeadChunk2Offset - HeadChunk1Offset;
        /// <summary>
        /// The length of part 2 of the HEAD chunk.
        /// </summary>
        public int HeadChunk2Length => HeadChunk3Offset - HeadChunk2Offset;
        /// <summary>
        /// The length of part 3 of the HEAD chunk.
        /// </summary>
        public int HeadChunk3Length => HeadChunkLength - HeadChunk3Offset;

        /// <summary>
        /// The audio codec.
        /// </summary>
        public BrstmCodec Codec { get; set; }
        /// <summary>
        /// This flag is set if the BRSTM loops.
        /// </summary>
        public bool Looping { get; set; }
        /// <summary>
        /// The number of channels as stated in part 1
        /// of the HEAD chunk.
        /// </summary>
        public int NumChannelsPart1 { get; set; }
        /// <summary>
        /// The sample rate of the audio.
        /// </summary>
        public int SampleRate { get; set; }
        /// <summary>
        /// The start loop point in samples.
        /// </summary>
        public int LoopStart { get; set; }
        /// <summary>
        /// The number of samples in the BRSTM.
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
        /// he number of samples per channel in the final
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
        public int SamplesPerAdpcEntry { get; set; }

        /// <summary>
        /// The type of description used for the tracks
        /// in part 2 of the HEAD chunk.
        /// </summary>
        public BrstmTrackType HeaderType { get; set; }
        /// <summary>
        /// A list of all tracks defined in the BRSTM.
        /// </summary>
        public List<AdpcmTrack> Tracks { get; set; } = new List<AdpcmTrack>();

        /// <summary>
        /// The number of channels as stated in part 1
        /// of the HEAD chunk.
        /// </summary>
        public int NumChannelsPart3 { get; set; }
        /// <summary>
        /// The ADPCM information for each channel.
        /// </summary>
        public List<BrstmChannelInfo> Channels { get; set; } = new List<BrstmChannelInfo>();

        /// <summary>
        /// The length of the ADPC chunk as stated in the
        /// ADPC chunk header.
        /// </summary>
        public int AdpcChunkLength { get; set; }
        /// <summary>
        /// The length of the seek table in the
        /// ADPC chunk.
        /// </summary>
        public int SeekTableLength { get; set; }
        /// <summary>
        /// The seek table containing PCM samples
        /// from throughout the audio stream.
        /// </summary>
        public short[][] SeekTable { get; set; }
        /// <summary>
        /// Specifies whether the seek table is full
        /// length, or a truncated table used in some
        /// games including Pokémon Battle Revolution
        /// and Mario Party 8. 
        /// </summary>
        public BrstmSeekTableType SeekTableType { get; set; }

        /// <summary>
        /// The length of the DATA chunk as stated in the
        /// DATA chunk header.
        /// </summary>
        public int DataChunkLength { get; set; }
    }

    /// <summary>
    /// Defines the ADPCM information for a single
    /// ADPCM channel.
    /// </summary>
    public class BrstmChannelInfo : AdpcmChannelInfo
    {
        /// <summary>
        /// The offset of the coefficients of the
        /// channel. Used in a BRSTM header.
        /// </summary>
        public int Offset { get; set; }
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

    /// <summary>
    /// The different audio codecs used in BRSTM files.
    /// </summary>
    public enum BrstmCodec
    {
        /// <summary>
        /// Big-endian, 8-bit PCM
        /// </summary>
        Pcm8Bit = 0,
        /// <summary>
        /// Big-endian, 16-bit PCM
        /// </summary>
        Pcm16Bit = 1,
        /// <summary>
        /// Nintendo's 4-Bit ADPCM
        /// </summary>
        Adpcm = 2
    }
}