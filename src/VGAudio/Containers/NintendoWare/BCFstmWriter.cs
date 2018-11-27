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
using static VGAudio.Utilities.Helpers;

namespace VGAudio.Containers.NintendoWare
{
    public class BCFstmWriter : AudioWriter<BCFstmWriter, BxstmConfiguration>
    {
        private GcAdpcmFormat Adpcm { get; set; }
        private Pcm16Format Pcm16 { get; set; }
        private Pcm8SignedFormat Pcm8 { get; set; }
        private IAudioFormat AudioFormat { get; set; }

        protected override int FileSize => HeaderSize + InfoBlockSize + SeekBlockSize + DataBlockSize;

        public NwTarget Type { get; }
        public Endianness Endianness => Configuration.Endianness ?? GetTypeEndianness(Type);
        private int SampleCount => AudioFormat.Looping ? LoopEnd : AudioFormat.SampleCount;
        private int ChannelCount => AudioFormat.ChannelCount;
        private int TrackCount => Tracks?.Count ?? 0;
        private List<AudioTrack> Tracks { get; set; }

        private int LoopStart => AudioFormat.LoopStart;
        private int LoopEnd => AudioFormat.LoopEnd;

        private NwCodec Codec => Configuration.Codec;
        private int AudioDataOffset => DataBlockOffset + 0x20;

        /// <summary>
        /// Size of a single channel's ADPCM audio data with padding when written to a file
        /// </summary>
        private int AudioDataSize => GetNextMultiple(SamplesToBytes(SampleCount, Codec), 0x20);

        private int SamplesPerInterleave => Configuration.SamplesPerInterleave;
        private int InterleaveSize => SamplesToBytes(SamplesPerInterleave, Codec);
        private int InterleaveCount => SampleCount.DivideByRoundUp(SamplesPerInterleave);

        private int LastBlockSamples => SampleCount - ((InterleaveCount - 1) * SamplesPerInterleave);
        private int LastBlockSizeWithoutPadding => SamplesToBytes(LastBlockSamples, Codec);
        private int LastBlockSize => GetNextMultiple(LastBlockSizeWithoutPadding, 0x20);

        private int SamplesPerSeekTableEntry => Configuration.SamplesPerSeekTableEntry;
        private int BytesPerSeekTableEntry => 4;
        private int SeekTableEntryCount => SampleCount.DivideByRoundUp(SamplesPerSeekTableEntry);

        private static int HeaderSize => 0x40;

        private NwVersion Version => Configuration.Version ??
                                     (Type == NwTarget.Ctr ? DefaultBcstmVersion : DefaultBfstmVersion);
        private static NwVersion DefaultBcstmVersion { get; } = new NwVersion(2, 1);
        private static NwVersion DefaultBfstmVersion { get; } = new NwVersion(0, 3);
        private bool IncludeRegionInfo => IncludeRegionInfo(Version);
        private bool IncludeUnalignedLoopPoints => IncludeUnalignedLoop(Version);
        private bool IncludeTrackInformation => IncludeTrackInfo(Version);

        private int InfoBlockOffset => HeaderSize;
        private int InfoBlockSize => GetNextMultiple(InfoBlockHeaderSize + InfoBlockTableSize +
            InfoBlock1Size + InfoBlock2Size + InfoBlock3Size, 0x20);
        private int InfoBlockHeaderSize => 8;
        private int InfoBlockTableSize => 8 * 3;
        private int InfoBlock1Size => 0x38 + (!IncludeRegionInfo ? 0 : 0xc) + (!IncludeUnalignedLoopPoints ? 0 : 8);
        private int InfoBlock2Size => IncludeTrackInformation ? 4 + 8 * TrackCount : 0;
        private int InfoBlock3Size => (4 + 8 * ChannelCount) +
            (IncludeTrackInformation ? 0x14 * TrackCount : 0) +
            8 * ChannelCount +
            ChannelInfoSize * ChannelCount;

        private int ChannelInfoSize => Codec == NwCodec.GcAdpcm ? 0x2e : 0;

        private int SeekBlockOffset => Codec == NwCodec.GcAdpcm ? HeaderSize + InfoBlockSize : 0;
        private int SeekBlockSize => Codec == NwCodec.GcAdpcm ? GetNextMultiple(8 + SeekTableEntryCount * ChannelCount * BytesPerSeekTableEntry, 0x20) : 0;

        private int DataBlockOffset => HeaderSize + InfoBlockSize + SeekBlockSize;
        private int DataBlockSize => 0x20 + AudioDataSize * ChannelCount;

        public BCFstmWriter(NwTarget type)
        {
            Type = type;
        }

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
            using (BinaryWriter writer = GetBinaryWriter(stream, Endianness))
            {
                stream.Position = 0;
                WriteHeader(writer);
                stream.Position = InfoBlockOffset;
                WriteInfoBlock(writer);
                stream.Position = SeekBlockOffset;
                WriteSeekBlock(writer);
                stream.Position = DataBlockOffset;
                WriteDataBlock(writer);
            }
        }

        private int GetVersion(NwTarget type)
        {
            if (type == NwTarget.Cafe)
            {
                return IncludeUnalignedLoopPoints ? 4 : 3;
            }

            //All BCSTM files I've seen follow this pattern except for Kingdom Hearts 3D
            if (IncludeTrackInformation && IncludeRegionInfo)
                return 0x201;

            if (!IncludeTrackInformation && IncludeRegionInfo)
                return 0x202;

            return 0x200;
        }

        private void WriteHeader(BinaryWriter writer)
        {
            writer.WriteUTF8(Type == NwTarget.Ctr ? "CSTM" : "FSTM");
            writer.Write((ushort)0xfeff); //Endianness
            writer.Write((short)HeaderSize);
            writer.Write(GetVersion(Type) << 16);
            writer.Write(FileSize);

            writer.Write((short)(Codec == NwCodec.GcAdpcm ? 3 : 2)); // NumEntries
            writer.Write((short)0);
            writer.Write((short)ReferenceType.StreamInfoBlock);
            writer.Write((short)0);
            writer.Write(InfoBlockOffset);
            writer.Write(InfoBlockSize);
            if (Codec == NwCodec.GcAdpcm)
            {
                writer.Write((short)ReferenceType.StreamSeekBlock);
                writer.Write((short)0);
                writer.Write(SeekBlockOffset);
                writer.Write(SeekBlockSize);
            }
            writer.Write((short)ReferenceType.StreamDataBlock);
            writer.Write((short)0);
            writer.Write(DataBlockOffset);
            writer.Write(DataBlockSize);
        }

        private void WriteInfoBlock(BinaryWriter writer)
        {
            writer.WriteUTF8("INFO");
            writer.Write(InfoBlockSize);

            int headerTableSize = 8 * 3;

            writer.Write((short)ReferenceType.StreamInfo);
            writer.Write((short)0);
            writer.Write(headerTableSize);
            if (IncludeTrackInformation)
            {
                writer.Write((short)ReferenceType.ReferenceTable);
                writer.Write((short)0);
                writer.Write(headerTableSize + InfoBlock1Size);
            }
            else
            {
                writer.Write(0);
                writer.Write(-1);
            }
            writer.Write((short)ReferenceType.ReferenceTable);
            writer.Write((short)0);
            writer.Write(headerTableSize + InfoBlock1Size + InfoBlock2Size);

            WriteInfoBlock1(writer);
            WriteInfoBlock2(writer);
            WriteInfoBlock3(writer);
        }

        private void WriteInfoBlock1(BinaryWriter writer)
        {
            writer.Write((byte)Codec);
            writer.Write(AudioFormat.Looping);
            writer.Write((byte)ChannelCount);
            writer.Write((byte)0);
            writer.Write(AudioFormat.SampleRate);
            writer.Write(LoopStart);
            writer.Write(SampleCount);
            writer.Write(InterleaveCount);
            writer.Write(InterleaveSize);
            writer.Write(SamplesPerInterleave);
            writer.Write(LastBlockSizeWithoutPadding);
            writer.Write(LastBlockSamples);
            writer.Write(LastBlockSize);
            writer.Write(BytesPerSeekTableEntry);
            writer.Write(SamplesPerSeekTableEntry);
            writer.Write((short)ReferenceType.SampleData);
            writer.Write((short)0);
            writer.Write(0x18);

            if (IncludeRegionInfo)
            {
                writer.Write((short)ReferenceType.ByteTable);
                writer.Write((short)0);
                writer.Write(0);
                writer.Write(-1);
            }

            if (IncludeUnalignedLoopPoints)
            {
                writer.Write(Adpcm.LoopStart);
                writer.Write(Adpcm.LoopEnd);
            }
        }

        private void WriteInfoBlock2(BinaryWriter writer)
        {
            if (!IncludeTrackInformation) return;

            int trackTableSize = 4 + 8 * TrackCount;
            int channelTableSize = 4 + 8 * ChannelCount;
            int trackSize = 0x14;

            writer.Write(TrackCount);

            for (int i = 0; i < TrackCount; i++)
            {
                writer.Write((short)ReferenceType.TrackInfo);
                writer.Write((short)0);
                writer.Write(trackTableSize + channelTableSize + trackSize * i);
            }
        }

        private void WriteInfoBlock3(BinaryWriter writer)
        {
            int channelTableSize = 4 + 8 * ChannelCount;
            int trackTableSize = IncludeTrackInformation ? 0x14 * TrackCount : 0;

            writer.Write(ChannelCount);
            for (int i = 0; i < ChannelCount; i++)
            {
                writer.Write((short)ReferenceType.ChannelInfo);
                writer.Write((short)0);
                writer.Write(channelTableSize + trackTableSize + 8 * i);
            }

            if (IncludeTrackInformation && Tracks != null)
            {
                foreach (AudioTrack track in Tracks)
                {
                    writer.Write((byte)track.Volume);
                    writer.Write((byte)track.Panning);
                    writer.Write((short)0);
                    writer.Write((short)ReferenceType.ByteTable);
                    writer.Write((short)0);
                    writer.Write(0xc);
                    writer.Write(track.ChannelCount);
                    writer.Write((byte)track.ChannelLeft);
                    writer.Write((byte)track.ChannelRight);
                    writer.Write((short)0);
                }
            }

            int channelTable2Size = 8 * ChannelCount;
            for (int i = 0; i < ChannelCount; i++)
            {
                if (Codec == NwCodec.GcAdpcm)
                {
                    writer.Write((short)ReferenceType.GcAdpcmInfo);
                    writer.Write((short)0);
                    writer.Write(channelTable2Size - 8 * i + ChannelInfoSize * i);
                }
                else
                {
                    writer.Write(0);
                    writer.Write(-1);
                }
            }

            if (Codec != NwCodec.GcAdpcm) { return; }

            foreach (GcAdpcmChannel channel in Adpcm.Channels)
            {
                GcAdpcmContext loopContext = Adpcm.Looping ? channel.LoopContext : channel.StartContext;

                writer.Write(channel.Coefs.ToByteArray(Endianness));
                channel.StartContext.Write(writer);
                loopContext.Write(writer);
                writer.Write((short)0);
            }
        }

        private void WriteSeekBlock(BinaryWriter writer)
        {
            if (Codec != NwCodec.GcAdpcm) return;
            writer.WriteUTF8("SEEK");
            writer.Write(SeekBlockSize);

            byte[] table = Adpcm.BuildSeekTable(SeekTableEntryCount, Endianness.LittleEndian);

            writer.Write(table);
        }

        private void WriteDataBlock(BinaryWriter writer)
        {
            writer.WriteUTF8("DATA");
            writer.Write(DataBlockSize);

            writer.BaseStream.Position = AudioDataOffset;

            byte[][] channels = null;
            switch (Codec)
            {
                case NwCodec.GcAdpcm:
                    channels = Adpcm.Channels.Select(x => x.GetAdpcmAudio()).ToArray();
                    break;
                case NwCodec.Pcm16Bit:
                    channels = Pcm16.Channels.Select(x => x.ToByteArray(Endianness)).ToArray();
                    break;
                case NwCodec.Pcm8Bit:
                    channels = Pcm8.Channels;
                    break;
            }

            channels.Interleave(writer.BaseStream, InterleaveSize, AudioDataSize);
        }

        private static Endianness GetTypeEndianness(NwTarget type) =>
            type == NwTarget.Ctr ? Endianness.LittleEndian : Endianness.BigEndian;
    }
}