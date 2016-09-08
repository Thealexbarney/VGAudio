using DspAdpcm.Adpcm.Formats.Internal;

namespace DspAdpcm.Adpcm.Formats.Configuration
{
    /// <summary>
    /// Contains the options used to build the BCSTM file.
    /// </summary>
    public class BcstmConfiguration
    {
        internal BCFstmConfiguration Configuration { get; set; } = new BCFstmConfiguration();

        /// <summary>
        /// <inheritdoc cref="B_stmConfiguration.RecalculateSeekTable"/>
        /// </summary>
        public bool RecalculateSeekTable
        {
            get { return Configuration.RecalculateSeekTable; }
            set { Configuration.RecalculateSeekTable = value; }
        }

        /// <summary>
        /// <inheritdoc cref="B_stmConfiguration.RecalculateLoopContext"/>
        /// </summary>
        public bool RecalculateLoopContext
        {
            get { return Configuration.RecalculateLoopContext; }
            set { Configuration.RecalculateLoopContext = value; }
        }

        /// <summary>
        /// <inheritdoc cref="B_stmConfiguration.SamplesPerInterleave"/>
        /// </summary>
        public int SamplesPerInterleave
        {
            get { return Configuration.SamplesPerInterleave; }
            set { Configuration.SamplesPerInterleave = value; }
        }

        /// <summary>
        /// <inheritdoc cref="B_stmConfiguration.SamplesPerSeekTableEntry"/>
        /// </summary>
        public int SamplesPerSeekTableEntry
        {
            get { return Configuration.SamplesPerSeekTableEntry; }
            set { Configuration.SamplesPerSeekTableEntry = value; }
        }

        /// <summary>
        /// <inheritdoc cref="B_stmConfiguration.LoopPointAlignment"/>
        /// </summary>
        public int LoopPointAlignment
        {
            get { return Configuration.LoopPointAlignment; }
            set { Configuration.LoopPointAlignment = value; }
        }

        /// <summary>
        /// If <c>true</c>, include track information in the BCSTM
        /// header. Default is <c>true</c>.
        /// </summary>
        public bool IncludeTrackInformation
        {
            get { return Configuration.IncludeTrackInformation; }
            set { Configuration.IncludeTrackInformation = value; }
        }

        /// <summary>
        /// If <c>true</c>, include an extra chunk in the header
        /// after the stream info and before the track offset table.
        /// The purpose of this chunk is unknown.
        /// Default is <c>false</c>.
        /// </summary>
        public bool InfoPart1Extra
        {
            get { return Configuration.InfoPart1Extra; }
            set { Configuration.InfoPart1Extra = value; }
        }
    }
}
