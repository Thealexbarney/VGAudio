namespace DspAdpcm.Adpcm.Formats.Structures
{
    /// <summary>
    /// Defines the ADPCM information for a single
    /// ADPCM channel.
    /// Used in BRSTM, BCSTM, and BFSTM files.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public class B_stmChannelInfo : AdpcmChannelInfo
    {
        internal B_stmChannelInfo() { }
        /// <summary>
        /// The offset of the channel information. 
        /// Used in BRSTM, BCSTM, and BFSTM headers.
        /// </summary>
        public int Offset { get; set; }
    }

    /// <summary>
    /// The different audio codecs used in BRSTM, BCSTM, and BFSTM files.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public enum B_stmCodec
    {
        /// <summary>
        /// Big-endian, 8-bit PCM.
        /// </summary>
        Pcm8Bit = 0,
        /// <summary>
        /// Big-endian, 16-bit PCM.
        /// </summary>
        Pcm16Bit = 1,
        /// <summary>
        /// Nintendo's 4-Bit ADPCM codec.
        /// </summary>
        Adpcm = 2
    }
}