using VGAudio.Codecs.GcAdpcm;

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
            PredScale = GetPredScale(adpcm, loopStart);
            Hist1 = GetHist1(pcm, loopStart);
            Hist2 = GetHist2(pcm, loopStart);
            LoopStart = loopStart;
            IsSelfCalculated = true;
        }

        public static byte GetPredScale(byte[] adpcm, int sampleNum) => GcAdpcmDecoder.GetPredictorScale(adpcm, sampleNum);
        public static short GetHist1(short[] pcm, int sampleNum) => sampleNum < 1 ? (short)0 : pcm?[sampleNum - 1] ?? 0;
        public static short GetHist2(short[] pcm, int sampleNum) => sampleNum < 2 ? (short)0 : pcm?[sampleNum - 2] ?? 0;
    }
}
