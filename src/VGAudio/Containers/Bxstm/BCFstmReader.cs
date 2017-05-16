using System.IO;
using System.Linq;
using System.Text;
using VGAudio.Formats;
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
                string magic = Encoding.UTF8.GetString(reader.ReadBytes(4), 0, 4);
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
                ReadRegnChunk(reader, structure);
                Common.ReadDataChunk(reader, structure, readAudioData);

                return structure;
            }
        }
        
        private static void ReadHeader(BinaryReader reader, BCFstmStructure structure)
        {
            reader.Expect((ushort)0xfeff);
            structure.HeaderSize = reader.ReadInt16();
            structure.Version = reader.ReadInt32() >> 16;
            structure.FileSize = reader.ReadInt32();

            if (reader.BaseStream.Length < structure.FileSize)
            {
                throw new InvalidDataException("Actual file length is less than stated length");
            }

            structure.HeaderSections = reader.ReadInt16();
            reader.BaseStream.Position += 2;

            for (int i = 0; i < structure.HeaderSections; i++)
            {
                int type = reader.ReadInt16();
                reader.BaseStream.Position += 2;
                switch (type)
                {
                    case 0x4000:
                        structure.InfoChunkOffset = reader.ReadInt32();
                        structure.InfoChunkSizeHeader = reader.ReadInt32();
                        break;
                    case 0x4001:
                        structure.SeekChunkOffset = reader.ReadInt32();
                        structure.SeekChunkSizeHeader = reader.ReadInt32();
                        break;
                    case 0x4002:
                        structure.DataChunkOffset = reader.ReadInt32();
                        structure.DataChunkSizeHeader = reader.ReadInt32();
                        break;
                    case 0x4003:
                        structure.RegnChunkOffset = reader.ReadInt32();
                        structure.RegnChunkSizeHeader = reader.ReadInt32();
                        break;
                    case 0x4004:
                        structure.PdatChunkOffset = reader.ReadInt32();
                        structure.PdatChunkSizeHeader = reader.ReadInt32();
                        break;
                    default:
                        throw new InvalidDataException($"Unknown section type {type}");
                }
            }
        }

        private static void ReadInfoChunk(BinaryReader reader, BCFstmStructure structure)
        {
            reader.BaseStream.Position = structure.InfoChunkOffset;
            if (Encoding.UTF8.GetString(reader.ReadBytes(4), 0, 4) != "INFO")
            {
                throw new InvalidDataException("Unknown or invalid INFO chunk");
            }

            structure.InfoChunkSize = reader.ReadInt32();
            if (structure.InfoChunkSize != structure.InfoChunkSizeHeader)
            {
                throw new InvalidDataException("INFO chunk size in CSTM header doesn't match size in INFO header");
            }

            reader.Expect((short)0x4100);
            reader.BaseStream.Position += 2;
            structure.InfoChunk1Offset = reader.ReadInt32();
            reader.Expect((short)0x0101, (short)0);
            reader.BaseStream.Position += 2;
            structure.InfoChunk2Offset = reader.ReadInt32();
            reader.Expect((short)0x0101);
            reader.BaseStream.Position += 2;
            structure.InfoChunk3Offset = reader.ReadInt32();

            ReadInfoChunk1(reader, structure);
            ReadInfoChunk2(reader, structure);
            ReadInfoChunk3(reader, structure);
        }

        private static void ReadInfoChunk1(BinaryReader reader, BCFstmStructure structure)
        {
            reader.BaseStream.Position = structure.InfoChunkOffset + 8 + structure.InfoChunk1Offset;
            structure.Codec = (BxstmCodec)reader.ReadByte();

            structure.Looping = reader.ReadBoolean();
            structure.ChannelCount = reader.ReadByte();
            structure.SectionCount = reader.ReadByte();

            structure.SampleRate = reader.ReadInt32();

            structure.LoopStart = reader.ReadInt32();
            structure.SampleCount = reader.ReadInt32();

            structure.InterleaveCount = reader.ReadInt32();
            structure.InterleaveSize = reader.ReadInt32();
            structure.SamplesPerInterleave = reader.ReadInt32();
            structure.LastBlockSizeWithoutPadding = reader.ReadInt32();
            structure.LastBlockSamples = reader.ReadInt32();
            structure.LastBlockSize = reader.ReadInt32();
            structure.BytesPerSeekTableEntry = reader.ReadInt32();
            structure.SamplesPerSeekTableEntry = reader.ReadInt32();

            reader.Expect((short)0x1f00);
            reader.BaseStream.Position += 2;
            structure.AudioDataOffset = reader.ReadInt32() + structure.DataChunkOffset + 8;
            structure.InfoPart1Extra = reader.ReadInt16() == 0x100;
            if (structure.InfoPart1Extra)
            {
                reader.BaseStream.Position += 10;
            }
            if (structure.Version == 4)
            {
                structure.LoopStartUnaligned = reader.ReadInt32();
                structure.LoopEndUnaligned = reader.ReadInt32();
            }
        }

        private static void ReadInfoChunk2(BinaryReader reader, BCFstmStructure structure)
        {
            if (structure.InfoChunk2Offset == -1)
            {
                structure.IncludeTracks = false;
                return;
            }

            structure.IncludeTracks = true;
            int part2Offset = structure.InfoChunkOffset + 8 + structure.InfoChunk2Offset;
            reader.BaseStream.Position = part2Offset;

            int numTracks = reader.ReadInt32();

            int[] trackOffsets = new int[numTracks];
            for (int i = 0; i < numTracks; i++)
            {
                reader.Expect((short)0x4101);
                reader.BaseStream.Position += 2;
                trackOffsets[i] = reader.ReadInt32();
            }

            foreach (int offset in trackOffsets)
            {
                reader.BaseStream.Position = part2Offset + offset;

                var track = new AudioTrack();
                track.Volume = reader.ReadByte();
                track.Panning = reader.ReadByte();
                reader.BaseStream.Position += 2;

                reader.BaseStream.Position += 8;
                track.ChannelCount = reader.ReadInt32();
                track.ChannelLeft = reader.ReadByte();
                track.ChannelRight = reader.ReadByte();
                structure.Tracks.Add(track);
            }
        }

        private static void ReadInfoChunk3(BinaryReader reader, BCFstmStructure structure)
        {
            int part3Offset = structure.InfoChunkOffset + 8 + structure.InfoChunk3Offset;
            reader.BaseStream.Position = part3Offset;

            reader.Expect(structure.ChannelCount);

            for (int i = 0; i < structure.ChannelCount; i++)
            {
                var channel = new BxstmChannelInfo();
                reader.Expect((short)0x4102);
                reader.BaseStream.Position += 2;
                channel.Offset = reader.ReadInt32();
                structure.Channels.Add(channel);
            }

            if (structure.Codec != BxstmCodec.Adpcm) return;

            foreach (BxstmChannelInfo channel in structure.Channels)
            {
                int channelInfoOffset = part3Offset + channel.Offset;
                reader.BaseStream.Position = channelInfoOffset;
                reader.Expect((short)0x0300);
                reader.BaseStream.Position += 2;
                int coefsOffset = reader.ReadInt32() + channelInfoOffset;
                reader.BaseStream.Position = coefsOffset;

                channel.Coefs = Enumerable.Range(0, 16).Select(x => reader.ReadInt16()).ToArray();
                channel.PredScale = reader.ReadInt16();
                channel.Hist1 = reader.ReadInt16();
                channel.Hist2 = reader.ReadInt16();
                channel.LoopPredScale = reader.ReadInt16();
                channel.LoopHist1 = reader.ReadInt16();
                channel.LoopHist2 = reader.ReadInt16();
                channel.Gain = reader.ReadInt16();
            }
        }

        private static void ReadSeekChunk(BinaryReader reader, BCFstmStructure structure)
        {
            if (structure.SeekChunkOffset == 0) return;
            reader.BaseStream.Position = structure.SeekChunkOffset;

            if (Encoding.UTF8.GetString(reader.ReadBytes(4), 0, 4) != "SEEK")
            {
                throw new InvalidDataException("Unknown or invalid SEEK chunk");
            }
            structure.SeekChunkSize = reader.ReadInt32();

            if (structure.SeekChunkSizeHeader != structure.SeekChunkSize)
            {
                throw new InvalidDataException("SEEK chunk size in header doesn't match size in SEEK header");
            }

            int bytesPerEntry = 4 * structure.ChannelCount;
            int numSeekTableEntries = structure.SampleCount.DivideByRoundUp(structure.SamplesPerSeekTableEntry);

            structure.SeekTableSize = bytesPerEntry * numSeekTableEntries;

            byte[] tableBytes = reader.ReadBytes(structure.SeekTableSize);

            structure.SeekTable = tableBytes.ToShortArray()
                .DeInterleave(2, structure.ChannelCount);
        }

        private static void ReadRegnChunk(BinaryReader reader, BCFstmStructure structure)
        {
            if (structure.RegnChunkOffset == 0) return;

            reader.BaseStream.Position = structure.RegnChunkOffset;

            if (Encoding.UTF8.GetString(reader.ReadBytes(4), 0, 4) != "REGN")
            {
                throw new InvalidDataException("Unknown or invalid REGN chunk");
            }
            structure.RegnChunkSize = reader.ReadInt32();

            if (structure.RegnChunkSizeHeader != structure.RegnChunkSize)
            {
                throw new InvalidDataException("REGN chunk size in header doesn't match size in REGN header");
            }

            if (structure.SectionCount * 0x100 + 0x20 != structure.RegnChunkSize)
            {
                throw new InvalidDataException(
                    $"Invalid REGN chunk size 0x{structure.RegnChunkSize:x}. Expected 0x{structure.SectionCount * 0x100 + 0x20:x}");
            }

            var regn = new RegnChunk
            {
                Size = structure.RegnChunkSize,
                EntryCount = structure.SectionCount
            };

            for (int i = 0; i < regn.EntryCount; i++)
            {
                reader.BaseStream.Position = structure.RegnChunkOffset + 0x20 + 0x100 * i;

                var entry = new RegnEntry
                {
                    StartSample = reader.ReadInt32(),
                    EndSample = reader.ReadInt32()
                };

                for (int c = 0; c < structure.ChannelCount; c++)
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