using System.IO;
using System.Text;
using VGAudio.Containers.McAdpcm;

namespace VGAudio.Cli.Metadata.Containers
{
    internal class McAdpcm : MetadataReader
    {
        public override Common ToCommon(object structure)
        {
            if (!(structure is McAdpcmStructure McAdpcm)) throw new InvalidDataException("Could not parse file metadata.");

            return new Common
            {
                SampleCount = McAdpcm.SampleCount,
                SampleRate = McAdpcm.SampleRate,
                ChannelCount = McAdpcm.ChannelCount,
                Format = AudioFormat.McAdpcm,
                Looping = McAdpcm.Looping,
                LoopStart = McAdpcm.LoopStart,
                LoopEnd = McAdpcm.LoopEnd
            };
        }

        public override object ReadMetadata(Stream stream) => new McAdpcmReader().ReadMetadata(stream);

        public override void PrintSpecificMetadata(object structure, StringBuilder builder)
        {
            if (!(structure is McAdpcmStructure McAdpcm)) throw new InvalidDataException("Could not parse file metadata.");

            builder.AppendLine();

            builder.AppendLine($"Nibble Count: {McAdpcm.NibbleCount}");
            builder.AppendLine($"Start Address: 0x{McAdpcm.StartAddress:X8}");
            builder.AppendLine($"End Address: 0x{McAdpcm.EndAddress:X8}");
            builder.AppendLine($"Current Address: 0x{McAdpcm.CurrentAddress:X8}");

            GcAdpcm.PrintAdpcmMetadata(McAdpcm.Channels, builder);
        }
    }
}
