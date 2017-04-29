using System.IO;
using System.Linq;
using VGAudio.Containers.Bxstm;
using VGAudio.Formats;
using VGAudio.Formats.GcAdpcm;
using VGAudio.Utilities;
using static VGAudio.Formats.GcAdpcm.GcAdpcmHelpers;
using static VGAudio.Utilities.Helpers;

namespace VGAudio.Containers
{
    public class BrstmWriter : AudioWriter<BrstmWriter, BrstmConfiguration>
    {
        private GcAdpcmFormat Adpcm { get; set; }
        protected override int FileSize => RstmHeaderSize + HeadChunkSize + AdpcChunkSize + DataChunkSize;

        private int SampleCount => Adpcm.Looping ? LoopEnd : Adpcm.SampleCount;
        private int ChannelCount => Adpcm.ChannelCount;
        private int TrackCount => Adpcm.Tracks?.Count ?? 0;

        private int LoopStart => Adpcm.LoopStart;
        private int LoopEnd => Adpcm.LoopEnd;

        private static BxstmCodec Codec => BxstmCodec.Adpcm;
        private int AudioDataOffset => DataChunkOffset + 0x20;

        /// <summary>
        /// Size of a single channel's ADPCM audio data with padding when written to a file
        /// </summary>
        private int AudioDataSize => GetNextMultiple(SampleCountToByteCount(SampleCount), 0x20);

        private int SamplesPerInterleave => Configuration.SamplesPerInterleave;
        private int InterleaveSize => SampleCountToByteCount(SamplesPerInterleave);
        private int InterleaveCount => SampleCount.DivideByRoundUp(SamplesPerInterleave);

        private int LastBlockSamples => SampleCount - ((InterleaveCount - 1) * SamplesPerInterleave);
        private int LastBlockSizeWithoutPadding => SampleCountToByteCount(LastBlockSamples);
        private int LastBlockSize => GetNextMultiple(LastBlockSizeWithoutPadding, 0x20);

        private int SamplesPerSeekTableEntry => Configuration.SamplesPerSeekTableEntry;
        private int BytesPerSeekTableEntry => 4;
        private int SeekTableEntryCount => Configuration.SeekTableType == BrstmSeekTableType.Standard
            ? SampleCount.DivideByRoundUp(SamplesPerSeekTableEntry)
            : (SampleCountToByteCount(SampleCount) / SamplesPerSeekTableEntry) + 1;

        private int RstmHeaderSize => 0x40;

        private int HeadChunkOffset => RstmHeaderSize;
        private int HeadChunkSize => GetNextMultiple(HeadChunkHeaderSize + HeadChunkTableSize +
            HeadChunk1Size + HeadChunk2Size + HeadChunk3Size, 0x20);
        private int HeadChunkHeaderSize => 8;
        private int HeadChunkTableSize => 8 * 3;
        private int HeadChunk1Size => 0x34;
        private int HeadChunk2Size => 4 + (8 * TrackCount) + (TrackInfoSize * TrackCount);
        private BrstmTrackType HeaderType => Configuration.TrackType;
        private int TrackInfoSize => HeaderType == BrstmTrackType.Short ? 4 : 0x0c;
        private int HeadChunk3Size => 4 + (8 * ChannelCount) + (ChannelInfoSize * ChannelCount);
        private int ChannelInfoSize => 0x38;

        private int AdpcChunkOffset => RstmHeaderSize + HeadChunkSize;
        private int AdpcChunkSize => GetNextMultiple(8 + SeekTableEntryCount * ChannelCount * BytesPerSeekTableEntry, 0x20);

        private int DataChunkOffset => RstmHeaderSize + HeadChunkSize + AdpcChunkSize;
        private int DataChunkSize => 0x20 + AudioDataSize * ChannelCount;

        //Used to mark data offsets in the file
        private const int OffsetMarker = 0x01000000;

        //Used to mark an offset of a longer track description
        private const int OffsetMarkerV2 = 0x01010000;

        protected override void SetupWriter(AudioData audio)
        {
            Adpcm = audio.GetFormat<GcAdpcmFormat>();

            if (!LoopPointsAreAligned(LoopStart, Configuration.LoopPointAlignment))
            {
                Adpcm = Adpcm.GetCloneBuilder().WithAlignment(Configuration.LoopPointAlignment).Build();
            }

            Parallel.For(0, ChannelCount, i =>
            {
                GcAdpcmChannelBuilder builder = Adpcm.Channels[i].GetCloneBuilder()
                    .WithSamplesPerSeekTableEntry(SamplesPerSeekTableEntry)
                    .WithLoop(Adpcm.Looping, Adpcm.UnalignedLoopStart, Adpcm.UnalignedLoopEnd);

                builder.LoopAlignmentMultiple = Configuration.LoopPointAlignment;
                builder.EnsureLoopContextIsSelfCalculated = Configuration.RecalculateLoopContext;
                builder.EnsureSeekTableIsSelfCalculated = Configuration.RecalculateSeekTable;
                Adpcm.Channels[i] = builder.Build();
            });
        }

        protected override void WriteStream(Stream stream)
        {
            using (BinaryWriter writer = GetBinaryWriter(stream, Endianness.BigEndian))
            {
                stream.Position = 0;
                WriteRstmHeader(writer);
                stream.Position = HeadChunkOffset;
                WriteHeadChunk(writer);
                stream.Position = AdpcChunkOffset;
                WriteAdpcChunk(writer);
                stream.Position = DataChunkOffset;
                WriteDataChunk(writer);
            }
        }

        private void WriteRstmHeader(BinaryWriter writer)
        {
            writer.WriteUTF8("RSTM");
            writer.Write((ushort)0xfeff); //Endianness
            writer.Write((short)0x0100); //BRSTM format version
            writer.Write(FileSize);
            writer.Write((short)RstmHeaderSize);
            writer.Write((short)2); // EntryCount
            writer.Write(HeadChunkOffset);
            writer.Write(HeadChunkSize);
            writer.Write(AdpcChunkOffset);
            writer.Write(AdpcChunkSize);
            writer.Write(DataChunkOffset);
            writer.Write(DataChunkSize);
        }

        private void WriteHeadChunk(BinaryWriter writer)
        {
            writer.WriteUTF8("HEAD");
            writer.Write(HeadChunkSize);

            writer.Write(OffsetMarker);
            writer.Write(HeadChunkTableSize); //Chunk 1 offset
            writer.Write(OffsetMarker);
            writer.Write(HeadChunkTableSize + HeadChunk1Size); //Chunk 2 offset
            writer.Write(OffsetMarker);
            writer.Write(HeadChunkTableSize + HeadChunk1Size + HeadChunk2Size); //Chunk 3 offset

            WriteHeadChunk1(writer);
            WriteHeadChunk2(writer);
            WriteHeadChunk3(writer);
        }

        private void WriteHeadChunk1(BinaryWriter writer)
        {
            writer.Write((byte)Codec);
            writer.Write(Adpcm.Looping);
            writer.Write((byte)ChannelCount);
            writer.Write((byte)0); //padding
            writer.Write((ushort)Adpcm.SampleRate);
            writer.Write((short)0);//padding
            writer.Write(LoopStart);
            writer.Write(SampleCount);
            writer.Write(AudioDataOffset);
            writer.Write(InterleaveCount);
            writer.Write(InterleaveSize);
            writer.Write(SamplesPerInterleave);
            writer.Write(LastBlockSizeWithoutPadding);
            writer.Write(LastBlockSamples);
            writer.Write(LastBlockSize);
            writer.Write(SamplesPerSeekTableEntry);
            writer.Write(BytesPerSeekTableEntry);
        }

        private void WriteHeadChunk2(BinaryWriter writer)
        {
            writer.Write((byte)TrackCount);
            writer.Write((byte)(HeaderType == BrstmTrackType.Short ? 0 : 1));
            writer.Write((short)0);

            int baseOffset = HeadChunkTableSize + HeadChunk1Size + 4;
            int offsetTableSize = TrackCount * 8;

            for (int i = 0; i < TrackCount; i++)
            {
                writer.Write(HeaderType == BrstmTrackType.Short ? OffsetMarker : OffsetMarkerV2);
                writer.Write(baseOffset + offsetTableSize + TrackInfoSize * i);
            }

            for (int i = 0; i < TrackCount; i++)
            {
                AudioTrack track = Adpcm.Tracks[i];
                if (HeaderType == BrstmTrackType.Standard)
                {
                    writer.Write((byte)track.Volume);
                    writer.Write((byte)track.Panning);
                    writer.Write((short)0);
                    writer.Write(0);
                }
                writer.Write((byte)track.ChannelCount);
                writer.Write((byte)track.ChannelLeft); //First channel ID
                writer.Write((byte)track.ChannelRight); //Second channel ID
                writer.Write((byte)0);
            }
        }

        private void WriteHeadChunk3(BinaryWriter writer)
        {
            writer.Write((byte)ChannelCount);
            writer.Write((byte)0); //padding
            writer.Write((short)0); //padding

            int baseOffset = HeadChunkTableSize + HeadChunk1Size + HeadChunk2Size + 4;
            int offsetTableSize = ChannelCount * 8;

            for (int i = 0; i < ChannelCount; i++)
            {
                writer.Write(OffsetMarker);
                writer.Write(baseOffset + offsetTableSize + ChannelInfoSize * i);
            }

            for (int i = 0; i < ChannelCount; i++)
            {
                GcAdpcmChannel channel = Adpcm.Channels[i];
                writer.Write(OffsetMarker);
                writer.Write(baseOffset + offsetTableSize + ChannelInfoSize * i + 8);
                writer.Write(channel.Coefs.ToByteArray(Endianness.BigEndian));
                writer.Write(channel.Gain);
                writer.Write(channel.PredScale);
                writer.Write(channel.Hist1);
                writer.Write(channel.Hist2);
                writer.Write(Adpcm.Looping ? channel.LoopPredScale : channel.PredScale);
                writer.Write(Adpcm.Looping ? channel.LoopHist1 : (short)0);
                writer.Write(Adpcm.Looping ? channel.LoopHist2 : (short)0);
                writer.Write((short)0);
            }
        }

        private void WriteAdpcChunk(BinaryWriter writer)
        {
            writer.WriteUTF8("ADPC");
            writer.Write(AdpcChunkSize);

            var table = Adpcm.BuildSeekTable(SeekTableEntryCount, Endianness.BigEndian);

            writer.Write(table);
        }

        private void WriteDataChunk(BinaryWriter writer)
        {
            writer.WriteUTF8("DATA");
            writer.Write(DataChunkSize);
            writer.Write(0x18);

            writer.BaseStream.Position = AudioDataOffset;

            byte[][] channels = Adpcm.Channels.Select(x => x.GetAdpcmAudio()).ToArray();

            channels.Interleave(writer.BaseStream, InterleaveSize, AudioDataSize);
        }
    }
}