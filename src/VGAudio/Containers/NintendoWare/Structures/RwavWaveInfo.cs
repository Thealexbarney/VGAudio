using System.Collections.Generic;
using System.IO;
using VGAudio.Formats.GcAdpcm;

namespace VGAudio.Containers.NintendoWare.Structures
{
    public class RwavWaveInfo : StreamInfo
    {
        public int ChannelInfoOffset { get; set; }
        public int InfoStructureLength { get; set; }
        public List<RwavChannelInfo> Channels { get; set; } = new List<RwavChannelInfo>();

        public static RwavWaveInfo ReadBrwav(BinaryReader reader)
        {
            int baseOffset = (int)reader.BaseStream.Position;
            var info = new RwavWaveInfo();

            info.Codec = (NwCodec)reader.ReadByte();
            info.Looping = reader.ReadBoolean();
            info.ChannelCount = reader.ReadByte();
            reader.BaseStream.Position++;
            info.SampleRate = reader.ReadUInt16();
            reader.BaseStream.Position += 2;
            info.LoopStart = GcAdpcmHelpers.NibbleToSample(reader.ReadInt32());
            info.SampleCount = GcAdpcmHelpers.NibbleToSample(reader.ReadInt32());
            info.ChannelInfoOffset = reader.ReadInt32();
            info.InfoStructureLength = reader.ReadInt32();

            var channelInfoOffsets = new int[info.ChannelCount];
            reader.BaseStream.Position = baseOffset + info.ChannelInfoOffset;

            for (int i = 0; i < info.ChannelCount; i++)
            {
                channelInfoOffsets[i] = reader.ReadInt32();
            }

            for (int i = 0; i < info.ChannelCount; i++)
            {
                reader.BaseStream.Position = baseOffset + channelInfoOffsets[i];
                var channel = RwavChannelInfo.Read(reader, baseOffset);
                info.Channels.Add(channel);
            }

            return info;
        }
    }
}
