using System.IO;
using VGAudio.Codecs.CriAdx;
using VGAudio.Formats;
using VGAudio.Formats.CriAdx;
using VGAudio.Utilities;
using static VGAudio.Utilities.Helpers;

namespace VGAudio.Containers.Adx
{
    public class AdxReader : AudioReader<AdxReader, AdxStructure, AdxConfiguration>
    {
        public CriAdxKey EncryptionKey { get; set; }

        protected override AdxStructure ReadFile(Stream stream, bool readAudioData = true)
        {
            using (BinaryReader reader = GetBinaryReader(stream, Endianness.BigEndian))
            {
                if (reader.ReadUInt16() != 0x8000)
                {
                    throw new InvalidDataException("File doesn't have ADX signature (0x80 0x00)");
                }

                var structure = new AdxStructure();

                ReadHeader(reader, structure);

                if (readAudioData)
                {
                    reader.BaseStream.Position = structure.HeaderSize + 4;
                    ReadData(reader, structure);
                    structure.EncryptionKey = EncryptionKey ?? FindKey(structure);
                }

                return structure;
            }
        }

        protected override IAudioFormat ToAudioStream(AdxStructure structure)
        {
            if (structure.EncryptionKey != null)
            {
                CriAdxEncryption.EncryptDecrypt(structure.AudioData, structure.EncryptionKey, structure.Revision, structure.FrameSize);
            }

            var channels = new CriAdxChannel[structure.ChannelCount];

            for (int i = 0; i < structure.ChannelCount; i++)
            {
                channels[i] = new CriAdxChannel(structure.AudioData[i], structure.HistorySamples[i][0], structure.Version);
            }

            return new CriAdxFormatBuilder(channels, structure.SampleCount - structure.InsertedSamples, structure.SampleRate, structure.FrameSize, structure.HighpassFreq)
                .WithLoop(structure.Looping, structure.LoopStartSample - structure.InsertedSamples, structure.LoopEndSample - structure.InsertedSamples)
                .WithAlignmentSamples(structure.InsertedSamples)
                .WithEncodingType(structure.EncodingType)
                .Build();
        }

        protected override AdxConfiguration GetConfiguration(AdxStructure structure)
        {
            var configuration = new AdxConfiguration
            {
                FrameSize = structure.FrameSize,
                Type = structure.EncodingType,
                EncryptionType = structure.Revision,
                EncryptionKey = structure.EncryptionKey
            };
            return configuration;
        }

        private static void ReadHeader(BinaryReader reader, AdxStructure structure)
        {
            structure.HeaderSize = reader.ReadInt16();
            structure.EncodingType = (CriAdxType)reader.ReadByte();
            structure.FrameSize = reader.ReadByte();
            structure.BitDepth = reader.ReadByte();
            structure.ChannelCount = reader.ReadByte();
            structure.SampleRate = reader.ReadInt32();
            structure.SampleCount = reader.ReadInt32();
            structure.HighpassFreq = reader.ReadInt16();
            structure.Version = reader.ReadByte();
            structure.Revision = reader.ReadByte();
            structure.HistorySamples = CreateJaggedArray<short[][]>(structure.ChannelCount, 2);

            if (structure.Version >= 4)
            {
                reader.BaseStream.Position += 4;
                foreach (short[] history in structure.HistorySamples)
                {
                    history[0] = reader.ReadInt16();
                    history[1] = reader.ReadInt16();
                }

                //Header contains, at minimum, space for 2 channels' history samples, so skip if mono
                if (structure.ChannelCount == 1)
                {
                    reader.BaseStream.Position += 4;
                }
            }

            if (reader.BaseStream.Position + 24 > structure.HeaderSize) { return; }

            structure.InsertedSamples = reader.ReadInt16();
            structure.LoopCount = reader.ReadInt16();

            if (structure.LoopCount <= 0) { return; }

            structure.Looping = true;
            structure.LoopType = reader.ReadInt32();
            structure.LoopStartSample = reader.ReadInt32();
            structure.LoopStartByte = reader.ReadInt32();
            structure.LoopEndSample = reader.ReadInt32();
            structure.LoopEndByte = reader.ReadInt32();
        }

        private static void ReadData(BinaryReader reader, AdxStructure structure)
        {
            int audioOffset = structure.HeaderSize + 4;
            int frameCount = structure.SampleCount.DivideByRoundUp(structure.SamplesPerFrame);
            int audioSize = structure.FrameSize * frameCount * structure.ChannelCount;

            reader.BaseStream.Position = audioOffset;
            structure.AudioData = reader.BaseStream.DeInterleave(audioSize, structure.FrameSize, structure.ChannelCount);
        }

        private static CriAdxKey FindKey(AdxStructure structure)
        {
            return structure.Revision == 0
                ? null
                : CriAdxEncryption.FindKey(structure.AudioData, structure.Revision, structure.FrameSize);
        }
    }
}
