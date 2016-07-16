using System.Collections.Generic;

namespace DspAdpcm.Encode.Wave
{
    public class WaveChannel : IPcmChannel
    {
        public int NumSamples { get; set; }
        private short[] AudioData { get; set; }
        private int _currentSample;
        
        public WaveChannel(short[] audioData)
        {
            SetAudioData(audioData);
        }

        public WaveChannel(int numSamples)
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
            AudioData[_currentSample++] = sample;
        }
    }
}
