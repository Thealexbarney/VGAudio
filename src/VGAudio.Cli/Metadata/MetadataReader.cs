using System.IO;
using System.Text;

namespace VGAudio.Cli.Metadata
{
    internal abstract class MetadataReader
    {
        public abstract object ReadMetadata(Stream stream);
        public abstract Common ToCommon(object metadata);
        public virtual void PrintSpecificMetadata(object metadata, StringBuilder builder) { }
    }
}