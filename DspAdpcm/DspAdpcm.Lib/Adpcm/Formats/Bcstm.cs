using System;
using System.IO;

namespace DspAdpcm.Lib.Adpcm.Formats
{
    /// <summary>
    /// Represents a BCSTM file.
    /// </summary>
    public class Bcstm : BCFstm
    {
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
        /// Initializes a new <see cref="Bcstm"/> by parsing an existing
        /// BCSTM file.
        /// </summary>
        /// <param name="file">A <c>byte[]</c> containing 
        /// the BCSTM file.</param>
        public Bcstm(byte[] file)
        {
            using (var stream = new MemoryStream(file))
            {
                ReadBcstmFile(stream);
            }
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

        /// <summary>
        /// Initializes a new <see cref="Bcstm"/> by parsing an existing
        /// BCSTM file.
        /// </summary>
        /// <param name="file">A <c>byte[]</c> containing 
        /// the BCSTM file.</param>
        /// <param name="configuration">A <see cref="BcstmConfiguration"/>
        /// to use for the <see cref="Bcstm"/></param>
        public Bcstm(byte[] file, BcstmConfiguration configuration) : this(file)
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
        /// <returns>A <see cref="BCFstmStructure"/> containing
        /// the data from the BCSTM header.</returns>
        public static BcstmStructure ReadMetadata(Stream stream)
        {
            if (!stream.CanSeek)
            {
                throw new NotSupportedException("A seekable stream is required");
            }

            return new Bcstm().ReadBcstmFile(stream, false);
        }

        /// <summary>
        /// Builds a BRSTM file from the current <see cref="BCFstm.AudioStream"/>.
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
            WriteBCFstmFile(stream, BCFstmType.Bfstm);
        }
    }

    /// <summary>
    /// Contains the options used to build the BCSTM file.
    /// </summary>
    public class BcstmConfiguration : BCFstm.BCFstmConfiguration
    { }
}
