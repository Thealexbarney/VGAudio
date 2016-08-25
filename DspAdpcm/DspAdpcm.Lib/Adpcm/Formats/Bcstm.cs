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

        private int NumSamples => AudioStream.Looping && Configuration.TrimFile ? LoopEnd : AudioStream.NumSamples;
        private int NumSamplesUntrimmed => AudioStream.Channels?[0]?.NumSamplesUntrimmed ?? 0;
        private int NumChannels => AudioStream.Channels.Count;
        private int NumTracks => AudioStream.Tracks.Count;

        private int AlignmentSamples => GetNextMultiple(AudioStream.LoopStart, Configuration.LoopPointAlignment) - AudioStream.LoopStart;
        private int LoopStart => AudioStream.LoopStart + AlignmentSamples;
        private int LoopEnd => AudioStream.LoopEnd + AlignmentSamples;

        private BcstmCodec Codec { get; } = BcstmCodec.Adpcm;
        private byte Looping => (byte)(AudioStream.Looping ? 1 : 0);
        private int InterleaveSize => GetBytesForAdpcmSamples(SamplesPerInterleave);
        private int SamplesPerInterleave => Configuration.SamplesPerInterleave;
        private int InterleaveCount => NumSamples.DivideByRoundUp(SamplesPerInterleave);
        private int LastBlockSamples => (AudioStream.Looping ? LoopEnd : NumSamples) - ((InterleaveCount - 1) * SamplesPerInterleave);

        private int AudioDataSize => Configuration.TrimFile
            ? GetBytesForAdpcmSamples(NumSamples)
            : (AudioStream.Channels[0]?.GetAudioData.Length ?? 0);
        private int LastBlockSizeWithoutPadding => GetBytesForAdpcmSamples(NumSamples - ((InterleaveCount - 1) * SamplesPerInterleave));
        private int LastBlockSize => Math.Min(GetNextMultiple(AudioDataSize - ((InterleaveCount - 1) * InterleaveSize), 0x20), InterleaveSize);

        private int SamplesPerSeekTableEntry => Configuration.SamplesPerSeekTableEntry;
        private bool FullLastSeekTableEntry => NumSamples % SamplesPerSeekTableEntry == 0 && NumSamples > 0;
        private int NumSeekTableEntries => (NumSamples / SamplesPerSeekTableEntry) + (FullLastSeekTableEntry ? 0 : 1);
        private int BytesPerSeekTableEntry => 4;

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

        private int SamplesToWrite => Configuration.TrimFile ? NumSamples : NumSamplesUntrimmed;
        private int DataChunkOffset => CstmHeaderLength + InfoChunkLength + SeekChunkLength;
        private int DataChunkLength => 0x20 + GetNextMultiple(GetBytesForAdpcmSamples(SamplesToWrite), 0x20) * NumChannels;

        /// <summary>
        /// The size in bytes of the BCSTM file.
        /// </summary>
        public int FileLength => CstmHeaderLength + InfoChunkLength + SeekChunkLength + DataChunkLength;

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

            var bcstm = new Bcstm();
            return bcstm.ReadBcstmFile(stream, false);
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

            stream.Position = 0;
            GetCstmHeader(stream);
            stream.Position = HeadChunkOffset;
            GetInfoChunk(stream);
            stream.Position = SeekChunkOffset;
            GetSeekChunk(stream);
            stream.Position = DataChunkOffset;
            GetDataChunk(stream);
        }

        private void GetCstmHeader(Stream stream)
        {
            BinaryWriter header = new BinaryWriter(stream);

            header.WriteASCII("CSTM");
            header.Write((ushort)0xfeff); //Endianness
            stream.Position += 6;
            header.Write(FileLength);

            header.Write(3); // NumEntries
            header.Write(0x4000);
            header.Write(InfoChunkOffset);
            header.Write(InfoChunkLength);
            header.Write(0x4001);
            header.Write(SeekChunkOffset);
            header.Write(SeekChunkLength);
            header.Write(0x4002);
            header.Write(DataChunkOffset);
            header.Write(DataChunkLength);
        }

        private void GetInfoChunk(Stream stream)
        {
            var chunk = new BinaryWriter(stream);

            chunk.WriteASCII("INFO");
            chunk.Write(InfoChunkLength);

            int headerTableLength = 8 * 3;

            chunk.Write(0x4100);
            chunk.Write(headerTableLength);
            if (Configuration.IncludeTrackInformation)
            {
                chunk.Write(0x0101);
                chunk.Write(headerTableLength + InfoChunk1Length);
            }
            else
            {
                chunk.Write(0);
                chunk.Write(-1);
            }
            chunk.Write(0x0101);
            chunk.Write(headerTableLength + InfoChunk1Length + InfoChunk2Length);

            GetInfoChunk1(stream);
            GetInfoChunk2(stream);
            GetInfoChunk3(stream);
        }

        private void GetInfoChunk1(Stream stream)
        {
            var chunk = new BinaryWriter(stream);

            chunk.Write((byte)Codec);
            chunk.Write(Looping);
            chunk.Write((byte)NumChannels);
            chunk.Write((byte)0);
            chunk.Write((ushort)AudioStream.SampleRate);
            chunk.Write((short)0);
            chunk.Write(LoopStart);
            chunk.Write(LoopEnd);
            chunk.Write(InterleaveCount);
            chunk.Write(InterleaveSize);
            chunk.Write(SamplesPerInterleave);
            chunk.Write(LastBlockSizeWithoutPadding);
            chunk.Write(LastBlockSamples);
            chunk.Write(LastBlockSize);
            chunk.Write(BytesPerSeekTableEntry);
            chunk.Write(SamplesPerSeekTableEntry);
            chunk.Write(0x1f00);
            chunk.Write(0x18);

            if (Configuration.InfoPart1Extra)
            {
                chunk.Write(0x100);
                chunk.Write(0);
                chunk.Write(-1);
            }
        }

        private void GetInfoChunk2(Stream stream)
        {
            if (!Configuration.IncludeTrackInformation) return;

            var chunk = new BinaryWriter(stream);
            int trackTableLength = 4 + 8 * NumTracks;
            int channelTableLength = 4 + 8 * NumChannels;
            int trackLength = 0x14;

            chunk.Write(NumTracks);

            for (int i = 0; i < NumTracks; i++)
            {
                chunk.Write(0x4101);
                chunk.Write(trackTableLength + channelTableLength + trackLength * i);
            }
        }

        private void GetInfoChunk3(Stream stream)
        {
            var chunk = new BinaryWriter(stream);
            int channelTableLength = 4 + 8 * NumChannels;
            int trackTableLength = Configuration.IncludeTrackInformation ? 0x14 * NumTracks : 0;

            chunk.Write(NumChannels);
            for (int i = 0; i < NumChannels; i++)
            {
                chunk.Write(0x4102);
                chunk.Write(channelTableLength + trackTableLength + 8 * i);
            }

            foreach (var track in AudioStream.Tracks)
            {
                chunk.Write((byte)track.Volume);
                chunk.Write((byte)track.Panning);
                chunk.Write((short)0);
                chunk.Write(0x100);
                chunk.Write(0xc);
                chunk.Write(track.NumChannels);
                chunk.Write((byte)track.ChannelLeft);
                chunk.Write((byte)track.ChannelRight);
                chunk.Write((short)0);
            }

            int channelTable2Length = 8 * NumChannels;
            for (int i = 0; i < NumChannels; i++)
            {
                chunk.Write(0x300);
                chunk.Write(channelTable2Length - 8 * i + ChannelInfoLength * i);
            }

            foreach (var channel in AudioStream.Channels)
            {
                chunk.Write(channel.Coefs.ToByteArray());
                chunk.Write((short)channel.GetAudioData[0]);
                chunk.Write(channel.Hist1);
                chunk.Write(channel.Hist2);
                chunk.Write(channel.LoopPredScale);
                chunk.Write(channel.LoopHist1);
                chunk.Write(channel.LoopHist2);
                chunk.Write(channel.Gain);
            }
        }

        private void GetSeekChunk(Stream stream)
        {
            var chunk = new BinaryWriter(stream);

            chunk.WriteASCII("SEEK");
            chunk.Write(SeekChunkLength);

            var table = Decode.BuildSeekTable(AudioStream.Channels, SamplesPerSeekTableEntry, NumSeekTableEntries, false);

            chunk.Write(table);
        }

        private void GetDataChunk(Stream stream)
        {
            var chunk = new BinaryWriter(stream);

            chunk.WriteASCII("DATA");
            chunk.Write(DataChunkLength);
            stream.Position += 0x18;

            byte[][] channels = AudioStream.Channels.Select(x => x.GetAudioData).ToArray();

            channels.Interleave(stream, GetBytesForAdpcmSamples(SamplesToWrite), InterleaveSize, 0x20);
        }

        private BcstmStructure ReadBcstmFile(Stream stream, bool readAudioData = true)
        {
            using (var reader = new BinaryReader(stream))
            {
                if (Encoding.UTF8.GetString(reader.ReadBytes(4), 0, 4) != "CSTM")
                {
                    throw new InvalidDataException("File has no CSTM header");
                }

                var structure = new BcstmStructure();

                reader.BaseStream.Position = 0xc;
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
                    AudioStream.SetLoop(structure.LoopStart, structure.LoopEnd);
                }
                AudioStream.Tracks = structure.Tracks;
                Configuration.IncludeTrackInformation = structure.IncludeTracks;
                Configuration.InfoPart1Extra = structure.InfoPart1Extra;

                ParseDataChunk(reader, structure);

                return structure;
            }
        }

        private static void ParseInfoChunk(BinaryReader chunk, BcstmStructure structure)
        {
            chunk.BaseStream.Position = structure.InfoChunkOffset;
            if (Encoding.UTF8.GetString(chunk.ReadBytes(4), 0, 4) != "INFO")
            {
                throw new InvalidDataException("Unknown or invalid INFO chunk");
            }

            structure.InfoChunkLength = chunk.ReadInt32();
            if (structure.InfoChunkLength != structure.InfoChunkLengthCstm)
            {
                throw new InvalidDataException("INFO chunk length in CSTM header doesn't match length in INFO header");
            }

            chunk.Expect(0x4100);
            structure.InfoChunk1Offset = chunk.ReadInt32();
            chunk.Expect(0x0101, 0);
            structure.InfoChunk2Offset = chunk.ReadInt32();
            chunk.Expect(0x0101);
            structure.InfoChunk3Offset = chunk.ReadInt32();

            ParseInfoChunk1(chunk, structure);
            ParseInfoChunk2(chunk, structure);
            ParseInfoChunk3(chunk, structure);
        }

        private static void ParseInfoChunk1(BinaryReader chunk, BcstmStructure structure)
        {
            chunk.BaseStream.Position = structure.InfoChunkOffset + 8 + structure.InfoChunk1Offset;
            structure.Codec = (BcstmCodec)chunk.ReadByte();
            if (structure.Codec != BcstmCodec.Adpcm)
            {
                throw new InvalidDataException("File must contain 4-bit ADPCM encoded audio");
            }

            structure.Looping = chunk.ReadByte() == 1;
            structure.NumChannelsPart1 = chunk.ReadByte();
            chunk.BaseStream.Position += 1;

            structure.SampleRate = chunk.ReadUInt16();
            chunk.BaseStream.Position += 2;

            structure.LoopStart = chunk.ReadInt32();
            structure.LoopEnd = chunk.ReadInt32();

            structure.InterleaveCount = chunk.ReadInt32();
            structure.InterleaveSize = chunk.ReadInt32();
            structure.SamplesPerInterleave = chunk.ReadInt32();
            structure.LastBlockSizeWithoutPadding = chunk.ReadInt32();
            structure.LastBlockSamples = chunk.ReadInt32();
            structure.LastBlockSize = chunk.ReadInt32();
            structure.BytesPerSeekTableEntry = chunk.ReadInt32();
            structure.SamplesPerSeekTableEntry = chunk.ReadInt32();
            structure.NumSamples =
                GetSampleFromNibble(((structure.InterleaveCount - 1) * structure.InterleaveSize +
                                     structure.LastBlockSizeWithoutPadding) * 2);

            chunk.BaseStream.Position += 8;
            structure.InfoPart1Extra = chunk.ReadInt32() == 0x100;
        }

        private static void ParseInfoChunk2(BinaryReader chunk, BcstmStructure structure)
        {
            if (structure.InfoChunk2Offset == -1)
            {
                structure.IncludeTracks = false;
                return;
            }

            structure.IncludeTracks = true;
            int part2Offset = structure.InfoChunkOffset + 8 + structure.InfoChunk2Offset;
            chunk.BaseStream.Position = part2Offset;

            int numTracks = chunk.ReadInt32();

            var offsets = new List<int>(numTracks);
            for (int i = 0; i < numTracks; i++)
            {
                chunk.Expect(0x4101);
                offsets.Add(chunk.ReadInt32());
            }

            foreach (int offset in offsets)
            {
                chunk.BaseStream.Position = part2Offset + offset;

                var track = new AdpcmTrack();
                track.Volume = chunk.ReadByte();
                track.Panning = chunk.ReadByte();
                chunk.BaseStream.Position += 2;

                chunk.BaseStream.Position += 8;
                track.NumChannels = chunk.ReadInt32();
                track.ChannelLeft = chunk.ReadByte();
                track.ChannelRight = chunk.ReadByte();
                structure.Tracks.Add(track);
            }
        }

        private static void ParseInfoChunk3(BinaryReader chunk, BcstmStructure structure)
        {
            int part3Offset = structure.InfoChunkOffset + 8 + structure.InfoChunk3Offset;
            chunk.BaseStream.Position = part3Offset;

            structure.NumChannelsPart3 = chunk.ReadInt32();

            for (int i = 0; i < structure.NumChannelsPart3; i++)
            {
                var channel = new BcstmChannelInfo();
                chunk.Expect(0x4102);
                channel.Offset = chunk.ReadInt32();
                structure.Channels.Add(channel);
            }

            foreach (BcstmChannelInfo channel in structure.Channels)
            {
                int channelInfoOffset = part3Offset + channel.Offset;
                chunk.BaseStream.Position = channelInfoOffset;
                chunk.Expect(0x0300);
                int coefsOffset = chunk.ReadInt32() + channelInfoOffset;
                chunk.BaseStream.Position = coefsOffset;

                channel.Coefs = Enumerable.Range(0, 16).Select(x => chunk.ReadInt16()).ToArray();

                channel.PredScale = chunk.ReadInt16();
                channel.Hist1 = chunk.ReadInt16();
                channel.Hist2 = chunk.ReadInt16();
                channel.LoopPredScale = chunk.ReadInt16();
                channel.LoopHist1 = chunk.ReadInt16();
                channel.LoopHist2 = chunk.ReadInt16();
                channel.Gain = chunk.ReadInt16();
            }
        }

        private static void ParseSeekChunk(BinaryReader chunk, BcstmStructure structure)
        {
            chunk.BaseStream.Position = structure.SeekChunkOffset;

            if (Encoding.UTF8.GetString(chunk.ReadBytes(4), 0, 4) != "SEEK")
            {
                throw new InvalidDataException("Unknown or invalid SEEK chunk");
            }
            structure.SeekChunkLength = chunk.ReadInt32();

            if (structure.SeekChunkLengthCstm != structure.SeekChunkLength)
            {
                throw new InvalidDataException("SEEK chunk length in CSTM header doesn't match length in SEEK header");
            }

            bool fullLastSeekTableEntry = structure.NumSamples % structure.SamplesPerSeekTableEntry == 0 && structure.NumSamples > 0;
            int bytesPerEntry = 4 * structure.NumChannelsPart1;
            int numSeekTableEntries = (structure.NumSamples / structure.SamplesPerSeekTableEntry) + (fullLastSeekTableEntry ? 0 : 1);

            structure.SeekTableLength = bytesPerEntry * numSeekTableEntries;

            byte[] tableBytes = chunk.ReadBytes(structure.SeekTableLength);

            structure.SeekTable = tableBytes.ToShortArray()
                .DeInterleave(2, structure.NumChannelsPart1);
        }

        private void ParseDataChunk(BinaryReader chunk, BcstmStructure structure)
        {
            chunk.BaseStream.Position = structure.DataChunkOffset;

            if (Encoding.UTF8.GetString(chunk.ReadBytes(4), 0, 4) != "DATA")
            {
                throw new InvalidDataException("Unknown or invalid DATA chunk");
            }
            structure.DataChunkLength = chunk.ReadInt32();

            if (structure.DataChunkLengthCstm != structure.DataChunkLength)
            {
                throw new InvalidDataException("DATA chunk length in CSTM header doesn't match length in DATA header");
            }

            int audioDataOffset = structure.DataChunkOffset + 0x20;
            chunk.BaseStream.Position = audioDataOffset;
            int audioDataLength = structure.DataChunkLength - (audioDataOffset - structure.DataChunkOffset);

            byte[][] deInterleavedAudioData = chunk.BaseStream.DeInterleave(audioDataLength, structure.InterleaveSize,
                structure.NumChannelsPart1);

            for (int c = 0; c < structure.NumChannelsPart1; c++)
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
            public bool IncludeTrackInformation { get; set; } = true;
            public bool InfoPart1Extra { get; set; } = false;
        }
    }
}
