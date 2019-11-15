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
                    case NxOpusHeaderType.Ktss:
                        return KtssHeaderSize;
                    default:
                        return 0;
                }
            }
        }

        private const int StandardHeaderSize = 0x28;
        private const int NamcoHeaderSize = 0x40;
        private const int KtssHeaderSize = 0x70;

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
                case NxOpusHeaderType.Ktss:
                    WriteKtssHeader(GetBinaryWriter(stream, Endianness.LittleEndian));
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

        private void WriteKtssHeader(BinaryWriter writer)
        {
            long startPos = writer.BaseStream.Position;

            writer.WriteUTF8("KTSS");
            writer.Write(KtssHeaderSize + DataSize);
            writer.Seek(0x20, SeekOrigin.Begin);
            writer.Write((short)9);
            writer.Write((byte)3);
            writer.Write((byte)3);
            writer.Write(0x50);
            writer.Write((byte)1);
            writer.Write((byte)Format.ChannelCount);
            writer.Write((short)0);
            writer.Write(Format.SampleRate);
            writer.Write(Format.SampleCount);
            writer.Write(Format.LoopStart);
            writer.Write(Format.Looping ? Format.LoopEnd - Format.LoopStart : 0);
            writer.Write(0); // Padding
            writer.Write(0x70);
            writer.Write(DataSize);
            writer.Write(0);
            writer.Write(Format.Frames.Count);
            writer.Write((short)(DataSize / Format.Frames.Count)); // Frame size
            writer.Write((short)0x3C0); // Some constant
            writer.Write(Format.SampleRate); // "Original" sample rate
            writer.Write((short)Format.PreSkipCount);
            writer.Write((byte)1);
            writer.Write((byte)1);

            // Channel mapping, Koei Tecmo doesn't seem to care about the order so we don't either
            for (int i = 0; i < Format.ChannelCount; i++)
                writer.Write((byte)i);

            writer.BaseStream.Position = startPos + KtssHeaderSize;
        }

        private void WriteData(BinaryWriter writer)
        {
            foreach (OpusFrame frame in Format.Frames)
            {
                writer.Write(frame.Length);
                writer.Write(frame.FinalRange);
                writer.Write(frame.Data);
            }

            // KTSS need to be 0x40 aligned
            if (Configuration.HeaderType == NxOpusHeaderType.Ktss)
                writer.Write(new byte[0x40 - writer.BaseStream.Position % 0x40]);
        }
    }
}
