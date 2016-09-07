namespace DspAdpcm.Adpcm.Formats.Internal
{
    /// <summary>
    /// Defines the structure and metadata
    /// of a BCSTM file.
    /// </summary>
    public class BCFstmStructure : B_stmStructure
    {
        internal BCFstmStructure() { }
        /// <summary>
        /// The length of the entire file.
        /// </summary>
        public int FileLength { get; set; }
        /// <summary>
        /// The version listed in the header.
        /// </summary>
        public int Version { get; set; }
        /// <summary>
        /// The length of the CSTM header.
        /// </summary>
        public int HeaderLength { get; set; }
        /// <summary>
        /// The number of sections listed in the header.
        /// </summary>
        public int CstmHeaderSections { get; set; }
        /// <summary>
        /// The offset of the INFO chunk.
        /// </summary>
        public int InfoChunkOffset { get; set; }
        /// <summary>
        /// The length of the INFO chunk as stated in the header.
        /// </summary>
        public int InfoChunkLengthHeader { get; set; }
        /// <summary>
        /// The offset of the SEEK chunk.
        /// </summary>
        public int SeekChunkOffset { get; set; }
        /// <summary>
        /// The length of the SEEK chunk as stated in the header.
        /// </summary>
        public int SeekChunkLengthHeader { get; set; }
        /// <summary>
        /// The offset of the DATA chunk.
        /// </summary>
        public int DataChunkOffset { get; set; }
        /// <summary>
        /// The length of the DATA chunk as stated in the header.
        /// </summary>
        public int DataChunkLengthHeader { get; set; }

        /// <summary>
        /// The length of the INFO chunk as stated in the
        /// INFO chunk header.
        /// </summary>
        public int InfoChunkLength { get; set; }
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
        /// The length of the SEEK chunk as stated in the
        /// SEEK chunk header.
        /// </summary>
        public int SeekChunkLength { get; set; }

        /// <summary>
        /// The length of the DATA chunk as stated in the
        /// DATA chunk header.
        /// </summary>
        public int DataChunkLength { get; set; }

        /// <summary>
        /// The audio codec.
        /// </summary>
        public BcstmCodec Codec { get; set; }

        /// <summary>
        /// Specifies whether the BCSTM includes an extra chunk in the header
        /// after the stream info and before the track offset table.
        /// The purpose of this chunk is unknown.
        /// </summary>
        public bool InfoPart1Extra { get; set; }

        /// <summary>
        /// Specifies whether the BCSTM lists the tracks
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

    /// <summary>
    /// The different audio codecs used in BCSTM files.
    /// </summary>
    public enum BcstmCodec
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
