namespace DspAdpcm.Formats.GcAdpcm
{
    internal class GcAdpcmLoopContext
    {
        public short PredScale { get; set; }
        public short Hist1 { get; set; }
        public short Hist2 { get; set; }
        public bool IsSelfCalculated { get; set; }
    }
}
