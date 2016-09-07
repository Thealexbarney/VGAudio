using System;
using System.IO;
using DspAdpcm.Adpcm.Formats.Internal;
using DspAdpcm.Adpcm.Formats.Structures;

namespace DspAdpcm.Adpcm.Formats
{
    /// <summary>
    /// Represents a BCSTM file.
    /// </summary>
    public class Bcstm
    {
        private BCFstm BCFstm { get; set; }

        /// <summary>
        /// The underlying <see cref="AdpcmStream"/> used to build the BCSTM file.
        /// </summary>
        public AdpcmStream AudioStream => BCFstm.AudioStream;

        /// <summary>
        /// Contains various settings used when building the BCSTM file.
        /// </summary>
        public BcstmConfiguration Configuration { get; } = new BcstmConfiguration();

        /// <summary>
        /// The size in bytes of the BCSTM file.
        /// </summary>
        public int FileLength => BCFstm.FileLength;

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

            BCFstm = new BCFstm {AudioStream = stream};
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

            BCFstm = new BCFstm();
            BCFstm.ReadBcstmFile(stream);
            Configuration.Configuration = BCFstm.Configuration;
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
                BCFstm = new BCFstm();
                BCFstm.ReadBcstmFile(stream);
                Configuration.Configuration = BCFstm.Configuration;
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
        /// BCSTM file.
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

            return new BCFstm().ReadBcstmFile(stream, false);
        }

        /// <summary>
        /// Builds a BCSTM file from the current <see cref="AudioStream"/>.
        /// </summary>
        /// <returns>A BCSTM file</returns>
        public byte[] GetFile()
        {
            var file = new byte[FileLength];
            var stream = new MemoryStream(file);
            WriteFile(stream);
            return file;
        }

        /// <summary>
        /// Writes the BCSTM file to a <see cref="Stream"/>.
        /// The file is written starting at the beginning
        /// of the <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to write the
        /// BCSTM to.</param>
        public void WriteFile(Stream stream)
        {
            BCFstm.Configuration = Configuration.Configuration;
            BCFstm.WriteBCFstmFile(stream, BCFstm.BCFstmType.Bcstm);
        }
    }

    /// <summary>
    /// Contains the options used to build the BCSTM file.
    /// </summary>
    public class BcstmConfiguration
    {
        internal BCFstmConfiguration Configuration { get; set; } = new BCFstmConfiguration();

        /// <summary>
        /// <inheritdoc cref="B_stmConfiguration.RecalculateSeekTable"/>
        /// </summary>
        public bool RecalculateSeekTable
        {
            get { return Configuration.RecalculateSeekTable; }
            set { Configuration.RecalculateSeekTable = value; }
        }

        /// <summary>
        /// <inheritdoc cref="B_stmConfiguration.RecalculateLoopContext"/>
        /// </summary>
        public bool RecalculateLoopContext
        {
            get { return Configuration.RecalculateLoopContext; }
            set { Configuration.RecalculateLoopContext = value; }
        }

        /// <summary>
        /// <inheritdoc cref="B_stmConfiguration.SamplesPerInterleave"/>
        /// </summary>
        public int SamplesPerInterleave
        {
            get { return Configuration.SamplesPerInterleave; }
            set { Configuration.SamplesPerInterleave = value; }
        }

        /// <summary>
        /// <inheritdoc cref="B_stmConfiguration.SamplesPerSeekTableEntry"/>
        /// </summary>
        public int SamplesPerSeekTableEntry
        {
            get { return Configuration.SamplesPerSeekTableEntry; }
            set { Configuration.SamplesPerSeekTableEntry = value; }
        }

        /// <summary>
        /// <inheritdoc cref="B_stmConfiguration.LoopPointAlignment"/>
        /// </summary>
        public int LoopPointAlignment
        {
            get { return Configuration.LoopPointAlignment; }
            set { Configuration.LoopPointAlignment = value; }
        }

        /// <summary>
        /// If <c>true</c>, include track information in the BCSTM
        /// header. Default is <c>true</c>.
        /// </summary>
        public bool IncludeTrackInformation
        {
            get { return Configuration.IncludeTrackInformation; }
            set { Configuration.IncludeTrackInformation = value; }
        }

        /// <summary>
        /// If <c>true</c>, include an extra chunk in the header
        /// after the stream info and before the track offset table.
        /// The purpose of this chunk is unknown.
        /// Default is <c>false</c>.
        /// </summary>
        public bool InfoPart1Extra
        {
            get { return Configuration.InfoPart1Extra; }
            set { Configuration.InfoPart1Extra = value; }
        }
    }
}
