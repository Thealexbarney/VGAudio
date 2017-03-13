using System.IO;
using System.Text;
using VGAudio.Containers;

namespace VGAudio.Cli.Metadata.Containers
{
    internal class Bcstm : MetadataReader
    {
        public override Common ToCommon(object structure) => Bxstm.ToCommon(structure);
        public override object ReadMetadata(Stream stream) => new BcstmReader().ReadMetadata(stream);
        public override void PrintSpecificMetadata(object structure, StringBuilder builder)
            => Bxstm.PrintSpecificMetadata(structure, builder);
    }
}
