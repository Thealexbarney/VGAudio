using System.Collections.Generic;
using System.IO;
using System.Linq;
using VGAudio.Formats.GcAdpcm;

namespace VGAudio.Containers.NintendoWare.Structures
{
    public class ChannelInfo
    {
        public List<GcAdpcmChannelInfo> Channels { get; } = new List<GcAdpcmChannelInfo>();
        public List<int> WaveAudioOffsets { get; } = new List<int>();

        public static ChannelInfo ReadBfstm(BinaryReader reader)
        {
            var info = new ChannelInfo();
            int baseOffset = (int)reader.BaseStream.Position;

            var table = new ReferenceTable(reader, baseOffset);

            foreach (Reference channelInfo in table.References)
            {
                reader.BaseStream.Position = channelInfo.AbsoluteOffset;
                if (channelInfo.IsType(ReferenceType.WaveChannelInfo))
                {
                    var audioData = new Reference(reader);
                    info.WaveAudioOffsets.Add(audioData.Offset);
                }

                var adpcmInfo = new Reference(reader, channelInfo.AbsoluteOffset);

                if (adpcmInfo.IsType(ReferenceType.GcAdpcmInfo))
                {
                    reader.BaseStream.Position = adpcmInfo.AbsoluteOffset;

                    var channel = new GcAdpcmChannelInfo
                    {
                        Coefs = Enumerable.Range(0, 16).Select(x => reader.ReadInt16()).ToArray(),
                        Start = new GcAdpcmContext(reader),
                        Loop = new GcAdpcmContext(reader)
                    };
                    info.Channels.Add(channel);
                }
            }

            return info;
        }

        public static ChannelInfo ReadBrstm(BinaryReader reader, Reference reference)
        {
            var info = new ChannelInfo();

            int channelCount = reader.ReadByte();
            reader.BaseStream.Position += 3;

            var references = new List<Reference>();
            for (int i = 0; i < channelCount; i++)
            {
                references.Add(new Reference(reader, reference.BaseOffset));
            }

            foreach (Reference channelInfo in references)
            {
                reader.BaseStream.Position = channelInfo.AbsoluteOffset;
                var adpcmInfo = new Reference(reader, reference.BaseOffset);

                if (adpcmInfo.Offset > 0)
                {
                    reader.BaseStream.Position = adpcmInfo.AbsoluteOffset;

                    var channel = new GcAdpcmChannelInfo
                    {
                        Coefs = Enumerable.Range(0, 16).Select(x => reader.ReadInt16()).ToArray(),
                        Gain = reader.ReadInt16(),
                        Start = new GcAdpcmContext(reader),
                        Loop = new GcAdpcmContext(reader)
                    };
                    info.Channels.Add(channel);
                }
            }

            return info;
        }
    }
}
