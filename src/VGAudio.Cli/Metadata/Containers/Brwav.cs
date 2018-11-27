using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VGAudio.Containers.NintendoWare;
using VGAudio.Formats.GcAdpcm;

namespace VGAudio.Cli.Metadata.Containers
{
    internal class Brwav : MetadataReader
    {
        public override object ReadMetadata(Stream stream) => new BrwavReader().ReadMetadata(stream);
        public override Common ToCommon(object metadata) => Bxstm.ToCommon(metadata);

        public override void PrintSpecificMetadata(object metadata, StringBuilder builder)
        {
            var brwav = metadata as BrwavStructure;
            if (brwav == null) throw new InvalidDataException("Could not parse file metadata.");

            List<GcAdpcmChannelInfo> channels = brwav.WaveInfo.Channels.Select(x => x.AdpcmInfo).ToList();
            GcAdpcm.PrintAdpcmMetadata(channels, builder, brwav.WaveInfo.Looping);
        }
    }
}
