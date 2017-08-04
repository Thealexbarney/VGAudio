using System.IO;
using System.Linq;
using VGAudio.Containers.Bxstm.Structures;
using VGAudio.Formats;
using VGAudio.Formats.GcAdpcm;
using VGAudio.Formats.Pcm16;
using VGAudio.Formats.Pcm8;
using VGAudio.Utilities;

namespace VGAudio.Containers.Bxstm
{
    internal static class Common
    {
        public static int SamplesToBytes(int sampleCount, BxstmCodec codec)
        {
            switch (codec)
            {
                case BxstmCodec.Adpcm:
                    return GcAdpcmHelpers.SampleCountToByteCount(sampleCount);
                case BxstmCodec.Pcm16Bit:
                    return sampleCount * 2;
                case BxstmCodec.Pcm8Bit:
                    return sampleCount;
                default:
                    return 0;
            }
        }

        public static int BytesToSamples(int byteCount, BxstmCodec codec)
        {
            switch (codec)
            {
                case BxstmCodec.Adpcm:
                    return GcAdpcmHelpers.NibbleCountToSampleCount(byteCount * 2);
                case BxstmCodec.Pcm16Bit:
                    return byteCount / 2;
                case BxstmCodec.Pcm8Bit:
                    return byteCount;
                default:
                    return 0;
            }
        }

        public static IAudioFormat ToAudioStream(BxstmStructure structure)
        {
            switch (structure.StreamInfo.Codec)
            {
                case BxstmCodec.Adpcm:
                    return ToAdpcmStream(structure);
                case BxstmCodec.Pcm16Bit:
                    return ToPcm16Stream(structure);
                case BxstmCodec.Pcm8Bit:
                    return ToPcm8Stream(structure);
                default:
                    return null;
            }
        }

        private static GcAdpcmFormat ToAdpcmStream(BxstmStructure structure)
        {
            var info = structure.StreamInfo;
            var channels = new GcAdpcmChannel[info.ChannelCount];

            for (int c = 0; c < channels.Length; c++)
            {
                var channelBuilder = new GcAdpcmChannelBuilder(structure.AudioData[c], structure.Channels[c].Coefs, info.SampleCount)
                {
                    Gain = structure.Channels[c].Gain,
                    StartContext = structure.Channels[c].Start
                };

                channelBuilder.WithLoop(info.Looping, info.LoopStart, info.SampleCount)
                    .WithLoopContext(info.LoopStart, structure.Channels[c].Loop.PredScale,
                        structure.Channels[c].Loop.Hist1, structure.Channels[c].Loop.Hist2);

                if (structure.SeekTable != null)
                {
                    channelBuilder.WithSeekTable(structure.SeekTable[c], info.SamplesPerSeekTableEntry);
                }

                channels[c] = channelBuilder.Build();
            }

            return new GcAdpcmFormatBuilder(channels, info.SampleRate)
                .WithTracks(structure.Tracks)
                .WithLoop(info.Looping, info.LoopStart, info.SampleCount)
                .Build();
        }

        private static Pcm16Format ToPcm16Stream(BxstmStructure structure)
        {
            var info = structure.StreamInfo;
            short[][] channels = structure.AudioData.Select(x => x.ToShortArray(structure.Endianness)).ToArray();
            return new Pcm16FormatBuilder(channels, info.SampleRate)
                .WithTracks(structure.Tracks)
                .WithLoop(info.Looping, info.LoopStart, info.SampleCount)
                .Build();
        }

        private static Pcm8SignedFormat ToPcm8Stream(BxstmStructure structure)
        {
            var info = structure.StreamInfo;
            return new Pcm8FormatBuilder(structure.AudioData, info.SampleRate, true)
                .WithTracks(structure.Tracks)
                .WithLoop(info.Looping, info.LoopStart, info.SampleCount)
                .Build() as Pcm8SignedFormat;
        }

        public static void ReadDataChunk2(BinaryReader reader, BxstmStructure structure, bool readAudioData)
        {
            SizedReference reference = structure.Sections.FirstOrDefault(x => x.Type == ReferenceType.StreamDataBlock) ??
                                       throw new InvalidDataException("File has no DATA chunk");

            var info = structure.StreamInfo;

            reader.BaseStream.Position = reference.AbsoluteOffset;

            if (reader.ReadUTF8(4) != "DATA")
            {
                throw new InvalidDataException("Unknown or invalid DATA chunk");
            }
            structure.DataChunkSize = reader.ReadInt32();

            if (structure.DataChunkSize != reference.Size)
            {
                throw new InvalidDataException("DATA chunk size in main header doesn't match size in DATA header");
            }

            if (!readAudioData) return;

            int audioOffset = reference.AbsoluteOffset + info.AudioReference.Offset + 8;
            reader.BaseStream.Position = audioOffset;
            int audioDataLength = structure.DataChunkSize - (audioOffset - reference.AbsoluteOffset);
            int outputSize = SamplesToBytes(info.SampleCount, info.Codec);

            structure.AudioData = reader.BaseStream.DeInterleave(audioDataLength, info.InterleaveSize,
                info.ChannelCount, outputSize);
        }

        public static void ReadDataChunk(BinaryReader reader, BxstmStructure structure, bool readAudioData)
        {
            reader.BaseStream.Position = structure.DataChunkOffset;
            var info = structure.StreamInfo;

            if (reader.ReadUTF8(4) != "DATA")
            {
                throw new InvalidDataException("Unknown or invalid DATA chunk");
            }
            structure.DataChunkSize = reader.ReadInt32();

            if (structure.DataChunkSizeHeader != structure.DataChunkSize)
            {
                throw new InvalidDataException("DATA chunk size in header doesn't match size in DATA header");
            }

            if (!readAudioData) return;

            reader.BaseStream.Position = info.AudioDataOffset;
            int audioDataLength = structure.DataChunkSize - (info.AudioDataOffset - structure.DataChunkOffset);
            int outputSize = SamplesToBytes(info.SampleCount, info.Codec);

            structure.AudioData = reader.BaseStream.DeInterleave(audioDataLength, info.InterleaveSize,
                info.ChannelCount, outputSize);
        }
        
        private static NwVersion IncludeTrackInfoBfstm { get; } = new NwVersion(0, 2, 0, 0);
        private static NwVersion IncludeUnalignedLoopBfstm { get; } = new NwVersion(0, 4, 0, 0);
        private static NwVersion IncludeChecksumBfstm { get; } = new NwVersion(0, 5, 0, 0);

        //This one is weird. Some version 2.1 BCSTM files have the region offset, and some don't.
        private static NwVersion IncludeRegionBcstm { get; } = new NwVersion(2, 1, 0, 0);
        private static NwVersion IncludeTrackInfoBcstm { get; } = new NwVersion(2, 1, 0, 0);
        private static NwVersion IncludeUnalignedLoopBcstm { get; } = new NwVersion(2, 3, 0, 0);

        public static bool IncludeTrackInfo(NwVersion version)
        {
            return version.Major == 0 && version.Version <= IncludeTrackInfoBfstm.Version ||
                   version.Major >= 2 && version.Version <= IncludeTrackInfoBcstm.Version;
        }

        public static bool IncludeRegionInfo(NwVersion version)
        {
            return version.Major >= 2 && version.Version >= IncludeRegionBcstm.Version ||
                   version.Major == 0;
        }

        public static bool IncludeUnalignedLoop(NwVersion version)
        {
            return version.Major == 0 && version.Version >= IncludeUnalignedLoopBfstm.Version ||
                   version.Major >= 2 && version.Version >= IncludeUnalignedLoopBcstm.Version;
        }

        public static bool IncludeChecksum(NwVersion version)
        {
            return version.Major == 0 && version.Version >= IncludeChecksumBfstm.Version;
        }
    }
}
