using static DspAdpcm.Encode.Adpcm.Helpers;

namespace DspAdpcm.Encode.Adpcm
{
    public class AdpcmChannel
    {
        public byte[] AudioData { get; set; }

        public int NumSamples { get; set; }

        public short[] Coefs { get; set; }
        public short Hist1 { get; } = 0;
        public short Hist2 { get; } = 0;

        public short LoopPredScale { get; set; }
        public short LoopHist1 { get; set; }
        public short LoopHist2 { get; set; }

        public AdpcmChannel(int numSamples)
        {
            NumSamples = numSamples;
            AudioData = new byte[GetBytesForAdpcmSamples(numSamples)];
        }
    }
}
