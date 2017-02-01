using System;
using System.IO;
using DspAdpcm.Utilities;
using static DspAdpcm.Utilities.Helpers;

namespace DspAdpcm.Containers
{
    public class WaveWriter : IAudioWriter
    {
        private AudioStream AudioStream { get; set; }
        private int NumChannels => AudioStream.NumChannels;
        private int NumSamples => AudioStream.NumSamples;
        private int SampleRate => AudioStream.SampleRate;
        private int FileSize => 8 + RiffChunkSize;
        private int RiffChunkSize => 4 + 8 + FmtChunkSize + 8 + DataChunkSize;
        private int FmtChunkSize => NumChannels > 2 ? 40 : 16;
        private int DataChunkSize => NumChannels * NumSamples * sizeof(short);

        private int BitDepth => 16;
        private int BytesPerSample => BitDepth.DivideByRoundUp(8);
        private int BytesPerSecond => SampleRate * BytesPerSample * NumChannels;
        private int BlockAlign => BytesPerSample * NumChannels;

        // ReSharper disable InconsistentNaming
        private static readonly Guid KSDATAFORMAT_SUBTYPE_PCM =
            new Guid("00000001-0000-0010-8000-00aa00389b71");
        private const ushort WAVE_FORMAT_PCM = 1;
        private const ushort WAVE_FORMAT_EXTENSIBLE = 0xfffe;
        // ReSharper restore InconsistentNaming

        byte[] IAudioWriter.GetFile(AudioStream audio) => GetFile(audio);
        void IAudioWriter.WriteToStream(AudioStream audio, Stream stream) => WriteToStream(audio, stream);

        public static byte[] GetFile(AudioStream audio) => new WaveWriter(audio).GetFile();
        public static void WriteToStream(AudioStream audio, Stream stream) => new WaveWriter(audio).WriteToStream(stream);

        private WaveWriter(AudioStream audio)
        {
            AudioStream = audio;
        }

        private byte[] GetFile()
        {
            var file = new byte[FileSize];
            var stream = new MemoryStream(file);
            WriteToStream(AudioStream, stream);
            return file;
        }

        /// <summary>
        /// Writes the WAVE file to a <see cref="Stream"/>.
        /// The file is written starting at the beginning
        /// of the <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to write the
        /// WAVE to.</param>
        public void WriteToStream(Stream stream)
        {
            if (stream.Length != FileSize)
            {
                try
                {
                    stream.SetLength(FileSize);
                }
                catch (NotSupportedException ex)
                {
                    throw new ArgumentException("Stream is too small.", nameof(stream), ex);
                }
            }

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
            writer.Write((short)(NumChannels > 2 ? WAVE_FORMAT_EXTENSIBLE : WAVE_FORMAT_PCM));
            writer.Write((short)NumChannels);
            writer.Write(SampleRate);
            writer.Write(BytesPerSecond);
            writer.Write((short)BlockAlign);
            writer.Write((short)BitDepth);

            if (NumChannels > 2)
            {
                writer.Write((short)22);
                writer.Write((short)BitDepth);
                writer.Write(GetChannelMask(NumChannels));
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

        private static int GetChannelMask(int numChannels)
        {
            //Nothing special about these masks. I just choose
            //whatever channel combinations seemed okay.
            switch (numChannels)
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
                    return (1 << numChannels) - 1;
            }
        }
    }
}
