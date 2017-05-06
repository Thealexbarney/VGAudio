namespace VGAudio.TestsLong.GcAdpcm
{
    public class Result
    {
        public string Filename { get; set; }
        public int Channel { get; set; }

        public bool Equal { get; set; }
        public bool RanFineComparison { get; set; }
        public bool CoefsEqual { get; set; }

        public short[] CoefsA { get; set; }
        public short[] CoefsB { get; set; }

        public int Frame { get; set; }
        public int FrameSample { get; set; }
        public int Sample { get; set; }

        public short[] PcmIn { get; set; }
        public short[] PcmOutA { get; set; }
        public short[] PcmOutB { get; set; }
        public byte[] AdpcmOutA { get; set; }
        public byte[] AdpcmOutB { get; set; }
    }
}