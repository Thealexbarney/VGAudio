using System.Collections.Generic;
using DspAdpcm.Codecs;

namespace DspAdpcm.Formats.GcAdpcm
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
             => AddLoopContext(loopStart, predScale, hist1, hist2, false);

        private void AddLoopContext(int loopStart, short predScale, short hist1, short hist2, bool selfCalculated)
             => Contexts[loopStart] = new LoopContext
             {
                 PredScale = predScale,
                 Hist1 = hist1,
                 Hist2 = hist2,
                 IsSelfCalculated = selfCalculated
             };

        private LoopContext GetLoopContext(int loopStart, bool ensureSelfCalculated)
        {
            LoopContext context;

            if (Contexts.TryGetValue(loopStart, out context) && !(ensureSelfCalculated && !context.IsSelfCalculated))
            {
                return context;
            }

            CalculateLoopContext(loopStart);
            return Contexts[loopStart];
        }

        private void CalculateLoopContext(int loopStart)
        {
            byte ps = GcAdpcmDecoder.GetPredictorScale(Adpcm.GetAudioData(), loopStart);
            short[] hist = GcAdpcmDecoder.Decode(Adpcm, loopStart, 0, true);
            AddLoopContext(loopStart, ps, hist[1], hist[0], true);
        }

        private struct LoopContext
        {
            public short PredScale;
            public short Hist1;
            public short Hist2;
            public bool IsSelfCalculated;
        }
    }
}
