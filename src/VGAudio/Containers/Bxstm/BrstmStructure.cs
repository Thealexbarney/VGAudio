namespace VGAudio.Containers.Bxstm
{
    public class BrstmStructure : BxstmStructure
    {
        /// <summary>
        /// The offset of the HEAD block.
        /// </summary>
        public int HeadBlockOffset { get; set; }
        /// <summary>
        /// The size of the HEAD block as stated in the header.
        /// </summary>
        public int HeadBlockSize { get; set; }
        /// <summary>
        /// The offset of the ADPC block.
        /// </summary>
        public int SeekBlockOffset { get; set; }
        /// <summary>
        /// The size of the ADPC block as stated in the header.
        /// </summary>
        public int SeekBlockSize { get; set; }
        /// <summary>
        /// The offset of the DATA block.
        /// </summary>
        public int DataBlockOffset { get; set; }
        /// <summary>
        /// The size of the DATA block as stated in the header.
        /// </summary>
        public int DataBlockSize { get; set; }

        /// <summary>
        /// The size of the seek table in the file.
        /// </summary>
        public int SeekTableSize { get; set; }

        /// <summary>
        /// Specifies whether the seek table is full
        /// length, or a truncated table used in some
        /// games including Pokémon Battle Revolution
        /// and Mario Party 8. 
        /// </summary>
        public BrstmSeekTableType SeekTableType { get; set; }

        /// <summary>
        /// The type of description used for the tracks
        /// in part 2 of the HEAD block.
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
        Standard = 1,
        /// <summary>
        /// The track description used in Super Smash Bros. Brawl.
        /// It does not contain values for volume or panning.
        /// </summary>
        Short = 0
    }
}
