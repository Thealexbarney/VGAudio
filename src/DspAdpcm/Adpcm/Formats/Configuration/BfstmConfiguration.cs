using DspAdpcm.Adpcm.Formats.Internal;

namespace DspAdpcm.Adpcm.Formats.Configuration
{
    /// <summary>
    /// Contains the options used to build the BFSTM file.
    /// </summary>
    public class BfstmConfiguration
    {
        internal BCFstmConfiguration Configuration { get; set; } = new BCFstmConfiguration()
        {
            IncludeTrackInformation = false,
            InfoPart1Extra = true
        };

        /// <summary>
        /// <inheritdoc cref="AdpcmB_stmConfiguration.RecalculateSeekTable"/>
        /// </summary>
        public bool RecalculateSeekTable
        {
            get { return Configuration.RecalculateSeekTable; }
            set { Configuration.RecalculateSeekTable = value; }
        }

        /// <summary>
        /// <inheritdoc cref="AdpcmB_stmConfiguration.RecalculateLoopContext"/>
        /// </summary>
        public bool RecalculateLoopContext
        {
            get { return Configuration.RecalculateLoopContext; }
            set { Configuration.RecalculateLoopContext = value; }
        }

        /// <summary>
        /// <inheritdoc cref="AdpcmB_stmConfiguration.SamplesPerInterleave"/>
        /// </summary>
        public int SamplesPerInterleave
        {
            get { return Configuration.SamplesPerInterleave; }
            set { Configuration.SamplesPerInterleave = value; }
        }

        /// <summary>
        /// <inheritdoc cref="AdpcmB_stmConfiguration.SamplesPerSeekTableEntry"/>
        /// </summary>
        public int SamplesPerSeekTableEntry
        {
            get { return Configuration.SamplesPerSeekTableEntry; }
            set { Configuration.SamplesPerSeekTableEntry = value; }
        }

        /// <summary>
        /// <inheritdoc cref="AdpcmB_stmConfiguration.LoopPointAlignment"/>
        /// </summary>
        public int LoopPointAlignment
        {
            get { return Configuration.LoopPointAlignment; }
            set { Configuration.LoopPointAlignment = value; }
        }

        /// <summary>
        /// If <c>true</c>, include the loop points, before alignment,
        /// in the header of the BFSTM.
        /// </summary>
        public bool IncludeUnalignedLoopPoints
        {
            get { return Configuration.IncludeUnalignedLoopPoints; }
            set { Configuration.IncludeUnalignedLoopPoints = value; }
        }
    }
}
