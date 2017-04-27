using System;
using System.IO;
using System.Linq;
using System.Text;
using VGAudio.Containers.Bxstm;
using VGAudio.Formats;
using VGAudio.Formats.GcAdpcm;
using VGAudio.Utilities;
using static VGAudio.Formats.GcAdpcm.GcAdpcmHelpers;
using static VGAudio.Utilities.Helpers;

namespace VGAudio.Containers
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
                if (Encoding.UTF8.GetString(reader.ReadBytes(4), 0, 4) != "RSTM")
                {
                    throw new InvalidDataException("File has no RSTM header");
                }

                var structure = new BrstmStructure();

                ReadRstmHeader(reader, structure);
                ReadHeadChunk(reader, structure);
                ReadAdpcChunk(reader, structure);
                ReadDataChunk(reader, structure, readAudioData);

                return structure;
            }
        }

        protected override IAudioFormat ToAudioStream(BrstmStructure structure)
        {
            var channels = new GcAdpcmChannel[structure.ChannelCount];

            for (int c = 0; c < channels.Length; c++)
            {
                var channelBuilder = new GcAdpcmChannelBuilder(structure.AudioData[c], structure.Channels[c].Coefs, structure.SampleCount)
                {
                    Gain = structure.Channels[c].Gain,
                    Hist1 = structure.Channels[c].Hist1,
                    Hist2 = structure.Channels[c].Hist2
                };

                channelBuilder.WithLoopContext(structure.LoopStart, structure.Channels[c].LoopPredScale,
                    structure.Channels[c].LoopHist1, structure.Channels[c].LoopHist2);

                if (structure.SeekTable != null)
                {
                    channelBuilder.WithSeekTable(structure.SeekTable[c], structure.SamplesPerSeekTableEntry);
                }

                channels[c] = channelBuilder.Build();
            }

            var builder = new GcAdpcmFormat.Builder(channels)
            {
                SampleCount = structure.SampleCount,
                SampleRate = structure.SampleRate,
                Tracks = structure.Tracks
            };
            return builder.Loop(structure.Looping, structure.LoopStart, structure.SampleCount).Build();
        }

        protected override BrstmConfiguration GetConfiguration(BrstmStructure structure)
        {
            return new BrstmConfiguration
            {
                SamplesPerInterleave = structure.SamplesPerInterleave,
                SamplesPerSeekTableEntry = structure.SamplesPerSeekTableEntry,
                TrackType = structure.HeaderType,
                SeekTableType = structure.SeekTableType
            };
        }

        private static void ReadRstmHeader(BinaryReader reader, BrstmStructure structure)
        {
            reader.Expect((ushort)0xfeff);
            structure.Version = reader.ReadInt16();
            structure.FileSize = reader.ReadInt32();

            if (reader.BaseStream.Length < structure.FileSize)
            {
                throw new InvalidDataException("Actual file length is less than stated length");
            }

            structure.RstmHeaderSize = reader.ReadInt16();
            structure.RstmHeaderSections = reader.ReadInt16();

            structure.HeadChunkOffset = reader.ReadInt32();
            structure.HeadChunkSizeRstm = reader.ReadInt32();
            structure.AdpcChunkOffset = reader.ReadInt32();
            structure.AdpcChunkSizeRstm = reader.ReadInt32();
            structure.DataChunkOffset = reader.ReadInt32();
            structure.DataChunkSizeRstm = reader.ReadInt32();
        }

        private static void ReadHeadChunk(BinaryReader reader, BrstmStructure structure)
        {
            reader.BaseStream.Position = structure.HeadChunkOffset;

            if (Encoding.UTF8.GetString(reader.ReadBytes(4), 0, 4) != "HEAD")
            {
                throw new InvalidDataException("Unknown or invalid HEAD chunk");
            }

            structure.HeadChunkSize = reader.ReadInt32();
            if (structure.HeadChunkSize != structure.HeadChunkSizeRstm)
            {
                throw new InvalidDataException("HEAD chunk size in RSTM header doesn't match size in HEAD header");
            }

            reader.Expect(OffsetMarker);
            structure.HeadChunk1Offset = reader.ReadInt32();
            reader.Expect(OffsetMarker);
            structure.HeadChunk2Offset = reader.ReadInt32();
            reader.Expect(OffsetMarker);
            structure.HeadChunk3Offset = reader.ReadInt32();

            ReadHeadChunk1(reader, structure);
            ReadHeadChunk2(reader, structure);
            ReadHeadChunk3(reader, structure);
        }

        private static void ReadHeadChunk1(BinaryReader reader, BrstmStructure structure)
        {
            reader.BaseStream.Position = structure.HeadChunkOffset + 8 + structure.HeadChunk1Offset;
            structure.Codec = (BxstmCodec)reader.ReadByte();
            if (structure.Codec != BxstmCodec.Adpcm)
            {
                throw new NotSupportedException("File must contain 4-bit ADPCM encoded audio");
            }

            structure.Looping = reader.ReadByte() == 1;
            structure.ChannelCount = reader.ReadByte();
            reader.BaseStream.Position += 1;

            structure.SampleRate = reader.ReadUInt16();
            reader.BaseStream.Position += 2;

            structure.LoopStart = reader.ReadInt32();
            structure.SampleCount = reader.ReadInt32();

            structure.AudioDataOffset = reader.ReadInt32();
            structure.InterleaveCount = reader.ReadInt32();
            structure.InterleaveSize = reader.ReadInt32();
            structure.SamplesPerInterleave = reader.ReadInt32();
            structure.LastBlockSizeWithoutPadding = reader.ReadInt32();
            structure.LastBlockSamples = reader.ReadInt32();
            structure.LastBlockSize = reader.ReadInt32();
            structure.SamplesPerSeekTableEntry = reader.ReadInt32();
        }

        private static void ReadHeadChunk2(BinaryReader reader, BrstmStructure structure)
        {
            int baseOffset = structure.HeadChunkOffset + 8;
            reader.BaseStream.Position = baseOffset + structure.HeadChunk2Offset;

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
                var track = new GcAdpcmTrack();

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
            int baseOffset = structure.HeadChunkOffset + 8;
            reader.BaseStream.Position = baseOffset + structure.HeadChunk3Offset;

            reader.Expect((byte)structure.ChannelCount);
            reader.BaseStream.Position += 3;

            for (int i = 0; i < structure.ChannelCount; i++)
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
                int coefsOffset = reader.ReadInt32();
                reader.BaseStream.Position = baseOffset + coefsOffset;

                channel.Coefs = Enumerable.Range(0, 16).Select(x => reader.ReadInt16()).ToArray();
                channel.Gain = reader.ReadInt16();
                channel.PredScale = reader.ReadInt16();
                channel.Hist1 = reader.ReadInt16();
                channel.Hist2 = reader.ReadInt16();
                channel.LoopPredScale = reader.ReadInt16();
                channel.LoopHist1 = reader.ReadInt16();
                channel.LoopHist2 = reader.ReadInt16();
            }
        }

        private static void ReadAdpcChunk(BinaryReader reader, BrstmStructure structure)
        {
            reader.BaseStream.Position = structure.AdpcChunkOffset;

            if (Encoding.UTF8.GetString(reader.ReadBytes(4), 0, 4) != "ADPC")
            {
                throw new InvalidDataException("Unknown or invalid ADPC chunk");
            }
            structure.AdpcChunkSize = reader.ReadInt32();

            if (structure.AdpcChunkSizeRstm != structure.AdpcChunkSize)
            {
                throw new InvalidDataException("ADPC chunk size in RSTM header doesn't match size in ADPC header");
            }

            bool fullLastSeekTableEntry = structure.SampleCount % structure.SamplesPerSeekTableEntry == 0 && structure.SampleCount > 0;
            int bytesPerEntry = 4 * structure.ChannelCount;
            int seekTableEntriesCountShortened = (SampleCountToByteCount(structure.SampleCount) / structure.SamplesPerSeekTableEntry) + 1;
            int seekTableEntriesCountStandard = (structure.SampleCount / structure.SamplesPerSeekTableEntry) + (fullLastSeekTableEntry ? 0 : 1);
            int expectedSizeShortened = GetNextMultiple(8 + seekTableEntriesCountShortened * bytesPerEntry, 0x20);
            int expectedSizeStandard = GetNextMultiple(8 + seekTableEntriesCountStandard * bytesPerEntry, 0x20);

            if (structure.AdpcChunkSize == expectedSizeStandard)
            {
                structure.SeekTableSize = bytesPerEntry * seekTableEntriesCountStandard;
                structure.SeekTableType = BrstmSeekTableType.Standard;
            }
            else if (structure.AdpcChunkSize == expectedSizeShortened)
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
                .DeInterleave(2, structure.ChannelCount);
        }

        private static void ReadDataChunk(BinaryReader reader, BrstmStructure structure, bool readAudioData)
        {
            reader.BaseStream.Position = structure.DataChunkOffset;

            if (Encoding.UTF8.GetString(reader.ReadBytes(4), 0, 4) != "DATA")
            {
                throw new InvalidDataException("Unknown or invalid DATA chunk");
            }
            structure.DataChunkSize = reader.ReadInt32();

            if (structure.DataChunkSizeRstm != structure.DataChunkSize)
            {
                throw new InvalidDataException("DATA chunk size in RSTM header doesn't match size in DATA header");
            }

            if (!readAudioData) return;

            reader.BaseStream.Position = structure.AudioDataOffset;
            int audioDataLength = structure.DataChunkSize - (structure.AudioDataOffset - structure.DataChunkOffset);

            structure.AudioData = reader.BaseStream.DeInterleave(audioDataLength, structure.InterleaveSize,
                structure.ChannelCount, SampleCountToByteCount(structure.SampleCount));
        }
    }
}
