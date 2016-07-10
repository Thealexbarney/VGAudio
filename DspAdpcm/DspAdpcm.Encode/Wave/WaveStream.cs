using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DspAdpcm.Encode.Wave
{
    public class WaveStream : IAudioStream
    {
        public int NumSamples { get; set; }
        public int SampleRate { get; set; }
        public int NumChannels { get; set; }
        public int BitDepth { get; set; }
        public int BytesPerSample => (int)Math.Ceiling((double)BitDepth / 8);

        public short[][] AudioData { get; set; }

        public int GetNumSamples() => NumSamples;
        public int GetSampleRate() => SampleRate;
        public IEnumerable<short> GetAudioData()
        {
            return AudioData[0];
        }

        public WaveStream(Stream path)
        {
            OpenWaveFile(path);
        }

        private void OpenWaveFile(Stream stream)
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

                if (AudioData == null)
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
                SampleRate = reader.ReadInt32();
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
            NumSamples = chunkSize / 2 / NumChannels;

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

            var samples = new short[NumSamples * NumChannels];
            Buffer.BlockCopy(audioBytes, 0, samples, 0, audioBytes.Length);

            AudioData = new short[NumChannels][];

            if (NumChannels == 1)
            {
                AudioData[0] = samples;
                return;
            }

            for (int i = 0; i < AudioData.Length; i++)
            {
                AudioData[i] = new short[NumSamples];
            }

            for (int s = 0; s < NumSamples; s++)
            {
                for (int c = 0; c < NumChannels; c++)
                {
                    AudioData[c][s] = samples[s * NumChannels + c];
                }
            }
        }

        private void ReadDataChunk(BinaryReader reader)
        {
            AudioData = new short[NumChannels][];

            for (int i = 0; i < AudioData.Length; i++)
            {
                AudioData[i] = new short[NumSamples];
            }

            for (int s = 0; s < NumSamples; s++)
            {
                for (int c = 0; c < NumChannels; c++)
                {
                    AudioData[c][s] = reader.ReadInt16();
                }
            }
        }
    }
}
