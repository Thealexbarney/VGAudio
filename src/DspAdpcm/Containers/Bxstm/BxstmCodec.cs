namespace DspAdpcm.Containers.Bxstm
{
    /// <summary>
    /// The different audio codecs used in BRSTM, BCSTM, and BFSTM files.
    /// </summary>
    public enum BxstmCodec
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
