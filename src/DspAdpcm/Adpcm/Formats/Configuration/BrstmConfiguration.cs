using DspAdpcm.Adpcm.Formats.Internal;
using DspAdpcm.Adpcm.Formats.Structures;

namespace DspAdpcm.Adpcm.Formats.Configuration
{
    /// <summary>
    /// Contains the options used to build the BRSTM file.
    /// </summary>
    public interface BrstmConfiguration : B_stmConfiguration
    {
        /// <summary>
        /// The type of track description to be used when building the 
        /// BRSTM header.
        /// Default is <see cref="BrstmTrackType.Standard"/>
        /// </summary>
        BrstmTrackType TrackType { get; }
    }

    /// <summary>
    /// Contains the options used to build the BRSTM file, including options
    /// specific to ADPCM encoding.
    /// </summary>
    public class AdpcmBrstmConfiguration : AdpcmB_stmConfiguration, BrstmConfiguration
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

    /// <summary>
    /// Contains the options used to build the BRSTM file, including options
    /// specific to PCM encoding.
    /// </summary>
    public class PcmBrstmConfiguration : PcmB_stmConfiguration, BrstmConfiguration
    {
        /// <summary>
        /// The type of track description to be used when building the 
        /// BRSTM header.
        /// Default is <see cref="BrstmTrackType.Standard"/>
        /// </summary>
        public BrstmTrackType TrackType { get; set; } = BrstmTrackType.Standard;
    }
}
