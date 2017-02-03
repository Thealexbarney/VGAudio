using System;

namespace DspAdpcm.Codecs
{
    public class AdpcmChannel
    {
        public byte[] AudioData { get; }
        public int SampleCount { get; }

        public short Gain { get; set; }
        public short[] Coefs { get; set; }
        public short PredScale => AudioData[0];
        public short Hist1 { get; set; }
        public short Hist2 { get; set; }

        internal AdpcmSeekTable SeekTable { get; set; }
        internal AdpcmLoopContext LoopContext { get; set; }
        internal AdpcmAlignment Alignment { get; set; }

        public AdpcmChannel(int sampleCount)
        {
            SampleCount = sampleCount;
            AudioData = new byte[Adpcm.SampleCountToByteCount(sampleCount)];
        }

        public AdpcmChannel(int sampleCount, byte[] audio)
        {
            if (audio.Length < Adpcm.SampleCountToByteCount(sampleCount))
            {
                throw new ArgumentException("Audio array length is too short for the specified number of samples.");
            }

            SampleCount = sampleCount;
            AudioData = audio;
        }
    }
}
