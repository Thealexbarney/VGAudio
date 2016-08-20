using System;
using System.Collections.Generic;

namespace DspAdpcm.Lib.Pcm
{
    internal class PcmChannel
    {
        public int NumSamples { get; set; }
        internal short[] AudioData { get; set; }
        private int CurrentSample { get; set; }
        
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

        public void SetAudioData(short[] audio)
        {
            AudioData = audio;
            NumSamples = audio.Length;
        }

        public void AddSample(short sample)
        {
            if (CurrentSample >= NumSamples) return;
            AudioData[CurrentSample++] = sample;
        }
    }
}
