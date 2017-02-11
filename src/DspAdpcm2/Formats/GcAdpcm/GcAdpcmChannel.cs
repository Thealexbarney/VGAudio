using System;
using System.Collections.Generic;

namespace DspAdpcm.Formats.GcAdpcm
{
    public class GcAdpcmChannel
    {
        public byte[] AudioData { get; }
        public int SampleCount { get; }

        public short Gain { get; set; }
        public short[] Coefs { get; set; }
        public short PredScale => AudioData[0];
        public short Hist1 { get; set; }
        public short Hist2 { get; set; }

        public short LoopPredScale => LoopContext?.PredScale ?? 0;
        public short LoopHist1 => LoopContext?.Hist1 ?? 0;
        public short LoopHist2 => LoopContext?.Hist2 ?? 0;

        internal List<GcAdpcmSeekTable> SeekTable { get; set; } = new List<GcAdpcmSeekTable>();
        internal GcAdpcmLoopContext LoopContext { get; set; }
        internal GcAdpcmAlignment Alignment { get; set; }

        public GcAdpcmChannel(int sampleCount)
        {
            SampleCount = sampleCount;
            AudioData = new byte[GcAdpcmHelpers.SampleCountToByteCount(sampleCount)];
        }

        public GcAdpcmChannel(int sampleCount, byte[] audio)
        {
            if (audio.Length < GcAdpcmHelpers.SampleCountToByteCount(sampleCount))
            {
                throw new ArgumentException("Audio array length is too short for the specified number of samples.");
            }

            SampleCount = sampleCount;
            AudioData = audio;
        }

        internal void SetLoopContext(short predScale, short hist1, short hist2)
        {
            LoopContext = new GcAdpcmLoopContext()
            {
                PredScale = predScale,
                Hist1 = hist1,
                Hist2 = hist2
            };
        }
    }
}
