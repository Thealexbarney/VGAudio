using System;
using System.IO;
using System.Text;
using DspAdpcm.Containers.Structures;
using static DspAdpcm.Helpers;

namespace DspAdpcm.Containers
{
    public class Wave : IReadableAudio
    {
        // ReSharper disable InconsistentNaming
        private static readonly Guid KSDATAFORMAT_SUBTYPE_PCM =
            new Guid("00000001-0000-0010-8000-00aa00389b71");
        private const ushort WAVE_FORMAT_PCM = 1;
        private const ushort WAVE_FORMAT_EXTENSIBLE = 0xfffe;
        // ReSharper restore InconsistentNaming

        AudioStream IReadableAudio.Read(Stream stream) => Read(stream);
        AudioStream IReadableAudio.Read(byte[] file) => Read(file);

        public static AudioStream Read(Stream stream)
        {
            if (!stream.CanSeek)
            {
                throw new NotSupportedException("A seekable stream is required");
            }

            WaveStructure wave = ReadWaveFile(stream);
            return GetAudioStream(wave);
        }
        public static AudioStream Read(byte[] file)
        {
            using (var stream = new MemoryStream(file))
            {
                return Read(stream);
            }
        }

        private static WaveStructure ReadWaveFile(Stream stream, bool readAudioData = true)
        {
            using (BinaryReader reader = GetBinaryReader(stream, Endianness.LittleEndian))
            {
                var structure = new WaveStructure();

                ParseRiffHeader(reader, structure);

                byte[] chunkId = new byte[4];
                while (reader.Read(chunkId, 0, 4) == 4)
                {
                    int chunkSize = reader.ReadInt32();
                    if (Encoding.UTF8.GetString(chunkId, 0, 4) == "fmt ")
                    {
                        ParseFmtChunk(reader, structure);
                    }
                    else if (Encoding.UTF8.GetString(chunkId, 0, 4) == "data")
                    {
                        if (!readAudioData)
                        {
                            structure.NumSamples = chunkSize / structure.BytesPerSample / structure.NumChannels;
                            return structure;
                        }
                        ParseDataChunk(reader, chunkSize, structure);
                        break;
                    }
                    else
                        reader.BaseStream.Seek(chunkSize, SeekOrigin.Current);
                }

                if ((structure.AudioData?.Length ?? 0) == 0)
                {
                    throw new InvalidDataException("Must have a valid data chunk following a fmt chunk");
                }

                return structure;
            }
        }

        private static AudioStream GetAudioStream(WaveStructure structure)
        {
            var audioStream = new AudioStream(structure.NumSamples, structure.SampleRate);

            for (int i = 0; i < structure.NumChannels; i++)
            {
                audioStream.Pcm16.AddChannel(structure.AudioData[i]);
            }

            return audioStream;
        }

        private static void ParseRiffHeader(BinaryReader reader, WaveStructure structure)
        {
            byte[] riffChunkId = reader.ReadBytes(4);
            structure.RiffSize = reader.ReadInt32();
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

        private static void ParseFmtChunk(BinaryReader reader, WaveStructure structure)
        {
            structure.FormatTag = reader.ReadUInt16();
            structure.NumChannels = reader.ReadInt16();
            structure.SampleRate = reader.ReadInt32();
            structure.AvgBytesPerSec = reader.ReadInt32();
            structure.BlockAlign = reader.ReadInt16();
            structure.BitsPerSample = reader.ReadInt16();

            if (structure.FormatTag == WAVE_FORMAT_EXTENSIBLE)
            {
                ParseWaveFormatExtensible(reader, structure);
            }

            if (structure.FormatTag != WAVE_FORMAT_PCM && structure.FormatTag != WAVE_FORMAT_EXTENSIBLE)
            {
                throw new InvalidDataException($"Must contain PCM data. Has invalid format {structure.FormatTag}");
            }

            if (structure.BitsPerSample != 16)
            {
                throw new InvalidDataException($"Must have 16 bits per sample, not {structure.BitsPerSample} bits per sample");
            }

            if (structure.BlockAlign != structure.BytesPerSample * structure.NumChannels)
            {
                throw new InvalidDataException("File has invalid block alignment");
            }
        }

        private static void ParseWaveFormatExtensible(BinaryReader reader, WaveStructure structure)
        {
            structure.CbSize = reader.ReadInt16();
            if (structure.CbSize != 22) return;

            structure.ValidBitsPerSample = reader.ReadInt16();
            if (structure.ValidBitsPerSample > structure.BitsPerSample)
            {
                throw new InvalidDataException("Inconsistent bits per sample");
            }
            structure.ChannelMask = reader.ReadUInt32();

            structure.SubFormat = new Guid(reader.ReadBytes(16));
            if (!structure.SubFormat.Equals(KSDATAFORMAT_SUBTYPE_PCM))
            {
                throw new InvalidDataException($"Must contain PCM data. Has invalid format {structure.SubFormat}");
            }
        }

        private static void ParseDataChunk(BinaryReader reader, int chunkSize, WaveStructure structure)
        {
            structure.NumSamples = chunkSize / structure.BytesPerSample / structure.NumChannels;

            int extraBytes = chunkSize % (structure.NumChannels * structure.BytesPerSample);
            if (extraBytes != 0)
            {
                throw new InvalidDataException($"{extraBytes} extra bytes at end of audio data chunk");
            }

            byte[] interleavedAudio = reader.ReadBytes(chunkSize);
            if (interleavedAudio.Length != chunkSize)
            {
                throw new InvalidDataException("Incomplete Wave file");
            }

            structure.AudioData = InterleavedByteToShort(interleavedAudio, structure.NumChannels);
        }

        private static short[][] InterleavedByteToShort(byte[] input, int numOutputs)
        {
            int numItems = input.Length / 2 / numOutputs;
            short[][] output = new short[numOutputs][];
            for (int i = 0; i < numOutputs; i++)
            {
                output[i] = new short[numItems];
            }

            for (int i = 0; i < numItems; i++)
            {
                for (int o = 0; o < numOutputs; o++)
                {
                    int offset = (i * numOutputs + o) * 2;
                    output[o][i] = (short)(input[offset] | (input[offset + 1] << 8));
                }
            }

            return output;
        }

        private static byte[] ShortToInterleavedByte(short[][] input)
        {
            int numInputs = input.Length;
            int length = input[0].Length;
            byte[] output = new byte[numInputs * length * 2];

            for (int i = 0; i < length; i++)
            {
                for (int j = 0; j < numInputs; j++)
                {
                    int offset = (i * numInputs + j) * 2;
                    output[offset] = (byte)input[j][i];
                    output[offset + 1] = (byte)(input[j][i] >> 8);
                }
            }

            return output;
        }
    }
}
