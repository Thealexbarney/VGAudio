namespace DspAdpcm.Codecs
{
    internal class AdpcmChannel
    {
        public byte[] AudioData { get; set; }

        public short Gain { get; set; }
        public short[] Coefs { get; set; }
        public short PredScale => AudioData[0];
        public short Hist1 { get; set; }
        public short Hist2 { get; set; }

        public AdpcmSeekTable SeekTable { get; set; }
        public AdpcmLoopContext LoopContext { get; set; }
        public AdpcmAlignment Alignment { get; set; }
    }
}
