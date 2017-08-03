using VGAudio.Codecs.GcAdpcm;

namespace VGAudio.Formats.GcAdpcm
{
    internal class GcAdpcmLoopContext : GcAdpcmContext
    {
        public int LoopStart { get; }
        public bool IsSelfCalculated { get; }

        public GcAdpcmLoopContext(short predScale, short hist1, short hist2, int loopStart, bool isSelfCalculated)
            : base(predScale, hist1, hist2)
        {
            LoopStart = loopStart;
            IsSelfCalculated = isSelfCalculated;
        }

        public GcAdpcmLoopContext(byte[] adpcm, short[] pcm, int loopStart)
            : base(GetPredScale(adpcm, loopStart), GetHist1(pcm, loopStart), GetHist2(pcm, loopStart))
        {
            LoopStart = loopStart;
            IsSelfCalculated = true;
        }

        public static byte GetPredScale(byte[] adpcm, int sampleNum) => GcAdpcmDecoder.GetPredictorScale(adpcm, sampleNum);
        public static short GetHist1(short[] pcm, int sampleNum) => sampleNum < 1 ? (short)0 : pcm?[sampleNum - 1] ?? 0;
        public static short GetHist2(short[] pcm, int sampleNum) => sampleNum < 2 ? (short)0 : pcm?[sampleNum - 2] ?? 0;
    }
}
