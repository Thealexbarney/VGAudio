namespace VGAudio.Formats
{
    public class Pcm8SignedFormat : Pcm8Format
    {
        public override bool Signed { get; } = true;
        public Pcm8SignedFormat() { }
        public Pcm8SignedFormat(byte[][] channels, int sampleRate) : base(new Builder(channels, sampleRate)) { }
        internal Pcm8SignedFormat(Builder b) : base(b) { }
    }
}
