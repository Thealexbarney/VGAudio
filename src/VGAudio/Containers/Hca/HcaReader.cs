using System;
using System.IO;
using VGAudio.Formats;
using VGAudio.Formats.CriHca;
using VGAudio.Utilities;
using static VGAudio.Utilities.Helpers;

namespace VGAudio.Containers.Hca
{
    public class HcaReader : AudioReader<HcaReader, HcaStructure, Configuration>
    {
        protected override HcaStructure ReadFile(Stream stream, bool readAudioData = true)
        {
            using (BinaryReader reader = GetBinaryReader(stream, Endianness.BigEndian))
            {
                var structure = new HcaStructure();
                ReadHcaHeader(reader, structure);

                reader.BaseStream.Position = structure.HeaderSize;

                ReadHcaData(reader, structure);
                return structure;
            }
        }

        protected override IAudioFormat ToAudioStream(HcaStructure structure)
        {
            return new CriHcaFormatBuilder(structure.AudioData, structure.Hca).Build();
        }

        private static void ReadHcaHeader(BinaryReader reader, HcaStructure structure)
        {
            string signature = reader.ReadUTF8(4);
            structure.Version = reader.ReadInt16();
            structure.HeaderSize = reader.ReadInt16();

            if (signature != "HCA\0")
            {
                throw new InvalidDataException("Not a valid HCA file");
            }

            while (reader.BaseStream.Position < structure.HeaderSize)
            {
                string chunkId = reader.ReadUTF8(4);

                switch (chunkId)
                {
                    case "fmt\0":
                        ReadFmtChunk(reader, structure);
                        break;
                    case "comp":
                        ReadCompChunk(reader, structure);
                        break;
                    case "dec\0":
                        ReadDecChunk(reader, structure);
                        break;
                    case "loop":
                        ReadLoopChunk(reader, structure);
                        break;
                    case "ath\0":
                        ReadAthChunk(reader, structure);
                        break;
                    case "ciph":
                        ReadCiphChunk(reader, structure);
                        break;
                    case "pad\0":
                        reader.BaseStream.Position = structure.HeaderSize;
                        break;
                    default:
                        throw new NotSupportedException($"Chunk {chunkId} is not supported.");
                }
            }

            if (structure.Hca.TrackCount < 1) structure.Hca.TrackCount = 1;
        }

        private static void ReadHcaData(BinaryReader reader, HcaStructure structure)
        {
            structure.AudioData = new byte[structure.Hca.FrameCount][];
            for (int i = 0; i < structure.Hca.FrameCount; i++)
            {
                structure.AudioData[i] = reader.ReadBytes(structure.Hca.FrameSize);
            }
        }

        private static void ReadFmtChunk(BinaryReader reader, HcaStructure structure)
        {
            structure.Hca.ChannelCount = reader.ReadByte();
            structure.Hca.SampleRate = reader.ReadByte() << 16 | reader.ReadUInt16();
            structure.Hca.FrameCount = reader.ReadInt32();
            structure.Hca.InsertedSamples = reader.ReadInt16();
            structure.Hca.AppendedSamples = reader.ReadInt16();
            structure.Hca.SampleCount = structure.Hca.FrameCount * 1024;
        }

        private static void ReadCompChunk(BinaryReader reader, HcaStructure structure)
        {
            structure.Hca.FrameSize = reader.ReadInt16();
            structure.Hca.MinResolution = reader.ReadByte();
            structure.Hca.MaxResolution = reader.ReadByte();
            structure.Hca.TrackCount = reader.ReadByte();
            structure.Hca.ChannelConfig = reader.ReadByte();
            structure.Hca.TotalBandCount = reader.ReadByte();
            structure.Hca.BaseBandCount = reader.ReadByte();
            structure.Hca.StereoBandCount = reader.ReadByte();
            structure.Hca.BandsPerHfrGroup = reader.ReadByte();
            structure.Reserved1 = reader.ReadByte();
            structure.Reserved2 = reader.ReadByte();
        }

        private static void ReadDecChunk(BinaryReader reader, HcaStructure structure)
        {
            structure.Hca.FrameSize = reader.ReadInt16();
            structure.Hca.MinResolution = reader.ReadByte();
            structure.Hca.MaxResolution = reader.ReadByte();
            structure.Hca.TotalBandCount = reader.ReadByte() + 1;
            structure.Hca.BaseBandCount = reader.ReadByte() + 1;

            byte a = reader.ReadByte();
            structure.Hca.TrackCount = a >> 4 & 0xf;
            structure.Hca.ChannelConfig = a & 0xf;
            structure.Hca.DecStereoType = reader.ReadByte();

            if (structure.Hca.DecStereoType == 0)
            {
                structure.Hca.BaseBandCount = structure.Hca.TotalBandCount;
            }
            else
            {
                structure.Hca.StereoBandCount = structure.Hca.TotalBandCount - structure.Hca.BaseBandCount;
            }
        }

        private static void ReadLoopChunk(BinaryReader reader, HcaStructure structure)
        {
            structure.Hca.Looping = true;
            structure.Hca.LoopStartFrame = reader.ReadInt32();
            structure.Hca.LoopEndFrame = reader.ReadInt32();
            structure.Hca.PreLoopSamples = reader.ReadInt16();
            structure.Hca.PostLoopSamples = reader.ReadInt16();
        }

        private static void ReadAthChunk(BinaryReader reader, HcaStructure structure)
        {
            structure.Hca.AthTableType = reader.ReadInt16();
        }

        private static void ReadCiphChunk(BinaryReader reader, HcaStructure structure)
        {
            structure.Hca.EncryptionType = reader.ReadInt16();
        }
    }
}
