using System.Collections.Generic;
using System.IO;
using System.Linq;
using VGAudio.Formats;
using VGAudio.Formats.GcAdpcm;
using VGAudio.Utilities;
using static VGAudio.Containers.Bxstm.Common;
using static VGAudio.Utilities.Helpers;

namespace VGAudio.Containers.Bxstm
{
    internal class BCFstmWriter
    {
        private GcAdpcmFormat Adpcm { get; set; }
        private Pcm16Format Pcm16 { get; set; }
        private Pcm8SignedFormat Pcm8 { get; set; }
        private IAudioFormat AudioFormat { get; set; }

        public int FileSize => HeaderSize + InfoChunkSize + SeekChunkSize + DataChunkSize;

        public BcstmConfiguration BcstmConfig { get; } = new BcstmConfiguration();
        public BfstmConfiguration BfstmConfig { get; } = new BfstmConfiguration();
        public BxstmConfiguration Configuration => Type == BCFstmType.Bcstm ? (BxstmConfiguration)BcstmConfig : BfstmConfig;

        public BCFstmType Type { get; }
        public Endianness Endianness => Configuration.Endianness ?? GetTypeEndianness(Type);
        private int SampleCount => AudioFormat.Looping ? LoopEnd : AudioFormat.SampleCount;
        private int ChannelCount => AudioFormat.ChannelCount;
        private int TrackCount => Tracks?.Count ?? 0;
        private List<AudioTrack> Tracks { get; set; }

        private int LoopStart => AudioFormat.LoopStart;
        private int LoopEnd => AudioFormat.LoopEnd;

        private BxstmCodec Codec => Configuration.Codec;
        private int AudioDataOffset => DataChunkOffset + 0x20;

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

        private bool InfoPart1Extra => Type == BCFstmType.Bcstm ? BcstmConfig.InfoPart1Extra : true;
        private bool IncludeUnalignedLoopPoints => Type == BCFstmType.Bfstm ? BfstmConfig.IncludeUnalignedLoopPoints : false;
        private bool IncludeTrackInformation => Type == BCFstmType.Bcstm ? BcstmConfig.IncludeTrackInformation : false;

        private int InfoChunkOffset => HeaderSize;
        private int InfoChunkSize => GetNextMultiple(InfoChunkHeaderSize + InfoChunkTableSize +
            InfoChunk1Size + InfoChunk2Size + InfoChunk3Size, 0x20);
        private int InfoChunkHeaderSize => 8;
        private int InfoChunkTableSize => 8 * 3;
        private int InfoChunk1Size => 0x38 + (!InfoPart1Extra ? 0 : 0xc) + (!IncludeUnalignedLoopPoints ? 0 : 8);
        private int InfoChunk2Size => IncludeTrackInformation ? 4 + 8 * TrackCount : 0;
        private int InfoChunk3Size => (4 + 8 * ChannelCount) +
            (IncludeTrackInformation ? 0x14 * TrackCount : 0) +
            8 * ChannelCount +
            ChannelInfoSize * ChannelCount;

        private int ChannelInfoSize => Codec == BxstmCodec.Adpcm ? 0x2e : 0;

        private int SeekChunkOffset => Codec == BxstmCodec.Adpcm ? HeaderSize + InfoChunkSize : 0;
        private int SeekChunkSize => Codec == BxstmCodec.Adpcm ? GetNextMultiple(8 + SeekTableEntryCount * ChannelCount * BytesPerSeekTableEntry, 0x20) : 0;

        private int DataChunkOffset => HeaderSize + InfoChunkSize + SeekChunkSize;
        private int DataChunkSize => 0x20 + AudioDataSize * ChannelCount;

        public BCFstmWriter(BcstmConfiguration configuration)
        {
            BcstmConfig = configuration;
            Type = BCFstmType.Bcstm;
        }

        public BCFstmWriter(BfstmConfiguration configuration)
        {
            BfstmConfig = configuration;
            Type = BCFstmType.Bfstm;
        }

        internal void SetupWriter(AudioData audio)
        {
            if (Codec == BxstmCodec.Adpcm)
            {
                Adpcm = audio.GetFormat<GcAdpcmFormat>();
                
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
            else if (Codec == BxstmCodec.Pcm16Bit)
            {
                Pcm16 = audio.GetFormat<Pcm16Format>();
                AudioFormat = Pcm16;
                Tracks = Pcm16.Tracks;
            }
            else if (Codec == BxstmCodec.Pcm8Bit)
            {
                Pcm8 = audio.GetFormat<Pcm8SignedFormat>();
                AudioFormat = Pcm8;
                Tracks = Pcm8.Tracks;
            }
        }

        public void WriteStream(Stream stream)
        {
            using (BinaryWriter writer = GetBinaryWriter(stream, Endianness))
            {
                stream.Position = 0;
                WriteHeader(writer);
                stream.Position = InfoChunkOffset;
                WriteInfoChunk(writer);
                stream.Position = SeekChunkOffset;
                WriteSeekChunk(writer);
                stream.Position = DataChunkOffset;
                WriteDataChunk(writer);
            }
        }

        private int GetVersion(BCFstmType type)
        {
            if (type == BCFstmType.Bfstm)
            {
                return IncludeUnalignedLoopPoints ? 4 : 3;
            }

            //All BCSTM files I've seen follow this pattern except for Kingdom Hearts 3D
            if (IncludeTrackInformation && InfoPart1Extra)
                return 0x201;

            if (!IncludeTrackInformation && InfoPart1Extra)
                return 0x202;

            return 0x200;
        }

        private void WriteHeader(BinaryWriter writer)
        {
            writer.WriteUTF8(Type == BCFstmType.Bcstm ? "CSTM" : "FSTM");
            writer.Write((ushort)0xfeff); //Endianness
            writer.Write((short)HeaderSize);
            writer.Write(GetVersion(Type) << 16);
            writer.Write(FileSize);

            writer.Write((short)(Codec == BxstmCodec.Adpcm ? 3 : 2)); // NumEntries
            writer.Write((short)0);
            writer.Write((short)0x4000);
            writer.Write((short)0);
            writer.Write(InfoChunkOffset);
            writer.Write(InfoChunkSize);
            if (Codec == BxstmCodec.Adpcm)
            {
                writer.Write((short)0x4001);
                writer.Write((short)0);
                writer.Write(SeekChunkOffset);
                writer.Write(SeekChunkSize);
            }
            writer.Write((short)0x4002);
            writer.Write((short)0);
            writer.Write(DataChunkOffset);
            writer.Write(DataChunkSize);
        }

        private void WriteInfoChunk(BinaryWriter writer)
        {
            writer.WriteUTF8("INFO");
            writer.Write(InfoChunkSize);

            int headerTableSize = 8 * 3;

            writer.Write((short)0x4100);
            writer.Write((short)0);
            writer.Write(headerTableSize);
            if (IncludeTrackInformation)
            {
                writer.Write((short)0x0101);
                writer.Write((short)0);
                writer.Write(headerTableSize + InfoChunk1Size);
            }
            else
            {
                writer.Write(0);
                writer.Write(-1);
            }
            writer.Write((short)0x0101);
            writer.Write((short)0);
            writer.Write(headerTableSize + InfoChunk1Size + InfoChunk2Size);

            WriteInfoChunk1(writer);
            WriteInfoChunk2(writer);
            WriteInfoChunk3(writer);
        }

        private void WriteInfoChunk1(BinaryWriter writer)
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
            writer.Write((short)0x1f00);
            writer.Write((short)0);
            writer.Write(0x18);

            if (InfoPart1Extra)
            {
                writer.Write((short)0x0100);
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

        private void WriteInfoChunk2(BinaryWriter writer)
        {
            if (!IncludeTrackInformation) return;

            int trackTableSize = 4 + 8 * TrackCount;
            int channelTableSize = 4 + 8 * ChannelCount;
            int trackSize = 0x14;

            writer.Write(TrackCount);

            for (int i = 0; i < TrackCount; i++)
            {
                writer.Write((short)0x4101);
                writer.Write((short)0);
                writer.Write(trackTableSize + channelTableSize + trackSize * i);
            }
        }

        private void WriteInfoChunk3(BinaryWriter writer)
        {
            int channelTableSize = 4 + 8 * ChannelCount;
            int trackTableSize = IncludeTrackInformation ? 0x14 * TrackCount : 0;

            writer.Write(ChannelCount);
            for (int i = 0; i < ChannelCount; i++)
            {
                writer.Write((short)0x4102);
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
                    writer.Write(0x0100);
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
                if (Codec == BxstmCodec.Adpcm)
                {
                    writer.Write((short)0x0300);
                    writer.Write((short)0);
                    writer.Write(channelTable2Size - 8 * i + ChannelInfoSize * i);
                }
                else
                {
                    writer.Write(0);
                    writer.Write(-1);
                }
            }

            if (Codec != BxstmCodec.Adpcm) { return; }

            foreach (var channel in Adpcm.Channels)
            {
                writer.Write(channel.Coefs.ToByteArray(Endianness));
                writer.Write(channel.PredScale);
                writer.Write(channel.Hist1);
                writer.Write(channel.Hist2);
                writer.Write(Adpcm.Looping ? channel.LoopPredScale : channel.PredScale);
                writer.Write(Adpcm.Looping ? channel.LoopHist1 : (short)0);
                writer.Write(Adpcm.Looping ? channel.LoopHist2 : (short)0);
                writer.Write(channel.Gain);
            }
        }

        private void WriteSeekChunk(BinaryWriter writer)
        {
            if (Codec != BxstmCodec.Adpcm) return;
            writer.WriteUTF8("SEEK");
            writer.Write(SeekChunkSize);

            var table = Adpcm.BuildSeekTable(SeekTableEntryCount, Endianness.LittleEndian);

            writer.Write(table);
        }

        private void WriteDataChunk(BinaryWriter writer)
        {
            writer.WriteUTF8("DATA");
            writer.Write(DataChunkSize);

            writer.BaseStream.Position = AudioDataOffset;

            byte[][] channels = null;
            switch (Codec)
            {
                case BxstmCodec.Adpcm:
                    channels = Adpcm.Channels.Select(x => x.GetAdpcmAudio()).ToArray();
                    break;
                case BxstmCodec.Pcm16Bit:
                    channels = Pcm16.Channels.Select(x => x.ToByteArray(Endianness)).ToArray();
                    break;
                case BxstmCodec.Pcm8Bit:
                    channels = Pcm8.Channels;
                    break;
            }

            channels.Interleave(writer.BaseStream, InterleaveSize, AudioDataSize);
        }

        public enum BCFstmType
        {
            Bcstm,
            Bfstm
        }

        private static Endianness GetTypeEndianness(BCFstmType type) =>
            type == BCFstmType.Bcstm ? Endianness.LittleEndian : Endianness.BigEndian;
    }
}