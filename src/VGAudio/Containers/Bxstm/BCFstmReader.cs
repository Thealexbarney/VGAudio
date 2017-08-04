using System.IO;
using System.Linq;
using VGAudio.Containers.Bxstm.Structures;
using VGAudio.Formats.GcAdpcm;
using VGAudio.Utilities;
using static VGAudio.Utilities.Helpers;

namespace VGAudio.Containers.Bxstm
{
    public class BCFstmReader
    {
        public BCFstmStructure ReadFile(Stream stream, bool readAudioData = true)
        {
            BCFstmType type;
            Endianness endianness;
            using (BinaryReader reader = GetBinaryReader(stream, Endianness.LittleEndian))
            {
                string magic = reader.ReadUTF8(4);
                switch (magic)
                {
                    case "CSTM":
                        type = BCFstmType.Bcstm;
                        break;
                    case "FSTM":
                        type = BCFstmType.Bfstm;
                        break;
                    default:
                        throw new InvalidDataException("File has no CSTM or FSTM header");
                }

                var bom = reader.ReadUInt16();
                switch (bom)
                {
                    case 0xFEFF:
                        endianness = Endianness.LittleEndian;
                        break;
                    case 0xFFFE:
                        endianness = Endianness.BigEndian;
                        break;
                    default:
                        throw new InvalidDataException("File has no byte order mark");
                }
                stream.Position -= 2;
            }

            using (BinaryReader reader = GetBinaryReader(stream, endianness))
            {
                BCFstmStructure structure = type == BCFstmType.Bcstm ? (BCFstmStructure)new BcstmStructure() : new BfstmStructure();
                structure.Endianness = endianness;

                ReadHeader(reader, structure);
                ReadInfoChunk(reader, structure);
                ReadSeekChunk(reader, structure);
                //ReadRegnChunk(reader, structure);
                Common.ReadDataChunk2(reader, structure, readAudioData);

                return structure;
            }
        }

        private static void ReadHeader(BinaryReader reader, BCFstmStructure structure)
        {
            reader.Expect((ushort)0xfeff);
            structure.HeaderSize = reader.ReadInt16();
            structure.Version = new NwVersion(reader.ReadUInt32());
            structure.FileSize = reader.ReadInt32();

            if (reader.BaseStream.Length < structure.FileSize)
            {
                throw new InvalidDataException("Actual file length is less than stated length");
            }

            structure.HeaderSections = reader.ReadInt16();
            reader.BaseStream.Position += 2;

            for (int i = 0; i < structure.HeaderSections; i++)
            {
                structure.Sections.Add(new SizedReference(reader));
            }
        }

        private static void ReadInfoChunk(BinaryReader reader, BCFstmStructure structure)
        {
            SizedReference reference = structure.Sections.FirstOrDefault(x => x.Type == ReferenceType.StreamInfoBlock) ??
                throw new InvalidDataException("File has no INFO chunk");

            reader.BaseStream.Position = reference.AbsoluteOffset;
            if (reader.ReadUTF8(4) != "INFO")
            {
                throw new InvalidDataException("Unknown or invalid INFO chunk");
            }

            structure.InfoChunkSize = reader.ReadInt32();
            if (structure.InfoChunkSize != reference.Size)
            {
                throw new InvalidDataException("INFO chunk size in main header doesn't match size in INFO header");
            }

            int baseOffset = (int)reader.BaseStream.Position;
            var streamInfo = new Reference(reader, baseOffset);
            var trackInfo = new Reference(reader, baseOffset);
            var channelInfo = new Reference(reader, baseOffset);

            ReadStreamInfo(reader, structure, streamInfo);
            ReadTrackInfo(reader, structure, trackInfo);
            ReadChannelInfo(reader, structure, channelInfo);
        }

        private static void ReadStreamInfo(BinaryReader reader, BCFstmStructure structure, Reference reference)
        {
            if (reference?.IsType(ReferenceType.StreamInfo) != true)
                throw new InvalidDataException("Could not read stream info.");

            reader.BaseStream.Position = reference.AbsoluteOffset;
            structure.StreamInfo = StreamInfo.ReadBfstm(reader, structure.Version);
        }

        private static void ReadTrackInfo(BinaryReader reader, BCFstmStructure structure, Reference reference)
        {
            if (reference?.IsType(ReferenceType.ReferenceTable) != true) { return; }

            reader.BaseStream.Position = reference.AbsoluteOffset;
            structure.TrackInfo = TrackInfo.ReadBfstm(reader);
        }

        private static void ReadChannelInfo(BinaryReader reader, BCFstmStructure structure, Reference reference)
        {
            if (reference?.IsType(ReferenceType.ReferenceTable) != true) { return; }

            reader.BaseStream.Position = reference.AbsoluteOffset;
            int baseOffset = reference.AbsoluteOffset;
            var table = new ReferenceTable(reader, baseOffset);

            foreach (Reference channelInfo in table.References)
            {
                reader.BaseStream.Position = channelInfo.AbsoluteOffset;
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
                    structure.Channels.Add(channel);
                }
            }
        }

        private static void ReadSeekChunk(BinaryReader reader, BCFstmStructure structure)
        {
            StreamInfo info = structure.StreamInfo;
            if (structure.SeekChunkOffset == 0) return;
            reader.BaseStream.Position = structure.SeekChunkOffset;

            if (reader.ReadUTF8(4) != "SEEK")
            {
                throw new InvalidDataException("Unknown or invalid SEEK chunk");
            }
            structure.SeekChunkSize = reader.ReadInt32();

            if (structure.SeekChunkSizeHeader != structure.SeekChunkSize)
            {
                throw new InvalidDataException("SEEK chunk size in header doesn't match size in SEEK header");
            }

            int bytesPerEntry = 4 * info.ChannelCount;
            int numSeekTableEntries = info.SampleCount.DivideByRoundUp(info.SamplesPerSeekTableEntry);

            structure.SeekTableSize = bytesPerEntry * numSeekTableEntries;

            byte[] tableBytes = reader.ReadBytes(structure.SeekTableSize);

            structure.SeekTable = tableBytes.ToShortArray()
                .DeInterleave(2, info.ChannelCount);
        }

        private static void ReadRegnChunk(BinaryReader reader, BCFstmStructure structure)
        {
            SizedReference reference = structure.Sections.FirstOrDefault(x => x.Type == ReferenceType.StreamInfoBlock);
            if (reference == null) return;

            StreamInfo info = structure.StreamInfo;

            reader.BaseStream.Position = reference.AbsoluteOffset;

            if (reader.ReadUTF8(4) != "REGN")
            {
                throw new InvalidDataException("Unknown or invalid REGN chunk");
            }
            structure.RegnChunkSize = reader.ReadInt32();

            if (structure.RegnChunkSizeHeader != structure.RegnChunkSize)
            {
                throw new InvalidDataException("REGN chunk size in header doesn't match size in REGN header");
            }

            if (info.RegionCount * info.RegionInfoSize != structure.RegnChunkSize)
            {
                throw new InvalidDataException(
                    $"Invalid REGN chunk size 0x{structure.RegnChunkSize:x}. Expected 0x{info.RegionCount * info.RegionInfoSize + 0x20:x}");
            }

            var regn = new RegnChunk
            {
                Size = structure.RegnChunkSize,
                EntryCount = info.RegionCount
            };

            for (int i = 0; i < regn.EntryCount; i++)
            {
                reader.BaseStream.Position = structure.RegnChunkOffset + 0x20 + info.RegionInfoSize * i;

                var entry = new RegnEntry
                {
                    StartSample = reader.ReadInt32(),
                    EndSample = reader.ReadInt32()
                };

                for (int c = 0; c < info.ChannelCount; c++)
                {
                    entry.Channels.Add(new RegnChannel
                    {
                        PredScale = reader.ReadInt16(),
                        Value1 = reader.ReadInt16(),
                        Value2 = reader.ReadInt16()
                    });
                }
                regn.Entries.Add(entry);
            }

            structure.Regn = regn;
        }

        private enum BCFstmType
        {
            Bcstm,
            Bfstm
        }
    }
}