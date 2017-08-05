using System.IO;
using System.Text;
using VGAudio.Containers.NintendoWare;

namespace VGAudio.Cli.Metadata.Containers
{
    internal class Bcstm : MetadataReader
    {
        public override Common ToCommon(object structure) => Bxstm.ToCommon(structure);
        public override object ReadMetadata(Stream stream) => new BCFstmReader().ReadMetadata(stream);
        public override void PrintSpecificMetadata(object structure, StringBuilder builder)
            => Bxstm.PrintSpecificMetadata(structure, builder);
    }
}
