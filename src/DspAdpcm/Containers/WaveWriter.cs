using System;
using System.IO;
using DspAdpcm.Containers.Wave;
using DspAdpcm.Formats;
using DspAdpcm.Utilities;
using static DspAdpcm.Utilities.Helpers;

namespace DspAdpcm.Containers
{
    public class WaveWriter : AudioWriter<WaveWriter, WaveConfiguration>
    {
        private Pcm16Format Pcm16 { get; set; }
        private int ChannelCount => Pcm16.ChannelCount;
        private int SampleCount => Pcm16.SampleCount;
        private int SampleRate => Pcm16.SampleRate;
        protected override int FileSize => 8 + RiffChunkSize;
        private int RiffChunkSize => 4 + 8 + FmtChunkSize + 8 + DataChunkSize;
        private int FmtChunkSize => ChannelCount > 2 ? 40 : 16;
        private int DataChunkSize => ChannelCount * SampleCount * sizeof(short);

        private int BitDepth => 16;
        private int BytesPerSample => BitDepth.DivideByRoundUp(8);
        private int BytesPerSecond => SampleRate * BytesPerSample * ChannelCount;
        private int BlockAlign => BytesPerSample * ChannelCount;

        // ReSharper disable InconsistentNaming
        private static readonly Guid KSDATAFORMAT_SUBTYPE_PCM =
            new Guid("00000001-0000-0010-8000-00aa00389b71");
        private const ushort WAVE_FORMAT_PCM = 1;
        private const ushort WAVE_FORMAT_EXTENSIBLE = 0xfffe;
        // ReSharper restore InconsistentNaming

        protected override void SetupWriter(AudioData audio)
        {
            Pcm16 = audio.GetFormat<Pcm16Format>();
        }

        protected override void WriteStream(Stream stream)
        {
            using (BinaryWriter writer = GetBinaryWriter(stream, Endianness.LittleEndian))
            {
                stream.Position = 0;
                WriteRiffHeader(writer);
                WriteFmtChunk(writer);
                WriteDataChunk(writer);
            }
        }

        private void WriteRiffHeader(BinaryWriter writer)
        {
            writer.WriteUTF8("RIFF");
            writer.Write(RiffChunkSize);
            writer.WriteUTF8("WAVE");
        }

        private void WriteFmtChunk(BinaryWriter writer)
        {
            writer.WriteUTF8("fmt ");
            writer.Write(FmtChunkSize);
            writer.Write((short)(ChannelCount > 2 ? WAVE_FORMAT_EXTENSIBLE : WAVE_FORMAT_PCM));
            writer.Write((short)ChannelCount);
            writer.Write(SampleRate);
            writer.Write(BytesPerSecond);
            writer.Write((short)BlockAlign);
            writer.Write((short)BitDepth);

            if (ChannelCount > 2)
            {
                writer.Write((short)22);
                writer.Write((short)BitDepth);
                writer.Write(GetChannelMask(ChannelCount));
                writer.Write(KSDATAFORMAT_SUBTYPE_PCM.ToByteArray());
            }
        }

        private void WriteDataChunk(BinaryWriter writer)
        {
            writer.WriteUTF8("data");
            writer.Write(DataChunkSize);
            short[][] channels = Pcm16.Channels;

            var audioData = channels.ShortToInterleavedByte();
            writer.BaseStream.Write(audioData, 0, audioData.Length);
        }

        private static int GetChannelMask(int channelCount)
        {
            //Nothing special about these masks. I just choose
            //whatever channel combinations seemed okay.
            switch (channelCount)
            {
                case 4:
                    return 0x0033;
                case 5:
                    return 0x0133;
                case 6:
                    return 0x0633;
                case 7:
                    return 0x01f3;
                case 8:
                    return 0x06f3;
                default:
                    return (1 << channelCount) - 1;
            }
        }
    }
}
