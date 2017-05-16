namespace VGAudio.Containers.Bxstm
{
    /// <summary>
    /// Defines the structure and metadata
    /// of a BCSTM or BFSTM file.
    /// </summary>
    public abstract class BCFstmStructure : BxstmStructure
    {
        /// <summary>
        /// The offset of the REGN chunk.
        /// </summary>
        public int RegnChunkOffset { get; set; }
        /// <summary>
        /// The size of the REGN chunk as stated in the header.
        /// </summary>
        public int RegnChunkSizeHeader { get; set; }
        /// <summary>
        /// The offset of the PDAT chunk.
        /// </summary>
        public int PdatChunkOffset { get; set; }
        /// <summary>
        /// The size of the PDAT chunk as stated in the header.
        /// </summary>
        public int PdatChunkSizeHeader { get; set; }

        /// <summary>
        /// The size of the REGN chunk as stated in the
        /// REGN chunk header.
        /// </summary>
        public int RegnChunkSize { get; set; }

        /// <summary>
        /// The REGN chunk.
        /// </summary>
        public RegnChunk Regn { get; set; }

        /// <summary>
        /// The number of audio sections in the file.
        /// </summary>
        public int SectionCount { get; set; }

        /// <summary>
        /// Specifies whether the file includes an extra chunk in the header
        /// after the stream info and before the track offset table.
        /// The purpose of this chunk is unknown.
        /// </summary>
        public bool InfoPart1Extra { get; set; }

        /// <summary>
        /// Specifies whether the file lists the tracks
        /// included in it.
        /// </summary>
        public bool IncludeTracks { get; set; }
        /// <summary>
        /// The start loop point before alignment.
        /// </summary>
        public int LoopStartUnaligned { get; set; }
        /// <summary>
        /// The end loop point before alignment.
        /// </summary>
        public int LoopEndUnaligned { get; set; }
    }
}