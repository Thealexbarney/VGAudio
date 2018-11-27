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
            BinaryReader reader = GetBinaryReader(stream, Endianness.LittleEndian);

            var structure = new NxOpusStructure();
            ReadHeader(reader, structure);

            reader = GetBinaryReader(stream, Endianness.BigEndian);
            ReadData(reader, structure);

            return structure;
        }

        protected override IAudioFormat ToAudioStream(NxOpusStructure structure)
        {
            return new OpusFormatBuilder(structure.ChannelCount, structure.Frames.Sum(x => x.SampleCount),
                structure.Frames).Build();
        }

        private static void ReadHeader(BinaryReader reader, NxOpusStructure structure)
        {
            reader.BaseStream.Position = 0;

            structure.Type = reader.ReadUInt32();
            structure.HeaderSize = reader.ReadInt32();
            structure.Version = reader.ReadByte();
            structure.ChannelCount = reader.ReadByte();
            structure.FrameSize = reader.ReadInt16();
            structure.SampleRate = reader.ReadInt32();
            structure.DataOffset = reader.ReadInt32();

            reader.BaseStream.Position = structure.DataOffset;

            structure.DataType = reader.ReadUInt32();
            structure.DataSize = reader.ReadInt32();
        }

        private static void ReadData(BinaryReader reader, NxOpusStructure structure)
        {
            long inputLength = reader.BaseStream.Length;

            while (true)
            {
                if (inputLength - reader.BaseStream.Position < 8) break;

                var frame = new NxOpusFrame();
                frame.Length = reader.ReadInt32();
                frame.FinalRange = reader.ReadUInt32();

                if (inputLength - reader.BaseStream.Position < frame.Length) break;

                frame.Data = reader.ReadBytes(frame.Length);
                frame.SampleCount = GetOpusSamplesPerFrame(frame.Data[0], 48000);
                structure.Frames.Add(frame);
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
}
