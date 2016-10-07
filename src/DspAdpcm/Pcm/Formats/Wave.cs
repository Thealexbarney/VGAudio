using System;
using System.IO;
using System.Linq;
using System.Text;
using DspAdpcm.Compatibility;

namespace DspAdpcm.Pcm.Formats
{
    /// <summary>
    /// Represents a PCM WAVE file
    /// </summary>
    public class Wave
    {
        /// <summary>
        /// The underlying <see cref="PcmStream"/> used to build the WAVE file
        /// </summary>
        public PcmStream AudioStream { get; set; }
        private int NumChannels => AudioStream.Channels.Count;
        private int NumSamples => AudioStream.NumSamples;
        private int SampleRate => AudioStream.SampleRate;

        // ReSharper disable InconsistentNaming
        private static readonly Guid KSDATAFORMAT_SUBTYPE_PCM =
            new Guid("00000001-0000-0010-8000-00aa00389b71");
        private const ushort WAVE_FORMAT_PCM = 1;
        private const ushort WAVE_FORMAT_EXTENSIBLE = 0xfffe;
        // ReSharper restore InconsistentNaming

        /// <summary>
        /// The size in bytes of the WAVE file.
        /// </summary>
        public int FileSize => 8 + RiffChunkSize;
        private int RiffChunkSize => 4 + 8 + FmtChunkSize + 8 + DataChunkSize;
        private int FmtChunkSize => NumChannels > 2 ? 40 : 16;
        private int DataChunkSize => NumChannels * NumSamples * sizeof(short);

        private int BitDepth => 16;
        private int BytesPerSample => BitDepth.DivideByRoundUp(8);
        private int BytesPerSecond => SampleRate * BytesPerSample * NumChannels;
        private int BlockAlign => BytesPerSample * NumChannels;

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

            WaveStructure wave = ReadWaveFile(stream);
            AudioStream = GetPcmStream(wave);
        }

        /// <summary>
        /// Initializes a new <see cref="Wave"/> by parsing an existing
        /// WAVE file.
        /// </summary>
        /// <param name="file">A <c>byte[]</c> containing 
        /// the WAVE file.</param>
        public Wave(byte[] file)
        {
            using (var stream = new MemoryStream(file))
            {
                WaveStructure wave = ReadWaveFile(stream);
                AudioStream = GetPcmStream(wave);
            }
        }

        /// <summary>
        /// Parses the header of a WAVE file and returns the metadata
        /// and structure data of that file.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> containing 
        /// the WAVE file. Must be seekable.</param>
        /// <returns>A <see cref="WaveStructure"/> containing
        /// the data from the WAVE header.</returns>
        public static WaveStructure ReadMetadata(Stream stream)
        {
            if (!stream.CanSeek)
            {
                throw new NotSupportedException("A seekable stream is required");
            }

            return ReadWaveFile(stream, false);
        }

        /// <summary>
        /// Builds a WAVE file from the current <see cref="AudioStream"/>.
        /// </summary>
        /// <returns>A WAVE file</returns>
        public byte[] GetFile()
        {
            var file = new byte[FileSize];
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

            using (BinaryWriter writer = GetStream.GetBinaryWriter(stream))
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
            short[][] channels = AudioStream.Channels
                .Select(x => x.AudioData)
                .ToArray();

            var audioData = ShortToInterleavedByte(channels);
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

        private static WaveStructure ReadWaveFile(Stream stream, bool readAudioData = true)
        {
            using (BinaryReader reader = GetStream.GetBinaryReader(stream))
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

        private static PcmStream GetPcmStream(WaveStructure structure)
        {
            var audioStream = new PcmStream(structure.NumSamples, structure.SampleRate);

            for (int i = 0; i < structure.NumChannels; i++)
            {
                audioStream.Channels.Add(new PcmChannel(structure.NumSamples, structure.AudioData[i]));
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
