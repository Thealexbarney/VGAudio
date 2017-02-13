namespace DspAdpcm.Formats
{
    /// <summary>
    /// A 16-bit PCM audio stream.
    /// The stream can contain any number of individual channels.
    /// </summary>
    public class Pcm16Format : AudioFormatBase<Pcm16Format>
    {
        public short[][] Channels { get; }

        public Pcm16Format(int sampleCount, int sampleRate, short[][] channels)
            : base(sampleCount, sampleRate, channels.Length)
        {
            Channels = channels;
        }

        public Pcm16Format() : base(0, 0, 0)
        {
            Channels = new short[0][];
        }

        public override Pcm16Format ToPcm16()
        {
            return new Pcm16Format(SampleCount, SampleRate, Channels);
        }

        public override Pcm16Format EncodeFromPcm16(Pcm16Format pcm16)
        {
            return new Pcm16Format(SampleCount, SampleRate, Channels);
        }
    }
}
