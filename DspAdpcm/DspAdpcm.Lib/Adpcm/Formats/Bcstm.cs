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

        private Bcstm()
        {
        }

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

                AudioStream = new AdpcmStream(structure.NumSamples, structure.SampleRate);
                if (structure.Looping)
                {
                    AudioStream.SetLoop(structure.LoopStart, structure.NumSamples);
                }
                AudioStream.Tracks = structure.Tracks;

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
            structure.NumSamples = chunk.ReadInt32();

            structure.InterleaveCount = chunk.ReadInt32();
            structure.InterleaveSize = chunk.ReadInt32();
            structure.SamplesPerInterleave = chunk.ReadInt32();
            structure.LastBlockSizeWithoutPadding = chunk.ReadInt32();
            structure.LastBlockSamples = chunk.ReadInt32();
            structure.LastBlockSize = chunk.ReadInt32();
            structure.BytesPerSeekTableEntry = chunk.ReadInt32();
            structure.SamplesPerSeekTableEntry = chunk.ReadInt32();
        }

        private static void ParseInfoChunk2(BinaryReader chunk, BcstmStructure structure)
        {
            if (structure.InfoChunk2Offset == -1) return;
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

            bool fullLastAdpcEntry = structure.NumSamples % structure.SamplesPerSeekTableEntry == 0 && structure.NumSamples > 0;
            int bytesPerEntry = 4 * structure.NumChannelsPart1;
            int numAdpcEntries = (structure.NumSamples / structure.SamplesPerSeekTableEntry) + (fullLastAdpcEntry ? 0 : 1);

            structure.SeekTableLength = bytesPerEntry * numAdpcEntries;

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
    }
}
