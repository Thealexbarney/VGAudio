using System.IO;

namespace VGAudio.Cli.Metadata
{
    internal interface IMetadataReader
    {
        object ReadMetadata(Stream stream);
        Common ToCommon(object metadata);
    }
}