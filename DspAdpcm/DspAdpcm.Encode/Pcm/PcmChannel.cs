using System.Collections.Generic;

namespace DspAdpcm.Encode.Pcm
{
    public class PcmChannel
    {
        public int NumSamples { get; set; }
        internal short[] AudioData { get; set; }
        private int CurrentSample { get; set; }
        
        public PcmChannel(short[] audioData)
        {
            SetAudioData(audioData);
        }

        public PcmChannel(int numSamples)
        {
            AudioData = new short[numSamples];
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
