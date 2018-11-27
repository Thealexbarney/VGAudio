using System.Collections.Generic;
using System.IO;
using System.Linq;
using VGAudio.Codecs.GcAdpcm;
using VGAudio.Containers.NintendoWare.Structures;
using VGAudio.Formats;
using VGAudio.Formats.GcAdpcm;
using VGAudio.Formats.Pcm16;
using VGAudio.Formats.Pcm8;
using VGAudio.Utilities;
using static VGAudio.Containers.NintendoWare.Common;
using static VGAudio.Codecs.GcAdpcm.GcAdpcmMath;
using static VGAudio.Utilities.Helpers;

namespace VGAudio.Containers.NintendoWare
{
    public class BrstmWriter : AudioWriter<BrstmWriter, BxstmConfiguration>
    {
        private GcAdpcmFormat Adpcm { get; set; }
        private Pcm16Format Pcm16 { get; set; }
        private Pcm8SignedFormat Pcm8 { get; set; }
        private IAudioFormat AudioFormat { get; set; }

        protected override int FileSize => RstmHeaderSize + HeadBlockSize + AdpcBlockSize + DataBlockSize;

        private int SampleCount => AudioFormat.Looping ? LoopEnd : AudioFormat.SampleCount;
        private int ChannelCount => AudioFormat.ChannelCount;
        private int TrackCount => Tracks?.Count ?? 0;
        private List<AudioTrack> Tracks { get; set; }

        private int LoopStart => AudioFormat.LoopStart;
        private int LoopEnd => AudioFormat.LoopEnd;

        private NwCodec Codec => Configuration.Codec;
        private int AudioDataOffset => DataBlockOffset + 0x20;

        /// <summary>
        /// Size of a single channel's audio data with padding when written to a file
        /// </summary>
        private int AudioDataSize => GetNextMultiple(SamplesToBytes(SampleCount, Codec), 0x20);

        private int SamplesPerInterleave => Configuration.SamplesPerInterleave;
        private int InterleaveSize => SamplesToBytes(SamplesPerInterleave, Codec);
        private int InterleaveCount => SampleCount.DivideByRoundUp(SamplesPerInterleave);

        private int LastBlockSamples => SampleCount - ((InterleaveCount - 1) * SamplesPerInterleave);
        private int LastBlockSizeWithoutPadding => SamplesToBytes(LastBlockSamples, Codec);
        private int LastBlockSize => GetNextMultiple(LastBlockSizeWithoutPadding, 0x20);

        private int SamplesPerSeekTableEntry => Codec == NwCodec.GcAdpcm ? Configuration.SamplesPerSeekTableEntry : 0;
        private int BytesPerSeekTableEntry => Codec == NwCodec.GcAdpcm ? 4 : 0;
        private int SeekTableEntryCount => Configuration.SeekTableType == BrstmSeekTableType.Standard
            ? SampleCount.DivideByRoundUp(SamplesPerSeekTableEntry)
            : (SampleCountToByteCount(SampleCount) / SamplesPerSeekTableEntry) + 1;

        private int RstmHeaderSize => 0x40;

        private int HeadBlockOffset => RstmHeaderSize;
        private int HeadBlockSize => GetNextMultiple(HeadBlockHeaderSize + HeadBlockTableSize +
            HeadBlock1Size + HeadBlock2Size + HeadBlock3Size, 0x20);
        private int HeadBlockHeaderSize => 8;
        private int HeadBlockTableSize => 8 * 3;
        private int HeadBlock1Size => 0x34;
        private int HeadBlock2Size => 4 + (8 * TrackCount) + (TrackInfoSize * TrackCount);
        private BrstmTrackType HeaderType => Configuration.TrackType;
        private int TrackInfoSize => HeaderType == BrstmTrackType.Short ? 4 : 0x0c;
        private int HeadBlock3Size => 4 + (8 * ChannelCount) + (ChannelInfoSize * ChannelCount);
        private int ChannelInfoSize => Codec == NwCodec.GcAdpcm ? 0x38 : 8;

        private int AdpcBlockOffset => Codec == NwCodec.GcAdpcm ? RstmHeaderSize + HeadBlockSize : 0;
        private int AdpcBlockSize => Codec == NwCodec.GcAdpcm ? GetNextMultiple(8 + SeekTableEntryCount * ChannelCount * BytesPerSeekTableEntry, 0x20) : 0;

        private int DataBlockOffset => RstmHeaderSize + HeadBlockSize + AdpcBlockSize;
        private int DataBlockSize => 0x20 + AudioDataSize * ChannelCount;

        //Used to mark data offsets in the file
        private const int OffsetMarker = 0x01000000;

        //Used to mark an offset of a longer track description
        private const int OffsetMarkerV2 = 0x01010000;

        protected override void SetupWriter(AudioData audio)
        {
            var parameters = new GcAdpcmParameters { Progress = Configuration.Progress };

            if (Codec == NwCodec.GcAdpcm)
            {
                Adpcm = audio.GetFormat<GcAdpcmFormat>(parameters);

                if (!LoopPointsAreAligned(Adpcm.LoopStart, Configuration.LoopPointAlignment))
                {
                    Adpcm = Adpcm.GetCloneBuilder().WithAlignment(Configuration.LoopPointAlignment).Build();
                }

                Parallel.For(0, Adpcm.ChannelCount, i =>
                {
                    GcAdpcmChannelBuilder builder = Adpcm.Channels[i].GetCloneBuilder()
                        .WithSamplesPerSeekTableEntry(SamplesPerSeekTableEntry)
                        .WithLoop(Adpcm.Looping, Adpcm.UnalignedLoopStart, Adpcm.UnalignedLoopEnd);

                    builder.LoopAlignmentMultiple = Configuration.LoopPointAlignment;
                    builder.EnsureLoopContextIsSelfCalculated = Configuration.RecalculateLoopContext;
                    builder.EnsureSeekTableIsSelfCalculated = Configuration.RecalculateSeekTable;
                    Adpcm.Channels[i] = builder.Build();
                });

                AudioFormat = Adpcm;
                Tracks = Adpcm.Tracks;
            }
            else if (Codec == NwCodec.Pcm16Bit)
            {
                Pcm16 = audio.GetFormat<Pcm16Format>(parameters);
                AudioFormat = Pcm16;
                Tracks = Pcm16.Tracks;
            }
            else if (Codec == NwCodec.Pcm8Bit)
            {
                Pcm8 = audio.GetFormat<Pcm8SignedFormat>(parameters);
                AudioFormat = Pcm8;
                Tracks = Pcm8.Tracks;
            }
        }

        protected override void WriteStream(Stream stream)
        {
            using (BinaryWriter writer = GetBinaryWriter(stream, Endianness.BigEndian))
            {
                stream.Position = 0;
                WriteRstmHeader(writer);
                stream.Position = HeadBlockOffset;
                WriteHeadBlock(writer);
                stream.Position = AdpcBlockOffset;
                WriteAdpcBlock(writer);
                stream.Position = DataBlockOffset;
                WriteDataBlock(writer);
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
            writer.Write(HeadBlockOffset);
            writer.Write(HeadBlockSize);
            writer.Write(AdpcBlockOffset);
            writer.Write(AdpcBlockSize);
            writer.Write(DataBlockOffset);
            writer.Write(DataBlockSize);
        }

        private void WriteHeadBlock(BinaryWriter writer)
        {
            writer.WriteUTF8("HEAD");
            writer.Write(HeadBlockSize);

            writer.Write(OffsetMarker);
            writer.Write(HeadBlockTableSize); //Block 1 offset
            writer.Write(OffsetMarker);
            writer.Write(HeadBlockTableSize + HeadBlock1Size); //Block 2 offset
            writer.Write(OffsetMarker);
            writer.Write(HeadBlockTableSize + HeadBlock1Size + HeadBlock2Size); //Block 3 offset

            WriteHeadBlock1(writer);
            WriteHeadBlock2(writer);
            WriteHeadBlock3(writer);
        }

        private void WriteHeadBlock1(BinaryWriter writer)
        {
            writer.Write((byte)Codec);
            writer.Write(AudioFormat.Looping);
            writer.Write((byte)ChannelCount);
            writer.Write((byte)0); //padding
            writer.Write((ushort)AudioFormat.SampleRate);
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

        private void WriteHeadBlock2(BinaryWriter writer)
        {
            writer.Write((byte)TrackCount);
            writer.Write((byte)(HeaderType == BrstmTrackType.Short ? 0 : 1));
            writer.Write((short)0);

            int baseOffset = HeadBlockTableSize + HeadBlock1Size + 4;
            int offsetTableSize = TrackCount * 8;

            for (int i = 0; i < TrackCount; i++)
            {
                writer.Write(HeaderType == BrstmTrackType.Short ? OffsetMarker : OffsetMarkerV2);
                writer.Write(baseOffset + offsetTableSize + TrackInfoSize * i);
            }

            for (int i = 0; i < TrackCount; i++)
            {
                AudioTrack track = Tracks[i];
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

        private void WriteHeadBlock3(BinaryWriter writer)
        {
            writer.Write((byte)ChannelCount);
            writer.Write((byte)0); //padding
            writer.Write((short)0); //padding

            int baseOffset = HeadBlockTableSize + HeadBlock1Size + HeadBlock2Size + 4;
            int offsetTableSize = ChannelCount * 8;

            for (int i = 0; i < ChannelCount; i++)
            {
                writer.Write(OffsetMarker);
                writer.Write(baseOffset + offsetTableSize + ChannelInfoSize * i);
            }

            for (int i = 0; i < ChannelCount; i++)
            {
                writer.Write(OffsetMarker);
                if (Codec != NwCodec.GcAdpcm)
                {
                    writer.Write(0);
                    continue;
                }

                GcAdpcmChannel channel = Adpcm.Channels[i];
                GcAdpcmContext loopContext = Adpcm.Looping ? channel.LoopContext : channel.StartContext;

                writer.Write(baseOffset + offsetTableSize + ChannelInfoSize * i + 8);
                writer.Write(channel.Coefs.ToByteArray(Endianness.BigEndian));
                writer.Write(channel.Gain);
                channel.StartContext.Write(writer);
                loopContext.Write(writer);
                writer.Write((short)0);
            }
        }

        private void WriteAdpcBlock(BinaryWriter writer)
        {
            if (Codec != NwCodec.GcAdpcm) return;
            writer.WriteUTF8("ADPC");
            writer.Write(AdpcBlockSize);

            byte[] table = Adpcm.BuildSeekTable(SeekTableEntryCount, Endianness.BigEndian);

            writer.Write(table);
        }

        private void WriteDataBlock(BinaryWriter writer)
        {
            writer.WriteUTF8("DATA");
            writer.Write(DataBlockSize);
            writer.Write(0x18);

            writer.BaseStream.Position = AudioDataOffset;

            byte[][] channels = null;
            switch (Codec)
            {
                case NwCodec.GcAdpcm:
                    channels = Adpcm.Channels.Select(x => x.GetAdpcmAudio()).ToArray();
                    break;
                case NwCodec.Pcm16Bit:
                    channels = Pcm16.Channels.Select(x => x.ToByteArray(Endianness.BigEndian)).ToArray();
                    break;
                case NwCodec.Pcm8Bit:
                    channels = Pcm8.Channels;
                    break;
            }

            channels.Interleave(writer.BaseStream, InterleaveSize, AudioDataSize);
        }
    }
}