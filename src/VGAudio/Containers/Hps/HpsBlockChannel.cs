namespace VGAudio.Containers.Hps
{
    public class HpsBlockChannel
    {
        public short PredScale { get; set; }
        public short Hist1 { get; set; }
        public short Hist2 { get; set; }
        public byte[] AudioData { get; set; }
    }
}
