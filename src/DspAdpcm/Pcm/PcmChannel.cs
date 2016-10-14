using System;
using System.Collections.Generic;
using static DspAdpcm.Helpers;

namespace DspAdpcm.Pcm
{
    internal class PcmChannel
    {
        public int NumSamples { get; set; }
        public short[] AudioData { get; set; }
        
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

        public override bool Equals(object obj)
        {
            var item = obj as PcmChannel;

            if (item == null)
            {
                return false;
            }

            return
                item.NumSamples == NumSamples &&
                ArraysEqual(item.AudioData, AudioData);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = NumSamples.GetHashCode();
                hashCode = (hashCode * 397) ^ AudioData.GetHashCode();
                return hashCode;
            }
        }
    }
}
