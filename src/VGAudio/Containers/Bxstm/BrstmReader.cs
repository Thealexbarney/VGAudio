using System.IO;
using System.Linq;
using System.Text;
using VGAudio.Containers.Bxstm.Structures;
using VGAudio.Formats;
using VGAudio.Formats.GcAdpcm;
using VGAudio.Utilities;
using static VGAudio.Formats.GcAdpcm.GcAdpcmHelpers;
using static VGAudio.Utilities.Helpers;

namespace VGAudio.Containers.Bxstm
{
    public class BrstmReader : AudioReader<BrstmReader, BrstmStructure, BrstmConfiguration>
    {
        //Used to mark data offsets in the file
        private const int OffsetMarker = 0x01000000;

        //Used to mark an offset of a longer track description
        private const int OffsetMarkerV2 = 0x01010000;

        protected override BrstmStructure ReadFile(Stream stream, bool readAudioData = true)
        {
            using (BinaryReader reader = GetBinaryReader(stream, Endianness.BigEndian))
            {
                if (reader.ReadUTF8(4) != "RSTM")
                {
                    throw new InvalidDataException("File has no RSTM header");
                }

                var structure = new BrstmStructure();

                ReadRstmHeader(reader, structure);
                ReadHeadChunk(reader, structure);
                ReadAdpcChunk(reader, structure);
                Common.ReadDataChunk(reader, structure, readAudioData);

                return structure;
            }
        }

        protected override IAudioFormat ToAudioStream(BrstmStructure structure) => Common.ToAudioStream(structure);

        protected override BrstmConfiguration GetConfiguration(BrstmStructure structure)
        {
            var info = new StreamInfo();
            var configuration = new BrstmConfiguration();
            if (info.Codec == BxstmCodec.Adpcm)
            {
                configuration.SamplesPerSeekTableEntry = info.SamplesPerSeekTableEntry;
            }
            configuration.Codec = info.Codec;
            configuration.SamplesPerInterleave = info.SamplesPerInterleave;
            configuration.TrackType = structure.HeaderType;
            configuration.SeekTableType = structure.SeekTableType;
            return configuration;
        }

        private static void ReadRstmHeader(BinaryReader reader, BrstmStructure structure)
        {
            reader.Expect((ushort)0xfeff);
            structure.Version = new NwVersion(reader.ReadByte(), reader.ReadByte());
            structure.FileSize = reader.ReadInt32();

            if (reader.BaseStream.Length < structure.FileSize)
            {
                throw new InvalidDataException("Actual file length is less than stated length");
            }

            structure.HeaderSize = reader.ReadInt16();
            structure.HeaderSections = reader.ReadInt16();

            structure.InfoChunkOffset = reader.ReadInt32();
            structure.InfoChunkSizeHeader = reader.ReadInt32();
            structure.SeekChunkOffset = reader.ReadInt32();
            structure.SeekChunkSizeHeader = reader.ReadInt32();
            structure.DataChunkOffset = reader.ReadInt32();
            structure.DataChunkSizeHeader = reader.ReadInt32();
        }

        private static void ReadHeadChunk(BinaryReader reader, BrstmStructure structure)
        {
            reader.BaseStream.Position = structure.InfoChunkOffset;

            if (reader.ReadUTF8(4) != "HEAD")
            {
                throw new InvalidDataException("Unknown or invalid HEAD chunk");
            }

            structure.InfoChunkSize = reader.ReadInt32();
            if (structure.InfoChunkSize != structure.InfoChunkSizeHeader)
            {
                throw new InvalidDataException("HEAD chunk size in RSTM header doesn't match size in HEAD header");
            }

            int baseOffset = (int)reader.BaseStream.Position;
            var streamInfo = new Reference(reader, baseOffset);
            var trackInfo = new Reference(reader, baseOffset);
            var channelInfo = new Reference(reader, baseOffset);

            ReadStreamInfo(reader, structure, streamInfo);
            ReadTrackInfo(reader, structure, trackInfo);
            ReadHeadChunk3(reader, structure);
        }

        private static void ReadStreamInfo(BinaryReader reader, BrstmStructure structure, Reference reference)
        {
            if (reference?.IsType(ReferenceType.ByteTable) != true)
                throw new InvalidDataException("Could not read stream info.");

            reader.BaseStream.Position = reference.AbsoluteOffset;
            structure.StreamInfo = StreamInfo.ReadBrstm(reader);
        }

        private static void ReadTrackInfo(BinaryReader reader, BrstmStructure structure, Reference reference)
        {
            if (reference?.IsType(ReferenceType.ByteTable) != true)
                throw new InvalidDataException("Could not read track info.");

            reader.BaseStream.Position = reference.AbsoluteOffset;
            structure.TrackInfo = TrackInfo.ReadBrstm(reader, reference);
        }

        private static void ReadTrackInfof(BinaryReader reader, BrstmStructure structure)
        {
            int baseOffset = structure.InfoChunkOffset + 8;
            reader.BaseStream.Position = baseOffset + structure.InfoChunk2Offset;

            int trackCount = reader.ReadByte();
            int[] trackOffsets = new int[trackCount];

            structure.HeaderType = reader.ReadByte() == 0 ? BrstmTrackType.Short : BrstmTrackType.Standard;
            int marker = structure.HeaderType == BrstmTrackType.Short ? OffsetMarker : OffsetMarkerV2;

            reader.BaseStream.Position += 2;
            for (int i = 0; i < trackCount; i++)
            {
                reader.Expect(marker);
                trackOffsets[i] = reader.ReadInt32();
            }

            foreach (int offset in trackOffsets)
            {
                reader.BaseStream.Position = baseOffset + offset;
                var track = new AudioTrack();

                if (structure.HeaderType == BrstmTrackType.Standard)
                {
                    track.Volume = reader.ReadByte();
                    track.Panning = reader.ReadByte();
                    reader.BaseStream.Position += 6;
                }

                track.ChannelCount = reader.ReadByte();
                track.ChannelLeft = reader.ReadByte();
                track.ChannelRight = reader.ReadByte();

                structure.Tracks.Add(track);
            }
        }

        private static void ReadHeadChunk3(BinaryReader reader, BrstmStructure structure)
        {
            int baseOffset = structure.InfoChunkOffset + 8;
            reader.BaseStream.Position = baseOffset + structure.InfoChunk3Offset;
            var info = new StreamInfo();

            reader.Expect((byte)info.ChannelCount);
            reader.BaseStream.Position += 3;

            for (int i = 0; i < info.ChannelCount; i++)
            {
                var channel = new BxstmChannelInfo();
                reader.Expect(OffsetMarker);
                channel.Offset = reader.ReadInt32();
                structure.Channels.Add(channel);
            }

            foreach (BxstmChannelInfo channel in structure.Channels)
            {
                reader.BaseStream.Position = baseOffset + channel.Offset;
                reader.Expect(OffsetMarker);
                if (info.Codec != BxstmCodec.Adpcm) continue;

                int coefsOffset = reader.ReadInt32();
                reader.BaseStream.Position = baseOffset + coefsOffset;

                channel.Coefs = Enumerable.Range(0, 16).Select(x => reader.ReadInt16()).ToArray();
                channel.Gain = reader.ReadInt16();
                channel.Start = new GcAdpcmContext(reader);
                channel.Loop = new GcAdpcmContext(reader);
            }
        }

        private static void ReadAdpcChunk(BinaryReader reader, BrstmStructure structure)
        {
            var info = new StreamInfo();
            if (structure.SeekChunkOffset == 0) return;
            reader.BaseStream.Position = structure.SeekChunkOffset;

            if (reader.ReadUTF8(4) != "ADPC")
            {
                throw new InvalidDataException("Unknown or invalid ADPC chunk");
            }
            structure.SeekChunkSize = reader.ReadInt32();

            if (structure.SeekChunkSizeHeader != structure.SeekChunkSize)
            {
                throw new InvalidDataException("ADPC chunk size in RSTM header doesn't match size in ADPC header");
            }

            bool fullLastSeekTableEntry = info.SampleCount % info.SamplesPerSeekTableEntry == 0 && info.SampleCount > 0;
            int bytesPerEntry = 4 * info.ChannelCount;
            int seekTableEntriesCountShortened = (SampleCountToByteCount(info.SampleCount) / info.SamplesPerSeekTableEntry) + 1;
            int seekTableEntriesCountStandard = (info.SampleCount / info.SamplesPerSeekTableEntry) + (fullLastSeekTableEntry ? 0 : 1);
            int expectedSizeShortened = GetNextMultiple(8 + seekTableEntriesCountShortened * bytesPerEntry, 0x20);
            int expectedSizeStandard = GetNextMultiple(8 + seekTableEntriesCountStandard * bytesPerEntry, 0x20);

            if (structure.SeekChunkSize == expectedSizeStandard)
            {
                structure.SeekTableSize = bytesPerEntry * seekTableEntriesCountStandard;
                structure.SeekTableType = BrstmSeekTableType.Standard;
            }
            else if (structure.SeekChunkSize == expectedSizeShortened)
            {
                structure.SeekTableSize = bytesPerEntry * seekTableEntriesCountShortened;
                structure.SeekTableType = BrstmSeekTableType.Short;
            }
            else
            {
                return; //Unknown format. Don't parse table
            }

            byte[] tableBytes = reader.ReadBytes(structure.SeekTableSize);

            structure.SeekTable = tableBytes.ToShortArray(Endianness.BigEndian)
                .DeInterleave(2, info.ChannelCount);
        }
    }
}
