using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static DspAdpcm.Lib.Helpers;

namespace DspAdpcm.Lib.Pcm.Formats
{
    /// <summary>
    /// Represents a PCM WAVE file
    /// </summary>
    public class Wave
    {
        /// <summary>
        /// The underlying <see cref="PcmStream"/> used to build the WAVE file
        /// </summary>
        public PcmStream AudioStream { get; }
        private int NumChannelsReading { get; set; } //used when reading in a wave file
        private int NumChannels => AudioStream.Channels.Count;
        private int BitDepth { get; set; } = 16;
        private int BytesPerSample => BitDepth.DivideByRoundUp(8);
        private int NumSamples => AudioStream.NumSamples;
        private int SampleRate => AudioStream.SampleRate;
        private IList<PcmChannel> Channels => AudioStream.Channels;

        // ReSharper disable InconsistentNaming
        private static readonly Guid KSDATAFORMAT_SUBTYPE_PCM =
            new Guid("00000001-0000-0010-8000-00aa00389b71");
        private const ushort WAVE_FORMAT_PCM = 1;
        private const ushort WAVE_FORMAT_EXTENSIBLE = 0xfffe;
        // ReSharper restore InconsistentNaming

        private int FileLength => 8 + RiffChunkLength;
        private int RiffChunkLength => 4 + 8 + FmtChunkLength + 8 + DataChunkLength;
        private int FmtChunkLength => NumChannels > 2 ? 40 : 16;
        private int DataChunkLength => NumChannels * NumSamples * sizeof(short);

        private int BytesPerSecond => SampleRate * BytesPerSample * NumChannels;
        private int BlockAlign => BytesPerSample * NumChannels;

        /// <summary>
        /// Initializes a new <see cref="Wave"/> by parsing an existing
        /// WAVE file.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> containing 
        /// the WAVE file. Must be seekable.</param>
        public Wave(Stream stream)
        {
            if (!stream.CanSeek)
            {
                throw new NotSupportedException("A seekable stream is required");
            }

            AudioStream = new PcmStream();
            ReadWaveFile(stream);
        }

        /// <summary>
        /// Initializes a new <see cref="Wave"/> from a <see cref="PcmStream"/>
        /// </summary>
        /// <param name="stream">The <see cref="PcmStream"/> used to
        /// create the <see cref="Wave"/></param>
        public Wave(PcmStream stream)
        {
            AudioStream = stream;
        }

        /// <summary>
        /// Builds a WAVE file from the current <see cref="AudioStream"/>.
        /// </summary>
        /// <returns>A WAVE file</returns>
        public byte[] GetFile()
        {
            var file = new byte[FileLength];
            var stream = new MemoryStream(file);
            WriteFile(stream);
            return file;
        }

        /// <summary>
        /// Writes the WAVE file to a <see cref="Stream"/>.
        /// The file is written starting at the beginning
        /// of the <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to write the
        /// WAVE to.</param>
        public void WriteFile(Stream stream)
        {
            if (stream.Length != FileLength)
            {
                try
                {
                    stream.SetLength(FileLength);
                }
                catch (NotSupportedException ex)
                {
                    throw new ArgumentException("Stream is too small.", nameof(stream), ex);
                }
            }

            stream.Position = 0;
            GetRiffHeader(stream);
            GetFmtChunk(stream);
            GetDataChunk(stream);
        }

        private void GetRiffHeader(Stream stream)
        {
            var header = new BinaryWriter(stream);

            header.WriteASCII("RIFF");
            header.Write(RiffChunkLength);
            header.WriteASCII("WAVE");
        }

        private void GetFmtChunk(Stream stream)
        {
            var chunk = new BinaryWriter(stream);

            chunk.WriteASCII("fmt ");
            chunk.Write(FmtChunkLength);
            chunk.Write((short)(NumChannels > 2 ? WAVE_FORMAT_EXTENSIBLE : WAVE_FORMAT_PCM));
            chunk.Write((short)NumChannels);
            chunk.Write(SampleRate);
            chunk.Write(BytesPerSecond);
            chunk.Write((short)BlockAlign);
            chunk.Write((short)BitDepth);

            if (NumChannels > 2)
            {
                chunk.Write((short)22);
                chunk.Write((short)BitDepth);
                chunk.Write(GetChannelMask(NumChannels));
                chunk.Write(KSDATAFORMAT_SUBTYPE_PCM.ToByteArray());
            }
        }

        private void GetDataChunk(Stream stream)
        {
            var chunk = new BinaryWriter(stream);

            chunk.WriteASCII("data");
            chunk.Write(DataChunkLength);
            byte[][] channels = AudioStream.Channels
                .Select(x =>
                {
                    byte[] bytes = new byte[x.AudioData.Length * sizeof(short)];
                    Buffer.BlockCopy(x.AudioData, 0, bytes, 0, bytes.Length);
                    return bytes;
                })
                .ToArray();

            channels.Interleave(stream, NumSamples * BytesPerSample, BytesPerSample);
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
                NumChannelsReading = reader.ReadInt16();
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

                if (blockAlign != BytesPerSample * NumChannelsReading)
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
            AudioStream.NumSamples = chunkSize / 2 / NumChannelsReading;

            int extraBytes = chunkSize % (NumChannelsReading * BytesPerSample);
            if (extraBytes != 0)
            {
                throw new InvalidDataException($"{extraBytes} extra bytes at end of audio data chunk");
            }

            ReadDataChunkInMemory(reader);
        }

        //Much faster, but 3X memory usage
        private void ReadDataChunkInMemory(BinaryReader reader)
        {
            byte[] audioBytes = reader.ReadBytes(NumSamples * NumChannelsReading * 2);
            if (audioBytes.Length != NumSamples * NumChannelsReading * 2)
            {
                throw new InvalidDataException("Incomplete Wave file");
            }

            var interlacedSamples = new short[NumSamples * NumChannelsReading];
            Buffer.BlockCopy(audioBytes, 0, interlacedSamples, 0, audioBytes.Length);

            if (NumChannelsReading == 1)
            {
                Channels.Add(new PcmChannel(NumSamples, interlacedSamples));
                return;
            }

            for (int i = 0; i < NumChannelsReading; i++)
            {
                Channels.Add(new PcmChannel(NumSamples));
            }

            for (int s = 0; s < NumSamples; s++)
            {
                for (int c = 0; c < NumChannelsReading; c++)
                {
                    Channels[c].AddSample(interlacedSamples[s * NumChannelsReading + c]);
                }
            }
        }

        private void ReadDataChunk(BinaryReader reader)
        {
            for (int i = 0; i < NumChannelsReading; i++)
            {
                Channels.Add(new PcmChannel(NumSamples));
            }

            for (int s = 0; s < NumSamples; s++)
            {
                for (int c = 0; c < NumChannelsReading; c++)
                {
                    Channels[c].AddSample(reader.ReadInt16());
                }
            }
        }
    }
}
