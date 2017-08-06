using System.IO;
using System.Text;
using VGAudio.Containers.NintendoWare;

namespace VGAudio.Cli.Metadata.Containers
{
    internal class Bfwav : MetadataReader
    {
        public override object ReadMetadata(Stream stream) => new BCFstmReader().ReadMetadata(stream);
        public override Common ToCommon(object metadata) => Bxstm.ToCommon(metadata);

        public override void PrintSpecificMetadata(object metadata, StringBuilder builder)
        {
            var bfwav = metadata as BxstmStructure;
            if (bfwav == null) throw new InvalidDataException("Could not parse file metadata.");

            GcAdpcm.PrintAdpcmMetadata(bfwav.ChannelInfo.Channels, builder);
        }
    }
}
