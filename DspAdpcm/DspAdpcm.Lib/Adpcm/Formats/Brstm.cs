using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static DspAdpcm.Lib.Helpers;

namespace DspAdpcm.Lib.Adpcm.Formats
{
    /// <summary>
    /// Represents a BRSTM file.
    /// </summary>
    public class Brstm
    {
        /// <summary>
        /// The underlying <see cref="AdpcmStream"/> used to build the BRSTM file.
        /// </summary>
        public AdpcmStream AudioStream { get; set; }

        /// <summary>
        /// Contains various settings used when building the BRSTM file.
        /// </summary>
        public BrstmConfiguration Configuration { get; } = new BrstmConfiguration();

        private int NumSamples => AudioStream.Looping ? LoopEnd : AudioStream.NumSamples;
        private int NumChannels => AudioStream.Channels.Count;
        private int NumTracks => AudioStream.Tracks.Count;

        private int AlignmentSamples => GetNextMultiple(AudioStream.LoopStart, Configuration.LoopPointAlignment) - AudioStream.LoopStart;
        private int LoopStart => AudioStream.LoopStart + AlignmentSamples;
        private int LoopEnd => AudioStream.LoopEnd + AlignmentSamples;

        private BrstmCodec Codec { get; } = BrstmCodec.Adpcm;
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
        private int NumSeekTableEntries => Configuration.SeekTableType == BrstmSeekTableType.Standard
            ? NumSamples.DivideByRoundUp(SamplesPerSeekTableEntry)
            : (GetBytesForAdpcmSamples(NumSamples) / SamplesPerSeekTableEntry) + 1;

        private int RstmHeaderLength => 0x40;

        private int HeadChunkOffset => RstmHeaderLength;
        private int HeadChunkLength => GetNextMultiple(HeadChunkHeaderLength + HeadChunkTableLength +
            HeadChunk1Length + HeadChunk2Length + HeadChunk3Length, 0x20);
        private int HeadChunkHeaderLength => 8;
        private int HeadChunkTableLength => 8 * 3;
        private int HeadChunk1Length => 0x34;
        private int HeadChunk2Length => 4 + (8 * NumTracks) + (TrackInfoLength * NumTracks);
        private BrstmTrackType HeaderType => Configuration.TrackType;
        private int TrackInfoLength => HeaderType == BrstmTrackType.Short ? 4 : 0x0c;
        private int HeadChunk3Length => 4 + (8 * NumChannels) + (ChannelInfoLength * NumChannels);
        private int ChannelInfoLength => 0x38;

        private int AdpcChunkOffset => RstmHeaderLength + HeadChunkLength;
        private int AdpcChunkLength => GetNextMultiple(8 + NumSeekTableEntries * NumChannels * BytesPerSeekTableEntry, 0x20);

        private int DataChunkOffset => RstmHeaderLength + HeadChunkLength + AdpcChunkLength;
        private int DataChunkLength => 0x20 + GetNextMultiple(GetBytesForAdpcmSamples(NumSamples), 0x20) * NumChannels;

        /// <summary>
        /// The size in bytes of the BRSTM file.
        /// </summary>
        public int FileLength => RstmHeaderLength + HeadChunkLength + AdpcChunkLength + DataChunkLength;

        /// <summary>
        /// Initializes a new <see cref="Brstm"/> from an <see cref="AdpcmStream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="AdpcmStream"/> used to
        /// create the <see cref="Brstm"/>.</param>
        public Brstm(AdpcmStream stream)
        {
            if (stream.Channels.Count < 1)
            {
                throw new InvalidDataException("Stream must have at least one channel ");
            }

            AudioStream = stream;
        }

        /// <summary>
        /// Initializes a new <see cref="Brstm"/> by parsing an existing
        /// BRSTM file.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> containing 
        /// the BRSTM file. Must be seekable.</param>
        public Brstm(Stream stream)
        {
            if (!stream.CanSeek)
            {
                throw new NotSupportedException("A seekable stream is required");
            }

            ReadBrstmFile(stream);
        }

        /// <summary>
        /// Initializes a new <see cref="Brstm"/> from an <see cref="AdpcmStream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="AdpcmStream"/> used to
        /// create the <see cref="Brstm"/>.</param>
        /// <param name="configuration">A <see cref="BrstmConfiguration"/>
        /// to use for the <see cref="Brstm"/></param>
        public Brstm(AdpcmStream stream, BrstmConfiguration configuration) : this(stream)
        {
            Configuration = configuration;
        }

        /// <summary>
        /// Initializes a new <see cref="Brstm"/> by parsing an existing
        /// BRSTM file.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> containing 
        /// the BRSTM file. Must be seekable.</param>
        /// <param name="configuration">A <see cref="BrstmConfiguration"/>
        /// to use for the <see cref="Brstm"/></param>
        public Brstm(Stream stream, BrstmConfiguration configuration) : this(stream)
        {
            Configuration = configuration;
        }

        private Brstm() { }

        /// <summary>
        /// Parses the header of a BRSTM file and returns the metadata
        /// and structure data of that file.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> containing 
        /// the BRSTM file. Must be seekable.</param>
        /// <returns>A <see cref="BrstmStructure"/> containing
        /// the data from the BRSTM header.</returns>
        public static BrstmStructure ReadMetadata(Stream stream)
        {
            if (!stream.CanSeek)
            {
                throw new NotSupportedException("A seekable stream is required");
            }

            return new Brstm().ReadBrstmFile(stream, false);
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
            RecalculateData();

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

            var writer = new BinaryWriterBE(stream);

            stream.Position = 0;
            GetRstmHeader(writer);
            stream.Position = HeadChunkOffset;
            GetHeadChunk(writer);
            stream.Position = AdpcChunkOffset;
            GetAdpcChunk(writer);
            stream.Position = DataChunkOffset;
            GetDataChunk(writer);
        }

        private void GetRstmHeader(BinaryWriterBE writer)
        {
            writer.WriteASCII("RSTM");
            writer.WriteBE((ushort)0xfeff); //Endianness
            writer.WriteBE((short)0x0100); //BRSTM format version
            writer.WriteBE(FileLength);
            writer.WriteBE((short)RstmHeaderLength);
            writer.WriteBE((short)2); // NumEntries
            writer.WriteBE(HeadChunkOffset);
            writer.WriteBE(HeadChunkLength);
            writer.WriteBE(AdpcChunkOffset);
            writer.WriteBE(AdpcChunkLength);
            writer.WriteBE(DataChunkOffset);
            writer.WriteBE(DataChunkLength);
        }

        private void GetHeadChunk(BinaryWriterBE writer)
        {
            writer.WriteASCII("HEAD");
            writer.WriteBE(HeadChunkLength);

            writer.WriteBE(0x01000000);
            writer.WriteBE(HeadChunkTableLength); //Chunk 1 offset
            writer.WriteBE(0x01000000);
            writer.WriteBE(HeadChunkTableLength + HeadChunk1Length); //Chunk 2 offset
            writer.WriteBE(0x01000000);
            writer.WriteBE(HeadChunkTableLength + HeadChunk1Length + HeadChunk2Length); //Chunk 3 offset

            GetHeadChunk1(writer);
            GetHeadChunk2(writer);
            GetHeadChunk3(writer);
        }

        private void GetHeadChunk1(BinaryWriterBE writer)
        {
            writer.Write((byte)Codec);
            writer.Write(Looping);
            writer.Write((byte)NumChannels);
            writer.Write((byte)0); //padding
            writer.WriteBE((ushort)AudioStream.SampleRate);
            writer.WriteBE((short)0);//padding
            writer.WriteBE(LoopStart);
            writer.WriteBE(NumSamples);
            writer.WriteBE(AudioDataOffset);
            writer.WriteBE(InterleaveCount);
            writer.WriteBE(InterleaveSize);
            writer.WriteBE(SamplesPerInterleave);
            writer.WriteBE(LastBlockSizeWithoutPadding);
            writer.WriteBE(LastBlockSamples);
            writer.WriteBE(LastBlockSize);
            writer.WriteBE(SamplesPerSeekTableEntry);
            writer.WriteBE(BytesPerSeekTableEntry);
        }

        private void GetHeadChunk2(BinaryWriterBE writer)
        {
            writer.Write((byte)NumTracks);
            writer.Write((byte)(HeaderType == BrstmTrackType.Short ? 0 : 1));
            writer.WriteBE((short)0);

            int baseOffset = HeadChunkTableLength + HeadChunk1Length + 4;
            int offsetTableLength = NumTracks * 8;

            for (int i = 0; i < NumTracks; i++)
            {
                writer.WriteBE(HeaderType == BrstmTrackType.Short ? 0x01000000 : 0x01010000);
                writer.WriteBE(baseOffset + offsetTableLength + TrackInfoLength * i);
            }

            foreach (AdpcmTrack track in AudioStream.Tracks)
            {
                if (HeaderType == BrstmTrackType.Standard)
                {
                    writer.Write((byte)track.Volume);
                    writer.Write((byte)track.Panning);
                    writer.WriteBE((short)0);
                    writer.WriteBE(0);
                }
                writer.Write((byte)track.NumChannels);
                writer.Write((byte)track.ChannelLeft); //First channel ID
                writer.Write((byte)track.ChannelRight); //Second channel ID
                writer.Write((byte)0);
            }
        }

        private void GetHeadChunk3(BinaryWriterBE writer)
        {
            writer.Write((byte)NumChannels);
            writer.Write((byte)0); //padding
            writer.WriteBE((short)0); //padding

            int baseOffset = HeadChunkTableLength + HeadChunk1Length + HeadChunk2Length + 4;
            int offsetTableLength = NumChannels * 8;

            for (int i = 0; i < NumChannels; i++)
            {
                writer.WriteBE(0x01000000);
                writer.WriteBE(baseOffset + offsetTableLength + ChannelInfoLength * i);
            }

            for (int i = 0; i < NumChannels; i++)
            {
                AdpcmChannel channel = AudioStream.Channels[i];
                writer.WriteBE(0x01000000);
                writer.WriteBE(baseOffset + offsetTableLength + ChannelInfoLength * i + 8);
                writer.Write(channel.Coefs.ToFlippedBytes());
                writer.WriteBE(channel.Gain);
                writer.WriteBE(channel.GetAudioData[0]);
                writer.WriteBE(channel.Hist1);
                writer.WriteBE(channel.Hist2);
                writer.WriteBE(AudioStream.Looping ? channel.LoopPredScale : channel.GetAudioData[0]);
                writer.WriteBE(AudioStream.Looping ? channel.LoopHist1 : (short)0);
                writer.WriteBE(AudioStream.Looping ? channel.LoopHist2 : (short)0);
                writer.WriteBE((short)0);
            }
        }

        private void GetAdpcChunk(BinaryWriterBE writer)
        {
            writer.WriteASCII("ADPC");
            writer.WriteBE(AdpcChunkLength);

            var table = Decode.BuildSeekTable(AudioStream.Channels, SamplesPerSeekTableEntry, NumSeekTableEntries);

            writer.Write(table);
        }

        private void GetDataChunk(BinaryWriterBE writer)
        {
            writer.WriteASCII("DATA");
            writer.WriteBE(DataChunkLength);
            writer.WriteBE(0x18);

            writer.BaseStream.Position = AudioDataOffset;

            byte[][] channels = AudioStream.Channels.Select(x => x.GetAudioData).ToArray();

            channels.Interleave(writer.BaseStream, GetBytesForAdpcmSamples(NumSamples), InterleaveSize, 0x20);
        }

        private BrstmStructure ReadBrstmFile(Stream stream, bool readAudioData = true)
        {
            var reader = new BinaryReaderBE(stream);
            if (Encoding.UTF8.GetString(reader.ReadBytes(4), 0, 4) != "RSTM")
            {
                throw new InvalidDataException("File has no RSTM header");
            }

            var structure = new BrstmStructure();

            reader.Expect((ushort)0xfeff);
            structure.Version = reader.ReadInt16BE();
            structure.FileLength = reader.ReadInt32BE();

            if (stream.Length < structure.FileLength)
            {
                throw new InvalidDataException("Actual file length is less than stated length");
            }

            structure.RstmHeaderLength = reader.ReadInt16BE();
            structure.RstmHeaderSections = reader.ReadInt16BE();

            structure.HeadChunkOffset = reader.ReadInt32BE();
            structure.HeadChunkLengthRstm = reader.ReadInt32BE();
            structure.AdpcChunkOffset = reader.ReadInt32BE();
            structure.AdpcChunkLengthRstm = reader.ReadInt32BE();
            structure.DataChunkOffset = reader.ReadInt32BE();
            structure.DataChunkLengthRstm = reader.ReadInt32BE();

            reader.BaseStream.Position = structure.HeadChunkOffset;
            ParseHeadChunk(reader, structure);

            Configuration.SamplesPerInterleave = structure.SamplesPerInterleave;
            Configuration.SamplesPerSeekTableEntry = structure.SamplesPerSeekTableEntry;
            Configuration.TrackType = structure.HeaderType;

            AudioStream = new AdpcmStream(structure.NumSamples, structure.SampleRate);
            if (structure.Looping)
            {
                AudioStream.SetLoop(structure.LoopStart, structure.NumSamples);
            }
            AudioStream.Tracks = structure.Tracks;

            ParseAdpcChunk(reader, structure);

            if (!readAudioData)
            {
                reader.BaseStream.Position = structure.DataChunkOffset + 4;
                structure.DataChunkLength = reader.ReadInt32BE();
                return structure;
            }

            ParseDataChunk(reader, structure);

            Configuration.SeekTableType = structure.SeekTableType;

            return structure;
        }

        private static void ParseHeadChunk(BinaryReaderBE reader, BrstmStructure structure)
        {
            reader.BaseStream.Position = structure.HeadChunkOffset;
            int baseOffset = structure.HeadChunkOffset + 8;

            if (Encoding.UTF8.GetString(reader.ReadBytes(4), 0, 4) != "HEAD")
            {
                throw new InvalidDataException("Unknown or invalid HEAD chunk");
            }

            structure.HeadChunkLength = reader.ReadInt32BE();
            if (structure.HeadChunkLength != structure.HeadChunkLengthRstm)
            {
                throw new InvalidDataException("HEAD chunk length in RSTM header doesn't match length in HEAD header");
            }

            reader.Expect(0x01000000);
            structure.HeadChunk1Offset = reader.ReadInt32BE();
            reader.Expect(0x01000000);
            structure.HeadChunk2Offset = reader.ReadInt32BE();
            reader.Expect(0x01000000);
            structure.HeadChunk3Offset = reader.ReadInt32BE();

            ParseHeadChunk1(reader, structure);
            ParseHeadChunk2(reader, structure);
            ParseHeadChunk3(reader, structure);
        }

        private static void ParseHeadChunk1(BinaryReaderBE reader, BrstmStructure structure)
        {
            reader.BaseStream.Position = structure.HeadChunkOffset + 8 + structure.HeadChunk1Offset;
            structure.Codec = (BrstmCodec)reader.ReadByte();
            if (structure.Codec != BrstmCodec.Adpcm)
            {
                throw new InvalidDataException("File must contain 4-bit ADPCM encoded audio");
            }

            structure.Looping = reader.ReadByte() == 1;
            structure.NumChannels = reader.ReadByte();
            reader.BaseStream.Position += 1;

            structure.SampleRate = reader.ReadUInt16BE();
            reader.BaseStream.Position += 2;

            structure.LoopStart = reader.ReadInt32BE();
            structure.NumSamples = reader.ReadInt32BE();

            structure.AudioDataOffset = reader.ReadInt32BE();
            structure.InterleaveCount = reader.ReadInt32BE();
            structure.InterleaveSize = reader.ReadInt32BE();
            structure.SamplesPerInterleave = reader.ReadInt32BE();
            structure.LastBlockSizeWithoutPadding = reader.ReadInt32BE();
            structure.LastBlockSamples = reader.ReadInt32BE();
            structure.LastBlockSize = reader.ReadInt32BE();
            structure.SamplesPerSeekTableEntry = reader.ReadInt32BE();
        }

        private static void ParseHeadChunk2(BinaryReaderBE reader, BrstmStructure structure)
        {
            int baseOffset = structure.HeadChunkOffset + 8;
            reader.BaseStream.Position = baseOffset + structure.HeadChunk2Offset;

            int numTracks = reader.ReadByte();
            int[] trackOffsets = new int[numTracks];

            structure.HeaderType = reader.ReadByte() == 0 ? BrstmTrackType.Short : BrstmTrackType.Standard;
            int marker = structure.HeaderType == BrstmTrackType.Short ? 0x01000000 : 0x01010000;

            reader.BaseStream.Position += 2;
            for (int i = 0; i < numTracks; i++)
            {
                reader.Expect(marker);
                trackOffsets[i] = reader.ReadInt32BE();
            }

            foreach (int offset in trackOffsets)
            {
                reader.BaseStream.Position = baseOffset + offset;
                var track = new AdpcmTrack();

                if (structure.HeaderType == BrstmTrackType.Standard)
                {
                    track.Volume = reader.ReadByte();
                    track.Panning = reader.ReadByte();
                    reader.BaseStream.Position += 6;
                }

                track.NumChannels = reader.ReadByte();
                track.ChannelLeft = reader.ReadByte();
                track.ChannelRight = reader.ReadByte();

                structure.Tracks.Add(track);
            }
        }

        private static void ParseHeadChunk3(BinaryReaderBE reader, BrstmStructure structure)
        {
            int baseOffset = structure.HeadChunkOffset + 8;
            reader.BaseStream.Position = baseOffset + structure.HeadChunk3Offset;

            reader.Expect((byte)structure.NumChannels);
            reader.BaseStream.Position += 3;

            for (int i = 0; i < structure.NumChannels; i++)
            {
                var channel = new B_stmChannelInfo();
                reader.Expect(0x01000000);
                channel.Offset = reader.ReadInt32BE();
                structure.Channels.Add(channel);
            }

            foreach (B_stmChannelInfo channel in structure.Channels)
            {
                reader.BaseStream.Position = baseOffset + channel.Offset;
                reader.Expect(0x01000000);
                int coefsOffset = reader.ReadInt32BE();
                reader.BaseStream.Position = baseOffset + coefsOffset;

                channel.Coefs = Enumerable.Range(0, 16).Select(x => reader.ReadInt16BE()).ToArray();
                channel.Gain = reader.ReadInt16BE();
                channel.PredScale = reader.ReadInt16BE();
                channel.Hist1 = reader.ReadInt16BE();
                channel.Hist2 = reader.ReadInt16BE();
                channel.LoopPredScale = reader.ReadInt16BE();
                channel.LoopHist1 = reader.ReadInt16BE();
                channel.LoopHist2 = reader.ReadInt16BE();
            }
        }

        private static void ParseAdpcChunk(BinaryReaderBE reader, BrstmStructure structure)
        {
            reader.BaseStream.Position = structure.AdpcChunkOffset;

            if (Encoding.UTF8.GetString(reader.ReadBytes(4), 0, 4) != "ADPC")
            {
                throw new InvalidDataException("Unknown or invalid ADPC chunk");
            }
            structure.AdpcChunkLength = reader.ReadInt32BE();

            if (structure.AdpcChunkLengthRstm != structure.AdpcChunkLength)
            {
                throw new InvalidDataException("ADPC chunk length in RSTM header doesn't match length in ADPC header");
            }

            bool fullLastSeekTableEntry = structure.NumSamples % structure.SamplesPerSeekTableEntry == 0 && structure.NumSamples > 0;
            int bytesPerEntry = 4 * structure.NumChannels;
            int numSeekTableEntriesShortened = (GetBytesForAdpcmSamples(structure.NumSamples) / structure.SamplesPerSeekTableEntry) + 1;
            int numSeekTableEntriesStandard = (structure.NumSamples / structure.SamplesPerSeekTableEntry) + (fullLastSeekTableEntry ? 0 : 1);
            int expectedLengthShortened = GetNextMultiple(8 + numSeekTableEntriesShortened * bytesPerEntry, 0x20);
            int expectedLengthStandard = GetNextMultiple(8 + numSeekTableEntriesStandard * bytesPerEntry, 0x20);

            if (structure.AdpcChunkLength == expectedLengthStandard)
            {
                structure.SeekTableLength = bytesPerEntry * numSeekTableEntriesStandard;
                structure.SeekTableType = BrstmSeekTableType.Standard;
            }
            else if (structure.AdpcChunkLength == expectedLengthShortened)
            {
                structure.SeekTableLength = bytesPerEntry * numSeekTableEntriesShortened;
                structure.SeekTableType = BrstmSeekTableType.Short;
            }
            else
            {
                return; //Unknown format. Don't parse table
            }

            byte[] tableBytes = reader.ReadBytes(structure.SeekTableLength);

            structure.SeekTable = tableBytes.ToShortArrayFlippedBytes()
                .DeInterleave(2, structure.NumChannels);
        }

        private void ParseDataChunk(BinaryReaderBE reader, BrstmStructure structure)
        {
            reader.BaseStream.Position = structure.DataChunkOffset;

            if (Encoding.UTF8.GetString(reader.ReadBytes(4), 0, 4) != "DATA")
            {
                throw new InvalidDataException("Unknown or invalid DATA chunk");
            }
            structure.DataChunkLength = reader.ReadInt32BE();

            if (structure.DataChunkLengthRstm != structure.DataChunkLength)
            {
                throw new InvalidDataException("DATA chunk length in RSTM header doesn't match length in DATA header");
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
        /// Contains the options used to build the BRSTM file.
        /// </summary>
        public class BrstmConfiguration : B_stmConfiguration
        {
            /// <summary>
            /// The type of track description to be used when building the 
            /// BRSTM header.
            /// Default is <see cref="BrstmTrackType.Short"/>
            /// </summary>
            public BrstmTrackType TrackType { get; set; } = BrstmTrackType.Short;

            /// <summary>
            /// The type of seek table to use when building the BRSTM
            /// ADPC chunk.
            /// Default is <see cref="BrstmSeekTableType.Standard"/>
            /// </summary>
            public BrstmSeekTableType SeekTableType { get; set; } = BrstmSeekTableType.Standard;
        }
    }
}
