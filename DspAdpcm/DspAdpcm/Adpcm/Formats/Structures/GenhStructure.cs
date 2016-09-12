using System;
using System.Collections.Generic;

namespace DspAdpcm.Adpcm.Formats.Structures
{
    public class GenhStructure
    {
        public int NumChannels { get; set; }
        public int Interleave { get; set; }
        public int SampleRate { get; set; }
        public int LoopStart { get; set; }
        public int LoopEnd { get; set; }
        public int Codec { get; set; }
        public int AudioDataOffset { get; set; }
        public int HeaderSize { get; set; }
        public int[] Coefs { get; set; } = new int[2];
        public int[] CoefsSplit { get; set; } = new int[2];
        public int InterleaveType { get; set; }
        public GenhCoefType CoefType { get; set; }
        public IList<AdpcmChannelInfo> Channels { get; } = new List<AdpcmChannelInfo>();

        public int NumSamples => LoopEnd;
        public bool Looping => LoopStart != -1;
        internal byte[][] AudioData { get; set; }
    }

    [Flags]
    public enum GenhCoefType
    {
        Split = 1,
        LittleEndian = 2
    }
}
