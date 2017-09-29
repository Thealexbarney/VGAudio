namespace VGAudio.Formats.GcAdpcm
{
    /// <summary>
    /// Defines the ADPCM information for a single
    /// GC ADPCM channel.
    /// </summary>
    public class GcAdpcmChannelInfo
    {
        /// <summary>
        /// The ADPCM coefficients of the channel.
        /// </summary>
        public short[] Coefs { get; set; }

        /// <summary>
        /// The gain level for the channel.
        /// </summary>
        public short Gain { get; set; }

        public GcAdpcmContext Start { get; set; }
        public GcAdpcmContext Loop { get; set; }
    }
}
