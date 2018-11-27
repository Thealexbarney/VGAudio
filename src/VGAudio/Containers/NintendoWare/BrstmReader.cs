using System.IO;
using VGAudio.Containers.NintendoWare.Structures;
using VGAudio.Formats;
using VGAudio.Utilities;
using static VGAudio.Codecs.GcAdpcm.GcAdpcmMath;
using static VGAudio.Utilities.Helpers;

namespace VGAudio.Containers.NintendoWare
{
    public class BrstmReader : AudioReader<BrstmReader, BxstmStructure, BxstmConfiguration>
    {
        protected override BxstmStructure ReadFile(Stream stream, bool readAudioData = true)
        {
            using (BinaryReader reader = GetBinaryReader(stream, Endianness.BigEndian))
            {
                if (reader.ReadUTF8(4) != "RSTM")
                {
                    throw new InvalidDataException("File has no RSTM header");
                }

                var structure = new BxstmStructure();

                ReadRstmHeader(reader, structure);
                ReadHeadBlock(reader, structure);
                ReadAdpcBlock(reader, structure);
                ReadDataBlock(reader, structure, readAudioData);

                return structure;
            }
        }

        protected override IAudioFormat ToAudioStream(BxstmStructure structure) => Common.ToAudioStream(structure);

        protected override BxstmConfiguration GetConfiguration(BxstmStructure structure)
        {
            StreamInfo info = structure.StreamInfo;
            var configuration = new BxstmConfiguration();
            if (info.Codec == NwCodec.GcAdpcm)
            {
                configuration.SamplesPerSeekTableEntry = info.SamplesPerSeekTableEntry;
            }
            configuration.Codec = info.Codec;
            configuration.SamplesPerInterleave = info.SamplesPerInterleave;
            configuration.TrackType = structure.TrackInfo.Type;
            configuration.SeekTableType = structure.BrstmSeekTableType;
            return configuration;
        }

        private static void ReadRstmHeader(BinaryReader reader, BxstmStructure structure)
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

            structure.BrstmHeader = BrstmHeader.Read(reader);
        }

        private static void ReadHeadBlock(BinaryReader reader, BxstmStructure structure)
        {
            reader.BaseStream.Position = structure.BrstmHeader.HeadBlockOffset;

            if (reader.ReadUTF8(4) != "HEAD")
            {
                throw new InvalidDataException("Unknown or invalid HEAD block");
            }

            if (reader.ReadInt32() != structure.BrstmHeader.HeadBlockSize)
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

        private static void ReadStreamInfo(BinaryReader reader, BxstmStructure structure, Reference reference)
        {
            if (reference?.IsType(ReferenceType.ByteTable) != true)
                throw new InvalidDataException("Could not read stream info.");

            reader.BaseStream.Position = reference.AbsoluteOffset;
            structure.StreamInfo = StreamInfo.ReadBrstm(reader);
        }

        private static void ReadTrackInfo(BinaryReader reader, BxstmStructure structure, Reference reference)
        {
            if (reference?.IsType(ReferenceType.ByteTable) != true)
                throw new InvalidDataException("Could not read track info.");

            reader.BaseStream.Position = reference.AbsoluteOffset;
            structure.TrackInfo = TrackInfo.ReadBrstm(reader, reference);
        }

        private static void ReadChannelInfo(BinaryReader reader, BxstmStructure structure, Reference reference)
        {
            if (reference?.IsType(ReferenceType.ByteTable) != true)
                throw new InvalidDataException("Could not read channel info.");

            reader.BaseStream.Position = reference.AbsoluteOffset;
            structure.ChannelInfo = ChannelInfo.ReadBrstm(reader, reference);
        }

        private static void ReadAdpcBlock(BinaryReader reader, BxstmStructure structure)
        {
            if (structure.BrstmHeader.SeekBlockOffset == 0) return;
            reader.BaseStream.Position = structure.BrstmHeader.SeekBlockOffset;

            if (reader.ReadUTF8(4) != "ADPC")
            {
                throw new InvalidDataException("Unknown or invalid ADPC block");
            }

            if (reader.ReadInt32() != structure.BrstmHeader.SeekBlockSize)
            {
                throw new InvalidDataException("ADPC block size in RSTM header doesn't match size in ADPC header");
            }

            StreamInfo info = structure.StreamInfo;
            bool fullLastSeekTableEntry = info.SampleCount % info.SamplesPerSeekTableEntry == 0 && info.SampleCount > 0;
            int bytesPerEntry = 4 * info.ChannelCount;
            int seekTableEntriesCountShortened = (SampleCountToByteCount(info.SampleCount) / info.SamplesPerSeekTableEntry) + 1;
            int seekTableEntriesCountStandard = (info.SampleCount / info.SamplesPerSeekTableEntry) + (fullLastSeekTableEntry ? 0 : 1);
            int expectedSizeShortened = GetNextMultiple(8 + seekTableEntriesCountShortened * bytesPerEntry, 0x20);
            int expectedSizeStandard = GetNextMultiple(8 + seekTableEntriesCountStandard * bytesPerEntry, 0x20);

            int seekTableSize;
            if (structure.BrstmHeader.SeekBlockSize == expectedSizeStandard)
            {
                seekTableSize = bytesPerEntry * seekTableEntriesCountStandard;
                structure.BrstmSeekTableType = BrstmSeekTableType.Standard;
            }
            else if (structure.BrstmHeader.SeekBlockSize == expectedSizeShortened)
            {
                seekTableSize = bytesPerEntry * seekTableEntriesCountShortened;
                structure.BrstmSeekTableType = BrstmSeekTableType.Short;
            }
            else
            {
                return; //Unknown format. Don't parse table
            }

            byte[] tableBytes = reader.ReadBytes(seekTableSize);

            structure.SeekTable = tableBytes.ToShortArray(Endianness.BigEndian)
                .DeInterleave(2, info.ChannelCount);
        }

        private static void ReadDataBlock(BinaryReader reader, BxstmStructure structure, bool readAudioData)
        {
            reader.BaseStream.Position = structure.BrstmHeader.DataBlockOffset;
            StreamInfo info = structure.StreamInfo;

            if (reader.ReadUTF8(4) != "DATA")
            {
                throw new InvalidDataException("Unknown or invalid DATA block");
            }

            if (reader.ReadInt32() != structure.BrstmHeader.DataBlockSize)
            {
                throw new InvalidDataException("DATA block size in main header doesn't match size in DATA header");
            }

            if (!readAudioData) return;

            reader.BaseStream.Position = info.AudioDataOffset;
            int audioDataLength = structure.BrstmHeader.DataBlockSize - (info.AudioDataOffset - structure.BrstmHeader.DataBlockOffset);
            int outputSize = Common.SamplesToBytes(info.SampleCount, info.Codec);

            structure.AudioData = reader.BaseStream.DeInterleave(audioDataLength, info.InterleaveSize,
                info.ChannelCount, outputSize);
        }
    }
}
