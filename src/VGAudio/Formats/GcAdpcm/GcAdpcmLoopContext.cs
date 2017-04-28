using VGAudio.Codecs;

namespace VGAudio.Formats.GcAdpcm
{
    internal class GcAdpcmLoopContext
    {
        public short PredScale { get; }
        public short Hist1 { get; }
        public short Hist2 { get; }
        public int LoopStart { get; }
        public bool IsSelfCalculated { get; }

        public GcAdpcmLoopContext(short predScale, short hist1, short hist2, int loopStart, bool isSelfCalculated)
        {
            PredScale = predScale;
            Hist1 = hist1;
            Hist2 = hist2;
            LoopStart = loopStart;
            IsSelfCalculated = isSelfCalculated;
        }

        public GcAdpcmLoopContext(byte[] adpcm, short[] pcm, int loopStart)
        {
            PredScale = GcAdpcmDecoder.GetPredictorScale(adpcm, loopStart);
            Hist1 = loopStart < 1 ? (short) 0 : pcm[loopStart - 1];
            Hist2 = loopStart < 2 ? (short) 0 : pcm[loopStart - 2];
            LoopStart = loopStart;
            IsSelfCalculated = true;
        }
    }
}
