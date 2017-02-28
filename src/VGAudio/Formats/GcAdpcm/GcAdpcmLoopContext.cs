using System.Collections.Generic;
using VGAudio.Codecs;

namespace VGAudio.Formats.GcAdpcm
{
    internal class GcAdpcmLoopContext
    {
        private Dictionary<int, LoopContext> Contexts { get; } = new Dictionary<int, LoopContext>();

        private GcAdpcmChannel Adpcm { get; }
        public short PredScale(int loopStart, bool ensureSelfCalculated) => GetLoopContext(loopStart, ensureSelfCalculated).PredScale;
        public short Hist1(int loopStart, bool ensureSelfCalculated) => GetLoopContext(loopStart, ensureSelfCalculated).Hist1;
        public short Hist2(int loopStart, bool ensureSelfCalculated) => GetLoopContext(loopStart, ensureSelfCalculated).Hist2;

        public GcAdpcmLoopContext(GcAdpcmChannel adpcmParent)
        {
            Adpcm = adpcmParent;
        }

        public void AddLoopContext(int loopStart, short predScale, short hist1, short hist2)
             => Contexts[loopStart] = new LoopContext(predScale, hist1, hist2, false);

        private LoopContext GetLoopContext(int loopStart, bool ensureSelfCalculated)
        {
            LoopContext context;

            if (Contexts.TryGetValue(loopStart, out context) && !(ensureSelfCalculated && !context.IsSelfCalculated))
            {
                return context;
            }

            CreateLoopContext(loopStart);
            return Contexts[loopStart];
        }

        public void CreateLoopContext(int loopStart)
        {
            byte ps = GcAdpcmDecoder.GetPredictorScale(Adpcm.GetAudioData(), loopStart);
            short[] hist = GcAdpcmDecoder.Decode(Adpcm, loopStart, 0, true);
            Contexts[loopStart] = new LoopContext(ps, hist[1], hist[0], true);
        }

        private class LoopContext
        {
            public LoopContext(short predScale, short hist1, short hist2, bool isSelfCalculated)
            {
                PredScale = predScale;
                Hist1 = hist1;
                Hist2 = hist2;
                IsSelfCalculated = isSelfCalculated;
            }

            public readonly short PredScale;
            public readonly short Hist1;
            public readonly short Hist2;
            public readonly bool IsSelfCalculated;
        }
    }
}
