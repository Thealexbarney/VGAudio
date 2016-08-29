using System;
using System.Collections.Generic;

namespace DspAdpcm.Lib.Pcm
{
    internal class PcmChannel
    {
        public int NumSamples { get; set; }
        internal short[] AudioData { get; set; }
        
        public PcmChannel(int numSamples)
        {
            AudioData = new short[numSamples];
            NumSamples = numSamples;
        }

        public PcmChannel(int numSamples, short[] audio)
        {
            if (audio.Length != numSamples)
            {
                throw new ArgumentException("Audio array length does not match the specified number of samples.");
            }
            AudioData = audio;
            NumSamples = numSamples;
        }

        public IEnumerable<short> GetAudioData() => AudioData;
    }
}
