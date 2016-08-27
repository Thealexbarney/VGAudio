using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static DspAdpcm.Lib.Helpers;

namespace DspAdpcm.Lib.Adpcm.Formats
{
    /// <summary>
    /// Represents a BCSTM file.
    /// </summary>
    public class Bcstm
    {
        /// <summary>
        /// The underlying <see cref="AdpcmStream"/> used to build the BCSTM file.
        /// </summary>
        public AdpcmStream AudioStream { get; set; }

        /// <summary>
        /// Contains various settings used when building the BCSTM file.
        /// </summary>
        public BcstmConfiguration Configuration { get; } = new BcstmConfiguration();

        private int NumSamples => AudioStream.Looping ? LoopEnd : AudioStream.NumSamples;
        private int NumChannels => AudioStream.Channels.Count;
        private int NumTracks => AudioStream.Tracks.Count;

        private int AlignmentSamples => GetNextMultiple(AudioStream.LoopStart, Configuration.LoopPointAlignment) - AudioStream.LoopStart;
        private int LoopStart => AudioStream.LoopStart + AlignmentSamples;
        private int LoopEnd => AudioStream.LoopEnd + AlignmentSamples;

        private BcstmCodec Codec { get; } = BcstmCodec.Adpcm;
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

        private int CstmHeaderLength => 0x40;

        private int HeadChunkOffset => CstmHeaderLength;

        private int InfoChunkOffset => CstmHeaderLength;
        private int InfoChunkLength => GetNextMultiple(InfoChunkHeaderLength + InfoChunkTableLength +
            InfoChunk1Length + InfoChunk2Length + InfoChunk3Length, 0x20);
        private int InfoChunkHeaderLength => 8;
        private int InfoChunkTableLength => 8 * 3;
        private int InfoChunk1Length => 0x38 + (!Configuration.InfoPart1Extra ? 0 : 0xc);
        private int InfoChunk2Length => Configuration.IncludeTrackInformation ? 4 + 8 * NumTracks : 0;
        private int InfoChunk3Length => (4 + 8 * NumChannels) +
            (Configuration.IncludeTrackInformation ? 0x14 * NumTracks : 0) +
            8 * NumChannels +
            ChannelInfoLength * NumChannels;

        private int ChannelInfoLength => 0x2e;

        private int SeekChunkOffset => CstmHeaderLength + InfoChunkLength;
        private int SeekChunkLength => GetNextMultiple(8 + NumSeekTableEntries * NumChannels * BytesPerSeekTableEntry, 0x20);

        private int DataChunkOffset => CstmHeaderLength + InfoChunkLength + SeekChunkLength;
        private int DataChunkLength => 0x20 + GetNextMultiple(GetBytesForAdpcmSamples(NumSamples), 0x20) * NumChannels;

        private int? _version;
        private int Version
        {
            get
            {
                return _version ?? VersionDictionary[new Tuple<bool, bool>(Configuration.IncludeTrackInformation, Configuration.InfoPart1Extra)];
            }
            set { _version = value; }
        }
        //All BCSTM files I've seen follow this format except for Kingdom Hearts 3D
        private Dictionary<Tuple<bool, bool>, int> VersionDictionary { get; } = new Dictionary<Tuple<bool, bool>, int>()
        {
            [new Tuple<bool, bool>(true, false)] = 0x200,
            [new Tuple<bool, bool>(true, true)] = 0x201,
            [new Tuple<bool, bool>(false, true)] = 0x202,
            [new Tuple<bool, bool>(false, false)] = 0x200 //Unused in any BCSTM I've seen. Here for completion
        };

        /// <summary>
        /// The size in bytes of the BCSTM file.
        /// </summary>
        public int FileLength => CstmHeaderLength + InfoChunkLength + SeekChunkLength + DataChunkLength;

        /// <summary>
        /// Initializes a new <see cref="Bcstm"/> from an <see cref="AdpcmStream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="AdpcmStream"/> used to
        /// create the <see cref="Bcstm"/>.</param>
        public Bcstm(AdpcmStream stream)
        {
            if (stream.Channels.Count < 1)
            {
                throw new InvalidDataException("Stream must have at least one channel ");
            }

            AudioStream = stream;
        }

        /// <summary>
        /// Initializes a new <see cref="Bcstm"/> by parsing an existing
        /// BCSTM file.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> containing 
        /// the BCSTM file. Must be seekable.</param>
        public Bcstm(Stream stream)
        {
            if (!stream.CanSeek)
            {
                throw new NotSupportedException("A seekable stream is required");
            }

            ReadBcstmFile(stream);
        }

        /// <summary>
        /// Initializes a new <see cref="Bcstm"/> from an <see cref="AdpcmStream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="AdpcmStream"/> used to
        /// create the <see cref="Bcstm"/>.</param>
        /// <param name="configuration">A <see cref="BcstmConfiguration"/>
        /// to use for the <see cref="Bcstm"/></param>
        public Bcstm(AdpcmStream stream, BcstmConfiguration configuration) : this(stream)
        {
            Configuration = configuration;
        }

        /// <summary>
        /// Initializes a new <see cref="Bcstm"/> by parsing an existing
        /// BRSTM file.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> containing 
        /// the BCSTM file. Must be seekable.</param>
        /// <param name="configuration">A <see cref="BcstmConfiguration"/>
        /// to use for the <see cref="Bcstm"/></param>
        public Bcstm(Stream stream, BcstmConfiguration configuration) : this(stream)
        {
            Configuration = configuration;
        }

        private Bcstm() { }

        /// <summary>
        /// Parses the header of a BCSTM file and returns the metadata
        /// and structure data of that file.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> containing 
        /// the BCSTM file. Must be seekable.</param>
        /// <returns>A <see cref="BcstmStructure"/> containing
        /// the data from the BCSTM header.</returns>
        public static BcstmStructure ReadMetadata(Stream stream)
        {
            if (!stream.CanSeek)
            {
                throw new NotSupportedException("A seekable stream is required");
            }

            return new Bcstm().ReadBcstmFile(stream, false);
        }

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

        /// <summary>
        /// Builds a BRSTM file from the current <see cref="AudioStream"/>.
        /// </summary>
        /// <returns>A BRSTM file</returns>
        public byte[] GetFile()
        {
            var file = new byte[FileLength];
            var stream = new MemoryStream(file);
            WriteFile(stream);
            return file;
        }

        /// <summary>
        /// Writes the BRSTM file to a <see cref="Stream"/>.
        /// The file is written starting at the beginning
        /// of the <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to write the
        /// BRSTM to.</param>
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

            RecalculateData();

            BinaryWriter writer = new BinaryWriter(stream);

            stream.Position = 0;
            GetCstmHeader(writer);
            stream.Position = HeadChunkOffset;
            GetInfoChunk(writer);
            stream.Position = SeekChunkOffset;
            GetSeekChunk(writer);
            stream.Position = DataChunkOffset;
            GetDataChunk(writer);
        }

        private void GetCstmHeader(BinaryWriter writer)
        {
            writer.WriteASCII("CSTM");
            writer.Write((ushort)0xfeff); //Endianness
            writer.Write(CstmHeaderLength);
            writer.Write((short)Version);
            writer.Write(FileLength);

            writer.Write(3); // NumEntries
            writer.Write(0x4000);
            writer.Write(InfoChunkOffset);
            writer.Write(InfoChunkLength);
            writer.Write(0x4001);
            writer.Write(SeekChunkOffset);
            writer.Write(SeekChunkLength);
            writer.Write(0x4002);
            writer.Write(DataChunkOffset);
            writer.Write(DataChunkLength);
        }

        private void GetInfoChunk(BinaryWriter writer)
        {
            writer.WriteASCII("INFO");
            writer.Write(InfoChunkLength);

            int headerTableLength = 8 * 3;

            writer.Write(0x4100);
            writer.Write(headerTableLength);
            if (Configuration.IncludeTrackInformation)
            {
                writer.Write(0x0101);
                writer.Write(headerTableLength + InfoChunk1Length);
            }
            else
            {
                writer.Write(0);
                writer.Write(-1);
            }
            writer.Write(0x0101);
            writer.Write(headerTableLength + InfoChunk1Length + InfoChunk2Length);

            GetInfoChunk1(writer);
            GetInfoChunk2(writer);
            GetInfoChunk3(writer);
        }

        private void GetInfoChunk1(BinaryWriter writer)
        {
            writer.Write((byte)Codec);
            writer.Write(Looping);
            writer.Write((byte)NumChannels);
            writer.Write((byte)0);
            writer.Write((ushort)AudioStream.SampleRate);
            writer.Write((short)0);
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
            writer.Write(0x1f00);
            writer.Write(0x18);

            if (Configuration.InfoPart1Extra)
            {
                writer.Write(0x100);
                writer.Write(0);
                writer.Write(-1);
            }
        }

        private void GetInfoChunk2(BinaryWriter writer)
        {
            if (!Configuration.IncludeTrackInformation) return;

            int trackTableLength = 4 + 8 * NumTracks;
            int channelTableLength = 4 + 8 * NumChannels;
            int trackLength = 0x14;

            writer.Write(NumTracks);

            for (int i = 0; i < NumTracks; i++)
            {
                writer.Write(0x4101);
                writer.Write(trackTableLength + channelTableLength + trackLength * i);
            }
        }

        private void GetInfoChunk3(BinaryWriter writer)
        {
            int channelTableLength = 4 + 8 * NumChannels;
            int trackTableLength = Configuration.IncludeTrackInformation ? 0x14 * NumTracks : 0;

            writer.Write(NumChannels);
            for (int i = 0; i < NumChannels; i++)
            {
                writer.Write(0x4102);
                writer.Write(channelTableLength + trackTableLength + 8 * i);
            }

            if (Configuration.IncludeTrackInformation)
            {
                foreach (var track in AudioStream.Tracks)
                {
                    writer.Write((byte)track.Volume);
                    writer.Write((byte)track.Panning);
                    writer.Write((short)0);
                    writer.Write(0x100);
                    writer.Write(0xc);
                    writer.Write(track.NumChannels);
                    writer.Write((byte)track.ChannelLeft);
                    writer.Write((byte)track.ChannelRight);
                    writer.Write((short)0);
                }
            }

            int channelTable2Length = 8 * NumChannels;
            for (int i = 0; i < NumChannels; i++)
            {
                writer.Write(0x300);
                writer.Write(channelTable2Length - 8 * i + ChannelInfoLength * i);
            }

            foreach (var channel in AudioStream.Channels)
            {
                writer.Write(channel.Coefs.ToByteArray());
                writer.Write((short)channel.GetAudioData[0]);
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
            writer.WriteASCII("SEEK");
            writer.Write(SeekChunkLength);

            var table = Decode.BuildSeekTable(AudioStream.Channels, SamplesPerSeekTableEntry, NumSeekTableEntries, false);

            writer.Write(table);
        }

        private void GetDataChunk(BinaryWriter writer)
        {
            writer.WriteASCII("DATA");
            writer.Write(DataChunkLength);

            writer.BaseStream.Position = AudioDataOffset;

            byte[][] channels = AudioStream.Channels.Select(x => x.GetAudioData).ToArray();

            channels.Interleave(writer.BaseStream, GetBytesForAdpcmSamples(NumSamples), InterleaveSize, 0x20);
        }

        private BcstmStructure ReadBcstmFile(Stream stream, bool readAudioData = true)
        {
            var reader = new BinaryReader(stream);
            if (Encoding.UTF8.GetString(reader.ReadBytes(4), 0, 4) != "CSTM")
            {
                throw new InvalidDataException("File has no CSTM header");
            }

            var structure = new BcstmStructure();

            reader.Expect((ushort)0xfeff);
            structure.CstmHeaderLength = reader.ReadInt32();
            structure.Version = reader.ReadInt16();
            structure.FileLength = reader.ReadInt32();

            if (stream.Length < structure.FileLength)
            {
                throw new InvalidDataException("Actual file length is less than stated length");
            }

            structure.CstmHeaderSections = reader.ReadInt32();

            for (int i = 0; i < structure.CstmHeaderSections; i++)
            {
                int type = reader.ReadInt32();
                switch (type)
                {
                    case 0x4000:
                        structure.InfoChunkOffset = reader.ReadInt32();
                        structure.InfoChunkLengthCstm = reader.ReadInt32();
                        break;
                    case 0x4001:
                        structure.SeekChunkOffset = reader.ReadInt32();
                        structure.SeekChunkLengthCstm = reader.ReadInt32();
                        break;
                    case 0x4002:
                        structure.DataChunkOffset = reader.ReadInt32();
                        structure.DataChunkLengthCstm = reader.ReadInt32();
                        break;
                    default:
                        throw new InvalidDataException($"Unknown section type {type}");
                }
            }

            ParseInfoChunk(reader, structure);
            ParseSeekChunk(reader, structure);

            if (!readAudioData)
            {
                reader.BaseStream.Position = structure.DataChunkOffset + 4;
                structure.DataChunkLength = reader.ReadInt32();
                return structure;
            }

            Configuration.SamplesPerInterleave = structure.SamplesPerInterleave;
            Configuration.SamplesPerSeekTableEntry = structure.SamplesPerSeekTableEntry;

            AudioStream = new AdpcmStream(structure.NumSamples, structure.SampleRate);
            if (structure.Looping)
            {
                AudioStream.SetLoop(structure.LoopStart, structure.NumSamples);
            }
            AudioStream.Tracks = structure.Tracks;
            Configuration.IncludeTrackInformation = structure.IncludeTracks;
            Configuration.InfoPart1Extra = structure.InfoPart1Extra;
            Version = structure.Version;

            ParseDataChunk(reader, structure);

            return structure;
        }

        private static void ParseInfoChunk(BinaryReader reader, BcstmStructure structure)
        {
            reader.BaseStream.Position = structure.InfoChunkOffset;
            if (Encoding.UTF8.GetString(reader.ReadBytes(4), 0, 4) != "INFO")
            {
                throw new InvalidDataException("Unknown or invalid INFO chunk");
            }

            structure.InfoChunkLength = reader.ReadInt32();
            if (structure.InfoChunkLength != structure.InfoChunkLengthCstm)
            {
                throw new InvalidDataException("INFO chunk length in CSTM header doesn't match length in INFO header");
            }

            reader.Expect(0x4100);
            structure.InfoChunk1Offset = reader.ReadInt32();
            reader.Expect(0x0101, 0);
            structure.InfoChunk2Offset = reader.ReadInt32();
            reader.Expect(0x0101);
            structure.InfoChunk3Offset = reader.ReadInt32();

            ParseInfoChunk1(reader, structure);
            ParseInfoChunk2(reader, structure);
            ParseInfoChunk3(reader, structure);
        }

        private static void ParseInfoChunk1(BinaryReader reader, BcstmStructure structure)
        {
            reader.BaseStream.Position = structure.InfoChunkOffset + 8 + structure.InfoChunk1Offset;
            structure.Codec = (BcstmCodec)reader.ReadByte();
            if (structure.Codec != BcstmCodec.Adpcm)
            {
                throw new InvalidDataException("File must contain 4-bit ADPCM encoded audio");
            }

            structure.Looping = reader.ReadByte() == 1;
            structure.NumChannels = reader.ReadByte();
            reader.BaseStream.Position += 1;

            structure.SampleRate = reader.ReadUInt16();
            reader.BaseStream.Position += 2;

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

            reader.Expect(0x1f00);
            structure.AudioDataOffset = reader.ReadInt32() + structure.DataChunkOffset + 8;
            structure.InfoPart1Extra = reader.ReadInt32() == 0x100;
        }

        private static void ParseInfoChunk2(BinaryReader reader, BcstmStructure structure)
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
                reader.Expect(0x4101);
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

        private static void ParseInfoChunk3(BinaryReader reader, BcstmStructure structure)
        {
            int part3Offset = structure.InfoChunkOffset + 8 + structure.InfoChunk3Offset;
            reader.BaseStream.Position = part3Offset;

            reader.Expect((byte)structure.NumChannels);
            reader.BaseStream.Position += 3;

            for (int i = 0; i < structure.NumChannels; i++)
            {
                var channel = new B_stmChannelInfo();
                reader.Expect(0x4102);
                channel.Offset = reader.ReadInt32();
                structure.Channels.Add(channel);
            }

            foreach (B_stmChannelInfo channel in structure.Channels)
            {
                int channelInfoOffset = part3Offset + channel.Offset;
                reader.BaseStream.Position = channelInfoOffset;
                reader.Expect(0x0300);
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

        private static void ParseSeekChunk(BinaryReader reader, BcstmStructure structure)
        {
            reader.BaseStream.Position = structure.SeekChunkOffset;

            if (Encoding.UTF8.GetString(reader.ReadBytes(4), 0, 4) != "SEEK")
            {
                throw new InvalidDataException("Unknown or invalid SEEK chunk");
            }
            structure.SeekChunkLength = reader.ReadInt32();

            if (structure.SeekChunkLengthCstm != structure.SeekChunkLength)
            {
                throw new InvalidDataException("SEEK chunk length in CSTM header doesn't match length in SEEK header");
            }

            int bytesPerEntry = 4 * structure.NumChannels;
            int numSeekTableEntries = structure.NumSamples.DivideByRoundUp(structure.SamplesPerSeekTableEntry);

            structure.SeekTableLength = bytesPerEntry * numSeekTableEntries;

            byte[] tableBytes = reader.ReadBytes(structure.SeekTableLength);

            structure.SeekTable = tableBytes.ToShortArray()
                .DeInterleave(2, structure.NumChannels);
        }

        private void ParseDataChunk(BinaryReader reader, BcstmStructure structure)
        {
            reader.BaseStream.Position = structure.DataChunkOffset;

            if (Encoding.UTF8.GetString(reader.ReadBytes(4), 0, 4) != "DATA")
            {
                throw new InvalidDataException("Unknown or invalid DATA chunk");
            }
            structure.DataChunkLength = reader.ReadInt32();

            if (structure.DataChunkLengthCstm != structure.DataChunkLength)
            {
                throw new InvalidDataException("DATA chunk length in CSTM header doesn't match length in DATA header");
            }

            reader.BaseStream.Position = structure.AudioDataOffset;
            int audioDataLength = structure.DataChunkLength - (structure.AudioDataOffset - structure.DataChunkOffset);

            byte[][] deInterleavedAudioData = reader.BaseStream.DeInterleave(audioDataLength, structure.InterleaveSize,
                structure.NumChannels);

            for (int c = 0; c < structure.NumChannels; c++)
            {
                var channel = new AdpcmChannel(structure.NumSamples, deInterleavedAudioData[c])
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
                AudioStream.Channels.Add(channel);
            }
        }

        /// <summary>
        /// Contains the options used to build the BCSTM file.
        /// </summary>
        public class BcstmConfiguration : B_stmConfiguration
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
        }
    }
}
