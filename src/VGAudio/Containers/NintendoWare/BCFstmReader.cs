using System.Collections.Generic;
using System.IO;
using System.Linq;
using VGAudio.Containers.NintendoWare.Structures;
using VGAudio.Formats;
using VGAudio.Formats.GcAdpcm;
using VGAudio.Utilities;
using static VGAudio.Utilities.Helpers;

namespace VGAudio.Containers.NintendoWare
{
    public class BCFstmReader : AudioReader<BCFstmReader, BxstmStructure, BxstmConfiguration>
    {
        protected override BxstmStructure ReadFile(Stream stream, bool readAudioData = true)
        {
            Endianness endianness;
            using (BinaryReader reader = GetBinaryReader(stream, Endianness.LittleEndian))
            {
                string magic = reader.ReadUTF8(4);
                if (magic != "CSTM" && magic != "FSTM" && magic != "CWAV" && magic != "FWAV" && magic != "CSTP" && magic != "FSTP")
                {
                    throw new InvalidDataException("File has no CSTM or FSTM header");
                }

                ushort bom = reader.ReadUInt16();
                stream.Position -= 2;
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
            }

            using (BinaryReader reader = GetBinaryReader(stream, endianness))
            {
                var structure = new BxstmStructure { Endianness = endianness };

                ReadHeader(reader, structure);
                ReadInfoBlock(reader, structure);
                ReadSeekBlock(reader, structure);
                ReadRegionBlock(reader, structure);
                ReadDataBlock(reader, structure, readAudioData);

                return structure;
            }
        }

        protected override IAudioFormat ToAudioStream(BxstmStructure structure) => Common.ToAudioStream(structure);

        protected override BxstmConfiguration GetConfiguration(BxstmStructure structure)
        {
            StreamInfo info = structure.StreamInfo;
            var configuration = new BxstmConfiguration();
            if (info.Codec == NwCodec.GcAdpcm && info.SamplesPerSeekTableEntry != 0)
            {
                configuration.SamplesPerSeekTableEntry = info.SamplesPerSeekTableEntry;
            }
            configuration.Codec = info.Codec;
            configuration.Endianness = structure.Endianness;
            if (info.SamplesPerInterleave != 0)
            {
                configuration.SamplesPerInterleave = info.SamplesPerInterleave;
            }
            configuration.Version = structure.Version;
            return configuration;
        }

        private static void ReadHeader(BinaryReader reader, BxstmStructure structure)
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

        private static void ReadInfoBlock(BinaryReader reader, BxstmStructure structure)
        {
            SizedReference reference =
                structure.Blocks.FirstOrDefault(x => x.Type == ReferenceType.StreamInfoBlock ||
                                                     x.Type == ReferenceType.WaveInfoBlock) ??
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

            switch (reference.Type)
            {
                case ReferenceType.StreamInfoBlock:
                    int baseOffset = (int)reader.BaseStream.Position;
                    var streamInfo = new Reference(reader, baseOffset);
                    var trackInfo = new Reference(reader, baseOffset);
                    var channelInfo = new Reference(reader, baseOffset);

                    ReadStreamInfo(reader, structure, streamInfo);
                    ReadTrackInfo(reader, structure, trackInfo);
                    ReadChannelInfo(reader, structure, channelInfo);
                    break;
                case ReferenceType.WaveInfoBlock:
                    structure.StreamInfo = StreamInfo.ReadBfwav(reader, structure.Version);
                    structure.ChannelInfo = ChannelInfo.ReadBfstm(reader);
                    break;
            }
        }

        private static void ReadStreamInfo(BinaryReader reader, BxstmStructure structure, Reference reference)
        {
            if (reference?.IsType(ReferenceType.StreamInfo) != true)
                throw new InvalidDataException("Could not read stream info.");

            reader.BaseStream.Position = reference.AbsoluteOffset;
            structure.StreamInfo = StreamInfo.ReadBfstm(reader, structure.Version);
        }

        private static void ReadTrackInfo(BinaryReader reader, BxstmStructure structure, Reference reference)
        {
            if (reference?.IsType(ReferenceType.ReferenceTable) != true) { return; }

            reader.BaseStream.Position = reference.AbsoluteOffset;
            structure.TrackInfo = TrackInfo.ReadBfstm(reader);
        }

        private static void ReadChannelInfo(BinaryReader reader, BxstmStructure structure, Reference reference)
        {
            if (reference?.IsType(ReferenceType.ReferenceTable) != true) { return; }

            reader.BaseStream.Position = reference.AbsoluteOffset;
            structure.ChannelInfo = ChannelInfo.ReadBfstm(reader);
        }

        private static void ReadSeekBlock(BinaryReader reader, BxstmStructure structure)
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

        private static void ReadRegionBlock(BinaryReader reader, BxstmStructure structure)
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

        private static void ReadDataBlock(BinaryReader reader, BxstmStructure structure, bool readAudioData)
        {
            SizedReference reference =
                structure.Blocks.FirstOrDefault(x => x.Type == ReferenceType.StreamDataBlock ||
                                                     x.Type == ReferenceType.StreamPrefetchDataBlock ||
                                                     x.Type == ReferenceType.WaveDataBlock) ??
                throw new InvalidDataException("File has no DATA block");

            StreamInfo info = structure.StreamInfo;

            reader.BaseStream.Position = reference.AbsoluteOffset;

            string blockId = reader.ReadUTF8(4);
            if (blockId != "DATA" && blockId != "PDAT")
            {
                throw new InvalidDataException("Unknown or invalid DATA block");
            }

            if (reader.ReadInt32() != reference.Size)
            {
                throw new InvalidDataException("DATA block size in main header doesn't match size in DATA header");
            }

            if (!readAudioData) return;

            if (reference.IsType(ReferenceType.WaveDataBlock))
            {
                int audioDataLength = Common.SamplesToBytes(info.SampleCount, info.Codec);
                structure.AudioData = CreateJaggedArray<byte[][]>(info.ChannelCount, audioDataLength);
                int baseOffset = (int)reader.BaseStream.Position;

                for (int i = 0; i < info.ChannelCount; i++)
                {
                    reader.BaseStream.Position = baseOffset + structure.ChannelInfo.WaveAudioOffsets[i];
                    structure.AudioData[i] = reader.ReadBytes(audioDataLength);
                }
            }
            else if (reference.IsType(ReferenceType.StreamDataBlock))
            {
                int audioOffset = reference.AbsoluteOffset + info.AudioReference.Offset + 8;
                reader.BaseStream.Position = audioOffset;
                int audioDataLength = reference.Size - (audioOffset - reference.AbsoluteOffset);
                int outputSize = Common.SamplesToBytes(info.SampleCount, info.Codec);

                structure.AudioData = reader.BaseStream.DeInterleave(audioDataLength, info.InterleaveSize,
                    info.ChannelCount, outputSize);
            }
            else if (reference.IsType(ReferenceType.StreamPrefetchDataBlock))
            {
                structure.PrefetchData = new List<PrefetchData>();
                int count = reader.ReadInt32();

                for (int i = 0; i < count; i++)
                {
                    structure.PrefetchData.Add(PrefetchData.ReadPrefetchData(reader, info));
                }

                reader.BaseStream.Position = structure.PrefetchData[0].AudioData.AbsoluteOffset;
                structure.AudioData = reader.BaseStream.DeInterleave(structure.PrefetchData[0].Size, info.InterleaveSize,
                    info.ChannelCount, structure.PrefetchData[0].Size);
            }
        }
    }
}