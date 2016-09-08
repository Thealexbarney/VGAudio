namespace DspAdpcm.Adpcm.Formats.Configuration
{
    /// <summary>
    /// Contains the options used to build the DSP file.
    /// </summary>
    public class DspConfiguration
    {
        /// <summary>
        /// If <c>true</c>, recalculates the loop context when building the DSP.
        /// If <c>false</c>, reuses the loop context read from an imported DSP
        /// if available.
        /// Default is <c>true</c>.
        /// </summary>
        public bool RecalculateLoopContext { get; set; } = true;

        /// <summary>
        /// If <c>true</c>, trims the output file length to the set LoopEnd.
        /// If <c>false</c> or if the <see cref="Dsp"/> does not loop,
        /// the output file is not trimmed.
        /// if available.
        /// Default is <c>true</c>.
        /// </summary>
        public bool TrimFile { get; set; } = true;

        /// <summary>
        /// When building the DSP file, the loop points and audio will
        /// be adjusted so that the start loop point is a multiple of
        /// this number. Default is 1.
        /// </summary>
        public int LoopPointAlignment { get; set; } = 1;
    }
}
