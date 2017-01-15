using System;
using System.Collections.Generic;
using System.IO;
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

        public PcmChannel(int numSamples, byte[] audio, Endianness endianness)
        {
            short[] samples = new short[audio.Length / sizeof(short)];
            if (samples.Length != numSamples)
            {
                throw new ArgumentException("Audio array length does not match the specified number of samples.");
            }
            using (MemoryStream stream = new MemoryStream(audio, false))
            {
                var reader = GetBinaryReader(stream, endianness);
                for (int i=0; i<samples.Length; i++)
                {
                    samples[i] = reader.ReadInt16();
                }
            }
            AudioData = samples;
            NumSamples = numSamples;
        }

        public byte[] GetAudioData(Endianness endianness)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                var writer = GetBinaryWriter(stream, endianness);
                foreach (short s in AudioData) writer.Write(s);
                return stream.ToArray();
            }
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
