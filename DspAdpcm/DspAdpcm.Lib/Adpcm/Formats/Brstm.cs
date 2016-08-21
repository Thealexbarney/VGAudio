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
        private int InterleaveSize => GetBytesForAdpcmSamples(SamplesPerInterleave);
        private int SamplesPerInterleave => Configuration.SamplesPerInterleave;
        private int InterleaveCount => (NumSamples / SamplesPerInterleave) + (FullLastBlock ? 0 : 1);
        private int LastBlockSizeWithoutPadding => GetBytesForAdpcmSamples(LastBlockSamples);
        private int LastBlockSamples => FullLastBlock ? SamplesPerInterleave : NumSamples % SamplesPerInterleave;
        private int LastBlockSize => GetNextMultiple(LastBlockSizeWithoutPadding, 0x20);
        private bool FullLastBlock => NumSamples % SamplesPerInterleave == 0 && NumChannels > 0;
        private int SamplesPerAdpcEntry => Configuration.SamplesPerAdpcEntry;
        private bool FullLastAdpcEntry => NumSamples % SamplesPerAdpcEntry == 0 && NumSamples > 0;
        private int NumAdpcEntriesShortened => (GetBytesForAdpcmSamples(NumSamples) / SamplesPerAdpcEntry) + 1;
        private int NumAdpcEntries => Configuration.SeekTableType == BrstmSeekTableType.Standard ?
            (NumSamples / SamplesPerAdpcEntry) + (FullLastAdpcEntry ? 0 : 1) : NumAdpcEntriesShortened;
        private int BytesPerAdpcEntry => 4; //Or is it bits per sample?

        private int RstmHeaderLength => 0x40;

        private int HeadChunkOffset => RstmHeaderLength;
        private int HeadChunkLength => GetNextMultiple(HeadChunkHeaderLength + HeadChunkTableLength +
            HeadChunk1Length + HeadChunk2Length + HeadChunk3Length, 0x20);
        private int HeadChunkHeaderLength => 8;
        private int HeadChunkTableLength => 8 * 3;
        private int HeadChunk1Length => 0x34;
        private int HeadChunk2Length => 4 + (8 * NumTracks) + (TrackInfoLength * NumTracks);
        private BrstmTrackType HeaderType => Configuration.HeaderType;
        private int TrackInfoLength => HeaderType == BrstmTrackType.Short ? 4 : 0x0c;
        private int HeadChunk3Length => 4 + (8 * NumChannels) + (ChannelInfoLength * NumChannels);
        private int ChannelInfoLength => 0x38;

        private int AdpcChunkOffset => RstmHeaderLength + HeadChunkLength;
        private int AdpcChunkLength => GetNextMultiple(8 + NumAdpcEntries * NumChannels * BytesPerAdpcEntry, 0x20);

        private int DataChunkOffset => RstmHeaderLength + HeadChunkLength + AdpcChunkLength;
        private int DataChunkLength => GetNextMultiple(0x20 + (InterleaveCount - (LastBlockSamples == 0 ? 0 : 1)) * InterleaveSize * NumChannels + LastBlockSize * NumChannels, 0x20);

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

            var brstm = new Brstm();
            var a = brstm.ReadBrstmFile(stream, false);
            return a;
        }

        private void RecalculateData()
        {
            var seekTableToCalculate = Configuration.RecalculateSeekTable
                ? AudioStream.Channels.Where(
                    x => !x.SelfCalculatedSeekTable || x.SamplesPerSeekTableEntry != SamplesPerAdpcEntry)
                : AudioStream.Channels.Where(
                    x => x.SeekTable == null || x.SamplesPerSeekTableEntry != SamplesPerAdpcEntry);

            var loopContextToCalculate = Configuration.RecalculateLoopContext
                ? AudioStream.Channels.Where(x => !x.SelfCalculatedLoopContext)
                : AudioStream.Channels.Where(x => !x.LoopContextCalculated);

            if (AudioStream.Looping)
            {
                Decode.CalculateLoopAlignment(AudioStream.Channels, Configuration.LoopPointAlignment,
                    AudioStream.LoopStart, AudioStream.LoopEnd);
            }
            Decode.CalculateAdpcTable(seekTableToCalculate, SamplesPerAdpcEntry);
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

            stream.Position = 0;
            GetRstmHeader(stream);
            stream.Position = HeadChunkOffset;
            GetHeadChunk(stream);
            stream.Position = AdpcChunkOffset;
            GetAdpcChunk(stream);
            stream.Position = DataChunkOffset;
            GetDataChunk(stream);
        }

        private void GetRstmHeader(Stream stream)
        {
            BinaryWriterBE header = new BinaryWriterBE(stream);

            header.WriteASCII("RSTM");
            header.WriteBE((ushort)0xfeff); //Endianness
            header.WriteBE((short)0x0100); //BRSTM format version
            header.WriteBE(FileLength);
            header.WriteBE((short)RstmHeaderLength);
            header.WriteBE((short)2); // NumEntries
            header.WriteBE(HeadChunkOffset);
            header.WriteBE(HeadChunkLength);
            header.WriteBE(AdpcChunkOffset);
            header.WriteBE(AdpcChunkLength);
            header.WriteBE(DataChunkOffset);
            header.WriteBE(DataChunkLength);
        }

        private void GetHeadChunk(Stream stream)
        {
            var chunk = new BinaryWriterBE(stream);

            chunk.WriteASCII("HEAD");
            chunk.WriteBE(HeadChunkLength);
            chunk.Write(GetHeadChunkHeader());
            chunk.Write(GetHeadChunk1());
            chunk.Write(GetHeadChunk2());
            chunk.Write(GetHeadChunk3());
        }

        private byte[] GetHeadChunkHeader()
        {
            var chunk = new List<byte>();

            chunk.Add32BE(0x01000000);
            chunk.Add32BE(HeadChunkTableLength); //Chunk 1 offset
            chunk.Add32BE(0x01000000);
            chunk.Add32BE(HeadChunkTableLength + HeadChunk1Length); //Chunk 2 offset
            chunk.Add32BE(0x01000000);
            chunk.Add32BE(HeadChunkTableLength + HeadChunk1Length + HeadChunk2Length); //Chunk 3 offset

            return chunk.ToArray();
        }

        private byte[] GetHeadChunk1()
        {
            var chunk = new List<byte>();

            chunk.Add((byte)Codec);
            chunk.Add(Looping);
            chunk.Add((byte)NumChannels);
            chunk.Add(0); //padding
            chunk.Add16BE(AudioStream.SampleRate);
            chunk.Add16BE(0); //padding
            chunk.Add32BE(LoopStart);
            chunk.Add32BE(NumSamples);
            chunk.Add32BE(AudioDataOffset);
            chunk.Add32BE(InterleaveCount);
            chunk.Add32BE(InterleaveSize);
            chunk.Add32BE(SamplesPerInterleave);
            chunk.Add32BE(LastBlockSizeWithoutPadding);
            chunk.Add32BE(LastBlockSamples);
            chunk.Add32BE(LastBlockSize);
            chunk.Add32BE(SamplesPerAdpcEntry);
            chunk.Add32BE(BytesPerAdpcEntry);

            return chunk.ToArray();
        }

        private byte[] GetHeadChunk2()
        {
            var chunk = new List<byte>();

            chunk.Add((byte)NumTracks);
            chunk.Add((byte)(HeaderType == BrstmTrackType.Short ? 0 : 1));
            chunk.Add16BE(0);

            int baseOffset = HeadChunkTableLength + HeadChunk1Length + 4;
            int offsetTableLength = NumTracks * 8;

            for (int i = 0; i < NumTracks; i++)
            {
                chunk.Add32BE(HeaderType == BrstmTrackType.Short ? 0x01000000 : 0x01010000);
                chunk.Add32BE(baseOffset + offsetTableLength + TrackInfoLength * i);
            }

            foreach (AdpcmTrack track in AudioStream.Tracks)
            {
                if (HeaderType == BrstmTrackType.Standard)
                {
                    chunk.Add((byte)track.Volume);
                    chunk.Add((byte)track.Panning);
                    chunk.Add16BE(0);
                    chunk.Add32BE(0);
                }
                chunk.Add((byte)track.NumChannels);
                chunk.Add((byte)track.ChannelLeft); //First channel ID
                chunk.Add((byte)track.ChannelRight); //Second channel ID
                chunk.Add(0);
            }

            chunk.AddRange(new byte[HeadChunk2Length - chunk.Count]);

            return chunk.ToArray();
        }

        private byte[] GetHeadChunk3()
        {
            var chunk = new List<byte>();

            chunk.Add((byte)NumChannels);
            chunk.Add(0); //padding
            chunk.Add16BE(0); //padding

            int baseOffset = HeadChunkTableLength + HeadChunk1Length + HeadChunk2Length + 4;
            int offsetTableLength = NumChannels * 8;

            for (int i = 0; i < NumChannels; i++)
            {
                chunk.Add32BE(0x01000000);
                chunk.Add32BE(baseOffset + offsetTableLength + ChannelInfoLength * i);
            }

            for (int i = 0; i < NumChannels; i++)
            {
                AdpcmChannel channel = AudioStream.Channels[i];
                chunk.Add32BE(0x01000000);
                chunk.Add32BE(baseOffset + offsetTableLength + ChannelInfoLength * i + 8);
                chunk.AddRange(channel.Coefs.ToFlippedBytes());
                chunk.Add16BE(channel.Gain);
                chunk.Add16BE(channel.GetAudioData[0]);
                chunk.Add16BE(channel.Hist1);
                chunk.Add16BE(channel.Hist2);
                chunk.Add16BE(AudioStream.Looping ? channel.LoopPredScale : channel.GetAudioData[0]);
                chunk.Add16BE(AudioStream.Looping ? channel.LoopHist1 : 0);
                chunk.Add16BE(AudioStream.Looping ? channel.LoopHist2 : 0);
                chunk.Add16(0);
            }

            return chunk.ToArray();
        }

        private void GetAdpcChunk(Stream stream)
        {
            var chunk = new BinaryWriterBE(stream);

            chunk.WriteASCII("ADPC");
            chunk.WriteBE(AdpcChunkLength);

            var table = Decode.BuildAdpcTable(AudioStream.Channels, SamplesPerAdpcEntry, NumAdpcEntries).ToArray();

            chunk.Write(table);
        }

        private void GetDataChunk(Stream stream)
        {
            var chunk = new BinaryWriterBE(stream);

            chunk.WriteASCII("DATA");
            chunk.WriteBE(DataChunkLength);
            chunk.WriteBE(0x18);

            stream.Position += AudioDataOffset - DataChunkOffset - 3 * sizeof(int);

            byte[][] channels = AudioStream.Channels.Select(x => x.GetAudioData).ToArray();

            channels.Interleave(stream, GetBytesForAdpcmSamples(NumSamples), InterleaveSize, LastBlockSize);
        }

        private BrstmStructure ReadBrstmFile(Stream stream, bool readAudioData = true)
        {
            using (var reader = new BinaryReaderBE(stream))
            {
                if (Encoding.UTF8.GetString(reader.ReadBytes(4), 0, 4) != "RSTM")
                {
                    throw new InvalidDataException("File has no RSTM header");
                }

                var structure = new BrstmStructure();

                reader.BaseStream.Position = 8;
                structure.FileLength = reader.ReadInt32BE();

                if (stream.Length < structure.FileLength)
                {
                    throw new InvalidDataException("Actual file length is less than stated length");
                }

                structure.RstmHeaderLength = reader.ReadInt16BE();
                reader.BaseStream.Position += 2;

                structure.HeadChunkOffset = reader.ReadInt32BE();
                structure.HeadChunkLengthRstm = reader.ReadInt32BE();
                structure.AdpcChunkOffset = reader.ReadInt32BE();
                structure.AdpcChunkLengthRstm = reader.ReadInt32BE();
                structure.DataChunkOffset = reader.ReadInt32BE();
                structure.DataChunkLengthRstm = reader.ReadInt32BE();

                reader.BaseStream.Position = structure.HeadChunkOffset;
                byte[] headChunk = reader.ReadBytes(structure.HeadChunkLengthRstm);
                ParseHeadChunk(headChunk, structure);

                Configuration.SamplesPerInterleave = structure.SamplesPerInterleave;
                Configuration.SamplesPerAdpcEntry = structure.SamplesPerAdpcEntry;
                Configuration.HeaderType = structure.HeaderType;

                AudioStream = new AdpcmStream(structure.NumSamples, structure.SampleRate);
                if (structure.Looping)
                {
                    AudioStream.SetLoop(structure.LoopStart, structure.NumSamples);
                }
                AudioStream.Tracks = structure.Tracks;

                ParseAdpcChunk(stream, structure);

                if (!readAudioData)
                {
                    reader.BaseStream.Position = structure.DataChunkOffset + 4;
                    structure.DataChunkLength = reader.ReadInt32BE();
                    return structure;
                }

                ParseDataChunk(stream, structure);

                Configuration.SeekTableType = structure.SeekTableType;

                return structure;
            }
        }

        private static void ParseHeadChunk(byte[] head, BrstmStructure structure)
        {
            const int baseOffset = 8;

            if (Encoding.UTF8.GetString(head, 0, 4) != "HEAD")
            {
                throw new InvalidDataException("Unknown or invalid HEAD chunk");
            }

            using (var reader = new BinaryReaderBE(new MemoryStream(head)))
            {
                reader.BaseStream.Position = 4;
                structure.HeadChunkLength = reader.ReadInt32BE();
                if (structure.HeadChunkLength != head.Length)
                {
                    throw new InvalidDataException("HEAD chunk stated length does not match actual length");
                }

                reader.BaseStream.Position += 4;
                structure.HeadChunk1Offset = reader.ReadInt32BE();
                reader.BaseStream.Position += 4;
                structure.HeadChunk2Offset = reader.ReadInt32BE();
                reader.BaseStream.Position += 4;
                structure.HeadChunk3Offset = reader.ReadInt32BE();

                reader.BaseStream.Position = structure.HeadChunk1Offset + baseOffset;
                byte[] headChunk1 = reader.ReadBytes(structure.HeadChunk1Length);

                reader.BaseStream.Position = structure.HeadChunk2Offset + baseOffset;
                byte[] headChunk2 = reader.ReadBytes(structure.HeadChunk2Length);

                reader.BaseStream.Position = structure.HeadChunk3Offset + baseOffset;
                byte[] headChunk3 = reader.ReadBytes(structure.HeadChunk3Length);

                ParseHeadChunk1(headChunk1, structure);
                ParseHeadChunk2(headChunk2, structure);
                ParseHeadChunk3(headChunk3, structure);
            }
        }

        private static void ParseHeadChunk1(byte[] chunk, BrstmStructure structure)
        {
            using (var reader = new BinaryReaderBE(new MemoryStream(chunk)))
            {
                structure.Codec = (BrstmCodec)reader.ReadByte();
                if (structure.Codec != BrstmCodec.Adpcm)
                {
                    throw new InvalidDataException("File must contain 4-bit ADPCM encoded audio");
                }

                structure.Looping = reader.ReadByte() == 1;
                structure.NumChannelsPart1 = reader.ReadByte();
                reader.BaseStream.Position += 1;

                structure.SampleRate = (ushort)reader.ReadInt16BE();
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
                structure.SamplesPerAdpcEntry = reader.ReadInt32BE();
            }
        }

        private static void ParseHeadChunk2(byte[] chunk, BrstmStructure structure)
        {
            using (var reader = new BinaryReaderBE(new MemoryStream(chunk)))
            {
                int numTracks = reader.ReadByte();
                int[] trackOffsets = new int[numTracks];

                structure.HeaderType = reader.ReadByte() == 0 ? BrstmTrackType.Short : BrstmTrackType.Standard;

                reader.BaseStream.Position = 4;
                for (int i = 0; i < numTracks; i++)
                {
                    reader.BaseStream.Position += 4;
                    trackOffsets[i] = reader.ReadInt32BE();
                }

                for (int i = 0; i < numTracks; i++)
                {
                    reader.BaseStream.Position = trackOffsets[i] - structure.HeadChunk2Offset;
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
        }

        private static void ParseHeadChunk3(byte[] chunk, BrstmStructure structure)
        {
            using (var reader = new BinaryReaderBE(new MemoryStream(chunk)))
            {
                structure.NumChannelsPart3 = reader.ReadByte();
                reader.BaseStream.Position += 3;

                for (int i = 0; i < structure.NumChannelsPart3; i++)
                {
                    var channel = new BrstmChannelInfo();
                    reader.BaseStream.Position += 4;
                    channel.Offset = reader.ReadInt32BE();
                    structure.Channels.Add(channel);
                }

                foreach (BrstmChannelInfo channel in structure.Channels)
                {
                    reader.BaseStream.Position = channel.Offset - structure.HeadChunk3Offset + 4;
                    int coefsOffset = reader.ReadInt32BE();
                    reader.BaseStream.Position = coefsOffset - structure.HeadChunk3Offset;

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
        }

        private static void ParseAdpcChunk(Stream chunk, BrstmStructure structure)
        {
            var reader = new BinaryReaderBE(chunk);
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

            bool fullLastAdpcEntry = structure.NumSamples % structure.SamplesPerAdpcEntry == 0 && structure.NumSamples > 0;
            int bytesPerEntry = 4 * structure.NumChannelsPart1;
            int numAdpcEntriesShortened = (GetBytesForAdpcmSamples(structure.NumSamples) / structure.SamplesPerAdpcEntry) + 1;
            int numAdpcEntriesStandard = (structure.NumSamples / structure.SamplesPerAdpcEntry) + (fullLastAdpcEntry ? 0 : 1);
            int expectedLengthShortened = GetNextMultiple(8 + numAdpcEntriesShortened * bytesPerEntry, 0x20);
            int expectedLengthStandard = GetNextMultiple(8 + numAdpcEntriesStandard * bytesPerEntry, 0x20);

            if (structure.AdpcChunkLength == expectedLengthStandard)
            {
                structure.SeekTableLength = bytesPerEntry * numAdpcEntriesStandard;
                structure.SeekTableType = BrstmSeekTableType.Standard;
            }
            else if (structure.AdpcChunkLength == expectedLengthShortened)
            {
                structure.SeekTableLength = bytesPerEntry * numAdpcEntriesShortened;
                structure.SeekTableType = BrstmSeekTableType.Short;
            }
            else
            {
                return; //Unknown format. Don't parse table
            }

            byte[] tableBytes = reader.ReadBytes(structure.SeekTableLength);

            structure.SeekTable = tableBytes.ToShortArrayFlippedBytes()
                .DeInterleave(2, structure.NumChannelsPart1);
        }

        private void ParseDataChunk(Stream chunk, BrstmStructure structure)
        {
            var reader = new BinaryReaderBE(chunk);
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
                structure.NumChannelsPart1, structure.LastBlockSize, GetBytesForAdpcmSamples(structure.NumSamples));

            for (int c = 0; c < structure.NumChannelsPart1; c++)
            {
                var channel = new AdpcmChannel(structure.NumSamples, deInterleavedAudioData[c])
                {
                    Coefs = structure.Channels[c].Coefs,
                    Gain = structure.Channels[c].Gain,
                    Hist1 = structure.Channels[c].Hist1,
                    Hist2 = structure.Channels[c].Hist2,
                    SeekTable = structure.SeekTable?[c],
                    SamplesPerSeekTableEntry = structure.SamplesPerAdpcEntry
                };
                channel.SetLoopContext(structure.Channels[c].LoopPredScale, structure.Channels[c].LoopHist1,
                    structure.Channels[c].LoopHist2);
                AudioStream.Channels.Add(channel);
            }
        }

        /// <summary>
        /// Contains the options used to build the BRSTM file.
        /// </summary>
        public class BrstmConfiguration
        {
            private int _samplesPerInterleave = 0x3800;
            private int _samplesPerAdpcEntry = 0x3800;
            /// <summary>
            /// The type of track description to be used when building the 
            /// BRSTM header.
            /// Default is <see cref="BrstmTrackType.Short"/>
            /// </summary>
            public BrstmTrackType HeaderType { get; set; } = BrstmTrackType.Short;

            /// <summary>
            /// The type of seek table to use when building the BRSTM
            /// ADPC chunk.
            /// Default is <see cref="BrstmSeekTableType.Standard"/>
            /// </summary>
            public BrstmSeekTableType SeekTableType { get; set; } = BrstmSeekTableType.Standard;

            /// <summary>
            /// If <c>true</c>, rebuilds the seek table when building the BRSTM.
            /// If <c>false</c>, reuses the seek table read from an imported BRSTM
            /// if available.
            /// Default is <c>true</c>.
            /// </summary>
            public bool RecalculateSeekTable { get; set; } = true;

            /// <summary>
            /// If <c>true</c>, recalculates the loop context when building the BRSTM.
            /// If <c>false</c>, reuses the loop context read from an imported BRSTM
            /// if available.
            /// Default is <c>true</c>.
            /// </summary>
            public bool RecalculateLoopContext { get; set; } = true;

            /// <summary>
            /// The number of samples in each block when interleaving
            /// the audio data in a BRSTM file.
            /// Must be divisible by 14.
            /// Default is 14,336 (0x3800).
            /// </summary>
            /// <exception cref="ArgumentOutOfRangeException">Thrown if value is negative 
            /// or not divisible by 14.</exception>
            public int SamplesPerInterleave
            {
                get { return _samplesPerInterleave; }
                set
                {
                    if (value < 1)
                    {
                        throw new ArgumentOutOfRangeException(nameof(value), value,
                            "Number of samples per interleave must be positive");
                    }
                    if (value % SamplesPerBlock != 0)
                    {
                        throw new ArgumentOutOfRangeException(nameof(value), value,
                            "Number of samples per interleave must be divisible by 14");
                    }
                    _samplesPerInterleave = value;
                }
            }

            /// <summary>
            /// The number of samples per entry in the seek table. Used when
            /// building a BRSTM file.
            /// Default is 14,336 (0x3800).
            /// </summary>
            /// <exception cref="ArgumentOutOfRangeException">Thrown if
            /// value is less than 2.</exception>
            public int SamplesPerAdpcEntry
            {
                get { return _samplesPerAdpcEntry; }
                set
                {
                    if (value < 2)
                    {
                        throw new ArgumentOutOfRangeException(nameof(value), value,
                            "Number of samples per interleave must be 2 or greater");
                    }
                    _samplesPerAdpcEntry = value;
                }
            }

            /// <summary>
            /// When building the BRSTM file, the loop points and audio will
            /// be adjusted so that the start loop point is a multiple of
            /// this number. Default is 14,336 (0x3800).
            /// </summary>
            public int LoopPointAlignment { get; set; } = 0x3800;
        }
    }
}
