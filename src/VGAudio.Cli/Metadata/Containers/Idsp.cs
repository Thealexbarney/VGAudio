using System.IO;
using System.Linq;
using System.Text;
using VGAudio.Containers.Idsp;
using VGAudio.Formats.GcAdpcm;

namespace VGAudio.Cli.Metadata.Containers
{
    internal class Idsp : MetadataReader
    {
        public override Common ToCommon(object structure)
        {
            var idsp = structure as IdspStructure;
            if (idsp == null) throw new InvalidDataException("Could not parse file metadata.");

            return new Common
            {
                SampleCount = idsp.SampleCount,
                SampleRate = idsp.SampleRate,
                ChannelCount = idsp.ChannelCount,
                Format = AudioFormat.GcAdpcm,
                Looping = idsp.Looping,
                LoopStart = idsp.LoopStart,
                LoopEnd = idsp.LoopEnd
            };
        }

        public override object ReadMetadata(Stream stream) => new IdspReader().ReadMetadata(stream);

        public override void PrintSpecificMetadata(object structure, StringBuilder builder)
        {
            var idsp = structure as IdspStructure;
            if (idsp == null) throw new InvalidDataException("Could not parse file metadata.");

            builder.AppendLine();

            builder.AppendLine($"Interleave size: 0x{idsp.InterleaveSize:X}");
            builder.AppendLine($"Audio data size: 0x{idsp.AudioDataLength:X}");

            GcAdpcm.PrintAdpcmMetadata(idsp.Channels.Cast<GcAdpcmChannelInfo>().ToList(), builder);
        }
    }
}
