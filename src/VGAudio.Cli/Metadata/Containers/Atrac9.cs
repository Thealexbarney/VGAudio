using System.IO;
using System.Text;
using VGAudio.Codecs.Atrac9;
using VGAudio.Containers.At9;

namespace VGAudio.Cli.Metadata.Containers
{
    internal class Atrac9 : MetadataReader
    {
        public override object ReadMetadata(Stream stream)
        {
            return new At9Reader().ReadMetadata(stream);
        }

        public override Common ToCommon(object metadata)
        {
            var at9Structure = metadata as At9Structure;
            Atrac9Config config = at9Structure?.Config;
            if (at9Structure == null || config == null) throw new InvalidDataException("Could not parse file metadata.");

            return new Common
            {
                SampleCount = at9Structure.SampleCount,
                SampleRate = config.SampleRate,
                ChannelCount = config.ChannelCount,
                Format = AudioFormat.Atrac9,
                Looping = at9Structure.Looping,
                LoopStart = at9Structure.LoopStart,
                LoopEnd = at9Structure.LoopEnd
            };
        }

        public override void PrintSpecificMetadata(object metadata, StringBuilder builder)
        {
            var at9Structure = metadata as At9Structure;
            Atrac9Config config = at9Structure?.Config;
            if (at9Structure == null || config == null) throw new InvalidDataException("Could not parse file metadata.");

            builder.AppendLine();

            builder.AppendLine($"ATRAC9 version: {at9Structure.Version}");
            builder.AppendLine($"Superframe count: {at9Structure.SuperframeCount}");
            builder.AppendLine($"Superframe size: {config.SuperframeBytes} bytes");
            builder.AppendLine($"Frames per superframe: {config.FramesPerSuperframe}");
            builder.AppendLine($"Encoder delay: {at9Structure.EncoderDelay} samples");
        }
    }
}
