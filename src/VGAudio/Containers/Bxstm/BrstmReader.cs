using System.IO;
using VGAudio.Containers.Bxstm.Structures;
using VGAudio.Formats;
using VGAudio.Utilities;
using static VGAudio.Formats.GcAdpcm.GcAdpcmHelpers;
using static VGAudio.Utilities.Helpers;

namespace VGAudio.Containers.Bxstm
{
    public class BrstmReader : AudioReader<BrstmReader, BrstmStructure, BrstmConfiguration>
    {
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
                ReadHeadBlock(reader, structure);
                ReadAdpcBlock(reader, structure);
                ReadDataBlock(reader, structure, readAudioData);

                return structure;
            }
        }

        protected override IAudioFormat ToAudioStream(BrstmStructure structure) => Common.ToAudioStream(structure);

        protected override BrstmConfiguration GetConfiguration(BrstmStructure structure)
        {
            var info = structure.StreamInfo;
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
            structure.BlockCount = reader.ReadInt16();

            structure.HeadBlockOffset = reader.ReadInt32();
            structure.HeadBlockSize = reader.ReadInt32();
            structure.SeekBlockOffset = reader.ReadInt32();
            structure.SeekBlockSize = reader.ReadInt32();
            structure.DataBlockOffset = reader.ReadInt32();
            structure.DataBlockSize = reader.ReadInt32();
        }

        private static void ReadHeadBlock(BinaryReader reader, BrstmStructure structure)
        {
            reader.BaseStream.Position = structure.HeadBlockOffset;

            if (reader.ReadUTF8(4) != "HEAD")
            {
                throw new InvalidDataException("Unknown or invalid HEAD block");
            }

            if (reader.ReadInt32() != structure.HeadBlockSize)
            {
                throw new InvalidDataException("HEAD block size in RSTM header doesn't match size in HEAD header");
            }

            int baseOffset = (int)reader.BaseStream.Position;
            var streamInfo = new Reference(reader, baseOffset);
            var trackInfo = new Reference(reader, baseOffset);
            var channelInfo = new Reference(reader, baseOffset);

            ReadStreamInfo(reader, structure, streamInfo);
            ReadTrackInfo(reader, structure, trackInfo);
            ReadChannelInfo(reader, structure, channelInfo);
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

        private static void ReadChannelInfo(BinaryReader reader, BrstmStructure structure, Reference reference)
        {
            if (reference?.IsType(ReferenceType.ByteTable) != true)
                throw new InvalidDataException("Could not read channel info.");

            reader.BaseStream.Position = reference.AbsoluteOffset;
            structure.ChannelInfo = ChannelInfo.ReadBrstm(reader, reference);
        }

        private static void ReadAdpcBlock(BinaryReader reader, BrstmStructure structure)
        {
            var info = structure.StreamInfo;
            if (structure.SeekBlockOffset == 0) return;
            reader.BaseStream.Position = structure.SeekBlockOffset;

            if (reader.ReadUTF8(4) != "ADPC")
            {
                throw new InvalidDataException("Unknown or invalid ADPC block");
            }

            if (reader.ReadInt32() != structure.SeekBlockSize)
            {
                throw new InvalidDataException("ADPC block size in RSTM header doesn't match size in ADPC header");
            }

            bool fullLastSeekTableEntry = info.SampleCount % info.SamplesPerSeekTableEntry == 0 && info.SampleCount > 0;
            int bytesPerEntry = 4 * info.ChannelCount;
            int seekTableEntriesCountShortened = (SampleCountToByteCount(info.SampleCount) / info.SamplesPerSeekTableEntry) + 1;
            int seekTableEntriesCountStandard = (info.SampleCount / info.SamplesPerSeekTableEntry) + (fullLastSeekTableEntry ? 0 : 1);
            int expectedSizeShortened = GetNextMultiple(8 + seekTableEntriesCountShortened * bytesPerEntry, 0x20);
            int expectedSizeStandard = GetNextMultiple(8 + seekTableEntriesCountStandard * bytesPerEntry, 0x20);

            if (structure.SeekBlockSize == expectedSizeStandard)
            {
                structure.SeekTableSize = bytesPerEntry * seekTableEntriesCountStandard;
                structure.SeekTableType = BrstmSeekTableType.Standard;
            }
            else if (structure.SeekBlockSize == expectedSizeShortened)
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

        private static void ReadDataBlock(BinaryReader reader, BrstmStructure structure, bool readAudioData)
        {
            reader.BaseStream.Position = structure.DataBlockOffset;
            StreamInfo info = structure.StreamInfo;

            if (reader.ReadUTF8(4) != "DATA")
            {
                throw new InvalidDataException("Unknown or invalid DATA block");
            }

            if (reader.ReadInt32() != structure.DataBlockSize)
            {
                throw new InvalidDataException("DATA block size in main header doesn't match size in DATA header");
            }

            if (!readAudioData) return;

            reader.BaseStream.Position = info.AudioDataOffset;
            int audioDataLength = structure.DataBlockSize - (info.AudioDataOffset - structure.DataBlockOffset);
            int outputSize = Common.SamplesToBytes(info.SampleCount, info.Codec);

            structure.AudioData = reader.BaseStream.DeInterleave(audioDataLength, info.InterleaveSize,
                info.ChannelCount, outputSize);
        }
    }
}
