using System;
using System.IO;
using System.Linq;
using System.Text;
using DspAdpcm.Adpcm.Formats.Structures;
using static DspAdpcm.Helpers;

namespace DspAdpcm.Adpcm.Formats.Internal
{
    /// <summary>
    /// Represents a BCSTM or BFSTM file.
    /// </summary> 
    internal class BCFstm
    {
        public AdpcmStream AudioStream { get; set; }

        public BCFstmConfiguration Configuration { get; internal set; } = new BCFstmConfiguration();

        private int NumSamples => AudioStream.Looping ? LoopEnd : AudioStream.NumSamples;
        private int NumChannels => AudioStream.Channels.Count;
        private int NumTracks => AudioStream.Tracks.Count;

        private int AlignmentSamples => GetNextMultiple(AudioStream.LoopStart, Configuration.LoopPointAlignment) - AudioStream.LoopStart;
        private int LoopStart => AudioStream.LoopStart + AlignmentSamples;
        private int LoopEnd => AudioStream.LoopEnd + AlignmentSamples;

        private static B_stmCodec Codec => B_stmCodec.Adpcm;
        private byte Looping => (byte)(AudioStream.Looping ? 1 : 0);
        private int AudioDataOffset => DataChunkOffset + 0x20;

        private int SamplesPerInterleave => Configuration.SamplesPerInterleave;
        private int InterleaveSize => GetBytesForAdpcmSamples(SamplesPerInterleave);
        private int InterleaveCount => NumSamples.DivideByRoundUp(SamplesPerInterleave);

        private int LastBlockSamples => NumSamples - ((InterleaveCount - 1) * SamplesPerInterleave);
        private int LastBlockSizeWithoutPadding => GetBytesForAdpcmSamples(LastBlockSamples);
        private int LastBlockSize => GetNextMultiple(LastBlockSizeWithoutPadding, 0x20);

        private int SamplesPerSeekTableEntry => Configuration.SamplesPerSeekTableEntry;
        private int BytesPerSeekTableEntry => 4;
        private int NumSeekTableEntries => NumSamples.DivideByRoundUp(SamplesPerSeekTableEntry);

        private static int HeaderSize => 0x40;

        private int InfoChunkOffset => HeaderSize;
        private int InfoChunkSize => GetNextMultiple(InfoChunkHeaderSize + InfoChunkTableSize +
            InfoChunk1Size + InfoChunk2Size + InfoChunk3Size, 0x20);
        private int InfoChunkHeaderSize => 8;
        private int InfoChunkTableSize => 8 * 3;
        private int InfoChunk1Size => 0x38 + (!Configuration.InfoPart1Extra ? 0 : 0xc) + (!Configuration.IncludeUnalignedLoopPoints ? 0 : 8);
        private int InfoChunk2Size => Configuration.IncludeTrackInformation ? 4 + 8 * NumTracks : 0;
        private int InfoChunk3Size => (4 + 8 * NumChannels) +
            (Configuration.IncludeTrackInformation ? 0x14 * NumTracks : 0) +
            8 * NumChannels +
            ChannelInfoSize * NumChannels;

        private int ChannelInfoSize => 0x2e;

        private int SeekChunkOffset => HeaderSize + InfoChunkSize;
        private int SeekChunkSize => GetNextMultiple(8 + NumSeekTableEntries * NumChannels * BytesPerSeekTableEntry, 0x20);

        private int DataChunkOffset => HeaderSize + InfoChunkSize + SeekChunkSize;
        private int DataChunkSize => 0x20 + GetNextMultiple(GetBytesForAdpcmSamples(NumSamples), 0x20) * NumChannels;

        private int GetVersion(BCFstmType type)
        {
            if (type == BCFstmType.Bfstm)
            {
                return Configuration.IncludeUnalignedLoopPoints ? 4 : 3;
            }

            //All BCSTM files I've seen follow this pattern except for Kingdom Hearts 3D
            if (Configuration.IncludeTrackInformation && Configuration.InfoPart1Extra)
                return 0x201;

            if (!Configuration.IncludeTrackInformation && Configuration.InfoPart1Extra)
                return 0x202;

            return 0x200;
        }

        /// <summary>
        /// The size in bytes of the file.
        /// </summary>
        public int FileSize => HeaderSize + InfoChunkSize + SeekChunkSize + DataChunkSize;

        public BCFstm(Stream stream, BCFstmConfiguration configuration = null)
        {
            CheckStream(stream, HeaderSize);

            BCFstmStructure bcfstm = ReadBCFstmFile(stream);
            AudioStream = GetAdpcmStream(bcfstm);
            Configuration = configuration ?? GetConfiguration(bcfstm);
        }

        public BCFstm() { }

        private void RecalculateData()
        {
            var seekTableToCalculate = Configuration.RecalculateSeekTable
                ? AudioStream.Channels.Where(
                    x => !x.SelfCalculatedSeekTable || x.SamplesPerSeekTableEntry != SamplesPerSeekTableEntry)
                : AudioStream.Channels.Where(
                    x => x.SeekTable == null || x.SamplesPerSeekTableEntry != SamplesPerSeekTableEntry);

            var loopContextToCalculate = Configuration.RecalculateLoopContext
                ? AudioStream.Channels.Where(x => !x.SelfCalculatedLoopContext)
                : AudioStream.Channels.Where(x => !x.LoopContextCalculated);

            if (AudioStream.Looping)
            {
                Decode.CalculateLoopAlignment(AudioStream.Channels, Configuration.LoopPointAlignment,
                    AudioStream.LoopStart, AudioStream.LoopEnd);
            }
            Decode.CalculateSeekTable(seekTableToCalculate, SamplesPerSeekTableEntry);
            Decode.CalculateLoopContext(loopContextToCalculate, AudioStream.Looping ? LoopStart : 0);
        }

        internal void WriteBCFstmFile(Stream stream, BCFstmType type)
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

            Endianness endianness = type == BCFstmType.Bcstm ? Endianness.LittleEndian : Endianness.BigEndian;

            RecalculateData();

            using (BinaryWriter writer = endianness == Endianness.LittleEndian ?
                GetBinaryWriter(stream) :
                GetBinaryWriterBE(stream))
            {
                stream.Position = 0;
                GetHeader(writer, type);
                stream.Position = InfoChunkOffset;
                GetInfoChunk(writer, endianness);
                stream.Position = SeekChunkOffset;
                GetSeekChunk(writer);
                stream.Position = DataChunkOffset;
                GetDataChunk(writer);
            }
        }

        private void GetHeader(BinaryWriter writer, BCFstmType type)
        {
            writer.WriteUTF8(type == BCFstmType.Bcstm ? "CSTM" : "FSTM");
            writer.Write((ushort)0xfeff); //Endianness
            writer.Write((short)HeaderSize);
            writer.Write(GetVersion(type) << 16);
            writer.Write(FileSize);

            writer.Write((short)3); // NumEntries
            writer.Write((short)0);
            writer.Write((short)0x4000);
            writer.Write((short)0);
            writer.Write(InfoChunkOffset);
            writer.Write(InfoChunkSize);
            writer.Write((short)0x4001);
            writer.Write((short)0);
            writer.Write(SeekChunkOffset);
            writer.Write(SeekChunkSize);
            writer.Write((short)0x4002);
            writer.Write((short)0);
            writer.Write(DataChunkOffset);
            writer.Write(DataChunkSize);
        }

        private void GetInfoChunk(BinaryWriter writer, Endianness endianness)
        {
            writer.WriteUTF8("INFO");
            writer.Write(InfoChunkSize);

            int headerTableSize = 8 * 3;

            writer.Write((short)0x4100);
            writer.Write((short)0);
            writer.Write(headerTableSize);
            if (Configuration.IncludeTrackInformation)
            {
                writer.Write((short)0x0101);
                writer.Write((short)0);
                writer.Write(headerTableSize + InfoChunk1Size);
            }
            else
            {
                writer.Write(0);
                writer.Write(-1);
            }
            writer.Write((short)0x0101);
            writer.Write((short)0);
            writer.Write(headerTableSize + InfoChunk1Size + InfoChunk2Size);

            GetInfoChunk1(writer);
            GetInfoChunk2(writer);
            GetInfoChunk3(writer, endianness);
        }

        private void GetInfoChunk1(BinaryWriter writer)
        {
            writer.Write((byte)Codec);
            writer.Write(Looping);
            writer.Write((byte)NumChannels);
            writer.Write((byte)0);
            writer.Write(AudioStream.SampleRate);
            writer.Write(LoopStart);
            writer.Write(NumSamples);
            writer.Write(InterleaveCount);
            writer.Write(InterleaveSize);
            writer.Write(SamplesPerInterleave);
            writer.Write(LastBlockSizeWithoutPadding);
            writer.Write(LastBlockSamples);
            writer.Write(LastBlockSize);
            writer.Write(BytesPerSeekTableEntry);
            writer.Write(SamplesPerSeekTableEntry);
            writer.Write((short)0x1f00);
            writer.Write((short)0);
            writer.Write(0x18);

            if (Configuration.InfoPart1Extra)
            {
                writer.Write((short)0x0100);
                writer.Write((short)0);
                writer.Write(0);
                writer.Write(-1);
            }

            if (Configuration.IncludeUnalignedLoopPoints)
            {
                writer.Write(AudioStream.LoopStart);
                writer.Write(AudioStream.LoopEnd);
            }
        }

        private void GetInfoChunk2(BinaryWriter writer)
        {
            if (!Configuration.IncludeTrackInformation) return;

            int trackTableSize = 4 + 8 * NumTracks;
            int channelTableSize = 4 + 8 * NumChannels;
            int trackSize = 0x14;

            writer.Write(NumTracks);

            for (int i = 0; i < NumTracks; i++)
            {
                writer.Write((short)0x4101);
                writer.Write((short)0);
                writer.Write(trackTableSize + channelTableSize + trackSize * i);
            }
        }

        private void GetInfoChunk3(BinaryWriter writer, Endianness endianness)
        {
            int channelTableSize = 4 + 8 * NumChannels;
            int trackTableSize = Configuration.IncludeTrackInformation ? 0x14 * NumTracks : 0;

            writer.Write(NumChannels);
            for (int i = 0; i < NumChannels; i++)
            {
                writer.Write((short)0x4102);
                writer.Write((short)0);
                writer.Write(channelTableSize + trackTableSize + 8 * i);
            }

            if (Configuration.IncludeTrackInformation)
            {
                foreach (var track in AudioStream.Tracks)
                {
                    writer.Write((byte)track.Volume);
                    writer.Write((byte)track.Panning);
                    writer.Write((short)0);
                    writer.Write(0x0100);
                    writer.Write(0xc);
                    writer.Write(track.NumChannels);
                    writer.Write((byte)track.ChannelLeft);
                    writer.Write((byte)track.ChannelRight);
                    writer.Write((short)0);
                }
            }

            int channelTable2Size = 8 * NumChannels;
            for (int i = 0; i < NumChannels; i++)
            {
                writer.Write((short)0x0300);
                writer.Write((short)0);
                writer.Write(channelTable2Size - 8 * i + ChannelInfoSize * i);
            }

            foreach (var channel in AudioStream.Channels)
            {
                writer.Write(channel.Coefs.ToByteArray(endianness));
                writer.Write(channel.PredScale);
                writer.Write(channel.Hist1);
                writer.Write(channel.Hist2);
                writer.Write(channel.LoopPredScale);
                writer.Write(channel.LoopHist1);
                writer.Write(channel.LoopHist2);
                writer.Write(channel.Gain);
            }
        }

        private void GetSeekChunk(BinaryWriter writer)
        {
            writer.WriteUTF8("SEEK");
            writer.Write(SeekChunkSize);

            var table = Decode.BuildSeekTable(AudioStream.Channels, SamplesPerSeekTableEntry, NumSeekTableEntries, Endianness.LittleEndian);

            writer.Write(table);
        }

        private void GetDataChunk(BinaryWriter writer)
        {
            writer.WriteUTF8("DATA");
            writer.Write(DataChunkSize);

            writer.BaseStream.Position = AudioDataOffset;

            byte[][] channels = AudioStream.Channels.Select(x => x.GetAudioData).ToArray();

            channels.Interleave(writer.BaseStream, GetBytesForAdpcmSamples(NumSamples), InterleaveSize, 0x20);
        }

        internal static BCFstmStructure ReadBCFstmFile(Stream stream, bool readAudioData = true)
        {
            BCFstmType type;
            using (BinaryReader reader = GetBinaryReader(stream))
            {
                string magic = Encoding.UTF8.GetString(reader.ReadBytes(4), 0, 4);
                switch (magic)
                {
                    case "CSTM":
                        type = BCFstmType.Bcstm;
                        break;
                    case "FSTM":
                        type = BCFstmType.Bfstm;
                        break;
                    default:
                        throw new InvalidDataException("File has no CSTM or FSTM header");
                }
            }

            using (BinaryReader reader = type == BCFstmType.Bcstm ?
                GetBinaryReader(stream) :
                GetBinaryReaderBE(stream))
            {
                BCFstmStructure structure = new BcstmStructure();

                ParseHeader(reader, structure);
                ParseInfoChunk(reader, structure);
                ParseSeekChunk(reader, structure);
                ParseDataChunk(reader, structure, readAudioData);

                return structure;
            }
        }

        private static BCFstmConfiguration GetConfiguration(BCFstmStructure structure)
        {
            return new BCFstmConfiguration
            {
                SamplesPerInterleave = structure.SamplesPerInterleave,
                SamplesPerSeekTableEntry = structure.SamplesPerSeekTableEntry,
                IncludeTrackInformation = structure.IncludeTracks,
                InfoPart1Extra = structure.InfoPart1Extra,
                IncludeUnalignedLoopPoints = structure.Version == 4
            };
        }

        private static AdpcmStream GetAdpcmStream(BCFstmStructure structure)
        {
            var audioStream = new AdpcmStream(structure.NumSamples, structure.SampleRate);
            if (structure.Looping)
            {
                audioStream.SetLoop(structure.LoopStart, structure.NumSamples);
            }
            audioStream.Tracks = structure.Tracks;

            for (int c = 0; c < structure.NumChannels; c++)
            {
                var channel = new AdpcmChannel(structure.NumSamples, structure.AudioData[c])
                {
                    Coefs = structure.Channels[c].Coefs,
                    Gain = structure.Channels[c].Gain,
                    Hist1 = structure.Channels[c].Hist1,
                    Hist2 = structure.Channels[c].Hist2,
                    SeekTable = structure.SeekTable?[c],
                    SamplesPerSeekTableEntry = structure.SamplesPerSeekTableEntry
                };
                channel.SetLoopContext(structure.Channels[c].LoopPredScale, structure.Channels[c].LoopHist1,
                    structure.Channels[c].LoopHist2);
                audioStream.Channels.Add(channel);
            }

            return audioStream;
        }

        private static void ParseHeader(BinaryReader reader, BCFstmStructure structure)
        {
            reader.Expect((ushort)0xfeff);
            structure.HeaderSize = reader.ReadInt16();
            structure.Version = reader.ReadInt32() >> 16;
            structure.FileSize = reader.ReadInt32();

            if (reader.BaseStream.Length < structure.FileSize)
            {
                throw new InvalidDataException("Actual file length is less than stated length");
            }

            structure.HeaderSections = reader.ReadInt16();
            reader.BaseStream.Position += 2;

            for (int i = 0; i < structure.HeaderSections; i++)
            {
                int type = reader.ReadInt16();
                reader.BaseStream.Position += 2;
                switch (type)
                {
                    case 0x4000:
                        structure.InfoChunkOffset = reader.ReadInt32();
                        structure.InfoChunkSizeHeader = reader.ReadInt32();
                        break;
                    case 0x4001:
                        structure.SeekChunkOffset = reader.ReadInt32();
                        structure.SeekChunkSizeHeader = reader.ReadInt32();
                        break;
                    case 0x4002:
                        structure.DataChunkOffset = reader.ReadInt32();
                        structure.DataChunkSizeHeader = reader.ReadInt32();
                        break;
                    case 0x4003:
                        structure.RegnChunkOffset = reader.ReadInt32();
                        structure.RegnChunkSizeHeader = reader.ReadInt32();
                        break;
                    case 0x4004:
                        structure.PdatChunkOffset = reader.ReadInt32();
                        structure.PdatChunkSizeHeader = reader.ReadInt32();
                        break;
                    default:
                        throw new InvalidDataException($"Unknown section type {type}");
                }
            }
        }

        private static void ParseInfoChunk(BinaryReader reader, BCFstmStructure structure)
        {
            reader.BaseStream.Position = structure.InfoChunkOffset;
            if (Encoding.UTF8.GetString(reader.ReadBytes(4), 0, 4) != "INFO")
            {
                throw new InvalidDataException("Unknown or invalid INFO chunk");
            }

            structure.InfoChunkSize = reader.ReadInt32();
            if (structure.InfoChunkSize != structure.InfoChunkSizeHeader)
            {
                throw new InvalidDataException("INFO chunk size in CSTM header doesn't match size in INFO header");
            }

            reader.Expect((short)0x4100);
            reader.BaseStream.Position += 2;
            structure.InfoChunk1Offset = reader.ReadInt32();
            reader.Expect((short)0x0101, (short)0);
            reader.BaseStream.Position += 2;
            structure.InfoChunk2Offset = reader.ReadInt32();
            reader.Expect((short)0x0101);
            reader.BaseStream.Position += 2;
            structure.InfoChunk3Offset = reader.ReadInt32();

            ParseInfoChunk1(reader, structure);
            ParseInfoChunk2(reader, structure);
            ParseInfoChunk3(reader, structure);
        }

        private static void ParseInfoChunk1(BinaryReader reader, BCFstmStructure structure)
        {
            reader.BaseStream.Position = structure.InfoChunkOffset + 8 + structure.InfoChunk1Offset;
            structure.Codec = (B_stmCodec)reader.ReadByte();
            if (structure.Codec != B_stmCodec.Adpcm)
            {
                throw new NotSupportedException("File must contain 4-bit ADPCM encoded audio");
            }

            structure.Looping = reader.ReadByte() == 1;
            structure.NumChannels = reader.ReadByte();
            reader.BaseStream.Position += 1;

            structure.SampleRate = reader.ReadInt32();

            structure.LoopStart = reader.ReadInt32();
            structure.NumSamples = reader.ReadInt32();

            structure.InterleaveCount = reader.ReadInt32();
            structure.InterleaveSize = reader.ReadInt32();
            structure.SamplesPerInterleave = reader.ReadInt32();
            structure.LastBlockSizeWithoutPadding = reader.ReadInt32();
            structure.LastBlockSamples = reader.ReadInt32();
            structure.LastBlockSize = reader.ReadInt32();
            structure.BytesPerSeekTableEntry = reader.ReadInt32();
            structure.SamplesPerSeekTableEntry = reader.ReadInt32();

            reader.Expect((short)0x1f00);
            reader.BaseStream.Position += 2;
            structure.AudioDataOffset = reader.ReadInt32() + structure.DataChunkOffset + 8;
            structure.InfoPart1Extra = reader.ReadInt16() == 0x100;
            if (structure.InfoPart1Extra)
            {
                reader.BaseStream.Position += 10;
            }
            if (structure.Version == 4)
            {
                structure.LoopStartUnaligned = reader.ReadInt32();
                structure.LoopEndUnaligned = reader.ReadInt32();
            }
        }

        private static void ParseInfoChunk2(BinaryReader reader, BCFstmStructure structure)
        {
            if (structure.InfoChunk2Offset == -1)
            {
                structure.IncludeTracks = false;
                return;
            }

            structure.IncludeTracks = true;
            int part2Offset = structure.InfoChunkOffset + 8 + structure.InfoChunk2Offset;
            reader.BaseStream.Position = part2Offset;

            int numTracks = reader.ReadInt32();

            int[] trackOffsets = new int[numTracks];
            for (int i = 0; i < numTracks; i++)
            {
                reader.Expect((short)0x4101);
                reader.BaseStream.Position += 2;
                trackOffsets[i] = reader.ReadInt32();
            }

            foreach (int offset in trackOffsets)
            {
                reader.BaseStream.Position = part2Offset + offset;

                var track = new AdpcmTrack();
                track.Volume = reader.ReadByte();
                track.Panning = reader.ReadByte();
                reader.BaseStream.Position += 2;

                reader.BaseStream.Position += 8;
                track.NumChannels = reader.ReadInt32();
                track.ChannelLeft = reader.ReadByte();
                track.ChannelRight = reader.ReadByte();
                structure.Tracks.Add(track);
            }
        }

        private static void ParseInfoChunk3(BinaryReader reader, BCFstmStructure structure)
        {
            int part3Offset = structure.InfoChunkOffset + 8 + structure.InfoChunk3Offset;
            reader.BaseStream.Position = part3Offset;

            reader.Expect(structure.NumChannels);

            for (int i = 0; i < structure.NumChannels; i++)
            {
                var channel = new B_stmChannelInfo();
                reader.Expect((short)0x4102);
                reader.BaseStream.Position += 2;
                channel.Offset = reader.ReadInt32();
                structure.Channels.Add(channel);
            }

            foreach (B_stmChannelInfo channel in structure.Channels)
            {
                int channelInfoOffset = part3Offset + channel.Offset;
                reader.BaseStream.Position = channelInfoOffset;
                reader.Expect((short)0x0300);
                reader.BaseStream.Position += 2;
                int coefsOffset = reader.ReadInt32() + channelInfoOffset;
                reader.BaseStream.Position = coefsOffset;

                channel.Coefs = Enumerable.Range(0, 16).Select(x => reader.ReadInt16()).ToArray();
                channel.PredScale = reader.ReadInt16();
                channel.Hist1 = reader.ReadInt16();
                channel.Hist2 = reader.ReadInt16();
                channel.LoopPredScale = reader.ReadInt16();
                channel.LoopHist1 = reader.ReadInt16();
                channel.LoopHist2 = reader.ReadInt16();
                channel.Gain = reader.ReadInt16();
            }
        }

        private static void ParseSeekChunk(BinaryReader reader, BCFstmStructure structure)
        {
            reader.BaseStream.Position = structure.SeekChunkOffset;

            if (Encoding.UTF8.GetString(reader.ReadBytes(4), 0, 4) != "SEEK")
            {
                throw new InvalidDataException("Unknown or invalid SEEK chunk");
            }
            structure.SeekChunkSize = reader.ReadInt32();

            if (structure.SeekChunkSizeHeader != structure.SeekChunkSize)
            {
                throw new InvalidDataException("SEEK chunk size in header doesn't match size in SEEK header");
            }

            int bytesPerEntry = 4 * structure.NumChannels;
            int numSeekTableEntries = structure.NumSamples.DivideByRoundUp(structure.SamplesPerSeekTableEntry);

            structure.SeekTableSize = bytesPerEntry * numSeekTableEntries;

            byte[] tableBytes = reader.ReadBytes(structure.SeekTableSize);

            structure.SeekTable = tableBytes.ToShortArray()
                .DeInterleave(2, structure.NumChannels);
        }

        private static void ParseDataChunk(BinaryReader reader, BCFstmStructure structure, bool readAudioData)
        {
            reader.BaseStream.Position = structure.DataChunkOffset;

            if (Encoding.UTF8.GetString(reader.ReadBytes(4), 0, 4) != "DATA")
            {
                throw new InvalidDataException("Unknown or invalid DATA chunk");
            }
            structure.DataChunkSize = reader.ReadInt32();

            if (structure.DataChunkSizeHeader != structure.DataChunkSize)
            {
                throw new InvalidDataException("DATA chunk size in header doesn't match size in DATA header");
            }

            if (!readAudioData) return;

            reader.BaseStream.Position = structure.AudioDataOffset;
            int audioDataLength = structure.DataChunkSize - (structure.AudioDataOffset - structure.DataChunkOffset);

            structure.AudioData = reader.BaseStream.DeInterleave(audioDataLength, structure.InterleaveSize,
                structure.NumChannels);
        }

        internal enum BCFstmType
        {
            Bcstm,
            Bfstm
        }
    }

    internal class BCFstmConfiguration : B_stmConfiguration
    {
        /// <summary>
        /// If <c>true</c>, include track information in the BCSTM
        /// header. Default is <c>true</c>.
        /// </summary>
        public bool IncludeTrackInformation { get; set; } = true;
        /// <summary>
        /// If <c>true</c>, include an extra chunk in the header
        /// after the stream info and before the track offset table.
        /// The purpose of this chunk is unknown.
        /// Default is <c>false</c>.
        /// </summary>
        public bool InfoPart1Extra { get; set; }
        public bool IncludeUnalignedLoopPoints { get; set; }
    }
}
