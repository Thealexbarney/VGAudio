using System;
using System.IO;
using DspAdpcm.Utilities;
using static DspAdpcm.Utilities.Helpers;

namespace DspAdpcm.Containers
{
    public class WaveWriter : AudioWriter<WaveWriter>
    {
        private int ChannelCount => AudioStream.ChannelCount;
        private int SampleCount => AudioStream.SampleCount;
        private int SampleRate => AudioStream.SampleRate;
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


        protected override void WriteStream(Stream stream)
        {
            using (BinaryWriter writer = GetBinaryWriter(stream, Endianness.LittleEndian))
            {
                stream.Position = 0;
                GetRiffHeader(writer);
                GetFmtChunk(writer);
                GetDataChunk(writer);
            }
        }

        private void GetRiffHeader(BinaryWriter writer)
        {
            writer.WriteUTF8("RIFF");
            writer.Write(RiffChunkSize);
            writer.WriteUTF8("WAVE");
        }

        private void GetFmtChunk(BinaryWriter writer)
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

        private void GetDataChunk(BinaryWriter writer)
        {
            writer.WriteUTF8("data");
            writer.Write(DataChunkSize);
            short[][] channels = AudioStream.Pcm16.GetAudio;

            var audioData = channels.ShortToInterleavedByte();
            writer.BaseStream.Write(audioData, 0, audioData.Length);
        }

        private static int GetChannelMask(int ChannelCount)
        {
            //Nothing special about these masks. I just choose
            //whatever channel combinations seemed okay.
            switch (ChannelCount)
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
                    return (1 << ChannelCount) - 1;
            }
        }
    }
}
