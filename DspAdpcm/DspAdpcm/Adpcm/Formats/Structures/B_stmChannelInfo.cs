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
}