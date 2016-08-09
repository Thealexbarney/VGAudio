using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static DspAdpcm.Encode.Helpers;

namespace DspAdpcm.Encode.Pcm.Formats
{
    public class Wave
    {
        public PcmStream AudioStream { get; set; }
        private int NumChannels { get; set; }
        private int BitDepth { get; set; }
        private int BytesPerSample => (int)Math.Ceiling((double)BitDepth / 8);
        private int NumSamples => AudioStream.NumSamples;
        private int SampleRate => AudioStream.SampleRate;
        private IList<PcmChannel> Channels => AudioStream.Channels;

        // ReSharper disable InconsistentNaming
        private static readonly Guid KSDATAFORMAT_SUBTYPE_PCM =
            new Guid("00000001-0000-0010-8000-00aa00389b71");
        private const ushort WAVE_FORMAT_PCM = 1;
        private const ushort WAVE_FORMAT_EXTENSIBLE = 0xfffe;
        // ReSharper restore InconsistentNaming

        private int RiffChunkLength => 4 + 8 + FmtChunkLength + 8 + DataChunkLength;
        private int FmtChunkLength => NumChannels > 2 ? 40 : 16;
        private int DataChunkLength => NumChannels * NumSamples * sizeof(short);

        private int BytesPerSecond => SampleRate * BytesPerSample * NumChannels;
        private int BlockAlign => BytesPerSample * NumChannels;


        public Wave(Stream stream)
        {
            AudioStream = new PcmStream();
            ReadWaveFile(stream);
        }

        public IEnumerable<byte> GetFile()
        {
            return Combine(GetRiffHeader(), GetFmtChunk(), GetDataChunk());
        }

        private byte[] GetRiffHeader()
        {
            var header = new List<byte>();

            header.Add32("RIFF");
            header.Add32(RiffChunkLength);
            header.Add32("WAVE");

            return header.ToArray();
        }

        private byte[] GetFmtChunk()
        {
            var chunk = new List<byte>();

            chunk.Add32("fmt ");
            chunk.Add32(FmtChunkLength);
            chunk.Add16(NumChannels > 2 ? WAVE_FORMAT_EXTENSIBLE : WAVE_FORMAT_PCM);
            chunk.Add16((short)NumChannels);
            chunk.Add32(SampleRate);
            chunk.Add32(BytesPerSecond);
            chunk.Add16((short)BlockAlign);
            chunk.Add16((short)BitDepth);

            if (NumChannels > 2)
            {
                chunk.Add16(22);
                chunk.Add16((short)BitDepth);
                chunk.Add32(0xff);
                chunk.AddRange(KSDATAFORMAT_SUBTYPE_PCM.ToByteArray());
            }

            return chunk.ToArray();
        }

        private byte[] GetDataChunk()
        {
            var chunk = new List<byte>();

            chunk.Add32("data");
            chunk.Add32(DataChunkLength);
            short[][] channels = AudioStream.Channels.Select(x => x.AudioData).ToArray();
            short[] interleavedAudio = channels.Interleave(1);
            byte[] interleavedBytes = new byte[interleavedAudio.Length * sizeof(short)];
            Buffer.BlockCopy(interleavedAudio, 0, interleavedBytes, 0, interleavedBytes.Length);
            chunk.AddRange(interleavedBytes);

            return chunk.ToArray();
        }

        private void ReadWaveFile(Stream stream)
        {
            using (var reader = new BinaryReader(stream))
            {
                ParseRiffHeader(reader);

                byte[] chunkId = new byte[4];
                while (reader.Read(chunkId, 0, 4) == 4)
                {
                    int chunkSize = reader.ReadInt32();
                    if (Encoding.UTF8.GetString(chunkId, 0, 4) == "fmt ")
                    {
                        ParseFmtChunk(reader.ReadBytes(chunkSize));
                    }
                    else if (Encoding.UTF8.GetString(chunkId, 0, 4) == "data")
                    {
                        ParseDataChunk(reader, chunkSize);
                        break;
                    }
                    else
                        reader.BaseStream.Seek(chunkSize, SeekOrigin.Current);
                }

                if (AudioStream.Channels.Count == 0)
                {
                    throw new InvalidDataException("Must have a valid data chunk following a fmt chunk");
                }
            }
        }

        private static void ParseRiffHeader(BinaryReader reader)
        {
            byte[] riffChunkId = reader.ReadBytes(4);
            int fileSize = reader.ReadInt32();
            byte[] riffType = reader.ReadBytes(4);

            if (Encoding.UTF8.GetString(riffChunkId, 0, 4) != "RIFF")
            {
                throw new InvalidDataException("Not a valid RIFF file");
            }

            if (Encoding.UTF8.GetString(riffType, 0, 4) != "WAVE")
            {
                throw new InvalidDataException("Not a valid WAVE file");
            }
        }

        private void ParseFmtChunk(byte[] chunk)
        {
            using (var reader = new BinaryReader(new MemoryStream(chunk)))
            {
                int fmtCode = reader.ReadUInt16();
                NumChannels = reader.ReadInt16();
                AudioStream.SampleRate = reader.ReadInt32();
                int fmtAvgBps = reader.ReadInt32();
                int blockAlign = reader.ReadInt16();
                BitDepth = reader.ReadInt16();

                if (fmtCode == WAVE_FORMAT_EXTENSIBLE)
                {
                    ParseWaveFormatExtensible(reader);
                }

                if (fmtCode != WAVE_FORMAT_PCM && fmtCode != WAVE_FORMAT_EXTENSIBLE)
                {
                    var a = KSDATAFORMAT_SUBTYPE_PCM;
                    throw new InvalidDataException($"Must contain PCM data. Has invalid format {fmtCode}");
                }

                if (BitDepth != 16)
                {
                    throw new InvalidDataException($"Must have 16 bits per sample, not {BitDepth} bits per sample");
                }

                if (blockAlign != BytesPerSample * NumChannels)
                {
                    throw new InvalidDataException("File has invalid block alignment");
                }
            }
        }

        private void ParseWaveFormatExtensible(BinaryReader reader)
        {
            int cbSize = reader.ReadInt16();
            if (cbSize != 22) return;

            int wValidBitsPerSample = reader.ReadInt16();
            if (wValidBitsPerSample > BitDepth)
            {
                throw new InvalidDataException("Inconsistent bits per sample");
            }
            uint channelMask = reader.ReadUInt32();

            var subFormat = new Guid(reader.ReadBytes(16));
            if (!subFormat.Equals(KSDATAFORMAT_SUBTYPE_PCM))
            {
                throw new InvalidDataException($"Must contain PCM data. Has invalid format {subFormat}");
            }
        }

        private void ParseDataChunk(BinaryReader reader, int chunkSize)
        {
            AudioStream.NumSamples = chunkSize / 2 / NumChannels;

            int extraBytes = chunkSize % (NumChannels * BytesPerSample);
            if (extraBytes != 0)
            {
                throw new InvalidDataException($"{extraBytes} extra bytes at end of audio data chunk");
            }

            ReadDataChunkInMemory(reader);
        }

        //Much faster, but 3X memory usage
        private void ReadDataChunkInMemory(BinaryReader reader)
        {
            byte[] audioBytes = reader.ReadBytes(NumSamples * NumChannels * 2);
            if (audioBytes.Length != NumSamples * NumChannels * 2)
            {
                throw new InvalidDataException("Incomplete Wave file");
            }

            var interlacedSamples = new short[NumSamples * NumChannels];
            Buffer.BlockCopy(audioBytes, 0, interlacedSamples, 0, audioBytes.Length);

            if (NumChannels == 1)
            {
                Channels.Add(new PcmChannel(interlacedSamples));
                return;
            }

            for (int i = 0; i < NumChannels; i++)
            {
                Channels.Add(new PcmChannel(NumSamples));
            }

            for (int s = 0; s < NumSamples; s++)
            {
                for (int c = 0; c < NumChannels; c++)
                {
                    Channels[c].AddSample(interlacedSamples[s * NumChannels + c]);
                }
            }
        }

        private void ReadDataChunk(BinaryReader reader)
        {
            for (int i = 0; i < NumChannels; i++)
            {
                Channels.Add(new PcmChannel(NumSamples));
            }

            for (int s = 0; s < NumSamples; s++)
            {
                for (int c = 0; c < NumChannels; c++)
                {
                    Channels[c].AddSample(reader.ReadInt16());
                }
            }
        }
    }
}
