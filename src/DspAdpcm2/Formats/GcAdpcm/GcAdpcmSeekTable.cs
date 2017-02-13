namespace DspAdpcm.Formats.GcAdpcm
{
    public class GcAdpcmSeekTable
    {
        public short[] Table { get; set; }
        public int SamplesPerEntry { get; set; }
        public bool IsSelfCalculated { get; set; }
    }
}
