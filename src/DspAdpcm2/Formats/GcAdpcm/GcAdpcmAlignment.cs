using System.Collections.Generic;

namespace DspAdpcm.Formats.GcAdpcm
{
    public class GcAdpcmAlignment
    {
        private Dictionary<Loops, Alignment> Contexts { get; } = new Dictionary<Loops, Alignment>();

        private class Alignment
        {
            public byte[] AudioData { get; set; }

            public int AlignmentMultiple { get; set; }
            public int LoopStartAligned { get; set; }
            public int LoopEndAligned { get; set; }
            public int SampleCountAligned { get; set; }
        }

        private struct Loops
        {
            public int LoopStart;
            public int LoopEnd;
        }
    }
}
