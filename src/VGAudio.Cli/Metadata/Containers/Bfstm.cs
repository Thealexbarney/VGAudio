using System.IO;
using System.Text;
using VGAudio.Containers.NintendoWare;

namespace VGAudio.Cli.Metadata.Containers
{
    internal class Bfstm : MetadataReader
    {
        public override Common ToCommon(object structure) => Bxstm.ToCommon(structure);
        public override object ReadMetadata(Stream stream) => new BCFstmReader().ReadMetadata(stream);
        public override void PrintSpecificMetadata(object structure, StringBuilder builder)
        {
            Bxstm.PrintSpecificMetadata(structure, builder);

            var bfstm = structure as BxstmStructure;
            if (bfstm == null) throw new InvalidDataException("Could not parse file metadata.");

            if (bfstm.Regions == null) return;

            builder.AppendLine("\nAudio Regions");
            builder.AppendLine(new string('-', 40));
            builder.AppendLine("Start sample - End sample\n");

            for (int i = 0; i < bfstm.Regions.Count; i++)
            {
                builder.AppendLine($"{i}: {bfstm.Regions[i].StartSample} - {bfstm.Regions[i].EndSample}");
            }
        }
    }
}
