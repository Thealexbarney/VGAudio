using System;
using System.IO;
using System.Linq;
using System.Text;
using DspAdpcm.Adpcm.Formats.Configuration;
using DspAdpcm.Adpcm.Formats.Structures;
using static DspAdpcm.Helpers;

namespace DspAdpcm.Adpcm.Formats
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
        public BrstmConfiguration Configuration { get; set; }

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
        private int NumSeekTableEntries => Configuration.SeekTableType == BrstmSeekTableType.Standard
            ? NumSamples.DivideByRoundUp(SamplesPerSeekTableEntry)
            : (GetBytesForAdpcmSamples(NumSamples) / SamplesPerSeekTableEntry) + 1;

        private static int RstmHeaderSize => 0x40;

        private int HeadChunkOffset => RstmHeaderSize;
        private int HeadChunkSize => GetNextMultiple(HeadChunkHeaderSize + HeadChunkTableSize +
            HeadChunk1Size + HeadChunk2Size + HeadChunk3Size, 0x20);
        private int HeadChunkHeaderSize => 8;
        private int HeadChunkTableSize => 8 * 3;
        private int HeadChunk1Size => 0x34;
        private int HeadChunk2Size => 4 + (8 * NumTracks) + (TrackInfoSize * NumTracks);
        private BrstmTrackType HeaderType => Configuration.TrackType;
        private int TrackInfoSize => HeaderType == BrstmTrackType.Short ? 4 : 0x0c;
        private int HeadChunk3Size => 4 + (8 * NumChannels) + (ChannelInfoSize * NumChannels);
        private int ChannelInfoSize => 0x38;

        private int AdpcChunkOffset => RstmHeaderSize + HeadChunkSize;
        private int AdpcChunkSize => GetNextMultiple(8 + NumSeekTableEntries * NumChannels * BytesPerSeekTableEntry, 0x20);

        private int DataChunkOffset => RstmHeaderSize + HeadChunkSize + AdpcChunkSize;
        private int DataChunkSize => 0x20 + GetNextMultiple(GetBytesForAdpcmSamples(NumSamples), 0x20) * NumChannels;

        /// <summary>
        /// The size in bytes of the BRSTM file.
        /// </summary>
        public int FileSize => RstmHeaderSize + HeadChunkSize + AdpcChunkSize + DataChunkSize;

        /// <summary>
        /// Initializes a new <see cref="Brstm"/> from an <see cref="AdpcmStream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="AdpcmStream"/> used to
        /// create the <see cref="Brstm"/>.</param>
        /// <param name="configuration">A <see cref="BrstmConfiguration"/>
        /// to use for the <see cref="Brstm"/></param>
        public Brstm(AdpcmStream stream, BrstmConfiguration configuration = null)
        {
            if (stream.Channels.Count < 1)
            {
                throw new InvalidDataException("Stream must have at least one channel ");
            }

            AudioStream = stream;
            Configuration = configuration ?? new BrstmConfiguration();
        }

        /// <summary>
        /// Initializes a new <see cref="Brstm"/> by parsing an existing
        /// BRSTM file.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> containing 
        /// the BRSTM file. Must be seekable.</param>
        /// <param name="configuration">A <see cref="BrstmConfiguration"/>
        /// to use for the <see cref="Brstm"/></param>
        public Brstm(Stream stream, BrstmConfiguration configuration = null)
        {
            ReadStream(stream, configuration);
        }

        /// <summary>
        /// Initializes a new <see cref="Brstm"/> by parsing an existing
        /// BRSTM file.
        /// </summary>
        /// <param name="file">A <c>byte[]</c> containing 
        /// the BRSTM file.</param>
        /// <param name="configuration">A <see cref="BrstmConfiguration"/>
        /// to use for the <see cref="Brstm"/></param>
        public Brstm(byte[] file, BrstmConfiguration configuration = null)
        {
            using (var stream = new MemoryStream(file))
            {
                ReadStream(stream, configuration);
            }
        }

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
            CheckStream(stream, RstmHeaderSize);
            return ReadBrstmFile(stream, false);
        }

        private void ReadStream(Stream stream, BrstmConfiguration configuration = null)
        {
            CheckStream(stream, RstmHeaderSize);

            BrstmStructure brstm = ReadBrstmFile(stream);
            AudioStream = GetAdpcmStream(brstm);
            Configuration = configuration ?? GetConfiguration(brstm);
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
            var file = new byte[FileSize];
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

            RecalculateData();

            using (BinaryWriter writer = GetBinaryWriterBE(stream))
            {
                stream.Position = 0;
                GetRstmHeader(writer);
                stream.Position = HeadChunkOffset;
                GetHeadChunk(writer);
                stream.Position = AdpcChunkOffset;
                GetAdpcChunk(writer);
                stream.Position = DataChunkOffset;
                GetDataChunk(writer);
            }
        }

        private void GetRstmHeader(BinaryWriter writer)
        {
            writer.WriteUTF8("RSTM");
            writer.Write((ushort)0xfeff); //Endianness
            writer.Write((short)0x0100); //BRSTM format version
            writer.Write(FileSize);
            writer.Write((short)RstmHeaderSize);
            writer.Write((short)2); // NumEntries
            writer.Write(HeadChunkOffset);
            writer.Write(HeadChunkSize);
            writer.Write(AdpcChunkOffset);
            writer.Write(AdpcChunkSize);
            writer.Write(DataChunkOffset);
            writer.Write(DataChunkSize);
        }

        private void GetHeadChunk(BinaryWriter writer)
        {
            writer.WriteUTF8("HEAD");
            writer.Write(HeadChunkSize);

            writer.Write(0x01000000);
            writer.Write(HeadChunkTableSize); //Chunk 1 offset
            writer.Write(0x01000000);
            writer.Write(HeadChunkTableSize + HeadChunk1Size); //Chunk 2 offset
            writer.Write(0x01000000);
            writer.Write(HeadChunkTableSize + HeadChunk1Size + HeadChunk2Size); //Chunk 3 offset

            GetHeadChunk1(writer);
            GetHeadChunk2(writer);
            GetHeadChunk3(writer);
        }

        private void GetHeadChunk1(BinaryWriter writer)
        {
            writer.Write((byte)Codec);
            writer.Write(Looping);
            writer.Write((byte)NumChannels);
            writer.Write((byte)0); //padding
            writer.Write((ushort)AudioStream.SampleRate);
            writer.Write((short)0);//padding
            writer.Write(LoopStart);
            writer.Write(NumSamples);
            writer.Write(AudioDataOffset);
            writer.Write(InterleaveCount);
            writer.Write(InterleaveSize);
            writer.Write(SamplesPerInterleave);
            writer.Write(LastBlockSizeWithoutPadding);
            writer.Write(LastBlockSamples);
            writer.Write(LastBlockSize);
            writer.Write(SamplesPerSeekTableEntry);
            writer.Write(BytesPerSeekTableEntry);
        }

        private void GetHeadChunk2(BinaryWriter writer)
        {
            writer.Write((byte)NumTracks);
            writer.Write((byte)(HeaderType == BrstmTrackType.Short ? 0 : 1));
            writer.Write((short)0);

            int baseOffset = HeadChunkTableSize + HeadChunk1Size + 4;
            int offsetTableSize = NumTracks * 8;

            for (int i = 0; i < NumTracks; i++)
            {
                writer.Write(HeaderType == BrstmTrackType.Short ? 0x01000000 : 0x01010000);
                writer.Write(baseOffset + offsetTableSize + TrackInfoSize * i);
            }

            foreach (AdpcmTrack track in AudioStream.Tracks)
            {
                if (HeaderType == BrstmTrackType.Standard)
                {
                    writer.Write((byte)track.Volume);
                    writer.Write((byte)track.Panning);
                    writer.Write((short)0);
                    writer.Write(0);
                }
                writer.Write((byte)track.NumChannels);
                writer.Write((byte)track.ChannelLeft); //First channel ID
                writer.Write((byte)track.ChannelRight); //Second channel ID
                writer.Write((byte)0);
            }
        }

        private void GetHeadChunk3(BinaryWriter writer)
        {
            writer.Write((byte)NumChannels);
            writer.Write((byte)0); //padding
            writer.Write((short)0); //padding

            int baseOffset = HeadChunkTableSize + HeadChunk1Size + HeadChunk2Size + 4;
            int offsetTableSize = NumChannels * 8;

            for (int i = 0; i < NumChannels; i++)
            {
                writer.Write(0x01000000);
                writer.Write(baseOffset + offsetTableSize + ChannelInfoSize * i);
            }

            for (int i = 0; i < NumChannels; i++)
            {
                AdpcmChannel channel = AudioStream.Channels[i];
                writer.Write(0x01000000);
                writer.Write(baseOffset + offsetTableSize + ChannelInfoSize * i + 8);
                writer.Write(channel.Coefs.ToByteArray(Endianness.BigEndian));
                writer.Write(channel.Gain);
                writer.Write(channel.PredScale);
                writer.Write(channel.Hist1);
                writer.Write(channel.Hist2);
                writer.Write(AudioStream.Looping ? channel.LoopPredScale : channel.PredScale);
                writer.Write(AudioStream.Looping ? channel.LoopHist1 : (short)0);
                writer.Write(AudioStream.Looping ? channel.LoopHist2 : (short)0);
                writer.Write((short)0);
            }
        }

        private void GetAdpcChunk(BinaryWriter writer)
        {
            writer.WriteUTF8("ADPC");
            writer.Write(AdpcChunkSize);

            var table = Decode.BuildSeekTable(AudioStream.Channels, SamplesPerSeekTableEntry, NumSeekTableEntries, Endianness.BigEndian);

            writer.Write(table);
        }

        private void GetDataChunk(BinaryWriter writer)
        {
            writer.WriteUTF8("DATA");
            writer.Write(DataChunkSize);
            writer.Write(0x18);

            writer.BaseStream.Position = AudioDataOffset;

            byte[][] channels = AudioStream.Channels.Select(x => x.GetAudioData).ToArray();

            channels.Interleave(writer.BaseStream, GetBytesForAdpcmSamples(NumSamples), InterleaveSize, 0x20);
        }

        private static BrstmStructure ReadBrstmFile(Stream stream, bool readAudioData = true)
        {
            using (BinaryReader reader = GetBinaryReaderBE(stream))
            {
                if (Encoding.UTF8.GetString(reader.ReadBytes(4), 0, 4) != "RSTM")
                {
                    throw new InvalidDataException("File has no RSTM header");
                }

                var structure = new BrstmStructure();

                ParseRstmHeader(reader, structure);
                ParseHeadChunk(reader, structure);
                ParseAdpcChunk(reader, structure);
                ParseDataChunk(reader, structure, readAudioData);

                return structure;
            }
        }

        private static BrstmConfiguration GetConfiguration(BrstmStructure structure)
        {
            return new BrstmConfiguration()
            {
                SamplesPerInterleave = structure.SamplesPerInterleave,
                SamplesPerSeekTableEntry = structure.SamplesPerSeekTableEntry,
                TrackType = structure.HeaderType,
                SeekTableType = structure.SeekTableType
            };
        }

        private static AdpcmStream GetAdpcmStream(BrstmStructure structure)
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

        private static void ParseRstmHeader(BinaryReader reader, BrstmStructure structure)
        {
            reader.Expect((ushort)0xfeff);
            structure.Version = reader.ReadInt16();
            structure.FileSize = reader.ReadInt32();

            if (reader.BaseStream.Length < structure.FileSize)
            {
                throw new InvalidDataException("Actual file length is less than stated length");
            }

            structure.RstmHeaderSize = reader.ReadInt16();
            structure.RstmHeaderSections = reader.ReadInt16();

            structure.HeadChunkOffset = reader.ReadInt32();
            structure.HeadChunkSizeRstm = reader.ReadInt32();
            structure.AdpcChunkOffset = reader.ReadInt32();
            structure.AdpcChunkSizeRstm = reader.ReadInt32();
            structure.DataChunkOffset = reader.ReadInt32();
            structure.DataChunkSizeRstm = reader.ReadInt32();
        }

        private static void ParseHeadChunk(BinaryReader reader, BrstmStructure structure)
        {
            reader.BaseStream.Position = structure.HeadChunkOffset;

            if (Encoding.UTF8.GetString(reader.ReadBytes(4), 0, 4) != "HEAD")
            {
                throw new InvalidDataException("Unknown or invalid HEAD chunk");
            }

            structure.HeadChunkSize = reader.ReadInt32();
            if (structure.HeadChunkSize != structure.HeadChunkSizeRstm)
            {
                throw new InvalidDataException("HEAD chunk size in RSTM header doesn't match size in HEAD header");
            }

            reader.Expect(0x01000000);
            structure.HeadChunk1Offset = reader.ReadInt32();
            reader.Expect(0x01000000);
            structure.HeadChunk2Offset = reader.ReadInt32();
            reader.Expect(0x01000000);
            structure.HeadChunk3Offset = reader.ReadInt32();

            ParseHeadChunk1(reader, structure);
            ParseHeadChunk2(reader, structure);
            ParseHeadChunk3(reader, structure);
        }

        private static void ParseHeadChunk1(BinaryReader reader, BrstmStructure structure)
        {
            reader.BaseStream.Position = structure.HeadChunkOffset + 8 + structure.HeadChunk1Offset;
            structure.Codec = (B_stmCodec)reader.ReadByte();
            if (structure.Codec != B_stmCodec.Adpcm)
            {
                throw new NotSupportedException("File must contain 4-bit ADPCM encoded audio");
            }

            structure.Looping = reader.ReadByte() == 1;
            structure.NumChannels = reader.ReadByte();
            reader.BaseStream.Position += 1;

            structure.SampleRate = reader.ReadUInt16();
            reader.BaseStream.Position += 2;

            structure.LoopStart = reader.ReadInt32();
            structure.NumSamples = reader.ReadInt32();

            structure.AudioDataOffset = reader.ReadInt32();
            structure.InterleaveCount = reader.ReadInt32();
            structure.InterleaveSize = reader.ReadInt32();
            structure.SamplesPerInterleave = reader.ReadInt32();
            structure.LastBlockSizeWithoutPadding = reader.ReadInt32();
            structure.LastBlockSamples = reader.ReadInt32();
            structure.LastBlockSize = reader.ReadInt32();
            structure.SamplesPerSeekTableEntry = reader.ReadInt32();
        }

        private static void ParseHeadChunk2(BinaryReader reader, BrstmStructure structure)
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
                trackOffsets[i] = reader.ReadInt32();
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

        private static void ParseHeadChunk3(BinaryReader reader, BrstmStructure structure)
        {
            int baseOffset = structure.HeadChunkOffset + 8;
            reader.BaseStream.Position = baseOffset + structure.HeadChunk3Offset;

            reader.Expect((byte)structure.NumChannels);
            reader.BaseStream.Position += 3;

            for (int i = 0; i < structure.NumChannels; i++)
            {
                var channel = new B_stmChannelInfo();
                reader.Expect(0x01000000);
                channel.Offset = reader.ReadInt32();
                structure.Channels.Add(channel);
            }

            foreach (B_stmChannelInfo channel in structure.Channels)
            {
                reader.BaseStream.Position = baseOffset + channel.Offset;
                reader.Expect(0x01000000);
                int coefsOffset = reader.ReadInt32();
                reader.BaseStream.Position = baseOffset + coefsOffset;

                channel.Coefs = Enumerable.Range(0, 16).Select(x => reader.ReadInt16()).ToArray();
                channel.Gain = reader.ReadInt16();
                channel.PredScale = reader.ReadInt16();
                channel.Hist1 = reader.ReadInt16();
                channel.Hist2 = reader.ReadInt16();
                channel.LoopPredScale = reader.ReadInt16();
                channel.LoopHist1 = reader.ReadInt16();
                channel.LoopHist2 = reader.ReadInt16();
            }
        }

        private static void ParseAdpcChunk(BinaryReader reader, BrstmStructure structure)
        {
            reader.BaseStream.Position = structure.AdpcChunkOffset;

            if (Encoding.UTF8.GetString(reader.ReadBytes(4), 0, 4) != "ADPC")
            {
                throw new InvalidDataException("Unknown or invalid ADPC chunk");
            }
            structure.AdpcChunkSize = reader.ReadInt32();

            if (structure.AdpcChunkSizeRstm != structure.AdpcChunkSize)
            {
                throw new InvalidDataException("ADPC chunk size in RSTM header doesn't match size in ADPC header");
            }

            bool fullLastSeekTableEntry = structure.NumSamples % structure.SamplesPerSeekTableEntry == 0 && structure.NumSamples > 0;
            int bytesPerEntry = 4 * structure.NumChannels;
            int numSeekTableEntriesShortened = (GetBytesForAdpcmSamples(structure.NumSamples) / structure.SamplesPerSeekTableEntry) + 1;
            int numSeekTableEntriesStandard = (structure.NumSamples / structure.SamplesPerSeekTableEntry) + (fullLastSeekTableEntry ? 0 : 1);
            int expectedSizeShortened = GetNextMultiple(8 + numSeekTableEntriesShortened * bytesPerEntry, 0x20);
            int expectedSizeStandard = GetNextMultiple(8 + numSeekTableEntriesStandard * bytesPerEntry, 0x20);

            if (structure.AdpcChunkSize == expectedSizeStandard)
            {
                structure.SeekTableSize = bytesPerEntry * numSeekTableEntriesStandard;
                structure.SeekTableType = BrstmSeekTableType.Standard;
            }
            else if (structure.AdpcChunkSize == expectedSizeShortened)
            {
                structure.SeekTableSize = bytesPerEntry * numSeekTableEntriesShortened;
                structure.SeekTableType = BrstmSeekTableType.Short;
            }
            else
            {
                return; //Unknown format. Don't parse table
            }

            byte[] tableBytes = reader.ReadBytes(structure.SeekTableSize);

            structure.SeekTable = tableBytes.ToShortArray(Endianness.BigEndian)
                .DeInterleave(2, structure.NumChannels);
        }

        private static void ParseDataChunk(BinaryReader reader, BrstmStructure structure, bool readAudioData)
        {
            reader.BaseStream.Position = structure.DataChunkOffset;

            if (Encoding.UTF8.GetString(reader.ReadBytes(4), 0, 4) != "DATA")
            {
                throw new InvalidDataException("Unknown or invalid DATA chunk");
            }
            structure.DataChunkSize = reader.ReadInt32();

            if (structure.DataChunkSizeRstm != structure.DataChunkSize)
            {
                throw new InvalidDataException("DATA chunk size in RSTM header doesn't match size in DATA header");
            }

            if (!readAudioData) return;

            reader.BaseStream.Position = structure.AudioDataOffset;
            int audioDataLength = structure.DataChunkSize - (structure.AudioDataOffset - structure.DataChunkOffset);

            structure.AudioData = reader.BaseStream.DeInterleave(audioDataLength, structure.InterleaveSize,
                structure.NumChannels);
        }
    }
}
