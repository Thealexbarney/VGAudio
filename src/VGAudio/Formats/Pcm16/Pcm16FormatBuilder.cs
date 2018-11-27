using System.IO;
using System.Linq;
using VGAudio.Codecs;

namespace VGAudio.Formats.Pcm16
{
    public class Pcm16FormatBuilder : AudioFormatBaseBuilder<Pcm16Format, Pcm16FormatBuilder, CodecParameters>
    {
        public short[][] Channels { get; set; }
        public override int ChannelCount => Channels.Length;

        public Pcm16FormatBuilder(short[][] channels, int sampleRate)
        {
            if (channels == null || channels.Length < 1)
                throw new InvalidDataException("Channels parameter cannot be empty or null");

            Channels = channels.ToArray();
            SampleCount = Channels[0]?.Length ?? 0;
            SampleRate = sampleRate;

            foreach (short[] channel in Channels)
            {
                if (channel == null)
                    throw new InvalidDataException("All provided channels must be non-null");

                if (channel.Length != SampleCount)
                    throw new InvalidDataException("All channels must have the same sample count");
            }
        }

        public override Pcm16Format Build() => new Pcm16Format(this);
    }
}
