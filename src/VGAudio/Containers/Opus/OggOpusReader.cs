using System.IO;
using System.Linq;
using System.Text;
using Concentus.Oggfile;
using VGAudio.Codecs.Opus;
using VGAudio.Formats;
using VGAudio.Formats.Opus;
using VGAudio.Utilities;
using static VGAudio.Utilities.Helpers;

namespace VGAudio.Containers.Opus
{
    public class OggOpusReader : AudioReader<OggOpusReader, OggOpusStructure, NxOpusConfiguration>
    {

        protected override OggOpusStructure ReadFile(Stream stream, bool readAudioData = true)
        {
            var structure = new OggOpusStructure();

            var reader = new OggContainerReader(stream, false);
            reader.Init();
            int streamSerial = reader.StreamSerials[0];
            IPacketProvider packetProvider = reader.GetStream(streamSerial);

            ReadOpusHead(structure, packetProvider);
            SkipOpusTags(packetProvider);
            ReadData(structure, packetProvider);

            return structure;
        }

        protected override IAudioFormat ToAudioStream(OggOpusStructure structure)
        {
            return new OpusFormatBuilder(structure.SampleCount, structure.SampleRate, structure.ChannelCount,
                structure.PreSkip, structure.Frames).Build();
        }

        private static void ReadData(OggOpusStructure structure, IPacketProvider packetProvider)
        {
            bool finished = false;
            DataPacket packet = packetProvider.GetNextPacket();

            while (!finished)
            {
                finished = packet.IsEndOfStream;
                byte[] buffer = GetPacketData(packet);

                var frame = new OpusFrame();
                frame.Length = buffer.Length;
                frame.Data = buffer;
                frame.SampleCount = GetOpusSamplesPerFrame(frame.Data[0], 48000);
                structure.Frames.Add(frame);

                packet = packetProvider.GetNextPacket();
            }

            structure.SampleCount = structure.Frames.Sum(x => x.SampleCount);
        }

        private static byte[] GetPacketData(DataPacket packet)
        {
            var buffer = new byte[packet.Length];
            packet.Read(buffer, 0, packet.Length);
            return buffer;
        }

        private static void ReadOpusHead(OggOpusStructure structure, IPacketProvider packetProvider)
        {
            DataPacket packet = packetProvider.GetNextPacket();
            byte[] buffer = GetPacketData(packet);
            packet.Done();

            BinaryReader reader = GetBinaryReader(new MemoryStream(buffer), Endianness.LittleEndian);
            if (reader.ReadUTF8(8) != "OpusHead")
            {
                throw new InvalidDataException("Expected OpusHead segment");
            }

            structure.Version = reader.ReadByte();
            structure.ChannelCount = reader.ReadByte();
            structure.PreSkip = reader.ReadInt16();
            structure.SampleRate = reader.ReadInt32();
            structure.OutputGain = reader.ReadInt16();
            structure.ChannelMapping = reader.ReadByte();
        }

        private void SkipOpusTags(IPacketProvider packetProvider)
        {
            DataPacket packet = packetProvider.PeekNextPacket();
            byte[] buffer = GetPacketData(packet);

            if (Encoding.UTF8.GetString(buffer, 0, 8) == "OpusTags")
            {
                packetProvider.GetNextPacket();
            }
        }

        internal static int GetOpusSamplesPerFrame(byte data, int fs)
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
