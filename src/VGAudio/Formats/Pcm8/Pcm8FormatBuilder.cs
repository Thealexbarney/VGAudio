using System.IO;
using System.Linq;
using VGAudio.Codecs;

namespace VGAudio.Formats.Pcm8
{
    public class Pcm8FormatBuilder : AudioFormatBaseBuilder<Pcm8Format, Pcm8FormatBuilder, CodecParameters>
    {
        public byte[][] Channels { get; set; }
        public bool Signed { get; set; }
        public override int ChannelCount => Channels.Length;

        public Pcm8FormatBuilder(byte[][] channels, int sampleRate, bool signed = false)
        {
            if (channels == null || channels.Length < 1)
                throw new InvalidDataException("Channels parameter cannot be empty or null");

            Channels = channels.ToArray();
            SampleCount = Channels[0]?.Length ?? 0;
            SampleRate = sampleRate;
            Signed = signed;

            foreach (byte[] channel in Channels)
            {
                if (channel == null)
                    throw new InvalidDataException("All provided channels must be non-null");

                if (channel.Length != SampleCount)
                    throw new InvalidDataException("All channels must have the same sample count");
            }
        }

        public override Pcm8Format Build() => Signed ? new Pcm8SignedFormat(this) : new Pcm8Format(this);
    }
}
