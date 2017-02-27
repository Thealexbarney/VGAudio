namespace DspAdpcm.Containers.Bxstm
{
    /// <summary>
    /// Defines the structure and metadata
    /// of a BCSTM or BFSTM file.
    /// </summary>
    public abstract class BCFstmStructure : BxstmStructure
    {
        /// <summary>
        /// The size of the file header.
        /// </summary>
        public int HeaderSize { get; set; }
        /// <summary>
        /// The number of sections listed in the header.
        /// </summary>
        public int HeaderSections { get; set; }
        /// <summary>
        /// The offset of the INFO chunk.
        /// </summary>
        public int InfoChunkOffset { get; set; }
        /// <summary>
        /// The size of the INFO chunk as stated in the header.
        /// </summary>
        public int InfoChunkSizeHeader { get; set; }
        /// <summary>
        /// The offset of the SEEK chunk.
        /// </summary>
        public int SeekChunkOffset { get; set; }
        /// <summary>
        /// The size of the SEEK chunk as stated in the header.
        /// </summary>
        public int SeekChunkSizeHeader { get; set; }
        /// <summary>
        /// The offset of the DATA chunk.
        /// </summary>
        public int DataChunkOffset { get; set; }
        /// <summary>
        /// The size of the DATA chunk as stated in the header.
        /// </summary>
        public int DataChunkSizeHeader { get; set; }
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
        /// The size of the INFO chunk as stated in the
        /// INFO chunk header.
        /// </summary>
        public int InfoChunkSize { get; set; }
        /// <summary>
        /// The offset of part 1 of the INFO chunk.
        /// </summary>
        public int InfoChunk1Offset { get; set; }
        /// <summary>
        /// The offset of part 2 of the INFO chunk.
        /// </summary>
        public int InfoChunk2Offset { get; set; }
        /// <summary>
        /// The offset of part 3 of the INFO chunk.
        /// </summary>
        public int InfoChunk3Offset { get; set; }

        /// <summary>
        /// The size of the SEEK chunk as stated in the
        /// SEEK chunk header.
        /// </summary>
        public int SeekChunkSize { get; set; }

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