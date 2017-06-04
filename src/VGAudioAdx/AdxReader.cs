using System.IO;
using VGAudio.Containers.Adx;
using VGAudio.Formats;
using VGAudio.Utilities;
using static VGAudio.Utilities.Helpers;

// ReSharper disable once CheckNamespace
namespace VGAudio.Containers
{
    public class AdxReader : AudioReader<AdxReader, AdxStructure, AdxConfiguration>
    {
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
                    reader.BaseStream.Position = structure.CopyrightOffset + 4;
                    ReadData(reader, structure);
                }

                return structure;
            }
        }

        protected override IAudioFormat ToAudioStream(AdxStructure structure)
        {
            var channels = new CriAdxChannel[structure.ChannelCount];

            for (int i = 0; i < structure.ChannelCount; i++)
            {
                channels[i] = new CriAdxChannel(structure.AudioData[i], structure.HistorySamples[i][0], structure.Version);
            }

            return new CriAdxFormat.Builder(channels, structure.SampleCount, structure.SampleRate, structure.FrameSize, structure.HighpassFreq)
                .WithLoop(structure.Looping, structure.LoopStartSample, structure.LoopEndSample)
                .WithEncodingType(structure.EncodingType)
                .Build();
        }

        private static void ReadHeader(BinaryReader reader, AdxStructure structure)
        {
            structure.CopyrightOffset = reader.ReadInt16();
            structure.EncodingType = (AdxType)reader.ReadByte();
            structure.FrameSize = reader.ReadByte();
            structure.BitDepth = reader.ReadByte();
            structure.ChannelCount = reader.ReadByte();
            structure.SampleRate = reader.ReadInt32();
            structure.SampleCount = reader.ReadInt32();
            structure.HighpassFreq = reader.ReadInt16();
            structure.Version = reader.ReadByte();
            structure.Flags = reader.ReadByte();

            var history = new short[structure.ChannelCount][];
            for (int i = 0; i < history.Length; i++)
            {
                history[i] = structure.Version >= 4 ? new[] { reader.ReadInt16(), reader.ReadInt16() } : new short[2];
            }
            structure.HistorySamples = history;

            if (reader.BaseStream.Position + 24 > structure.CopyrightOffset) { return; }

            structure.AlignmentSamples = reader.ReadInt16();
            structure.LoopCount = reader.ReadInt16();

            if (structure.LoopCount <= 0) { return; }

            reader.BaseStream.Position += 4;
            structure.Looping = true;
            structure.LoopStartSample = reader.ReadInt32();
            structure.LoopStartByte = reader.ReadInt32();
            structure.LoopEndSample = reader.ReadInt32();
            structure.LoopEndByte = reader.ReadInt32();
        }

        private static void ReadData(BinaryReader reader, AdxStructure structure)
        {
            int audioOffset = structure.CopyrightOffset + 4;
            int footerOffset = FindFooter(reader, structure);
            int blockSize = structure.FrameSize * structure.ChannelCount;
            int blockCount = (footerOffset - audioOffset) / blockSize;

            reader.BaseStream.Position = audioOffset;
            int dataLength = blockCount * blockSize;
            structure.AudioData = reader.BaseStream.DeInterleave(dataLength, structure.FrameSize, structure.ChannelCount);
        }

        public static int FindFooter(BinaryReader reader, AdxStructure structure)
        {
            const ushort footerSignature = 0x8001;
            int fileSize = (int)reader.BaseStream.Length;
            int audioStart = structure.CopyrightOffset + 4;
            int frameSize = structure.FrameSize;

            int position = GetNextMultiple(fileSize - audioStart, frameSize) + audioStart;

            ushort peek;
            do
            {
                position -= frameSize;
                reader.BaseStream.Position = position;
                peek = reader.ReadUInt16();
            } while (peek != footerSignature && position > 0);

            return position;
        }
    }
}
