using System.Collections.Generic;
using static DspAdpcm.Encode.Helpers;

namespace DspAdpcm.Encode.Adpcm
{
    public class AdpcmChannel
    {
        public byte[] AudioByteArray { get; set; }

        public IEnumerable<byte> AudioData => AudioByteArray;

        public int NumSamples => AudioByteArray.Length;

        public short Gain { get; set; }
        public short[] Coefs { get; set; }
        public short Hist1 { get; set; } = 0;
        public short Hist2 { get; set; } = 0;

        public short LoopPredScale { get; set; }
        public short LoopHist1 { get; set; }
        public short LoopHist2 { get; set; }

        public short[] SeekTable { get; set; } = null;
        public int SamplesPerSeekTableEntry { get; set; }

        public AdpcmChannel(int numSamples)
        {
            AudioByteArray = new byte[GetBytesForAdpcmSamples(numSamples)];
        }

        public void SetLoopContext(short loopPredScale, short loopHist1, short loopHist2)
        {
            LoopPredScale = loopPredScale;
            LoopHist1 = loopHist1;
            LoopHist2 = loopHist2;
        }
    }
}
