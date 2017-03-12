using System.IO;
using VGAudio.Containers;

namespace VGAudio.Cli.Metadata.Containers
{
    internal class Bcstm : IMetadataReader
    {
        public Common ToCommon(object structure) => Bxstm.ToCommon(structure);
        public object ReadMetadata(Stream stream) => new BcstmReader().ReadMetadata(stream);
    }
}
