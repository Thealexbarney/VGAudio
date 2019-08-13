using System.IO;
using System.Text;
using VGAudio.Containers.McAdpcm;

namespace VGAudio.Cli.Metadata.Containers
{
    internal class McAdpcm : MetadataReader
    {
        public override Common ToCommon(object structure)
        {
            if (!(structure is McAdpcmStructure mcAdpcm)) throw new InvalidDataException("Could not parse file metadata.");

            return new Common
            {
                SampleCount = mcAdpcm.SampleCount,
                SampleRate = mcAdpcm.SampleRate,
                ChannelCount = mcAdpcm.ChannelCount,
                Format = AudioFormat.McAdpcm,
                Looping = mcAdpcm.Looping,
                LoopStart = mcAdpcm.LoopStart,
                LoopEnd = mcAdpcm.LoopEnd
            };
        }

        public override object ReadMetadata(Stream stream) => new McAdpcmReader().ReadMetadata(stream);

        public override void PrintSpecificMetadata(object structure, StringBuilder builder)
        {
            if (!(structure is McAdpcmStructure mcAdpcm)) throw new InvalidDataException("Could not parse file metadata.");

            builder.AppendLine();

            builder.AppendLine($"Nibble Count: {mcAdpcm.NibbleCount}");
            builder.AppendLine($"Start Address: 0x{mcAdpcm.StartAddress:X8}");
            builder.AppendLine($"End Address: 0x{mcAdpcm.EndAddress:X8}");
            builder.AppendLine($"Current Address: 0x{mcAdpcm.CurrentAddress:X8}");

            GcAdpcm.PrintAdpcmMetadata(mcAdpcm.Channels, builder);
        }
    }
}
