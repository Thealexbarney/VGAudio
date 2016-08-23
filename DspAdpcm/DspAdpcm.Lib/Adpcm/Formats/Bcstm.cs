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
                
                return structure;
            }
        }
    }
}
