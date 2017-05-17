using System;
using System.IO;
using VGAudio.Containers.Wave;
using VGAudio.Formats;
using VGAudio.Utilities;
using static VGAudio.Utilities.Helpers;

namespace VGAudio.Containers
{
    public class WaveReader : AudioReader<WaveReader, WaveStructure, WaveConfiguration>
    {
        // ReSharper disable InconsistentNaming
        private static readonly Guid KSDATAFORMAT_SUBTYPE_PCM =
            new Guid("00000001-0000-0010-8000-00aa00389b71");
        private const ushort WAVE_FORMAT_PCM = 1;
        private const ushort WAVE_FORMAT_EXTENSIBLE = 0xfffe;
        // ReSharper restore InconsistentNaming

        protected override WaveStructure ReadFile(Stream stream, bool readAudioData = true)
        {
            using (BinaryReader reader = GetBinaryReader(stream, Endianness.LittleEndian))
            {
                var structure = new WaveStructure();
                bool dataChunkRead = false;

                ReadRiffHeader(reader, structure);

                while (!reader.Eof())
                {
                    string chunkId = reader.ReadUTF8(4);
                    int chunkDataSize = reader.ReadInt32();
                    long nextChunkOffset = reader.BaseStream.Position + chunkDataSize;

                    switch (chunkId)
                    {
                        case "fmt ":
                            ReadFmtChunk(reader, structure);
                            break;
                        case "data":
                            if (!readAudioData)
                            {
                                structure.SampleCount = chunkDataSize / structure.BytesPerSample / structure.ChannelCount;
                                return structure;
                            }
                            ReadDataChunk(reader, chunkDataSize, structure);
                            dataChunkRead = true;
                            break;
                        case "smpl":
                            ReadSmplChunk(reader, structure);
                            break;
                    }

                    reader.BaseStream.Position = nextChunkOffset;
                }

                if (!dataChunkRead)
                {
                    throw new InvalidDataException("Must have a valid data chunk following a fmt chunk");
                }

                return structure;
            }
        }

        protected override IAudioFormat ToAudioStream(WaveStructure structure)
        {
            switch (structure.BitsPerSample)
            {
                case 16:
                    return new Pcm16Format.Builder(structure.AudioData16, structure.SampleRate)
                        .Loop(structure.Looping, structure.LoopStart, structure.LoopEnd)
                        .Build();
                case 8:
                    return new Pcm8Format.Builder(structure.AudioData8, structure.SampleRate)
                        .Loop(structure.Looping, structure.LoopStart, structure.LoopEnd)
                        .Build();
                default:
                    return null;
            }
        }

        private static void ReadRiffHeader(BinaryReader reader, WaveStructure structure)
        {
            string riffChunkId = reader.ReadUTF8(4);
            structure.RiffSize = reader.ReadInt32();
            string riffType = reader.ReadUTF8(4);

            if (riffChunkId != "RIFF")
            {
                throw new InvalidDataException("Not a valid RIFF file");
            }

            if (riffType != "WAVE")
            {
                throw new InvalidDataException("Not a valid WAVE file");
            }
        }

        private static void ReadFmtChunk(BinaryReader reader, WaveStructure structure)
        {
            structure.FormatTag = reader.ReadUInt16();
            structure.ChannelCount = reader.ReadInt16();
            structure.SampleRate = reader.ReadInt32();
            structure.AvgBytesPerSec = reader.ReadInt32();
            structure.BlockAlign = reader.ReadInt16();
            structure.BitsPerSample = reader.ReadInt16();

            if (structure.FormatTag == WAVE_FORMAT_EXTENSIBLE)
            {
                ReadWaveFormatExtensible(reader, structure);
            }

            if (structure.FormatTag != WAVE_FORMAT_PCM && structure.FormatTag != WAVE_FORMAT_EXTENSIBLE)
            {
                throw new InvalidDataException($"Must contain PCM data. Has unsupported format {structure.FormatTag}");
            }

            if (structure.BitsPerSample != 16 && structure.BitsPerSample != 8)
            {
                throw new InvalidDataException($"Must have 8 or 16 bits per sample, not {structure.BitsPerSample} bits per sample");
            }

            if (structure.BlockAlign != structure.BytesPerSample * structure.ChannelCount)
            {
                throw new InvalidDataException("File has invalid block alignment");
            }
        }

        private static void ReadWaveFormatExtensible(BinaryReader reader, WaveStructure structure)
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
                throw new InvalidDataException($"Must contain PCM data. Has unsupported format {structure.SubFormat}");
            }
        }

        private static void ReadDataChunk(BinaryReader reader, int chunkSize, WaveStructure structure)
        {
            structure.SampleCount = chunkSize / structure.BytesPerSample / structure.ChannelCount;

            int extraBytes = chunkSize % (structure.ChannelCount * structure.BytesPerSample);
            if (extraBytes != 0)
            {
                throw new InvalidDataException($"{extraBytes} extra bytes at end of audio data chunk");
            }

            byte[] interleavedAudio = reader.ReadBytes(chunkSize);
            if (interleavedAudio.Length != chunkSize)
            {
                throw new InvalidDataException("Incomplete Wave file");
            }

            switch (structure.BitsPerSample)
            {
                case 16:
                    structure.AudioData16 = interleavedAudio.InterleavedByteToShort(structure.ChannelCount);
                    break;
                case 8:
                    structure.AudioData8 = interleavedAudio.DeInterleave(structure.BytesPerSample, structure.ChannelCount);
                    break;
            }
        }

        private static void ReadSmplChunk(BinaryReader reader, WaveStructure structure)
        {
            reader.BaseStream.Position += 0x1c;
            int loopRegionCount = reader.ReadInt32();
            reader.BaseStream.Position += 4;

            if (loopRegionCount == 1)  // Supporting only 1 loop region for now
            {
                reader.BaseStream.Position += 8;
                structure.LoopStart = reader.ReadInt32();
                structure.LoopEnd = reader.ReadInt32();
                structure.Looping = structure.LoopEnd > structure.LoopStart;
                reader.BaseStream.Position += 8;
            }
        }
    }
}
