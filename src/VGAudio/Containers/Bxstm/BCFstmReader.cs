using System.Collections.Generic;
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
                ReadInfoBlock(reader, structure);
                ReadSeekBlock(reader, structure);
                ReadRegionBlock(reader, structure);
                ReadDataBlock(reader, structure, readAudioData);

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

            structure.BlockCount = reader.ReadInt16();
            reader.BaseStream.Position += 2;

            for (int i = 0; i < structure.BlockCount; i++)
            {
                structure.Blocks.Add(new SizedReference(reader));
            }
        }

        private static void ReadInfoBlock(BinaryReader reader, BCFstmStructure structure)
        {
            SizedReference reference = structure.Blocks.FirstOrDefault(x => x.Type == ReferenceType.StreamInfoBlock) ??
                throw new InvalidDataException("File has no INFO block");

            reader.BaseStream.Position = reference.AbsoluteOffset;
            if (reader.ReadUTF8(4) != "INFO")
            {
                throw new InvalidDataException("Unknown or invalid INFO block");
            }

            if (reader.ReadInt32() != reference.Size)
            {
                throw new InvalidDataException("INFO block size in main header doesn't match size in INFO header");
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
            structure.ChannelInfo = ChannelInfo.ReadBfstm(reader);
        }

        private static void ReadSeekBlock(BinaryReader reader, BCFstmStructure structure)
        {
            SizedReference reference = structure.Blocks.FirstOrDefault(x => x.Type == ReferenceType.StreamSeekBlock);
            if (reference == null) return;

            reader.BaseStream.Position = reference.AbsoluteOffset;
            StreamInfo info = structure.StreamInfo;

            if (reader.ReadUTF8(4) != "SEEK")
            {
                throw new InvalidDataException("Unknown or invalid SEEK block");
            }

            if (reader.ReadInt32() != reference.Size)
            {
                throw new InvalidDataException("SEEK block size in main header doesn't match size in SEEK header");
            }

            int bytesPerEntry = 4 * info.ChannelCount;
            int numSeekTableEntries = info.SampleCount.DivideByRoundUp(info.SamplesPerSeekTableEntry);

            int seekTableSize = bytesPerEntry * numSeekTableEntries;

            byte[] tableBytes = reader.ReadBytes(seekTableSize);

            structure.SeekTable = tableBytes.ToShortArray()
                .DeInterleave(2, info.ChannelCount);
        }

        private static void ReadRegionBlock(BinaryReader reader, BCFstmStructure structure)
        {
            SizedReference reference = structure.Blocks.FirstOrDefault(x => x.Type == ReferenceType.StreamRegionBlock);
            if (reference == null) return;
            
            reader.BaseStream.Position = reference.AbsoluteOffset;

            if (reader.ReadUTF8(4) != "REGN")
            {
                throw new InvalidDataException("Unknown or invalid REGN block");
            }

            if (reader.ReadInt32() != reference.Size)
            {
                throw new InvalidDataException("REGN block size in main header doesn't match size in REGN header");
            }

            StreamInfo info = structure.StreamInfo;
            int startAddress = reference.AbsoluteOffset + 8 + info.RegionReference.Offset;
            var regions = new List<RegionInfo>();

            for (int i = 0; i < info.RegionCount; i++)
            {
                reader.BaseStream.Position = startAddress + info.RegionInfoSize * i;

                var entry = new RegionInfo
                {
                    StartSample = reader.ReadInt32(),
                    EndSample = reader.ReadInt32()
                };

                for (int c = 0; c < info.ChannelCount; c++)
                {
                    entry.Channels.Add(new GcAdpcmContext(reader));
                }
                regions.Add(entry);
            }

            structure.Regions = regions;
        }

        private static void ReadDataBlock(BinaryReader reader, BCFstmStructure structure, bool readAudioData)
        {
            SizedReference reference = structure.Blocks.FirstOrDefault(x => x.Type == ReferenceType.StreamDataBlock) ??
                                       throw new InvalidDataException("File has no DATA block");

            var info = structure.StreamInfo;

            reader.BaseStream.Position = reference.AbsoluteOffset;

            if (reader.ReadUTF8(4) != "DATA")
            {
                throw new InvalidDataException("Unknown or invalid DATA block");
            }

            if (reader.ReadInt32() != reference.Size)
            {
                throw new InvalidDataException("DATA block size in main header doesn't match size in DATA header");
            }

            if (!readAudioData) return;

            int audioOffset = reference.AbsoluteOffset + info.AudioReference.Offset + 8;
            reader.BaseStream.Position = audioOffset;
            int audioDataLength = reference.Size - (audioOffset - reference.AbsoluteOffset);
            int outputSize = Common.SamplesToBytes(info.SampleCount, info.Codec);

            structure.AudioData = reader.BaseStream.DeInterleave(audioDataLength, info.InterleaveSize,
                info.ChannelCount, outputSize);
        }

        private enum BCFstmType
        {
            Bcstm,
            Bfstm
        }
    }
}