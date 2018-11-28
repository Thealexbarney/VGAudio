using System.IO;
using System.Linq;
using VGAudio.Codecs.Opus;
using VGAudio.Formats;
using VGAudio.Utilities;
using static VGAudio.Utilities.Helpers;

namespace VGAudio.Containers.Opus
{
    public class NxOpusWriter : AudioWriter<NxOpusWriter, NxOpusConfiguration>
    {
        private OpusFormat Format { get; set; }
        protected override int FileSize => DataSize + 0x28;

        private int DataSize { get; set; }

        protected override void SetupWriter(AudioData audio)
        {
            Format = audio.GetFormat<OpusFormat>();

            DataSize = Format.Frames.Sum(x => x.Length + 8);
        }

        protected override void WriteStream(Stream stream)
        {
            using (BinaryWriter writer = GetBinaryWriter(stream, Endianness.LittleEndian))
            {
                WriteHeader(writer);
            }

            using (BinaryWriter writer = GetBinaryWriter(stream, Endianness.BigEndian))
            {
                WriteData(writer);
            }
        }

        private void WriteHeader(BinaryWriter writer)
        {
            writer.Write(0x80000001);
            writer.Write(0x18);
            writer.Write((byte)0);
            writer.Write((byte)Format.ChannelCount);
            writer.Write((short)0);
            writer.Write(Format.SampleRate);
            writer.Write(0x20);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0x80000004);
            writer.Write(DataSize);
        }

        private void WriteData(BinaryWriter writer)
        {
            foreach (var frame in Format.Frames)
            {
                writer.Write(frame.Length);
                writer.Write(frame.FinalRange);
                writer.Write(frame.Data);
            }
        }
    }
}
