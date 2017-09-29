using System.IO;
using System.Linq;
using System.Text;
using VGAudio.Containers.Hps;
using VGAudio.Formats.GcAdpcm;

namespace VGAudio.Cli.Metadata.Containers
{
    internal class Hps : MetadataReader
    {
        public override Common ToCommon(object structure)
        {
            var hps = structure as HpsStructure;
            if (hps == null) throw new InvalidDataException("Could not parse file metadata.");

            return new Common
            {
                SampleCount = hps.SampleCount,
                SampleRate = hps.SampleRate,
                ChannelCount = hps.ChannelCount,
                Format = AudioFormat.GcAdpcm,
                Looping = hps.Looping,
                LoopStart = hps.LoopStart,
                LoopEnd = hps.SampleCount
            };
        }

        public override object ReadMetadata(Stream stream) => new HpsReader().ReadMetadata(stream);

        public override void PrintSpecificMetadata(object structure, StringBuilder builder)
        {
            var hps = structure as HpsStructure;
            if (hps == null) throw new InvalidDataException("Could not parse file metadata.");

            builder.AppendLine();

            builder.AppendLine($"Max block size: 0x{hps.Channels.First().MaxBlockSize:X}");

            GcAdpcm.PrintAdpcmMetadata(hps.Channels.Cast<GcAdpcmChannelInfo>().ToList(), builder, printLoopInfo: false);
        }
    }
}
