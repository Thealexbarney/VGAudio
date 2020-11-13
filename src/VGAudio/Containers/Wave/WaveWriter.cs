using System.IO;
using VGAudio.Codecs;
using VGAudio.Formats;
using VGAudio.Formats.Pcm16;
using VGAudio.Formats.Pcm8;
using VGAudio.Utilities;
using VGAudio.Utilities.Riff;
using static VGAudio.Utilities.Helpers;

namespace VGAudio.Containers.Wave
{
    public class WaveWriter : AudioWriter<WaveWriter, WaveConfiguration>
    {
        private Pcm16Format Pcm16 { get; set; }
        private Pcm8Format Pcm8 { get; set; }
        private IAudioFormat AudioFormat { get; set; }

        private WaveCodec Codec => Configuration.Codec;
        private int ChannelCount => AudioFormat.ChannelCount;
        private int SampleCount => AudioFormat.SampleCount;
        private int SampleRate => AudioFormat.SampleRate;
        private bool Looping => AudioFormat.Looping;
        private int LoopStart => AudioFormat.LoopStart;
        private int LoopEnd => AudioFormat.LoopEnd;
        protected override int FileSize => 8 + RiffChunkSize;
        private int RiffChunkSize => 4 + 8 + FmtChunkSize + 8 + DataChunkSize
            + (Looping ? 8 + SmplChunkSize : 0);
        private int FmtChunkSize => ChannelCount > 2 ? 40 : 16;
        private int DataChunkSize => ChannelCount * SampleCount * BytesPerSample;
        private int SmplChunkSize => 0x3c;

        private int BitDepth => Configuration.Codec == WaveCodec.Pcm16Bit ? 16 : 8;
        private int BytesPerSample => BitDepth.DivideByRoundUp(8);
        private int BytesPerSecond => SampleRate * BytesPerSample * ChannelCount;
        private int BlockAlign => BytesPerSample * ChannelCount;

        protected override void SetupWriter(AudioData audio)
        {
            var parameters = new CodecParameters { Progress = Configuration.Progress };

            switch (Codec)
            {
                case WaveCodec.Pcm16Bit:
                    Pcm16 = audio.GetFormat<Pcm16Format>(parameters);
                    AudioFormat = Pcm16;
                    break;
                case WaveCodec.Pcm8Bit:
                    Pcm8 = audio.GetFormat<Pcm8Format>(parameters);
                    AudioFormat = Pcm8;
                    break;
            }
        }

        protected override void WriteStream(Stream stream)
        {
            using (BinaryWriter writer = GetBinaryWriter(stream, Endianness.LittleEndian))
            {
                stream.Position = 0;
                WriteRiffHeader(writer);
                WriteFmtChunk(writer);
                if (Looping)
                    WriteSmplChunk(writer);
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
            // Every chunk should be 2-byte aligned
            writer.BaseStream.Position += writer.BaseStream.Position & 1;
            writer.WriteUTF8("fmt ");
            writer.Write(FmtChunkSize);
            writer.Write((short)(ChannelCount > 2 ? WaveFormatTags.WaveFormatExtensible : WaveFormatTags.WaveFormatPcm));
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
                writer.Write(MediaSubtypes.MediaSubtypePcm.ToByteArray());
            }
        }

        private void WriteDataChunk(BinaryWriter writer)
        {
            writer.BaseStream.Position += writer.BaseStream.Position & 1;
            writer.WriteUTF8("data");
            writer.Write(DataChunkSize);

            switch (Codec)
            {
                case WaveCodec.Pcm16Bit:
                    byte[] audioData = Pcm16.Channels.ShortToInterleavedByte();
                    writer.BaseStream.Write(audioData, 0, audioData.Length);
                    break;
                case WaveCodec.Pcm8Bit:
                    Pcm8.Channels.Interleave(writer.BaseStream, BytesPerSample);
                    break;
            }
        }

        private void WriteSmplChunk(BinaryWriter writer)
        {
            writer.BaseStream.Position += writer.BaseStream.Position & 1;
            writer.WriteUTF8("smpl");
            writer.Write(SmplChunkSize);
            for (int i = 0; i < 7; i++)
                writer.Write(0);
            writer.Write(1);
            for (int i = 0; i < 3; i++)
                writer.Write(0);
            writer.Write(LoopStart);
            writer.Write(LoopEnd);
            writer.Write(0);
            writer.Write(0);
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
