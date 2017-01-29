using static DspAdpcm.Utilities.Helpers;

namespace DspAdpcm.Codecs
{
    internal class Pcm16Channel
    {
        public short[] AudioData { get; }
        public int NumSamples => AudioData.Length;

        public Pcm16Channel(int numSamples)
        {
            AudioData = new short[numSamples];
        }

        public Pcm16Channel(short[] audio)
        {
            AudioData = audio;
        }

        public override bool Equals(object obj)
        {
            var item = obj as Pcm16Channel;

            return item != null && ArraysEqual(item.AudioData, AudioData);
        }

        public override int GetHashCode()
        {
            return AudioData.GetHashCode();
        }
    }
}
