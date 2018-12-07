using System;
using System.IO;
using System.Linq;
using VGAudio.Codecs.Opus;
using VGAudio.Formats;
using VGAudio.Utilities;
using static VGAudio.Utilities.Helpers;

namespace VGAudio.Containers.Opus
{
    public class NxOpusReader : AudioReader<NxOpusReader, NxOpusStructure, NxOpusConfiguration>
    {
        protected override NxOpusStructure ReadFile(Stream stream, bool readAudioData = true)
        {
            var structure = new NxOpusStructure();
            structure.HeaderType = DetectHeader(stream);

            switch (structure.HeaderType)
            {
                case NxOpusHeaderType.Standard:
                    ReadStandardHeader(GetBinaryReader(stream, Endianness.LittleEndian), structure);
                    break;
                case NxOpusHeaderType.Namco:
                    long startPos = stream.Position;
                    ReadNamcoHeader(GetBinaryReader(stream, Endianness.BigEndian), structure);

                    stream.Position = startPos + structure.NamcoDataOffset;
                    ReadStandardHeader(GetBinaryReader(stream, Endianness.LittleEndian), structure);
                    break;
            }

            BinaryReader reader = GetBinaryReader(stream, Endianness.BigEndian);
            ReadData(reader, structure);

            return structure;
        }

        protected override IAudioFormat ToAudioStream(NxOpusStructure structure)
        {
            return new OpusFormatBuilder(structure.ChannelCount, structure.SampleCount, structure.EncoderDelay, structure.Frames)
                .WithLoop(structure.Looping, structure.LoopStart, structure.LoopEnd)
                .Build();
        }

        protected override NxOpusConfiguration GetConfiguration(NxOpusStructure structure)
        {
            return new NxOpusConfiguration
            {
                HeaderType = structure.HeaderType
            };
        }

        private static NxOpusHeaderType DetectHeader(Stream stream)
        {
            uint value = GetBinaryReader(stream, Endianness.LittleEndian).ReadUInt32();
            stream.Position -= 4;

            switch (value)
            {
                case 0x80000001: return NxOpusHeaderType.Standard;
                case 0x5355504F: return NxOpusHeaderType.Namco; // OPUS
                default: throw new NotImplementedException("This Opus header is not supported");
            }
        }

        private static void ReadStandardHeader(BinaryReader reader, NxOpusStructure structure)
        {
            long startPos = reader.BaseStream.Position;

            structure.Type = reader.ReadUInt32();
            structure.HeaderSize = reader.ReadInt32();
            structure.Version = reader.ReadByte();
            structure.ChannelCount = reader.ReadByte();
            structure.FrameSize = reader.ReadInt16();
            structure.SampleRate = reader.ReadInt32();
            structure.DataOffset = reader.ReadInt32();

            reader.BaseStream.Position += 8;
            structure.EncoderDelay = reader.ReadInt32();

            reader.BaseStream.Position = startPos + structure.DataOffset;

            structure.DataType = reader.ReadUInt32();
            structure.DataSize = reader.ReadInt32();
        }

        private static void ReadNamcoHeader(BinaryReader reader, NxOpusStructure structure)
        {
            if (reader.ReadUInt32() != 0x4F505553) throw new InvalidDataException();

            reader.BaseStream.Position += 4;
            structure.SampleCount = reader.ReadInt32();
            structure.ChannelCount = reader.ReadInt32();
            structure.SampleRate = reader.ReadInt32();
            structure.LoopStart = reader.ReadInt32();
            structure.LoopEnd = reader.ReadInt32();
            structure.Looping = structure.LoopEnd != 0;
            structure.NamcoField1C = reader.ReadInt32();
            structure.NamcoDataOffset = reader.ReadInt32();
            structure.NamcoCoreDataLength = reader.ReadInt32();
        }

        private static void ReadData(BinaryReader reader, NxOpusStructure structure)
        {
            long startPos = reader.BaseStream.Position;
            long endPos = startPos + structure.DataSize;

            while (true)
            {
                if (endPos - reader.BaseStream.Position < 8) break;

                var frame = new NxOpusFrame();
                frame.Length = reader.ReadInt32();
                frame.FinalRange = reader.ReadUInt32();

                if (endPos - reader.BaseStream.Position < frame.Length) break;

                frame.Data = reader.ReadBytes(frame.Length);
                frame.SampleCount = GetOpusSamplesPerFrame(frame.Data[0], 48000);
                structure.Frames.Add(frame);
            }

            if (structure.HeaderType == NxOpusHeaderType.Standard)
            {
                structure.SampleCount = structure.Frames.Sum(x => x.SampleCount);
            }
        }

        private static int GetOpusSamplesPerFrame(byte data, int fs)
        {
            int audioSize;
            if ((data & 0x80) != 0)
            {
                audioSize = ((data >> 3) & 0x3);
                audioSize = (fs << audioSize) / 400;
            }
            else if ((data & 0x60) == 0x60)
            {
                audioSize = ((data & 0x08) != 0) ? fs / 50 : fs / 100;
            }
            else
            {
                audioSize = ((data >> 3) & 0x3);
                if (audioSize == 3)
                    audioSize = fs * 60 / 1000;
                else
                    audioSize = (fs << audioSize) / 100;
            }
            return audioSize;
        }
    }

    public enum NxOpusHeaderType
    {
        Standard,
        Namco
    }
}
