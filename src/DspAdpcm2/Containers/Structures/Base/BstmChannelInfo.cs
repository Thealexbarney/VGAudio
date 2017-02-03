using DspAdpcm.Codecs;

namespace DspAdpcm.Containers.Structures.Base
{
    /// <summary>
    /// Defines the ADPCM information for a single
    /// ADPCM channel.
    /// Used in BRSTM, BCSTM, and BFSTM files.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public class BstmChannelInfo : AdpcmChannelInfo
    {
        internal BstmChannelInfo() { }
        /// <summary>
        /// The offset of the channel information. 
        /// Used in BRSTM, BCSTM, and BFSTM headers.
        /// </summary>
        public int Offset { get; set; }
    }
}
