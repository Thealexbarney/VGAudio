using System;
using System.IO;
using System.Text;

namespace DspAdpcm.Adpcm.Formats
{
    /// <summary>
    /// Represents an IDSP file.
    /// </summary>
    public class Idsp
    {
        /// <summary>
        /// The underlying <see cref="AdpcmStream"/> used to build the IDSP file.
        /// </summary>
        public AdpcmStream AudioStream { get; set; }

        /// <summary>
        /// Contains various settings used when building the IDSP file.
        /// </summary>
        public IdspConfiguration Configuration { get; } = new IdspConfiguration();

        /// <summary>
        /// The size in bytes of the IDSP file.
        /// </summary>
        public int FileLength => 0;// RstmHeaderLength + HeadChunkLength + AdpcChunkLength + DataChunkLength;

        /// <summary>
        /// Initializes a new <see cref="Idsp"/> from an <see cref="AdpcmStream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="AdpcmStream"/> used to
        /// create the <see cref="Idsp"/>.</param>
        public Idsp(AdpcmStream stream)
        {
            if (stream.Channels.Count < 1)
            {
                throw new InvalidDataException("Stream must have at least one channel ");
            }

            AudioStream = stream;
        }

        /// <summary>
        /// Initializes a new <see cref="Idsp"/> by parsing an existing
        /// IDSP file.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> containing 
        /// the IDSP file. Must be seekable.</param>
        public Idsp(Stream stream)
        {
            if (!stream.CanSeek)
            {
                throw new NotSupportedException("A seekable stream is required");
            }

            ReadIdspFile(stream);
        }

        /// <summary>
        /// Initializes a new <see cref="Idsp"/> by parsing an existing
        /// IDSP file.
        /// </summary>
        /// <param name="file">A <c>byte[]</c> containing 
        /// the IDSP file.</param>
        public Idsp(byte[] file)
        {
            using (var stream = new MemoryStream(file))
            {
                ReadIdspFile(stream);
            }
        }

        /// <summary>
        /// Initializes a new <see cref="Idsp"/> from an <see cref="AdpcmStream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="AdpcmStream"/> used to
        /// create the <see cref="Idsp"/>.</param>
        /// <param name="configuration">A <see cref="IdspConfiguration"/>
        /// to use for the <see cref="Idsp"/></param>
        public Idsp(AdpcmStream stream, IdspConfiguration configuration) : this(stream)
        {
            Configuration = configuration;
        }

        /// <summary>
        /// Initializes a new <see cref="Idsp"/> by parsing an existing
        /// IDSP file.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> containing 
        /// the IDSP file. Must be seekable.</param>
        /// <param name="configuration">A <see cref="IdspConfiguration"/>
        /// to use for the <see cref="Idsp"/></param>
        public Idsp(Stream stream, IdspConfiguration configuration) : this(stream)
        {
            Configuration = configuration;
        }

        /// <summary>
        /// Initializes a new <see cref="Idsp"/> by parsing an existing
        /// IDSP file.
        /// </summary>
        /// <param name="file">A <c>byte[]</c> containing 
        /// the IDSP file.</param>
        /// <param name="configuration">A <see cref="IdspConfiguration"/>
        /// to use for the <see cref="Idsp"/></param>
        public Idsp(byte[] file, IdspConfiguration configuration) : this(file)
        {
            Configuration = configuration;
        }

        private Idsp() { }

        /// <summary>
        /// Parses the header of an IDSP file and returns the metadata
        /// and structure data of that file.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> containing 
        /// the IDSP file. Must be seekable.</param>
        /// <returns>A <see cref="IdspStructure"/> containing
        /// the data from the IDSP header.</returns>
        public static IdspStructure ReadMetadata(Stream stream)
        {
            if (!stream.CanSeek)
            {
                throw new NotSupportedException("A seekable stream is required");
            }

            return new Idsp().ReadIdspFile(stream, false);
        }

        /// <summary>
        /// Builds an IDSP file from the current <see cref="AudioStream"/>.
        /// </summary>
        /// <returns>An IDSP file</returns>
        public byte[] GetFile()
        {
            var file = new byte[FileLength];
            var stream = new MemoryStream(file);
            WriteFile(stream);
            return file;
        }

        /// <summary>
        /// Writes the IDSP file to a <see cref="Stream"/>.
        /// The file is written starting at the beginning
        /// of the <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to write the
        /// IDSP to.</param>
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
        }

        private IdspStructure ReadIdspFile(Stream stream, bool readAudioData = true)
        {
            using (BinaryReader reader = new BinaryReaderBE(stream, Encoding.UTF8, true))
            {
                if (Encoding.UTF8.GetString(reader.ReadBytes(4), 0, 4) != "IDSP")
                {
                    throw new InvalidDataException("File has no IDSP header");
                }

                var structure = new IdspStructure();

                return structure;
            }
        }
    }

    public class IdspConfiguration
    {
        
    }
}
