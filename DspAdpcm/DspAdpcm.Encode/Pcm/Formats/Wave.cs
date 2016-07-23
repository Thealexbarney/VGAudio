using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DspAdpcm.Encode.Pcm.Formats
{
    public class Wave
    {
        public IPcmStream AudioStream { get; set; }
        private int NumChannels { get; set; }
        private int BitDepth { get; set; }
        private int BytesPerSample => (int)Math.Ceiling((double)BitDepth / 8);
        private int NumSamples => AudioStream.NumSamples;
        private IList<IPcmChannel> Channels => AudioStream.Channels;

        public Wave(Stream stream)
        {
            AudioStream = new PcmStream();
            ReadWaveFile(stream);
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
                int fmtCode = reader.ReadInt16();
                NumChannels = reader.ReadInt16();
                AudioStream.SampleRate = reader.ReadInt32();
                int fmtAvgBps = reader.ReadInt32();
                int blockAlign = reader.ReadInt16();
                BitDepth = reader.ReadInt16();

                if (fmtCode != 1)
                {
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
