using System.IO;
using System.Linq;
using System.Text;
using VGAudio.Formats;
using VGAudio.Formats.GcAdpcm;
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
                    Hist1 = structure.Channels[c].Hist1,
                    Hist2 = structure.Channels[c].Hist2
                };

                channelBuilder.WithLoop(structure.Looping, structure.LoopStart, structure.SampleCount)
                    .WithLoopContext(structure.LoopStart, structure.Channels[c].LoopPredScale,
                        structure.Channels[c].LoopHist1, structure.Channels[c].LoopHist2);

                if (structure.SeekTable != null)
                {
                    channelBuilder.WithSeekTable(structure.SeekTable[c], structure.SamplesPerSeekTableEntry);
                }

                channels[c] = channelBuilder.Build();
            }

            return new GcAdpcmFormatBuilder(channels, structure.SampleRate)
                .WithTracks(structure.Tracks)
                .Loop(structure.Looping, structure.LoopStart, structure.SampleCount)
                .Build();
        }

        private static Pcm16Format ToPcm16Stream(BxstmStructure structure)
        {
            short[][] channels = structure.AudioData.Select(x => x.ToShortArray(structure.Endianness)).ToArray();
            return new Pcm16Format.Builder(channels, structure.SampleRate)
                .WithTracks(structure.Tracks)
                .Loop(structure.Looping, structure.LoopStart, structure.SampleCount)
                .Build();
        }

        private static Pcm8SignedFormat ToPcm8Stream(BxstmStructure structure)
        {
            return new Pcm8Format.Builder(structure.AudioData, structure.SampleRate, true)
                .WithTracks(structure.Tracks)
                .Loop(structure.Looping, structure.LoopStart, structure.SampleCount)
                .Build() as Pcm8SignedFormat;
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
