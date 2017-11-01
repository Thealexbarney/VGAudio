using System;
using System.IO;
using System.Text;
using VGAudio.Codecs.CriHca;
using VGAudio.Formats;
using VGAudio.Formats.CriHca;
using VGAudio.Utilities;
using static VGAudio.Utilities.Helpers;

namespace VGAudio.Containers.Hca
{
    public class HcaReader : AudioReader<HcaReader, HcaStructure, HcaConfiguration>
    {
        /// <summary>If <c>true</c>, decrypts the HCA data if possible.</summary>
        public bool Decrypt { get; set; } = true;
        public CriHcaKey EncryptionKey { get; set; }

        private static Crc16 Crc { get; } = new Crc16(0x8005);

        protected override HcaStructure ReadFile(Stream stream, bool readAudioData = true)
        {
            using (BinaryReader reader = GetBinaryReader(stream, Endianness.BigEndian))
            {
                var structure = new HcaStructure();
                ReadHcaHeader(reader, structure);

                reader.BaseStream.Position = structure.HeaderSize;

                if (readAudioData)
                {
                    ReadHcaData(reader, structure);
                    structure.EncryptionKey = Decrypt ? EncryptionKey ?? FindKey(structure) : null;
                }

                return structure;
            }
        }

        protected override IAudioFormat ToAudioStream(HcaStructure structure)
        {
            if (structure.EncryptionKey != null)
            {
                CriHcaEncryption.Decrypt(structure.Hca, structure.AudioData, structure.EncryptionKey);
            }

            return new CriHcaFormatBuilder(structure.AudioData, structure.Hca).Build();
        }

        protected override HcaConfiguration GetConfiguration(HcaStructure structure)
        {
            return new HcaConfiguration
            {
                EncryptionKey = structure.EncryptionKey
            };
        }

        private static void ReadHcaHeader(BinaryReader reader, HcaStructure structure)
        {
            HcaInfo hca = structure.Hca;
            string signature = ReadChunkId(reader);
            structure.Version = reader.ReadInt16();
            structure.HeaderSize = reader.ReadInt16();

            if (signature != "HCA\0")
            {
                throw new InvalidDataException("Not a valid HCA file");
            }

            bool hasAthChunk = false;
            while (reader.BaseStream.Position < structure.HeaderSize)
            {
                string chunkId = ReadChunkId(reader);

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
                        hasAthChunk = true;
                        break;
                    case "ciph":
                        ReadCiphChunk(reader, structure);
                        break;
                    case "rva\0":
                        ReadRvaChunk(reader, structure);
                        break;
                    case "vbr\0":
                        ReadVbrChunk(reader, structure);
                        break;
                    case "comm":
                        ReadCommChunk(reader, structure);
                        reader.BaseStream.Position = structure.HeaderSize;
                        break;
                    case "pad\0":
                        reader.BaseStream.Position = structure.HeaderSize;
                        break;
                    default:
                        throw new NotSupportedException($"Chunk {chunkId} is not supported.");
                }
            }

            if (structure.Version < 0x0200 && !hasAthChunk) hca.UseAthCurve = true;

            if (hca.TrackCount < 1) hca.TrackCount = 1;

            hca.CalculateHfrValues();
        }

        private static void ReadHcaData(BinaryReader reader, HcaStructure structure)
        {
            structure.AudioData = new byte[structure.Hca.FrameCount][];
            for (int i = 0; i < structure.Hca.FrameCount; i++)
            {
                byte[] data = reader.ReadBytes(structure.Hca.FrameSize);
                int crc = Crc.Compute(data, data.Length - 2);
                int expectedCrc = data[data.Length - 2] << 8 | data[data.Length - 1];
                if (crc != expectedCrc)
                {
                    // TODO: Decide how to handle bad CRC
                }

                structure.AudioData[i] = data;
            }
        }

        private static void ReadFmtChunk(BinaryReader reader, HcaStructure structure)
        {
            structure.Hca.ChannelCount = reader.ReadByte();
            structure.Hca.SampleRate = reader.ReadByte() << 16 | reader.ReadUInt16();
            structure.Hca.FrameCount = reader.ReadInt32();
            structure.Hca.InsertedSamples = reader.ReadInt16();
            structure.Hca.AppendedSamples = reader.ReadInt16();
            structure.Hca.SampleCount = structure.Hca.FrameCount * 1024 -
                structure.Hca.InsertedSamples - structure.Hca.AppendedSamples;
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
            structure.Hca.TrackCount = GetHighNibble(a);
            structure.Hca.ChannelConfig = GetLowNibble(a);
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
            structure.Hca.SampleCount = Math.Min(structure.Hca.SampleCount, structure.Hca.LoopEndSample);
        }

        private static void ReadAthChunk(BinaryReader reader, HcaStructure structure)
        {
            structure.Hca.UseAthCurve = reader.ReadInt16() == 1;
        }

        private static void ReadVbrChunk(BinaryReader reader, HcaStructure structure)
        {
            structure.Hca.VbrMaxFrameSize = reader.ReadInt16();
            structure.Hca.VbrNoiseLevel = reader.ReadInt16();
        }

        private static void ReadCiphChunk(BinaryReader reader, HcaStructure structure)
        {
            structure.Hca.EncryptionType = reader.ReadInt16();
        }

        private static void ReadRvaChunk(BinaryReader reader, HcaStructure structure)
        {
            structure.Hca.Volume = reader.ReadSingle();
        }

        private static void ReadCommChunk(BinaryReader reader, HcaStructure structure)
        {
            reader.BaseStream.Position++;
            structure.Hca.Comment = reader.ReadUTF8Z();
        }

        private static string ReadChunkId(BinaryReader reader)
        {
            byte[] bytes = reader.ReadBytes(4);

            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] &= 0x7f;
            }

            return Encoding.UTF8.GetString(bytes, 0, 4);
        }

        private static CriHcaKey FindKey(HcaStructure structure)
        {
            switch (structure.Hca.EncryptionType)
            {
                case 0:
                    return null;
                case 1:
                    return new CriHcaKey(CriHcaKey.Type.Type1);
                case 56:
                    return CriHcaEncryption.FindKey(structure.Hca, structure.AudioData) ??
                           throw new InvalidDataException("Cannot find key to decrypt HCA file.");
                default:
                    return null;
            }
        }
    }
}
