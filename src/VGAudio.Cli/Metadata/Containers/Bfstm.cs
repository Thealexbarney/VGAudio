using System.IO;
using System.Text;
using VGAudio.Containers;
using VGAudio.Containers.Bxstm;

namespace VGAudio.Cli.Metadata.Containers
{
    internal class Bfstm : MetadataReader
    {
        public override Common ToCommon(object structure) => Bxstm.ToCommon(structure);
        public override object ReadMetadata(Stream stream) => new BfstmReader().ReadMetadata(stream);
        public override void PrintSpecificMetadata(object structure, StringBuilder builder)
        {
            Bxstm.PrintSpecificMetadata(structure, builder);

            var bfstm = structure as BfstmStructure;
            if (bfstm == null) throw new InvalidDataException("Could not parse file metadata.");

            if (bfstm.Regn == null) return;

            builder.AppendLine("\nREGN Chunk");
            builder.AppendLine(new string('-', 40));

            for (int i = 0; i < bfstm.Regn.EntryCount; i++)
            {
                builder.AppendLine();
                builder.AppendLine($"Entry {i}");
                builder.AppendLine(new string('-', 25));
                builder.AppendLine($"Start sample: {bfstm.Regn.Entries[i].StartSample}");
                builder.AppendLine($"End sample: {bfstm.Regn.Entries[i].EndSample}");

                for (int c = 0; c < bfstm.ChannelCount; c++)
                {
                    short v1 = bfstm.Regn.Entries[i].Channels[c].Value1;
                    short v2 = bfstm.Regn.Entries[i].Channels[c].Value2;

                    builder.AppendLine($"Channel {c}: {v1}, {v2}");
                }
            }
        }
    }
}
