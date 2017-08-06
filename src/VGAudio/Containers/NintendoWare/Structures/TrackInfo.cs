using System.Collections.Generic;
using System.IO;
using VGAudio.Formats;

namespace VGAudio.Containers.NintendoWare.Structures
{
    public class TrackInfo
    {
        public List<AudioTrack> Tracks { get; } = new List<AudioTrack>();
        public BrstmTrackType Type { get; set; }

        public static TrackInfo ReadBfstm(BinaryReader reader)
        {
            var info = new TrackInfo();
            int baseOffset = (int)reader.BaseStream.Position;

            var table = new ReferenceTable(reader, baseOffset);

            foreach (Reference trackInfo in table.References)
            {
                reader.BaseStream.Position = trackInfo.AbsoluteOffset;

                var track = new AudioTrack();
                track.Volume = reader.ReadByte();
                track.Panning = reader.ReadByte();
                track.SurroundPanning = reader.ReadByte();
                track.Flags = reader.ReadByte();

                var trackRef = new Reference(reader, trackInfo.AbsoluteOffset);
                reader.BaseStream.Position = trackRef.AbsoluteOffset;

                track.ChannelCount = reader.ReadInt32();
                track.ChannelLeft = reader.ReadByte();
                track.ChannelRight = reader.ReadByte();
                info.Tracks.Add(track);
            }
            return info;
        }

        public static TrackInfo ReadBrstm(BinaryReader reader, Reference reference)
        {
            var info = new TrackInfo();

            int trackCount = reader.ReadByte();
            info.Type = (BrstmTrackType)reader.ReadByte();
            reader.BaseStream.Position += 2;

            var references = new List<Reference>();
            for (int i = 0; i < trackCount; i++)
            {
                references.Add(new Reference(reader, reference.BaseOffset));
            }

            foreach (Reference trackInfo in references)
            {
                reader.BaseStream.Position = trackInfo.AbsoluteOffset;

                var track = new AudioTrack();
                if (trackInfo.DataType == (int) BrstmTrackType.Standard)
                {
                    track.Volume = reader.ReadByte();
                    track.Panning = reader.ReadByte();
                    reader.BaseStream.Position += 6;
                }

                track.ChannelCount = reader.ReadByte();
                track.ChannelLeft = reader.ReadByte();
                track.ChannelRight = reader.ReadByte();

                info.Tracks.Add(track);
            }
            return info;
        }
    }
}
