using System.IO;
using System.Linq;
using System.Text;
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
            switch (structure.Codec)
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
            var channels = new GcAdpcmChannel[structure.ChannelCount];

            for (int c = 0; c < channels.Length; c++)
            {
                var channelBuilder = new GcAdpcmChannelBuilder(structure.AudioData[c], structure.Channels[c].Coefs, structure.SampleCount)
                {
                    Gain = structure.Channels[c].Gain,
                    StartContext = structure.Channels[c].Start
                };

                channelBuilder.WithLoop(structure.Looping, structure.LoopStart, structure.SampleCount)
                    .WithLoopContext(structure.LoopStart, structure.Channels[c].Loop.PredScale,
                        structure.Channels[c].Loop.Hist1, structure.Channels[c].Loop.Hist2);

                if (structure.SeekTable != null)
                {
                    channelBuilder.WithSeekTable(structure.SeekTable[c], structure.SamplesPerSeekTableEntry);
                }

                channels[c] = channelBuilder.Build();
            }

            return new GcAdpcmFormatBuilder(channels, structure.SampleRate)
                .WithTracks(structure.Tracks)
                .WithLoop(structure.Looping, structure.LoopStart, structure.SampleCount)
                .Build();
        }

        private static Pcm16Format ToPcm16Stream(BxstmStructure structure)
        {
            short[][] channels = structure.AudioData.Select(x => x.ToShortArray(structure.Endianness)).ToArray();
            return new Pcm16FormatBuilder(channels, structure.SampleRate)
                .WithTracks(structure.Tracks)
                .WithLoop(structure.Looping, structure.LoopStart, structure.SampleCount)
                .Build();
        }

        private static Pcm8SignedFormat ToPcm8Stream(BxstmStructure structure)
        {
            return new Pcm8FormatBuilder(structure.AudioData, structure.SampleRate, true)
                .WithTracks(structure.Tracks)
                .WithLoop(structure.Looping, structure.LoopStart, structure.SampleCount)
                .Build() as Pcm8SignedFormat;
        }

        public static void ReadDataChunk2(BinaryReader reader, BxstmStructure structure, bool readAudioData)
        {
            SizedReference reference = structure.Sections.FirstOrDefault(x => x.Type == ReferenceType.StreamDataBlock) ??
                                       throw new InvalidDataException("File has no DATA chunk");

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

            int audioOffset = reference.AbsoluteOffset + structure.AudioOffset.Offset + 8;
            reader.BaseStream.Position = audioOffset;
            int audioDataLength = structure.DataChunkSize - (audioOffset - reference.AbsoluteOffset);
            int outputSize = SamplesToBytes(structure.SampleCount, structure.Codec);

            structure.AudioData = reader.BaseStream.DeInterleave(audioDataLength, structure.InterleaveSize,
                structure.ChannelCount, outputSize);
        }

        public static void ReadDataChunk(BinaryReader reader, BxstmStructure structure, bool readAudioData)
        {
            reader.BaseStream.Position = structure.DataChunkOffset;

            if (Encoding.UTF8.GetString(reader.ReadBytes(4), 0, 4) != "DATA")
            {
                throw new InvalidDataException("Unknown or invalid DATA chunk");
            }
            structure.DataChunkSize = reader.ReadInt32();

            if (structure.DataChunkSizeHeader != structure.DataChunkSize)
            {
                throw new InvalidDataException("DATA chunk size in header doesn't match size in DATA header");
            }

            if (!readAudioData) return;

            reader.BaseStream.Position = structure.AudioDataOffset;
            int audioDataLength = structure.DataChunkSize - (structure.AudioDataOffset - structure.DataChunkOffset);
            int outputSize = SamplesToBytes(structure.SampleCount, structure.Codec);

            structure.AudioData = reader.BaseStream.DeInterleave(audioDataLength, structure.InterleaveSize,
                structure.ChannelCount, outputSize);
        }
    }
}
