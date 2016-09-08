using DspAdpcm.Adpcm.Formats.Internal;

namespace DspAdpcm.Adpcm.Formats.Structures
{
    /// <summary>
    /// Defines the structure and metadata
    /// of a BRSTM file.
    /// </summary>
    public class BrstmStructure : B_stmStructure
    {
        internal BrstmStructure() { }

        /// <summary>
        /// The length of the entire BRSTM file.
        /// </summary>
        public int FileLength { get; set; }
        /// <summary>
        /// The version listed in the RSTM header.
        /// </summary>
        public int Version { get; set; }
        /// <summary>
        /// The length of the RSTM header.
        /// </summary>
        public int RstmHeaderLength { get; set; }
        /// <summary>
        /// The number of sections listed in the RSTM header.
        /// </summary>
        public int RstmHeaderSections { get; set; }
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
        /// The length of the ADPC chunk as stated in the
        /// ADPC chunk header.
        /// </summary>
        public int AdpcChunkLength { get; set; }
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

        /// <summary>
        /// The audio codec.
        /// </summary>
        public B_stmCodec Codec { get; set; }

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