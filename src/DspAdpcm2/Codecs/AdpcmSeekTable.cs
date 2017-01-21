namespace DspAdpcm.Codecs
{
    internal class AdpcmSeekTable
    {
        public short[] Table { get; set; }
        public int SamplesPerEntry { get; set; }
        public bool IsSelfCalculated { get; set; }
    }
}
