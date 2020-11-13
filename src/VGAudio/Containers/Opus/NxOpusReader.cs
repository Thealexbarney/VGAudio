using System;
using System.IO;
using System.Linq;
using VGAudio.Codecs.Opus;
using VGAudio.Formats;
using VGAudio.Formats.Opus;
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

            long startPos = stream.Position;

            switch (structure.HeaderType)
            {
                case NxOpusHeaderType.Standard:
                    ReadStandardHeader(GetBinaryReader(stream, Endianness.LittleEndian), structure);
                    break;
                case NxOpusHeaderType.Namco:
                    ReadNamcoHeader(GetBinaryReader(stream, Endianness.BigEndian), structure);

                    stream.Position = startPos + structure.NamcoDataOffset;
                    ReadStandardHeader(GetBinaryReader(stream, Endianness.LittleEndian), structure);
                    break;
                case NxOpusHeaderType.Sadf:
                    ReadSadfHeader(GetBinaryReader(stream, Endianness.LittleEndian), structure);

                    stream.Position = startPos + structure.SadfDataOffset;
                    ReadStandardHeader(GetBinaryReader(stream, Endianness.LittleEndian), structure);
                    break;
                case NxOpusHeaderType.Ktss:
                    ReadKtssHeader(GetBinaryReader(stream, Endianness.LittleEndian), structure);
                    break;
            }

            BinaryReader reader = GetBinaryReader(stream, Endianness.BigEndian);
            ReadData(reader, structure);

            return structure;
        }

        protected override IAudioFormat ToAudioStream(NxOpusStructure structure)
        {
            return new OpusFormatBuilder(structure.SampleCount, structure.SampleRate, structure.ChannelCount,
                    structure.PreSkip, structure.Frames)
                .WithLoop(structure.Looping, structure.LoopStart, structure.LoopEnd)
                .HasFinalRange()
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
                case 0x66646173: return NxOpusHeaderType.Sadf; // sadf
                case 0x5353544B: return NxOpusHeaderType.Ktss; // KTSS
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
            structure.PreSkip = reader.ReadInt32();

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

        private static void ReadSadfHeader(BinaryReader reader, NxOpusStructure structure)
        {
            if (reader.ReadUInt32() != 0x66646173) throw new InvalidDataException();
            reader.ReadInt32(); // fileSize
            if (reader.ReadUInt32() != 0x7375706F) throw new InvalidDataException();

            reader.ReadInt32(); // sectionCount
            if (reader.ReadUTF8(4) != "head") throw new InvalidDataException();
            reader.ReadInt32(); // sectionLength

            structure.ChannelCount = reader.ReadByte();
            structure.Looping = (reader.ReadByte() & 2) != 0;
            reader.BaseStream.Position += 2; // padding

            structure.SadfDataOffset = reader.ReadInt32();
            reader.ReadInt32(); // dataLength

            structure.SampleRate = reader.ReadInt32();
            structure.SampleCount = reader.ReadInt32();
            structure.LoopStart = reader.ReadInt32();
            structure.LoopEnd = reader.ReadInt32();
        }

        private static void ReadKtssHeader(BinaryReader reader, NxOpusStructure structure)
        {
            if (reader.ReadUInt32() != 0x5353544B) throw new InvalidDataException();
            reader.ReadInt32(); // fileSize
            reader.BaseStream.Position += 0x18; // padding
            reader.ReadUInt16(); // Codec ID
            reader.ReadUInt16(); // Unknown
            reader.ReadUInt32(); // Subsection start offset
            reader.ReadByte(); // Layer count
            structure.ChannelCount = reader.ReadByte();
            reader.ReadUInt16(); // Unknown
            structure.SampleRate = reader.ReadInt32();
            structure.SampleCount = reader.ReadInt32();

            structure.LoopStart = reader.ReadInt32();
            int loopLength = reader.ReadInt32();
            structure.LoopEnd = structure.LoopStart + loopLength;
            structure.Looping = loopLength != 0;

            reader.ReadInt32(); // Padding. Moaaar padding. Koei Tecmo loves padding.
            structure.DataOffset = reader.ReadInt32(); // Audio section address
            structure.DataSize = reader.ReadInt32(); // Audio section size
            reader.ReadInt32(); // Unknown
            reader.ReadInt32(); // Frame count
            structure.FrameSize = reader.ReadInt16();
            reader.ReadInt16(); // Unknown. Always 0x3C0
            reader.ReadInt32(); // Original sample rate?
            structure.PreSkip = reader.ReadUInt16(); // Pre-skip
            reader.ReadByte(); // Stream count
            reader.ReadByte(); // Coupled count
            reader.ReadBytes(structure.ChannelCount); // Channel mapping

            reader.BaseStream.Position = structure.DataOffset;
        }

        private static void ReadData(BinaryReader reader, NxOpusStructure structure)
        {
            long startPos = reader.BaseStream.Position;
            long endPos = startPos + structure.DataSize;

            while (true)
            {
                if (endPos - reader.BaseStream.Position < 8) break;

                var frame = new OpusFrame();
                frame.Length = reader.ReadInt32();
                frame.FinalRange = reader.ReadUInt32();

                if (endPos - reader.BaseStream.Position < frame.Length) break;

                frame.Data = reader.ReadBytes(frame.Length);
                frame.SampleCount = OggOpusReader.GetOpusSamplesPerFrame(frame.Data[0], 48000);
                structure.Frames.Add(frame);
            }

            if (structure.HeaderType == NxOpusHeaderType.Standard)
            {
                structure.SampleCount = structure.Frames.Sum(x => x.SampleCount);
            }
        }
    }

    public enum NxOpusHeaderType
    {
        Standard,
        Namco,
        Sadf,
        Ktss
    }
}
