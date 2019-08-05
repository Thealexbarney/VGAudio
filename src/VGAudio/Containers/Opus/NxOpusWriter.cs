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
    public class NxOpusWriter : AudioWriter<NxOpusWriter, NxOpusConfiguration>
    {
        private OpusFormat Format { get; set; }
        protected override int FileSize
        {
            get
            {
                switch (Configuration.HeaderType)
                {
                    case NxOpusHeaderType.Standard:
                        return StandardFileSize;
                    case NxOpusHeaderType.Namco:
                        return NamcoHeaderSize + StandardFileSize;
                    case NxOpusHeaderType.Skyrim:
                        return SkyrimHeaderSize + StandardFileSize;
                    default:
                        return 0;
                }
            }
        }

        private const int StandardHeaderSize = 0x28;
        private const int NamcoHeaderSize = 0x40;
        private const int SkyrimHeaderSize = 0x14;

        private int StandardFileSize => StandardHeaderSize + DataSize;
        private int DataSize { get; set; }

        protected override void SetupWriter(AudioData audio)
        {
            var encodingConfig = new OpusParameters
            {
                Bitrate = Configuration.Bitrate,
                EncodeCbr = Configuration.EncodeCbr,
                Progress = Configuration.Progress
            };

            Format = audio.GetFormat<OpusFormat>(encodingConfig);
            Format.EnsureHasFinalRange();

            DataSize = Format.Frames.Sum(x => x.Length + 8);
        }

        protected override void WriteStream(Stream stream)
        {
            switch (Configuration.HeaderType)
            {
                case NxOpusHeaderType.Standard:
                    WriteStandardHeader(GetBinaryWriter(stream, Endianness.LittleEndian));
                    WriteData(GetBinaryWriter(stream, Endianness.BigEndian));
                    break;
                case NxOpusHeaderType.Namco:
                    WriteNamcoHeader(GetBinaryWriter(stream, Endianness.BigEndian));
                    WriteStandardHeader(GetBinaryWriter(stream, Endianness.LittleEndian));
                    WriteData(GetBinaryWriter(stream, Endianness.BigEndian));
                    break;
                case NxOpusHeaderType.Skyrim:
                    WriteSkyrimHeader(GetBinaryWriter(stream, Endianness.LittleEndian));
                    WriteStandardHeader(GetBinaryWriter(stream, Endianness.LittleEndian));
                    WriteData(GetBinaryWriter(stream, Endianness.BigEndian));
                    break;
                default:
                    throw new NotImplementedException("Writing this Opus header is not supported");
            }
        }

        private void WriteStandardHeader(BinaryWriter writer)
        {
            writer.Write(0x80000001);
            writer.Write(0x18);
            writer.Write((byte)0);
            writer.Write((byte)Format.ChannelCount);
            // If frame length is inconsistent, frameLength = 0
            int frameLength = 0;
            if (Format.Frames.Count > 0)
            {
                frameLength = Format.Frames[0].Length;
                foreach (OpusFrame frame in Format.Frames)
                {
                    if (frame.Length != frameLength)
                    {
                        frameLength = 0;
                        break;
                    }
                }
            }
            writer.Write((short)(frameLength + 8));
            writer.Write(Format.SampleRate);
            writer.Write(0x20);
            writer.Write(0);
            writer.Write(0);
            writer.Write((short)Format.PreSkipCount);
            writer.Write((short)0);
            writer.Write(0x80000004);
            writer.Write(DataSize);
        }

        private void WriteSkyrimHeader(BinaryWriter writer)
        {
            long startPos = writer.BaseStream.Position;

            writer.Write(0xFFD58D0A);
            int duration_ms = (int) ((float)(Format.SampleCount * 1000) / (float)(Format.SampleRate));
            writer.Write(duration_ms);
            writer.Write(Format.ChannelCount);
            writer.Write(SkyrimHeaderSize);
            writer.Write(StandardHeaderSize + DataSize); // OPUS payload size
            writer.BaseStream.Position = startPos + SkyrimHeaderSize;
        }

        private void WriteNamcoHeader(BinaryWriter writer)
        {
            long startPos = writer.BaseStream.Position;

            writer.WriteUTF8("OPUS");
            writer.Write(0);
            writer.Write(Format.SampleCount);
            writer.Write(Format.ChannelCount);
            writer.Write(Format.SampleRate);
            writer.Write(Format.LoopStart);
            writer.Write(Format.LoopEnd);
            writer.Write(Format.Looping ? 0x10 : 0);
            writer.Write(NamcoHeaderSize);
            writer.Write(StandardFileSize);

            writer.BaseStream.Position = startPos + NamcoHeaderSize;
        }

        private void WriteData(BinaryWriter writer)
        {
            foreach (OpusFrame frame in Format.Frames)
            {
                writer.Write(frame.Length);
                writer.Write(frame.FinalRange);
                writer.Write(frame.Data);
            }
        }
    }
}
