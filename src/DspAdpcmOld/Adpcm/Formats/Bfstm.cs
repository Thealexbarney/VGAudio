using System.IO;
using DspAdpcm.Adpcm.Formats.Configuration;
using DspAdpcm.Adpcm.Formats.Internal;
using DspAdpcm.Adpcm.Formats.Structures;
using static DspAdpcm.Helpers;

namespace DspAdpcm.Adpcm.Formats
{
    /// <summary>
    /// Represents a BFSTM file.
    /// </summary>
    public class Bfstm
    {
        private BCFstm BCFstm { get; set; }
        private static int HeaderSize => 0x40;

        /// <summary>
        /// The underlying <see cref="AdpcmStream"/> used to build the BFSTM file.
        /// </summary>
        public AdpcmStream AudioStream => BCFstm.AudioStream;

        /// <summary>
        /// Contains various settings used when building the BFSTM file.
        /// </summary>
        public BfstmConfiguration Configuration { get; set; } = new BfstmConfiguration();

        /// <summary>
        /// The size in bytes of the BFSTM file.
        /// </summary>
        public int FileSize => BCFstm.FileSize;

        /// <summary>
        /// Initializes a new <see cref="Bfstm"/> from an <see cref="AdpcmStream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="AdpcmStream"/> used to
        /// create the <see cref="Bfstm"/>.</param>
        /// <param name="configuration">A <see cref="BfstmConfiguration"/>
        /// to use for the <see cref="Bfstm"/></param>
        public Bfstm(AdpcmStream stream, BfstmConfiguration configuration = null)
        {
            if (stream.Channels.Count < 1)
            {
                throw new InvalidDataException("Stream must have at least one channel ");
            }

            BCFstm = new BCFstm { AudioStream = stream };
            Configuration = configuration ?? Configuration;
        }

        /// <summary>
        /// Initializes a new <see cref="Bfstm"/> by parsing an existing
        /// BFSTM file.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> containing 
        /// the BFSTM file. Must be seekable.</param>
        /// <param name="configuration">A <see cref="BfstmConfiguration"/>
        /// to use for the <see cref="Bfstm"/></param>
        public Bfstm(Stream stream, BfstmConfiguration configuration = null)
        {
            ReadStream(stream, Configuration);
        }

        /// <summary>
        /// Initializes a new <see cref="Bfstm"/> by parsing an existing
        /// BFSTM file.
        /// </summary>
        /// <param name="file">A <c>byte[]</c> containing 
        /// the BFSTM file.</param>
        /// <param name="configuration">A <see cref="BfstmConfiguration"/>
        /// to use for the <see cref="Bfstm"/></param>
        public Bfstm(byte[] file, BfstmConfiguration configuration = null)
        {
            using (var stream = new MemoryStream(file))
            {
                ReadStream(stream, Configuration);
            }
        }

        /// <summary>
        /// Parses the header of a BFSTM file and returns the metadata
        /// and structure data of that file.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> containing 
        /// the BFSTM file. Must be seekable.</param>
        /// <returns>A <see cref="BfstmStructure"/> containing
        /// the data from the BFSTM header.</returns>
        public static BfstmStructure ReadMetadata(Stream stream)
        {
            CheckStream(stream, HeaderSize);
            return (BfstmStructure)BCFstm.ReadBCFstmFile(stream, false);
        }

        /// <summary>
        /// Builds a BFSTM file from the current <see cref="AudioStream"/>.
        /// </summary>
        /// <returns>A BFSTM file.</returns>
        public byte[] GetFile()
        {
            var file = new byte[FileSize];
            var stream = new MemoryStream(file);
            WriteFile(stream);
            return file;
        }

        /// <summary>
        /// Writes the BFSTM file to a <see cref="Stream"/>.
        /// The file is written starting at the beginning
        /// of the <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to write the
        /// BFSTM file to.</param>
        public void WriteFile(Stream stream)
        {
            BCFstm.Configuration = Configuration.Configuration;
            BCFstm.WriteBCFstmFile(stream, BCFstm.BCFstmType.Bfstm);
        }

        private void ReadStream(Stream stream, BfstmConfiguration configuration = null)
        {
            BCFstm = new BCFstm(stream, configuration?.Configuration);
            Configuration.Configuration = BCFstm.Configuration;
        }
    }
}
