using System.IO;
using System.Text;
using VGAudio.Containers.NintendoWare;

namespace VGAudio.Cli.Metadata.Containers
{
    internal class Bcwav : MetadataReader
    {
        public override object ReadMetadata(Stream stream) => new BCFstmReader().ReadMetadata(stream);
        public override Common ToCommon(object metadata) => Bxstm.ToCommon(metadata);

        public override void PrintSpecificMetadata(object metadata, StringBuilder builder)
        {
            var bcwav = metadata as BxstmStructure;
            if (bcwav == null) throw new InvalidDataException("Could not parse file metadata.");

            GcAdpcm.PrintAdpcmMetadata(bcwav.ChannelInfo.Channels, builder);
        }
    }
}
