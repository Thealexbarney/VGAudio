using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

                return structure;
            }
        }

        private static void ParseInfoChunk(BinaryReader chunk, BcstmStructure structure)
        {
            chunk.BaseStream.Position = structure.InfoChunkOffset;

            byte[] chunkId = chunk.ReadBytes(4);
            if (Encoding.UTF8.GetString(chunkId, 0, 4) != "INFO")
            {
                throw new InvalidDataException("Unknown or invalid INFO chunk");
            }

            structure.InfoChunkLength = chunk.ReadInt32();
            if (structure.InfoChunkLength != structure.InfoChunkLengthCstm)
            {
                throw new InvalidDataException("INFO chunk length in CSTM header doesn't match length in INFO header");
            }

            chunk.BaseStream.Position += 4;
            structure.InfoChunk1Offset = chunk.ReadInt32();
            chunk.BaseStream.Position += 4;
            structure.InfoChunk2Offset = chunk.ReadInt32();
            chunk.BaseStream.Position += 4;
            structure.InfoChunk3Offset = chunk.ReadInt32();

            ParseInfoChunk1(chunk, structure);
            ParseInfoChunk3(chunk, structure);
        }

        private static void ParseInfoChunk1(BinaryReader chunk, BcstmStructure structure)
        {
            chunk.BaseStream.Position = structure.InfoChunkOffset + 8 + structure.InfoChunk1Offset;
            structure.Codec = (BcstmCodec) chunk.ReadByte();
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

        private static void ParseInfoChunk3(BinaryReader chunk, BcstmStructure structure)
        {
            int part3Offset = structure.InfoChunkOffset + 8 + structure.InfoChunk3Offset;
            chunk.BaseStream.Position = part3Offset;

            structure.NumChannelsPart3 = chunk.ReadInt32();

            for (int i = 0; i < structure.NumChannelsPart3; i++)
            {
                var channel = new BcstmChannelInfo();
                chunk.BaseStream.Position += 4;
                channel.Offset = chunk.ReadInt32();
                structure.Channels.Add(channel);
            }

            foreach (BcstmChannelInfo channel in structure.Channels)
            {
                int channelInfoOffset = part3Offset + channel.Offset;
                chunk.BaseStream.Position = channelInfoOffset + 4;
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
    }
}
