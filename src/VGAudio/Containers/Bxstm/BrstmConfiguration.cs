namespace VGAudio.Containers.Bxstm
{
    /// <summary>
    /// Contains the options used to build BRSTM files.
    /// </summary>
    public class BrstmConfiguration : BxstmConfiguration
    {
        /// <summary>
        /// The type of track description to be used when building the 
        /// BRSTM header.
        /// Default is <see cref="BrstmTrackType.Standard"/>
        /// </summary>
        public BrstmTrackType TrackType { get; set; } = BrstmTrackType.Standard;

        /// <summary>
        /// The type of seek table to use when building the BRSTM
        /// ADPC chunk.
        /// Default is <see cref="BrstmSeekTableType.Standard"/>
        /// </summary>
        public BrstmSeekTableType SeekTableType { get; set; } = BrstmSeekTableType.Standard;
    }
}