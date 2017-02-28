namespace DspAdpcm.Formats.GcAdpcm
{
    /// <summary>
    /// Defines the ADPCM information for a single
    /// GC ADPCM channel.
    /// </summary>
    public class GcAdpcmChannelInfo
    {
        internal GcAdpcmChannelInfo() { }

        /// <summary>
        /// The ADPCM coefficients of the channel.
        /// </summary>
        public short[] Coefs { get; set; }

        /// <summary>
        /// The gain level for the channel.
        /// </summary>
        public short Gain { get; set; }
        /// <summary>
        /// The predictor and scale for the first
        /// frame of the channel.
        /// </summary>
        public short PredScale { get; set; }
        /// <summary>
        /// The first PCM history sample for the stream.
        /// (Initial sample - 1).
        /// </summary>
        public short Hist1 { get; set; }
        /// <summary>
        /// The second PCM history sample for the stream.
        /// (Initial sample - 2).
        /// </summary>
        public short Hist2 { get; set; }

        /// <summary>
        /// The predictor and scale for the loop
        /// point frame.
        /// </summary>
        public short LoopPredScale { get; set; }
        /// <summary>
        /// The first PCM history sample for the start
        /// loop point. (loop point - 1).
        /// </summary>
        public short LoopHist1 { get; set; }
        /// <summary>
        /// The second PCM history sample for the start
        /// loop point. (loop point - 2).
        /// </summary>
        public short LoopHist2 { get; set; }
    }
}
