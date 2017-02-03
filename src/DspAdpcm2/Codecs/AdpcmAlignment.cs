namespace DspAdpcm.Codecs
{
    internal class AdpcmAlignment
    {
        public byte[] AudioData { get; set; }

        public int Alignment { get; set; }
        public int LoopStartAligned { get; set; }
        public int LoopEndAligned { get; set; }
        public int SampleCountAligned { get; set; }
    }
}
